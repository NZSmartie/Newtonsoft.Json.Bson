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
using System.Reflection;
using System.Runtime.Serialization;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Tests.TestObjects;
#if !(NET20 || NET35 || PORTABLE) || NETSTANDARD1_3
using System.Numerics;
#endif
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Newtonsoft.Json.Cbor;
using System.IO;
using Newtonsoft.Json.Utilities;
using Newtonsoft.Json.Linq;

namespace Newtonsoft.Json.Cbor.Tests
{
    [TestFixture]
    public class CborDataReaderTests : TestFixtureBase
    {
        private const char Euro = '\u20ac';

        [Test]
        public void DeserializeLargeBsonObject()
        {
            throw new NotImplementedException();
            //byte[] data = System.IO.File.ReadAllBytes(ResolvePath(@"SpaceShipV2.bson"));

            //MemoryStream ms = new MemoryStream(data);
            //BsonDataReader reader = new BsonDataReader(ms);

            //JObject o = (JObject)JToken.ReadFrom(reader);

            //Assert.AreEqual("1", (string)o["$id"]);
        }

        public class MyTest
        {
            public DateTime TimeStamp { get; set; }
            public string UserName { get; set; }
            public MemoryStream Blob { get; set; }
        }

        public void Bson_SupportMultipleContent()
        {
            MemoryStream myStream = new MemoryStream();
            CborDataWriter writer = new CborDataWriter(myStream);
            JsonSerializer serializer = new JsonSerializer();
            MyTest tst1 = new MyTest
            {
                TimeStamp = new DateTime(2000, 12, 20, 12, 59, 59, DateTimeKind.Utc),
                UserName = "Joe Doe"
            };
            MyTest tst2 = new MyTest
            {
                TimeStamp = new DateTime(2010, 12, 20, 12, 59, 59, DateTimeKind.Utc),
                UserName = "Bob"
            };
            serializer.Serialize(writer, tst1);
            serializer.Serialize(writer, tst2);

            myStream.Seek(0, SeekOrigin.Begin);

            CborDataReader reader = new CborDataReader(myStream)
            {
                SupportMultipleContent = true,
                DateTimeKindHandling = DateTimeKind.Utc
            };

            MyTest tst1A = serializer.Deserialize<MyTest>(reader);

            reader.Read();

            MyTest tst2A = serializer.Deserialize<MyTest>(reader);

            Assert.AreEqual(tst1.UserName, tst1A.UserName);
            Assert.AreEqual(tst1.TimeStamp, tst1A.TimeStamp);

            Assert.AreEqual(tst2.UserName, tst2A.UserName);
            Assert.AreEqual(tst2.TimeStamp, tst2A.TimeStamp);
        }

        [Test]
        public void CloseInput()
        {
            MemoryStream ms = new MemoryStream();
            CborDataReader reader = new CborDataReader(ms);

            Assert.IsTrue(ms.CanRead);
            reader.Close();
            Assert.IsFalse(ms.CanRead);

            ms = new MemoryStream();
            reader = new CborDataReader(ms) { CloseInput = false };

            Assert.IsTrue(ms.CanRead);
            reader.Close();
            Assert.IsTrue(ms.CanRead);
        }

        [Test]
        public void ReadSingleObject()
        {
            byte[] data = HexToBytes("A1-64-42-6C-61-68-01");
            MemoryStream ms = new MemoryStream(data);
            CborDataReader reader = new CborDataReader(ms);

            Assert.IsTrue(reader.Read());
            Assert.AreEqual(JsonToken.StartObject, reader.TokenType);

            Assert.IsTrue(reader.Read());
            Assert.AreEqual(JsonToken.PropertyName, reader.TokenType);
            Assert.AreEqual("Blah", reader.Value);
            Assert.AreEqual(typeof(string), reader.ValueType);

            Assert.IsTrue(reader.Read());
            Assert.AreEqual(JsonToken.Integer, reader.TokenType);
            Assert.AreEqual(1, reader.Value);
            Assert.AreEqual(typeof(int), reader.ValueType);

            Assert.IsTrue(reader.Read());
            Assert.AreEqual(JsonToken.EndObject, reader.TokenType);

            Assert.IsFalse(reader.Read());
            Assert.AreEqual(JsonToken.None, reader.TokenType);
        }

        [Test]
        public void ReadGuid_Text()
        {
            byte[] data = HexToBytes("81-78-24-64-38-32-31-65-65-64-37-2D-34-62-35-63-2D-34-33-63-39-2D-38-61-63-32-2D-36-39-32-38-65-35-37-39-62-37-30-35");

            MemoryStream ms = new MemoryStream(data);
            CborDataReader reader = new CborDataReader(ms);

            Assert.IsTrue(reader.Read());
            Assert.AreEqual(JsonToken.StartArray, reader.TokenType);

            Assert.IsTrue(reader.Read());
            Assert.AreEqual(JsonToken.String, reader.TokenType);
            Assert.AreEqual("d821eed7-4b5c-43c9-8ac2-6928e579b705", reader.Value);
            Assert.AreEqual(typeof(string), reader.ValueType);

            Assert.IsTrue(reader.Read());
            Assert.AreEqual(JsonToken.EndArray, reader.TokenType);

            Assert.IsFalse(reader.Read());
            Assert.AreEqual(JsonToken.None, reader.TokenType);

            ms = new MemoryStream(data);
            reader = new CborDataReader(ms);

            JsonSerializer serializer = new JsonSerializer();
            IList<Guid> l = serializer.Deserialize<IList<Guid>>(reader);

            Assert.AreEqual(1, l.Count);
            Assert.AreEqual(new Guid("D821EED7-4B5C-43C9-8AC2-6928E579B705"), l[0]);
        }

        [Test]
        public void ReadGuid_Bytes()
        {
            byte[] data = HexToBytes("81-D8-25-50-D7-EE-21-D8-5C-4B-C9-43-8A-C2-69-28-E5-79-B7-05");

            MemoryStream ms = new MemoryStream(data);
            CborDataReader reader = new CborDataReader(ms);

            Assert.IsTrue(reader.Read());
            Assert.AreEqual(JsonToken.StartArray, reader.TokenType);

            Guid g = new Guid("D821EED7-4B5C-43C9-8AC2-6928E579B705");

            Assert.IsTrue(reader.Read());
            Assert.AreEqual(JsonToken.Bytes, reader.TokenType);
            Assert.AreEqual(g, reader.Value);
            Assert.AreEqual(typeof(Guid), reader.ValueType);

            Assert.IsTrue(reader.Read());
            Assert.AreEqual(JsonToken.EndArray, reader.TokenType);

            Assert.IsFalse(reader.Read());
            Assert.AreEqual(JsonToken.None, reader.TokenType);

            ms = new MemoryStream(data);
            reader = new CborDataReader(ms);

            JsonSerializer serializer = new JsonSerializer();
            IList<Guid> l = serializer.Deserialize<IList<Guid>>(reader);

            Assert.AreEqual(1, l.Count);
            Assert.AreEqual(g, l[0]);
        }

        [Test]
        public void ReadDouble()
        {
            byte[] data = HexToBytes("81-fb-40-58-FF-5C-28-F5-C2-8F");

            MemoryStream ms = new MemoryStream(data);
            CborDataReader reader = new CborDataReader(ms);

            Assert.IsTrue(reader.Read());
            Assert.AreEqual(JsonToken.StartArray, reader.TokenType);

            Assert.IsTrue(reader.Read());
            Assert.AreEqual(JsonToken.Float, reader.TokenType);
            Assert.AreEqual(99.99d, reader.Value);
            Assert.AreEqual(typeof(double), reader.ValueType);

            Assert.IsTrue(reader.Read());
            Assert.AreEqual(JsonToken.EndArray, reader.TokenType);

            Assert.IsFalse(reader.Read());
            Assert.AreEqual(JsonToken.None, reader.TokenType);
        }

        [Test]
        public void ReadDouble_Decimal()
        {
            throw new NotImplementedException();
            //byte[] data = HexToBytes("10-00-00-00-01-30-00-8F-C2-F5-28-5C-FF-58-40-00");

            //MemoryStream ms = new MemoryStream(data);
            //BsonDataReader reader = new BsonDataReader(ms);
            //reader.FloatParseHandling = FloatParseHandling.Decimal;

            //Assert.IsTrue(reader.Read());
            //Assert.AreEqual(JsonToken.StartArray, reader.TokenType);

            //Assert.IsTrue(reader.Read());
            //Assert.AreEqual(JsonToken.Float, reader.TokenType);
            //Assert.AreEqual(99.99m, reader.Value);
            //Assert.AreEqual(typeof(decimal), reader.ValueType);

            //Assert.IsTrue(reader.Read());
            //Assert.AreEqual(JsonToken.EndArray, reader.TokenType);

            //Assert.IsFalse(reader.Read());
            //Assert.AreEqual(JsonToken.None, reader.TokenType);
        }

