/*
   Filename   : ImageProccessing.cs
   Author     : Alex Stone and Ben Moran
   Course     : CSCI 476
   Date       : 12/2/2021
   Assignment : Final Project
   Description: Takes in an image and modifies it or compresses it in parallel.
*/
/************************************************************/
// Using declaration
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

/************************************************************/
class ImageProcessing
{
    /************************************************************/
    static void Main(string[] args)
    {
        // Gets command line arguments
        if (args.Length >= 4)
        {
            int threads = Int32.Parse(args[0]);
            String filename = args[1];
            String newFilename = args[2];
            String mode = args[3];

            // For compression
            if (mode == "compress")
            {
                int compressionValue = Int32.Parse(args[4]);

                Stopwatch watch = new Stopwatch();
                watch.Start();
                compressImageParallel (
                    threads,
                    filename,
                    newFilename,
                    compressionValue
                );
                watch.Stop();
                long parallelTime = watch.ElapsedMilliseconds;

                // Console
                //     .WriteLine("Parallel time for compression: " +
                //     parallelTime +
                //     " ms\n");

                int index = newFilename.IndexOf(".");

                String serialNewFileName = newFilename.Substring(0, index);

                serialNewFileName += "Serial.jpg";

                watch.Reset();
                watch.Start();
                compressImageSerial (
                    filename,
                    serialNewFileName,
                    compressionValue
                );
                watch.Stop();
                long serialTime = watch.ElapsedMilliseconds;
                // Console
                //     .WriteLine("Serial Time time for compression: " +
                //     serialTime +
                //     " ms\n");

                // if (File.Exists(serialNewFileName))
                // {
                //     File.Delete (serialNewFileName);
                // }
            } // For black and white
            else if (mode == "baw")
            {
                Stopwatch watch = new Stopwatch();
                watch.Start();
                blackAndWhiteImageInParallel (threads, filename, newFilename);
                watch.Stop();
                long parallelTime = watch.ElapsedMilliseconds;

                Console
                    .WriteLine("Parallel time for compression: " +
                    parallelTime +
                    " ms\n");

                int index = newFilename.IndexOf(".");

                String serialNewFileName = newFilename.Substring(0, index);

                serialNewFileName += "Serial.jpg";
                watch.Reset();
                watch.Start();
                blackAndWhiteImageSerial (filename, serialNewFileName);
                long serialTime = watch.ElapsedMilliseconds;
                Console
                    .WriteLine("Serial Time time for compression: " +
                    serialTime +
                    " ms\n");

                if (File.Exists(serialNewFileName))
                {
                    File.Delete (serialNewFileName);
                }
            }
            else if (mode == "cursed")
            {
                Stopwatch watch = new Stopwatch();
                watch.Start();
                blackAndWhiteImageInParallel (threads, filename, newFilename);
                watch.Stop();
                long parallelTime = watch.ElapsedMilliseconds;

                Console
                    .WriteLine("Parallel time for compression: " +
                    parallelTime +
                    " ms\n");

                int index = newFilename.IndexOf(".");

                String serialNewFileName = newFilename.Substring(0, index);

                serialNewFileName += "Serial.jpg";
                watch.Reset();
                watch.Start();
                curesedImageSerial (filename, serialNewFileName);
                long serialTime = watch.ElapsedMilliseconds;
                Console
                    .WriteLine("Serial Time time for compression: " +
                    serialTime +
                    " ms\n");

                // if (File.Exists(serialNewFileName))
                // {
                //     File.Delete (serialNewFileName);
                // }
            }
        }
        else
        {
            // Help messave
            Console.WriteLine("No command line arguments passed.");
            Console.WriteLine("Arg0 = threads");
            Console.WriteLine("Arg1 = filename");
            Console.WriteLine("Arg2 = new fileanme");
            Console.WriteLine("Arg3 = Mode (compress/modify)");
        }
    }

