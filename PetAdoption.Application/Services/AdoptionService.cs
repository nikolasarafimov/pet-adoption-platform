using PetAdoption.Application.DTOs;
using PetAdoption.Application.Interfaces;
using PetAdoption.Domain.Abstractions;
using PetAdoption.Domain.Entities;
using PetAdoption.Domain.Enums;

namespace PetAdoption.Application.Services
{
    public class AdoptionService : IAdoptionService
    {
        private const int MaxMessageLength = 512;

        private readonly IRepository<AdoptionRequest> _requests;
        private readonly IRepository<Pet> _pets;
        private readonly IRepository<Adopter> _adopters;

        public AdoptionService(
            IRepository<AdoptionRequest> requests,
            IRepository<Pet> pets,
            IRepository<Adopter> adopters
        )
        {
            _requests = requests;
            _pets = pets;
            _adopters = adopters;
        }

        public async Task<IReadOnlyList<AdoptionRequestDto>> GetForAdopterAsync(
            int adopterId
        )
        {
            var requests = await _requests.ListAsync(
                request => request.AdopterId == adopterId,
                request => request.Pet,
                request => request.Adopter
            );

            return requests
                .OrderByDescending(request => request.CreatedAt)
                .Select(Map)
                .ToList();
        }

        public async Task<IReadOnlyList<AdoptionRequestDto>> GetForShelterAsync(
            int shelterId
        )
        {
            var requests = await _requests.ListAsync(
                request => request.Pet.ShelterId == shelterId,
                request => request.Pet,
                request => request.Adopter
            );

            return requests
                .OrderByDescending(request => request.CreatedAt)
                .Select(Map)
                .ToList();
        }

        public async Task SubmitAsync(
            int petId,
            int adopterId,
            string? message
        )
        {
            var normalizedMessage = NormalizeMessage(message);

            var pet = await _pets.GetByIdAsync(petId)
                ?? throw new ArgumentException(
                    "Pet not found.",
                    nameof(petId)
                );

            if (pet.Status != PetStatus.Available)
            {
                throw new InvalidOperationException(
                    "This pet is not available for adoption."
                );
            }

            _ = await _adopters.GetByIdAsync(adopterId)
                ?? throw new ArgumentException(
                    "Adopter not found.",
                    nameof(adopterId)
                );

            var pendingRequests = await _requests.ListAsync(
                request =>
                    request.PetId == petId
                    && request.Status == AdoptionRequestStatus.Pending
            );

            if (pendingRequests.Any(
                request => request.AdopterId == adopterId
            ))
            {
                throw new InvalidOperationException(
                    "You already have a pending request for this pet."
                );
            }

            if (pendingRequests.Any())
            {
                throw new InvalidOperationException(
                    "Another adoption request is already pending for this pet."
                );
            }

            pet.Status = PetStatus.Reserved;

            var request = new AdoptionRequest
            {
                PetId = petId,
                AdopterId = adopterId,
                Message = normalizedMessage,
                Status = AdoptionRequestStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            await _requests.AddAsync(request);
            await _requests.SaveChangesAsync();
        }

        public async Task SetStatusAsync(
            int requestId,
            AdoptionRequestStatus status
        )
        {
            if (status != AdoptionRequestStatus.Approved
                && status != AdoptionRequestStatus.Rejected)
            {
                throw new ArgumentException(
                    "Only approval or rejection is allowed here.",
                    nameof(status)
                );
            }

            var requests = await _requests.ListAsync(
                request => request.Id == requestId,
                request => request.Pet,
                request => request.Adopter
            );

            var request = requests.FirstOrDefault()
                ?? throw new ArgumentException(
                    "Adoption request not found.",
                    nameof(requestId)
                );

            if (request.Status != AdoptionRequestStatus.Pending)
            {
                throw new InvalidOperationException(
                    "This request has already been decided."
                );
            }

            if (request.Pet.Status != PetStatus.Reserved)
            {
                throw new InvalidOperationException(
                    "The pet is no longer reserved for this request."
                );
            }

            request.Status = status;
            request.DecisionAt = DateTime.UtcNow;

            request.Pet.Status =
                status == AdoptionRequestStatus.Approved
                    ? PetStatus.Adopted
                    : PetStatus.Available;

            await _requests.UpdateAsync(request);
            await _pets.UpdateAsync(request.Pet);
            await _requests.SaveChangesAsync();
        }

        public async Task CancelAsync(
            int requestId,
            int adopterId
        )
        {
            var requests = await _requests.ListAsync(
                request => request.Id == requestId,
                request => request.Pet
            );

            var request = requests.FirstOrDefault()
                ?? throw new ArgumentException(
                    "Adoption request not found.",
                    nameof(requestId)
                );

            if (request.AdopterId != adopterId)
            {
                throw new UnauthorizedAccessException(
                    "You cannot cancel another adopter's request."
                );
            }

            if (request.Status != AdoptionRequestStatus.Pending)
            {
                throw new InvalidOperationException(
                    "Only pending requests can be cancelled."
                );
            }

            if (request.Pet.Status != PetStatus.Reserved)
            {
                throw new InvalidOperationException(
                    "The pet is no longer reserved for this request."
                );
            }

            request.Status = AdoptionRequestStatus.Cancelled;
            request.DecisionAt = DateTime.UtcNow;
            request.Pet.Status = PetStatus.Available;

            await _requests.UpdateAsync(request);
            await _pets.UpdateAsync(request.Pet);
            await _requests.SaveChangesAsync();
        }

        public async Task<int> GetShelterIdForRequestAsync(
            int requestId
        )
        {
            var requests = await _requests.ListAsync(
                request => request.Id == requestId,
                request => request.Pet
            );

            var request = requests.FirstOrDefault()
                ?? throw new ArgumentException(
                    "Adoption request not found.",
                    nameof(requestId)
                );

            return request.Pet.ShelterId;
        }

        private static string? NormalizeMessage(string? message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return null;
            }

            var normalized = message.Trim();

            if (normalized.Length > MaxMessageLength)
            {
                throw new ArgumentException(
                    $"Message cannot exceed {MaxMessageLength} characters.",
                    nameof(message)
                );
            }

            return normalized;
        }

        private static AdoptionRequestDto Map(
            AdoptionRequest request
        )
        {
            return new AdoptionRequestDto(
                request.Id,
                request.PetId,
                request.Pet.Name,
                request.AdopterId,
                request.Adopter.FullName,
                request.Status,
                request.Message,
                request.CreatedAt,
                request.DecisionAt
            );
        }
    }
}