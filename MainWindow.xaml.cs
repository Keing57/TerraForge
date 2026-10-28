using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace TerraForge
{
    public partial class MainWindow : Window
    {
        private bool isInitialized = false;
        private Noise currentNoise;
        private Noise moistureNoise;
        private float[,] falloffMap;

        public MainWindow()
        {
            InitializeComponent();
            falloffMap = FalloffGenerator.GenerateFalloffMap(512);
            InitializeNoise();
            isInitialized = true;
            GenerateHeightmap();
        }

        private void InitializeNoise()
        {
            if (int.TryParse(SeedTextBox.Text, out int seed))
            {
                currentNoise = new Noise(seed);
                moistureNoise = new Noise(seed + 1);
            }
            else
            {
                currentNoise = new Noise(0);
                moistureNoise = new Noise(1);
            }
        }

        private void SeedTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (isInitialized)
            {
                InitializeNoise();
                GenerateHeightmap();
            }
        }

        private void RandomizeSeedButton_Click(object sender, RoutedEventArgs e)
        {
            Random rnd = new Random();
            SeedTextBox.Text = rnd.Next().ToString();
        }

        private void CheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (isInitialized)
            {
                GenerateHeightmap();
            }
        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (isInitialized)
            {
                GenerateHeightmap();
            }
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (isInitialized)
            {
                ScaleValueText.Text = ScaleSlider.Value.ToString("F4");
                OctavesValueText.Text = OctavesSlider.Value.ToString("F0");
                PersistenceValueText.Text = PersistenceSlider.Value.ToString("F2");
                LacunarityValueText.Text = LacunaritySlider.Value.ToString("F2");
                
                GenerateHeightmap();
            }
        }

        private void GenerateHeightmap()
        {
            if (currentNoise == null || moistureNoise == null || falloffMap == null) return;

            int width = 512;
            int height = 512;
            WriteableBitmap bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
            MapImage.Source = bitmap;

            int bytesPerPixel = bitmap.Format.BitsPerPixel / 8;
            int stride = bitmap.PixelWidth * bytesPerPixel;
            byte[] pixelData = new byte[height * stride];

            float scale = (float)ScaleSlider.Value;
            int octaves = (int)OctavesSlider.Value;
            float persistence = (float)PersistenceSlider.Value;
            float lacunarity = (float)LacunaritySlider.Value;
            bool applyFalloff = IslandModeCheckBox.IsChecked == true;

            bool isColorMap = ColorMapRadio.IsChecked == true;
            bool isHeightMap = HeightMapRadio.IsChecked == true;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * stride + x * bytesPerPixel;
                    
                    float noiseValue = currentNoise.GetFBM(x * scale, y * scale, octaves, persistence, lacunarity);
                    float moistureValue = moistureNoise.GetFBM(x * scale, y * scale, octaves, persistence, lacunarity);
                    
                    if (applyFalloff)
                    {
                        noiseValue = Math.Clamp(noiseValue - falloffMap[x, y], -1f, 1f);
                    }
                    else
                    {
                        noiseValue = Math.Clamp(noiseValue, -1f, 1f);
                    }
                    moistureValue = Math.Clamp(moistureValue, -1f, 1f);
                    
                    byte r = 0, g = 0, b = 0;
                    
                    if (isColorMap)
                    {
                        if (noiseValue < 0f)
                        {
                            r = 0; g = 0; b = 139;
                        }
                        else if (noiseValue < 0.2f)
                        {
                            r = 173; g = 216; b = 230;
                        }
                        else
                        {
                            if (noiseValue < 0.4f)
                            {
                                if (moistureValue < 0f)
                                {
                                    r = 255; g = 255; b = 153;
                                }
                                else if (moistureValue < 0.4f)
                                {
                                    r = 144; g = 238; b = 144;
                                }
                                else
                                {
                                    r = 0; g = 100; b = 0;
                                }
                            }
                            else if (noiseValue < 0.7f)
                            {
                                if (moistureValue < 0f)
                                {
                                    r = 240; g = 230; b = 140;
                                }
                                else if (moistureValue < 0.4f)
                                {
                                    r = 34; g = 139; b = 34;
                                }
                                else
                                {
                                    r = 85; g = 107; b = 47;
                                }
                            }
                            else
                            {
                                if (moistureValue < 0f)
                                {
                                    r = 128; g = 128; b = 128;
                                }
                                else if (moistureValue < 0.4f)
                                {
                                    r = 192; g = 192; b = 192;
                                }
                                else
                                {
                                    r = 255; g = 255; b = 255;
                                }
                            }
                        }
                    }
                    else if (isHeightMap)
                    {
                        byte colorValue = (byte)Math.Clamp((noiseValue + 1f) * 0.5f * 255f, 0, 255);
                        r = colorValue;
                        g = colorValue;
                        b = colorValue;
                    }
                    else
                    {
                        byte colorValue = (byte)Math.Clamp((moistureValue + 1f) * 0.5f * 255f, 0, 255);
                        r = colorValue;
                        g = colorValue;
                        b = colorValue;
                    }

                    pixelData[index] = b;
                    pixelData[index + 1] = g;
                    pixelData[index + 2] = r;
                    pixelData[index + 3] = 255;
                }
            }

            bitmap.WritePixels(new Int32Rect(0, 0, width, height), pixelData, stride, 0);
        }
    }
}
