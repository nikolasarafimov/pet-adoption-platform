using System.ComponentModel.DataAnnotations;

namespace PetAdoption.Domain.Entities
{
    public class Shelter
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(128)]
        public string Name { get; set; } = default!;

        [Required]
        [MaxLength(256)]
        public string Address { get; set; } = default!;

        [Required]
        [MaxLength(128)]
        public string City { get; set; } = default!;

        [MaxLength(64)]
        public string? Phone { get; set; }

        [MaxLength(128)]
        [EmailAddress]
        public string? Email { get; set; }

        public ICollection<Pet> Pets { get; set; }
            = new List<Pet>();
    }
}