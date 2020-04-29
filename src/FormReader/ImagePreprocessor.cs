using System;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;

namespace FormReader
{ 
    class ImagePreprocessor{
        public int imgHeight { get; private set;}
        public int imgWidth { get; private set;}
        public int[] imgData { get; private set;}

        public string imgHexa { get; private set; }
        public ImagePreprocessor(string inputFile){
            using (var image = SixLabors.ImageSharp.Image.Load(inputFile)){
                try{
                    this.imgHeight = image.Height;
                    this.imgWidth = image.Width;
                    // FILTHY HARDCODING. IS PNG BEST?
                    this.imgHexa = image.ToBase64String(PngFormat.Instance);
                    this.imgData = grabDataFromImage(image);

                }
                
                catch(Exception){
                    throw;
                }
            }
        }

        private int[] grabDataFromImage(Image<Rgba32> image){
            int [] imgData = new int[image.Height * image.Width];
            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    var pixel = image[x, y];
                    var val = pixel.R | pixel.G | pixel.B;
                    imgData[y + x * image.Height] = val > 225 ? 0 : 255;
                }
            }

            return imgData;
        }
    }
}