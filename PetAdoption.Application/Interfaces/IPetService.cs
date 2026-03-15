using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PetAdoption.Application.DTOs;
using PetAdoption.Domain.Enums;

namespace PetAdoption.Application.Interfaces
{
    public interface IPetService
    {
        Task<IReadOnlyList<PetDto>> GetAllAsync(string? search = null, Species? species = null, int? shelterId = null);
        Task<PetDto?> GetAsync(int id);
        Task<int> CreateAsync(string name, Species species, string? breed, int ageYears, string? description, string? imageUrl, int shelterId);
        Task UpdateAsync(int id, string name, Species species, string? breed, int ageYears, string? description, string? imageUrl, int shelterId, PetStatus status);
        Task DeleteAsync(int id);
    }
}