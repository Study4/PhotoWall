using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoWall.Dal.Interfaces
{
    public interface IUnitOfWork
    {
        void Dispose();
        void Save();
        //void Dispose(bool disposing);
        IRepository<T> Repository<T>() where T : class;
    }
}
