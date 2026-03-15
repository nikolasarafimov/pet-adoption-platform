using System.ComponentModel.DataAnnotations;
using PetAdoption.Application.DTOs;
using PetAdoption.Domain.Enums;

namespace PetAdoption.Web.Models.Pets
{
    public sealed class PetFormVm
    {
        public int Id { get; set; }

        [Required, MaxLength(128)]
        public string Name { get; set; } = "";

        [Required]
        public Species Species { get; set; }

        [MaxLength(128)]
        public string? Breed { get; set; }

        [Range(0, 200)]
        public int AgeYears { get; set; }

        [MaxLength(1024)]
        public string? Description { get; set; }

        [MaxLength(512)]
        [Url]
        public string? ImageUrl { get; set; }

        [Required]
        public int ShelterId { get; set; }

        [Required]
        public PetStatus Status { get; set; } = PetStatus.Available;

        public static PetFormVm FromDto(PetDto p) => new PetFormVm
        {
            Id = p.Id,
            Name = p.Name,
            Species = p.Species,
            Breed = p.Breed,
            AgeYears = p.AgeYears,
            Description = p.Description,
            ImageUrl = p.ImageUrl,
            ShelterId = p.ShelterId,
            Status = p.Status
        };
    }
}
