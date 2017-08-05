#region License
// Copyright (c) 2017 James Newton-King
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
#if HAVE_BIG_INTEGER
using System.Numerics;
#endif
using System.Text;
using Newtonsoft.Json.Cbor.Utilities;
using Newtonsoft.Json.Linq;
using System.Globalization;
using System.Linq;

namespace Newtonsoft.Json.Cbor
{
    /// <summary>
    /// Represents a writer that provides a fast, non-cached, forward-only way of generating BSON data.
    /// </summary>
    public partial class CborDataWriter : JsonWriter
    {
        public DateTimeKind DateTimeKindHandling { get; set; }

        public DateTimeEncoding DateTimeEncoding { get; set; }
        
        public CborWriterCollectionBehaviour CollectionBehaviour { get; set; }
        
        private readonly BinaryWriter _writer;

        private readonly Stack<CborCollectionToken> _collectionStack = new Stack<CborCollectionToken>();

        private readonly Queue<CborToken> _values = new Queue<CborToken>();

        private class CborToken
        {
            public readonly CborMajorType MajorType;

            public CborToken(CborMajorType majorType)
            {
                MajorType = majorType;
            }
        }


        #region CborTokens

        private class CborTokenValue : CborToken
        {
            public byte[] Value;

            public CborTokenValue(CborMajorType majorType, byte[] value)
                : base(majorType)
            {
                Value = value;
            }
        }

        private class CborSimpleToken : CborToken
        {
            public CborSimpleType SimpleType;

            public CborSimpleToken(CborMajorType majorType, CborSimpleType simpleType)
                : base(majorType)
            {
                SimpleType = simpleType;
            }

            public CborSimpleToken(CborMajorType majorType, byte simpleType)
                : base(majorType)
            {
                SimpleType = (CborSimpleType)simpleType;
            }
        }

        private class CborCollectionToken : CborToken
        {
            public int Items = 0;

            public CborCollectionToken(CborMajorType majorType) 
                : base(majorType)
            { }
        }


        #endregion

        
        public CborDataWriter(Stream stream)
            :this(stream, CborWriterCollectionBehaviour.DefiniteWherePossible)
        { }

        public CborDataWriter(Stream stream, CborWriterCollectionBehaviour collectionBehaviour)
        {
            CollectionBehaviour = collectionBehaviour;
            _writer = new BinaryWriter(stream);
        }

        public override void Flush()
        {
            FlushInternal(false);
        }

        private void FlushInternal(bool safe = true)
        {
            while (_values.Count > 0)
            {
                var token = _values.Peek();

                if (token is CborCollectionToken)
                {
                    var collection = token as CborCollectionToken;
                    if (_collectionStack.Contains(collection))
                    {
                        // Final size is not available for collection still being built.
                        if (CollectionBehaviour != CborWriterCollectionBehaviour.AlwaysIndefinite || !safe)
                            return;
                        collection.Items = -1;
                    }

                    if (collection.Items == -1)
                        EncodeSimpleType(collection.MajorType, (byte) CborSimpleType.Break);
                    else
                        Encode(collection.MajorType, Convert.ToUInt32(collection.Items));
                }
                else if (token is CborSimpleToken)
                {
                    var simple = token as CborSimpleToken;
                    EncodeSimpleType(simple.MajorType, (byte) simple.SimpleType);
                }
                else if (token is CborTokenValue)
                {
                    var value = token as CborTokenValue;
                    if (value.MajorType == CborMajorType.ByteString || value.MajorType == CborMajorType.TextString)
                    {
                        Encode(value.MajorType, Convert.ToUInt32(value.Value?.Length ?? 0));
                        _writer.Write(value.Value);
                    }
                    else
                    {
                        Encode(value.MajorType, value.Value);
                    }
                }
                else
                {
                    throw new InvalidOperationException();
                }

                _values.Dequeue();
            }
            
            _writer.Flush();
        }

        public override void Close()
        {
            base.Close();
            if (CloseOutput)
#if HAVE_STREAM_READER_WRITER_CLOSE
                _writer.Close();
#else
                _writer.Dispose();
#endif
        }

        public override void WriteStartObject()
        {
            base.WriteStartObject();
            
            CountItem();
            _collectionStack.Push(new CborCollectionToken(CborMajorType.Map));
            _values.Enqueue(_collectionStack.Peek());
        }

        public override void WriteEndObject()
        {
            if(_collectionStack.Peek()?.MajorType != CborMajorType.Map)
                throw new JsonWriterException("Invalid state to end object on");
            
            base.WriteEndObject();

            FlushInternal(Top != 0);
        }

        public override void WriteStartArray()
        {
            base.WriteStartArray();

            CountItem();
            _collectionStack.Push(new CborCollectionToken(CborMajorType.Array));
            _values.Enqueue(_collectionStack.Peek());
        }

