using cloud.core.mongodb;
using MongoDB.Bson.Serialization.Attributes;

namespace BTL.Models
{
    public class Class : AbstractEntityObjectIdTracking
    {
        [BsonElement("classId")]
        public Guid CLassId { get; set; }

        [BsonElement("fullname")]
        public string Fullname { get; set; }

        [BsonIgnore]
        public string IdAsString => Id.ToString();
    }
}
