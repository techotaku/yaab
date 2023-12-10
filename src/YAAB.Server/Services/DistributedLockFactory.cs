using Medallion.Threading.FileSystem;
using Medallion.Threading;

namespace YAAB.Server.Services
{
    internal class DistributedLockFactory : IDistributedLockFactory
    {
        private readonly DirectoryInfo _lockFileDirectory = new(Path.Combine(Environment.CurrentDirectory, "_locks"));

        public IDistributedLock Create(string lockName)
        {
            return new FileDistributedLock(_lockFileDirectory, lockName);
        }
    }
}
