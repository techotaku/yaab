namespace YAAB.Server.Repositories
{
    public interface IWeChatAccessTokenEntityRepository : IEnumerable<Models.WeChatAccessTokenEntity>
    {
        void Insert(Models.WeChatAccessTokenEntity entity);

        void Update(Models.WeChatAccessTokenEntity entity);

        void Delete(Models.WeChatAccessTokenEntity entity);
    }
}
