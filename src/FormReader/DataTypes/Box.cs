using static FormReader.Structures;

namespace FormReader
{
    public class Box
    {
        public Point TopLeft { get; set; }
        public Point TopRight { get; set; }
        public Point BottomLeft { get; set; }
        public Point BottomRight { get; set; }

        public int GetHeight()
        {
            return ((this.BottomRight.Y + this.BottomLeft.Y) / 2) - ((this.TopRight.Y + this.TopLeft.Y) / 2);
        }

        public int GetWidth()
        {
            return ((this.TopRight.X + this.BottomRight.X) / 2) - ((this.TopLeft.X + this.BottomLeft.X) / 2);
        }
    }
}
