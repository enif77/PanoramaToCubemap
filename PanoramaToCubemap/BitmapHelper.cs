/* (C) 2018 - Premysl Fara */

namespace PanoramaToCubemap
{
    using System;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.IO;
    using System.Runtime.InteropServices;


    /// <summary>
    /// Helper class for bitmaps.
    /// </summary>
    public static class BitmapHelper
    {
        /// <summary>
        /// Saves a bitmap into a JPEG file.
        /// </summary>
        /// <param name="fileName">A File name.</param>
        /// <param name="bitmap">A Bitmap instance.</param>
        /// <param name="quality">A JPEG quality. The default is 100.</param>
        /// <returns>True on success.</returns>
        public static bool SaveBitmapAsJpeg(string fileName, Bitmap bitmap, long quality = 100)
        {
            var jpgEncoder = GetEncoder(ImageFormat.Jpeg);
            if (jpgEncoder == null)
            {
                return false;
            }

            // Create an EncoderParameters object.  
            // An EncoderParameters object has an array of EncoderParameter  
            // objects. In this case, there is only one EncoderParameter object in the array.  
            var encoderParameters = new EncoderParameters(1);

            encoderParameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);

            bitmap.Save(fileName, jpgEncoder, encoderParameters);

            return true;


            // bmp.Save(fileName, System.Drawing.Imaging.ImageFormat.Jpeg);
            //bitmap.Save("file.bla", ImageFormat.Png);

        }

        /// <summary>
        /// Gets an image encoder for a given ImageFormat.
        /// </summary>
        /// <param name="format">An output image format.</param>
        /// <returns>An image encoder or null.</returns>
        private static ImageCodecInfo GetEncoder(ImageFormat format)
        {
            foreach (var codec in ImageCodecInfo.GetImageDecoders())
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }

