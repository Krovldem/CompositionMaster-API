using Microsoft.EntityFrameworkCore;
using CompositionMaster.Models;

namespace CompositionMaster.Services.Base
{
    public class BaseService
    {
        protected readonly ApplicationContext _context;

        public BaseService(ApplicationContext context)
        {
            _context = context;
        }

        public List<T> GetAll<T>() where T : class
        {
            return _context.Set<T>()
                .AsNoTracking()
                .ToList();
        }

        public T? Get<T>(params object[] keyValues) where T : class
        {
            return _context.Set<T>().Find(keyValues);
        }

        public void Create<T>(T entity) where T : class
        {
            _context.Set<T>().Add(entity);
            _context.SaveChanges();
        }

        public void Update<T>(T entity) where T : class
        {
            _context.Entry(entity).State = EntityState.Modified;
            _context.SaveChanges();
        }

        public bool Delete<T>(params object[] keyValues) where T : class
        {
            var entity = _context.Set<T>().Find(keyValues);
            if (entity == null)
                return false;

            _context.Set<T>().Remove(entity);
            _context.SaveChanges();
            return true;
        }

        public void DeleteAll<T>() where T : class
        {
            var entities = _context.Set<T>().ToList();
            _context.Set<T>().RemoveRange(entities);
            _context.SaveChanges();
        }

        public bool Exists<T>(params object[] keyValues) where T : class
        {
            return _context.Set<T>().Find(keyValues) != null;
        }
    }
}