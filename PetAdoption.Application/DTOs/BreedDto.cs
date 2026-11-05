namespace PetAdoption.Application.DTOs
{
    public record BreedDto(
        string Name,
        string[] Temperaments,
        int? LifeSpanMinYears,
        int? LifeSpanMaxYears,
        double? WeightKgAverage,
        string? ImageUrl
    );
}