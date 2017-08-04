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
using System.Text.RegularExpressions;
using Newtonsoft.Json.Bson;
using System.Globalization;
using System.Text;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Cbor.Utilities;

namespace Newtonsoft.Json.Cbor.Converters
{
    /// <summary>
    /// Converts a <see cref="Regex"/> to and from BSON.
    /// </summary>
    public class CborDataRegexConverter : JsonConverter
    {
        private const int OptionTag = 35;

        /// <summary>
        /// Writes the JSON representation of the object.
        /// </summary>
        /// <param name="writer">The <see cref="JsonWriter"/> to write to.</param>
        /// <param name="value">The value.</param>
        /// <param name="serializer">The calling serializer.</param>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            Regex regex = (Regex) value;

            CborDataWriter cborWriter = writer as CborDataWriter;
            if (cborWriter == null)
            {
                throw ExceptionUtils.CreateJsonSerializationException(cborWriter as IJsonLineInfo, cborWriter.Path, $"{nameof(CborDataRegexConverter)} only supports writing a regex with {nameof(CborDataWriter)}.", null);
            }

            WriteCbor(cborWriter, regex);
        }

        private bool HasFlag(RegexOptions options, RegexOptions flag)
        {
            return ((options & flag) == flag);
        }

        private void WriteCbor(CborDataWriter writer, Regex regex)
        {
            var options = new StringBuilder();

            if (HasFlag(regex.Options, RegexOptions.IgnoreCase))
            {
                options.Append("i");
            }

            if (HasFlag(regex.Options, RegexOptions.Multiline))
            {
                options.Append("m");
            }

            if (!regex.Options.HasFlag(RegexOptions.ECMAScript))
            {
                if (HasFlag(regex.Options, RegexOptions.Singleline))
                {
                    options.Append("s");
                }

                // Support unicode codepoints 
                options.Append("u");

                if (HasFlag(regex.Options, RegexOptions.ExplicitCapture))
                {
                    options.Append("x");
                }
            }

            writer.WriteTag(OptionTag);
            writer.WriteValue(options.Length > 0 ? $"/{regex}/{options}" : regex.ToString());
        }

        /// <summary>
        /// Reads the JSON representation of the object.
        /// </summary>
        /// <param name="reader">The <see cref="JsonReader"/> to read from.</param>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="existingValue">The existing value of object being read.</param>
        /// <param name="serializer">The calling serializer.</param>
        /// <returns>The object value.</returns>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
            //switch (reader.TokenType)
            //{
            //    case JsonToken.StartObject:
            //        return ReadRegexObject(reader, serializer);
            //    case JsonToken.String:
            //        return ReadRegexString(reader);
            //    case JsonToken.Null:
            //        return null;
            //}

            //throw ExceptionUtils.CreateJsonSerializationException(reader as IJsonLineInfo, reader.Path, "Unexpected token when reading Regex.", null);
        }

        private object ReadRegexString(JsonReader reader)
        {
            string regexText = (string)reader.Value;
            int patternOptionDelimiterIndex = regexText.LastIndexOf('/');

            string patternText = regexText.Substring(1, patternOptionDelimiterIndex - 1);
            string optionsText = regexText.Substring(patternOptionDelimiterIndex + 1);

            RegexOptions options = RegexOptions.None;
            foreach (char c in optionsText)
            {
                switch (c)
                {
                    case 'i':
                        options |= RegexOptions.IgnoreCase;
                        break;
                    case 'm':
                        options |= RegexOptions.Multiline;
                        break;
                    case 's':
                        options |= RegexOptions.Singleline;
                        break;
                    case 'x':
                        options |= RegexOptions.ExplicitCapture;
                        break;
                }
            }

            return new Regex(patternText, options);
        }

        //private Regex ReadRegexObject(JsonReader reader, JsonSerializer serializer)
        //{
        //    string pattern = null;
        //    RegexOptions? options = null;

        //    while (reader.Read())
        //    {
        //        switch (reader.TokenType)
        //        {
        //            case JsonToken.PropertyName:
        //                string propertyName = reader.Value.ToString();

        //                if (!reader.Read())
        //                {
        //                    throw ExceptionUtils.CreateJsonSerializationException(reader as IJsonLineInfo, reader.Path, "Unexpected end when reading Regex.", null);
        //                }

        //                if (string.Equals(propertyName, PatternName, StringComparison.OrdinalIgnoreCase))
        //                {
        //                    pattern = (string)reader.Value;
        //                }
        //                else if (string.Equals(propertyName, OptionsName, StringComparison.OrdinalIgnoreCase))
        //                {
        //                    options = serializer.Deserialize<RegexOptions>(reader);
        //                }
        //                else
        //                {
        //                    reader.Skip();
        //                }
        //                break;
        //            case JsonToken.Comment:
        //                break;
        //            case JsonToken.EndObject:
        //                if (pattern == null)
        //                {
        //                    throw ExceptionUtils.CreateJsonSerializationException(reader as IJsonLineInfo, reader.Path, "Error deserializing Regex. No pattern found.", null);
        //                }

        //                return new Regex(pattern, options ?? RegexOptions.None);
        //        }
        //    }

        //    throw ExceptionUtils.CreateJsonSerializationException(reader as IJsonLineInfo, reader.Path, "Unexpected end when reading Regex.", null);
        //}

        /// <summary>
        /// Determines whether this instance can convert the specified object type.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <returns>
        /// 	<c>true</c> if this instance can convert the specified object type; otherwise, <c>false</c>.
        /// </returns>
        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(Regex));
        }
    }
}