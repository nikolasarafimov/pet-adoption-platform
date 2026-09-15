using System.ComponentModel.DataAnnotations;
using PetAdoption.Domain.Enums;

namespace PetAdoption.Domain.Entities
{
    public class Pet
    {
        public int Id { get; set; }

        [Required, MaxLength(128)]
        public string Name { get; set; } = default!;

        public Species Species { get; set; }

        [MaxLength(128)]
        public string? Breed { get; set; }

        [Range(0, int.MaxValue)]
        public int AgeYears { get; set; }

        [MaxLength(1024)]
        public string? Description { get; set; }

        [MaxLength(512)]
        public string? ImageUrl { get; set; }

        public PetStatus Status { get; set; }
            = PetStatus.Available;

        public int ShelterId { get; set; }

        public Shelter Shelter { get; set; }
            = default!;
    }
}