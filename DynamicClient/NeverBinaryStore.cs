using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Tecan.Sila2.DynamicClient
{
    internal class NeverBinaryStore : IBinaryStore
    {
        public static NeverBinaryStore Instance = new NeverBinaryStore();

        public void Delete(string identifier)
        {
            throw new NotSupportedException();
        }

        public Stream ResolveStoredBinary(string identifier)
        {
            throw new NotSupportedException();
        }

        public FileInfo ResolveStoredBinaryFile(string identifier)
        {
            throw new NotSupportedException();
        }

        public bool ShouldStoreBinary(long length)
        {
            return false;
        }

        public string StoreBinary(Stream data)
        {
            throw new NotSupportedException();
        }

        public string StoreBinary(FileInfo file)
        {
            throw new NotSupportedException();
        }
    }
}
