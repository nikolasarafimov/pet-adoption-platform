using System.ComponentModel.DataAnnotations;

namespace PetAdoption.Web.Models.Shelters
{
    public class ShelterFormVm
    {
        public int Id { get; set; }

        [Required, MaxLength(128)]
        public string Name { get; set; } = default!;

        [Required, MaxLength(256)]
        public string Address { get; set; } = default!;

        [Required, MaxLength(128)]
        public string City { get; set; } = default!;

        [MaxLength(64)]
        public string? Phone { get; set; }

        [EmailAddress, MaxLength(128)]
        public string? Email { get; set; }
    }
}