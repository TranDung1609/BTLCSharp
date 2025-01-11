using cloud.core;
using cloud.core.mongodb;

namespace BTL.Models
{
    public class AdsMongoDbContext : BaseMongoObjectIdDbContext
    {
        public AdsMongoDbContext() : base(AppSettingsHelper.GetValueByKey("AdsMongoDbContext:ConnectionString"))
        {

        }

        public DbSetObjectId<User> users { get; set; }
    }
}
