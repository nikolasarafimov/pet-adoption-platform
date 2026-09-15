using PetAdoption.Application.DTOs;
using PetAdoption.Domain.Enums;

namespace PetAdoption.Application.Interfaces
{
    public interface IAdoptionService
    {
        Task<IReadOnlyList<AdoptionRequestDto>> GetForAdopterAsync(
            int adopterId
        );

        Task<IReadOnlyList<AdoptionRequestDto>> GetForShelterAsync(
            int shelterId
        );

        Task SubmitAsync(
            int petId,
            int adopterId,
            string? message
        );

        Task SetStatusAsync(
            int requestId,
            AdoptionRequestStatus status
        );

        Task CancelAsync(
            int requestId,
            int adopterId
        );

        Task<int> GetShelterIdForRequestAsync(
            int requestId
        );
    }
}