    /************************************************************/
    // Takes a the number of threads (must be an even number), a filename for the original image,
    // and a newfile name for the compressed image
    public static void compressImageParallel(
        int threads,
        String filename,
        String newFilename,
        int compressionValue
    )
    {
        // Image bitmap
        Bitmap bmp = new Bitmap(filename);

        // Original height and width.
        int totalWidth = bmp.Width;
        int totalHeight = bmp.Height;

        // threads passed to partitioning algorithm.
        int numOfColumns = threads / 2;

        // Get the rectangles needed to compress the image.
        Rectangle[] partitionedRectangles = new Rectangle[threads];
        partitionImageAndMakeRectangles(ref bmp,
        threads,
        ref partitionedRectangles);

        // Final bitmap to have lines drawn on it.
        Bitmap finalBitmap = new Bitmap(totalWidth, totalHeight);

        // Set the graphics object to be the bitmap we will draw onto.
        Graphics g = Graphics.FromImage(finalBitmap);

        // Countdown event so we can tell when the threads are done.
        CountdownEvent cntEvent = new CountdownEvent(threads);

        // Go through all the rectangles, compressing and drawing them back on the bitmap.

        Stopwatch watch = new Stopwatch();
        watch.Start();
        int id = 0;
        foreach (var rec in partitionedRectangles)
        {
            var data = new ThreadData(rec, ref g, in bmp, compressionValue, ref cntEvent, id);
            ThreadPool.QueueUserWorkItem(s => compressRectangleAndDraw(s), data);
            ++id;
        }
        

        // Wait until all threads are done.
        cntEvent.Wait();



        // Create and save the final bitmap.
        finalBitmap.Save(newFilename, System.Drawing.Imaging.ImageFormat.Jpeg);
                watch.Stop();
        long parTime = watch.ElapsedMilliseconds;
        Console.WriteLine("New Parallel time Time time for compression: " + parTime + " ms\n\n");
    }

    public static void flipImageParallel(
        int threads,
        String filename,
        String newFilename,
        int compressionValue
    )
    {
        // Image bitmap
        Bitmap bmp = new Bitmap(filename);

        // Original height and width.
        int totalWidth = bmp.Width;
        int totalHeight = bmp.Height;

        // threads passed to partitioning algorithm.
        int numOfColumns = threads / 2;

        // Get the rectangles needed to compress the image.
        Rectangle[] partitionedRectangles = new Rectangle[threads];
        partitionImageAndMakeRectangles(ref bmp,
        threads,
        ref partitionedRectangles);

        // Final bitmap to have lines drawn on it.
        Bitmap finalBitmap = new Bitmap(totalWidth, totalHeight);

        // Set the graphics object to be the bitmap we will draw onto.
        Graphics g = Graphics.FromImage(finalBitmap);

        // Countdown event so we can tell when the threads are done.
        CountdownEvent cntEvent = new CountdownEvent(threads);

        // Go through all the rectangles, compressing and drawing them back on the bitmap.

        // int id;
        // foreach (var rec in partitionedRectangles)
        // {
        //     ThreadPool
        //         .QueueUserWorkItem(stat =>
        //             compressRectangleAndDraw(rec,
        //             ref g,
        //             ref bmp,
        //             compressionValue,
        //             ref cntEvent,
        //             ref id));
        // }

        // Wait until all threads are done.
        // cntEvent.Wait();

        // Create and save the final bitmap.
        finalBitmap.Save(newFilename, System.Drawing.Imaging.ImageFormat.Jpeg);
    }

    /************************************************************/
    public static void partitionImageAndMakeRectangles(
        ref Bitmap bmp,
        int threads,
        ref Rectangle[] partitionedRectangles
    )
    {
        int numOfColumns = threads / 2;

        // The list of partitioned values.
        List<int>[] partitionIValues = new List<int>[numOfColumns];
        List<int>[] partitionJValues = new List<int>[2];
        partitionImage(ref bmp,
        threads,
        ref partitionIValues,
        ref partitionJValues);

        // Image dimensions.
        int imageWidth = bmp.Width;
        int imageHeight = bmp.Height;

        // Go thro
        int count = 0;
        foreach (var iList in partitionIValues)
        {
            foreach (var jlist in partitionJValues)
            {
                List<int> recValues = new List<int>();
                int iStart = iList[0];
                int iEnd = iList[1];
                int jStart = jlist[0];
                int jEnd = jlist[1];

                int x = iStart;
                int y = jStart;
                int width = iEnd - iStart;
                int height = jEnd - jStart;

                Rectangle rec = new Rectangle(x, y, width, height);

                partitionedRectangles[count] = rec;
                ++count;
            }
        }
    }
    public class ThreadData {
        public Rectangle rec;
        public readonly Graphics g;
        public readonly Bitmap bmp;
        public int compressionValue;
        public readonly CountdownEvent cntEvent;
        public int id;

        public ThreadData(Rectangle r, ref Graphics g, in Bitmap bitmap, int cv, ref CountdownEvent ce, int id) {
          this.rec = r;
          this.g = g;
          this.bmp = bitmap;
          this.compressionValue = cv;
          this.cntEvent = ce;
          this.id = id;
        }
    }

