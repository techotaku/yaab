using Medallion.Threading;

namespace YAAB.Server.Services
{
    public interface IDistributedLockFactory
    {
        IDistributedLock Create(string lockName);
    }
}