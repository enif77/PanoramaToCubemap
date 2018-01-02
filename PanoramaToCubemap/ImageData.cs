/* (C) 2018 - Premysl Fara */

namespace PanoramaToCubemap
{
    /// <summary>
    /// Raw image
    /// </summary>
    public class ImageData
    {
        /// <summary>
        /// The Width of this image.
        /// </summary>
        public int Width { get; private set; }

        /// <summary>
        /// The height of this image.
        /// </summary>
        public int Height { get; private set; }

        /// <summary>
        /// The number of bytes per pixel.
        /// </summary>
        public int PixelSize { get { return Data.Length / (Width * Height); } }

        /// <summary>
        /// Is a one-dimensional array containing the data in (usually) the RGBA order, with integer values between 0 and 255 (included).
        /// </summary>
        public byte[] Data { get; private set; }


        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="width">A width of the new image.</param>
        /// <param name="height">A height of the new image.</param>
        /// <param name="pixelSize">A size of a pixel in bytes. It is 4 by default.</param>
        public ImageData(int width, int height, int pixelSize = 4)
        {
            Width = width;
            Height = height;
            Data = new byte[width * height * pixelSize];
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="width">A width of the new image.</param>
        /// <param name="height">A height of the new image.</param>
        /// <param name="pixels">An array containing the data in (usually) the RGBA order.</param>
        public ImageData(int width, int height, byte[] pixels)
        {
            Width = width;
            Height = height;
            Data = pixels;
        }
    }
}
