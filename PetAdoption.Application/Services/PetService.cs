using PetAdoption.Application.DTOs;
using PetAdoption.Application.Interfaces;
using PetAdoption.Domain.Abstractions;
using PetAdoption.Domain.Entities;
using PetAdoption.Domain.Enums;

namespace PetAdoption.Application.Services
{
    public class PetService : IPetService
    {
        private readonly IRepository<Pet> _pets;
        private readonly IRepository<Shelter> _shelters;
        private readonly IRepository<AdoptionRequest> _requests;

        public PetService(
            IRepository<Pet> pets,
            IRepository<Shelter> shelters,
            IRepository<AdoptionRequest> requests
        )
        {
            _pets = pets;
            _shelters = shelters;
            _requests = requests;
        }

        public async Task<IReadOnlyList<PetDto>> GetAllAsync(
            string? search = null,
            Species? species = null,
            int? shelterId = null
        )
        {
            var searchText = string.IsNullOrWhiteSpace(search)
                ? string.Empty
                : search.Trim().ToLower();

            var pets = await _pets.ListAsync(
                pet =>
                    (
                        string.IsNullOrEmpty(searchText)
                        || pet.Name.ToLower().Contains(searchText)
                        || (pet.Breed ?? string.Empty)
                            .ToLower()
                            .Contains(searchText)
                    )
                    && (
                        !species.HasValue
                        || pet.Species == species.Value
                    )
                    && (
                        !shelterId.HasValue
                        || pet.ShelterId == shelterId.Value
                    ),
                pet => pet.Shelter
            );

            return pets
                .Select(Map)
                .ToList();
        }

        public async Task<PetDto?> GetAsync(int id)
        {
            var pets = await _pets.ListAsync(
                pet => pet.Id == id,
                pet => pet.Shelter
            );

            var pet = pets.FirstOrDefault();

            return pet == null
                ? null
                : Map(pet);
        }

        public async Task<int> CreateAsync(
            string name,
            Species species,
            string? breed,
            int ageYears,
            string? description,
            string? imageUrl,
            int shelterId
        )
        {
            var normalizedName = ValidateName(name);
            ValidateSpecies(species);
            ValidateAge(ageYears);

            var normalizedBreed = NormalizeOptional(
                breed, nameof(breed), 128
            );

            var normalizedDescription = NormalizeOptional(
                description, nameof(description), 1024
            );

            var normalizedImageUrl = ValidateImageUrl(imageUrl);

            _ = await _shelters.GetByIdAsync(shelterId)
                ?? throw new ArgumentException(
                    "Shelter not found."
                );

            var pet = new Pet
            {
                Name = normalizedName,
                Species = species,
                Breed = normalizedBreed,
                AgeYears = ageYears,
                Description = normalizedDescription,
                ImageUrl = normalizedImageUrl,
                ShelterId = shelterId,
                Status = PetStatus.Available
            };

            await _pets.AddAsync(pet);
            await _pets.SaveChangesAsync();

            return pet.Id;
        }

