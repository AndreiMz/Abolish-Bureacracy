using System;
using System.Collections.Generic;
using System.Text;
using static FormReader.Structures;

namespace FormReader
{ 
    internal class Line 
    {
        public Junction[] Junctions { get; set; }
        public float GapX { get; set; }
    }
}