    /************************************************************/
    public static void compressRectangleAndDraw(object d)
    {
        ThreadData data = (ThreadData)d;
        var rec = data.rec;
        var g = data.g;
        var bmp = data.bmp;
        var compressionValue = data.compressionValue;
        var cntEvent = data.cntEvent;
        var id = data.id;

        Bitmap cloneBitmap = bmp.Clone(rec, bmp.PixelFormat);

        MemoryStream ms = CompressImage(cloneBitmap, compressionValue);
        var compressedImage = Image.FromStream(ms);

        lock (g)
        {
            // Draw the smaller rectangles on the bitmap
            g.DrawImage(compressedImage, new Point(rec.X, rec.Y));
        }

        cntEvent.Signal();
    }

    //     public static void flipRectangleAndDraw(
    //     Rectangle rec,
    //     ref Graphics g,
    //     ref Bitmap bmp,
    //     ref CountdownEvent cntEvent
    // )
    // {
    //     Bitmap cloneBitmap = bmp.Clone(rec, bmp.PixelFormat);
    //     MemoryStream ms = flip(cloneBitmap, compressionValue);
    //     var compressedImage = Image.FromStream(ms);
    //     lock (g)
    //     {
    //         // Draw the smaller rectangles on the bitmap
    //         g.DrawImage(compressedImage, new Point(rec.X, rec.Y));
    //     }
    //     cntEvent.Signal();
    // }
    /************************************************************/
    // Compressed the bitmap with the qualilty (0 - 100 inclusive).
    // Returns a memerory stream that the image is i so we can use it later.
    public static MemoryStream CompressImage(Bitmap bmp, int compressionValue)
    {
        // Get the encoder using our method.
        ImageCodecInfo jpgEncoder = GetEncoder(ImageFormat.Jpeg);

        // Get the quality of the encoder.
        System.Drawing.Imaging.Encoder QualityEncoder =
            System.Drawing.Imaging.Encoder.Quality;

        // Get an array of encoder objects. The 1 means it is of size one because we only need one encoder.
        EncoderParameters myEncoderParameters = new EncoderParameters(1);

        // Gets the encoder parameter with the specified quality.
        EncoderParameter myEncoderParameter =
            new EncoderParameter(QualityEncoder, compressionValue);

        // Sets the encoder parameter.
        myEncoderParameters.Param[0] = myEncoderParameter;

        // Save the bitmap to the memory stream so it gets compressed.
        var ms = new MemoryStream();
        bmp.Save (ms, jpgEncoder, myEncoderParameters);
        return ms;
    }

    /************************************************************/
    // Get the encoder of the specified format.
    // Returns an ImageCodeInfo object to be ussed in compression.
    private static ImageCodecInfo GetEncoder(ImageFormat format)
    {
        ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
        foreach (ImageCodecInfo codec in codecs)
        {
            if (codec.FormatID == format.Guid)
            {
                return codec;
            }
        }
        return null;
    }

    /************************************************************/
    // Takes an image and partions it using our partitioning formula using rectangles.
    // J/Height/Y will always be two to make it more parallelizable
    public static void partitionImage(
        ref Bitmap bmp,
        int threads,
        ref List<int>[] partitionIValues,
        ref List<int>[] partitionJValues
    )
    {
        int numOfColumns = threads / 2;

        // Image dimensions.
        int imageWidth = bmp.Width;
        int imageHeight = bmp.Height;

        for (int i = 0; i < numOfColumns; ++i)
        {
            // Calculate the I values using our algorithm.
            int myFirstI = (i * imageWidth) / (numOfColumns);
            int myLastI = ((i + 1) * imageWidth) / (numOfColumns);

            // Put the i values in the list.
            List<int> iValues = new List<int>();
            iValues.Add (myFirstI);
            iValues.Add (myLastI);
            partitionIValues[i] = iValues;
        }
        for (int j = 0; j < 2; ++j)
        {
            // Calculate the j values using our algorithm.
            int myFirstJ = (j * imageHeight) / (2);
            int myLastJ = ((j + 1) * imageHeight) / (2);

            // Place the j values in the list.
            List<int> jValues = new List<int>();
            jValues.Add (myFirstJ);
            jValues.Add (myLastJ);
            partitionJValues[j] = jValues;
        }
    }

