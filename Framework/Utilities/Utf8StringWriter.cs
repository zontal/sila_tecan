using System.IO;
using System.Text;

namespace Tecan.Sila2
{
    internal sealed class Utf8StringWriter : StringWriter
    {
        private static readonly Encoding Utf8Encoding = new UTF8Encoding( false );

        public override Encoding Encoding => Utf8Encoding;
    }
}