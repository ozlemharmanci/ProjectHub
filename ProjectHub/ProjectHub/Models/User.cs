using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace ProjectHub.Models
{
    public class User
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        [Required]
        [BsonElement("username")]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [BsonElement("email")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [BsonElement("password")]
        public string Password { get; set; } = string.Empty;

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [BsonElement("profileBio")]
        public string ProfileBio { get; set; } = string.Empty;

        [BsonElement("profileImage")]
        public string ProfileImage { get; set; } = "default-profile.png";

        [BsonElement("isAdmin")]
        public bool IsAdmin { get; set; } = false;

        [BsonElement("followers")]
        public List<string> Followers { get; set; } = new List<string>();

        [BsonElement("following")]
        public List<string> Following { get; set; } = new List<string>();

        [BsonIgnore]
        public int FollowerCount => Followers?.Count ?? 0;

        [BsonIgnore]
        public int FollowingCount => Following?.Count ?? 0;

        [BsonIgnore]
        public bool IsFollowedByCurrentUser { get; set; }
    }
}