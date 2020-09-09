using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Tecan.Sila2.Server.Binary
{
    internal class FileUploadData
    {
        public Stream FileStream { get; }

        public uint ChunkCount { get; }

        public uint ChunkIndex { get; set; }

        public DateTimeOffset ExpirationTime { get; set; }

        public SortedDictionary<uint, byte[]> Cached { get; } = new SortedDictionary<uint, byte[]>();

        public FileUploadData(Stream stream, uint chunkCount)
        {
            FileStream = stream;
            ChunkCount = chunkCount;
            Completion = new ManualResetEventSlim( false );
        }

        public ManualResetEventSlim Completion
        {
            get;
        }
    }

}
