using Microsoft.AspNetCore.Identity;

namespace PetAdoption.Infrastructure.Data
{
    public class ApplicationUser : IdentityUser
    {
        public int? AdopterId { get; set; }

        public int? ShelterId { get; set; }

        public string? FullName { get; set; }
    }
}