        [Test]
        public void ReadValues()
        {
            byte[] data = HexToBytes("8B-1B-7F-FF-FF-FF-FF-FF-FF-FF-1A-7F-FF-FF-FF-18-FF-18-7F-61-61-FB-7F-EF-FF-FF-FF-FF-FF-FF-FA-7F-7F-FF-FF-F5-45-00-01-02-03-04-C0-78-19-32-30-30-30-2D-31-32-2D-32-39-54-31-32-3A-33-30-3A-30-30-2B-30-30-3A-30-30-C1-1A-3A-4C-83-C8");
            MemoryStream ms = new MemoryStream(data);
            CborDataReader reader = new CborDataReader(ms);
#pragma warning disable 612,618
            reader.JsonNet35BinaryCompatibility = true;
#pragma warning restore 612,618
            reader.DateTimeKindHandling = DateTimeKind.Utc;

            Assert.IsTrue(reader.Read());
            Assert.AreEqual(JsonToken.StartArray, reader.TokenType);

            Assert.IsTrue(reader.Read());
            Assert.AreEqual(JsonToken.Integer, reader.TokenType);
            Assert.AreEqual(long.MaxValue, reader.Value);
            Assert.AreEqual(typeof(long), reader.ValueType);

            Assert.IsTrue(reader.Read());
            Assert.AreEqual(JsonToken.Integer, reader.TokenType);
            Assert.AreEqual(int.MaxValue, reader.Value);
            Assert.AreEqual(typeof(int), reader.ValueType);

            Assert.IsTrue(reader.Read());
            Assert.AreEqual(JsonToken.Integer, reader.TokenType);
            Assert.AreEqual((int)byte.MaxValue, reader.Value);
            Assert.AreEqual(typeof(int), reader.ValueType);

            Assert.IsTrue(reader.Read());
            Assert.AreEqual(JsonToken.Integer, reader.TokenType);
            Assert.AreEqual((int)sbyte.MaxValue, reader.Value);
            Assert.AreEqual(typeof(int), reader.ValueType);

            Assert.IsTrue(reader.Read());
            Assert.AreEqual(JsonToken.String, reader.TokenType);
            Assert.AreEqual("a", reader.Value);
            Assert.AreEqual(typeof(string), reader.ValueType);

            Assert.IsTrue(reader.Read());
            Assert.AreEqual(JsonToken.Float, reader.TokenType);
            Assert.AreEqual(double.MaxValue, reader.Value);
            Assert.AreEqual(typeof(double), reader.ValueType);

            Assert.IsTrue(reader.Read());
            Assert.AreEqual(JsonToken.Float, reader.TokenType);
            Assert.AreEqual(float.MaxValue, reader.Value);
            Assert.AreEqual(typeof(float), reader.ValueType);

            Assert.IsTrue(reader.Read());
            Assert.AreEqual(JsonToken.Boolean, reader.TokenType);
            Assert.AreEqual(true, reader.Value);
            Assert.AreEqual(typeof(bool), reader.ValueType);

            Assert.IsTrue(reader.Read());
            Assert.AreEqual(JsonToken.Bytes, reader.TokenType);
            CollectionAssert.AreEquivalent(new byte[] { 0, 1, 2, 3, 4 }, (byte[])reader.Value);
            Assert.AreEqual(typeof(byte[]), reader.ValueType);

            Assert.IsTrue(reader.Read());
            Assert.AreEqual(JsonToken.Date, reader.TokenType);
            Assert.AreEqual(new DateTime(2000, 12, 29, 12, 30, 0, DateTimeKind.Utc), reader.Value);
            Assert.AreEqual(typeof(DateTime), reader.ValueType);

            Assert.IsTrue(reader.Read());
            Assert.AreEqual(JsonToken.Date, reader.TokenType);
            Assert.AreEqual(new DateTime(2000, 12, 29, 12, 30, 0, DateTimeKind.Utc), reader.Value);
            Assert.AreEqual(typeof(DateTime), reader.ValueType);

            Assert.IsTrue(reader.Read());
            Assert.AreEqual(JsonToken.EndArray, reader.TokenType);

            Assert.IsFalse(reader.Read());
            Assert.AreEqual(JsonToken.None, reader.TokenType);
        }

        [Test]
        public void ReadObjectBsonFromSite()
        {
            byte[] data = HexToBytes("A3-61-30-61-61-61-31-61-62-61-32-61-63");

            MemoryStream ms = new MemoryStream(data);
            CborDataReader reader = new CborDataReader(ms);

            Assert.IsTrue(reader.Read());
            Assert.AreEqual(JsonToken.StartObject, reader.TokenType);
            Assert.IsTrue(reader.Read());
            Assert.AreEqual(JsonToken.PropertyName, reader.TokenType);
            Assert.AreEqual("0", reader.Value);
            Assert.AreEqual(typeof(string), reader.ValueType);

            Assert.IsTrue(reader.Read());
            Assert.AreEqual(JsonToken.String, reader.TokenType);
            Assert.AreEqual("a", reader.Value);
            Assert.AreEqual(typeof(string), reader.ValueType);

            Assert.IsTrue(reader.Read());
            Assert.AreEqual(JsonToken.PropertyName, reader.TokenType);
            Assert.AreEqual("1", reader.Value);
            Assert.AreEqual(typeof(string), reader.ValueType);

            Assert.IsTrue(reader.Read());
            Assert.AreEqual(JsonToken.String, reader.TokenType);
            Assert.AreEqual("b", reader.Value);
            Assert.AreEqual(typeof(string), reader.ValueType);

            Assert.IsTrue(reader.Read());
            Assert.AreEqual(JsonToken.PropertyName, reader.TokenType);
            Assert.AreEqual("2", reader.Value);
            Assert.AreEqual(typeof(string), reader.ValueType);

            Assert.IsTrue(reader.Read());
            Assert.AreEqual(JsonToken.String, reader.TokenType);
            Assert.AreEqual("c", reader.Value);
            Assert.AreEqual(typeof(string), reader.ValueType);

            Assert.IsTrue(reader.Read());
            Assert.AreEqual(JsonToken.EndObject, reader.TokenType);

            Assert.IsFalse(reader.Read());
            Assert.AreEqual(JsonToken.None, reader.TokenType);
        }

        [Test]
        public void ReadArrayBsonFromSite()
        {
            byte[] data = HexToBytes("83-61-61-61-62-61-63");

            MemoryStream ms = new MemoryStream(data);
            CborDataReader reader = new CborDataReader(ms);

            Assert.AreEqual(DateTimeKind.Local, reader.DateTimeKindHandling);

            reader.DateTimeKindHandling = DateTimeKind.Utc;

            Assert.AreEqual(DateTimeKind.Utc, reader.DateTimeKindHandling);

            Assert.IsTrue(reader.Read());
            Assert.AreEqual(JsonToken.StartArray, reader.TokenType);

            Assert.IsTrue(reader.Read());
            Assert.AreEqual(JsonToken.String, reader.TokenType);
            Assert.AreEqual("a", reader.Value);
            Assert.AreEqual(typeof(string), reader.ValueType);

            Assert.IsTrue(reader.Read());
            Assert.AreEqual(JsonToken.String, reader.TokenType);
            Assert.AreEqual("b", reader.Value);
            Assert.AreEqual(typeof(string), reader.ValueType);

            Assert.IsTrue(reader.Read());
            Assert.AreEqual(JsonToken.String, reader.TokenType);
            Assert.AreEqual("c", reader.Value);
            Assert.AreEqual(typeof(string), reader.ValueType);

            Assert.IsTrue(reader.Read());
            Assert.AreEqual(JsonToken.EndArray, reader.TokenType);

            Assert.IsFalse(reader.Read());
            Assert.AreEqual(JsonToken.None, reader.TokenType);
        }

