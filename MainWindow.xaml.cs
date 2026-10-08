using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace TerraForge
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            GenerateWhiteNoise();
        }

        private void GenerateWhiteNoise()
        {
            int width = 512;
            int height = 512;
            WriteableBitmap bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
            MapImage.Source = bitmap;

            int bytesPerPixel = bitmap.Format.BitsPerPixel / 8;
            int stride = bitmap.PixelWidth * bytesPerPixel;
            byte[] pixelData = new byte[height * stride];

            Random random = new Random();

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * stride + x * bytesPerPixel;
                    byte colorValue = (byte)(random.Next(2) == 0 ? 0 : 255);
                    
                    pixelData[index] = colorValue;
                    pixelData[index + 1] = colorValue;
                    pixelData[index + 2] = colorValue;
                    pixelData[index + 3] = 255;
                }
            }

            bitmap.WritePixels(new Int32Rect(0, 0, width, height), pixelData, stride, 0);
        }
    }
}
