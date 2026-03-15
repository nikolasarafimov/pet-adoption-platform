using PetAdoption.Application.DTOs;
using PetAdoption.Application.Interfaces;
using PetAdoption.Domain.Abstractions;
using PetAdoption.Domain.Entities;
using System.Security.Claims;

namespace PetAdoption.Application.Services
{
    public class AdopterService : IAdopterService
    {
        private readonly IRepository<Adopter> _adopters;

        public AdopterService(IRepository<Adopter> adopters)
        {
            _adopters = adopters;
        }

        public async Task<IReadOnlyList<AdopterDto>> GetAllAsync()
        {
            var list = await _adopters.ListAsync();
            return list.Select(a => new AdopterDto(a.Id, a.FullName, a.Email, a.Phone, a.Address, a.Notes)).ToList();
        }

        public async Task<AdopterDto?> GetAsync(int id)
        {
            var a = await _adopters.GetByIdAsync(id);
            return a == null ? null : new AdopterDto(a.Id, a.FullName, a.Email, a.Phone, a.Address, a.Notes);
        }

        public async Task<int> CreateAsync(string fullName, string? email, string? phone, string? address, string? notes)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                throw new ArgumentException("Full name is required.");

            var a = new Adopter
            {
                FullName = fullName.Trim(),
                Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
                Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim(),
                Address = string.IsNullOrWhiteSpace(address) ? null : address.Trim(),
                Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim()
            };

            await _adopters.AddAsync(a);
            await _adopters.SaveChangesAsync();
            return a.Id;
        }

        public async Task UpdateAsync(int id, string fullName, string? email, string? phone, string? address, string? notes)
        {
            var a = await _adopters.GetByIdAsync(id) ?? throw new ArgumentException("Adopter not found");
            if (string.IsNullOrWhiteSpace(fullName))
                throw new ArgumentException("Full name is required.");

            a.FullName = fullName.Trim();
            a.Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
            a.Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
            a.Address = string.IsNullOrWhiteSpace(address) ? null : address.Trim();
            a.Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();

            await _adopters.UpdateAsync(a);
            await _adopters.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var a = await _adopters.GetByIdAsync(id) ?? throw new ArgumentException("Adopter not found");
            await _adopters.DeleteAsync(a);
            await _adopters.SaveChangesAsync();
        }

        public async Task<int> GetMyAdopterIdAsync(ClaimsPrincipal user)
        {
            var email = user.Identity?.Name;
            var displayName = user.FindFirst("display_name")?.Value;

            var all = await _adopters.ListAsync();

            var existing = all.FirstOrDefault(a =>
                (!string.IsNullOrWhiteSpace(email) && a.Email == email) ||
                (!string.IsNullOrWhiteSpace(displayName) && a.FullName == displayName));

            if (existing != null)
                return existing.Id;

            var adopter = new Adopter
            {
                FullName = !string.IsNullOrWhiteSpace(displayName) ? displayName : (email ?? "Adopter"),
                Email = email
            };

            await _adopters.AddAsync(adopter);
            await _adopters.SaveChangesAsync();
            return adopter.Id;
        }
    }
}