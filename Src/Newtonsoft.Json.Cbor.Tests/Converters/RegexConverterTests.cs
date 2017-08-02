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
using System.IO;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Cbor;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Utilities;
#if DNXCORE50
using Xunit;
using Test = Xunit.FactAttribute;
using Assert = Newtonsoft.Json.Cbor.Tests.XUnitAssert;
#else
using NUnit.Framework;
#endif
using Newtonsoft.Json.Tests.TestObjects;

namespace Newtonsoft.Json.Cbor.Tests.Converters
{
    [TestFixture]
    public class RegexConverterTests : TestFixtureBase
    {
        public class RegexTestClass
        {
            public Regex Regex { get; set; }
        }

        [Test]
        public void SerializeToBson()
        {
            throw new NotImplementedException();
            //Regex regex = new Regex("abc", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

            //MemoryStream ms = new MemoryStream();
            //BsonDataWriter writer = new BsonDataWriter(ms);
            //JsonSerializer serializer = new JsonSerializer();
            //serializer.Converters.Add(new BsonDataRegexConverter());

            //serializer.Serialize(writer, new RegexTestClass { Regex = regex });

            //string expected = "13-00-00-00-0B-52-65-67-65-78-00-61-62-63-00-69-75-00-00";
            //string bson = BytesToHex(ms.ToArray());

            //Assert.AreEqual(expected, bson);
        }

        [Test]
        public void DeserializeFromBson()
        {
            throw new NotImplementedException();
            //MemoryStream ms = new MemoryStream(HexToBytes("13-00-00-00-0B-52-65-67-65-78-00-61-62-63-00-69-75-00-00"));
            //CborDataReader reader = new CborDataReader(ms);
            //JsonSerializer serializer = new JsonSerializer();
            //serializer.Converters.Add(new BsonDataRegexConverter());

            //RegexTestClass c = serializer.Deserialize<RegexTestClass>(reader);

            //Assert.AreEqual("abc", c.Regex.ToString());
            //Assert.AreEqual(RegexOptions.IgnoreCase, c.Regex.Options);
        }

        [Test]
        public void ConvertEmptyRegexBson()
        {
            throw new NotImplementedException();
            //Regex regex = new Regex(string.Empty);

            //MemoryStream ms = new MemoryStream();
            //BsonDataWriter writer = new BsonDataWriter(ms);
            //JsonSerializer serializer = new JsonSerializer();
            //serializer.Converters.Add(new BsonDataRegexConverter());

            //serializer.Serialize(writer, new RegexTestClass { Regex = regex });

            //ms.Seek(0, SeekOrigin.Begin);
            //CborDataReader reader = new CborDataReader(ms);
            //serializer.Converters.Add(new BsonDataRegexConverter());

            //RegexTestClass c = serializer.Deserialize<RegexTestClass>(reader);

            //Assert.AreEqual("", c.Regex.ToString());
            //Assert.AreEqual(RegexOptions.None, c.Regex.Options);
        }

        [Test]
        public void ConvertRegexWithAllOptionsBson()
        {
            throw new NotImplementedException();
            //Regex regex = new Regex(
            //    "/",
            //    RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Multiline | RegexOptions.ExplicitCapture);

            //MemoryStream ms = new MemoryStream();
            //BsonDataWriter writer = new BsonDataWriter(ms);
            //JsonSerializer serializer = new JsonSerializer();
            //serializer.Converters.Add(new BsonDataRegexConverter());

            //serializer.Serialize(writer, new RegexTestClass { Regex = regex });

            //string expected = "14-00-00-00-0B-52-65-67-65-78-00-2F-00-69-6D-73-75-78-00-00";
            //string bson = BytesToHex(ms.ToArray());

            //Assert.AreEqual(expected, bson);

            //ms.Seek(0, SeekOrigin.Begin);
            //CborDataReader reader = new CborDataReader(ms);
            //serializer.Converters.Add(new BsonDataRegexConverter());

            //RegexTestClass c = serializer.Deserialize<RegexTestClass>(reader);

            //Assert.AreEqual("/", c.Regex.ToString());
            //Assert.AreEqual(RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Multiline | RegexOptions.ExplicitCapture, c.Regex.Options);
        }
    }
}