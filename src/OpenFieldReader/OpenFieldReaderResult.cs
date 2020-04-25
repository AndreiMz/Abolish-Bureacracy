using System;
using System.Collections.Generic;
using System.Text;

namespace OpenFieldReader
{
    internal class OpenFieldReaderResult
    {
        public int ReturnCode { get; set; }
        public List<List<Box>> Boxes { get; set; }
    }
}
