namespace PhotoWall.Dal
{
    using PhotoWall.Domain.Entities;
    using System;
    using System.Data.Entity;
    using System.Linq;

    public class PhotoWallContext : DbContext
    {
        public PhotoWallContext()
            : base("name=PhotoWallContext")
        {
        }

        public virtual DbSet<Photo> Photos { get; set; }
    }
}