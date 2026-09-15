using System.ComponentModel.DataAnnotations;

namespace PetAdoption.Domain.Entities
{
    public class Adopter
    {
        public int Id { get; set; }

        [Required, MaxLength(128)]
        public string FullName { get; set; } = default!;

        [MaxLength(128), EmailAddress]
        public string? Email { get; set; }

        [MaxLength(64)]
        public string? Phone { get; set; }

        [MaxLength(256)]
        public string? Address { get; set; }

        [MaxLength(512)]
        public string? Notes { get; set; }

        public string? UserId { get; set; }
    }
}