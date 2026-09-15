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

        public PetService(
            IRepository<Pet> pets,
            IRepository<Shelter> shelters
        )
        {
            _pets = pets;
            _shelters = shelters;
        }

        public async Task<IReadOnlyList<PetDto>> GetAllAsync(
            string? search = null,
            Species? species = null,
            int? shelterId = null
        )
        {
            var searchText =
                string.IsNullOrWhiteSpace(search)
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
            ValidateName(name);
            ValidateAge(ageYears);

            _ = await _shelters.GetByIdAsync(shelterId)
                ?? throw new ArgumentException(
                    "Shelter not found."
                );

            var pet = new Pet
            {
                Name = name.Trim(),
                Species = species,
                Breed = NormalizeOptional(breed),
                AgeYears = ageYears,
                Description = NormalizeOptional(description),
                ImageUrl = NormalizeOptional(imageUrl),
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
            ValidateName(name);
            ValidateAge(ageYears);

            var pet = await _pets.GetByIdAsync(id)
                ?? throw new ArgumentException(
                    "Pet not found."
                );

            _ = await _shelters.GetByIdAsync(shelterId)
                ?? throw new ArgumentException(
                    "Shelter not found."
                );

            pet.Name = name.Trim();
            pet.Species = species;
            pet.Breed = NormalizeOptional(breed);
            pet.AgeYears = ageYears;
            pet.Description = NormalizeOptional(description);
            pet.ImageUrl = NormalizeOptional(imageUrl);
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

        private static void ValidateName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException(
                    "Pet name is required."
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