        [Test]
        public void ReadBytes()
        {
            byte[] data = HexToBytes("83-61-61-61-62-4C-48-65-6C-6C-6F-20-77-6F-72-6C-64-21");

            MemoryStream ms = new MemoryStream(data);
            CborDataReader reader = new CborDataReader(ms, DateTimeKind.Utc);
#pragma warning disable 612,618
            reader.JsonNet35BinaryCompatibility = true;
#pragma warning restore 612,618

            Assert.AreEqual(DateTimeKind.Utc, reader.DateTimeKindHandling);

            Assert.IsTrue(reader.Read());
            Assert.AreEqual(JsonToken.StartArray, reader.TokenType);

            Assert.IsTrue(reader.Read());
            Assert.AreEqual(JsonToken.String, reader.TokenType);
            Assert.AreEqual("a", reader.Value);
            Assert.AreEqual(typeof(string), reader.ValueType);

            Assert.IsTrue(reader.Read());
            Assert.AreEqual(JsonToken.String, reader.TokenType);
            Assert.AreEqual("b", reader.Value);
            Assert.AreEqual(typeof(string), reader.ValueType);

            byte[] encodedStringData = reader.ReadAsBytes();
            Assert.IsNotNull(encodedStringData);
            Assert.AreEqual(JsonToken.Bytes, reader.TokenType);
            Assert.AreEqual(encodedStringData, reader.Value);
            Assert.AreEqual(typeof(byte[]), reader.ValueType);

            Assert.IsTrue(reader.Read());
            Assert.AreEqual(JsonToken.EndArray, reader.TokenType);

            Assert.IsFalse(reader.Read());
            Assert.AreEqual(JsonToken.None, reader.TokenType);

            string decodedString = Encoding.UTF8.GetString(encodedStringData, 0, encodedStringData.Length);
            Assert.AreEqual("Hello world!", decodedString);
        }

        [Test]
        public void ReadNestedArray()
        {
            byte[] data = HexToBytes("A3-63-5F-69-64-4C-4A-78-93-79-17-22-00-00-00-00-61-CF-61-61-88-FB-3F-F0-00-00-00-00-00-00-FB-40-00-00-00-00-00-00-00-FB-40-08-00-00-00-00-00-00-FB-40-10-00-00-00-00-00-00-FB-50-14-00-00-00-00-00-00-FB-40-18-00-00-00-00-00-00-FB-40-1C-00-00-00-00-00-00-FB-40-20-00-00-00-00-00-00-61-62-64-74-65-73-74");

            MemoryStream ms = new MemoryStream(data);
            CborDataReader reader = new CborDataReader(ms);
            
            Assert.IsTrue(reader.Read());
            Assert.AreEqual(JsonToken.StartObject, reader.TokenType);

            Assert.IsTrue(reader.Read());
            Assert.AreEqual(JsonToken.PropertyName, reader.TokenType);
            Assert.AreEqual("_id", reader.Value);

            Assert.IsTrue(reader.Read());
            Assert.AreEqual(JsonToken.Bytes, reader.TokenType);
            CollectionAssert.AreEquivalent(HexToBytes("4A-78-93-79-17-22-00-00-00-00-61-CF"), (byte[])reader.Value);
            Assert.AreEqual(typeof(byte[]), reader.ValueType);

            Assert.IsTrue(reader.Read());
            Assert.AreEqual(JsonToken.PropertyName, reader.TokenType);
            Assert.AreEqual("a", reader.Value);

            Assert.IsTrue(reader.Read());
            Assert.AreEqual(JsonToken.StartArray, reader.TokenType);

            for (int i = 1; i <= 8; i++)
            {
                Assert.IsTrue(reader.Read());
                Assert.AreEqual(JsonToken.Float, reader.TokenType);

                double value = (i != 5)
                    ? Convert.ToDouble(i)
                    : 5.78960446186581E+77d;

                Assert.AreEqual(value, reader.Value);
            }

            Assert.IsTrue(reader.Read());
            Assert.AreEqual(JsonToken.EndArray, reader.TokenType);

            Assert.IsTrue(reader.Read());
            Assert.AreEqual(JsonToken.PropertyName, reader.TokenType);
            Assert.AreEqual("b", reader.Value);

            Assert.IsTrue(reader.Read());
            Assert.AreEqual(JsonToken.String, reader.TokenType);
            Assert.AreEqual("test", reader.Value);

            Assert.IsTrue(reader.Read());
            Assert.AreEqual(JsonToken.EndObject, reader.TokenType);

            Assert.IsFalse(reader.Read());
            Assert.AreEqual(JsonToken.None, reader.TokenType);
        }

        [Test]
        public void ReadNestedArrayIntoLinq()
        {
            string hexdoc = "A3-63-5F-69-64-70-53-6E-69-54-65-52-63-69-41-41-41-41-41-47-48-50-61-61-88-01-02-03-04-FB-50-14-00-00-00-00-00-00-06-07-08-61-62-64-74-65-73-74";

            byte[] data = HexToBytes(hexdoc);

            CborDataReader reader = new CborDataReader(new MemoryStream(data));
#pragma warning disable 612,618
            reader.JsonNet35BinaryCompatibility = true;
#pragma warning restore 612,618

            JObject o = (JObject)JToken.ReadFrom(reader);
            Assert.AreEqual(3, o.Count);

            MemoryStream ms = new MemoryStream();
            CborDataWriter writer = new CborDataWriter(ms);
            o.WriteTo(writer);
            writer.Flush();

            string bson = BytesToHex(ms.ToArray());
            Assert.AreEqual(hexdoc, bson);
        }

        [Test]
        public void ReadRegex()
        {
            string hexdoc = "A1-65-72-65-67-65-78-D8-23-69-2F-74-65-73-74-2F-67-69-6D";

            byte[] data = HexToBytes(hexdoc);

            MemoryStream ms = new MemoryStream(data);
            CborDataReader reader = new CborDataReader(ms);

            Assert.IsTrue(reader.Read());
            Assert.AreEqual(JsonToken.StartObject, reader.TokenType);

            Assert.IsTrue(reader.Read());
            Assert.AreEqual(JsonToken.PropertyName, reader.TokenType);
            Assert.AreEqual("regex", reader.Value);

            Assert.IsTrue(reader.Read());
            Assert.AreEqual(JsonToken.String, reader.TokenType);
            Assert.AreEqual(@"/test/gim", reader.Value);
            Assert.AreEqual(typeof(string), reader.ValueType);

            // { "regex": 35("/test/gim") } 
            Assert.IsTrue(reader.Read());
            Assert.AreEqual(JsonToken.EndObject, reader.TokenType);

            Assert.IsFalse(reader.Read());
            Assert.AreEqual(JsonToken.None, reader.TokenType);
        }

        [Test]
        public void ReadUndefined()
        {
            string hexdoc = "A1-69-75-6E-64-65-66-69-6E-65-64-F7";

            byte[] data = HexToBytes(hexdoc);

            MemoryStream ms = new MemoryStream(data);
            CborDataReader reader = new CborDataReader(ms);

            Assert.IsTrue(reader.Read());
            Assert.AreEqual(JsonToken.StartObject, reader.TokenType);

            Assert.IsTrue(reader.Read());
            Assert.AreEqual(JsonToken.PropertyName, reader.TokenType);
            Assert.AreEqual("undefined", reader.Value);

            Assert.IsTrue(reader.Read());
            Assert.AreEqual(JsonToken.Undefined, reader.TokenType);
            Assert.AreEqual(null, reader.Value);
            Assert.AreEqual(null, reader.ValueType);

            Assert.IsTrue(reader.Read());
            Assert.AreEqual(JsonToken.EndObject, reader.TokenType);

            Assert.IsFalse(reader.Read());
            Assert.AreEqual(JsonToken.None, reader.TokenType);
        }

        [Test]
        public void ReadLong()
        {
            string hexdoc = "A1-64-6C-6F-6E-67-1B-7F-FF-FF-FF-FF-FF-FF-FF";

            byte[] data = HexToBytes(hexdoc);

            MemoryStream ms = new MemoryStream(data);
            CborDataReader reader = new CborDataReader(ms);

            Assert.IsTrue(reader.Read());
            Assert.AreEqual(JsonToken.StartObject, reader.TokenType);

            Assert.IsTrue(reader.Read());
            Assert.AreEqual(JsonToken.PropertyName, reader.TokenType);
            Assert.AreEqual("long", reader.Value);

            Assert.IsTrue(reader.Read());
            Assert.AreEqual(JsonToken.Integer, reader.TokenType);
            Assert.AreEqual(long.MaxValue, reader.Value);
            Assert.AreEqual(typeof(long), reader.ValueType);

            Assert.IsTrue(reader.Read());
            Assert.AreEqual(JsonToken.EndObject, reader.TokenType);

            Assert.IsFalse(reader.Read());
            Assert.AreEqual(JsonToken.None, reader.TokenType);
        }

