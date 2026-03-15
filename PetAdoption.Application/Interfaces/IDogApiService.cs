using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PetAdoption.Application.DTOs;

namespace PetAdoption.Application.Interfaces
{
    public interface IDogApiService
    {
        Task<IReadOnlyList<BreedDto>> GetBreedsAsync(string? nameLike = null, int take = 20);
        Task<BreedDto?> GetBreedByExactNameAsync(string name);
    }
}