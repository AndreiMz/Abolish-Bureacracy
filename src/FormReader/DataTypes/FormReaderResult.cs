using System.Collections.Generic;

namespace FormReader
{
    public class FormReaderResult
    {
        public string ImageHexa { get; set; }
        public List<List<Box>> Boxes { get; set; }
    }
}