        [Test]
        public void ReadEndOfStream()
        {
            CborDataReader reader = new CborDataReader(new MemoryStream());
            Assert.IsFalse(reader.Read());
        }

        [Test]
        public void ReadLargeStrings()
        {
            string bson = "A1-79-01-21-30-2D-31-2D-32-2D-33-2D-34-2D-35-2D-36-2D-37-2D-38-2D-39-2D-31-30-2D-31-31-2D-31-32-2D-31-33-2D-31-34-2D-31-35-2D-31-36-2D-31-37-2D-31-38-2D-31-39-2D-32-30-2D-32-31-2D-32-32-2D-32-33-2D-32-34-2D-32-35-2D-32-36-2D-32-37-2D-32-38-2D-32-39-2D-33-30-2D-33-31-2D-33-32-2D-33-33-2D-33-34-2D-33-35-2D-33-36-2D-33-37-2D-33-38-2D-33-39-2D-34-30-2D-34-31-2D-34-32-2D-34-33-2D-34-34-2D-34-35-2D-34-36-2D-34-37-2D-34-38-2D-34-39-2D-35-30-2D-35-31-2D-35-32-2D-35-33-2D-35-34-2D-35-35-2D-35-36-2D-35-37-2D-35-38-2D-35-39-2D-36-30-2D-36-31-2D-36-32-2D-36-33-2D-36-34-2D-36-35-2D-36-36-2D-36-37-2D-36-38-2D-36-39-2D-37-30-2D-37-31-2D-37-32-2D-37-33-2D-37-34-2D-37-35-2D-37-36-2D-37-37-2D-37-38-2D-37-39-2D-38-30-2D-38-31-2D-38-32-2D-38-33-2D-38-34-2D-38-35-2D-38-36-2D-38-37-2D-38-38-2D-38-39-2D-39-30-2D-39-31-2D-39-32-2D-39-33-2D-39-34-2D-39-35-2D-39-36-2D-39-37-2D-39-38-2D-39-39-79-01-21-30-2D-31-2D-32-2D-33-2D-34-2D-35-2D-36-2D-37-2D-38-2D-39-2D-31-30-2D-31-31-2D-31-32-2D-31-33-2D-31-34-2D-31-35-2D-31-36-2D-31-37-2D-31-38-2D-31-39-2D-32-30-2D-32-31-2D-32-32-2D-32-33-2D-32-34-2D-32-35-2D-32-36-2D-32-37-2D-32-38-2D-32-39-2D-33-30-2D-33-31-2D-33-32-2D-33-33-2D-33-34-2D-33-35-2D-33-36-2D-33-37-2D-33-38-2D-33-39-2D-34-30-2D-34-31-2D-34-32-2D-34-33-2D-34-34-2D-34-35-2D-34-36-2D-34-37-2D-34-38-2D-34-39-2D-35-30-2D-35-31-2D-35-32-2D-35-33-2D-35-34-2D-35-35-2D-35-36-2D-35-37-2D-35-38-2D-35-39-2D-36-30-2D-36-31-2D-36-32-2D-36-33-2D-36-34-2D-36-35-2D-36-36-2D-36-37-2D-36-38-2D-36-39-2D-37-30-2D-37-31-2D-37-32-2D-37-33-2D-37-34-2D-37-35-2D-37-36-2D-37-37-2D-37-38-2D-37-39-2D-38-30-2D-38-31-2D-38-32-2D-38-33-2D-38-34-2D-38-35-2D-38-36-2D-38-37-2D-38-38-2D-38-39-2D-39-30-2D-39-31-2D-39-32-2D-39-33-2D-39-34-2D-39-35-2D-39-36-2D-39-37-2D-39-38-2D-39-39";

            CborDataReader reader = new CborDataReader(new MemoryStream(HexToBytes(bson)));

            StringBuilder largeStringBuilder = new StringBuilder();
            for (int i = 0; i < 100; i++)
            {
                if (i > 0)
                {
                    largeStringBuilder.Append("-");
                }

                largeStringBuilder.Append(i.ToString(CultureInfo.InvariantCulture));
            }
            string largeString = largeStringBuilder.ToString();

            Assert.IsTrue(reader.Read());
            Assert.AreEqual(JsonToken.StartObject, reader.TokenType);

            Assert.IsTrue(reader.Read());
            Assert.AreEqual(JsonToken.PropertyName, reader.TokenType);
            Assert.AreEqual(largeString, reader.Value);

            Assert.IsTrue(reader.Read());
            Assert.AreEqual(JsonToken.String, reader.TokenType);
            Assert.AreEqual(largeString, reader.Value);

            Assert.IsTrue(reader.Read());
            Assert.AreEqual(JsonToken.EndObject, reader.TokenType);

            Assert.IsFalse(reader.Read());
            Assert.AreEqual(JsonToken.None, reader.TokenType);
        }

        [Test]
        public void ReadEmptyStrings()
        {
            string bson = "A1-60-60";

            CborDataReader reader = new CborDataReader(new MemoryStream(HexToBytes(bson)));

            Assert.IsTrue(reader.Read());
            Assert.AreEqual(JsonToken.StartObject, reader.TokenType);

            Assert.IsTrue(reader.Read());
            Assert.AreEqual(JsonToken.PropertyName, reader.TokenType);
            Assert.AreEqual("", reader.Value);

            Assert.IsTrue(reader.Read());
            Assert.AreEqual(JsonToken.String, reader.TokenType);
            Assert.AreEqual("", reader.Value);

            Assert.IsTrue(reader.Read());
            Assert.AreEqual(JsonToken.EndObject, reader.TokenType);

            Assert.IsFalse(reader.Read());
            Assert.AreEqual(JsonToken.None, reader.TokenType);
        }

        [Test]
        public void WriteAndReadEmptyListsAndDictionaries()
        {
            MemoryStream ms = new MemoryStream();
            CborDataWriter writer = new CborDataWriter(ms);

            writer.WriteStartObject();
            writer.WritePropertyName("Arguments");
            writer.WriteStartObject();
            writer.WriteEndObject();
            writer.WritePropertyName("List");
            writer.WriteStartArray();
            writer.WriteEndArray();
            writer.WriteEndObject();

            string bson = BitConverter.ToString(ms.ToArray());

            Assert.AreEqual("A2-69-41-72-67-75-6D-65-6E-74-73-A0-64-4C-69-73-74-80", bson);

            CborDataReader reader = new CborDataReader(new MemoryStream(HexToBytes(bson)));

            Assert.IsTrue(reader.Read());
            Assert.AreEqual(JsonToken.StartObject, reader.TokenType);

            Assert.IsTrue(reader.Read());
            Assert.AreEqual(JsonToken.PropertyName, reader.TokenType);
            Assert.AreEqual("Arguments", reader.Value.ToString());

            Assert.IsTrue(reader.Read());
            Assert.AreEqual(JsonToken.StartObject, reader.TokenType);

            Assert.IsTrue(reader.Read());
            Assert.AreEqual(JsonToken.EndObject, reader.TokenType);

            Assert.IsTrue(reader.Read());
            Assert.AreEqual(JsonToken.PropertyName, reader.TokenType);
            Assert.AreEqual("List", reader.Value.ToString());

            Assert.IsTrue(reader.Read());
            Assert.AreEqual(JsonToken.StartArray, reader.TokenType);

            Assert.IsTrue(reader.Read());
            Assert.AreEqual(JsonToken.EndArray, reader.TokenType);

            Assert.IsTrue(reader.Read());
            Assert.AreEqual(JsonToken.EndObject, reader.TokenType);

            Assert.IsFalse(reader.Read());
            Assert.AreEqual(JsonToken.None, reader.TokenType);
        }

        [Test]
        public void DateTimeKindHandling()
        {
            DateTime value = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            MemoryStream ms = new MemoryStream();
            CborDataWriter writer = new CborDataWriter(ms);

            writer.WriteStartObject();
            writer.WritePropertyName("DateTime");
            writer.WriteValue(value);
            writer.WriteEndObject();

            byte[] bson = ms.ToArray();

            JObject o;
            CborDataReader reader;

            reader = new CborDataReader(new MemoryStream(bson), DateTimeKind.Utc);
            o = (JObject)JToken.ReadFrom(reader);
            Assert.AreEqual(value, (DateTime)o["DateTime"]);

            reader = new CborDataReader(new MemoryStream(bson), DateTimeKind.Local);
            o = (JObject)JToken.ReadFrom(reader);
            Assert.AreEqual(value.ToLocalTime(), (DateTime)o["DateTime"]);

            reader = new CborDataReader(new MemoryStream(bson),  DateTimeKind.Unspecified);
            o = (JObject)JToken.ReadFrom(reader);
            Assert.AreEqual(DateTime.SpecifyKind(value, DateTimeKind.Unspecified), (DateTime)o["DateTime"]);
        }

