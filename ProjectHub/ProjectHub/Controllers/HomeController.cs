using Microsoft.AspNetCore.Mvc;
using ProjectHub.Models;
using ProjectHub.Data;
using MongoDB.Driver;

namespace ProjectHub.Controllers
{
    public class HomeController : Controller
    {
        private readonly MongoDbContext _context;

        public HomeController(MongoDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var projects = await _context.Projects
                    .Find(p => p.IsApproved)
                    .ToListAsync();

                // En yeni 10 projeyi al
                var recentProjects = projects
                    .OrderByDescending(p => p.UploadDate)
                    .Take(10)
                    .ToList();

                return View(recentProjects);
            }
            catch (Exception ex)
            {
                // Hata durumunda boþ liste döndür
                return View(new List<Project>());
            }
        }

        public IActionResult About()
        {
            return View();
        }

        public IActionResult Contact()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }
    }
}