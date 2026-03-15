using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
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

        public PetService(IRepository<Pet> pets, IRepository<Shelter> shelters)
        {
            _pets = pets; _shelters = shelters;
        }

        public async Task<IReadOnlyList<PetDto>> GetAllAsync(string? search = null, Species? species = null, int? shelterId = null)
        {
            var needle = (search ?? "").Trim().ToLower();

            var list = await _pets.ListAsync(
                p => (string.IsNullOrEmpty(needle) ||
                      p.Name.ToLower().Contains(needle) ||
                      (p.Breed ?? "").ToLower().Contains(needle)) &&
                     (!species.HasValue || p.Species == species) &&
                     (!shelterId.HasValue || p.ShelterId == shelterId.Value),
                p => p.Shelter);

            return list.Select(p => new PetDto(
                p.Id, p.Name, p.Species, p.Breed, p.AgeYears, p.Description, p.ImageUrl,
                p.Status, p.ShelterId, p.Shelter.Name)).ToList();
        }

        public async Task<PetDto?> GetAsync(int id)
        {
            var list = await _pets.ListAsync(p => p.Id == id, p => p.Shelter);
            var p = list.FirstOrDefault();
            if (p == null) return null;
            return new PetDto(p.Id, p.Name, p.Species, p.Breed, p.AgeYears, p.Description, p.ImageUrl, p.Status, p.ShelterId, p.Shelter.Name);
        }

        public async Task<int> CreateAsync(string name, Species species, string? breed, int ageYears, string? description, string? imageUrl, int shelterId)
        {
            if (ageYears < 0) throw new ArgumentOutOfRangeException(nameof(ageYears));
            var shelter = await _shelters.GetByIdAsync(shelterId) ?? throw new ArgumentException("Shelter not found!");
            var pet = new Pet { Name = name, Species = species, Breed = breed, AgeYears = ageYears, Description = description, ImageUrl = imageUrl, ShelterId = shelterId, Status = PetStatus.Available };
            await _pets.AddAsync(pet);
            await _pets.SaveChangesAsync();
            return pet.Id;
        }

        public async Task UpdateAsync(int id, string name, Species species, string? breed, int ageYears, string? description, string? imageUrl, int shelterId, PetStatus status)
        {
            if (ageYears < 0) throw new ArgumentOutOfRangeException(nameof(ageYears));

            var pet = await _pets.GetByIdAsync(id) ?? throw new ArgumentException("Pet not found!");
            pet.Name = name; pet.Species = species; pet.Breed = breed; pet.AgeYears = ageYears; pet.Description = description; pet.ImageUrl = imageUrl; pet.ShelterId = shelterId; pet.Status = status;
            await _pets.UpdateAsync(pet);
            await _pets.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var pet = await _pets.GetByIdAsync(id) ?? throw new ArgumentException("Pet not found!");
            await _pets.DeleteAsync(pet);
            await _pets.SaveChangesAsync();
        }
    }
}