        public async Task UpdateAsync(
            int id,
            string name,
            Species species,
            string? breed,
            int ageYears,
            string? description,
            string? imageUrl,
            int shelterId,
            PetStatus status
        )
        {
            var normalizedName = ValidateName(name);
            ValidateSpecies(species);
            ValidateAge(ageYears);
            ValidateStatus(status);

            var normalizedBreed = NormalizeOptional(
                breed, nameof(breed), 128
            );

            var normalizedDescription = NormalizeOptional(
                description, nameof(description), 1024
            );

            var normalizedImageUrl = ValidateImageUrl(imageUrl);

            var pet = await _pets.GetByIdAsync(id)
                ?? throw new ArgumentException(
                    "Pet not found."
                );

            _ = await _shelters.GetByIdAsync(shelterId)
                ?? throw new ArgumentException(
                    "Shelter not found."
                );

            ValidateStatusTransition(pet.Status, status);

            if (pet.ShelterId != shelterId)
            {
                if (pet.Status == PetStatus.Reserved
                    || pet.Status == PetStatus.Adopted)
                {
                    throw new InvalidOperationException(
                        "Cannot change the shelter of a reserved or adopted pet."
                    );
                }

                var existingRequests = await _requests.ListAsync(
                    request => request.PetId == id
                );

                if (existingRequests.Any())
                {
                    throw new InvalidOperationException(
                        "Cannot change the shelter of a pet with adoption history."
                    );
                }
            }

            pet.Name = normalizedName;
            pet.Species = species;
            pet.Breed = normalizedBreed;
            pet.AgeYears = ageYears;
            pet.Description = normalizedDescription;
            pet.ImageUrl = normalizedImageUrl;
            pet.ShelterId = shelterId;
            pet.Status = status;

            await _pets.UpdateAsync(pet);
            await _pets.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var pet = await _pets.GetByIdAsync(id)
                ?? throw new ArgumentException(
                    "Pet not found."
                );

            var existingRequests = await _requests.ListAsync(
                request => request.PetId == id
            );

            if (existingRequests.Any())
            {
                throw new InvalidOperationException(
                    "Cannot delete a pet with existing adoption requests."
                );
            }

            if (pet.Status == PetStatus.Reserved
                || pet.Status == PetStatus.Adopted)
            {
                throw new InvalidOperationException(
                    "Cannot delete a reserved or adopted pet."
                );
            }

            await _pets.DeleteAsync(pet);
            await _pets.SaveChangesAsync();
        }

        private static PetDto Map(Pet pet)
        {
            return new PetDto(
                pet.Id,
                pet.Name,
                pet.Species,
                pet.Breed,
                pet.AgeYears,
                pet.Description,
                pet.ImageUrl,
                pet.Status,
                pet.ShelterId,
                pet.Shelter.Name
            );
        }

        private static string ValidateName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException(
                    "Pet name is required."
                );
            }

            var normalized = name.Trim();

            if (normalized.Length > 128)
            {
                throw new ArgumentException(
                    "Pet name cannot exceed 128 characters."
                );
            }

            return normalized;
        }

        private static void ValidateSpecies(Species species)
        {
            if (!Enum.IsDefined(species))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(species),
                    "Invalid pet species."
                );
            }
        }

        private static void ValidateStatus(PetStatus status)
        {
            if (!Enum.IsDefined(status))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(status),
                    "Invalid pet status."
                );
            }
        }

        private static void ValidateAge(int ageYears)
        {
            if (ageYears < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(ageYears),
                    "Pet age cannot be negative."
                );
            }

            if (ageYears > 200)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(ageYears),
                    "Pet age cannot exceed 200 years."
                );
            }
        }

        private static void ValidateStatusTransition(
            PetStatus currentStatus,
            PetStatus newStatus
        )
        {
            if (currentStatus == newStatus)
            {
                return;
            }

            if (currentStatus == PetStatus.Reserved
                || currentStatus == PetStatus.Adopted
                || newStatus == PetStatus.Reserved
                || newStatus == PetStatus.Adopted)
            {
                throw new InvalidOperationException(
                    "Reserved and adopted statuses can only be changed through the adoption workflow."
                );
            }
        }

        private static string? ValidateImageUrl(string? imageUrl)
        {
            var normalized = NormalizeOptional(
                imageUrl, nameof(imageUrl), 512
            );

            if (normalized == null)
            {
                return null;
            }

            if (!Uri.TryCreate(
                    normalized,
                    UriKind.Absolute,
                    out var uri
                )
                || (uri.Scheme != Uri.UriSchemeHttp
                    && uri.Scheme != Uri.UriSchemeHttps))
            {
                throw new ArgumentException(
                    "Image URL must be a valid HTTP or HTTPS URL."
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