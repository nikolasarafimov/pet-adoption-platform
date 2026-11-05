using System.ComponentModel.DataAnnotations;
using PetAdoption.Application.DTOs;
using PetAdoption.Application.Interfaces;
using PetAdoption.Domain.Abstractions;
using PetAdoption.Domain.Entities;

namespace PetAdoption.Application.Services
{
    public class AdopterService : IAdopterService
    {
        private readonly IRepository<Adopter> _adopters;
        private readonly IRepository<AdoptionRequest> _requests;

        public AdopterService(
            IRepository<Adopter> adopters,
            IRepository<AdoptionRequest> requests
        )
        {
            _adopters = adopters;
            _requests = requests;
        }

        public async Task<IReadOnlyList<AdopterDto>> GetAllAsync()
        {
            var adopters = await _adopters.ListAsync();

            return adopters
                .Select(Map)
                .ToList();
        }

        public async Task<AdopterDto?> GetAsync(int id)
        {
            var adopter = await _adopters.GetByIdAsync(id);

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
            var adopter = new Adopter
            {
                FullName = ValidateFullName(fullName),
                Email = NormalizeEmail(email),
                Phone = NormalizeOptional(phone, nameof(phone), 64),
                Address = NormalizeOptional(address, nameof(address), 256),
                Notes = NormalizeOptional(notes, nameof(notes), 512)
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
            var normalizedFullName = ValidateFullName(fullName);
            var normalizedEmail = NormalizeEmail(email);
            var normalizedPhone = NormalizeOptional(
                phone,
                nameof(phone),
                64
            );
            var normalizedAddress = NormalizeOptional(
                address,
                nameof(address),
                256
            );
            var normalizedNotes = NormalizeOptional(
                notes,
                nameof(notes),
                512
            );

            var adopter = await _adopters.GetByIdAsync(id)
                ?? throw new ArgumentException(
                    "Adopter not found.",
                    nameof(id)
                );

            adopter.FullName = normalizedFullName;
            adopter.Email = normalizedEmail;
            adopter.Phone = normalizedPhone;
            adopter.Address = normalizedAddress;
            adopter.Notes = normalizedNotes;

            await _adopters.UpdateAsync(adopter);
            await _adopters.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var adopter = await _adopters.GetByIdAsync(id)
                ?? throw new ArgumentException(
                    "Adopter not found.",
                    nameof(id)
                );

            var existingRequests = await _requests.ListAsync(
                request => request.AdopterId == id
            );

            if (existingRequests.Any())
            {
                throw new InvalidOperationException(
                    "Cannot delete an adopter with existing adoption requests."
                );
            }

            await _adopters.DeleteAsync(adopter);
            await _adopters.SaveChangesAsync();
        }

        private static AdopterDto Map(Adopter adopter)
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

        private static string ValidateFullName(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
            {
                throw new ArgumentException(
                    "Full name is required.",
                    nameof(fullName)
                );
            }

            var normalized = fullName.Trim();

            if (normalized.Length > 128)
            {
                throw new ArgumentException(
                    "Full name cannot exceed 128 characters.",
                    nameof(fullName)
                );
            }

            return normalized;
        }

        private static string? NormalizeEmail(string? email)
        {
            var normalized = NormalizeOptional(
                email,
                nameof(email),
                128
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