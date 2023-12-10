using NMemory;
using NMemory.Tables;

namespace YAAB.Server.Repositories
{
    internal class WeChatAccessTokenEntityRepository : IWeChatAccessTokenEntityRepository
    {
        public void Insert(Models.WeChatAccessTokenEntity entity)
        {
            entity.CreateTimestamp = DateTimeOffset.Now.ToUnixTimeSeconds();
            entity.UpdateTimestamp = entity.CreateTimestamp;
            GlobalDatabase.TableWeChatAccessTokenEntity.Insert(entity);
        }

        public void Update(Models.WeChatAccessTokenEntity entity)
        {
            entity.UpdateTimestamp = DateTimeOffset.Now.ToUnixTimeSeconds();
            GlobalDatabase.TableWeChatAccessTokenEntity.Update(entity);
        }

        public void Delete(Models.WeChatAccessTokenEntity entity)
        {
            GlobalDatabase.TableWeChatAccessTokenEntity.Delete(entity);
        }

        IEnumerator<Models.WeChatAccessTokenEntity> IEnumerable<Models.WeChatAccessTokenEntity>.GetEnumerator()
        {
            return GlobalDatabase.TableWeChatAccessTokenEntity.GetEnumerator();
        }

        public System.Collections.IEnumerator GetEnumerator()
        {
            return GlobalDatabase.TableWeChatAccessTokenEntity.GetEnumerator();
        }
    }

    internal class GlobalDatabase
    {
        static GlobalDatabase()
        {
            Database db = new();

            TableWeChatAccessTokenEntity = db.Tables.Create<Models.WeChatAccessTokenEntity, string>(e => e.AppId);
        }

        public static Table<Models.WeChatAccessTokenEntity, string> TableWeChatAccessTokenEntity { get; }
    }
}
