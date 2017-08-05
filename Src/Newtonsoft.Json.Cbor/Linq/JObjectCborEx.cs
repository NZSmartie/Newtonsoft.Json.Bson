using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Newtonsoft.Json.Cbor.Linq
{
    /// <summary>
    /// CBOR Extension methods for <see cref="JObject"/>
    /// </summary>
    public static class JObjectCborEx
    {
        /// <summary>
        /// Load a <see cref="JObject"/> from a byte array that contains CBOR.
        /// </summary>
        /// <param name="cbor">A <see cref="byte[]"/> that contains CBOR.</param>
        /// <returns>A <see cref="JObject"/> populated from the byte array that contains CBOR.</returns>
        /// <exception cref="JsonReaderException">
        ///     <paramref name="cbor"/> is not valid CBOR.
        /// </exception>
        public static JObject Parse(byte[] cbor)
        {
            return Parse(cbor, null);
        }

        /// <summary>
        /// Load a <see cref="JObject"/> from a byte array that contains CBOR.
        /// </summary>
        /// <param name="cbor">A <see cref="byte[]"/> that contains CBOR.</param>
        /// <param name="settings">The <see cref="JsonLoadSettings"/> used to load the CBOR.
        /// If this is <c>null</c>, default load settings will be used.</param>
        /// <returns>A <see cref="JObject"/> populated from the byte array that contains CBOR.</returns>
        /// <exception cref="JsonReaderException">
        ///     <paramref name="cbor"/> is not valid CBOR.
        /// </exception>
        public static JObject Parse(byte[] cbor, JsonLoadSettings settings)
        {
            using (var reader = new CborDataReader(new MemoryStream(cbor)))
            {
                JObject o = JObject.Load(reader, settings);

                while (reader.Read())
                {
                    // Any content encountered here other than a comment will throw in the reader.
                }

                return o;
            }
        }
    }
}