        [Test]
        public void UnspecifiedDateTimeKindHandling()
        {
            throw new NotImplementedException();
            //DateTime value = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);

            //MemoryStream ms = new MemoryStream();
            //CborDataWriter writer = new CborDataWriter(ms);
            //writer.DateTimeKindHandling = DateTimeKind.Unspecified;

            //writer.WriteStartObject();
            //writer.WritePropertyName("DateTime");
            //writer.WriteValue(value);
            //writer.WriteEndObject();

            //byte[] bson = ms.ToArray();

            //JObject o;
            //CborDataReader reader;

            //reader = new CborDataReader(new MemoryStream(bson), false, DateTimeKind.Unspecified);
            //o = (JObject)JToken.ReadFrom(reader);
            //Assert.AreEqual(value, (DateTime)o["DateTime"]);
        }

        [Test]
        public void LocalDateTimeKindHandling()
        {
            DateTime value = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Local);

            MemoryStream ms = new MemoryStream();
            CborDataWriter writer = new CborDataWriter(ms);

            writer.WriteStartObject();
            writer.WritePropertyName("DateTime");
            writer.WriteValue(value);
            writer.WriteEndObject();

            byte[] bson = ms.ToArray();

            JObject o;
            CborDataReader reader;

            reader = new CborDataReader(new MemoryStream(bson), DateTimeKind.Local);
            o = (JObject)JToken.ReadFrom(reader);
            Assert.AreEqual(value, (DateTime)o["DateTime"]);
        }

        private string WriteAndReadStringValue(string val)
        {
            MemoryStream ms = new MemoryStream();
            CborDataWriter bs = new CborDataWriter(ms);
            bs.WriteStartObject();
            bs.WritePropertyName("StringValue");
            bs.WriteValue(val);
            bs.WriteEnd();

            ms.Seek(0, SeekOrigin.Begin);

            CborDataReader reader = new CborDataReader(ms);
            // object
            reader.Read();
            // property name
            reader.Read();
            // string
            reader.Read();
            return (string)reader.Value;
        }

        private string WriteAndReadStringPropertyName(string val)
        {
            MemoryStream ms = new MemoryStream();
            CborDataWriter bs = new CborDataWriter(ms);
            bs.WriteStartObject();
            bs.WritePropertyName(val);
            bs.WriteValue("Dummy");
            bs.WriteEnd();

            ms.Seek(0, SeekOrigin.Begin);

            CborDataReader reader = new CborDataReader(ms);
            // object
            reader.Read();
            // property name
            reader.Read();
            return (string)reader.Value;
        }

        [Test]
        public void TestReadLenStringValueShortTripleByte()
        {
            StringBuilder sb = new StringBuilder();
            //sb.Append('1',127); //first char of euro at the end of the boundry.
            //sb.Append(euro, 5);
            //sb.Append('1',128);
            sb.Append(Euro);

            string expected = sb.ToString();
            Assert.AreEqual(expected, WriteAndReadStringValue(expected));
        }

        [Test]
        public void TestReadLenStringValueTripleByteCharBufferBoundry0()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append('1', 127); //first char of euro at the end of the boundry.
            sb.Append(Euro, 5);
            sb.Append('1', 128);
            sb.Append(Euro);

