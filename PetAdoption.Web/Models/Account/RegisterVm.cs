using System.ComponentModel.DataAnnotations;

namespace PetAdoption.Web.Models.Account
{
    public class RegisterVm
    {
        [Display(Name = "Full name")]
        [MaxLength(128)]
        public string? FullName { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(256)]
        [Display(Name = "Email address")]
        public string? Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [MinLength(
            8,
            ErrorMessage = "Password must be at least 8 characters long."
        )]
        public string? Password { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Compare(
            nameof(Password),
            ErrorMessage = "Passwords do not match."
        )]
        [Display(Name = "Confirm password")]
        public string? ConfirmPassword { get; set; }

        public string? ReturnUrl { get; set; }

        [Display(Name = "Register as a shelter")]
        public bool IsShelter { get; set; }

        [Display(Name = "Shelter name")]
        [MaxLength(128)]
        public string? ShelterName { get; set; }

        [Display(Name = "Shelter address")]
        [MaxLength(256)]
        public string? ShelterAddress { get; set; }

        [Display(Name = "City")]
        [MaxLength(128)]
        public string? ShelterCity { get; set; }

        [Display(Name = "Phone")]
        [MaxLength(64)]
        public string? ShelterPhone { get; set; }

        [Display(Name = "Shelter email")]
        [EmailAddress]
        [MaxLength(128)]
        public string? ShelterEmail { get; set; }
    }
}