using MongoDB.Driver;
using ProjectHub.Models;

namespace ProjectHub.Data
{
    public class MongoDbContext
    {
        private readonly IMongoDatabase _database;

        public MongoDbContext(IConfiguration configuration)
        {
            try
            {
                var connectionString = configuration.GetConnectionString("MongoDB");
                if (string.IsNullOrEmpty(connectionString))
                {
                    throw new ArgumentNullException("MongoDB connection string is missing");
                }

                var client = new MongoClient(connectionString);
                _database = client.GetDatabase("ProjectHubDB");

                // Database bağlantısını test et
                _database.ListCollectionNames();
            }
            catch (Exception ex)
            {
                throw new Exception("MongoDB bağlantı hatası: " + ex.Message);
            }
        }

        public IMongoCollection<User> Users => _database.GetCollection<User>("Users");
        public IMongoCollection<Project> Projects => _database.GetCollection<Project>("Projects");
        public IMongoCollection<Comment> Comments => _database.GetCollection<Comment>("Comments");
        public IMongoCollection<AdminComment> AdminComments => _database.GetCollection<AdminComment>("AdminComments");
    }
}