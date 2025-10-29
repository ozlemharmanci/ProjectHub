using Microsoft.AspNetCore.Mvc;
using ProjectHub.Models;
using ProjectHub.Data;
using MongoDB.Driver;
using System.Diagnostics;

namespace ProjectHub.Controllers
{
    public class SearchController : Controller
    {
        private readonly MongoDbContext _context;

        public SearchController(MongoDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string query, string type = "projects")
        {
            if (string.IsNullOrEmpty(query))
            {
                return View(new SearchResults());
            }

            var results = new SearchResults { Query = query, SearchType = type };

            try
            {
                if (type == "projects" || type == "all")
                {
                    var projectFilter = Builders<Project>.Filter.And(
                        Builders<Project>.Filter.Eq(p => p.IsApproved, true),
                        Builders<Project>.Filter.Or(
                            Builders<Project>.Filter.Regex(p => p.Title, new MongoDB.Bson.BsonRegularExpression(query, "i")),
                            Builders<Project>.Filter.Regex(p => p.Description, new MongoDB.Bson.BsonRegularExpression(query, "i")),
                            Builders<Project>.Filter.Regex(p => p.Username, new MongoDB.Bson.BsonRegularExpression(query, "i"))
                        )
                    );

                    results.Projects = await _context.Projects.Find(projectFilter)
                        .SortByDescending(p => p.DownloadCount)
                        .Limit(20)
                        .ToListAsync();
                }

                if (type == "users" || type == "all")
                {
                    var userFilter = Builders<User>.Filter.Or(
                        Builders<User>.Filter.Regex(u => u.Username, new MongoDB.Bson.BsonRegularExpression(query, "i")),
                        Builders<User>.Filter.Regex(u => u.ProfileBio, new MongoDB.Bson.BsonRegularExpression(query, "i"))
                    );

                    results.Users = await _context.Users.Find(userFilter)
                        .Limit(20)
                        .ToListAsync();

                    var currentUserId = HttpContext.Session.GetString("UserId");
                    if (!string.IsNullOrEmpty(currentUserId))
                    {
                        foreach (var user in results.Users)
                        {
                            user.IsFollowedByCurrentUser = user.Followers.Contains(currentUserId);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Arama hatası: {ex.Message}");
            }

            return View(results);
        }
    }

    public class SearchResults
    {
        public string Query { get; set; }
        public string SearchType { get; set; }
        public List<Project> Projects { get; set; } = new List<Project>();
        public List<User> Users { get; set; } = new List<User>();
    }
}