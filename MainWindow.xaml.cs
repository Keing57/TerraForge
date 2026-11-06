using System;
using System.Threading.Tasks;
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
        private int currentGeneration = 0;
        
        private Point dragStartPoint;
        private Point translateStartPoint;
        private bool isDragging = false;

        public MainWindow()
        {
            InitializeComponent();
            falloffMap = FalloffGenerator.GenerateFalloffMap(512);
            InitializeNoise();
            isInitialized = true;
            _ = GenerateHeightmapAsync();
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

        private void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            if (isInitialized)
            {
                InitializeNoise();
                _ = GenerateHeightmapAsync();
            }
        }

        private void ExportPngButton_Click(object sender, RoutedEventArgs e)
        {
            if (MapImage.Source is WriteableBitmap bitmap)
            {
                Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
                dlg.FileName = "TerraForgeMap";
                dlg.DefaultExt = ".png";
                dlg.Filter = "PNG images (.png)|*.png";

                if (dlg.ShowDialog() == true)
                {
                    PngBitmapEncoder encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(bitmap));
                    using (System.IO.FileStream stream = new System.IO.FileStream(dlg.FileName, System.IO.FileMode.Create))
                    {
                        encoder.Save(stream);
                    }
                }
            }
        }

        private void SeedTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (isInitialized)
            {
                InitializeNoise();
                if (AutoUpdateCheckBox.IsChecked == true)
                {
                    _ = GenerateHeightmapAsync();
                }
            }
        }

        private void RandomizeSeedButton_Click(object sender, RoutedEventArgs e)
        {
            Random rnd = new Random();
            SeedTextBox.Text = rnd.Next().ToString();
        }

        private void CheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (isInitialized && AutoUpdateCheckBox.IsChecked == true)
            {
                _ = GenerateHeightmapAsync();
            }
        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (isInitialized && AutoUpdateCheckBox.IsChecked == true)
            {
                _ = GenerateHeightmapAsync();
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
                
                if (AutoUpdateCheckBox.IsChecked == true)
                {
                    _ = GenerateHeightmapAsync();
                }
            }
        }

        private void Border_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            double zoomFactor = 1.1;
            if (e.Delta < 0) zoomFactor = 1.0 / zoomFactor;

            Point relative = e.GetPosition(MapImage);
            double absoluteX = relative.X * MapScaleTransform.ScaleX + MapTranslateTransform.X;
            double absoluteY = relative.Y * MapScaleTransform.ScaleY + MapTranslateTransform.Y;

            MapScaleTransform.ScaleX *= zoomFactor;
            MapScaleTransform.ScaleY *= zoomFactor;

            MapTranslateTransform.X = absoluteX - relative.X * MapScaleTransform.ScaleX;
            MapTranslateTransform.Y = absoluteY - relative.Y * MapScaleTransform.ScaleY;
        }

        private void Border_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var border = (Border)sender;
            dragStartPoint = e.GetPosition(border);
            translateStartPoint = new Point(MapTranslateTransform.X, MapTranslateTransform.Y);
            border.CaptureMouse();
            isDragging = true;
        }

        private void Border_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var border = (Border)sender;
            border.ReleaseMouseCapture();
            isDragging = false;
        }

        private void Border_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (isDragging)
            {
                var border = (Border)sender;
                border.ReleaseMouseCapture();
                isDragging = false;
            }
        }

        private void Border_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            var border = (Border)sender;
            if (isDragging)
            {
                Point currentPoint = e.GetPosition(border);
                MapTranslateTransform.X = translateStartPoint.X + (currentPoint.X - dragStartPoint.X);
                MapTranslateTransform.Y = translateStartPoint.Y + (currentPoint.Y - dragStartPoint.Y);
            }

            if (!isInitialized || currentNoise == null || moistureNoise == null || falloffMap == null) return;

            Point pos = e.GetPosition(MapImage);
            
            double actualWidth = MapImage.ActualWidth;
            double actualHeight = MapImage.ActualHeight;

            if (actualWidth == 0 || actualHeight == 0) return;

            double imageAspect = 1.0; 
            double controlAspect = actualWidth / actualHeight;

            double renderWidth, renderHeight;
            double offsetX = 0, offsetY = 0;

            if (controlAspect > imageAspect)
            {
                renderHeight = actualHeight;
                renderWidth = actualHeight * imageAspect;
                offsetX = (actualWidth - renderWidth) / 2;
            }
            else
            {
                renderWidth = actualWidth;
                renderHeight = actualWidth / imageAspect;
                offsetY = (actualHeight - renderHeight) / 2;
            }

            double xPos = pos.X - offsetX;
            double yPos = pos.Y - offsetY;

            if (xPos >= 0 && xPos < renderWidth && yPos >= 0 && yPos < renderHeight)
            {
                int x = (int)((xPos / renderWidth) * 512);
                int y = (int)((yPos / renderHeight) * 512);

                if (x >= 0 && x < 512 && y >= 0 && y < 512)
                {
                    float scale = (float)ScaleSlider.Value;
                    int octaves = (int)OctavesSlider.Value;
                    float persistence = (float)PersistenceSlider.Value;
                    float lacunarity = (float)LacunaritySlider.Value;
                    bool applyFalloff = IslandModeCheckBox.IsChecked == true;

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

                    string biome = "";
                    if (noiseValue < 0f)
                    {
                        biome = "Deep Water";
                    }
                    else if (noiseValue < 0.2f)
                    {
                        biome = "Shallow Water";
                    }
                    else
                    {
                        if (noiseValue < 0.4f)
                        {
                            if (moistureValue < 0f) biome = "Desert";
                            else if (moistureValue < 0.4f) biome = "Grassland";
                            else biome = "Jungle";
                        }
                        else if (noiseValue < 0.7f)
                        {
                            if (moistureValue < 0f) biome = "Savanna";
                            else if (moistureValue < 0.4f) biome = "Forest";
                            else biome = "Swamp";
                        }
                        else
                        {
                            if (moistureValue < 0f) biome = "Bare Mountain";
                            else if (moistureValue < 0.4f) biome = "Snowy Mountain";
                            else biome = "Snow";
                        }
                    }

                    MapImage.ToolTip = $"Biome: {biome}\nHeight: {noiseValue:F2}\nMoisture: {moistureValue:F2}";
                }
                else
                {
                    MapImage.ToolTip = null;
                }
            }
            else
            {
                MapImage.ToolTip = null;
            }
        }

        private async Task GenerateHeightmapAsync()
        {
            if (currentNoise == null || moistureNoise == null || falloffMap == null) return;

            int generation = ++currentGeneration;

            int width = 512;
            int height = 512;
            int bytesPerPixel = 4;
            int stride = width * bytesPerPixel;
            byte[] pixelData = new byte[height * stride];

            float scale = (float)ScaleSlider.Value;
            int octaves = (int)OctavesSlider.Value;
            float persistence = (float)PersistenceSlider.Value;
            float lacunarity = (float)LacunaritySlider.Value;
            bool applyFalloff = IslandModeCheckBox.IsChecked == true;
            bool isColorMap = ColorMapRadio.IsChecked == true;
            bool isHeightMap = HeightMapRadio.IsChecked == true;
            
            Noise localNoise = currentNoise;
            Noise localMoistureNoise = moistureNoise;

            await Task.Run(() =>
            {
                for (int y = 0; y < height; y++)
                {
                    if (generation != currentGeneration) return;

                    for (int x = 0; x < width; x++)
                    {
                        int index = y * stride + x * bytesPerPixel;
                        
                        float noiseValue = localNoise.GetFBM(x * scale, y * scale, octaves, persistence, lacunarity);
                        float moistureValue = localMoistureNoise.GetFBM(x * scale, y * scale, octaves, persistence, lacunarity);
                        
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
            });

            if (generation == currentGeneration)
            {
                WriteableBitmap bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
                bitmap.WritePixels(new Int32Rect(0, 0, width, height), pixelData, stride, 0);
                MapImage.Source = bitmap;
            }
        }
    }
}
