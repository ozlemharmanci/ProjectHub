using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ProjectHub.Models
{
    public class Comment
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("projectId")]
        public string ProjectId { get; set; }

        [BsonElement("userId")]
        public string UserId { get; set; }

        [BsonElement("username")]
        public string Username { get; set; }

        [BsonElement("text")]
        public string Text { get; set; }

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}