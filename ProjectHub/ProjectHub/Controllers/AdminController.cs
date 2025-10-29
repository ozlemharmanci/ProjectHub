using Microsoft.AspNetCore.Mvc;
using ProjectHub.Models;
using ProjectHub.Data;
using MongoDB.Driver;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace ProjectHub.Controllers
{
    public class AdminController : Controller
    {
        private readonly MongoDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AdminController(MongoDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        // Helper method for session operations
        private string GetSessionString(string key)
        {
            return _httpContextAccessor.HttpContext?.Session.GetString(key);
        }

        // GET: Admin/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            //  Admin yetkisi kontrolü
            var isAdmin = GetSessionString("IsAdmin");
            if (string.IsNullOrEmpty(isAdmin) || isAdmin != "True")
            {
                TempData["ErrorMessage"] = "Bu sayfaya erişmek için admin yetkisine sahip olmalısınız.";
                return RedirectToAction("Login", "Account");
            }

            try
            {
                //  Tüm projeleri al
                var allProjects = await _context.Projects.Find(_ => true).ToListAsync();

                //  Onay bekleyen projeler
                var pendingProjects = allProjects.Where(p => p.IsApproved == false).ToList();

                // 4. Onaylanmış projeler
                var approvedProjects = allProjects.Where(p => p.IsApproved == true).ToList();

                //  Tüm kullanıcılar
                var allUsers = await _context.Users.Find(_ => true).ToListAsync();

                //  İstatistikleri hesapla
                var totalDownloads = approvedProjects.Sum(p => p.DownloadCount);
                var activeUsers = allUsers?.Count ?? 0;
                var adminUsers = allUsers?.Count(u => u.IsAdmin) ?? 0;
                var totalProjects = allProjects?.Count ?? 0;

                // ViewData ile view'a gönder
                ViewData["PendingProjects"] = pendingProjects ?? new List<Project>();
                ViewData["ApprovedProjects"] = approvedProjects ?? new List<Project>();
                ViewData["AllUsers"] = allUsers ?? new List<User>();
                ViewData["TotalDownloads"] = totalDownloads;
                ViewData["ActiveUsers"] = activeUsers;
                ViewData["AdminUsers"] = adminUsers;
                ViewData["TotalProjects"] = totalProjects;

                return View();
            }
            catch (Exception ex)
            {
                //  Hata durumunda
                Console.WriteLine($"Dashboard yükleme hatası: {ex.Message}");

                ViewData["PendingProjects"] = new List<Project>();
                ViewData["ApprovedProjects"] = new List<Project>();
                ViewData["AllUsers"] = new List<User>();
                ViewData["TotalDownloads"] = 0;
                ViewData["ActiveUsers"] = 0;
                ViewData["AdminUsers"] = 0;
                ViewData["TotalProjects"] = 0;

                TempData["ErrorMessage"] = "Dashboard verileri yüklenirken bir hata oluştu. Lütfen daha sonra tekrar deneyin.";
                return View();
            }
        }

        // POST: Admin/ApproveProject/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveProject(string id, string commentText)
        {
            var isAdmin = GetSessionString("IsAdmin");
            if (string.IsNullOrEmpty(isAdmin) || isAdmin != "True")
            {
                TempData["ErrorMessage"] = "Admin yetkisi gerekiyor.";
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var project = await _context.Projects.Find(p => p.Id == id).FirstOrDefaultAsync();
                if (project == null)
                {
                    TempData["ErrorMessage"] = "Proje bulunamadı.";
                    return RedirectToAction("Dashboard");
                }

                // Proje durumunu güncelle
                var filter = Builders<Project>.Filter.Eq(p => p.Id, id);
                var update = Builders<Project>.Update.Set(p => p.IsApproved, true);
                var result = await _context.Projects.UpdateOneAsync(filter, update);

                if (result.ModifiedCount > 0)
                {
                    // Admin yorumu ekle
                    if (!string.IsNullOrEmpty(commentText))
                    {
                        var adminComment = new AdminComment
                        {
                            ProjectId = id,
                            AdminId = GetSessionString("UserId"),
                            AdminUsername = GetSessionString("Username"),
                            Text = commentText,
                            Action = "approve",
                            CreatedAt = DateTime.Now
                        };
                        await _context.AdminComments.InsertOneAsync(adminComment);
                    }

                    TempData["SuccessMessage"] = $"'{project.Title}' projesi başarıyla onaylandı.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Proje güncellenemedi.";
                }

                return RedirectToAction("Dashboard");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ApproveProject hatası: {ex.Message}");
                TempData["ErrorMessage"] = "Proje onaylanırken hata oluştu.";
                return RedirectToAction("Dashboard");
            }
        }

        // POST: Admin/RejectProject/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectProject(string id, string commentText)
        {
            var isAdmin = GetSessionString("IsAdmin");
            if (string.IsNullOrEmpty(isAdmin) || isAdmin != "True")
            {
                TempData["ErrorMessage"] = "Admin yetkisi gerekiyor.";
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var project = await _context.Projects.Find(p => p.Id == id).FirstOrDefaultAsync();
                if (project == null)
                {
                    TempData["ErrorMessage"] = "Proje bulunamadı.";
                    return RedirectToAction("Dashboard");
                }

                // Önce admin yorumu ekle
                if (!string.IsNullOrEmpty(commentText))
                {
                    var adminComment = new AdminComment
                    {
                        ProjectId = id,
                        AdminId = GetSessionString("UserId"),
                        AdminUsername = GetSessionString("Username"),
                        Text = commentText,
                        Action = "reject",
                        CreatedAt = DateTime.Now
                    };
                    await _context.AdminComments.InsertOneAsync(adminComment);
                }

                // Sonra projeyi sil
                var deleteResult = await _context.Projects.DeleteOneAsync(p => p.Id == id);

                if (deleteResult.DeletedCount > 0)
                {
                    TempData["SuccessMessage"] = $"'{project.Title}' projesi başarıyla reddedildi ve silindi.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Proje silinemedi.";
                }

                return RedirectToAction("Dashboard");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"RejectProject hatası: {ex.Message}");
                TempData["ErrorMessage"] = "Proje reddedilirken hata oluştu.";
                return RedirectToAction("Dashboard");
            }
        }

        // GET: Admin/ProjectDetails/{id}
        public async Task<IActionResult> ProjectDetails(string id)
        {
            var isAdmin = GetSessionString("IsAdmin");
            if (string.IsNullOrEmpty(isAdmin) || isAdmin != "True")
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var project = await _context.Projects.Find(p => p.Id == id).FirstOrDefaultAsync();
                if (project == null)
                {
                    TempData["ErrorMessage"] = "Proje bulunamadı.";
                    return RedirectToAction("Dashboard");
                }

                var adminComments = await _context.AdminComments.Find(c => c.ProjectId == id)
                    .SortByDescending(c => c.CreatedAt)
                    .ToListAsync();

                ViewData["AdminComments"] = adminComments ?? new List<AdminComment>();
                return View(project);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ProjectDetails hatası: {ex.Message}");
                TempData["ErrorMessage"] = "Proje detayları yüklenirken hata oluştu.";
                return RedirectToAction("Dashboard");
            }
        }

        // GET: Admin/Index
        public IActionResult Index()
        {
            var isAdmin = GetSessionString("IsAdmin");
            if (string.IsNullOrEmpty(isAdmin) || isAdmin != "True")
            {
                return RedirectToAction("Login", "Account");
            }
            return View();
        }
    }
}