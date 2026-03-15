using PetAdoption.Application.DTOs;
using PetAdoption.Application.Interfaces;
using PetAdoption.Domain.Abstractions;
using PetAdoption.Domain.Entities;
using System.Security.Claims;

namespace PetAdoption.Application.Services
{
    public class ShelterService : IShelterService
    {
        private readonly IRepository<Shelter> _shelters;

        public ShelterService(IRepository<Shelter> shelters)
        {
            _shelters = shelters;
        }

        public async Task<IReadOnlyList<ShelterDto>> GetAllAsync()
        {
            var list = await _shelters.ListAsync();
            return list.Select(s => new ShelterDto(s.Id, s.Name, s.Address, s.City, s.Phone, s.Email)).ToList();
        }

        public async Task<ShelterDto?> GetAsync(int id)
        {
            var s = await _shelters.GetByIdAsync(id);
            return s == null ? null : new ShelterDto(s.Id, s.Name, s.Address, s.City, s.Phone, s.Email);
        }

        public async Task<int> CreateAsync(string name, string address, string city, string? phone, string? email)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required.");
            if (string.IsNullOrWhiteSpace(address)) throw new ArgumentException("Address is required.");
            if (string.IsNullOrWhiteSpace(city)) throw new ArgumentException("City is required.");

            var s = new Shelter
            {
                Name = name.Trim(),
                Address = address.Trim(),
                City = city.Trim(),
                Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim(),
                Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim()
            };

            await _shelters.AddAsync(s);
            await _shelters.SaveChangesAsync();
            return s.Id;
        }

        public async Task UpdateAsync(int id, string name, string address, string city, string? phone, string? email)
        {
            var s = await _shelters.GetByIdAsync(id) ?? throw new ArgumentException("Shelter not found");

            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required.");
            if (string.IsNullOrWhiteSpace(address)) throw new ArgumentException("Address is required.");
            if (string.IsNullOrWhiteSpace(city)) throw new ArgumentException("City is required.");

            s.Name = name.Trim();
            s.Address = address.Trim();
            s.City = city.Trim();
            s.Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
            s.Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim();

            await _shelters.UpdateAsync(s);
            await _shelters.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var s = await _shelters.GetByIdAsync(id) ?? throw new ArgumentException("Shelter not found");
            await _shelters.DeleteAsync(s);
            await _shelters.SaveChangesAsync();
        }

        public async Task<int> GetMyShelterIdAsync(ClaimsPrincipal user)
        {
            var name = user.FindFirst("display_name")?.Value ?? user.Identity?.Name;

            var shelters = await _shelters.ListAsync();

            var exactEmail = shelters.FirstOrDefault(s =>
                !string.IsNullOrWhiteSpace(user.Identity?.Name) &&
                s.Email == user.Identity!.Name);

            if (exactEmail != null)
                return exactEmail.Id;

            var exactName = shelters.FirstOrDefault(s =>
                !string.IsNullOrWhiteSpace(name) &&
                s.Name == name);

            if (exactName != null)
                return exactName.Id;

            var first = shelters.FirstOrDefault()
                ?? throw new InvalidOperationException("No shelters exist.");

            return first.Id;
        }
    }
}