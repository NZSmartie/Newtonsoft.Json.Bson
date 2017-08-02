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

#if HAVE_ASYNC

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
#if HAVE_BIG_INTEGER
using System.Numerics;
#endif
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Cbor.Utilities;

namespace Newtonsoft.Json.Cbor
{
    public partial class CborDataReader
    {
        private AsyncBinaryReaderOwningReader _asyncReader;

        public override Task<bool> ReadAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public override Task<bool?> ReadAsBooleanAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public override Task<byte[]> ReadAsBytesAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public override Task<DateTime?> ReadAsDateTimeAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public override Task<DateTimeOffset?> ReadAsDateTimeOffsetAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public override Task<decimal?> ReadAsDecimalAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public override Task<double?> ReadAsDoubleAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public override Task<int?> ReadAsInt32Async(CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public override Task<string> ReadAsStringAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }
    }
}

#endif