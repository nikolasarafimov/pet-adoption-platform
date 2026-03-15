using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using PetAdoption.Application.DTOs;
using PetAdoption.Application.Interfaces;
using PetAdoption.Domain.Abstractions;
using PetAdoption.Domain.Entities;
using PetAdoption.Domain.Enums;

namespace PetAdoption.Application.Services
{
    public class AdoptionService : IAdoptionService
    {
        private readonly IRepository<AdoptionRequest> _requests;
        private readonly IRepository<Pet> _pets;
        private readonly IRepository<Adopter> _adopters;

        public AdoptionService(
            IRepository<AdoptionRequest> requests,
            IRepository<Pet> pets,
            IRepository<Adopter> adopters)
        {
            _requests = requests;
            _pets = pets;
            _adopters = adopters;
        }

        public async Task<IReadOnlyList<AdoptionRequestDto>> GetForAdopterAsync(int adopterId)
        {
            var list = await _requests.ListAsync(
                r => r.AdopterId == adopterId,
                r => r.Pet,
                r => r.Adopter
            );

            return list
                .OrderByDescending(r => r.CreatedAt)
                .Select(Map)
                .ToList();
        }

        public async Task<IReadOnlyList<AdoptionRequestDto>> GetForShelterAsync(int shelterId)
        {
            var list = await _requests.ListAsync(
                r => r.Pet.ShelterId == shelterId,
                r => r.Pet,
                r => r.Adopter
            );

            return list
                .OrderByDescending(r => r.CreatedAt)
                .Select(Map)
                .ToList();
        }

        public async Task SubmitAsync(int petId, int adopterId, string? message)
        {
            var pet = await _pets.GetByIdAsync(petId);
            if (pet == null) throw new ArgumentException("Pet not found.");

            if (pet.Status != PetStatus.Available)
                throw new InvalidOperationException("This pet is not available for adoption.");

            var adopter = await _adopters.GetByIdAsync(adopterId);
            if (adopter == null) throw new ArgumentException("Adopter not found.");

            var existing = await _requests.ListAsync(r =>
                r.PetId == petId &&
                r.AdopterId == adopterId &&
                r.Status == AdoptionRequestStatus.Pending);

            if (existing.Any())
                throw new InvalidOperationException("You already have a pending request for this pet.");

            pet.Status = PetStatus.Reserved;

            var req = new AdoptionRequest
            {
                PetId = petId,
                AdopterId = adopterId,
                Message = string.IsNullOrWhiteSpace(message) ? null : message.Trim(),
                Status = AdoptionRequestStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            await _requests.AddAsync(req);

            await _requests.SaveChangesAsync();
        }

        public async Task SetStatusAsync(int requestId, AdoptionRequestStatus status)
        {
            var list = await _requests.ListAsync(
                r => r.Id == requestId,
                r => r.Pet,
                r => r.Adopter
            );

            var req = list.FirstOrDefault();
            if (req == null) throw new ArgumentException("Adoption request not found.");

            if (status != AdoptionRequestStatus.Approved &&
                status != AdoptionRequestStatus.Rejected &&
                status != AdoptionRequestStatus.Cancelled)
            {
                throw new ArgumentException("Invalid status transition.");
            }

            if (req.Status != AdoptionRequestStatus.Pending)
                throw new InvalidOperationException("This request has already been decided.");

            req.Status = status;
            req.DecisionAt = DateTime.UtcNow;

            if (req.Pet == null)
                throw new InvalidOperationException("Request is missing Pet reference.");

            if (status == AdoptionRequestStatus.Approved)
            {
                req.Pet.Status = PetStatus.Adopted;
            }
            else 
            {
                if (req.Pet.Status == PetStatus.Reserved)
                {
                    var others = await _requests.ListAsync(r =>
                        r.PetId == req.PetId &&
                        r.Status == AdoptionRequestStatus.Pending &&
                        r.Id != req.Id);

                    if (!others.Any())
                        req.Pet.Status = PetStatus.Available;
                }
            }

            await _requests.SaveChangesAsync();
        }

        public async Task CancelAsync(int requestId, int adopterId)
        {
            var reqs = await _requests.ListAsync(r => r.Id == requestId, r => r.Pet);
            var req = reqs.FirstOrDefault();
            if (req == null) throw new ArgumentException("Request not found.");

            if (req.AdopterId != adopterId) throw new UnauthorizedAccessException("Not your request.");
            if (req.Status != AdoptionRequestStatus.Pending) throw new ArgumentException("Only pending requests can be cancelled.");

            req.Status = AdoptionRequestStatus.Cancelled;
            req.DecisionAt = DateTime.UtcNow;
            await _requests.UpdateAsync(req);

            var otherPending = await _requests.ListAsync(r => r.PetId == req.PetId && r.Status == AdoptionRequestStatus.Pending);
            if (otherPending.Count == 0)
            {
                req.Pet.Status = PetStatus.Available;
                await _pets.UpdateAsync(req.Pet);
            }

            await _requests.SaveChangesAsync();
            await _pets.SaveChangesAsync();
        }

        public async Task<int> GetShelterIdForRequestAsync(int requestId)
        {
            var req = (await _requests.ListAsync(r => r.Id == requestId, r => r.Pet))
                .FirstOrDefault();

            if (req == null)
                throw new ArgumentException("Request not found.");

            return req.Pet.ShelterId;
        }

        private static AdoptionRequestDto Map(AdoptionRequest r)
        {
            return new AdoptionRequestDto(
                r.Id,
                r.PetId,
                r.Pet?.Name ?? "",
                r.AdopterId,
                r.Adopter?.FullName ?? "",
                r.Status,
                r.Message,
                r.CreatedAt,
                r.DecisionAt
            );
        }
    }
}