            string expected = sb.ToString();
            Assert.AreEqual(expected, WriteAndReadStringValue(expected));
        }

        [Test]
        public void TestReadLenStringValueTripleByteCharBufferBoundry1()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append('1', 126);
            sb.Append(Euro, 5); //middle char of euro at the end of the boundry.
            sb.Append('1', 128);
            sb.Append(Euro);

            string expected = sb.ToString();
            string result = WriteAndReadStringValue(expected);
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void TestReadLenStringValueTripleByteCharOne()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(Euro, 1); //Just one triple byte char in the string.

            string expected = sb.ToString();
            Assert.AreEqual(expected, WriteAndReadStringValue(expected));
        }

        [Test]
        public void TestReadLenStringValueTripleByteCharBufferBoundry2()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append('1', 125);
            sb.Append(Euro, 5); //last char of the eruo at the end of the boundry.
            sb.Append('1', 128);
            sb.Append(Euro);

            string expected = sb.ToString();
            Assert.AreEqual(expected, WriteAndReadStringValue(expected));
        }

        [Test]
        public void TestReadStringValue()
        {
            string expected = "test";
            Assert.AreEqual(expected, WriteAndReadStringValue(expected));
        }

        [Test]
        public void TestReadStringValueLong()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append('t', 150);
            string expected = sb.ToString();
            Assert.AreEqual(expected, WriteAndReadStringValue(expected));
        }

        [Test]
        public void TestReadStringPropertyNameShortTripleByte()
        {
            StringBuilder sb = new StringBuilder();
            //sb.Append('1',127); //first char of euro at the end of the boundry.
            //sb.Append(euro, 5);
            //sb.Append('1',128);
            sb.Append(Euro);

            string expected = sb.ToString();
            Assert.AreEqual(expected, WriteAndReadStringPropertyName(expected));
        }

        [Test]
        public void TestReadStringPropertyNameTripleByteCharBufferBoundry0()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append('1', 127); //first char of euro at the end of the boundry.
            sb.Append(Euro, 5);
            sb.Append('1', 128);
            sb.Append(Euro);

            string expected = sb.ToString();
            string result = WriteAndReadStringPropertyName(expected);
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void TestReadStringPropertyNameTripleByteCharBufferBoundry1()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append('1', 126);
            sb.Append(Euro, 5); //middle char of euro at the end of the boundry.
            sb.Append('1', 128);
            sb.Append(Euro);

            string expected = sb.ToString();
            Assert.AreEqual(expected, WriteAndReadStringPropertyName(expected));
        }

        [Test]
        public void TestReadStringPropertyNameTripleByteCharOne()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(Euro, 1); //Just one triple byte char in the string.

            string expected = sb.ToString();
            Assert.AreEqual(expected, WriteAndReadStringPropertyName(expected));
        }

        [Test]
        public void TestReadStringPropertyNameTripleByteCharBufferBoundry2()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append('1', 125);
            sb.Append(Euro, 5); //last char of the eruo at the end of the boundry.
            sb.Append('1', 128);
            sb.Append(Euro);

            string expected = sb.ToString();
            Assert.AreEqual(expected, WriteAndReadStringPropertyName(expected));
        }

        [Test]
        public void TestReadStringPropertyName()
        {
            string expected = "test";
            Assert.AreEqual(expected, WriteAndReadStringPropertyName(expected));
        }

        [Test]
        public void TestReadStringPropertyNameLong()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append('t', 150);
            string expected = sb.ToString();
            Assert.AreEqual(expected, WriteAndReadStringPropertyName(expected));
        }

        [Test]
        public void ReadRegexWithOptions()
        {
            string hexdoc = "A2-61-61-D8-23-66-2F-61-62-63-2F-69-61-62-D8-23-62-2F-2F";

            byte[] data = HexToBytes(hexdoc);

            MemoryStream ms = new MemoryStream(data);
            CborDataReader reader = new CborDataReader(ms);

            Assert.IsTrue(reader.Read());
            Assert.AreEqual(JsonToken.StartObject, reader.TokenType);

            Assert.IsTrue(reader.Read());
            Assert.AreEqual(JsonToken.PropertyName, reader.TokenType);

            Assert.IsTrue(reader.Read());
            Assert.AreEqual(JsonToken.String, reader.TokenType);
            Assert.AreEqual("/abc/i", reader.Value);
            Assert.AreEqual(typeof(string), reader.ValueType);

            Assert.IsTrue(reader.Read());
            Assert.AreEqual(JsonToken.PropertyName, reader.TokenType);

            Assert.IsTrue(reader.Read());
            Assert.AreEqual(JsonToken.String, reader.TokenType);
            Assert.AreEqual("//", reader.Value);
            Assert.AreEqual(typeof(string), reader.ValueType);

            Assert.IsTrue(reader.Read());
            Assert.AreEqual(JsonToken.EndObject, reader.TokenType);

            Assert.IsFalse(reader.Read());
            Assert.AreEqual(JsonToken.None, reader.TokenType);
        }

        [Test]
        public void CanRoundTripStackOverflowData()
        {
            var hexDoc = "A2-67-41-62-6F-75-74-4D-65-79-03-F6-3C-70-3E-49-27-6D-20-74-68-65-20-44-69-72-65-63-74-6F-72-20-66-6F-72-20-52-65-73-65-61-72-63-68-20-61-6E-64-20-44-65-76-65-6C-6F-70-6D-65-6E-74-20-66-6F-72-20-3C-61-20-68-72-65-66-3D-22-68-74-74-70-3A-2F-2F-77-77-77-2E-70-72-6F-70-68-6F-65-6E-69-78-2E-63-6F-6D-22-20-72-65-6C-3D-22-6E-6F-66-6F-6C-6C-6F-77-22-3E-50-72-6F-50-68-6F-65-6E-69-78-3C-2F-61-3E-2C-20-61-20-70-75-62-6C-69-63-20-73-61-66-65-74-79-20-73-6F-66-74-77-61-72-65-20-63-6F-6D-70-61-6E-79-2E-20-20-54-68-69-73-20-70-6F-73-69-74-69-6F-6E-20-61-6C-6C-6F-77-73-20-6D-65-20-74-6F-20-69-6E-76-65-73-74-69-67-61-74-65-20-6E-65-77-20-61-6E-64-20-65-78-69-73-74-69-6E-67-20-74-65-63-68-6E-6F-6C-6F-67-69-65-73-20-61-6E-64-20-69-6E-63-6F-72-70-6F-72-61-74-65-20-74-68-65-6D-20-69-6E-74-6F-20-6F-75-72-20-70-72-6F-64-75-63-74-20-6C-69-6E-65-2C-20-77-69-74-68-20-74-68-65-20-65-6E-64-20-67-6F-61-6C-20-62-65-69-6E-67-20-74-6F-20-68-65-6C-70-20-70-75-62-6C-69-63-20-73-61-66-65-74-79-20-61-67-65-6E-63-69-65-73-20-74-6F-20-64-6F-20-74-68-65-69-72-20-6A-6F-62-73-20-6D-6F-72-65-20-65-66-66-65-63-69-65-6E-74-6C-79-20-61-6E-64-20-73-61-66-65-6C-79-2E-3C-2F-70-3E-0D-0A-0D-0A-3C-70-3E-49-27-6D-20-61-6E-20-61-64-76-6F-63-61-74-65-20-66-6F-72-20-50-6F-77-65-72-53-68-65-6C-6C-2C-20-61-73-20-49-20-62-65-6C-69-65-76-65-20-69-74-20-65-6E-63-6F-75-72-61-67-65-73-20-61-64-6D-69-6E-69-73-74-72-61-74-69-76-65-20-62-65-73-74-20-70-72-61-63-74-69-63-65-73-20-61-6E-64-20-61-6C-6C-6F-77-73-20-64-65-76-65-6C-6F-70-65-72-73-20-74-6F-20-70-72-6F-76-69-64-65-20-61-64-64-69-74-69-6F-6E-61-6C-20-61-63-63-65-73-73-20-74-6F-20-74-68-65-69-72-20-61-70-70-6C-69-63-61-74-69-6F-6E-73-2C-20-77-69-74-68-6F-75-74-20-6E-65-65-64-69-6E-67-20-74-6F-20-65-78-70-6C-69-63-69-74-79-20-77-72-69-74-65-20-63-6F-64-65-20-66-6F-72-20-65-61-63-68-20-61-64-6D-69-6E-69-73-74-72-61-74-69-76-65-20-66-65-61-74-75-72-65-2E-20-20-50-61-72-74-20-6F-66-20-6D-79-20-61-64-76-6F-63-61-63-79-20-66-6F-72-20-50-6F-77-65-72-53-68-65-6C-6C-20-69-6E-63-6C-75-64-65-73-20-3C-61-20-68-72-65-66-3D-22-68-74-74-70-3A-2F-2F-62-6C-6F-67-2E-75-73-65-70-6F-77-65-72-73-68-65-6C-6C-2E-63-6F-6D-22-20-72-65-6C-3D-22-6E-6F-66-6F-6C-6C-6F-77-22-3E-6D-79-20-62-6C-6F-67-3C-2F-61-3E-2C-20-61-70-70-65-61-72-61-6E-63-65-73-20-6F-6E-20-76-61-72-69-6F-75-73-20-70-6F-64-63-61-73-74-73-2C-20-61-6E-64-20-61-63-74-69-6E-67-20-61-73-20-61-20-43-6F-6D-6D-75-6E-69-74-79-20-44-69-72-65-63-74-6F-72-20-66-6F-72-20-3C-61-20-68-72-65-66-3D-22-68-74-74-70-3A-2F-2F-70-6F-77-65-72-73-68-65-6C-6C-63-6F-6D-6D-75-6E-69-74-79-2E-6F-72-67-22-20-72-65-6C-3D-22-6E-6F-66-6F-6C-6C-6F-77-22-3E-50-6F-77-65-72-53-68-65-6C-6C-43-6F-6D-6D-75-6E-69-74-79-2E-4F-72-67-3C-2F-61-3E-3C-2F-70-3E-0D-0A-0D-0A-3C-70-3E-49-E2-80-99-6D-20-61-6C-73-6F-20-61-20-63-6F-2D-68-6F-73-74-20-6F-66-20-4D-69-6E-64-20-6F-66-20-52-6F-6F-74-20-28-61-20-77-65-65-6B-6C-79-20-61-75-64-69-6F-20-70-6F-64-63-61-73-74-20-61-62-6F-75-74-20-73-79-73-74-65-6D-73-20-61-64-6D-69-6E-69-73-74-72-61-74-69-6F-6E-2C-20-74-65-63-68-20-6E-65-77-73-2C-20-61-6E-64-20-74-6F-70-69-63-73-29-2E-3C-2F-70-3E-0D-0A-6A-57-65-62-73-69-74-65-55-72-6C-78-1D-68-74-74-70-3A-2F-2F-62-6C-6F-67-2E-75-73-65-70-6F-77-65-72-73-68-65-6C-6C-2E-63-6F-6D";
            var memoryStream = new MemoryStream(HexToBytes(hexDoc));

            CborDataReader reader = new CborDataReader(memoryStream);

            Assert.IsTrue(reader.Read());
            Assert.AreEqual(JsonToken.StartObject, reader.TokenType);

            Assert.IsTrue(reader.Read());
            Assert.AreEqual(JsonToken.PropertyName, reader.TokenType);
            Assert.AreEqual("AboutMe", reader.Value);
            Assert.AreEqual(typeof(string), reader.ValueType);

            Assert.IsTrue(reader.Read());
            Assert.AreEqual(JsonToken.String, reader.TokenType);
            Assert.AreEqual("<p>I'm the Director for Research and Development for <a href=\"http://www.prophoenix.com\" rel=\"nofollow\">ProPhoenix</a>, a public safety software company.  This position allows me to investigate new and existing technologies and incorporate them into our product line, with the end goal being to help public safety agencies to do their jobs more effeciently and safely.</p>\r\n\r\n<p>I'm an advocate for PowerShell, as I believe it encourages administrative best practices and allows developers to provide additional access to their applications, without needing to explicity write code for each administrative feature.  Part of my advocacy for PowerShell includes <a href=\"http://blog.usepowershell.com\" rel=\"nofollow\">my blog</a>, appearances on various podcasts, and acting as a Community Director for <a href=\"http://powershellcommunity.org\" rel=\"nofollow\">PowerShellCommunity.Org</a></p>\r\n\r\n<p>I’m also a co-host of Mind of Root (a weekly audio podcast about systems administration, tech news, and topics).</p>\r\n", reader.Value);
            Assert.AreEqual(typeof(string), reader.ValueType);

            Assert.IsTrue(reader.Read());
            Assert.AreEqual(JsonToken.PropertyName, reader.TokenType);
            Assert.AreEqual("WebsiteUrl", reader.Value);
            Assert.AreEqual(typeof(string), reader.ValueType);

            Assert.IsTrue(reader.Read());
            Assert.AreEqual(JsonToken.String, reader.TokenType);
            Assert.AreEqual("http://blog.usepowershell.com", reader.Value);
            Assert.AreEqual(typeof(string), reader.ValueType);

            Assert.IsTrue(reader.Read());
            Assert.AreEqual(JsonToken.EndObject, reader.TokenType);

            Assert.IsFalse(reader.Read());
            Assert.AreEqual(JsonToken.None, reader.TokenType);
        }

        [Test]
        public void MultibyteCharacterPropertyNamesAndStrings()
        {
            string json = @"{
  ""ΕΝΤΟΛΗ ΧΧΧ ΧΧΧΧΧΧΧΧΧ ΤΑ ΠΡΩΤΑΣΦΑΛΙΣΤΗΡΙΑ ΠΟΥ ΔΕΝ ΕΧΟΥΝ ΥΠΟΛΟΙΠΟ ΝΑ ΤΑ ΣΤΕΛΝΟΥΜΕ ΑΠΕΥΘΕΙΑΣ ΣΤΟΥΣ ΠΕΛΑΤΕΣ"": ""ΕΝΤΟΛΗ ΧΧΧ ΧΧΧΧΧΧΧΧΧ ΤΑ ΠΡΩΤΑΣΦΑΛΙΣΤΗΡΙΑ ΠΟΥ ΔΕΝ ΕΧΟΥΝ ΥΠΟΛΟΙΠΟ ΝΑ ΤΑ ΣΤΕΛΝΟΥΜΕ ΑΠΕΥΘΕΙΑΣ ΣΤΟΥΣ ΠΕΛΑΤΕΣ""
}";
            JObject parsed = JObject.Parse(json);
            var memoryStream = new MemoryStream();
            var bsonWriter = new CborDataWriter(memoryStream);
            parsed.WriteTo(bsonWriter);
            bsonWriter.Flush();
            memoryStream.Position = 0;

            CborDataReader reader = new CborDataReader(memoryStream);

            Assert.IsTrue(reader.Read());
            Assert.AreEqual(JsonToken.StartObject, reader.TokenType);

            Assert.IsTrue(reader.Read());
            Assert.AreEqual(JsonToken.PropertyName, reader.TokenType);
            Assert.AreEqual("ΕΝΤΟΛΗ ΧΧΧ ΧΧΧΧΧΧΧΧΧ ΤΑ ΠΡΩΤΑΣΦΑΛΙΣΤΗΡΙΑ ΠΟΥ ΔΕΝ ΕΧΟΥΝ ΥΠΟΛΟΙΠΟ ΝΑ ΤΑ ΣΤΕΛΝΟΥΜΕ ΑΠΕΥΘΕΙΑΣ ΣΤΟΥΣ ΠΕΛΑΤΕΣ", reader.Value);

            Assert.IsTrue(reader.Read());
            Assert.AreEqual(JsonToken.String, reader.TokenType);
            Assert.AreEqual("ΕΝΤΟΛΗ ΧΧΧ ΧΧΧΧΧΧΧΧΧ ΤΑ ΠΡΩΤΑΣΦΑΛΙΣΤΗΡΙΑ ΠΟΥ ΔΕΝ ΕΧΟΥΝ ΥΠΟΛΟΙΠΟ ΝΑ ΤΑ ΣΤΕΛΝΟΥΜΕ ΑΠΕΥΘΕΙΑΣ ΣΤΟΥΣ ΠΕΛΑΤΕΣ", reader.Value);

            Assert.IsTrue(reader.Read());
            Assert.AreEqual(JsonToken.EndObject, reader.TokenType);
        }

        public void UriGuidTimeSpanTestClassEmptyTest()
        {
            UriGuidTimeSpanTestClass c1 = new UriGuidTimeSpanTestClass();

            var memoryStream = new MemoryStream();
            var bsonWriter = new CborDataWriter(memoryStream);
            JsonSerializer serializer = new JsonSerializer();
            serializer.Serialize(bsonWriter, c1);
            bsonWriter.Flush();
            memoryStream.Position = 0;

            var bsonReader = new CborDataReader(memoryStream);

            UriGuidTimeSpanTestClass c2 = serializer.Deserialize<UriGuidTimeSpanTestClass>(bsonReader);
            Assert.AreEqual(c1.Guid, c2.Guid);
            Assert.AreEqual(c1.NullableGuid, c2.NullableGuid);
            Assert.AreEqual(c1.TimeSpan, c2.TimeSpan);
            Assert.AreEqual(c1.NullableTimeSpan, c2.NullableTimeSpan);
            Assert.AreEqual(c1.Uri, c2.Uri);
        }

        public void UriGuidTimeSpanTestClassValuesTest()
        {
            UriGuidTimeSpanTestClass c1 = new UriGuidTimeSpanTestClass
            {
                Guid = new Guid("1924129C-F7E0-40F3-9607-9939C531395A"),
                NullableGuid = new Guid("9E9F3ADF-E017-4F72-91E0-617EBE85967D"),
                TimeSpan = TimeSpan.FromDays(1),
                NullableTimeSpan = TimeSpan.FromHours(1),
                Uri = new Uri("http://testuri.com")
            };

            var memoryStream = new MemoryStream();
            var bsonWriter = new CborDataWriter(memoryStream);
            JsonSerializer serializer = new JsonSerializer();
            serializer.Serialize(bsonWriter, c1);
            bsonWriter.Flush();
            memoryStream.Position = 0;

            var bsonReader = new CborDataReader(memoryStream);

            UriGuidTimeSpanTestClass c2 = serializer.Deserialize<UriGuidTimeSpanTestClass>(bsonReader);
            Assert.AreEqual(c1.Guid, c2.Guid);
            Assert.AreEqual(c1.NullableGuid, c2.NullableGuid);
            Assert.AreEqual(c1.TimeSpan, c2.TimeSpan);
            Assert.AreEqual(c1.NullableTimeSpan, c2.NullableTimeSpan);
            Assert.AreEqual(c1.Uri, c2.Uri);
        }

        [Test]
        public void DeserializeByteArrayWithTypeNameHandling()
        {
            TestObject test = new TestObject("Test", new byte[] { 72, 63, 62, 71, 92, 55 });

            JsonSerializer serializer = new JsonSerializer();
            serializer.TypeNameHandling = TypeNameHandling.All;

            byte[] objectBytes;
            using (MemoryStream bsonStream = new MemoryStream())
            using (JsonWriter bsonWriter = new CborDataWriter(bsonStream))
            {
                serializer.Serialize(bsonWriter, test);
                bsonWriter.Flush();

                objectBytes = bsonStream.ToArray();
            }

            using (MemoryStream bsonStream = new MemoryStream(objectBytes))
            using (JsonReader bsonReader = new CborDataReader(bsonStream))
            {
                // Get exception here
                TestObject newObject = (TestObject)serializer.Deserialize(bsonReader);

                Assert.AreEqual("Test", newObject.Name);
                CollectionAssert.AreEquivalent(new byte[] { 72, 63, 62, 71, 92, 55 }, newObject.Data);
            }
        }
        
        public void Utf8Text()
        {
            string badText = System.IO.File.ReadAllText(@"PoisonText.txt");
            var j = new JObject();
            j["test"] = badText;

            var memoryStream = new MemoryStream();
            var bsonWriter = new CborDataWriter(memoryStream);
            j.WriteTo(bsonWriter);
            bsonWriter.Flush();

            memoryStream.Position = 0;
            JObject o = JObject.Load(new CborDataReader(memoryStream));

            Assert.AreEqual(badText, (string)o["test"]);
        }

