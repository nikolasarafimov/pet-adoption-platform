using System.ComponentModel.DataAnnotations;
using PetAdoption.Application.DTOs;
using PetAdoption.Application.Interfaces;
using PetAdoption.Domain.Abstractions;
using PetAdoption.Domain.Entities;

namespace PetAdoption.Application.Services
{
    public class ShelterService : IShelterService
    {
        private readonly IRepository<Shelter> _shelters;
        private readonly IRepository<Pet> _pets;

        public ShelterService(
            IRepository<Shelter> shelters,
            IRepository<Pet> pets
        )
        {
            _shelters = shelters;
            _pets = pets;
        }

        public async Task<IReadOnlyList<ShelterDto>> GetAllAsync()
        {
            var shelters = await _shelters.ListAsync();

            return shelters
                .Select(Map)
                .ToList();
        }

        public async Task<ShelterDto?> GetAsync(int id)
        {
            var shelter = await _shelters.GetByIdAsync(id);

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
            var normalizedName = ValidateRequiredField(
                name, "Name", 128
            );

            var normalizedAddress = ValidateRequiredField(
                address, "Address", 256
            );

            var normalizedCity = ValidateRequiredField(
                city, "City", 128
            );

            var normalizedPhone = NormalizeOptional(
                phone, nameof(phone), 64
            );

            var normalizedEmail = ValidateEmail(email);

            var shelter = new Shelter
            {
                Name = normalizedName,
                Address = normalizedAddress,
                City = normalizedCity,
                Phone = normalizedPhone,
                Email = normalizedEmail
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
            var normalizedName = ValidateRequiredField(
                name, "Name", 128
            );

            var normalizedAddress = ValidateRequiredField(
                address, "Address", 256
            );

            var normalizedCity = ValidateRequiredField(
                city, "City", 128
            );

            var normalizedPhone = NormalizeOptional(
                phone, nameof(phone), 64
            );

            var normalizedEmail = ValidateEmail(email);

            var shelter = await _shelters.GetByIdAsync(id)
                ?? throw new ArgumentException(
                    "Shelter not found."
                );

            shelter.Name = normalizedName;
            shelter.Address = normalizedAddress;
            shelter.City = normalizedCity;
            shelter.Phone = normalizedPhone;
            shelter.Email = normalizedEmail;

            await _shelters.UpdateAsync(shelter);
            await _shelters.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var shelter = await _shelters.GetByIdAsync(id)
                ?? throw new ArgumentException(
                    "Shelter not found."
                );

            var existingPets = await _pets.ListAsync(
                pet => pet.ShelterId == id
            );

            if (existingPets.Any())
            {
                throw new InvalidOperationException(
                    "Cannot delete a shelter with existing pets. Move or delete its pets first."
                );
            }

            await _shelters.DeleteAsync(shelter);
            await _shelters.SaveChangesAsync();
        }

        private static ShelterDto Map(Shelter shelter)
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

        private static string ValidateRequiredField(
            string value,
            string fieldName,
            int maxLength
        )
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    $"{fieldName} is required."
                );
            }

            var normalized = value.Trim();

            if (normalized.Length > maxLength)
            {
                throw new ArgumentException(
                    $"{fieldName} cannot exceed {maxLength} characters."
                );
            }

            return normalized;
        }

        private static string? ValidateEmail(string? email)
        {
            var normalized = NormalizeOptional(
                email, nameof(email), 128
            );

            if (normalized != null
                && !new EmailAddressAttribute().IsValid(normalized))
            {
                throw new ArgumentException(
                    "Invalid email address.",
                    nameof(email)
                );
            }

            return normalized;
        }

        private static string? NormalizeOptional(
            string? value,
            string parameterName,
            int maxLength
        )
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var normalized = value.Trim();

            if (normalized.Length > maxLength)
            {
                throw new ArgumentException(
                    $"{parameterName} cannot exceed {maxLength} characters.",
                    parameterName
                );
            }

            return normalized;
        }
    }
}