        public override void WriteEndArray()
        {
            if (_collectionStack.Peek()?.MajorType != CborMajorType.Array)
                throw new JsonWriterException("Invalid state to end array on");

            base.WriteEndArray();

            FlushInternal(Top != 0);
        }

        public override void WriteStartConstructor(string name)
        {
            throw new JsonWriterException("Cannot write JSON constructor as CBOR.");
        }

        public override void WritePropertyName(string name)
        {
            if (_collectionStack.Peek()?.MajorType != CborMajorType.Map)
                throw new JsonWriterException("Invalid state to write property name");
            
            base.WritePropertyName(name);
            _values.Enqueue(new CborTokenValue(CborMajorType.TextString, Encoding.UTF8.GetBytes(name)));
        }

        protected override void WriteEnd(JsonToken token)
        {
            base.WriteEnd(token);
            
            var collection = _collectionStack.Pop();
            if (collection.Items == -1)
                _values.Enqueue(new CborSimpleToken(CborMajorType.Primitive, CborSimpleType.Break));

            FlushInternal(Top != 0);
        }
        
        
#region WriteValue overrides


        public override void WriteNull()
        {
            base.WriteNull();
            _values.Enqueue(new CborSimpleToken(CborMajorType.Primitive, CborSimpleType.Null));
            CountItem();
        }

        public override void WriteUndefined()
        {
            base.WriteUndefined();
            _values.Enqueue(new CborSimpleToken(CborMajorType.Primitive, CborSimpleType.Undefined));
            CountItem();
        }

        public override void WriteRaw(string json)
        {
            throw new JsonWriterException("Cannot write raw JSON as CBOR.");
        }

        public override void WriteRawValue(string json)
        {
            throw new JsonWriterException("Cannot write raw JSON as CBOR.");
        }

        public override void WriteValue(string value)
        {
            base.WriteValue(value);

            _values.Enqueue(new CborTokenValue(CborMajorType.TextString, Encoding.UTF8.GetBytes(value)));
            CountItem();
        }

        public override void WriteValue(int value)
        {
            base.WriteValue(value);
            EnqueueValue(value);
        }

        public override void WriteValue(uint value)
        {
            base.WriteValue(value);
            EnqueueValue(value);
        }

        public override void WriteValue(long value)
        {
            base.WriteValue(value);
            EnqueueValue(value);
        }

        public override void WriteValue(ulong value)
        {
            base.WriteValue(value);
            EnqueueValue(value);
        }

        public override void WriteValue(float value)
        {
            base.WriteValue(value);
            _values.Enqueue(new CborTokenValue(CborMajorType.Primitive, BitConverter.GetBytes(value)));
            CountItem();
        }

        public override void WriteValue(double value)
        {
            base.WriteValue(value);
            _values.Enqueue(new CborTokenValue(CborMajorType.Primitive, BitConverter.GetBytes(value)));
            CountItem();
        }

        public override void WriteValue(bool value)
        {
            base.WriteValue(value);
            _values.Enqueue(new CborSimpleToken(CborMajorType.Primitive, value ? CborSimpleType.True : CborSimpleType.False));
            CountItem();
        }

        public override void WriteValue(short value)
        {
            base.WriteValue(value);
            EnqueueValue(value);
        }

        public override void WriteValue(ushort value)
        {
            base.WriteValue(value);
            EnqueueValue(value);
        }

        public override void WriteValue(char value)
        {
            base.WriteValue(value);
            _values.Enqueue(new CborTokenValue(CborMajorType.TextString, Encoding.UTF8.GetBytes(new[] {value})));
            CountItem();
        }

        public override void WriteValue(byte value)
        {
            base.WriteValue(value);
            EnqueueValue(value);
        }

        public override void WriteValue(sbyte value)
        {
            base.WriteValue(value);
            EnqueueValue(value);
        }

        public override void WriteValue(decimal value)
        {
            throw new NotImplementedException();
            base.WriteValue(value);
            
        }

        /// <inheritdoc />
        public override void WriteValue(DateTime value)
        {
            base.WriteValue(value);
            WriteDateTime(value);
        }

        /// <inheritdoc />
        public override void WriteValue(DateTimeOffset value)
        {
            base.WriteValue(value);
            WriteDateTime(value.DateTime);
        }

