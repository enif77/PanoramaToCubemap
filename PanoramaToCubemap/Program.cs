/* (C) 2018 - Premysl Fara */

namespace PanoramaToCubemap
{
    using System;
    using System.IO;


    static class Program
    {
        private const string SourceArgName = "source=";
        private const string OutputDirectoryArgName = "output-directory=";
        private const string InterpolationArgName = "interpolation=";
        private const string OutputFormatArgName = "output-format=";
        private const string JpegQualityArgName = "jpeg-quality=";

        static int Main(string[] args)
        {
            Console.WriteLine("Panorama to Cubemap v1.0");
            if (args.Length < 2)
            {
                Usage();

                return 1;
            }

            var sourceFileName = (string)null;
            var outputDirectory = (string)null;
            var interpolation = "nearest";
            var outputFormat = "jpeg";
            var jpegQuality = 100;

            foreach (var arg in args)
            {
                if (arg.StartsWith(SourceArgName))
                {
                    sourceFileName = arg.Substring(SourceArgName.Length);
                }
                else if (arg.StartsWith(OutputDirectoryArgName))
                {
                    outputDirectory = arg.Substring(OutputDirectoryArgName.Length);
                }
                else if (arg.StartsWith(InterpolationArgName))
                {
                    interpolation = arg.Substring(InterpolationArgName.Length);

                    if (interpolation != "nearest" && interpolation != "linear" && interpolation != "cubic" && interpolation != "lanczos")
                    {
                        interpolation = "nearest";
                    }
                }
                else if (arg.StartsWith(OutputFormatArgName))
                {
                    outputFormat = arg.Substring(OutputFormatArgName.Length);

                    if (outputFormat != "jpeg" && outputFormat != "png")
                    {
                        outputFormat = "jpeg";
                    }
                }
                else if (arg.StartsWith(JpegQualityArgName))
                {
                    if (int.TryParse(arg.Substring(JpegQualityArgName.Length), out jpegQuality) == false)
                    {
                        jpegQuality = 100;
                    }

                    if (jpegQuality < 0) jpegQuality = 0;
                    else if (jpegQuality > 100) jpegQuality = 100;
                }
            }

            Console.WriteLine("  The source is: '{0}'", sourceFileName ?? "<NOT SET>");
            Console.WriteLine("  The output-directory is: '{0}'", outputDirectory ?? "<NOT SET>");
            Console.WriteLine("  The interpolation is: '{0}'", interpolation);
            Console.WriteLine("  The output-format is: '{0}'", outputFormat);
            Console.WriteLine("  The jpeg-quality is: '{0}'", jpegQuality);
            Console.WriteLine();

            if (string.IsNullOrWhiteSpace(sourceFileName) || string.IsNullOrWhiteSpace(outputDirectory))
            {
                Usage();

                return 1;
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
                Console.WriteLine("Loading image data from: {0}", sourceFileName);

                var image = BitmapHelper.LoadRgba(sourceFileName);

                Console.WriteLine("Creating the output directory: {0}", outputDirectory);

                Directory.CreateDirectory(outputDirectory);

                Console.WriteLine("Generating face images...");

                foreach (var faceName in faces)
                {
                    Console.Write("  {0}: ", faceName);

                    var faceImage = converter.RenderFace(image, faceName, 180.0f, interpolation, 4096);

                    using (var bitmap = BitmapHelper.GetBitmapFromRgba(faceImage))
                    {
                        var path = Path.Combine(outputDirectory, string.Format("{0}.{1}", faceName, outputFormat));

                        Console.Write("'{0}'... ", path);

                        if (outputFormat == "png")
                        {
                            BitmapHelper.SaveBitmapAsPng(path, bitmap);
                        }
                        else
                        {
                            BitmapHelper.SaveBitmapAsJpeg(path, bitmap, jpegQuality);
                        }
                        
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


        private static void Usage()
        {
            Console.WriteLine("Usage: ptc.exe source=an-input-image.* output-directory=an-output-directory-path [interpolation=nearest|linear|cubic|lanczos] [output-format=jpeg|png] [jpeg-quality=100]");
        }
    }
}
