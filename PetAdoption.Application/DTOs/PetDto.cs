using PetAdoption.Domain.Enums;

namespace PetAdoption.Application.DTOs
{
    public record PetDto(
        int Id,
        string Name,
        Species Species,
        string? Breed,
        int AgeYears,
        string? Description,
        string? ImageUrl,
        PetStatus Status,
        int ShelterId,
        string ShelterName
    );
}