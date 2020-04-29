using System;
using System.Collections.Generic;
using System.Text;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using static FormReader.Structures;

namespace FormReader.Debugger {
    internal class Painter{
        private static int nextGroupId = 0;
        private int [,] debugImg;
        private int imageHeight;
        private int imageWidth;

        public Painter(int imgHeight, int imgWidth){
            this.debugImg = new int[imgHeight, imgWidth];
            for (int y = 0; y < imgHeight; y++)
                for (int x = 0; x < imgWidth; x++)
                    this.debugImg[y, x] = 0;
            this.imageHeight = imgHeight;
            this.imageWidth = imgWidth;
        }
        private void DrawJunction(int[,] outputImg, int colorCode, Junction junction){
			var x = junction.X;
			var y = junction.Y;

			if (junction.Top)
				for (int i = 0; i < junction.NumTop; i++)
					outputImg[y - i, x] = colorCode;
			if (junction.Bottom)
				for (int i = 0; i < junction.NumBottom; i++)
					outputImg[y + i, x] = colorCode;
			if (junction.Right)
				for (int i = 0; i < junction.NumRight; i++)
					outputImg[y, x + i] = colorCode;
			if (junction.Left)
				for (int i = 0; i < junction.NumLeft; i++)
					outputImg[y, x - i] = colorCode;
		}

        internal void DrawPoint(int[,] outputImg, int colorCode, int x, int y, int size){
			// Must be centered.
			x -= size / 2;
			y -= size / 2;
			for (int i = 0; i < size; i++){
				for (int j = 0; j < size; j++){
					var curY = y + i;
					var curX = x + j;

					if (curX >= 0 && curY >= 0 && curX < outputImg.GetLength(1) && curY < outputImg.GetLength(0)){
						outputImg[y + i, x + j] = colorCode;
                    }
				}
            }
		}

		internal void AssignColor(int[,] labels, bool hasColor){
			var row = labels.GetLength(0);
			var col = labels.GetLength(1);
		    Random random = new Random();

			Dictionary<int, int> cacheColor = new Dictionary<int, int>();
			int color = 0;

			for (int i = 0; i < row; i++){
				for (int j = 0; j < col; j++){
					var val = labels[i, j];

					if (val > 0){
						if (hasColor){
							if (!cacheColor.ContainsKey(val)){
								// Generate a random color
								color =
									(255 - random.Next(70, 200)) << 16 |
									(255 - random.Next(100, 225)) << 8 |
									(255 - random.Next(100, 230));
								cacheColor.Add(val, color);
							}
							else{
								color = cacheColor[val];
							}
							labels[i, j] = color;
						}
						else{
							labels[i, j] = 0xFFFFFF;
						}
					}
					else{
						labels[i, j] = 0xFFFFFF;
					}
				}
			}
		}
    
        internal void DrawBoxes(List<List<Box>> allBoxes){
            int size = 5;
            foreach (var item in allBoxes){
                nextGroupId++;
                foreach (var box in item){
                    DrawPoint(debugImg, nextGroupId, box.TopLeft.X, box.TopLeft.Y, size);
                    DrawPoint(debugImg, nextGroupId, box.TopRight.X, box.TopRight.Y, size);
                    DrawPoint(debugImg, nextGroupId, box.BottomLeft.X, box.BottomLeft.Y, size);
                    DrawPoint(debugImg, nextGroupId, box.BottomRight.X, box.BottomRight.Y, size);

                    // Let's show the center of the box.
                    var x = (box.TopLeft.X + box.TopRight.X + box.BottomLeft.X + box.BottomRight.X) / 4;
                    var y = (box.TopLeft.Y + box.TopRight.Y + box.BottomLeft.Y + box.BottomRight.Y) / 4;
                    DrawPoint(debugImg, nextGroupId, x, y, 10);
                }
            }
        }
    
        internal void DrawImage(string outputPath="debug.jpg"){
            using (var image = new Image<Rgba32>(imageWidth, imageHeight)){
                AssignColor(debugImg, true);

                for (int y = 0; y < imageHeight; y++){
                    for (int x = 0; x < imageWidth; x++){
                        int val = debugImg[y, x];
                        byte r = (byte)(~((val >> 16) & 0xFF));
                        byte g = (byte)(~((val >> 8) & 0xFF));
                        byte b = (byte)(~((val) & 0xFF));
                        image[x, y] = new Rgba32(r, g, b);
                    }
                }

                image.Save(outputPath);
			}
        }

        internal void DrawJunctions(List<LineCluster> lineClusters, List<BoxesCluster> boxesClusters){
            foreach (var item in lineClusters){
					nextGroupId++;
					foreach (var junction in item.Junctions){
						DrawJunction(debugImg, nextGroupId, junction);
					}
				}

            foreach (var item in boxesClusters){
                nextGroupId++;

                // Debug img:
                foreach (var junction in item.TopLine.Junctions){
                    DrawJunction(debugImg, nextGroupId, junction);
                }

                foreach (var junction in item.BottomLine.Junctions){
                    DrawJunction(debugImg, nextGroupId, junction);
                }
            }
        }
    }
}