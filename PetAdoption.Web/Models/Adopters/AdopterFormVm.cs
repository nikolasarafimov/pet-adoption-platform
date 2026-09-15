using System.ComponentModel.DataAnnotations;

namespace PetAdoption.Web.Models.Adopters
{
    public class AdopterFormVm
    {
        public int Id { get; set; }

        [Required, MaxLength(128)]
        public string FullName { get; set; } = default!;

        [EmailAddress, MaxLength(128)]
        public string? Email { get; set; }

        [MaxLength(64)]
        public string? Phone { get; set; }

        [MaxLength(256)]
        public string? Address { get; set; }

        [MaxLength(512)]
        public string? Notes { get; set; }
    }
}