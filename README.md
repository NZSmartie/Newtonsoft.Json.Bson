# ![Logo](Doc/logo.jpg) Json.NET CBOR

A Concise Binary Object Representation (CBOR) extenesion for Newtonstoft's JSON lirbary that will **soon** fully support [[RFC 7049](https://tools.ietf.org/html/rfc7049)]

This library is still in early alpha. Forked from [Newtonsoft.Json.Bson](https://github.com/JamesNK/Newtonsoft.Json.Bson/) with plenty of renameing to do. 

So far this Supports everything short of Optional Tagging of Items ([Section 2.4 of RFC 7049](https://tools.ietf.org/html/rfc7049#section-2.4)) which includes:

 - Unsinged Numbers
 - Negative Numbers,
 - (deserialising only) IEEE 754 Half-Presicion flaots
 - IEEE 754 Single-Presicion flaots
 - IEEE 754 double-Presicion flaots
 - Booleans, Null, Undefined
 - Arrays (including indefinite length)
 - Maps (including indefinite length)
 - Text Strings
 - Byte Strings

Suppport for Serialising DateTime, Regex, et al will soon follow along with Async methods.

---

- [Homepage](http://www.newtonsoft.com/json)
- [Documentation](http://www.newtonsoft.com/json/help)
- [NuGet Package](https://www.nuget.org/packages/Newtonsoft.Json.Cbor)
- [Release Notes](https://github.com/NZSmartie/Newtonsoft.Json.Cbor/releases)
- [License](LICENSE.md)
- [Stack Overflow](http://stackoverflow.com/questions/tagged/json.net)
