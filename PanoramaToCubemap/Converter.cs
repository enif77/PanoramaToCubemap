/* (C) 2018 - Premysl Fara 
 
 https://github.com/jaxry/panorama-to-cubemap
 
 */

namespace PanoramaToCubemap
{
    using System;


    /// <summary>
    /// A 3D vector.
    /// </summary>
    public class Vector3
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }


        public Vector3(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }


    /// <summary>
    /// Converts panoramatic 2:1 image with a equirectangular projection to a 6 cube maps.
    /// </summary>
    public class Converter
    {
        public const string PositiveZOrientationFaceName = "pz";
        public const string NegativeZOrientationFaceName = "nz";
        public const string PositiveXOrientationFaceName = "px";
        public const string NegativeXOrientationFaceName = "nx";
        public const string PositiveYOrientationFaceName = "py";
        public const string NegativeYOrientationFaceName = "ny";


        /// <summary>
        /// Renders a cube face from a 2:1 panoramatic image.
        /// </summary>
        /// <param name="readData">Source image data.</param>
        /// <param name="faceName">A name of a face to be rendered.</param>
        /// <param name="rotation">A cube rotation. 0 .. 359</param>
        /// <param name="interpolation">Which image interpolation should be used.</param>
        /// <param name="maxWidth">The maximum width of the generated image.</param>
        /// <returns>A image representing a cube face.</returns>
        public ImageData RenderFace(ImageData readData, string faceName, double rotation, string interpolation, int maxWidth = int.MaxValue)
        {
            var faceWidth = Math.Min(maxWidth, readData.Width / 4);
            var faceHeight = faceWidth;

            var writeData = new ImageData(faceWidth, faceHeight, 4);
            var cubeOrientation = GetOrientation(faceName);
            var copyPixel = GetCopyPixelDelegate(interpolation);

            for (var x = 0; x < faceWidth; x++)
            {
                for (var y = 0; y < faceHeight; y++)
                {
                    var to = 4 * (y * faceWidth + x);

                    // fill alpha channel
                    writeData.Data[to + 3] = 255;

                    // get position on cube face
                    // cube is centered at the origin with a side length of 2
                    var cube = cubeOrientation(2 * (x + 0.5) / faceWidth - 1, 2 * (y + 0.5) / faceHeight - 1);

                    // project cube face onto unit sphere by converting cartesian to spherical coordinates
                    var r = Math.Sqrt(cube.X * cube.X + cube.Y * cube.Y + cube.Z * cube.Z);
                    var lon = Mod(Math.Atan2(cube.Y, cube.X) + rotation, 2.0 * Math.PI);
                    var lat = Math.Acos(cube.Z / r);

                    copyPixel(readData, writeData, readData.Width * lon / Math.PI / 2 - 0.5, readData.Height * lat / Math.PI - 0.5, to);
                }
            }

            return writeData;
        }


        private double Clamp(double x, double min, double max)
        {
            return Math.Min(max, Math.Max(x, min));
        }


        private double Mod(double x, double n)
        {
            return ((x % n) + n) % n;
        }


        private int GetReadIndex(int x, int y, int width)
        {
            return 4 * (y * width + x);
        }


        private delegate void CopyPixelDelegate(ImageData read, ImageData write, double xFrom, double yFrom, int to);
        private delegate double KernelDelegate(double x);


        private CopyPixelDelegate GetCopyPixelDelegate(string interpolation)
        {
            switch (interpolation)
            {
                case "linear": return CopyPixelBilinear;
                case "cubic": return CopyPixelBicubic;
                case "lanczos": return CopyPixelLanczos;
                default: return CopyPixelNearest;
            }
        }


        private void CopyPixelNearest(ImageData read, ImageData write, double xFrom, double yFrom, int to)
        {
            var nearest = GetReadIndex(
                  (int)Clamp(Math.Round(xFrom), 0, read.Width - 1),
                  (int)Clamp(Math.Round(yFrom), 0, read.Height - 1),
                  read.Width);

            for (var channel = 0; channel < 3; channel++)
            {
                write.Data[to + channel] = read.Data[nearest + channel];
            }
        }


        private void CopyPixelBilinear(ImageData read, ImageData write, double xFrom, double yFrom, int to)
        {
            var xl = Clamp(Math.Floor(xFrom), 0, read.Width - 1);
            var xr = Clamp(Math.Ceiling(xFrom), 0, read.Width - 1);
            var xf = xFrom - xl;

            var yl = Clamp(Math.Floor(yFrom), 0, read.Height - 1);
            var yr = Clamp(Math.Ceiling(yFrom), 0, read.Height - 1);
            var yf = yFrom - yl;

            var p00 = GetReadIndex((int)xl, (int)yl, read.Width);
            var p10 = GetReadIndex((int)xr, (int)yl, read.Width);
            var p01 = GetReadIndex((int)xl, (int)yr, read.Width);
            var p11 = GetReadIndex((int)xr, (int)yr, read.Width);

            for (var channel = 0; channel < 3; channel++)
            {
                var p0 = read.Data[p00 + channel] * (1 - xf) + read.Data[p10 + channel] * xf;
                var p1 = read.Data[p01 + channel] * (1 - xf) + read.Data[p11 + channel] * xf;
                write.Data[to + channel] = (byte)Clamp(Math.Ceiling(p0 * (1 - yf) + p1 * yf), 0.0, 255.0);
            }
        }


        private void CopyPixelBicubic(ImageData read, ImageData write, double xFrom, double yFrom, int to)
        {
            KernelResample(read, write, xFrom, yFrom, to, 2, BicubicKernel);
        }


        private double BicubicKernel(double x)
        {
            var b = -0.5;
            var x1 = Math.Abs(x);
            var x2 = x1 * x1;
            var x3 = x1 * x1 * x1;

            return (x1 <= 1.0)
                ? (b + 2.0) * x3 - (b + 3.0) * x2 + 1.0
                : b * x3 - 5.0 * b * x2 + 8.0 * b * x1 - 4.0 * b;
        }


        private void CopyPixelLanczos(ImageData read, ImageData write, double xFrom, double yFrom, int to)
        {
            KernelResample(read, write, xFrom, yFrom, to, 5, LanczosKernel);
        }


        private double LanczosKernel(double x)
        {
            var filterSize = 5;
            if (x == 0)
            {
                return 1;
            }
            else
            {
                var xp = Math.PI * x;

                return filterSize * Math.Sin(xp) * Math.Sin(xp / filterSize) / (xp * xp);
            }
        }

        /// <summary>
        /// Performs a discrete convolution with a provided kernel.
        /// </summary>
        /// <param name="read">The input image.</param>
        /// <param name="write">The output image.</param>
        /// <param name="xFrom">The source pixel X position.</param>
        /// <param name="yFrom">The source pixel Y position.</param>
        /// <param name="to">Target index in the output image.</param>
        /// <param name="filterSize">The filter size.</param>
        /// <param name="kernel">The kernel/filter function.</param>
        private void KernelResample(ImageData read, ImageData write, double xFrom, double yFrom, int to, int filterSize, KernelDelegate kernel)
        {
            var twoFilterSize = 2 * filterSize;
            var xMax = read.Width - 1;
            var yMax = read.Height - 1;
            var xKernel = new double[twoFilterSize];
            var yKernel = new double[twoFilterSize];

            var xl = Math.Floor(xFrom);
            var yl = Math.Floor(yFrom);
            var xStart = xl - filterSize + 1;
            var yStart = yl - filterSize + 1;

            for (var i = 0; i < twoFilterSize; i++)
            {
                xKernel[i] = kernel(xFrom - (xStart + i));
                yKernel[i] = kernel(yFrom - (yStart + i));
            }

            for (var channel = 0; channel < 3; channel++)
            {
                var q = 0.0;

                for (var i = 0; i < twoFilterSize; i++)
                {
                    var y = yStart + i;
                    var yClamped = Clamp(y, 0, yMax);
                    var p = 0.0;
                    for (var j = 0; j < twoFilterSize; j++)
                    {
                        var x = xStart + j;
                        var index = GetReadIndex((int)Clamp(x, 0, xMax), (int)yClamped, read.Width);
                        p += read.Data[index + channel] * xKernel[j];

                    }

                    q += p * yKernel[i];
                }

                write.Data[to + channel] = (byte)Clamp(Math.Round(q), 0.0, 255.0);
            }
        }
        

        private delegate Vector3 CubeOrientationDelegate(double x, double y);


        private Vector3 GetPositiveZOrientation(double x, double y)
        {
            return new Vector3(-1, -x, -y);
        }


        private Vector3 GetNegativeZOrientation(double x, double y)
        {
            return new Vector3(1, x, -y);
        }


        private Vector3 GetPositiveXOrientation(double x, double y)
        {
            return new Vector3(x, -1, -y);
        }


        private Vector3 GetNegativeXOrientation(double x, double y)
        {
            return new Vector3(-x, 1, -y);
        }


        private Vector3 GetPositiveYOrientation(double x, double y)
        {
            return new Vector3(-y, -x, 1);
        }


        private Vector3 GetNegativeYOrientation(double x, double y)
        {
            return new Vector3(y, -x, -1);
        }


        private Vector3 GetUnknownOrientation(double x, double y)
        {
            return new Vector3(1, 1, 1);
        }


        private CubeOrientationDelegate GetOrientation(string faceName)
        {
            switch (faceName)
            {
                case PositiveZOrientationFaceName: return GetPositiveZOrientation;
                case NegativeZOrientationFaceName: return GetNegativeZOrientation;

                case PositiveXOrientationFaceName: return GetPositiveXOrientation;
                case NegativeXOrientationFaceName: return GetNegativeXOrientation;

                case PositiveYOrientationFaceName: return GetPositiveYOrientation;
                case NegativeYOrientationFaceName: return GetNegativeYOrientation;
            }

            return GetUnknownOrientation;
        }
    }
}
