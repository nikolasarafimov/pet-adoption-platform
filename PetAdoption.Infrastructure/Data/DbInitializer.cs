using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PetAdoption.Domain.Entities;
using PetAdoption.Domain.Enums;

namespace PetAdoption.Infrastructure.Data
{
    public sealed record DemoSeedCredentials(
        string AdminEmail,
        string AdminPassword,
        string ShelterEmail,
        string ShelterPassword,
        string AdopterEmail,
        string AdopterPassword
    );

    public static class DbInitializer
    {
        private static readonly string[] RoleNames =
        {
            "Admin",
            "Shelter",
            "Adopter"
        };

        public static async Task SeedAsync(
            AppDbContext db,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            DemoSeedCredentials credentials
        )
        {
            await EnsureRolesAsync(roleManager);

            var shelters =
                await EnsureSheltersAsync(db);

            var happyTails =
                shelters.Single(shelter =>
                    shelter.Name == "Happy Tails Shelter"
                );

            var secondChance =
                shelters.Single(shelter =>
                    shelter.Name == "Second Chance"
                );

            var demoAdopter =
                await EnsureDemoAdopterAsync(
                    db,
                    credentials.AdopterEmail
                );

            await EnsureUserAsync(
                userManager,
                credentials.AdminEmail,
                credentials.AdminPassword,
                "Admin",
                "System Admin"
            );

            await EnsureUserAsync(
                userManager,
                credentials.ShelterEmail,
                credentials.ShelterPassword,
                "Shelter",
                "Happy Tails Demo Shelter",
                shelterId: happyTails.Id
            );

            var adopterUser =
                await EnsureUserAsync(
                    userManager,
                    credentials.AdopterEmail,
                    credentials.AdopterPassword,
                    "Adopter",
                    "Demo Adopter",
                    adopterId: demoAdopter.Id
                );

            if (demoAdopter.UserId != adopterUser.Id)
            {
                demoAdopter.UserId = adopterUser.Id;
                await db.SaveChangesAsync();
            }

            await EnsurePetsAsync(
                db,
                happyTails.Id,
                secondChance.Id
            );
        }

        private static async Task EnsureRolesAsync(
            RoleManager<IdentityRole> roleManager
        )
        {
            foreach (var roleName in RoleNames)
            {
                if (await roleManager.RoleExistsAsync(roleName))
                {
                    continue;
                }

                var result =
                    await roleManager.CreateAsync(
                        new IdentityRole(roleName)
                    );

                EnsureIdentitySucceeded(
                    result,
                    $"Unable to create role '{roleName}'."
                );
            }
        }

        private static async Task<List<Shelter>> EnsureSheltersAsync(
            AppDbContext db
        )
        {
            var happyTails =
                await db.Shelters.FirstOrDefaultAsync(
                    shelter =>
                        shelter.Name
                        == "Happy Tails Shelter"
                );

            if (happyTails == null)
            {
                happyTails = new Shelter
                {
                    Name = "Happy Tails Shelter",
                    Address = "123 Ilindenska",
                    City = "Veles",
                    Phone = "02-202-222",
                    Email = "contact@happytails.org"
                };

                db.Shelters.Add(happyTails);
            }

            var secondChance =
                await db.Shelters.FirstOrDefaultAsync(
                    shelter =>
                        shelter.Name
                        == "Second Chance"
                );

            if (secondChance == null)
            {
                secondChance = new Shelter
                {
                    Name = "Second Chance",
                    Address = "45 Nikola Karev",
                    City = "Skopje",
                    Phone = "02-211-321",
                    Email = "contact@secondchance.org"
                };

                db.Shelters.Add(secondChance);
            }

            await db.SaveChangesAsync();

            return new List<Shelter>
            {
                happyTails,
                secondChance
            };
        }

        private static async Task<Adopter> EnsureDemoAdopterAsync(
            AppDbContext db,
            string email
        )
        {
            var adopter =
                await db.Adopters.FirstOrDefaultAsync(
                    existing =>
                        existing.Email == email
                );

            if (adopter != null)
            {
                return adopter;
            }

            adopter = new Adopter
            {
                FullName = "Demo Adopter",
                Email = email,
                Phone = "070-000-001",
                Address = "Skopje",
                Notes = "Seeded demo adopter account"
            };

            db.Adopters.Add(adopter);
            await db.SaveChangesAsync();

            return adopter;
        }

        private static async Task<ApplicationUser> EnsureUserAsync(
            UserManager<ApplicationUser> userManager,
            string email,
            string password,
            string role,
            string fullName,
            int? shelterId = null,
            int? adopterId = null
        )
        {
            var user =
                await userManager.FindByEmailAsync(email);

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

                var createResult =
                    await userManager.CreateAsync(
                        user,
                        password
                    );

                EnsureIdentitySucceeded(
                    createResult,
                    $"Unable to create demo user '{email}'."
                );
            }
            else
            {
                var requiresUpdate = false;

                if (user.FullName != fullName)
                {
                    user.FullName = fullName;
                    requiresUpdate = true;
                }

                if (user.ShelterId != shelterId)
                {
                    user.ShelterId = shelterId;
                    requiresUpdate = true;
                }

                if (user.AdopterId != adopterId)
                {
                    user.AdopterId = adopterId;
                    requiresUpdate = true;
                }

                if (requiresUpdate)
                {
                    var updateResult =
                        await userManager.UpdateAsync(user);

                    EnsureIdentitySucceeded(
                        updateResult,
                        $"Unable to update demo user '{email}'."
                    );
                }
            }