            return null;
        }

        /// <summary>
        /// Loads an image into the RGBA array of bytes.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static ImageData LoadRgba(string fileName)
        {
            using (var bitmap = LoadBitmap(fileName))
            {
                var rgba = new byte[bitmap.Width * bitmap.Height * 4];

                GetRGBA(bitmap, rgba);

                return new ImageData(bitmap.Width, bitmap.Height, rgba);
            }
        }

        /// <summary>
        /// Loads a Bitmap from a file.
        /// </summary>
        /// <param name="fileName">A file name with a path.</param>
        /// <returns>A loaded Bitmap instance.</returns>
        public static Bitmap LoadBitmap(string fileName)
        {
            Bitmap bitmap;

            using (Stream bmpStream = File.Open(fileName, FileMode.Open))
            {
                bitmap = new Bitmap(Image.FromStream(bmpStream));
            }

            return bitmap;
        }

        /// <summary>
        /// Converts an ImageData instance to a Bitmap.
        /// </summary>
        /// <param name="image">An ImageData instance.</param>
        /// <returns>A Bitmap.</returns>
        public static Bitmap GetBitmapFromRgba(ImageData image)
        {
            return GetBitmapFromRgba(image.Data, image.Width, image.Height);
        }

        /// <summary>
        /// Converts a RGBA array of bytes to a Bitmap.
        /// </summary>
        /// <param name="rgbaArray">A RGBA array.</param>
        /// <param name="w">Width of a image.</param>
        /// <param name="h">Height of a image.</param>
        /// <returns>A Bitmap instance.</returns>
        public static Bitmap GetBitmapFromRgba(byte[] rgbaArray, int w, int h)
        {
            // create a bitmap and manipulate it
            var bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb);

            for (var y = 0; y < h; y++)
            {
                for (var x = 0; x < w; x++)
                {
                    var pixeloffset = (y * w + x) * 4;

                    bmp.SetPixel(x, y, Color.FromArgb(
                        rgbaArray[pixeloffset + 3],
                        rgbaArray[pixeloffset],
                        rgbaArray[pixeloffset + 1],
                        rgbaArray[pixeloffset + 2]));
                }
            }

            return bmp;
        }

        /*
        Bitmap foo = Bitmap.FromFile(@"somefile.jpg") as Bitmap;
        int[] rgbArray = new int[100];
        foo.getRGB(1, 1, 10, 10, rgbArray, 0, 10); 
        */

        /// <summary>
        /// Converts a Bitmap into a RGBA array of bytes format.
        /// </summary>
        /// <param name="image">A Bitmap instance.</param>
        /// <param name="rgbaArray">The output RGBA array.</param>
        public static void GetRGBA(Bitmap image, byte[] rgbaArray)
        {
            GetRGBA(image, 0, 0, image.Width, image.Height, rgbaArray, 0, image.Width * 4);
        }

        /// <summary>
        /// Converts a Bitmap into a RGBA array of bytes format.
        /// </summary>
        /// <param name="image">A Bitmap instance.</param>
        /// <param name="startX">Top left X of the extracted subimage.</param>
        /// <param name="startY">Top left Y of the extracted subimage.</param>
        /// <param name="w">The width of the extracted subimage..</param>
        /// <param name="h">The height of the extracted subimage.</param>
        /// <param name="rgbaArray">The output RGBA array.</param>
        /// <param name="offset">From which offset (in bytes) should be RGBA bytes written.</param>
        /// <param name="scansize">A size of a single image line in bytes. (Normally image.Width * 4 for RGBA.)</param>
        public static void GetRGBA(Bitmap image, int startX, int startY, int w, int h, byte[] rgbaArray, int offset, int scansize)
        {
            const int OutputPixelWidth = 4;
            const int InputPixelWidth = 3;

            if (image == null) throw new ArgumentNullException(nameof(image));
            if (rgbaArray == null) throw new ArgumentNullException(nameof(rgbaArray));
            if (startX < 0 || startX + w > image.Width) throw new ArgumentOutOfRangeException(nameof(startX));
            if (startY < 0 || startY + h > image.Height) throw new ArgumentOutOfRangeException(nameof(startY));
            if (w < 0 || w > scansize || w > image.Width) throw new ArgumentOutOfRangeException(nameof(w));
            if (h < 0 || (rgbaArray.Length < offset + h * scansize) || h > image.Height) throw new ArgumentOutOfRangeException(nameof(h));

            var data = image.LockBits(new Rectangle(startX, startY, w, h), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            try
            {
                var pixelData = new byte[data.Stride];
                for (int scanline = 0; scanline < data.Height; scanline++)
                {
                    Marshal.Copy(data.Scan0 + (scanline * data.Stride), pixelData, 0, data.Stride);
                    for (int pixeloffset = 0; pixeloffset < data.Width; pixeloffset++)
                    {
                        var inputByteOffset = pixeloffset * InputPixelWidth;
                        var outputByteOffset = offset + (scanline * scansize) + pixeloffset * OutputPixelWidth;

                        // PixelFormat.Format32bppRgb means the data is stored
                        // in memory as BGR. We want RGBA, so we must do some 
                        // bit-shuffling.
                        rgbaArray[outputByteOffset] = pixelData[inputByteOffset + 2];  // R
                        rgbaArray[outputByteOffset + 1] = pixelData[inputByteOffset + 1];  // G
                        rgbaArray[outputByteOffset + 2] = pixelData[inputByteOffset];      // B
                        rgbaArray[outputByteOffset + 3] = 255;                             // A

                        //rgbaArray[offset + (scanline * scansize) + pixeloffset] =
                        //    (pixelData[pixeloffset * PixelWidth + 2] << 24) +   // R 
                        //    (pixelData[pixeloffset * PixelWidth + 1] << 16) +   // G
                        //    (pixelData[pixeloffset * PixelWidth] << 8) +        // B
                        //    255;                                                // A
                    }
                }
            }
            finally
            {
                image.UnlockBits(data);
            }
        }


        //public byte[] ImageToByteArray(Image imageIn)
        //{
        //    using (var ms = new MemoryStream())
        //    {
        //        imageIn.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);

        //        return ms.ToArray();
        //    }
        //}

        //public Image byteArrayToImage(byte[] byteArrayIn)
        //{
        //    using (var ms = new MemoryStream(byteArrayIn))
        //    {
        //        return Image.FromStream(ms);
        //    }
        //}

        //public static byte[] converterDemo(Image x)
        //{
        //    var _imageConverter = new ImageConverter();
        //    return (byte[])_imageConverter.ConvertTo(x, typeof(byte[]));
        //}
    }
}
