// Data/SeedData.cs
using ProjectHub.Models;
using MongoDB.Driver;
using Microsoft.Extensions.DependencyInjection;

namespace ProjectHub.Data
{
    public static class SeedData
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<MongoDbContext>();

            try
            {
                Console.WriteLine("Test verileri ekleniyor...");

                // Kullanıcıları ekle
                await SeedUsers(context);

                // Projeleri ekle
                await SeedProjects(context);

                // Yorumları ekle
                await SeedComments(context);

                Console.WriteLine("Test verileri başarıyla eklendi!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Test verileri eklenirken hata: {ex.Message}");
            }
        }

        private static async Task SeedUsers(MongoDbContext context)
        {
            var usersCollection = context.Users;

            if (await usersCollection.CountDocumentsAsync(_ => true) == 0)
            {
                var users = new List<User>
                {
                    new User
                    {
                        Id = "6670a8b9e4b1a23456789011",
                        Username = "admin",
                        Email = "admin@projecthub.com",
                        Password = "admin123",
                        CreatedAt = DateTime.Parse("2025-08-03T09:00:00.000Z"),
                        ProfileBio = "Sistem Yöneticisi - Projeleri inceler ve onaylarım",
                        ProfileImage = "8b630a96-a46d-43fb-8365-93bdf561c48e.jpg",
                        IsAdmin = true,
                        Followers = new List<string>(),
                        Following = new List<string>()
                    },
                    new User
                    {
                        Id = "6670a8b9e4b1a23456789013",
                        Username = "zeynep",
                        Email = "zeynep@projecthub.com",
                        Password = "zeynep123",
                        CreatedAt = DateTime.Parse("2025-10-29T11:15:00.000Z"),
                        ProfileBio = "Frontend Developer - React ve Bootstrap ile güzel arayüzler tasarlıyorum",
                        ProfileImage = "default-profile.png",
                        IsAdmin = false,
                        Followers = new List<string>(),
                        Following = new List<string>()
                    },
                    new User
                    {
                        Id = "68c1ae2dae4d74e48ceedf56",
                        Username = "aslı",
                        Email = "asli@projecthub.com",
                        Password = "asli123",
                        CreatedAt = DateTime.Parse("2025-08-05T16:58:21.258Z"),
                        ProfileBio = "",
                        ProfileImage = "default-profile.png",
                        IsAdmin = false,
                        Followers = new List<string>(),
                        Following = new List<string>()
                    }
                };

                await usersCollection.InsertManyAsync(users);
            }
        }

        private static async Task SeedProjects(MongoDbContext context)
        {
            var projectsCollection = context.Projects;

            if (await projectsCollection.CountDocumentsAsync(_ => true) == 0)
            {
                var projects = new List<Project>
                {
                    new Project
                    {
                        Id = "68b0f62b140397f30e170277",
                        Title = "React Uygulaması",
                        Description = "Modern React.js uygulaması. Bootstrap 5, animasyonlar ve dark mode desteği içerir.",
                        FileName = "H05.zip",
                        FilePath = "631127eb-ac46-4905-a51c-5ea1333299a0_H05.zip",
                        UploadDate = DateTime.Parse("2025-08-12T00:36:59.793Z"),
                        UserId = "6670a8b9e4b1a23456789013",
                        Username = "zeynep",
                        DownloadCount = 2,
                        IsApproved = false
                    }
                };

                await projectsCollection.InsertManyAsync(projects);
            }
        }

        private static async Task SeedComments(MongoDbContext context)
        {
            var commentsCollection = context.Comments;

            if (await commentsCollection.CountDocumentsAsync(_ => true) == 0)
            {
                var comments = new List<Comment>
                {
                    new Comment
                    {
                        Id = "6670aabce4b1a23456789234",
                        ProjectId = "6670a9cde4b1a23456789123",
                        UserId = "6670a8b9e4b1a23456789013",
                        Username = "zeynep",
                        Text = "Harika bir proje! Kod yapısı çok temiz ve anlaşılır. Özellikle MongoDB entegrasyonu çok başarılı olmuş.",
                        CreatedAt = DateTime.Parse("2024-06-16T15:30:00.000Z")
                    }
                };

                await commentsCollection.InsertManyAsync(comments);
            }
        }
    }
}