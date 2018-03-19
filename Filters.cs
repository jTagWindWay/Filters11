using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.ComponentModel;




namespace Filters11
{
    abstract class Filters
    {
        protected abstract Color calculateNewPixelColor(Bitmap sourceImage, int x, int y);

        public Bitmap processImage(Bitmap sourceImage, BackgroundWorker worker)
        {
            Bitmap resultImage = new Bitmap(sourceImage.Width, sourceImage.Height);
            for (int i = 0; i < sourceImage.Width; i++)
            {
                worker.ReportProgress((int)((float)i / resultImage.Width * 100));
                if (worker.CancellationPending)
                    return null;
                for (int j = 0; j < sourceImage.Height; j++)
                {
                    resultImage.SetPixel(i, j, calculateNewPixelColor(sourceImage, i, j));
                }
            }
            return resultImage;
        }

        public int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

    }

    abstract class Morfology : MatrixFilters
    {
        public Morfology()
        {
            kernel = new float[,] { { 0, 1, 0 }, { 1, 1, 1 }, { 0, 1, 0 } };
        }
        public Morfology(float[,] struct_elem)
        {
            kernel = struct_elem;
        }
    }

    class dilation : Morfology
    {
        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            int radiusX = kernel.GetLength(0) / 2;
            int radiusY = kernel.GetLength(1) / 2;

            Color resultColor = Color.Black;

            byte max = 0;
            for (int l = -radiusY; l <= radiusY; l++)
                for (int k = -radiusX; k <= radiusX; k++)
                {
                    int idX = Clamp(x + k, 0, sourceImage.Width - 1);
                    int idY = Clamp(y + l, 0, sourceImage.Height - 1);
                    Color color = sourceImage.GetPixel(idX, idY);
                    int intensity = color.R;
                    if ((color.R != color.G) || (color.R != color.B) || (color.G != color.B))
                    {
                        intensity = (int)(0.36 * color.R + 0.53 * color.G + 0.11 * color.B);
                    }
                    if (kernel[k + radiusX, l + radiusY] > 0 && intensity > max)
                    {
                        max = (byte)intensity;
                        resultColor = color;
                    }
                }
            return resultColor;
        }
    }

    class erosion : Morfology
    {
        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            int radiusX = kernel.GetLength(0) / 2;
            int radiusY = kernel.GetLength(1) / 2;

            Color resultColor = Color.White;

            byte min = 255;
            for (int l = -radiusY; l <= radiusY; l++)
                for (int k = -radiusX; k <= radiusX; k++)
                {
                    int idX = Clamp(x + k, 0, sourceImage.Width - 1);
                    int idY = Clamp(y + l, 0, sourceImage.Height - 1);
                    Color color = sourceImage.GetPixel(idX, idY);
                    int intensity = color.R;
                    if ((color.R != color.G) || (color.R != color.B) || (color.G != color.B))
                    {
                        intensity = (int)(0.36 * color.R + 0.53 * color.G + 0.11 * color.B);
                    }
                    if (kernel[k + radiusX, l + radiusY] > 0 && intensity < min)
                    {
                        min = (byte)intensity;
                        resultColor = color;
                    }
                }
            return resultColor;
        }
    }

    class opening : Morfology
    {
        public new Bitmap processImage(Bitmap sourceImage, BackgroundWorker worker)
        {
            dilation dilat;
            erosion eros;

            dilat = new dilation();
            eros = new erosion();
            return dilat.processImage(eros.processImage(sourceImage, worker), worker);
        }
    }

    class closing : Morfology
    {
        public new Bitmap processImage(Bitmap sourceImage, BackgroundWorker worker)
        {
            erosion eros;
            dilation dilat;
          

            eros = new erosion();
            dilat = new dilation();
          

            return eros.processImage(dilat.processImage(sourceImage, worker), worker);
        }
    }

    class subtract : Filters
    {
        Bitmap MinuendImage = null;

        public subtract()
        {
        }

        public subtract(Bitmap minuendImage)
        {
            MinuendImage = minuendImage;
        }
        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            Color MinuendColor = MinuendImage.GetPixel(x, y);
            Color SubtractColor = sourceImage.GetPixel(x, y);
            return Color.FromArgb(Clamp(MinuendColor.R - SubtractColor.R, 0, 255),
            Clamp(MinuendColor.G - SubtractColor.G, 0, 255),
            Clamp(MinuendColor.B - SubtractColor.B, 0, 255));
        }
    }

    class top_hat : Morfology
    {
        public top_hat()
        {
            kernel = null;
        }
        public top_hat(float[,] kernel)
        {
            this.kernel = kernel;
        }
        public new Bitmap processImage(Bitmap sourceImage, BackgroundWorker worker)
        {
            closing close;
            //if (this.kernel == null) 
            //{ 
            close = new closing();
            //} 
            //else 
            //{ 
            // close = new closing(kernel); 
            //} 
            subtract subtraction = new subtract(sourceImage);
            return subtraction.processImage(close.processImage(sourceImage, worker), worker);
        }
    }


