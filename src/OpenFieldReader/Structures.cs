using System;
using System.Collections.Generic;
using System.Text;

namespace OpenFieldReader
{
    public class Structures
    {
        public struct Point
        {
            public int X { get; set; }
            public int Y { get; set; }

            public Point(double x, double y)
            {
                X = (int)x;
                Y = (int)y;
            }

            public override string ToString()
            {
                return X + ", " + Y;
            }
        }

        public struct CachedJunctions
        {
            public Dictionary<int, List<Junction>> cacheListJunctionPerLine;
            public List<Junction> listJunction;
        }

        public struct Junction
        {
            public bool Top { get; set; }
            public bool Bottom { get; set; }
            public bool Left { get; set; }
            public bool Right { get; set; }
            public byte NumTop { get; set; }
            public byte NumBottom { get; set; }
            public byte NumLeft { get; set; }
            public byte NumRight { get; set; }
            public int X { get; set; }
            public int Y { get; set; }
            public int GroupId { get; set; }
            public float GapX { get; set; }
        }
    }
}
