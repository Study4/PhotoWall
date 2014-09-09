using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PhotoWall.UI.Web.Models
{
    public class FileDetail
    {
        public string Name { get; set; }
        public long Size { get; set; }
        public string ContentType { get; set; }
        public string Location { get; set; }
    }
}