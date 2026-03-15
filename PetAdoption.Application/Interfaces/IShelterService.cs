using System.Security.Claims;
using PetAdoption.Application.DTOs;

namespace PetAdoption.Application.Interfaces
{
    public interface IShelterService
    {
        Task<IReadOnlyList<ShelterDto>> GetAllAsync();
        Task<ShelterDto?> GetAsync(int id);
        Task<int> CreateAsync(string name, string address, string city, string? phone, string? email);
        Task UpdateAsync(int id, string name, string address, string city, string? phone, string? email);
        Task DeleteAsync(int id);
        Task<int> GetMyShelterIdAsync(ClaimsPrincipal user);
    }
}