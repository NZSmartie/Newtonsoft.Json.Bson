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
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.IO;
using Newtonsoft.Json.Cbor.Utilities;
using Newtonsoft.Json.Linq;

namespace Newtonsoft.Json.Cbor
{
    /// <summary>
    /// Represents a reader that provides fast, non-cached, forward-only access to serialized BSON data.
    /// </summary>
    public partial class CborDataReader : JsonReader
    {
        private readonly BinaryReader _reader;
        private readonly List<ContainerContext> _stack;

        private ContainerContext _currentContext;

        private class ContainerContext
        {
            public readonly CborMajorType MajorType;
            public int Length;

            public ContainerContext(CborMajorType majorType)
            {
                MajorType = majorType;
            }
        }

        public ulong? Tag { get; protected set; }

        /// <summary>
        /// Gets or sets a value indicating whether binary data reading should be compatible with incorrect Json.NET 3.5 written binary.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if binary data reading will be compatible with incorrect Json.NET 3.5 written binary; otherwise, <c>false</c>.
        /// </value>
        [Obsolete("JsonNet35BinaryCompatibility will be removed in a future version of Json.NET.")]
        public bool JsonNet35BinaryCompatibility { get; set; }

        /// <summary>
        /// Gets or sets the default <see cref="DateTimeKind" /> used when reading <see cref="DateTime"/> values from Cbor.
        /// </summary>
        /// <value>The <see cref="DateTimeKind" /> used when reading <see cref="DateTime"/> values from BSON.</value>
        public DateTimeKind DateTimeKindHandling { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CborDataReader"/> class.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> containing the BSON data to read.</param>
        public CborDataReader(Stream stream)
            : this(stream, DateTimeKind.Local)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CborDataReader"/> class.
        /// </summary>
        /// <param name="reader">The <see cref="BinaryReader"/> containing the BSON data to read.</param>
        public CborDataReader(BinaryReader reader)
            : this(reader, DateTimeKind.Local)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CborDataReader"/> class.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> containing the BSON data to read.</param>
        /// <param name="readRootValueAsArray">if set to <c>true</c> the root object will be read as a JSON array.</param>
        /// <param name="dateTimeKindHandling">The <see cref="DateTimeKind" /> used when reading <see cref="DateTime"/> values from BSON.</param>
        public CborDataReader(Stream stream, DateTimeKind dateTimeKindHandling)
        {
            ValidationUtils.ArgumentNotNull(stream, nameof(stream));
            _stack = new List<ContainerContext>();
            DateTimeKindHandling = dateTimeKindHandling;
//#if HAVE_ASYNC
//            if (GetType() == typeof(CborDataReader))
//            {
//                _reader = _asyncReader = new AsyncBinaryReader(stream);
//                return;
//            }
//#endif
            _reader = new BinaryReader(stream);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CborDataReader"/> class.
        /// </summary>
        /// <param name="reader">The <see cref="BinaryReader"/> containing the BSON data to read.</param>
        /// <param name="dateTimeKindHandling">The <see cref="DateTimeKind" /> used when reading <see cref="DateTime"/> values from BSON.</param>
        public CborDataReader(BinaryReader reader, DateTimeKind dateTimeKindHandling)
        {
            ValidationUtils.ArgumentNotNull(reader, nameof(reader));
            _stack = new List<ContainerContext>();
            DateTimeKindHandling = dateTimeKindHandling;
//#if HAVE_ASYNC
//            if (GetType() == typeof(CborDataReader) && reader.GetType() == typeof(BinaryWriter))
//            {
//                _reader = _asyncReader = new AsyncBinaryReaderOwningReader(reader);
//                return;
//            }
//#endif
            _reader = reader;
        }

        /// <summary>
        /// Reads the next JSON token from the underlying <see cref="Stream"/>.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the next token was read successfully; <c>false</c> if there are no more tokens to read.
        /// </returns>
        public override bool Read()
        {
            try
            {
                if (CurrentState == State.PostValue)
                    SetStateBasedOnCurrent();
                
                switch (CurrentState)
                {
                    case State.Start:
                    case State.Property:
                    case State.Array:
                    case State.ArrayStart:
                    case State.Constructor:
                    case State.ConstructorStart:
                        return ParseValue();
                    case State.Object:
                    case State.ObjectStart:
                        return ParsePropertyName();
                    case State.Finished:
                        SetToken(JsonToken.None);
                        return false;
                    default:
                        throw new JsonReaderException($"Unexpected state: {CurrentState}.");
                }
            }
            catch (EndOfStreamException)
            {
                SetToken(JsonToken.None);
                return false;
            }
        }

        /// <summary>
        /// Changes the reader's state to <see cref="JsonReader.State.Closed"/>.
        /// If <see cref="JsonReader.CloseInput"/> is set to <c>true</c>, the underlying <see cref="Stream"/> is also closed.
        /// </summary>
        public override void Close()
        {
            base.Close();

            if (CloseInput)
            {
#if HAVE_STREAM_READER_WRITER_CLOSE
                _reader?.Close();
#else
                _reader?.Dispose();
#endif
            }
        }

        private void PopContext()
        {
            _stack.RemoveAt(_stack.Count - 1);
            if (_stack.Count == 0)
            {
                _currentContext = null;
            }
            else
            {
                _currentContext = _stack[_stack.Count - 1];
            }
        }

        private void PushContext(ContainerContext newContext)
        {
            _stack.Add(newContext);
            _currentContext = newContext;
        }

        private bool ParseValue()
        {
            while (true)
            {
                if (_currentContext != null)
                {
                    if (_currentContext.Length > 0)
                        _currentContext.Length--;
                    else if (_currentContext.Length == 0)
                    {
                        if (_currentContext.MajorType == CborMajorType.Array)
                            SetToken(JsonToken.EndArray);
                        else if (_currentContext.MajorType == CborMajorType.Map)
                            SetToken(JsonToken.EndObject);
                        else
                            throw new InvalidOperationException($"Should not have reached end of collection for MajorType {_currentContext.MajorType}");
                        PopContext();
                        return true;
                    }
                }

                var initialByte = ReadInitialByte();
                var majorType = initialByte.Item1;
                var simpleType = initialByte.Item2;

                if (_currentContext != null && 
                    (_currentContext.MajorType == CborMajorType.ByteString || _currentContext.MajorType == CborMajorType.TextString) && 
                    _currentContext.MajorType != majorType && !(majorType == CborMajorType.Primitive && simpleType == CborSimpleType.Break))
                    throw new InvalidDataException("Indefinite byte/text string has incorrect nested major type");

                if (majorType == CborMajorType.Tagged)
                {
                    Tag = ReadUInt64(simpleType);

                    initialByte = ReadInitialByte();
                    majorType = initialByte.Item1;
                    simpleType = initialByte.Item2;
                }
                else
                {
                    Tag = null;
                }

                switch (majorType)
                {
                    case CborMajorType.UnsignedInteger:
                        var un = ReadUInt64(simpleType);
                        if (un <= int.MaxValue)
                            SetToken(JsonToken.Integer, Convert.ToInt32(un));
                        else if (un <= long.MaxValue)
                            SetToken(JsonToken.Integer, Convert.ToInt64(un));
                        else
                            SetToken(JsonToken.Integer, un);
                        return true;
                    case CborMajorType.NegativeInteger:
                        var sn = -1 - Convert.ToInt64(ReadUInt64(simpleType));
                        if (sn >= int.MinValue)
                            SetToken(JsonToken.Integer, Convert.ToInt32(sn));
                        else if (sn >= long.MinValue)
                            SetToken(JsonToken.Integer, sn);
                        else
                            throw new IndexOutOfRangeException();
                        return true;
                    case CborMajorType.Primitive:
                        if (simpleType == CborSimpleType.False)
                        {
                            SetToken(JsonToken.Boolean, false);
                            return true;
                        }
                        else if (simpleType == CborSimpleType.True)
                        {
                            SetToken(JsonToken.Boolean, true);
                            return true;
                        }
                        else if (simpleType == CborSimpleType.Null)
                        {
                            SetToken(JsonToken.Null);
                            return true;
                        }
                        else if (simpleType == CborSimpleType.Undefined)
                        {
                            SetToken(JsonToken.Undefined);
                            return true;
                        }
                        else if (simpleType == CborSimpleType.HalfFloat)
                        {
                            // Suddenly Half-Floats! Thanks to Appendix D. of RFC 7049
                            var half = _reader.ReadUInt16BE();

                            var exp = (half >> 10) & 0x1F;
                            var mant = half & 0x3FF;
                            double val;
                            if (exp == 0) val = mant * Math.Pow(2, -24);
                            else if (exp != 31) val = (mant + 1024) * Math.Pow(2, exp - 25);
                            else val = mant == 0 ? double.PositiveInfinity : double.NaN;

                            SetToken(JsonToken.Float, ((half & 0x8000) > 0) ? -val : val);
                            return true;
                        }
                        if (simpleType == CborSimpleType.SingleFloat)
                        {
                            SetToken(JsonToken.Float, BitConverter.ToSingle(_reader.ReadBytesBE(4), 0));
                            return true;
                        }
                        else if (simpleType == CborSimpleType.DoubleFloat)
                        {
                            SetToken(JsonToken.Float, BitConverter.ToDouble(_reader.ReadBytesBE(8), 0));
                            return true;
                        }
                        else if (simpleType == CborSimpleType.Break)
                        {
                            if ((_currentContext?.Length ?? 0) == -1)
                            {
                                if (_currentContext.MajorType == CborMajorType.Array)
                                {
                                    SetToken(JsonToken.EndArray);
                                    PopContext();
                                    return true;
                                }
                                if (_currentContext.MajorType == CborMajorType.Map)
                                {
                                    SetToken(JsonToken.EndObject);
                                    PopContext();
                                    return true;
                                }
                                if (_currentContext.MajorType == CborMajorType.ByteString || _currentContext.MajorType == CborMajorType.TextString)
                                {
                                    PopContext();
                                }
                                else
                                    throw new InvalidDataException();

                            }
                            else
                                throw new InvalidDataException("Cannot break non indefinite collection");
                        }
                        //else
                        //throw new InvalidDataException();
                        break;
                    case CborMajorType.ByteString:
                        if (simpleType != CborSimpleType.Break)
                        {
                            if(_currentContext?.MajorType == CborMajorType.ByteString || !Tag.HasValue)
                                SetToken(JsonToken.Bytes, _reader.ReadBytes(Convert.ToInt32(ReadUInt32(simpleType))));
                            else
                                DecodeBytes(Tag.Value, _reader.ReadBytes(Convert.ToInt32(ReadUInt32(simpleType))));
                            return true;
                        }

                        if (_currentContext != null && _currentContext.MajorType == CborMajorType.ByteString && _currentContext.Length == -1)
                            throw new InvalidDataException("Nested indefinite byte-string is not permitted");
                        PushContext(new ContainerContext(CborMajorType.ByteString)
                        {
                            Length = -1
                        });
                        break;
                    case CborMajorType.TextString:
                        if (simpleType != CborSimpleType.Break)
                        {
                            if(_currentContext?.MajorType == CborMajorType.TextString || !Tag.HasValue)
                                SetToken(JsonToken.String, Encoding.UTF8.GetString(_reader.ReadBytes(Convert.ToInt32(ReadUInt32(simpleType)))));
                            else
                                DecodeString(Tag.Value, Encoding.UTF8.GetString(_reader.ReadBytes(Convert.ToInt32(ReadUInt32(simpleType)))));
                            return true;
                        }

                        if (_currentContext != null && _currentContext.MajorType == CborMajorType.TextString && _currentContext.Length == -1)
                            throw new InvalidDataException("Nested indefinite text-string is not permitted");
                        PushContext(new ContainerContext(CborMajorType.TextString)
                        {
                            Length = -1
                        });
                        break;
                    case CborMajorType.Array:
                        PushContext(new ContainerContext(CborMajorType.Array)
                        {
                            Length = simpleType == CborSimpleType.Break ? -1 : Convert.ToInt32(ReadUInt32(simpleType))
                        });
                        SetToken(JsonToken.StartArray);
                        return true;
                    case CborMajorType.Map:
                        PushContext(new ContainerContext(CborMajorType.Map)
                        {
                            Length = simpleType == CborSimpleType.Break ? -1 : (Convert.ToInt32(ReadUInt32(simpleType)) * 2)
                        });
                        SetToken(JsonToken.StartObject);
                        return true;
                    default:
                        throw new InvalidOperationException();
                }
            }
        }

        private bool ParsePropertyName()
        {
            if (_currentContext == null)
                throw new InvalidOperationException("Cannot parse property name without context");
            if(_currentContext.MajorType != CborMajorType.Map)
                throw new InvalidOperationException("Cannot parse property name with invalid context");

            if (_currentContext.Length > 0)
                _currentContext.Length--;
            else if (_currentContext.Length == 0) { 
                SetToken(JsonToken.EndObject);
                PopContext();
                return true;
            }

            var initialByte = ReadInitialByte();
            var majorType = initialByte.Item1;
            var simpleType = initialByte.Item2;

            if (majorType == CborMajorType.Tagged)
            {
                // State.Tag = (int)Value;
                // Todo: Support Optional Tags

                initialByte = ReadInitialByte();
                majorType = initialByte.Item1;
                simpleType = initialByte.Item2;
            }
            if (majorType != CborMajorType.TextString)
                throw new InvalidDataException($"Expecting string, got {majorType} instead");

            if (simpleType == CborSimpleType.Break)
                throw new InvalidDataException($"Indefinite strings are not supported for property names");

            SetToken(JsonToken.PropertyName, Encoding.UTF8.GetString(_reader.ReadBytes(Convert.ToInt32(ReadUInt32(simpleType)))));
            return true;
        }

        private Tuple<CborMajorType, CborSimpleType> ReadInitialByte()
        {
            var b = _reader.ReadByte();
            return Tuple.Create((CborMajorType)(b & 0xE0), (CborSimpleType)(b & 0x1F));
        }

        private void DecodeString(ulong tag, string value)
        {
            switch (tag)
            {
                case 0: // DateTime String
                    SetToken(JsonToken.Date, DateTime.ParseExact(value, "u", CultureInfo.InvariantCulture));
                    return;
                default:
                    SetToken(JsonToken.String, value);
                    return;
            }
            
        }

        private void DecodeBytes(ulong tag, byte[] value)
        {
            switch (tag)
            {
                case 37: // UUID binary format
                    SetToken(JsonToken.Bytes, new Guid(value));
                    return;
                default:
                    SetToken(JsonToken.Bytes, value);
                    return;
            }

        }

        private byte[] ReadBytes(int count)
        {
            return _reader.ReadBytes(count);
        }

        private uint ReadUInt32(CborSimpleType simpleType)
        {
            if ((byte)simpleType < 24)
                return Convert.ToUInt32((byte) simpleType);

            switch ((byte)simpleType)
            {
                case 24:
                    return Convert.ToUInt32(_reader.ReadByte()); 
                case 25:
                    return Convert.ToUInt32(_reader.ReadUInt16BE());
                case 26:
                    return _reader.ReadUInt32BE();
                case 27:
                    return Convert.ToUInt32(_reader.ReadUInt64BE());
                case 28: case 29: case 30:
                    throw new InvalidDataException(); // unassigned
                default:
                    throw new InvalidOperationException($"Invalid simple type ({(int)simpleType}) passed to {nameof(ReadUInt32)}");
            }
        }

        private ulong ReadUInt64(CborSimpleType simpleType)
        {
            if ((byte)simpleType < 24)
                return Convert.ToUInt64((byte)simpleType);

            switch ((byte)simpleType)
            {
                case 24:
                    return Convert.ToUInt64(_reader.ReadByte());
                case 25:
                    return Convert.ToUInt64(_reader.ReadUInt16BE());
                case 26:
                    return Convert.ToUInt64(_reader.ReadUInt32BE());
                case 27:
                    return _reader.ReadUInt64BE();
                case 28: case 29: case 30:
                    throw new InvalidDataException(); // unassigned
                default:
                    throw new InvalidOperationException($"Invalid simple type ({(int)simpleType}) passed to {nameof(ReadUInt64)}");
            }
        }
    }
}