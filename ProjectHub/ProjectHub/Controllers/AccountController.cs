using Microsoft.AspNetCore.Mvc;
using ProjectHub.Models;
using ProjectHub.Data;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
using System.Diagnostics;

namespace ProjectHub.Controllers
{
    public class AccountController : Controller
    {
        private readonly MongoDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AccountController(MongoDbContext context, IWebHostEnvironment environment, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _environment = environment;
            _httpContextAccessor = httpContextAccessor;
        }

        // Helper method for session operations
        private string GetSessionString(string key)
        {
            return _httpContextAccessor.HttpContext?.Session.GetString(key);
        }

        private void SetSessionString(string key, string value)
        {
            _httpContextAccessor.HttpContext?.Session.SetString(key, value);
        }

        private void ClearSession()
        {
            _httpContextAccessor.HttpContext?.Session.Clear();
        }

        // GET: Account/Register
        public IActionResult Register()
        {
            if (!string.IsNullOrEmpty(GetSessionString("UserId")))
            {
                return RedirectToAction("Index", "Home");
            }
            ViewBag.IsAuthPage = true;
            return View();
        }

        // POST: Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(User user, string confirmPassword)
        {
            ViewBag.IsAuthPage = true;

            if (!string.IsNullOrEmpty(GetSessionString("UserId")))
            {
                return RedirectToAction("Index", "Home");
            }

            if (user.Password != confirmPassword)
            {
                ModelState.AddModelError("", "Şifreler eşleşmiyor.");
                return View(user);
            }

            if (ModelState.IsValid)
            {
                var existingUserFilter = Builders<User>.Filter.Or(
                    Builders<User>.Filter.Regex(u => u.Username, new MongoDB.Bson.BsonRegularExpression($"^{user.Username}$", "i")),
                    Builders<User>.Filter.Regex(u => u.Email, new MongoDB.Bson.BsonRegularExpression($"^{user.Email}$", "i"))
                );

                var existingUser = await _context.Users.Find(existingUserFilter).FirstOrDefaultAsync();

                if (existingUser != null)
                {
                    ModelState.AddModelError("", "Kullanıcı adı veya e-posta zaten kullanımda.");
                    return View(user);
                }

                user.IsAdmin = false;
                user.CreatedAt = DateTime.Now;
                user.ProfileImage = "default-profile.png";
                user.Followers = new List<string>();
                user.Following = new List<string>();

                await _context.Users.InsertOneAsync(user);

                SetSessionString("UserId", user.Id);
                SetSessionString("Username", user.Username);
                SetSessionString("IsAdmin", user.IsAdmin.ToString());

                TempData["SuccessMessage"] = "Kayıt işlemi başarılı! Hoş geldiniz.";
                return RedirectToAction("Index", "Home");
            }
            return View(user);
        }

        // GET: Account/Login
        public IActionResult Login()
        {
            if (!string.IsNullOrEmpty(GetSessionString("UserId")))
            {
                return RedirectToAction("Index", "Home");
            }
            ViewBag.IsAuthPage = true;
            return View();
        }

        // POST: Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password)
        {
            ViewBag.IsAuthPage = true;

            if (!string.IsNullOrEmpty(GetSessionString("UserId")))
            {
                return RedirectToAction("Index", "Home");
            }

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ModelState.AddModelError("", "Kullanıcı adı ve şifre gereklidir.");
                return View();
            }

            var userFilter = Builders<User>.Filter.And(
                Builders<User>.Filter.Regex(u => u.Username, new MongoDB.Bson.BsonRegularExpression($"^{username}$", "i")),
                Builders<User>.Filter.Eq(u => u.Password, password)
            );

            var user = await _context.Users.Find(userFilter).FirstOrDefaultAsync();

            if (user != null)
            {
                SetSessionString("UserId", user.Id);
                SetSessionString("Username", user.Username);
                SetSessionString("IsAdmin", user.IsAdmin.ToString());

                TempData["SuccessMessage"] = "Giriş başarılı! Hoş geldiniz.";

                if (user.IsAdmin)
                    return RedirectToAction("Dashboard", "Admin");
                else
                    return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError("", "Kullanıcı adı veya şifre hatalı.");
            return View();
        }

        // GET: Account/Logout
        public IActionResult Logout()
        {
            ClearSession();
            TempData["SuccessMessage"] = "Çıkış yapıldı. Tekrar görüşmek üzere!";
            return RedirectToAction("Index", "Home");
        }