class InvertFilter : Filters
    {
        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            Color sourceColor = sourceImage.GetPixel(x, y);
            Color resultColor = Color.FromArgb(255 - sourceColor.R,
                                                 255 - sourceColor.G,
                                                 255 - sourceColor.B);
            return resultColor;
        }

    }

    class GrayScaleFilter : Filters
    {
        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            Color sourceColor = sourceImage.GetPixel(x, y);
            double Intensity = 0.36 * sourceColor.R + 0.53 * sourceColor.G + 0.11 * sourceColor.B;
            Color resultColor = Color.FromArgb((int)Intensity,
                                                (int)Intensity,
                                                (int)Intensity);
            return resultColor;
        }
    }

    class Sepia : Filters
    {
        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            Color sourceColor = sourceImage.GetPixel(x, y);
            double Intensity = 0.36 * sourceColor.R + 0.53 * sourceColor.G + 0.11 * sourceColor.B;
            int k = 4;
            Color resultColor = Color.FromArgb(Clamp(((int)Intensity + 2 * k), 0, 255),
                                  Clamp((int)(Intensity + 0.5 * k), 0, 255),
                                              Clamp(((int)Intensity - 1 * k), 0, 255));
            return resultColor;
        }
    }

    class Bright : Filters
    {
        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            Color sourceColor = sourceImage.GetPixel(x, y);
            Color resultColor = Color.FromArgb(Clamp(sourceColor.R + 25, 0, 255),
                                                 Clamp(sourceColor.G + 40, 0, 255),
                                                 Clamp(sourceColor.B + 60, 0, 255));
            return resultColor;
        }
    }

    class MatrixFilters : Filters
    {
        protected float[,] kernel = null;
        protected MatrixFilters() { }
        public MatrixFilters(float[,] kernel)
        {
            this.kernel = kernel;
        }

        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            int radiusX = kernel.GetLength(0) / 2;
            int radiusY = kernel.GetLength(1) / 2;
            float resultR = 0;
            float resultG = 0;
            float resultB = 0;
            for (int l = -radiusY; l <= radiusY; l++)
            {
                for (int k = -radiusX; k <= radiusX; k++)
                {
                    int idX = Clamp(x + k, 0, sourceImage.Width - 1);  //пиксели - соседи
                    int idY = Clamp(y + l, 0, sourceImage.Height - 1);
                    Color neighborColor = sourceImage.GetPixel(idX, idY);
                    resultR += neighborColor.R * kernel[k + radiusX, l + radiusY];
                    resultG += neighborColor.G * kernel[k + radiusX, l + radiusY];
                    resultB += neighborColor.B * kernel[k + radiusX, l + radiusY];
                }
            }
            return Color.FromArgb(Clamp((int)resultR, 0, 255),
                Clamp((int)resultG, 0, 255),
                Clamp((int)resultB, 0, 255));
        }
    }

    class BlurFilter : MatrixFilters
    {
        public BlurFilter()
        {
            int sizeX = 3;
            int sizeY = 3;
            kernel = new float[sizeX, sizeY];
            for (int i = 0; i < sizeX; i++)
            {
                for (int j = 0; j < sizeY; j++)
                {
                    kernel[i, j] = 1.0f / (float)(sizeX * sizeY);
                }
            }
        }
    }

    class GaussianFilter : MatrixFilters
    {
        public GaussianFilter()
        {
            createGaussianKernel(3, 2);
        }
        public void createGaussianKernel(int radius, float sigma)
        {
            int size = 2 * radius + 1;
            kernel = new float[size, size];
            float norm = 0;
            for (int i = -radius; i <= radius; i++)
            {
                for (int j = -radius; j <= radius; j++)
                {
                    kernel[i + radius, j + radius] = (float)(Math.Exp(-(i * i + j * j) / (sigma * sigma)));
                    norm += kernel[i + radius, j + radius];
                }
            }
            //нормировка ядра
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    kernel[i, j] /= norm;
                }
            }
        }
    }

    class HarshnessFilter : MatrixFilters
    {
        public HarshnessFilter()
        {
            kernel = new float[3, 3] { { -1, -1, -1 }, { -1, 9, -1 }, { -1, -1, -1 } };

        }
    }

    class SobelOperator : MatrixFilters
    {
        public SobelOperator()
        {
        }

        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {

            float resultR1 = 0;
            float resultG1 = 0;
            float resultB1 = 0;
            float resultR2 = 0;
            float resultG2 = 0;
            float resultB2 = 0;

            kernel = new float[3, 3] { { -1, -2, -1 }, { 0, 0, 0 }, { 1, 2, 1 } };
            int radiusX = kernel.GetLength(0) / 2;
            int radiusY = kernel.GetLength(1) / 2;

            for (int l = -radiusY; l <= radiusY; l++)
                for (int k = -radiusX; k <= radiusX; k++)
                {
                    int idX = Clamp(x + k, 0, sourceImage.Width - 1);  //пиксели - соседи
                    int idY = Clamp(y + l, 0, sourceImage.Height - 1);
                    Color neighborColor = sourceImage.GetPixel(idX, idY);
                    resultR1 += neighborColor.R * kernel[k + radiusX, l + radiusY];
                    resultG1 += neighborColor.G * kernel[k + radiusX, l + radiusY];
                    resultB1 += neighborColor.B * kernel[k + radiusX, l + radiusY];
                }

            kernel = new float[3, 3] { { -1, 0, 1 }, { -2, 0, 2 }, { -1, 0, 1 } };

            for (int l = -radiusY; l <= radiusY; l++)
                for (int k = -radiusX; k <= radiusX; k++)
                {
                    int idX = Clamp(x + k, 0, sourceImage.Width - 1);  //пиксели - соседи
                    int idY = Clamp(y + l, 0, sourceImage.Height - 1);
                    Color neighborColor = sourceImage.GetPixel(idX, idY);
                    resultR2 += neighborColor.R * kernel[k + radiusX, l + radiusY];
                    resultB2 += neighborColor.G * kernel[k + radiusX, l + radiusY];
                    resultG2 += neighborColor.B * kernel[k + radiusX, l + radiusY];
                }

            int sum = ((int)(Math.Sqrt(resultR1 * resultR1 + resultR2 * resultR2)) + (int)(Math.Sqrt(resultG1 * resultG1 + resultG2 * resultG2))
            + (int)(Math.Sqrt(resultB1 * resultB1 + resultB2 * resultB2))) / 3;

            return Color.FromArgb(Clamp((int)sum, 0, 255),
                Clamp((int)sum, 0, 255),
                Clamp((int)sum, 0, 255));
        }
    }

    class ScharOperator : MatrixFilters
    {
        public ScharOperator()
        {
        }

        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {

            float resultR1 = 0;
            float resultG1 = 0;
            float resultB1 = 0;
            float resultR2 = 0;
            float resultG2 = 0;
            float resultB2 = 0;

            kernel = new float[3, 3] { { 3, 10, 3 }, { 0, 0, 0 }, { -3, -10, -3 } };
            int radiusX = kernel.GetLength(0) / 2;
            int radiusY = kernel.GetLength(1) / 2;

            for (int l = -radiusY; l <= radiusY; l++)
                for (int k = -radiusX; k <= radiusX; k++)
                {
                    int idX = Clamp(x + k, 0, sourceImage.Width - 1);  //пиксели - соседи
                    int idY = Clamp(y + l, 0, sourceImage.Height - 1);
                    Color neighborColor = sourceImage.GetPixel(idX, idY);
                    resultR1 += neighborColor.R * kernel[k + radiusX, l + radiusY];
                    resultG1 += neighborColor.G * kernel[k + radiusX, l + radiusY];
                    resultB1 += neighborColor.B * kernel[k + radiusX, l + radiusY];
                }

            kernel = new float[3, 3] { { 3, 0, -3 }, { 10, 0, -10 }, { 3, 0, -3 } };

            for (int l = -radiusY; l <= radiusY; l++)
                for (int k = -radiusX; k <= radiusX; k++)
                {
                    int idX = Clamp(x + k, 0, sourceImage.Width - 1);  //пиксели - соседи
                    int idY = Clamp(y + l, 0, sourceImage.Height - 1);
                    Color neighborColor = sourceImage.GetPixel(idX, idY);
                    resultR2 += neighborColor.R * kernel[k + radiusX, l + radiusY];
                    resultB2 += neighborColor.G * kernel[k + radiusX, l + radiusY];
                    resultG2 += neighborColor.B * kernel[k + radiusX, l + radiusY];
                }

            int sum = ((int)(Math.Sqrt(resultR1 * resultR1 + resultR2 * resultR2)) + (int)(Math.Sqrt(resultG1 * resultG1 + resultG2 * resultG2))
            + (int)(Math.Sqrt(resultB1 * resultB1 + resultB2 * resultB2))) / 3;

            return Color.FromArgb(Clamp((int)sum, 0, 255),
                Clamp((int)sum, 0, 255),
                Clamp((int)sum, 0, 255));
        }
    }

    class PruittOperator : MatrixFilters
    {
        public PruittOperator()
        {
        }

        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {

            float resultR1 = 0;
            float resultG1 = 0;
            float resultB1 = 0;
            float resultR2 = 0;
            float resultG2 = 0;
            float resultB2 = 0;

            kernel = new float[3, 3] { { -1, -1, -1 }, { 0, 0, 0 }, { 1, 1, 1 } };
            int radiusX = kernel.GetLength(0) / 2;
            int radiusY = kernel.GetLength(1) / 2;

            for (int l = -radiusY; l <= radiusY; l++)
                for (int k = -radiusX; k <= radiusX; k++)
                {
                    int idX = Clamp(x + k, 0, sourceImage.Width - 1);  //пиксели - соседи
                    int idY = Clamp(y + l, 0, sourceImage.Height - 1);
                    Color neighborColor = sourceImage.GetPixel(idX, idY);
                    resultR1 += neighborColor.R * kernel[k + radiusX, l + radiusY];
                    resultG1 += neighborColor.G * kernel[k + radiusX, l + radiusY];
                    resultB1 += neighborColor.B * kernel[k + radiusX, l + radiusY];
                }

            kernel = new float[3, 3] { { -1, 0, 1 }, { -1, 0, 1 }, { -1, 0, 1 } };

            for (int l = -radiusY; l <= radiusY; l++)
                for (int k = -radiusX; k <= radiusX; k++)
                {
                    int idX = Clamp(x + k, 0, sourceImage.Width - 1);  //пиксели - соседи
                    int idY = Clamp(y + l, 0, sourceImage.Height - 1);
                    Color neighborColor = sourceImage.GetPixel(idX, idY);
                    resultR2 += neighborColor.R * kernel[k + radiusX, l + radiusY];
                    resultB2 += neighborColor.G * kernel[k + radiusX, l + radiusY];
                    resultG2 += neighborColor.B * kernel[k + radiusX, l + radiusY];
                }

            int sum = ((int)(Math.Sqrt(resultR1 * resultR1 + resultR2 * resultR2)) + (int)(Math.Sqrt(resultG1 * resultG1 + resultG2 * resultG2))
            + (int)(Math.Sqrt(resultB1 * resultB1 + resultB2 * resultB2))) / 3;

            return Color.FromArgb(Clamp((int)sum, 0, 255),
                Clamp((int)sum, 0, 255),
                Clamp((int)sum, 0, 255));
        }
    }

    class MotionBlur : MatrixFilters
    {
        public MotionBlur()
        {
            int size = 11;
            kernel = new float[size, size];
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    if (i == j) kernel[i, j] = 1 / (float)size;
                    else kernel[i, j] = 0;
                }
            }
        }
    }

    class RotationFilter : MatrixFilters
    {
        public RotationFilter() { }

        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            float resultR1 = 0;
            float resultG1 = 0;
            float resultB1 = 0;

            kernel = new float[3, 3] { { 0, 0, 0 }, { 0, 1, 0 }, { 0, 0, 0 } };
            int radiusX = kernel.GetLength(0) / 2;
            int radiusY = kernel.GetLength(1) / 2;

            for (int l = -radiusY; l <= radiusY; l++)
                for (int k = -radiusX; k <= radiusX; k++)
                {
                    int idX = Clamp(x + 50, 0, sourceImage.Width - 1);  //пиксели - соседи
                    int idY = Clamp(y, 0, sourceImage.Height - 1);
                    Color neighborColor = sourceImage.GetPixel(idX, idY);
                    resultR1 += neighborColor.R * kernel[k + radiusX, l + radiusY];
                    resultG1 += neighborColor.G * kernel[k + radiusX, l + radiusY];
                    resultB1 += neighborColor.B * kernel[k + radiusX, l + radiusY];
                }
            return Color.FromArgb(Clamp((int)resultR1, 0, 255),
                Clamp((int)resultG1, 0, 255),
                Clamp((int)resultB1, 0, 255));
        }
    }

    class GlassFilter : MatrixFilters
    {
        Random rand = new Random();

        public GlassFilter() { }

        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            float resultR1 = 0;
            float resultG1 = 0;
            float resultB1 = 0;

            kernel = new float[3, 3] { { 0, 0, 0 }, { 0, 1, 0 }, { 0, 0, 0 } };
            int radiusX = kernel.GetLength(0) / 2;
            int radiusY = kernel.GetLength(1) / 2;

            for (int l = -radiusY; l <= radiusY; l++)
                for (int k = -radiusX; k <= radiusX; k++)
                {
                    int idX = Clamp((int)(x + 10 * (rand.NextDouble() - 0.5)), 0, sourceImage.Width - 1);  //пиксели - соседи
                    int idY = Clamp((int)(y + 10 * (rand.NextDouble() - 0.5)), 0, sourceImage.Height - 1);
                    Color neighborColor = sourceImage.GetPixel(idX, idY);
                    resultR1 += neighborColor.R * kernel[k + radiusX, l + radiusY];
                    resultG1 += neighborColor.G * kernel[k + radiusX, l + radiusY];
                    resultB1 += neighborColor.B * kernel[k + radiusX, l + radiusY];
                }
            return Color.FromArgb(Clamp((int)resultR1, 0, 255),
                Clamp((int)resultG1, 0, 255),
                Clamp((int)resultB1, 0, 255));
        }

    }

    class WavesFilter : MatrixFilters
    {
        public WavesFilter() { }

        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            float resultR1 = 0;
            float resultG1 = 0;
            float resultB1 = 0;

            kernel = new float[3, 3] { { 0, 0, 0 }, { 0, 1, 0 }, { 0, 0, 0 } };
            int radiusX = kernel.GetLength(0) / 2;
            int radiusY = kernel.GetLength(1) / 2;

            for (int l = -radiusY; l <= radiusY; l++)
                for (int k = -radiusX; k <= radiusX; k++)
                {
                    int idX = Clamp((int)(x + 20 * Math.Sin((2 * Math.PI * y) / 60)), 0, sourceImage.Width - 1);
                    int idY = Clamp(y, 0, sourceImage.Height - 1);
                    Color neighborColor = sourceImage.GetPixel(idX, idY);
                    resultR1 += neighborColor.R * kernel[k + radiusX, l + radiusY];
                    resultG1 += neighborColor.G * kernel[k + radiusX, l + radiusY];
                    resultB1 += neighborColor.B * kernel[k + radiusX, l + radiusY];
                }
            return Color.FromArgb(Clamp((int)resultR1, 0, 255),
                Clamp((int)resultG1, 0, 255),
                Clamp((int)resultB1, 0, 255));
        }
    }

    class WavesFilter2 : MatrixFilters
    {
        public WavesFilter2() { }

        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            float resultR1 = 0;
            float resultG1 = 0;
            float resultB1 = 0;

            kernel = new float[3, 3] { { 0, 0, 0 }, { 0, 1, 0 }, { 0, 0, 0 } };
            int radiusX = kernel.GetLength(0) / 2;
            int radiusY = kernel.GetLength(1) / 2;

            for (int l = -radiusY; l <= radiusY; l++)
                for (int k = -radiusX; k <= radiusX; k++)
                {
                    int idX = Clamp((int)(x + 20 * Math.Sin((2 * Math.PI * x) / 30)), 0, sourceImage.Width - 1);
                    int idY = Clamp(y, 0, sourceImage.Height - 1);
                    Color neighborColor = sourceImage.GetPixel(idX, idY);
                    resultR1 += neighborColor.R * kernel[k + radiusX, l + radiusY];
                    resultG1 += neighborColor.G * kernel[k + radiusX, l + radiusY];
                    resultB1 += neighborColor.B * kernel[k + radiusX, l + radiusY];
                }
            return Color.FromArgb(Clamp((int)resultR1, 0, 255),
                Clamp((int)resultG1, 0, 255),
                Clamp((int)resultB1, 0, 255));
        }
    }

    class TwistFilter : MatrixFilters
    {
        public TwistFilter() { }

        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            float resultR1 = 0;
            float resultG1 = 0;
            float resultB1 = 0;
            int x0 = sourceImage.Width / 2;
            int y0 = sourceImage.Height / 2;
            kernel = new float[3, 3] { { 0, 0, 0 }, { 0, 1, 0 }, { 0, 0, 0 } };
            int radiusX = kernel.GetLength(0) / 2;
            int radiusY = kernel.GetLength(1) / 2;

            for (int l = -radiusY; l <= radiusY; l++)
                for (int k = -radiusX; k <= radiusX; k++)
                {
                    int idX = Clamp((int)((x - x0) * Math.Cos(45) - (y - y0) * Math.Sin(45) + x0), 0, sourceImage.Width - 1);
                    int idY = Clamp((int)((x - x0) * Math.Sin(45) + (y - y0) * Math.Cos(45) + y0), 0, sourceImage.Height - 1);
                    Color neighborColor = sourceImage.GetPixel(idX, idY);
                    resultR1 += neighborColor.R * kernel[k + radiusX, l + radiusY];
                    resultG1 += neighborColor.G * kernel[k + radiusX, l + radiusY];
                    resultB1 += neighborColor.B * kernel[k + radiusX, l + radiusY];
                }
            return Color.FromArgb(Clamp((int)resultR1, 0, 255),
                Clamp((int)resultG1, 0, 255),
                Clamp((int)resultB1, 0, 255));
        }
    }

    class EmbossingFilter : MatrixFilters
    {
        public EmbossingFilter()
        {
            kernel = new float[3, 3]{
             {0,1,0},
             {1,0,-1},
             {0,-1,0}
        };
        }

        protected override Color calculateNewPixelColor(Bitmap im, int x, int y)
        {
            float resultR1 = 0;
            float resultG1 = 0;
            float resultB1 = 0;
            int radiusX = kernel.GetLength(0) / 2;
            int radiusY = kernel.GetLength(1) / 2;
            for (int l = -radiusY; l <= radiusY; l++)
                for (int k = -radiusX; k <= radiusX; k++)
                {
                    int idX = Clamp(x + k, 0, im.Width - 1);
                    int idY = Clamp(y + l, 0, im.Height - 1);
                    Color neighborColor = im.GetPixel(idX, idY);
                    resultR1 += neighborColor.R * kernel[k + radiusX, l + radiusY];
                    resultG1 += neighborColor.G * kernel[k + radiusX, l + radiusY];
                    resultB1 += neighborColor.B * kernel[k + radiusX, l + radiusY];
                }

            return Color.FromArgb(
               Clamp((int)((resultR1 + 128) * 0.36f + (resultG1 + 128) * 0.53f + (resultB1 + 128) * 0.11f), 0, 255),
               Clamp((int)((resultR1 + 128) * 0.36f + (resultG1 + 128) * 0.53f + (resultB1 + 128) * 0.11f), 0, 255),
               Clamp((int)((resultR1 + 128) * 0.36f + (resultG1 + 128) * 0.53f + (resultB1 + 128) * 0.11f), 0, 255));
        }
    }

    class MedianFilter : MatrixFilters
    {
        public MedianFilter()
        { }

        double Intensity(Color rgb)
        {
            double needR = rgb.R;
            double needG = rgb.G;
            double needB = rgb.B;
            return (rgb.R + rgb.G + rgb.B) / 3;
        }



        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            //   kernel = new float[3, 3] { { 0, 0, 0 }, { 0, 1, 0 }, { 0, 0, 0 } };
            // int radiusX = kernel.GetLength(0) / 2;
            //int radiusY = kernel.GetLength(1) / 2;
            int radiusX = 5 / 2;
            int radiusY = 5 / 2;
            int t = 0;
            double[] mas1 = new double[25];
            double[] mas2 = new double[25];
            double[] mas3 = new double[25];

            for (int l = -radiusY; l <= radiusY; l++)
                for (int k = -radiusX; k <= radiusX; k++,t++)
                {

                    int idX = Clamp(x + k, 0, sourceImage.Width - 1);  //пиксели - соседи
                    int idY = Clamp(y + l, 0, sourceImage.Height - 1);
                    Color neighborColor = sourceImage.GetPixel(idX, idY);

                    mas1[t] = neighborColor.R;
                  
                    mas2[t] = neighborColor.G;

                    mas3[t] = neighborColor.B;
                
                }

            Array.Sort(mas1);
            Array.Sort(mas2);
            Array.Sort(mas3);

            return Color.FromArgb(
               Clamp((int)(mas1[13]), 0, 255),
               Clamp((int)(mas2[13]), 0, 255),
               Clamp((int)(mas3[13]), 0, 255));
        }
    }

    class WavesFilter12 : Filters
    {
        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            Color sourceColor = sourceImage.GetPixel(Clamp((int)(x + 20 * Math.Sin(2 * Math.PI * x / 30)), 0, sourceImage.Width - 1), y);


            int R = sourceColor.R;
            int G = sourceColor.G;
            int B = sourceColor.B;

            Color resultColor = Color.FromArgb(R, G, B);

            return resultColor;
        }
    }

    class WavesFilter11 : Filters
    {
        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            Color sourceColor = sourceImage.GetPixel(Clamp((int)(x + 20 * Math.Sin(2 * Math.PI * y / 60)), 0, sourceImage.Width - 1), y);


            int R = sourceColor.R;
            int G = sourceColor.G;
            int B = sourceColor.B;

            Color resultColor = Color.FromArgb(R, G, B);

            return resultColor;
        }
    }

    class GlassFilter1 : Filters
    {
        Random rand = new Random();

        public GlassFilter1() { }

        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {


            Color sourceColor = sourceImage.GetPixel(
                Clamp((int)(x + 10 * (rand.NextDouble() - 0.5)), 0, sourceImage.Width - 1),

                Clamp((int)(y + 10 * (rand.NextDouble() - 0.5)), 0, sourceImage.Height - 1)
                );


            int R = sourceColor.R;
            int G = sourceColor.G;
            int B = sourceColor.B;

            Color resultColor = Color.FromArgb(R, G, B);

            return resultColor;
        }
    }

    class RotationFilter1 : Filters
    {
        public RotationFilter1() { }

        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {


            Color sourceColor = sourceImage.GetPixel(
                Clamp(x + 50, 0, sourceImage.Width - 1),

                Clamp(y, 0, sourceImage.Height - 1)
                );


            int R = sourceColor.R;
            int G = sourceColor.G;
            int B = sourceColor.B;

            Color resultColor = Color.FromArgb(R, G, B);

            return resultColor;
        }
    }

    class TwistFilter1 : Filters
    {
        public TwistFilter1() { }

        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            int x0 = sourceImage.Width / 2;
            int y0 = sourceImage.Height / 2;

            Color sourceColor = sourceImage.GetPixel(
                Clamp((int)((x - x0) * Math.Cos(45) - (y - y0) * Math.Sin(45) + x0), 0, sourceImage.Width - 1),

                Clamp((int)((x - x0) * Math.Sin(45) + (y - y0) * Math.Cos(45) + y0), 0, sourceImage.Height - 1)
                );


            int R = sourceColor.R;
            int G = sourceColor.G;
            int B = sourceColor.B;

            Color resultColor = Color.FromArgb(R, G, B);

            return resultColor;
        }
    }

    class MaximusFilter : Filters
    {
        public MaximusFilter() { }

        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            Color sourceColor = sourceImage.GetPixel(x, y);
            Color resultColor;

            // RRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRR

            if ((sourceColor.R > sourceColor.G) && (sourceColor.R > sourceColor.B))
            {
                resultColor = Color.FromArgb(Clamp(sourceColor.R, 0, 255),
                                                    0,
                                                    0);
                return resultColor;
            }

            if ((sourceColor.R == sourceColor.G) && (sourceColor.R > sourceColor.B))
            {
                resultColor = Color.FromArgb(Clamp(sourceColor.R, 0, 255),
                                                    Clamp(sourceColor.G, 0, 255),
                                                    0);
                return resultColor;
            }

            if ((sourceColor.R == sourceColor.B) && (sourceColor.R > sourceColor.G))
            {
                resultColor = Color.FromArgb(Clamp(sourceColor.R, 0, 255),
                                                    0,
                                                    Clamp(sourceColor.B, 0, 255));
                return resultColor;
            }


            // GGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGG

            if ((sourceColor.G > sourceColor.R) && (sourceColor.G > sourceColor.B))
            {
                resultColor = Color.FromArgb(0,
                                                    Clamp(sourceColor.G, 0, 255),
                                                    0);
                return resultColor;
            }

            if ((sourceColor.G == sourceColor.B) && (sourceColor.G > sourceColor.R))
            {
                resultColor = Color.FromArgb(0,
                                                    Clamp(sourceColor.G, 0, 255),
                                                   Clamp(sourceColor.B, 0, 255));
                return resultColor;
            }


            // BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB

            if ((sourceColor.B > sourceColor.R) && (sourceColor.B > sourceColor.G))
            {
                resultColor = Color.FromArgb(0,
                                                    0,
                                                    Clamp(sourceColor.B, 0, 255));
                return resultColor;
            }
            else
                return resultColor = Color.FromArgb(Clamp(sourceColor.R, 0, 255),
                                                     Clamp(sourceColor.G, 0, 255),
                                                     Clamp(sourceColor.B, 0, 255)); ;

        }
    }

    class MinimumFilter : Filters
    {
        public MinimumFilter() { }

        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            Color sourceColor = sourceImage.GetPixel(x, y);
            Color resultColor;

            // RRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRRR

            if ((sourceColor.R < sourceColor.G) && (sourceColor.R < sourceColor.B))
            {
                resultColor = Color.FromArgb(Clamp(sourceColor.R, 0, 255),
                                                    0,
                                                    0);
                return resultColor;
            }

            if ((sourceColor.R == sourceColor.G) && (sourceColor.R < sourceColor.B))
            {
                resultColor = Color.FromArgb(Clamp(sourceColor.R, 0, 255),
                                                    Clamp(sourceColor.G, 0, 255),
                                                    0);
                return resultColor;
            }

            if ((sourceColor.R == sourceColor.B) && (sourceColor.R < sourceColor.G))
            {
                resultColor = Color.FromArgb(Clamp(sourceColor.R, 0, 255),
                                                    0,
                                                    Clamp(sourceColor.B, 0, 255));
                return resultColor;
            }


            // GGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGG

            if ((sourceColor.G < sourceColor.R) && (sourceColor.G < sourceColor.B))
            {
                resultColor = Color.FromArgb(0,
                                                    Clamp(sourceColor.G, 0, 255),
                                                    0);
                return resultColor;
            }

            if ((sourceColor.G == sourceColor.B) && (sourceColor.G < sourceColor.R))
            {
                resultColor = Color.FromArgb(0,
                                                    Clamp(sourceColor.G, 0, 255),
                                                   Clamp(sourceColor.B, 0, 255));
                return resultColor;
            }


            // BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB

            if ((sourceColor.B < sourceColor.R) && (sourceColor.B < sourceColor.G))
            {
                resultColor = Color.FromArgb(0,
                                                    0,
                                                    Clamp(sourceColor.B, 0, 255));
                return resultColor;
            }
            else
                return resultColor = Color.FromArgb(Clamp(sourceColor.R, 0, 255),
                                                     Clamp(sourceColor.G, 0, 255),
                                                     Clamp(sourceColor.B, 0, 255)); ;

        }


    }

    class Circuit : MatrixFilters
    {
        public Circuit()
        {

        }

        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            kernel = new float[3, 3] { { 1, 0, 1 }, { 0, -4, 0 }, { 1, 0, 1 } };
            int radiusX = kernel.GetLength(0) / 2;
            int radiusY = kernel.GetLength(1) / 2;
            float resultR = 0;
            float resultG = 0;
            float resultB = 0;
            for (int l = -radiusY; l <= radiusY; l++)
            {
                for (int k = -radiusX; k <= radiusX; k++)
                {
                    int idX = Clamp(x + k, 0, sourceImage.Width - 1);  //пиксели - соседи
                    int idY = Clamp(y + l, 0, sourceImage.Height - 1);
                    Color neighborColor = sourceImage.GetPixel(idX, idY);
                    resultR += neighborColor.R * kernel[k + radiusX, l + radiusY];
                    resultG += neighborColor.G * kernel[k + radiusX, l + radiusY];
                    resultB += neighborColor.B * kernel[k + radiusX, l + radiusY];
                }
            }
            return Color.FromArgb(Clamp((int)resultR, 0, 255),
                Clamp((int)resultG, 0, 255),
                Clamp((int)resultB, 0, 255));
        }
    }

    class Dilation : MatrixFilters
    {
        public Dilation() { }

        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {

            float resultR1 = 0;
            float resultG1 = 0;
            float resultB1 = 0;
           

            kernel = new float[3, 5] { { 1, 1, 1, 1, 1 }, { 1, 1, 1, 1, 1 }, { 1, 1, 1, 1, 1 } };
            int radiusX = kernel.GetLength(0) / 2;
            int radiusY = kernel.GetLength(1) / 2;

            for (int l = -radiusY; l <= radiusY; l++)
                for (int k = -radiusX; k <= radiusX; k++)
                {
                    int idX = Clamp(x + k, 0, sourceImage.Width - 1);  //пиксели - соседи
                    int idY = Clamp(y + l, 0, sourceImage.Height - 1);
                    Color neighborColor = sourceImage.GetPixel(idX, idY);
                    resultR1 += neighborColor.R * kernel[k + radiusX, l + radiusY];
                    resultG1 += neighborColor.G * kernel[k + radiusX, l + radiusY];
                    resultB1 += neighborColor.B * kernel[k + radiusX, l + radiusY];
                }

           

            return Color.FromArgb(Clamp((int)resultR1, 0, 255),
                Clamp((int)resultG1, 0, 255),
                Clamp((int)resultB1, 0, 255));
        }

    }





}










