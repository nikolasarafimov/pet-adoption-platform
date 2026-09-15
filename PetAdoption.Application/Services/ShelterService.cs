using PetAdoption.Application.DTOs;
using PetAdoption.Application.Interfaces;
using PetAdoption.Domain.Abstractions;
using PetAdoption.Domain.Entities;

namespace PetAdoption.Application.Services
{
    public class ShelterService : IShelterService
    {
        private readonly IRepository<Shelter> _shelters;

        public ShelterService(
            IRepository<Shelter> shelters
        )
        {
            _shelters = shelters;
        }

        public async Task<IReadOnlyList<ShelterDto>> GetAllAsync()
        {
            var shelters =
                await _shelters.ListAsync();

            return shelters
                .Select(Map)
                .ToList();
        }

        public async Task<ShelterDto?> GetAsync(int id)
        {
            var shelter =
                await _shelters.GetByIdAsync(id);

            return shelter == null
                ? null
                : Map(shelter);
        }

        public async Task<int> CreateAsync(
            string name,
            string address,
            string city,
            string? phone,
            string? email
        )
        {
            ValidateRequiredFields(
                name,
                address,
                city
            );

            var shelter = new Shelter
            {
                Name = name.Trim(),
                Address = address.Trim(),
                City = city.Trim(),
                Phone = NormalizeOptional(phone),
                Email = NormalizeOptional(email)
            };

            await _shelters.AddAsync(shelter);
            await _shelters.SaveChangesAsync();

            return shelter.Id;
        }

        public async Task UpdateAsync(
            int id,
            string name,
            string address,
            string city,
            string? phone,
            string? email
        )
        {
            ValidateRequiredFields(
                name,
                address,
                city
            );

            var shelter =
                await _shelters.GetByIdAsync(id)
                ?? throw new ArgumentException(
                    "Shelter not found."
                );

            shelter.Name = name.Trim();
            shelter.Address = address.Trim();
            shelter.City = city.Trim();
            shelter.Phone = NormalizeOptional(phone);
            shelter.Email = NormalizeOptional(email);

            await _shelters.UpdateAsync(shelter);
            await _shelters.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var shelter =
                await _shelters.GetByIdAsync(id)
                ?? throw new ArgumentException(
                    "Shelter not found."
                );

            await _shelters.DeleteAsync(shelter);
            await _shelters.SaveChangesAsync();
        }

        private static ShelterDto Map(
            Shelter shelter
        )
        {
            return new ShelterDto(
                shelter.Id,
                shelter.Name,
                shelter.Address,
                shelter.City,
                shelter.Phone,
                shelter.Email
            );
        }

        private static void ValidateRequiredFields(
            string name,
            string address,
            string city
        )
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException(
                    "Name is required."
                );
            }

            if (string.IsNullOrWhiteSpace(address))
            {
                throw new ArgumentException(
                    "Address is required."
                );
            }

            if (string.IsNullOrWhiteSpace(city))
            {
                throw new ArgumentException(
                    "City is required."
                );
            }
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