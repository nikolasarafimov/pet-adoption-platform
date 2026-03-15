using System.Security.Claims;
using PetAdoption.Application.DTOs;

namespace PetAdoption.Application.Interfaces
{
    public interface IAdopterService
    {
        Task<IReadOnlyList<AdopterDto>> GetAllAsync();
        Task<AdopterDto?> GetAsync(int id);
        Task<int> CreateAsync(string fullName, string? email, string? phone, string? address, string? notes);
        Task UpdateAsync(int id, string fullName, string? email, string? phone, string? address, string? notes);
        Task DeleteAsync(int id);
        Task<int> GetMyAdopterIdAsync(ClaimsPrincipal user);
    }
}