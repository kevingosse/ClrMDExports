using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace ClrMDExports
{
    internal class StripDmlWriter : TextWriter
    {
        private readonly TextWriter _out;

        public StripDmlWriter(TextWriter console)
        {
            _out = console;
        }

        public override Encoding Encoding => _out.Encoding;

        public override void Write(string value)
        {
            if (value != null)
            {
                _out.Write(Regex.Replace(value, @"<[^>]*>", string.Empty));
            }
        }
    }
}