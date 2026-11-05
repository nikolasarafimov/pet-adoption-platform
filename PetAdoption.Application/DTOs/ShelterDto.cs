namespace PetAdoption.Application.DTOs
{
    public record ShelterDto(
        int Id,
        string Name,
        string Address,
        string City,
        string? Phone,
        string? Email
    );
}