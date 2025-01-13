using cloud.core.mongodb;
using MongoDB.Bson.Serialization.Attributes;

namespace BTL.Models
{
    public class User: AbstractEntityObjectIdTracking
    {
        [BsonElement("username")]
        public string Username { get; set; }
        [BsonElement("password")]
        public string Password { get; set; }

        public string IdAsString => Id.ToString();
    }
}
