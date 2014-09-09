using PhotoWall.UI.Web.Models;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Text;

namespace PhotoWall.UI.Web.Handler
{
    public class CustomMultipartFormDataStreamProvider : MultipartFormDataStreamProvider
    {
        private CloudBlobContainer _container;
        public List<FileDetail> Files { get; set; }

        public CustomMultipartFormDataStreamProvider(CloudBlobContainer container) 
            : base(Path.GetTempPath()) 
        { 
            _container = container;
            Files = new List<FileDetail>(); 
        }

        

        public override Task ExecutePostProcessingAsync()
        {
            // Upload the files to azure blob storage and remove them from local disk 
            foreach (var fileData in this.FileData)
            {
                if (IsValidImage(fileData.LocalFileName))
                {
                    var guid = Guid.NewGuid();

                    CloudBlockBlob blockBlob = _container.GetBlockBlobReference(guid.ToString() + ".jpg");
                    blockBlob.Properties.ContentType = "image/jpg";
                    blockBlob.UploadFromFile(fileData.LocalFileName, FileMode.Open);
                    File.Delete(fileData.LocalFileName);
                    Files.Add(new FileDetail
                    {
                        ContentType = blockBlob.Properties.ContentType,
                        Name = guid.ToString(),
                        Size = blockBlob.Properties.Length,
                        Location = blockBlob.Uri.AbsoluteUri
                    });
                }
            }

            return base.ExecutePostProcessingAsync();
        }


        private bool IsValidImage(string filename)
        {
            Stream imageStream = null;
            try
            {
                imageStream = new FileStream(filename, FileMode.Open);

                if (imageStream.Length > 0)
                {
                    byte[] header = new byte[30]; // Change size if needed.
                    //string[] imageHeaders = new[]
                    //{
                    //    "BM",       // BMP
                    //    "GIF",      // GIF
                    //    Encoding.ASCII.GetString(new byte[]{137, 80, 78, 71}),// PNG
                    //    "MM\x00\x2a", // TIFF
                    //    "II\x2a\x00" // TIFF
                    //};

                    imageStream.Read(header, 0, header.Length);

                    //bool isImageHeader = imageHeaders.Count(str => Encoding.ASCII.GetString(header).StartsWith(str)) > 0;
                    bool isImageHeader = false;
                    if (imageStream != null)
                    {
                        imageStream.Close();
                        imageStream.Dispose();
                        imageStream = null;
                    }

                    if (isImageHeader == false)
                    {
                        //Verify if is jpeg
                        using (BinaryReader br = new BinaryReader(File.Open(filename, FileMode.Open)))
                        {
                            UInt16 soi = br.ReadUInt16();  // Start of Image (SOI) marker (FFD8)
                            UInt16 jfif = br.ReadUInt16(); // JFIF marker

                            return soi == 0xd8ff && (jfif == 0xe0ff || jfif == 57855);
                        }
                    }

                    return isImageHeader;
                }

                return false;
            }
            catch { return false; }
            finally
            {
                if (imageStream != null)
                {
                    imageStream.Close();
                    imageStream.Dispose();
                }
            }
        }

    }
}