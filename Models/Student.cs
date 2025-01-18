using cloud.core.mongodb;
using MongoDB.Bson.Serialization.Attributes;

namespace BTL.Models
{
    public class Student : AbstractEntityObjectIdTracking
    {
        [BsonElement("studentId")]
        public Guid StudentId { get; set; }

        [BsonElement("firstName")]
        public string FirstName { get; set; }

        [BsonElement("lastName")]
        public string LastName { get; set; }

        [BsonElement("classId")]
        public Guid ClassId { get; set; }

        [BsonElement("gender")]
        public short Gender { get; set; } = 1;

        [BsonElement("dayOfBirth")]
        public string DayOfBirth { get; set; }

        [BsonElement("avatar")]
        public string Avatar { get; set; }

        [BsonElement("isDeleted")]
        public int IsDeleted { get; set; } = 0;

        [BsonIgnore]
        public string IdAsString => Id.ToString();

    }
}
