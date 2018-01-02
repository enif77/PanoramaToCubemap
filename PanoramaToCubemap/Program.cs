/* (C) 2018 - Premysl Fara */

namespace PanoramaToCubemap
{
    using System;
    using System.IO;


    static class Program
    {
        static int Main(string[] args)
        {
            Console.WriteLine("Panorama to Cubemap v1.0");
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: ptc.exe input-image output-directory [output-jpeg-quality]");

                return 1;
            }

            var inputFileName = args[0];
            var outputDirectory = args[1];
            int quality = 100;
            if (args.Length > 2)
            {
                int.TryParse(args[2], out quality);

                if (quality < 0) quality = 0;
                else if (quality > 100) quality = 100;
            }

            var faces = new string[]
            {
                Converter.PositiveZOrientationFaceName,
                Converter.NegativeZOrientationFaceName,
                Converter.PositiveXOrientationFaceName,
                Converter.NegativeXOrientationFaceName,
                Converter.PositiveYOrientationFaceName,
                Converter.NegativeYOrientationFaceName
            };

            var converter = new Converter();

            try
            {
                Console.WriteLine("Loading image data from: {0}", inputFileName);

                var image = BitmapHelper.LoadRgba(inputFileName);

                Console.WriteLine("Creating the output directory: {0}", outputDirectory);

                Directory.CreateDirectory(outputDirectory);

                Console.WriteLine("Generating face images...");

                foreach (var faceName in faces)
                {
                    Console.Write("  {0}: ", faceName);

                    var faceImage = converter.RenderFace(image, faceName, 180.0f, "linear", 4096);

                    using (var bitmap = BitmapHelper.GetBitmapFromRgba(faceImage))
                    {
                        var path = Path.Combine(outputDirectory, faceName + ".jpeg");

                        Console.Write("'{0}'... ", path);

                        BitmapHelper.SaveBitmapAsJpeg(path, bitmap, quality);

                        Console.WriteLine("OK");
                    }
                }

                Console.WriteLine("DONE");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);

                return 1;
            }

            return 0;
        }
    }
}
