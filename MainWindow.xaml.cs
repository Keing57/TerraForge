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
            GenerateHeightmap();
        }

        private void GenerateHeightmap()
        {
            int width = 512;
            int height = 512;
            WriteableBitmap bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
            MapImage.Source = bitmap;

            int bytesPerPixel = bitmap.Format.BitsPerPixel / 8;
            int stride = bitmap.PixelWidth * bytesPerPixel;
            byte[] pixelData = new byte[height * stride];

            float scale = 0.05f;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * stride + x * bytesPerPixel;
                    
                    float noiseValue = Noise.GetNoise(x * scale, y * scale);
                    byte colorValue = (byte)Math.Clamp((noiseValue + 1f) * 0.5f * 255f, 0, 255);
                    
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
