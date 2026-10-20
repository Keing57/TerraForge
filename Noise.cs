using System;

namespace TerraForge
{
    public class Noise
    {
        private readonly int[] permutation = new int[512];

        public Noise(int seed)
        {
            Random random = new Random(seed);
            int[] p = new int[256];
            for (int i = 0; i < 256; i++)
                p[i] = i;

            for (int i = 0; i < 256; i++)
            {
                int r = random.Next(256);
                int temp = p[i];
                p[i] = p[r];
                p[r] = temp;
            }

            for (int i = 0; i < 256; i++)
            {
                permutation[i] = p[i];
                permutation[256 + i] = p[i];
            }
        }

        private float Fade(float t)
        {
            return t * t * t * (t * (t * 6 - 15) + 10);
        }

        private float Lerp(float t, float a, float b)
        {
            return a + t * (b - a);
        }

        private float Grad(int hash, float x, float y)
        {
            int h = hash & 15;
            float u = h < 8 ? x : y;
            float v = h < 4 ? y : h == 12 || h == 14 ? x : 0;
            return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
        }

        public float GetNoise(float x, float y)
        {
            int X = (int)Math.Floor(x) & 255;
            int Y = (int)Math.Floor(y) & 255;

            x -= (float)Math.Floor(x);
            y -= (float)Math.Floor(y);

            float u = Fade(x);
            float v = Fade(y);

            int A = permutation[X] + Y;
            int AA = permutation[A];
            int AB = permutation[A + 1];
            int B = permutation[X + 1] + Y;
            int BA = permutation[B];
            int BB = permutation[B + 1];

            float res = Lerp(v, Lerp(u, Grad(permutation[AA], x, y),
                                        Grad(permutation[BA], x - 1, y)),
                                Lerp(u, Grad(permutation[AB], x, y - 1),
                                        Grad(permutation[BB], x - 1, y - 1)));

            return res;
        }

        public float GetFBM(float x, float y, int octaves, float persistence, float lacunarity)
        {
            float total = 0;
            float frequency = 1;
            float amplitude = 1;
            float maxValue = 0;

            for (int i = 0; i < octaves; i++)
            {
                total += GetNoise(x * frequency, y * frequency) * amplitude;
                maxValue += amplitude;
                amplitude *= persistence;
                frequency *= lacunarity;
            }

            return total / maxValue;
        }
    }
}
