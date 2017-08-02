namespace Newtonsoft.Json.Cbor
{
    /// <summary>
    /// Affects the binary representation of collections (Arrays and Maps) during writing
    /// </summary>
    public enum CborWriterCollectionBehaviour
    {
        /// <summary>
        /// Try to get the size of the collection before flushing to the output stream. If flushed early, any collection queued will be treated as indefinite.
        /// </summary>
        DefiniteWherePossible,
        /// <summary>
        /// Always wait for the collection to be closed by queing data to write before flushing to the output stream.
        /// </summary>
        AlwaysDefinitie,
        /// <summary>
        /// Always treat collections as indefinite, allowing output streams to be written sooner.
        /// </summary>
        AlwaysIndefinite
    }
}