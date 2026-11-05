using System.ComponentModel.DataAnnotations;

namespace PetAdoption.Web.Models.Shelters
{
    public sealed class ShelterFormVm
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(128)]
        [Display(Name = "Shelter name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(256)]
        [Display(Name = "Address")]
        public string Address { get; set; } = string.Empty;

        [Required]
        [MaxLength(128)]
        [Display(Name = "City")]
        public string City { get; set; } = string.Empty;

        [MaxLength(64)]
        [Display(Name = "Phone number")]
        public string? Phone { get; set; }

        [EmailAddress]
        [MaxLength(128)]
        [Display(Name = "Email address")]
        public string? Email { get; set; }
    }
}