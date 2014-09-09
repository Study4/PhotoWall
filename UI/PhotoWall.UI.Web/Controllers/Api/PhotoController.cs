using PhotoWall.UI.Web.Handler;
using PhotoWall.UI.Web.Models;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.WindowsAzure.Storage.Queue;
using PhotoWall.Domain;
using Newtonsoft.Json;
using PhotoWall.Domain.Entities;
using System.Configuration;
using PhotoWall.Dal.Repositories;
using System.IO;
using System.Diagnostics;
using PhotoWall.Dal.Interfaces;

namespace PhotoWall.UI.Web.Controllers.API
{
    [Authorize]
    public class PhotoController : ApiController
    {
        private CloudBlobContainer _container;
        private CloudQueue _thumbnailRequestQueue;

        private IUnitOfWork _uow;

        public PhotoController()
        {
            _uow = new UnitOfWork();

            CloudStorageAccount account = CloudStorageAccount.Parse(
                ConfigurationManager.ConnectionStrings["StorageConnection"].ConnectionString);
            //Blob
            CloudBlobClient client = account.CreateCloudBlobClient();
            _container = client.GetContainerReference("photowall");
            _container.CreateIfNotExists();
            _container.SetPermissions(
                new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Blob });

            //Queue
            CloudQueueClient queueClient = account.CreateCloudQueueClient();
            _thumbnailRequestQueue = queueClient.GetQueueReference("thumbnailrequest");
            _thumbnailRequestQueue.CreateIfNotExists();
        }

        [AllowAnonymous]
        public IQueryable<Photo> Get()
        {
            return _uow.Repository<Photo>().Get(orderBy:m=>m.OrderByDescending(o=>o.UploadDate)).AsQueryable();
        }

        /// <summary>
        /// 上傳檔案
        /// </summary>
        /// <returns></returns>
        public async Task<List<FileDetail>> Post()
        {
            if (!Request.Content.IsMimeMultipartContent("form-data"))
            {
                throw new HttpResponseException(HttpStatusCode.UnsupportedMediaType);
            }

            var multipartStreamProvider = new CustomMultipartFormDataStreamProvider(_container);

            await Request.Content.ReadAsMultipartAsync<CustomMultipartFormDataStreamProvider>(
                multipartStreamProvider);

            foreach (var file in multipartStreamProvider.Files)
            {
                var photo = new Photo()
                {
                    Id = Guid.Parse(file.Name),
                    UploadDate = DateTime.UtcNow,
                    ImageURL = new Uri(file.Location).ToString(),
                    UploadUser = ""
                };

                //Queue
                var queueMessage = new CloudQueueMessage(JsonConvert.SerializeObject(photo));
                await _thumbnailRequestQueue.AddMessageAsync(queueMessage);
                Trace.TraceInformation("Created queue message for Id {0}", photo.Id);
            }

            return multipartStreamProvider.Files;
        }
    }
}





