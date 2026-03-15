using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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