using System;
using System.Threading.Tasks;

namespace TerraForge
{
    public class MapResult
    {
        public byte[] PixelData { get; set; }
        public float[,] Heights { get; set; }
        public float[,] Moistures { get; set; }
        public bool[,] Rivers { get; set; }
    }

    public class MapGenerator
    {
        public async Task<MapResult> GenerateMapAsync(
            int width, int height,
            Noise localNoise, Noise localMoistureNoise, float[,] falloffMap,
            int mapSeed, float scale, int octaves, float persistence, float lacunarity,
            bool applyFalloff, bool isColorMap, bool isHeightMap)
        {
            return await Task.Run(() =>
            {
                int bytesPerPixel = 4;
                int stride = width * bytesPerPixel;
                byte[] pixelData = new byte[height * stride];

                float[,] heights = new float[width, height];
                float[,] moistures = new float[width, height];
                bool[,] rivers = new bool[width, height];

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
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

                        heights[x, y] = noiseValue;
                        moistures[x, y] = moistureValue;
                    }
                }

                Random rnd = new Random(mapSeed);
                int attemptedRivers = 0;
                int validRivers = 0;

                while (attemptedRivers < 1000 && validRivers < 40)
                {
                    attemptedRivers++;
                    int startX = rnd.Next(width);
                    int startY = rnd.Next(height);

                    if (heights[startX, startY] > 0.6f)
                    {
                        validRivers++;
                        int cx = startX;
                        int cy = startY;

                        while (true)
                        {
                            rivers[cx, cy] = true;

                            if (heights[cx, cy] < 0.2f)
                            {
                                break;
                            }

                            int nextX = cx;
                            int nextY = cy;
                            float lowest = heights[cx, cy];

                            for (int ny = -1; ny <= 1; ny++)
                            {
                                for (int nx = -1; nx <= 1; nx++)
                                {
                                    if (nx == 0 && ny == 0) continue;
                                    
                                    int chkX = cx + nx;
                                    int chkY = cy + ny;

                                    if (chkX >= 0 && chkX < width && chkY >= 0 && chkY < height)
                                    {
                                        if (heights[chkX, chkY] < lowest)
                                        {
                                            lowest = heights[chkX, chkY];
                                            nextX = chkX;
                                            nextY = chkY;
                                        }
                                    }
                                }
                            }

                            if (nextX == cx && nextY == cy)
                            {
                                break;
                            }

                            cx = nextX;
                            cy = nextY;
                        }
                    }
                }

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int index = y * stride + x * bytesPerPixel;
                        
                        float noiseValue = heights[x, y];
                        float moistureValue = moistures[x, y];
                        
                        byte r = 0, g = 0, b = 0;
                        
                        if (isColorMap)
                        {
                            if (rivers[x, y] && noiseValue >= 0.2f)
                            {
                                r = 30; g = 144; b = 255;
                            }
                            else if (noiseValue < 0f)
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

                        if (x > 0)
                        {
                            float diff = heights[x, y] - heights[x - 1, y];
                            float shade = Math.Clamp(1.0f + diff * 5f, 0.3f, 1.7f);

                            r = (byte)Math.Clamp(r * shade, 0, 255);
                            g = (byte)Math.Clamp(g * shade, 0, 255);
                            b = (byte)Math.Clamp(b * shade, 0, 255);
                        }

                        pixelData[index] = b;
                        pixelData[index + 1] = g;
                        pixelData[index + 2] = r;
                        pixelData[index + 3] = 255;
                    }
                }

                return new MapResult
                {
                    PixelData = pixelData,
                    Heights = heights,
                    Moistures = moistures,
                    Rivers = rivers
                };
            });
        }
    }
}
