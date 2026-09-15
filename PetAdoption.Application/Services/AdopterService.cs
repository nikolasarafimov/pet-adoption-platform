using PetAdoption.Application.DTOs;
using PetAdoption.Application.Interfaces;
using PetAdoption.Domain.Abstractions;
using PetAdoption.Domain.Entities;

namespace PetAdoption.Application.Services
{
    public class AdopterService : IAdopterService
    {
        private readonly IRepository<Adopter> _adopters;

        public AdopterService(
            IRepository<Adopter> adopters
        )
        {
            _adopters = adopters;
        }

        public async Task<IReadOnlyList<AdopterDto>> GetAllAsync()
        {
            var adopters =
                await _adopters.ListAsync();

            return adopters
                .Select(Map)
                .ToList();
        }

        public async Task<AdopterDto?> GetAsync(int id)
        {
            var adopter =
                await _adopters.GetByIdAsync(id);

            return adopter == null
                ? null
                : Map(adopter);
        }

        public async Task<int> CreateAsync(
            string fullName,
            string? email,
            string? phone,
            string? address,
            string? notes
        )
        {
            if (string.IsNullOrWhiteSpace(fullName))
            {
                throw new ArgumentException(
                    "Full name is required."
                );
            }

            var adopter = new Adopter
            {
                FullName = fullName.Trim(),
                Email = NormalizeOptional(email),
                Phone = NormalizeOptional(phone),
                Address = NormalizeOptional(address),
                Notes = NormalizeOptional(notes)
            };

            await _adopters.AddAsync(adopter);
            await _adopters.SaveChangesAsync();

            return adopter.Id;
        }

        public async Task UpdateAsync(
            int id,
            string fullName,
            string? email,
            string? phone,
            string? address,
            string? notes
        )
        {
            if (string.IsNullOrWhiteSpace(fullName))
            {
                throw new ArgumentException(
                    "Full name is required."
                );
            }

            var adopter =
                await _adopters.GetByIdAsync(id)
                ?? throw new ArgumentException(
                    "Adopter not found."
                );

            adopter.FullName = fullName.Trim();
            adopter.Email = NormalizeOptional(email);
            adopter.Phone = NormalizeOptional(phone);
            adopter.Address = NormalizeOptional(address);
            adopter.Notes = NormalizeOptional(notes);

            await _adopters.UpdateAsync(adopter);
            await _adopters.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var adopter =
                await _adopters.GetByIdAsync(id)
                ?? throw new ArgumentException(
                    "Adopter not found."
                );

            await _adopters.DeleteAsync(adopter);
            await _adopters.SaveChangesAsync();
        }

        private static AdopterDto Map(
            Adopter adopter
        )
        {
            return new AdopterDto(
                adopter.Id,
                adopter.FullName,
                adopter.Email,
                adopter.Phone,
                adopter.Address,
                adopter.Notes
            );
        }

        private static string? NormalizeOptional(
            string? value
        )
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }
    }
}