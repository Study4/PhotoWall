using Microsoft.AspNet.SignalR.Client;
using Microsoft.Azure.WebJobs;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage.Blob;
using PhotoWall.Dal.Interfaces;
using PhotoWall.Dal.Repositories;
using PhotoWall.Domain;
using PhotoWall.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PhotoWall.Job.ReSize
{
    // To learn more about Microsoft Azure WebJobs, please see http://go.microsoft.com/fwlink/?LinkID=401557
    public class Program
    {
        private static IUnitOfWork _uow;
        //Proxy to SignalR Hub
        private static HubConnection mHubConnection;
        private static IHubProxy mHubProxy;

        private static object synRoot = new object();

        static void Main(string[] args)
        {
            Initialize();

            JobHost host = new JobHost();
            host.RunAndBlock();
        }

        private static void Initialize()
        {
            _uow = new UnitOfWork();
        }

        public static void GenerateThumbnail(
        [QueueTrigger("thumbnailrequest")] Photo photoInfo,
        [Blob("photowall/{Id}.jpg")] Stream input,
        [Blob("photowall/{Id}_thumbnail.jpg")] CloudBlockBlob outputBlob)
        {
            using (Stream output = outputBlob.OpenWrite())
            {
                ConvertImageToThumbnailJPG(input, output);
                outputBlob.Properties.ContentType = "image/jpeg";
            }

            photoInfo.ThumbnailURL = outputBlob.Uri.ToString();

            if(_uow.Repository<Photo>().Get(m=>m.Id == photoInfo.Id).Count() > 0)
            {
                return;
            }

            _uow.Repository<Photo>().Insert(photoInfo);
            _uow.Save();

            lock (synRoot)
            {
                //Reinitialize SignalR Hub proxy.
                if (mHubConnection == null)
                    initializeSignalRClient();
            }
            try
            {
                mHubProxy.Invoke("Send", photoInfo);
            }
            catch
            {
                //Clear the connection so it can be reinitialized next time.
                lock (synRoot)
                {
                    try
                    {
                        if (mHubConnection != null)
                            mHubConnection.Stop();
                    }
                    catch
                    {
                        //Failed to stop. We'll reset next
                    }
                    mHubConnection = null;
                }
            }

        }

        private static void initializeSignalRClient()
        {
            mHubConnection = new HubConnection(ConfigurationManager.AppSettings["HubAddress"]);
            //mHubConnection = new HubConnection("http://localhost:19661//signalr");

            mHubProxy = mHubConnection.CreateHubProxy("PhotoHub");
            mHubConnection.Start().Wait();
        }

        public static void ConvertImageToThumbnailJPG(Stream input, Stream output)
        {
            ImageCodecInfo jpgInfo = ImageCodecInfo.GetImageEncoders().Where(codecInfo => codecInfo.MimeType == "image/jpeg").First();
            Image image = new Bitmap(input);
            System.Drawing.Bitmap bitmap = null;
            try
            {
                int targetWidth = 200;
                int targetHeight = 200;
                int left = 0;
                int top = 0;
                int srcWidth = targetWidth;
                int srcHeight = targetHeight;
                bitmap = new System.Drawing.Bitmap(targetWidth, targetHeight);
                double croppedHeightToWidth = (double)targetHeight / targetWidth;
                double croppedWidthToHeight = (double)targetWidth / targetHeight;

                if (image.Width > image.Height)
                {
                    srcWidth = (int)(Math.Round(image.Height * croppedWidthToHeight));
                    if (srcWidth < image.Width)
                    {
                        srcHeight = image.Height;
                        left = (image.Width - srcWidth) / 2;
                    }
                    else
                    {
                        srcHeight = (int)Math.Round(image.Height * ((double)image.Width / srcWidth));
                        srcWidth = image.Width;
                        top = (image.Height - srcHeight) / 2;
                    }
                }
                else
                {
                    srcHeight = (int)(Math.Round(image.Width * croppedHeightToWidth));
                    if (srcHeight < image.Height)
                    {
                        srcWidth = image.Width;
                        top = (image.Height - srcHeight) / 2;
                    }
                    else
                    {
                        srcWidth = (int)Math.Round(image.Width * ((double)image.Height / srcHeight));
                        srcHeight = image.Height;
                        left = (image.Width - srcWidth) / 2;
                    }
                }

                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    g.CompositingQuality = CompositingQuality.HighQuality;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.DrawImage(image, new Rectangle(0, 0, bitmap.Width, bitmap.Height), new Rectangle(left, top, srcWidth, srcHeight), GraphicsUnit.Pixel);
                }
            }
            catch { }
            try
            {
                using (EncoderParameters encParams = new EncoderParameters(1))
                {
                    encParams.Param[0] = new EncoderParameter(Encoder.Quality, (long)100);
                    //quality should be in the range [0..100] .. 100 for max, 0 for min (0 best compression)
                    bitmap.Save(output, jpgInfo, encParams);
                }
            }
            catch { }
            if (bitmap != null)
            {
                bitmap.Dispose();
            }
        }
    }
}