        private void WriteDateTime(DateTime dateTime)
        {
            switch (DateTimeKindHandling)
            {
                case DateTimeKind.Local:
                    dateTime = dateTime.ToLocalTime();
                    break;
                case DateTimeKind.Utc:
                    dateTime = dateTime.ToUniversalTime();
                    break;
                case DateTimeKind.Unspecified:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(DateTimeKindHandling));
            }
            switch (DateTimeEncoding)
            {
                case DateTimeEncoding.String:
                    WriteTag(0);
                    _values.Enqueue(new CborTokenValue(CborMajorType.TextString,
                        Encoding.UTF8.GetBytes(dateTime.ToString("u"))));
                    CountItem();
                    break;
                case DateTimeEncoding.Epoch:
                    WriteTag(1);
                    EnqueueValue(Convert.ToUInt64(dateTime.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKindHandling))
                        .TotalSeconds));
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(DateTimeEncoding));
            }
        }

        /// <inheritdoc />
        public override void WriteValue(TimeSpan value)
        {
            base.WriteValue(value);
            _values.Enqueue(new CborTokenValue(CborMajorType.TextString,
                Encoding.UTF8.GetBytes(value.ToString())));
            CountItem();
        }

        /// <inheritdoc />
        public override void WriteValue(Guid value)
        {
            base.WriteValue(value);
            WriteTag(37);
            _values.Enqueue(new CborTokenValue(CborMajorType.ByteString,
                value.ToByteArray()));
            CountItem();
        }

        /// <inheritdoc />
        public override void WriteValue(Uri value)
        {
            if (value == null)
            {
                WriteNull();
                return;
            }

            base.WriteValue(value);
            _values.Enqueue(new CborTokenValue(CborMajorType.TextString, Encoding.UTF8.GetBytes(value.ToString())));
            CountItem();
        }

        public override void WriteValue(byte[] value)
        {
            base.WriteValue(value);
            _values.Enqueue(new CborTokenValue(CborMajorType.ByteString, value ?? new byte[] { }));
            CountItem();
        }

        public override void WriteValue(object value)
        {
            throw new NotImplementedException();
            base.WriteValue(value);
        }

        public override void WriteComment(string text)
        {
            throw new JsonWriterException("Cannot write JSON comment as CBOR.");
        }

        public override void WriteWhitespace(string ws)
        {
            throw new JsonWriterException("Cannot write whitespace as CBOR.");
        }

        
#endregion

        
        public void WriteTag(ulong tag)
        {
            EnqueueValue(CborMajorType.Tagged, tag, false);
        }

        private void EnqueueValue(long value)
        {
            if (value < 0)
                EnqueueValue(CborMajorType.NegativeInteger, (ulong) (-1 - value));
            else
                EnqueueValue(CborMajorType.UnsignedInteger,(ulong) value);
        }

        private void EnqueueValue(ulong value)
        {
            EnqueueValue(CborMajorType.UnsignedInteger, value);
        }

        private void EnqueueValue(CborMajorType majorType, ulong value, bool count = true)
        {
            if (value < 24)
            {
                _values.Enqueue(new CborSimpleToken(majorType, (byte) value));
            }
            else
            {
                byte[] bytes;
                if (value <= byte.MaxValue)
                    bytes = new[] {(byte) value};
                else if (value <= ushort.MaxValue)
                    bytes = BitConverter.GetBytes((ushort) value);
                else if (value <= uint.MaxValue)
                    bytes = BitConverter.GetBytes((uint) value);
                else
                    bytes = BitConverter.GetBytes(value);

                _values.Enqueue(new CborTokenValue(majorType, bytes));
            }
            if(count)
                CountItem();
        }

        private void CountItem()
        {
            if(_collectionStack.Count > 0 && _collectionStack.Peek()?.Items >= 0)
                _collectionStack.Peek().Items++;
        }

        private void EncodeSimpleType(CborMajorType majorType, byte value)
        {
            _writer.Write((byte)((byte)majorType + (value & 0x1F)));
        }

        private void Encode(CborMajorType majorType, UInt64 value)
        {
            if (value < 24)
                EncodeSimpleType(majorType, (byte)value);
            else if (value <= byte.MaxValue)
                Encode(majorType, new byte[] { (byte)value });
            else if (value <= ushort.MaxValue)
                Encode(majorType, BitConverter.GetBytes((ushort)value));
            else if (value <= uint.MaxValue)
                Encode(majorType, BitConverter.GetBytes((uint)value));
            else
                Encode(majorType, BitConverter.GetBytes(value));
        }

        private void Encode(CborMajorType majorType, byte[] bytes)
        {
            byte ib = (byte)majorType;

            if (bytes.Length == 1)
                _writer.Write((byte)(ib + 24));
            else if (bytes.Length == 2)
                _writer.Write((byte)(ib + 25));
            else if (bytes.Length == 4)
                _writer.Write((byte)(ib + 26));
            else if (bytes.Length == 8)
                _writer.Write((byte)(ib + 27));
            else
                throw new InvalidOperationException("Can not encode non primiative bytes");

            if (BitConverter.IsLittleEndian)
                _writer.Write(bytes.Reverse().ToArray());
            else
                _writer.Write(bytes);
        }
    }

    public enum DateTimeEncoding
    {
        String,
        Epoch
    }
}