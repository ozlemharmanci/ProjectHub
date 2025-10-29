using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ProjectHub.Models
{
    public class AdminComment
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("projectId")]
        public string ProjectId { get; set; }

        [BsonElement("adminId")]
        public string AdminId { get; set; }

        [BsonElement("adminUsername")]
        public string AdminUsername { get; set; }

        [BsonElement("text")]
        public string Text { get; set; }

        [BsonElement("action")]
        public string Action { get; set; } // "approve", "reject", "comment"

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}