        // GET: Account/Profile/{username}
        public async Task<IActionResult> Profile(string username)
        {
            try
            {
                var requestedUsername = !string.IsNullOrEmpty(username) ? username :
                                       !string.IsNullOrEmpty(Request.Query["username"]) ? Request.Query["username"].ToString() :
                                       GetSessionString("Username");

                if (string.IsNullOrEmpty(requestedUsername))
                {
                    return RedirectToAction("Login");
                }

                var userFilter = Builders<User>.Filter.Regex(u => u.Username,
                    new MongoDB.Bson.BsonRegularExpression($"^{requestedUsername}$", "i"));

                var user = await _context.Users.Find(userFilter).FirstOrDefaultAsync();

                if (user == null)
                {
                    return NotFound();
                }

                var projectFilter = Builders<Project>.Filter.Regex(p => p.Username,
                    new MongoDB.Bson.BsonRegularExpression($"^{user.Username}$", "i"));

                var projects = await _context.Projects.Find(projectFilter).ToListAsync();

                // Takip durumunu kontrol et
                var currentUserId = GetSessionString("UserId");
                if (!string.IsNullOrEmpty(currentUserId))
                {
                    user.IsFollowedByCurrentUser = user.Followers.Contains(currentUserId);
                }

                ViewBag.UserProjects = projects ?? new List<Project>();
                return View(user);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Profil sayfası hatası: {ex.Message}");
                ViewBag.UserProjects = new List<Project>();

                var currentUserId = GetSessionString("UserId");
                if (!string.IsNullOrEmpty(currentUserId))
                {
                    var currentUser = await _context.Users.Find(u => u.Id == currentUserId).FirstOrDefaultAsync();
                    if (currentUser != null)
                    {
                        return View(currentUser);
                    }
                }

                return View("Error", new ErrorViewModel
                {
                    RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                    ErrorDetails = ex.Message
                });
            }
        }

        // GET: Account/EditProfile
        public async Task<IActionResult> EditProfile()
        {
            var userId = GetSessionString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                TempData["ErrorMessage"] = "Profil düzenlemek için giriş yapmalısınız.";
                return RedirectToAction("Login");
            }

            var user = await _context.Users.Find(u => u.Id == userId).FirstOrDefaultAsync();
            if (user == null)
            {
                TempData["ErrorMessage"] = "Kullanıcı bulunamadı.";
                return RedirectToAction("Login");
            }

            return View(user);
        }

        // POST: Account/EditProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(User updatedUser, IFormFile profileImage, string removeProfileImage = "false")
        {
            var userId = GetSessionString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                TempData["ErrorMessage"] = "Profil düzenlemek için giriş yapmalısınız.";
                return RedirectToAction("Login");
            }

            var user = await _context.Users.Find(u => u.Id == userId).FirstOrDefaultAsync();
            if (user == null)
            {
                TempData["ErrorMessage"] = "Kullanıcı bulunamadı.";
                return RedirectToAction("Login");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Email değişiklik kontrolü
                    if (user.Email != updatedUser.Email)
                    {
                        var emailFilter = Builders<User>.Filter.And(
                            Builders<User>.Filter.Regex(u => u.Email, new MongoDB.Bson.BsonRegularExpression($"^{updatedUser.Email}$", "i")),
                            Builders<User>.Filter.Ne(u => u.Id, userId)
                        );

                        var existingEmail = await _context.Users.Find(emailFilter).FirstOrDefaultAsync();
                        if (existingEmail != null)
                        {
                            ModelState.AddModelError("Email", "Bu e-posta adresi zaten kullanılıyor.");
                            return View(user);
                        }
                        user.Email = updatedUser.Email;
                    }

                    // Profil resmi kaldırma
                    if (removeProfileImage == "true")
                    {
                        if (!string.IsNullOrEmpty(user.ProfileImage) && user.ProfileImage != "default-profile.png")
                        {
                            var oldFilePath = Path.Combine(_environment.WebRootPath, "uploads", "profiles", user.ProfileImage);
                            if (System.IO.File.Exists(oldFilePath))
                            {
                                System.IO.File.Delete(oldFilePath);
                            }
                        }
                        user.ProfileImage = "default-profile.png";
                    }
                    // Yeni profil resmi yükleme
                    else if (profileImage != null && profileImage.Length > 0)
                    {
                        // Dosya boyutu kontrolü (5MB)
                        if (profileImage.Length > 5 * 1024 * 1024)
                        {
                            ModelState.AddModelError("", "Profil resmi maksimum 5MB olabilir.");
                            return View(user);
                        }

                        // Dosya formatı kontrolü
                        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                        var extension = Path.GetExtension(profileImage.FileName).ToLower();
                        if (!allowedExtensions.Contains(extension))
                        {
                            ModelState.AddModelError("", "Sadece JPG, JPEG, PNG ve GIF formatları kabul edilir.");
                            return View(user);
                        }

                        // Upload klasörünü oluştur
                        var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "profiles");
                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        // Eski resmi sil
                        if (!string.IsNullOrEmpty(user.ProfileImage) && user.ProfileImage != "default-profile.png")
                        {
                            var oldFilePath = Path.Combine(uploadsFolder, user.ProfileImage);
                            if (System.IO.File.Exists(oldFilePath))
                            {
                                System.IO.File.Delete(oldFilePath);
                            }
                        }

                        // Yeni resmi kaydet
                        var uniqueFileName = $"{Guid.NewGuid()}{extension}";
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await profileImage.CopyToAsync(fileStream);
                        }

