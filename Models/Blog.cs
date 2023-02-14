using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MyApp.Models
{
    public class Blog
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public IFormFile Photo { get; set; }

    }
}
