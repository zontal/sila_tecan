using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Tecan.Sila2.Server.Binary
{
    internal class FileUploadData
    {
        public Stream FileStream { get; }

        public uint ChunkCount { get; }

        public uint ChunkIndex { get; set; }

        public long Size { get; }

        public DateTimeOffset ExpirationTime { get; set; }

        public SortedDictionary<uint, byte[]> Cached { get; } = new SortedDictionary<uint, byte[]>();

        public FileUploadData( Stream stream, uint chunkCount, long size )
        {
            FileStream = stream;
            ChunkCount = chunkCount;
            Size = size;
            Completion = new TaskCompletionSource<object>();
        }

        public TaskCompletionSource<object> Completion
        {
            get;
        }
    }

}
