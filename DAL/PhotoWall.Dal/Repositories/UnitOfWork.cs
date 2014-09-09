using PhotoWall.Dal.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoWall.Dal.Repositories
{
    public class UnitOfWork : IDisposable,IUnitOfWork
    {
        private PhotoWallContext _context = new PhotoWallContext();

        private Hashtable _repositories;
        private bool _disposed = false;

        /// <summary>
        /// 取得Repository
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public IRepository<T> Repository<T>() where T : class
        {
            //判斷repositories是否為Collection，不是的話就建立。
            if (_repositories == null)
                _repositories = new Hashtable();

            var type = typeof(T).Name;

            //判斷Collection是否已經擁有repository
            if (!_repositories.ContainsKey(type))
            {
                //建立需要的Repository並存回到Collection
                var repositoryType = typeof(GenericRepository<>);

                var repositoryInstance =
                    Activator.CreateInstance(repositoryType
                            .MakeGenericType(typeof(T)), _context);

                _repositories.Add(type, repositoryInstance);
            }

            return (IRepository<T>)_repositories[type];
        }

        /// <summary>
        /// 將異動儲存到資料庫
        /// </summary>
        public void Save()
        {
            _context.SaveChanges();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this._disposed)
            {
                if (disposing)
                {
                    _context.Dispose();
                }
            }
            this._disposed = true;
        }

        /// <summary>
        /// 銷毀此物件
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
