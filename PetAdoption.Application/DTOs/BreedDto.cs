using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PetAdoption.Application.DTOs
{
    public record BreedDto
    (
        string Name,
        string[] Temperaments,
        int? LifeSpanMinYears,
        int? LifeSpanMaxYears,
        double? WeightKgAverage,
        string? ImageUrl
    );
}