using System;

namespace SnitzMembership.Repositories
{
    public class UnitOfWork  : IDisposable
    {
        private SnitzMemberContext _context = new SnitzMemberContext();
        private UserProfileRepository _userProfileRepository;

        public UnitOfWork()
        {
            _userProfileRepository = new UserProfileRepository(_context);
        }

        public UserProfileRepository UserProfileRepository
        {
            get { return _userProfileRepository; }
        }

        public int Save()
         {
            return _context.SaveChanges();
         }

        public SnitzMemberContext Context
        {
            
            get{
                return _context;
            }
        }

       private bool disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    _context.Dispose();
                }
            }
            this.disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
   
}
