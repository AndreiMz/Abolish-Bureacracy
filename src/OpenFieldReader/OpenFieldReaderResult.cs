using System;
using System.Collections.Generic;
using System.Text;

namespace OpenFieldReader
{
    public class OpenFieldReaderResult
    {
        public string ImageHexa { get; set; }
        public List<List<Box>> Boxes { get; set; }
    }
}