#if !(NET20 || NET35 || PORTABLE || PORTABLE40) || NETSTANDARD1_3
        public class BigIntegerTestClass
        {
            public BigInteger Blah { get; set; }
        }

        [Test]
        public void ReadBigInteger()
        {
            BigInteger i = BigInteger.Parse("1999999999999999999999999999999999999999999999999999999999990");

            byte[] data = HexToBytes("A1-64-42-6C-61-68-C2-58-1A-01-3E-9E-4E-4C-2F-34-44-8A-03-AE-C4-84-59-28-CB-21-B2-1F-FF-FF-FF-FF-FF-FF-F6");
            MemoryStream ms = new MemoryStream(data);

            CborDataReader reader = new CborDataReader(ms);

            JsonSerializer serializer = new JsonSerializer();

            BigIntegerTestClass c = serializer.Deserialize<BigIntegerTestClass>(reader);

            Assert.AreEqual(i, c.Blah);
        }
#endif

        public class RegexTestClass
        {
            public Regex Regex { get; set; }
        }

        [Test]
        public void DeserializeRegexNonConverterBson()
        {
            string hex = "";
            byte[] data = HexToBytes(hex);
            MemoryStream ms = new MemoryStream(data);

            CborDataReader reader = new CborDataReader(ms);

            JsonSerializer serializer = new JsonSerializer();

            RegexTestClass c = serializer.Deserialize<RegexTestClass>(reader);

            Assert.AreEqual("(hi)", c.Regex.ToString());
            Assert.AreEqual(RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase, c.Regex.Options);
        }

        [Test]
        public void DeserializeRegexBson()
        {
            string hex = "";
            byte[] data = HexToBytes(hex);
            MemoryStream ms = new MemoryStream(data);

            CborDataReader reader = new CborDataReader(ms);

            JsonSerializer serializer = new JsonSerializer();

            RegexTestClass c = serializer.Deserialize<RegexTestClass>(reader);

            Assert.AreEqual("(hi)", c.Regex.ToString());
            Assert.AreEqual(RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase, c.Regex.Options);
        }

        class Zoo
        {
            public List<Animal> Animals { get; set; }
        }

        class Animal
        {
            public Animal(string name)
            {
                Name = name;
            }

            public string Name { get; private set; }
        }

        class Dog : Animal
        {
            public Dog(string name)
                : base(name)
            {
            }
        }

        class Cat : Animal
        {
            public Cat(string name)
                : base(name)
            {
            }
        }

        public class MyBinder : DefaultSerializationBinder
        {
            public bool BindToTypeCalled { get; set; }

#if !(NET20 || NET35)
            public bool BindToNameCalled { get; set; }

            public override void BindToName(Type serializedType, out string assemblyName, out string typeName)
            {
                BindToNameCalled = true;
                base.BindToName(serializedType, out assemblyName, out typeName);
            }
#endif

            public override Type BindToType(string assemblyName, string typeName)
            {
                BindToTypeCalled = true;
                return base.BindToType(assemblyName, typeName);
            }
        }

        [Test]
        public void TypeNameHandlingAuto()
        {
            var binder = new MyBinder();

            var settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto,
#pragma warning disable CS0618 // Type or member is obsolete
                Binder = binder
#pragma warning restore CS0618 // Type or member is obsolete
            };

            Zoo zoo = new Zoo
            {
                Animals = new List<Animal>
                {
                    new Dog("Dog!")
                }
            };

            JsonSerializer serializer = JsonSerializer.Create(settings);

            MemoryStream ms = new MemoryStream();
            CborDataWriter bsonWriter = new CborDataWriter(ms);
            serializer.Serialize(bsonWriter, zoo);

            ms.Seek(0, SeekOrigin.Begin);

            var deserialized = serializer.Deserialize<Zoo>(new CborDataReader(ms));

            Assert.AreEqual(1, deserialized.Animals.Count);
            Assert.AreEqual("Dog!", deserialized.Animals[0].Name);
            Assert.IsTrue(deserialized.Animals[0] is Dog);