    /************************************************************/
    // Gets the image and modifies it based on the method called
    private static void blackAndWhiteImageInParallel(
        int threads,
        string filename,
        String newFilename
    )
    {
        // New bitmap of the input image.
        Bitmap bmp = new Bitmap(filename);

        // threads passed to partitioning algorithm.
        int numOfColumns = threads / 2;

        // Get the rectangles needed to modify the image in.
        Rectangle[] partitionedRectangles = new Rectangle[threads];
        partitionImageAndMakeRectangles(ref bmp,
        threads,
        ref partitionedRectangles);

        // Count down event so we can see how many threads have been completed.
        CountdownEvent cntEvent = new CountdownEvent(threads);

        // Go through all the elements in the partioned list and apply the filter.
        foreach (var rec in partitionedRectangles)
        {
            // Get all the values for the bitmap.
            int iStart = rec.X;
            int iEnd = rec.Width + rec.X;
            int jStart = rec.Y;
            int jEnd = rec.Height + rec.Y;

            // Place the methods in the threadpool.
            ThreadPool
                .QueueUserWorkItem(state =>
                    setPixelBlackAndWhite(iStart,
                    iEnd,
                    jStart,
                    jEnd,
                    ref bmp,
                    ref cntEvent));
        }

        // Wiat for all threads to finish.
        cntEvent.Wait();

        // Save the bitmap to the new file name.
        bmp.Save (newFilename);
    }

    /************************************************************/
    // Changes a pixel color on the bitmap to black and write.
    public static void setPixelBlackAndWhite(
        int iStart,
        int iEnd,
        int jStart,
        int jEnd,
        ref Bitmap bmp,
        ref CountdownEvent cntEvent
    )
    {
        // Go through the part of the image and apply the grey image.
        for (int i = iStart; i < iEnd; ++i)
        {
            for (int j = jStart; j < jEnd; ++j)
            {
                Color c = bmp.GetPixel(i, j);

                //Apply conversion equation
                byte gray = (byte)(.21 * c.R + .71 * c.G + .071 * c.B);

                //Set the color of this pixel
                bmp.SetPixel(i, j, Color.FromArgb(gray, gray, gray));
            }
        }
        cntEvent.Signal();
    }

    /************************************************************/
    public static void setBrightness(
        int iStart,
        int iEnd,
        int jStart,
        int jEnd,
        ref Bitmap bmp,
        ref CountdownEvent cntEvent,
        int bri
    )
    {
        // Go through the part of the image and apply the grey image.
        for (int i = iStart; i < iEnd; ++i)
        {
            for (int j = jStart; j < jEnd; ++j)
            {
                Color c = bmp.GetPixel(i, j);

                //Apply conversion equation
                //byte gray = (byte)(.21 * c.R + .71 * c.G + .071 * c.B);
                //Set the color of this pixel
                // byte red = 0;
                // byte green = 0;
                // byte blue = 0;
                int red = 0;
                int green = 0;
                int blue = 0;
                int cR = (int) c.R;
                int cG = (int) c.G;
                int cB = (int) c.B;

                if (!(cR + bri > 255 || cR + bri < 0))
                {
                    red = cR + bri;
                }
                if (!(cG + bri > 255 || cG + bri < 0))
                {
                    green = cG + bri;
                }
                if (!(cB + bri > 255 || cB + bri < 0))
                {
                    blue = cB + bri;
                }
                bmp.SetPixel(i, j, Color.FromArgb(red, green, blue));
            }
        }
        cntEvent.Signal();
    }

    /************************************************************/
    public static void flipHorizontal(
        int iStart,
        int iEnd,
        int jStart,
        int jEnd,
        ref Bitmap bmp,
        ref CountdownEvent cntEvent
    )
    {
        // Go through the part of the image and apply the grey image.
        for (int i = iStart; i < iEnd; ++i)
        {
            var h = bmp.Height - 1;
            for (int j = jStart; j < jEnd; ++j)
            {
                Color c = bmp.GetPixel(i, j);

                bmp.SetPixel (i, h, c);
                h--;
            }
        }
        cntEvent.Signal();
    }

    /************************************************************/
    public static void flipVertical(
        int iStart,
        int iEnd,
        int jStart,
        int jEnd,
        ref Bitmap bmp,
        ref CountdownEvent cntEvent
    )
    {
        // Go through the part of the image and apply the grey image.
        var w = bmp.Width - 1;
        for (int i = iStart; i < iEnd; ++i)
        {
            for (int j = jStart; j < jEnd; ++j)
            {
                Color c = bmp.GetPixel(i, j);

                bmp.SetPixel (w, j, c);
            }
            w--;
        }
        cntEvent.Signal();
    }

