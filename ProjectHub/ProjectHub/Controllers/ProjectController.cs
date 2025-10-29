using Microsoft.AspNetCore.Mvc;
using ProjectHub.Models;
using ProjectHub.Data;
using MongoDB.Driver;

namespace ProjectHub.Controllers
{
    public class ProjectController : Controller
    {
        private readonly MongoDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public ProjectController(MongoDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public IActionResult Upload()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Account");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(Project project, IFormFile projectFile)
        {
            var userId = HttpContext.Session.GetString("UserId");
            var username = HttpContext.Session.GetString("Username");

            // Kullanıcı giriş kontrolü
            if (string.IsNullOrEmpty(userId))
            {
                TempData["ErrorMessage"] = "Proje yüklemek için giriş yapmalısınız.";
                return RedirectToAction("Login", "Account");
            }

            //  Dosya kontrolü
            if (projectFile == null || projectFile.Length == 0)
            {
                ModelState.AddModelError("", "Lütfen bir proje dosyası yükleyin.");
                return View(project);
            }

            //  Dosya formatı kontrolü (ZIP)
            var allowedExtensions = new[] { ".zip" };
            var fileExtension = Path.GetExtension(projectFile.FileName).ToLower();
            if (!allowedExtensions.Contains(fileExtension))
            {
                ModelState.AddModelError("", "Sadece ZIP formatında dosya yükleyebilirsiniz.");
                return View(project);
            }

            // ModelState validasyonunu atla, sadece required alanları kontrol et
            if (string.IsNullOrWhiteSpace(project.Title))
            {
                ModelState.AddModelError("Title", "Proje başlığı zorunludur.");
                return View(project);
            }

            if (string.IsNullOrWhiteSpace(project.Description))
            {
                ModelState.AddModelError("Description", "Proje açıklaması zorunludur.");
                return View(project);
            }

            try
            {
                //  Dosya yükleme klasörünü oluştur
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "projects");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                //  Benzersiz dosya adı oluştur
                var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(projectFile.FileName)}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                //  Dosyayı kaydet
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await projectFile.CopyToAsync(fileStream);
                }

                //  Proje nesnesini oluştur
                var newProject = new Project
                {
                    Title = project.Title.Trim(),
                    Description = project.Description.Trim(),
                    UserId = userId,
                    Username = username,
                    FileName = projectFile.FileName,
                    FilePath = uniqueFileName,
                    IsApproved = false,
                    UploadDate = DateTime.Now,
                    DownloadCount = 0
                };

                // Veritabanına kaydet
                await _context.Projects.InsertOneAsync(newProject);

                //  Başarı mesajı ve yönlendirme
                TempData["SuccessMessage"] = "Projeniz başarıyla yüklendi! Admin onayı bekleniyor.";

                // MyProjects sayfasına yönlendir
                return RedirectToAction("MyProjects");
            }
            catch (Exception ex)
            {
                // 11. Hata durumu
                Console.WriteLine($"Upload hatası: {ex.Message}");
                ModelState.AddModelError("", "Proje yüklenirken bir hata oluştu. Lütfen tekrar deneyin.");
                return View(project);
            }
        }


        public async Task<IActionResult> Edit(string id)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Account");

            var project = await _context.Projects.Find(p => p.Id == id && p.UserId == userId).FirstOrDefaultAsync();
            if (project == null) return NotFound();

            return View(project);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, Project updatedProject)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Account");

            var project = await _context.Projects.Find(p => p.Id == id && p.UserId == userId).FirstOrDefaultAsync();
            if (project == null) return NotFound();

            if (ModelState.IsValid)
            {
                project.Title = updatedProject.Title;
                project.Description = updatedProject.Description;

                await _context.Projects.ReplaceOneAsync(p => p.Id == id, project);
                return RedirectToAction("MyProjects");
            }

            return View(updatedProject);
        }

        public async Task<IActionResult> Delete(string id)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Account");

            var project = await _context.Projects.Find(p => p.Id == id && p.UserId == userId).FirstOrDefaultAsync();
            if (project == null) return NotFound();

            return View(project);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Delete")] 
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Account");

            var project = await _context.Projects.Find(p => p.Id == id && p.UserId == userId).FirstOrDefaultAsync();
            if (project == null) return NotFound();

            var filePathToDelete = Path.Combine(_environment.WebRootPath, "uploads", "projects", project.FilePath);
            if (System.IO.File.Exists(filePathToDelete)) System.IO.File.Delete(filePathToDelete);

            await _context.Projects.DeleteOneAsync(p => p.Id == id);
            await _context.Comments.DeleteManyAsync(c => c.ProjectId == id);
            await _context.AdminComments.DeleteManyAsync(c => c.ProjectId == id);

            return RedirectToAction("MyProjects");
        }

        public async Task<IActionResult> Details(string id)
        {
            var project = await _context.Projects.Find(p => p.Id == id).FirstOrDefaultAsync();
            if (project == null) return NotFound();

            var comments = await _context.Comments.Find(c => c.ProjectId == id)
                .SortByDescending(c => c.CreatedAt)
                .ToListAsync();
            ViewBag.Comments = comments;

            return View(project);
        }

        public async Task<IActionResult> MyProjects()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Account");

            var projects = await _context.Projects.Find(p => p.UserId == userId)
                .SortByDescending(p => p.UploadDate)
                .ToListAsync();
            return View(projects);
        }

        public async Task<IActionResult> Download(string id)
        {
            var project = await _context.Projects.Find(p => p.Id == id).FirstOrDefaultAsync();
            if (project == null) return NotFound();

            var update = Builders<Project>.Update.Inc(p => p.DownloadCount, 1);
            await _context.Projects.UpdateOneAsync(p => p.Id == id, update);

            var filePath = Path.Combine(_environment.WebRootPath, "uploads", "projects", project.FilePath);
            if (!System.IO.File.Exists(filePath)) return NotFound();

            var memory = new MemoryStream();
            using (var stream = new FileStream(filePath, FileMode.Open))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;

            return File(memory, "application/zip", project.FileName);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(string projectId, string text)
        {
            var userId = HttpContext.Session.GetString("UserId");
            var username = HttpContext.Session.GetString("Username");

            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Account");
            if (!string.IsNullOrEmpty(text))
            {
                var comment = new Comment
                {
                    ProjectId = projectId,
                    UserId = userId,
                    Username = username,
                    Text = text,
                    CreatedAt = DateTime.Now
                };
                await _context.Comments.InsertOneAsync(comment);
            }

            return RedirectToAction("Details", new { id = projectId });
        }
    }
}