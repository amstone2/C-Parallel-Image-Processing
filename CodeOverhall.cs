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

    public class ThreadDataForCompression {
        public Rectangle rec;
        public readonly Graphics g;
        public readonly Bitmap bmp;
        public int compressionValue;
        public readonly CountdownEvent cntEvent;

        public ThreadDataForCompression(Rectangle r, ref Graphics g, in Bitmap bitmap, int cv, ref CountdownEvent ce) {
          this.rec = r;
          this.g = g;
          this.bmp = bitmap;
          this.compressionValue = cv;
          this.cntEvent = ce;
        }
    }


        public class ThreadDataForModification {
        public Rectangle rec;
        public readonly Bitmap bmp;
        public Bitmap newBmp;

        public int modificationValue;
        public readonly CountdownEvent cntEvent;

        public ThreadDataForModification(Rectangle r, ref Bitmap bitmap, ref Bitmap newBmp, int modificationValue, ref CountdownEvent cntEvent) {
          this.rec = r;
          this.bmp = bitmap;
          this.newBmp = newBmp;
          this.modificationValue = modificationValue;
          this.cntEvent = cntEvent;
        }
    }
/************************************************************/

class CodeOverhall
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
            int index = newFilename.IndexOf(".");
            String serialNewFileName = newFilename.Substring(0, index);
            serialNewFileName += "Serial.jpg";

            String mode = args[3];

            // For compression
            if (mode == "compress")
            {
                int compressionValue = Int32.Parse(args[4]);

                compressImageParallel (threads, filename, newFilename, compressionValue);
                compressImageSerial (filename, serialNewFileName, compressionValue);

            }
            else if(mode == "baw")
            {
              blackAndWhiteImageInParallel(threads, filename, newFilename);
              blackAndWhiteImageSerial(filename, serialNewFileName);
            }
            else if(mode == "flip")
            {
              flipInParallel(threads, filename, newFilename);
              flipImageSerial(filename, serialNewFileName);
            }
        else
        {
            // Help message.
            Console.WriteLine("No command line arguments passed.");
            Console.WriteLine("Arg0 = threads");
            Console.WriteLine("Arg1 = filename");
            Console.WriteLine("Arg2 = new fileanme");
            Console.WriteLine("Arg3 = Mode (compress)");
        }
      
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
        Stopwatch watch = new Stopwatch();
        watch.Start();
        // Image bitmap
        Bitmap bmp = new Bitmap(filename);

        // Original height and width.
        int totalWidth = bmp.Width;
        int totalHeight = bmp.Height;

        // threads passed to partitioning algorithm.
        int numOfColumns = threads / 2;

        // Get the rectangles needed to compress the image.
        Rectangle[] partitionedRectangles = new Rectangle[threads];
        getRectangles(ref bmp,
        threads,
        ref partitionedRectangles);

        // Final bitmap to have lines drawn on it.
        Bitmap finalBitmap = new Bitmap(totalWidth, totalHeight);

        // Set the graphics object to be the bitmap we will draw onto.
        Graphics g = Graphics.FromImage(finalBitmap);

        // Countdown event so we can tell when the threads are done.
        CountdownEvent cntEvent = new CountdownEvent(threads);




        // Go through all the rectangles, compressing and drawing them back on the bitmap.
        foreach (var rec in partitionedRectangles)
        {
            var data = new ThreadDataForCompression(rec, ref g, in bmp, compressionValue, ref cntEvent);
            ThreadPool.QueueUserWorkItem(s => compressRectangleAndDraw(s), data);
        }
        

        // Wait until all threads are done.
        cntEvent.Wait();

        // Create and save the final bitmap.
        watch.Stop();
        long parTime = watch.ElapsedMilliseconds;
        Console.WriteLine("New Parallel time Time time for compression: " + parTime + " ms\n\n");
        finalBitmap.Save(newFilename, System.Drawing.Imaging.ImageFormat.Jpeg);
    }


    /************************************************************/
    public static void getRectangles(
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

        // Go through the i and j values to create the rectangles.
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
    public static void compressRectangleAndDraw(object d)
    {
        ThreadDataForCompression data = (ThreadDataForCompression)d;
        var rec = data.rec;
        var g = data.g;
        var bmp = data.bmp;
        var compressionValue = data.compressionValue;
        var cntEvent = data.cntEvent;

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
    // Returns an ImageCodeInfo object to be used in compression.
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
    public static void compressImageSerial(
        String filename,
        String newFilename,
        int compressionValue
    )
    {
        Stopwatch watch = new Stopwatch();
        watch.Start();
        Bitmap bmp = new Bitmap(filename);
        MemoryStream ms = CompressImage (bmp, compressionValue);
        var compressedImage = Image.FromStream(ms);

        // compressedImage.Save(newFilename);

        watch.Stop();
        long serialTime = watch.ElapsedMilliseconds;
        Console.WriteLine("Serial Time time for compression: " + serialTime + " ms\n");
    }
    /************************************************************/


      public static void blackAndWhiteImageInParallel(
        int threads,
        string filename,
        String newFilename
    )
    {
        Stopwatch watch = new Stopwatch();
        watch.Start();
        // New bitmap of the input image.
        Bitmap bmp = new Bitmap(filename);
        Bitmap newBmp = new Bitmap(0,0);

        // threads passed to partitioning algorithm.
        int numOfColumns = threads / 2;

        // Get the rectangles needed to modify the image in.
        Rectangle[] partitionedRectangles = new Rectangle[threads];
        getRectangles(ref bmp,
        threads,
        ref partitionedRectangles);

        // Count down event so we can see how many threads have been completed.
        CountdownEvent cntEvent = new CountdownEvent(threads);

        int modificationValue = 0;
        // Go through all the elements in the partioned list and apply the filter.
        foreach (var rec in partitionedRectangles)
        {
            var data = new ThreadDataForModification(rec, ref bmp, ref newBmp, modificationValue, ref cntEvent);
            ThreadPool.QueueUserWorkItem(s => setPixelBlackAndWhite(s), data);
        }

        // Wiat for all threads to finish.
        cntEvent.Wait();

        watch.Stop();
        long parallelTime = watch.ElapsedMilliseconds;
        Console.WriteLine("Serial Time time for baw: " + parallelTime + " ms\n");
        // Save the bitmap to the new file name.
        bmp.Save (newFilename);
    }

    public static void blackAndWhiteImageSerial(String filename, String newFilename)
    {
        Stopwatch watch = new Stopwatch();
        watch.Start();

        // New bitmap of the input image.
        Bitmap bmp = new Bitmap(filename);
        Bitmap newBmp = new Bitmap(0,0);

        Rectangle rec = new Rectangle(0, 0, bmp.Width, bmp.Height);
        int modificationValue = 0;
        CountdownEvent cntEvent = new CountdownEvent(1);

        var data = new ThreadDataForModification(rec, ref bmp, ref newBmp, modificationValue, ref cntEvent);

        setPixelBlackAndWhite(data);

        watch.Stop();
        long parallelTime = watch.ElapsedMilliseconds;
        Console.WriteLine("Serial Time time for baw: " + parallelTime + " ms\n");
        // bmp.Save (newFilename);
    }


    public static void setPixelBlackAndWhite(object d)
    {
        ThreadDataForModification data = (ThreadDataForModification)d;
        var rec = data.rec;
        var bmp = data.bmp;
        var modificationValue = data.modificationValue;
        var cntEvent = data.cntEvent;

        int iStart = rec.X;
        int iEnd = rec.Width + rec.X;
        int jStart = rec.Y;
        int jEnd = rec.Height + rec.Y;
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

  public static void flipInParallel(
        int threads,
        string filename,
        String newFilename
    )
    {
        Stopwatch watch = new Stopwatch();
        watch.Start();
        // New bitmap of the input image.
        Bitmap bmp = new Bitmap(filename);
        Bitmap newBmp = new Bitmap(bmp.Width, bmp.Height);

        // threads passed to partitioning algorithm.
        int numOfColumns = threads / 2;

        // Get the rectangles needed to modify the image in.
        Rectangle[] partitionedRectangles = new Rectangle[threads];
        getRectangles(ref bmp,
        threads,
        ref partitionedRectangles);

        // Count down event so we can see how many threads have been completed.
        CountdownEvent cntEvent = new CountdownEvent(threads);

        int modificationValue = 0;
        // Go through all the elements in the partioned list and apply the filter.
        foreach (var rec in partitionedRectangles)
        {
            var data = new ThreadDataForModification(rec, ref bmp, ref newBmp, modificationValue, ref cntEvent);
            ThreadPool.QueueUserWorkItem(s => flip(s), data);
        }

        // Wiat for all threads to finish.
        cntEvent.Wait();

        watch.Stop();
        long parallelTime = watch.ElapsedMilliseconds;
        Console.WriteLine("Parallel time for flip: " + parallelTime + " ms\n");
        // Save the bitmap to the new file name.
        newBmp.Save (newFilename);
    }
  
  
    public static void flipImageSerial(String filename, String newFilename)
    {
        Stopwatch watch = new Stopwatch();
        watch.Start();

        // New bitmap of the input image.
        Bitmap bmp = new Bitmap(filename);
        Bitmap newBmp = new Bitmap(bmp.Width, bmp.Height);
        Rectangle rec = new Rectangle(0, 0, bmp.Width, bmp.Height);
        int modificationValue = 0;
        CountdownEvent cntEvent = new CountdownEvent(1);

        var data = new ThreadDataForModification(rec, ref bmp, ref newBmp, modificationValue, ref cntEvent);

        flip(data);

        watch.Stop();
        long parallelTime = watch.ElapsedMilliseconds;
        Console.WriteLine("Serial Time time for flip: " + parallelTime + " ms\n");
        newBmp.Save (newFilename);
    }

    public static void flip(Object d)
    {
        ThreadDataForModification data = (ThreadDataForModification)d;
        var rec = data.rec;
        var bmp = data.bmp;
        var newBmp = data.newBmp;
        var modificationValue = data.modificationValue;
        var cntEvent = data.cntEvent;

        int iStart = rec.X;
        int iEnd = rec.Width + rec.X;
        int jStart = rec.Y;
        int jEnd = rec.Height + rec.Y;


        var w = newBmp.Width - 1;
        for (int i = iStart; i < iEnd; ++i)
        {
            var h = newBmp.Height - 1;
            for (int j = jStart; j < jEnd; ++j)
            {
                Color c = bmp.GetPixel(i, j);
                newBmp.SetPixel (w, h, c);
                h--;
            }
            w--;
        }
        cntEvent.Signal();
    }

    
}


/************************************************************/