    /************************************************************/
    public static void flip(
        int iStart,
        int iEnd,
        int jStart,
        int jEnd,
        ref Bitmap bmp,
        ref Bitmap output,
        ref CountdownEvent cntEvent
    )
    {
        // Go through the part of the image and apply the grey image.
        var w = output.Width - 1;
        for (int i = iStart; i < iEnd; ++i)
        {
            var h = output.Height - 1;
            for (int j = jStart; j < jEnd; ++j)
            {
                Color c = bmp.GetPixel(i, j);
                output.SetPixel (w, h, c);
                h--;
            }
            w--;
        }
        cntEvent.Signal();
    }

    /************************************************************/
    public static void setBorderRec(
        int iStart,
        int iEnd,
        int jStart,
        int jEnd,
        ref Bitmap bmp,
        ref CountdownEvent cntEvent
    )
    {
        // Go through the part of the image and apply the grey image.
        for (int i = iStart; i < iEnd; i += iEnd)
        {
            for (int j = jStart; j < jEnd; ++j)
            {
                Color c = bmp.GetPixel(i, j);

                // Apply conversion equation.
                byte black = (byte)(0 * c.R + 0 * c.G + 0 * c.B);

                // Set the color of this pixel.
                bmp.SetPixel(i, j, Color.FromArgb(black, black, black));
            }
        }
        for (int i = iStart; i < iEnd; ++i)
        {
            for (int j = jStart; j < jEnd; j += jEnd)
            {
                Color c = bmp.GetPixel(i, j);

                // Apply conversion equation.
                byte gray = (byte)(0 * c.R + 0 * c.G + 0 * c.B);

                // Set the color of this pixel.
                bmp.SetPixel(i, j, Color.FromArgb(gray, gray, gray));
            }
        }
        cntEvent.Signal();
    }

    /************************************************************/
    public static void compressImageSerial(
        String filename,
        String newFilename,
        int compressionValue
    )
    {
        // Image bitmap
        Bitmap bmp = new Bitmap(filename);

        // Original height and width.
        int totalWidth = bmp.Width;
        int totalHeight = bmp.Height;

        // Get the encoder using our method.
        ImageCodecInfo jpgEncoder = GetEncoder(ImageFormat.Jpeg);

        // Get the quality of the encoder.
        System.Drawing.Imaging.Encoder QualityEncoder =
            System.Drawing.Imaging.Encoder.Quality;

        // Get an array of encoder objects. The 1 means it is of size one because we only need one encoder.
        EncoderParameters myEncoderParameters = new EncoderParameters(1);

        // Gets the encoder parameter with the specified quality.
        EncoderParameter myEncoderParameter =
            new EncoderParameter(QualityEncoder, compressionValue);

        // Sets the encoder parameter.
        myEncoderParameters.Param[0] = myEncoderParameter;

        Stopwatch watch = new Stopwatch();
        watch.Start();
        // MemoryStream ms = CompressImage (bmp, 100);
        //                 long serialTime = watch.ElapsedMilliseconds;


        // bmp.Save (newFilename, jpgEncoder, myEncoderParameters);
        MemoryStream ms = CompressImage (bmp, compressionValue);
        var compressedImage = Image.FromStream(ms);
        
        long serialTime = watch.ElapsedMilliseconds;
        watch.Stop();
         Console
                    .WriteLine("Serial Time time for compression: " +
                    serialTime +
                    " ms\n");
      compressedImage.Save(newFilename);
    }

    /************************************************************/
    public static void blackAndWhiteImageSerial(
        string filename,
        String newFilename
    )
    {
        Bitmap bmp = new Bitmap(filename);

        // Get all the values for the bitmap.
        int iStart = 0;
        int iEnd = bmp.Width;
        int jStart = 0;
        int jEnd = bmp.Height;

        CountdownEvent cntEvent = new CountdownEvent(1);
        setPixelBlackAndWhite(iStart,
        iEnd,
        jStart,
        jEnd,
        ref bmp,
        ref cntEvent);

        cntEvent.Wait();

        bmp.Save (newFilename);
    }

    /************************************************************/
    public static void curesedImageSerial(string filename, String newFilename)
    {
        Bitmap bmp = new Bitmap(filename);
        Bitmap output = new Bitmap(bmp.Width, bmp.Height);

        // Get all the values for the bitmap.
        int iStart = 0;
        int iEnd = bmp.Width;
        int jStart = 0;
        int jEnd = bmp.Height;

        CountdownEvent cntEvent = new CountdownEvent(1);
        flip(iStart, iEnd, jStart, jEnd, ref bmp, ref output, ref cntEvent);

        //CountdownEvent cntEvent1 = new CountdownEvent(1);
        //flipVertical(iStart, iEnd, jStart, jEnd, ref bmp, ref cntEvent);
        cntEvent.Wait();
        

    }
    /************************************************************/
    /************************************************************/
}
