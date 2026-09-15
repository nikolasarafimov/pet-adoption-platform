using System.ComponentModel.DataAnnotations;
using PetAdoption.Application.DTOs;
using PetAdoption.Domain.Enums;

namespace PetAdoption.Web.Models.Pets
{
    public sealed class PetFormVm
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(128)]
        public string Name { get; set; } = string.Empty;

        [EnumDataType(typeof(Species))]
        public Species Species { get; set; }

        [MaxLength(128)]
        public string? Breed { get; set; }

        [Range(
            0,
            200,
            ErrorMessage = "Age must be between 0 and 200 years."
        )]
        public int AgeYears { get; set; }

        [MaxLength(1024)]
        public string? Description { get; set; }

        [MaxLength(512)]
        [Url]
        public string? ImageUrl { get; set; }

        [Range(
            1,
            int.MaxValue,
            ErrorMessage = "A shelter must be selected."
        )]
        public int ShelterId { get; set; }

        [EnumDataType(typeof(PetStatus))]
        public PetStatus Status { get; set; }
            = PetStatus.Available;

        public static PetFormVm FromDto(PetDto pet)
        {
            return new PetFormVm
            {
                Id = pet.Id,
                Name = pet.Name,
                Species = pet.Species,
                Breed = pet.Breed,
                AgeYears = pet.AgeYears,
                Description = pet.Description,
                ImageUrl = pet.ImageUrl,
                ShelterId = pet.ShelterId,
                Status = pet.Status
            };
        }
    }
}