            if (!await userManager.IsInRoleAsync(
                user,
                role
            ))
            {
                var roleResult =
                    await userManager.AddToRoleAsync(
                        user,
                        role
                    );

                EnsureIdentitySucceeded(
                    roleResult,
                    $"Unable to assign role '{role}' to '{email}'."
                );
            }

            var claims =
                await userManager.GetClaimsAsync(user);

            var existingDisplayName =
                claims.FirstOrDefault(claim =>
                    claim.Type == "display_name"
                );

            if (existingDisplayName == null)
            {
                var claimResult =
                    await userManager.AddClaimAsync(
                        user,
                        new Claim(
                            "display_name",
                            fullName
                        )
                    );

                EnsureIdentitySucceeded(
                    claimResult,
                    $"Unable to add display name claim to '{email}'."
                );
            }
            else if (existingDisplayName.Value != fullName)
            {
                var replaceResult =
                    await userManager.ReplaceClaimAsync(
                        user,
                        existingDisplayName,
                        new Claim(
                            "display_name",
                            fullName
                        )
                    );

                EnsureIdentitySucceeded(
                    replaceResult,
                    $"Unable to update display name claim for '{email}'."
                );
            }

            return user;
        }

        private static async Task EnsurePetsAsync(
            AppDbContext db,
            int happyTailsId,
            int secondChanceId
        )
        {
            await AddPetIfMissingAsync(
                db,
                new Pet
                {
                    Name = "Buddy",
                    Species = Species.Dog,
                    Breed = "Labrador Retriever",
                    AgeYears = 3,
                    Description =
                        "Friendly, playful, house-trained.",
                    ImageUrl =
                        "https://images.unsplash.com/photo-1543466835-00a7907e9de1?q=80&w=1200&auto=format&fit=crop",
                    Status = PetStatus.Reserved,
                    ShelterId = happyTailsId
                }
            );

            await AddPetIfMissingAsync(
                db,
                new Pet
                {
                    Name = "Milo",
                    Species = Species.Cat,
                    Breed = "British Shorthair",
                    AgeYears = 2,
                    Description =
                        "Calm, cuddly, indoor-friendly.",
                    ImageUrl =
                        "https://images.unsplash.com/photo-1592194996308-7b43878e84a6?q=80&w=1200&auto=format&fit=crop",
                    Status = PetStatus.Available,
                    ShelterId = happyTailsId
                }
            );

            await AddPetIfMissingAsync(
                db,
                new Pet
                {
                    Name = "Luna",
                    Species = Species.Dog,
                    Breed = "Border Collie",
                    AgeYears = 1,
                    Description =
                        "Smart, curious, loves long walks.",
                    ImageUrl =
                        "https://images.unsplash.com/photo-1548199973-03cce0bbc87b?q=80&w=1200&auto=format&fit=crop",
                    Status = PetStatus.Available,
                    ShelterId = secondChanceId
                }
            );

            await AddPetIfMissingAsync(
                db,
                new Pet
                {
                    Name = "Oliver",
                    Species = Species.Cat,
                    Breed = "Maine Coon",
                    AgeYears = 4,
                    Description =
                        "Big gentle boy. Great with kids.",
                    ImageUrl =
                        "https://images.unsplash.com/photo-1518791841217-8f162f1e1131?q=80&w=1200&auto=format&fit=crop",
                    Status = PetStatus.Adopted,
                    ShelterId = secondChanceId
                }
            );

            await AddPetIfMissingAsync(
                db,
                new Pet
                {
                    Name = "Coco",
                    Species = Species.Dog,
                    Breed = "Poodle",
                    AgeYears = 5,
                    Description =
                        "Hypoallergenic coat, well-trained.",
                    ImageUrl =
                        "https://images.unsplash.com/photo-1567752881298-894bb81f9379?q=80&w=1200&auto=format&fit=crop",
                    Status = PetStatus.Unavailable,
                    ShelterId = happyTailsId
                }
            );

            await AddPetIfMissingAsync(
                db,
                new Pet
                {
                    Name = "Nala",
                    Species = Species.Cat,
                    Breed = "Siamese",
                    AgeYears = 2,
                    Description =
                        "Talkative and affectionate.",
                    ImageUrl =
                        "https://images.unsplash.com/photo-1519052537078-e6302a4968d4?q=80&w=1200&auto=format&fit=crop",
                    Status = PetStatus.Reserved,
                    ShelterId = secondChanceId
                }
            );

            await AddPetIfMissingAsync(
                db,
                new Pet
                {
                    Name = "Rocky",
                    Species = Species.Dog,
                    Breed = "German Shepherd",
                    AgeYears = 6,
                    Description =
                        "Loyal guardian, basic obedience trained.",
                    ImageUrl =
                        "https://images.unsplash.com/photo-1552053831-71594a27632d?q=80&w=1200&auto=format&fit=crop",
                    Status = PetStatus.Available,
                    ShelterId = happyTailsId
                }
            );

            await db.SaveChangesAsync();
        }

        private static async Task AddPetIfMissingAsync(
            AppDbContext db,
            Pet pet
        )
        {
            var exists =
                await db.Pets.AnyAsync(existing =>
                    existing.Name == pet.Name
                    && existing.ShelterId
                    == pet.ShelterId
                );

            if (!exists)
            {
                db.Pets.Add(pet);
            }
        }

        private static void EnsureIdentitySucceeded(
            IdentityResult result,
            string message
        )
        {
            if (result.Succeeded)
            {
                return;
            }

            var details = string.Join(
                "; ",
                result.Errors.Select(
                    error => error.Description
                )
            );

            throw new InvalidOperationException(
                $"{message} {details}"
            );
        }
    }
}