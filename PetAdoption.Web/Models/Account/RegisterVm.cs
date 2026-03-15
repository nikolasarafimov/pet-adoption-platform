using System.ComponentModel.DataAnnotations;

namespace PetAdoption.Web.Models.Account
{
    public class RegisterVm
    {
        [Display(Name = "Full name")]
        public string? FullName { get; set; }

        [Required, EmailAddress]
        public string? Email { get; set; }

        [Required, DataType(DataType.Password)]
        [MinLength(6)]
        public string? Password { get; set; }

        [Required, DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
        public string? ConfirmPassword { get; set; }

        public string? ReturnUrl { get; set; }

        [Display(Name = "Register as a Shelter")]
        public bool IsShelter { get; set; }

        [Display(Name = "Shelter name")]
        public string? ShelterName { get; set; }

        [Display(Name = "Shelter address")]
        public string? ShelterAddress { get; set; }

        [Display(Name = "City")]
        public string? ShelterCity { get; set; }

        [Display(Name = "Phone")]
        public string? ShelterPhone { get; set; }

        [Display(Name = "Shelter email")]
        public string? ShelterEmail { get; set; }
    }
}
