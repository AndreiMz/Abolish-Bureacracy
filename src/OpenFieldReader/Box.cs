using System;
using System.Collections.Generic;
using System.Text;
using static OpenFieldReader.Structures;

namespace OpenFieldReader
{
    internal class Box
    {
        public Point TopLeft { get; set; }
        public Point TopRight { get; set; }
        public Point BottomLeft { get; set; }
        public Point BottomRight { get; set; }
    }
}
