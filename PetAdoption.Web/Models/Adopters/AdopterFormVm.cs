using System.ComponentModel.DataAnnotations;

namespace PetAdoption.Web.Models.Adopters
{
    public sealed class AdopterFormVm
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(128)]
        [Display(Name = "Full name")]
        public string FullName { get; set; } = string.Empty;

        [EmailAddress]
        [MaxLength(128)]
        [Display(Name = "Email address")]
        public string? Email { get; set; }

        [MaxLength(64)]
        [Display(Name = "Phone number")]
        public string? Phone { get; set; }

        [MaxLength(256)]
        [Display(Name = "Address")]
        public string? Address { get; set; }

        [MaxLength(512)]
        [Display(Name = "Administrative notes")]
        public string? Notes { get; set; }
    }
}