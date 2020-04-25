using System;
using System.Collections.Generic;
using System.Text;

namespace OpenFieldReader
{
    internal class BoxesCluster
    {
        public LineCluster TopLine { get; set; }
        public LineCluster BottomLine { get; set; }
        public float GapY { get; set; }
    }
}
