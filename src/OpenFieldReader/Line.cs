using System;
using System.Collections.Generic;
using System.Text;
using static OpenFieldReader.Structures;

namespace OpenFieldReader
{ 
    internal class Line 
    {
        public Junction[] Junctions { get; set; }
        public float GapX { get; set; }
    }
}
