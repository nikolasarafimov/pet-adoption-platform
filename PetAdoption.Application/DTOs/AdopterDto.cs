namespace PetAdoption.Application.DTOs
{
    public record AdopterDto(
        int Id,
        string FullName,
        string? Email,
        string? Phone,
        string? Address,
        string? Notes
    );
}