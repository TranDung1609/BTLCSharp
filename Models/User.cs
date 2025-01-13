using cloud.core.mongodb;

namespace BTL.Models
{
    public class User: AbstractEntityObjectIdTracking
    {
        public string username { get; set; }
        public string password { get; set; }

        public string IdAsString => Id.ToString();
    }
}
