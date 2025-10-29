using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ProjectHub.Models
{
    public class Admin
    {
        [BsonId, BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = default!;

        [BsonElement("FullName")]
        public string FullName { get; set; } = default!;

        [BsonElement("Email")]
        public string Email { get; set; } = default!;

        [BsonElement("PasswordHash")]
        public string PasswordHash { get; set; } = default!;

        [BsonElement("Role")]
        public string Role { get; set; } = "Admin";
    }
}