                        user.ProfileImage = uniqueFileName;
                    }

                    // Diğer bilgileri güncelle
                    user.ProfileBio = updatedUser.ProfileBio?.Trim();

                    // Veritabanını güncelle
                    await _context.Users.ReplaceOneAsync(u => u.Id == userId, user);

                    // Başarı mesajı
                    TempData["SuccessMessage"] = "Profil bilgileriniz başarıyla güncellendi.";

                    // Profile sayfasına yönlendir
                    return RedirectToAction("Profile", new { username = user.Username });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Profil güncelleme hatası: {ex.Message}");
                    ModelState.AddModelError("", "Profil güncellenirken bir hata oluştu. Lütfen tekrar deneyin.");
                    return View(user);
                }
            }

            // ModelState valid değilse
            return View(user);
        }

        // POST: Account/FollowUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FollowUser(string username)
        {
            var currentUserId = GetSessionString("UserId");
            if (string.IsNullOrEmpty(currentUserId))
            {
                return RedirectToAction("Login");
            }

            try
            {
                var userToFollowFilter = Builders<User>.Filter.Regex(u => u.Username,
                    new MongoDB.Bson.BsonRegularExpression($"^{username}$", "i"));
                var userToFollow = await _context.Users.Find(userToFollowFilter).FirstOrDefaultAsync();

                if (userToFollow == null || userToFollow.Id == currentUserId)
                {
                    return NotFound();
                }

                var currentUser = await _context.Users.Find(u => u.Id == currentUserId).FirstOrDefaultAsync();

                if (!userToFollow.Followers.Contains(currentUserId))
                {
                    userToFollow.Followers.Add(currentUserId);
                    currentUser.Following.Add(userToFollow.Id);

                    await _context.Users.ReplaceOneAsync(u => u.Id == userToFollow.Id, userToFollow);
                    await _context.Users.ReplaceOneAsync(u => u.Id == currentUserId, currentUser);
                }

                return RedirectToAction("Profile", new { username });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Follow error: {ex.Message}");
                return RedirectToAction("Profile", new { username });
            }
        }

        // POST: Account/UnfollowUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnfollowUser(string username)
        {
            var currentUserId = GetSessionString("UserId");
            if (string.IsNullOrEmpty(currentUserId))
            {
                return RedirectToAction("Login");
            }

            try
            {
                var userToUnfollowFilter = Builders<User>.Filter.Regex(u => u.Username,
                    new MongoDB.Bson.BsonRegularExpression($"^{username}$", "i"));
                var userToUnfollow = await _context.Users.Find(userToUnfollowFilter).FirstOrDefaultAsync();

                if (userToUnfollow == null)
                {
                    return NotFound();
                }

                var currentUser = await _context.Users.Find(u => u.Id == currentUserId).FirstOrDefaultAsync();

                userToUnfollow.Followers.Remove(currentUserId);
                currentUser.Following.Remove(userToUnfollow.Id);

                await _context.Users.ReplaceOneAsync(u => u.Id == userToUnfollow.Id, userToUnfollow);
                await _context.Users.ReplaceOneAsync(u => u.Id == currentUserId, currentUser);

                return RedirectToAction("Profile", new { username });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unfollow error: {ex.Message}");
                return RedirectToAction("Profile", new { username });
            }
        }

        // DEBUG: Kullanıcı kontrolü
        public async Task<IActionResult> CheckUser(string username)
        {
            try
            {
                var userFilter = Builders<User>.Filter.Regex(u => u.Username,
                    new MongoDB.Bson.BsonRegularExpression($"^{username}$", "i"));

                var user = await _context.Users.Find(userFilter).FirstOrDefaultAsync();

                return Json(new
                {
                    Exists = user != null,
                    User = user,
                    RequestedUsername = username,
                    ActualUsername = user?.Username
                });
            }
            catch (Exception ex)
            {
                return Json(new { Error = ex.Message });
            }
        }
    }
}