using System;
using System.Collections.Generic;
using System.Text;
using static OpenFieldReader.Structures;

namespace OpenFieldReader
{
    internal class LineCluster
    {
        public int X { get; set; }
        public int Y { get; set; }
        public bool Top { get; set; }
        public bool Bottom { get; set; }
        public Junction[] Junctions { get; set; }
    }
}
