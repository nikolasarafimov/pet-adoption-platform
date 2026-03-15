using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using PetAdoption.Domain.Entities;
using PetAdoption.Domain.Enums;

namespace PetAdoption.Infrastructure.Data
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(AppDbContext db, UserManager<ApplicationUser> users, RoleManager<IdentityRole> roles)
        {
            var roleNames = new[] { "Admin", "Shelter", "Adopter" };
            foreach (var r in roleNames)
            {
                if (!await roles.RoleExistsAsync(r))
                    await roles.CreateAsync(new IdentityRole(r));
            }

            var adminEmail = "admin@pets.com";
            var admin = await users.FindByEmailAsync(adminEmail);
            if (admin == null)
            {
                admin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    FullName = "System Admin"
                };

                await users.CreateAsync(admin, "Admin#12345");
                await users.AddToRoleAsync(admin, "Admin");
                await users.AddClaimAsync(admin, new Claim("display_name", "System Admin"));
            }

            if (!db.Shelters.Any())
            {
                var s1 = new Shelter
                {
                    Name = "Happy Tails Shelter",
                    Address = "123 Ilindenska",
                    City = "Veles",
                    Phone = "02-202-222",
                    Email = "contact@happytails.org"
                };

                var s2 = new Shelter
                {
                    Name = "Second Chance",
                    Address = "45 Nikola Karev",
                    City = "Skopje",
                    Phone = "02-211-321",
                    Email = "contact@secondchance.org"
                };

                db.Shelters.AddRange(s1, s2);
                await db.SaveChangesAsync();
            }

            var shelters = db.Shelters.ToList();
            int S(string name) => shelters.First(s => s.Name == name).Id;

            async Task EnsureUserAsync(
                string email,
                string password,
                string role,
                string? fullName = null,
                int? shelterId = null,
                int? adopterId = null)
            {
                var user = await users.FindByEmailAsync(email);
                if (user == null)
                {
                    user = new ApplicationUser
                    {
                        UserName = email,
                        Email = email,
                        EmailConfirmed = true,
                        FullName = fullName,
                        ShelterId = shelterId,
                        AdopterId = adopterId
                    };

                    var result = await users.CreateAsync(user, password);
                    if (result.Succeeded)
                    {
                        await users.AddToRoleAsync(user, role);

                        if (!string.IsNullOrWhiteSpace(fullName))
                        {
                            await users.AddClaimAsync(user, new Claim("display_name", fullName));
                        }
                    }
                }
            }

            var demoAdopter = db.Adopters.FirstOrDefault(a => a.Email == "adopter@pets.com");
            if (demoAdopter == null)
            {
                demoAdopter = new Adopter
                {
                    FullName = "Demo Adopter",
                    Email = "adopter@pets.com",
                    Phone = "070-000-001",
                    Address = "Skopje",
                    Notes = "Seeded demo adopter account"
                };

                db.Adopters.Add(demoAdopter);
                await db.SaveChangesAsync();
            }

            await EnsureUserAsync(
                email: "shelter@pets.com",
                password: "Shelter#123",
                role: "Shelter",
                fullName: "Happy Tails Demo Shelter",
                shelterId: S("Happy Tails Shelter")
            );

            await EnsureUserAsync(
                email: "adopter@pets.com",
                password: "Adopter#123",
                role: "Adopter",
                fullName: "Demo Adopter",
                adopterId: demoAdopter.Id
            );

            void AddPetIfMissing(Pet p)
            {
                if (!db.Pets.Any(x => x.Name == p.Name))
                    db.Pets.Add(p);
            }

            AddPetIfMissing(new Pet
            {
                Name = "Buddy",
                Species = Species.Dog,
                Breed = "Labrador Retriever",
                AgeYears = 3,
                Description = "Friendly, playful, house-trained.",
                ImageUrl = "https://images.unsplash.com/photo-1543466835-00a7907e9de1?q=80&w=1200&auto=format&fit=crop",
                Status = PetStatus.Reserved,
                ShelterId = S("Happy Tails Shelter")
            });

            AddPetIfMissing(new Pet
            {
                Name = "Milo",
                Species = Species.Cat,
                Breed = "British Shorthair",
                AgeYears = 2,
                Description = "Calm, cuddly, indoor-friendly.",
                ImageUrl = "https://images.unsplash.com/photo-1592194996308-7b43878e84a6?q=80&w=1200&auto=format&fit=crop",
                Status = PetStatus.Available,
                ShelterId = S("Happy Tails Shelter")
            });

            AddPetIfMissing(new Pet
            {
                Name = "Luna",
                Species = Species.Dog,
                Breed = "Border Collie",
                AgeYears = 1,
                Description = "Smart, curious, loves long walks.",
                ImageUrl = "https://images.unsplash.com/photo-1548199973-03cce0bbc87b?q=80&w=1200&auto=format&fit=crop",
                Status = PetStatus.Available,
                ShelterId = S("Second Chance")
            });

            AddPetIfMissing(new Pet
            {
                Name = "Oliver",
                Species = Species.Cat,
                Breed = "Maine Coon",
                AgeYears = 4,
                Description = "Big gentle boy. Great with kids.",
                ImageUrl = "https://images.unsplash.com/photo-1518791841217-8f162f1e1131?q=80&w=1200&auto=format&fit=crop",
                Status = PetStatus.Adopted,
                ShelterId = S("Second Chance")
            });

            AddPetIfMissing(new Pet
            {
                Name = "Coco",
                Species = Species.Dog,
                Breed = "Poodle",
                AgeYears = 5,
                Description = "Hypoallergenic coat, well-trained.",
                ImageUrl = "https://images.unsplash.com/photo-1567752881298-894bb81f9379?q=80&w=1200&auto=format&fit=crop",
                Status = PetStatus.Unavailable,
                ShelterId = S("Happy Tails Shelter")
            });

            AddPetIfMissing(new Pet
            {
                Name = "Nala",
                Species = Species.Cat,
                Breed = "Siamese",
                AgeYears = 2,
                Description = "Talkative and affectionate.",
                ImageUrl = "https://images.unsplash.com/photo-1519052537078-e6302a4968d4?q=80&w=1200&auto=format&fit=crop",
                Status = PetStatus.Reserved,
                ShelterId = S("Second Chance")
            });

            AddPetIfMissing(new Pet
            {
                Name = "Rocky",
                Species = Species.Dog,
                Breed = "German Shepherd",
                AgeYears = 6,
                Description = "Loyal guardian, basic obedience trained.",
                ImageUrl = "https://images.unsplash.com/photo-1552053831-71594a27632d?q=80&w=1200&auto=format&fit=crop",
                Status = PetStatus.Available,
                ShelterId = S("Happy Tails Shelter")
            });

            await db.SaveChangesAsync();
        }
    }
}