#if !(NET20 || NET35)
            Assert.IsTrue(binder.BindToNameCalled);
#endif
            Assert.IsTrue(binder.BindToTypeCalled);
        }

        [Test]
        public void GuidsShouldBeProperlyDeserialised()
        {
            Guid g = new Guid("822C0CE6-CC42-4753-A3C3-26F0684A4B88");

            MemoryStream ms = new MemoryStream();
            CborDataWriter writer = new CborDataWriter(ms);
            writer.WriteStartObject();
            writer.WritePropertyName("TheGuid");
            writer.WriteValue(g);
            writer.WriteEndObject();
            writer.Flush();

            byte[] bytes = ms.ToArray();

            CborDataReader reader = new CborDataReader(new MemoryStream(bytes));
            Assert.IsTrue(reader.Read());
            Assert.IsTrue(reader.Read());

            Assert.IsTrue(reader.Read());
            Assert.AreEqual(JsonToken.Bytes, reader.TokenType);
            Assert.AreEqual(typeof(Guid), reader.ValueType);
            Assert.AreEqual(g, (Guid)reader.Value);

            Assert.IsTrue(reader.Read());
            Assert.IsFalse(reader.Read());

            JsonSerializer serializer = new JsonSerializer();
            serializer.MetadataPropertyHandling = MetadataPropertyHandling.Default;
            ObjectTestClass b = serializer.Deserialize<ObjectTestClass>(new CborDataReader(new MemoryStream(bytes)));
            Assert.AreEqual(typeof(Guid), b.TheGuid.GetType());
            Assert.AreEqual(g, (Guid)b.TheGuid);
        }

        [Test]
        public void GuidsShouldBeProperlyDeserialised_AsBytes()
        {
            Guid g = new Guid("822C0CE6-CC42-4753-A3C3-26F0684A4B88");

            MemoryStream ms = new MemoryStream();
            CborDataWriter writer = new CborDataWriter(ms);
            writer.WriteStartObject();
            writer.WritePropertyName("TheGuid");
            writer.WriteValue(g);
            writer.WriteEndObject();
            writer.Flush();

            byte[] bytes = ms.ToArray();

            CborDataReader reader = new CborDataReader(new MemoryStream(bytes));
            Assert.IsTrue(reader.Read());
            Assert.IsTrue(reader.Read());

            CollectionAssert.AreEquivalent(g.ToByteArray(), reader.ReadAsBytes());
            Assert.AreEqual(JsonToken.Bytes, reader.TokenType);
            Assert.AreEqual(typeof(byte[]), reader.ValueType);
            CollectionAssert.AreEquivalent(g.ToByteArray(), (byte[])reader.Value);

            Assert.IsTrue(reader.Read());
            Assert.IsFalse(reader.Read());

            JsonSerializer serializer = new JsonSerializer();
            BytesTestClass b = serializer.Deserialize<BytesTestClass>(new CborDataReader(new MemoryStream(bytes)));
            CollectionAssert.AreEquivalent(g.ToByteArray(), b.TheGuid);
        }

        [Test]
        public void GuidsShouldBeProperlyDeserialised_AsBytes_ReadAhead()
        {
            Guid g = new Guid("822C0CE6-CC42-4753-A3C3-26F0684A4B88");

            MemoryStream ms = new MemoryStream();
            CborDataWriter writer = new CborDataWriter(ms);
            writer.WriteStartObject();
            writer.WritePropertyName("TheGuid");
            writer.WriteValue(g);
            writer.WriteEndObject();
            writer.Flush();

            byte[] bytes = ms.ToArray();

            CborDataReader reader = new CborDataReader(new MemoryStream(bytes));
            Assert.IsTrue(reader.Read());
            Assert.IsTrue(reader.Read());

            CollectionAssert.AreEquivalent(g.ToByteArray(), reader.ReadAsBytes());
            Assert.AreEqual(JsonToken.Bytes, reader.TokenType);
            Assert.AreEqual(typeof(byte[]), reader.ValueType);
            CollectionAssert.AreEquivalent(g.ToByteArray(), (byte[])reader.Value);

            Assert.IsTrue(reader.Read());
            Assert.IsFalse(reader.Read());

            JsonSerializer serializer = new JsonSerializer();
            serializer.MetadataPropertyHandling = MetadataPropertyHandling.ReadAhead;
            BytesTestClass b = serializer.Deserialize<BytesTestClass>(new CborDataReader(new MemoryStream(bytes)));
            CollectionAssert.AreEquivalent(g.ToByteArray(), b.TheGuid);
        }

        [Test]
        public void DeserializeBsonDocumentWithString()
        {
            byte[] data = HexToBytes("A1-61-62-63-61-62-63");
            JsonSerializer serializer = new JsonSerializer();
            JObject jObj = (JObject)serializer.Deserialize(new CborDataReader(new MemoryStream(data)));
            string stringValue = jObj.Value<string>("b");
            Assert.AreEqual("abc", stringValue);
        }

        [Test]
        public void DeserializeBsonDocumentWithGuid()
        {
            byte[] data = HexToBytes("A1-61-62-78-18-33-30-48-6A-34-6A-6E-75-75-30-79-47-77-41-61-6E-5A-44-4E-68-34-51-3D-3D");
            JsonSerializer serializer = new JsonSerializer();
            JObject jObj = (JObject)serializer.Deserialize(new CborDataReader(new MemoryStream(data)));
            Guid guidValue = jObj.Value<Guid>("b");
            Assert.AreEqual(new Guid("e2e341df-ee39-4cbb-86c0-06a7643361e1"), guidValue);
        }

        public class BytesTestClass
        {
            public byte[] TheGuid { get; set; }
        }

        public class ObjectTestClass
        {
            public object TheGuid { get; set; }
        }
    }
}