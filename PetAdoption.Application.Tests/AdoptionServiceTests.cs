using System.Linq.Expressions;
using Moq;
using PetAdoption.Application.Services;
using PetAdoption.Domain.Abstractions;
using PetAdoption.Domain.Entities;
using PetAdoption.Domain.Enums;

namespace PetAdoption.Application.Tests;

public class AdoptionServiceTests
{
    [Fact]
    public async Task SubmitAsync_WhenValid_CreatesPendingRequestAndReservesPet()
    {
        var pet = new Pet
        {
            Id = 1,
            Name = "Luna",
            Status = PetStatus.Available,
            ShelterId = 10
        };

        var adopter = new Adopter
        {
            Id = 2,
            FullName = "Test Adopter"
        };

        var requestRepository =
            new Mock<IRepository<AdoptionRequest>>();

        var petRepository =
            new Mock<IRepository<Pet>>();

        var adopterRepository =
            new Mock<IRepository<Adopter>>();

        petRepository
            .Setup(repository =>
                repository.GetByIdAsync(pet.Id))
            .ReturnsAsync(pet);

        adopterRepository
            .Setup(repository =>
                repository.GetByIdAsync(adopter.Id))
            .ReturnsAsync(adopter);

        requestRepository
            .Setup(repository =>
                repository.ListAsync(
                    It.IsAny<
                        Expression<Func<AdoptionRequest, bool>>
                    >(),
                    It.IsAny<
                        Expression<
                            Func<AdoptionRequest, object>
                        >[]
                    >()
                ))
            .ReturnsAsync(
                Array.Empty<AdoptionRequest>()
            );

        AdoptionRequest? createdRequest = null;

        requestRepository
            .Setup(repository =>
                repository.AddAsync(
                    It.IsAny<AdoptionRequest>()
                ))
            .Callback<AdoptionRequest>(
                request => createdRequest = request
            )
            .ReturnsAsync(
                (AdoptionRequest request) => request
            );

        requestRepository
            .Setup(repository =>
                repository.SaveChangesAsync())
            .ReturnsAsync(1);

        var service = new AdoptionService(
            requestRepository.Object,
            petRepository.Object,
            adopterRepository.Object
        );

        await service.SubmitAsync(
            pet.Id,
            adopter.Id,
            "  I would love to adopt Luna.  "
        );

        Assert.NotNull(createdRequest);

        Assert.Equal(
            pet.Id,
            createdRequest.PetId
        );

        Assert.Equal(
            adopter.Id,
            createdRequest.AdopterId
        );

        Assert.Equal(
            AdoptionRequestStatus.Pending,
            createdRequest.Status
        );

        Assert.Equal(
            "I would love to adopt Luna.",
            createdRequest.Message
        );

        Assert.Equal(
            PetStatus.Reserved,
            pet.Status
        );

        requestRepository.Verify(
            repository =>
                repository.AddAsync(
                    It.IsAny<AdoptionRequest>()
                ),
            Times.Once
        );

        requestRepository.Verify(
            repository =>
                repository.SaveChangesAsync(),
            Times.Once
        );
    }

    [Fact]
    public async Task SubmitAsync_WhenMessageIsWhitespace_StoresNullMessage()
    {
        var pet = new Pet
        {
            Id = 1,
            Name = "Luna",
            Status = PetStatus.Available
        };

        var adopter = new Adopter
        {
            Id = 2,
            FullName = "Test Adopter"
        };

        var requestRepository =
            new Mock<IRepository<AdoptionRequest>>();

        var petRepository =
            new Mock<IRepository<Pet>>();

        var adopterRepository =
            new Mock<IRepository<Adopter>>();

        petRepository
            .Setup(repository =>
                repository.GetByIdAsync(1))
            .ReturnsAsync(pet);

        adopterRepository
            .Setup(repository =>
                repository.GetByIdAsync(2))
            .ReturnsAsync(adopter);

        requestRepository
            .Setup(repository =>
                repository.ListAsync(
                    It.IsAny<
                        Expression<Func<AdoptionRequest, bool>>
                    >(),
                    It.IsAny<
                        Expression<
                            Func<AdoptionRequest, object>
                        >[]
                    >()
                ))
            .ReturnsAsync(
                Array.Empty<AdoptionRequest>()
            );

        AdoptionRequest? createdRequest = null;

        requestRepository
            .Setup(repository =>
                repository.AddAsync(
                    It.IsAny<AdoptionRequest>()
                ))
            .Callback<AdoptionRequest>(
                request => createdRequest = request
            )
            .ReturnsAsync(
                (AdoptionRequest request) => request
            );

        requestRepository
            .Setup(repository =>
                repository.SaveChangesAsync())
            .ReturnsAsync(1);

        var service = new AdoptionService(
            requestRepository.Object,
            petRepository.Object,
            adopterRepository.Object
        );

        await service.SubmitAsync(
            1,
            2,
            "   "
        );

        Assert.NotNull(createdRequest);
        Assert.Null(createdRequest.Message);
    }

    [Fact]
    public async Task SubmitAsync_WhenPetDoesNotExist_ThrowsArgumentException()
    {
        var requestRepository =
            new Mock<IRepository<AdoptionRequest>>();

        var petRepository =
            new Mock<IRepository<Pet>>();

        var adopterRepository =
            new Mock<IRepository<Adopter>>();

        petRepository
            .Setup(repository =>
                repository.GetByIdAsync(999))
            .ReturnsAsync((Pet?)null);

        var service = new AdoptionService(
            requestRepository.Object,
            petRepository.Object,
            adopterRepository.Object
        );

        var exception =
            await Assert.ThrowsAsync<ArgumentException>(
                () => service.SubmitAsync(
                    999,
                    1,
                    null
                )
            );

        Assert.Equal(
            "Pet not found.",
            exception.Message
        );
    }

    [Fact]
    public async Task SubmitAsync_WhenPetIsNotAvailable_ThrowsInvalidOperationException()
    {
        var pet = new Pet
        {
            Id = 1,
            Name = "Luna",
            Status = PetStatus.Reserved
        };

        var requestRepository =
            new Mock<IRepository<AdoptionRequest>>();

        var petRepository =
            new Mock<IRepository<Pet>>();

        var adopterRepository =
            new Mock<IRepository<Adopter>>();

        petRepository
            .Setup(repository =>
                repository.GetByIdAsync(pet.Id))
            .ReturnsAsync(pet);

        var service = new AdoptionService(
            requestRepository.Object,
            petRepository.Object,
            adopterRepository.Object
        );

        var exception =
            await Assert.ThrowsAsync<
                InvalidOperationException
            >(
                () => service.SubmitAsync(
                    pet.Id,
                    2,
                    null
                )
            );

        Assert.Equal(
            "This pet is not available for adoption.",
            exception.Message
        );
    }

    [Fact]
    public async Task SubmitAsync_WhenAdopterDoesNotExist_ThrowsArgumentException()
    {
        var pet = new Pet
        {
            Id = 1,
            Name = "Luna",
            Status = PetStatus.Available
        };

        var requestRepository =
            new Mock<IRepository<AdoptionRequest>>();

        var petRepository =
            new Mock<IRepository<Pet>>();

        var adopterRepository =
            new Mock<IRepository<Adopter>>();

        petRepository
            .Setup(repository =>
                repository.GetByIdAsync(1))
            .ReturnsAsync(pet);

        adopterRepository
            .Setup(repository =>
                repository.GetByIdAsync(999))
            .ReturnsAsync((Adopter?)null);

        var service = new AdoptionService(
            requestRepository.Object,
            petRepository.Object,
            adopterRepository.Object
        );

        var exception =
            await Assert.ThrowsAsync<ArgumentException>(
                () => service.SubmitAsync(
                    1,
                    999,
                    null
                )
            );

        Assert.Equal(
            "Adopter not found.",
            exception.Message
        );
    }

    [Fact]
    public async Task SubmitAsync_WhenPendingRequestAlreadyExists_ThrowsInvalidOperationException()
    {
        var pet = new Pet
        {
            Id = 1,
            Name = "Luna",
            Status = PetStatus.Available
        };

        var adopter = new Adopter
        {
            Id = 2,
            FullName = "Test Adopter"
        };

        var existingRequest =
            new AdoptionRequest
            {
                Id = 5,
                PetId = pet.Id,
                AdopterId = adopter.Id,
                Status =
                    AdoptionRequestStatus.Pending
            };

        var requestRepository =
            new Mock<IRepository<AdoptionRequest>>();

        var petRepository =
            new Mock<IRepository<Pet>>();

        var adopterRepository =
            new Mock<IRepository<Adopter>>();

        petRepository
            .Setup(repository =>
                repository.GetByIdAsync(pet.Id))
            .ReturnsAsync(pet);

        adopterRepository
            .Setup(repository =>
                repository.GetByIdAsync(adopter.Id))
            .ReturnsAsync(adopter);

        requestRepository
            .Setup(repository =>
                repository.ListAsync(
                    It.IsAny<
                        Expression<Func<AdoptionRequest, bool>>
                    >(),
                    It.IsAny<
                        Expression<
                            Func<AdoptionRequest, object>
                        >[]
                    >()
                ))
            .ReturnsAsync(
                new[] { existingRequest }
            );

        var service = new AdoptionService(
            requestRepository.Object,
            petRepository.Object,
            adopterRepository.Object
        );

        var exception =
            await Assert.ThrowsAsync<
                InvalidOperationException
            >(
                () => service.SubmitAsync(
                    pet.Id,
                    adopter.Id,
                    null
                )
            );

        Assert.Equal(
            "You already have a pending request for this pet.",
            exception.Message
        );

        requestRepository.Verify(
            repository =>
                repository.AddAsync(
                    It.IsAny<AdoptionRequest>()
                ),
            Times.Never
        );
    }

    [Fact]
    public async Task SetStatusAsync_WhenApproved_ApprovesRequestAndMarksPetAsAdopted()
    {
        var pet = new Pet
        {
            Id = 1,
            Name = "Luna",
            Status = PetStatus.Reserved
        };

        var adopter = new Adopter
        {
            Id = 2,
            FullName = "Test Adopter"
        };

        var request =
            new AdoptionRequest
            {
                Id = 3,
                PetId = pet.Id,
                Pet = pet,
                AdopterId = adopter.Id,
                Adopter = adopter,
                Status =
                    AdoptionRequestStatus.Pending
            };

        var requestRepository =
            new Mock<IRepository<AdoptionRequest>>();

        var petRepository =
            new Mock<IRepository<Pet>>();

        var adopterRepository =
            new Mock<IRepository<Adopter>>();

        requestRepository
            .Setup(repository =>
                repository.ListAsync(
                    It.IsAny<
                        Expression<Func<AdoptionRequest, bool>>
                    >(),
                    It.IsAny<
                        Expression<
                            Func<AdoptionRequest, object>
                        >[]
                    >()
                ))
            .ReturnsAsync(
                new[] { request }
            );

        requestRepository
            .Setup(repository =>
                repository.UpdateAsync(request))
            .Returns(Task.CompletedTask);

        petRepository
            .Setup(repository =>
                repository.UpdateAsync(pet))
            .Returns(Task.CompletedTask);

        requestRepository
            .Setup(repository =>
                repository.SaveChangesAsync())
            .ReturnsAsync(1);

        var service = new AdoptionService(
            requestRepository.Object,
            petRepository.Object,
            adopterRepository.Object
        );

        await service.SetStatusAsync(
            request.Id,
            AdoptionRequestStatus.Approved
        );

        Assert.Equal(
            AdoptionRequestStatus.Approved,
            request.Status
        );

        Assert.Equal(
            PetStatus.Adopted,
            pet.Status
        );

        Assert.NotNull(request.DecisionAt);

        requestRepository.Verify(
            repository =>
                repository.UpdateAsync(request),
            Times.Once
        );

        petRepository.Verify(
            repository =>
                repository.UpdateAsync(pet),
            Times.Once
        );

        requestRepository.Verify(
            repository =>
                repository.SaveChangesAsync(),
            Times.Once
        );
    }

    [Fact]
    public async Task SetStatusAsync_WhenRejected_RejectsRequestAndMakesPetAvailable()
    {
        var pet = new Pet
        {
            Id = 1,
            Name = "Luna",
            Status = PetStatus.Reserved
        };

        var adopter = new Adopter
        {
            Id = 2,
            FullName = "Test Adopter"
        };

        var request =
            new AdoptionRequest
            {
                Id = 3,
                PetId = pet.Id,
                Pet = pet,
                AdopterId = adopter.Id,
                Adopter = adopter,
                Status =
                    AdoptionRequestStatus.Pending
            };

        var requestRepository =
            new Mock<IRepository<AdoptionRequest>>();

        var petRepository =
            new Mock<IRepository<Pet>>();

        var adopterRepository =
            new Mock<IRepository<Adopter>>();

        requestRepository
            .Setup(repository =>
                repository.ListAsync(
                    It.IsAny<
                        Expression<Func<AdoptionRequest, bool>>
                    >(),
                    It.IsAny<
                        Expression<
                            Func<AdoptionRequest, object>
                        >[]
                    >()
                ))
            .ReturnsAsync(
                new[] { request }
            );

        requestRepository
            .Setup(repository =>
                repository.UpdateAsync(request))
            .Returns(Task.CompletedTask);

        petRepository
            .Setup(repository =>
                repository.UpdateAsync(pet))
            .Returns(Task.CompletedTask);

        requestRepository
            .Setup(repository =>
                repository.SaveChangesAsync())
            .ReturnsAsync(1);

        var service = new AdoptionService(
            requestRepository.Object,
            petRepository.Object,
            adopterRepository.Object
        );

        await service.SetStatusAsync(
            request.Id,
            AdoptionRequestStatus.Rejected
        );

        Assert.Equal(
            AdoptionRequestStatus.Rejected,
            request.Status
        );

        Assert.Equal(
            PetStatus.Available,
            pet.Status
        );

        Assert.NotNull(request.DecisionAt);
    }

    [Fact]
    public async Task SetStatusAsync_WhenStatusIsNotApprovedOrRejected_ThrowsArgumentException()
    {
        var service = new AdoptionService(
            Mock.Of<IRepository<AdoptionRequest>>(),
            Mock.Of<IRepository<Pet>>(),
            Mock.Of<IRepository<Adopter>>()
        );

        var exception =
            await Assert.ThrowsAsync<ArgumentException>(
                () => service.SetStatusAsync(
                    1,
                    AdoptionRequestStatus.Cancelled
                )
            );

        Assert.Equal(
            "Only approval or rejection is allowed here.",
            exception.Message
        );
    }

    [Fact]
    public async Task SetStatusAsync_WhenRequestAlreadyDecided_ThrowsInvalidOperationException()
    {
        var pet = new Pet
        {
            Id = 1,
            Name = "Luna",
            Status = PetStatus.Adopted
        };

        var adopter = new Adopter
        {
            Id = 2,
            FullName = "Test Adopter"
        };

        var request =
            new AdoptionRequest
            {
                Id = 3,
                Pet = pet,
                PetId = pet.Id,
                Adopter = adopter,
                AdopterId = adopter.Id,
                Status =
                    AdoptionRequestStatus.Approved
            };

        var requestRepository =
            new Mock<IRepository<AdoptionRequest>>();

        requestRepository
            .Setup(repository =>
                repository.ListAsync(
                    It.IsAny<
                        Expression<Func<AdoptionRequest, bool>>
                    >(),
                    It.IsAny<
                        Expression<
                            Func<AdoptionRequest, object>
                        >[]
                    >()
                ))
            .ReturnsAsync(
                new[] { request }
            );

        var service = new AdoptionService(
            requestRepository.Object,
            Mock.Of<IRepository<Pet>>(),
            Mock.Of<IRepository<Adopter>>()
        );

        var exception =
            await Assert.ThrowsAsync<
                InvalidOperationException
            >(
                () => service.SetStatusAsync(
                    request.Id,
                    AdoptionRequestStatus.Rejected
                )
            );

        Assert.Equal(
            "This request has already been decided.",
            exception.Message
        );
    }

    [Fact]
    public async Task CancelAsync_WhenValid_CancelsRequestAndMakesPetAvailable()
    {
        var pet = new Pet
        {
            Id = 1,
            Name = "Luna",
            Status = PetStatus.Reserved
        };

        var request =
            new AdoptionRequest
            {
                Id = 3,
                PetId = pet.Id,
                Pet = pet,
                AdopterId = 2,
                Status =
                    AdoptionRequestStatus.Pending
            };

        var requestRepository =
            new Mock<IRepository<AdoptionRequest>>();

        var petRepository =
            new Mock<IRepository<Pet>>();

        requestRepository
            .Setup(repository =>
                repository.ListAsync(
                    It.IsAny<
                        Expression<Func<AdoptionRequest, bool>>
                    >(),
                    It.IsAny<
                        Expression<
                            Func<AdoptionRequest, object>
                        >[]
                    >()
                ))
            .ReturnsAsync(
                new[] { request }
            );

        requestRepository
            .Setup(repository =>
                repository.UpdateAsync(request))
            .Returns(Task.CompletedTask);

        petRepository
            .Setup(repository =>
                repository.UpdateAsync(pet))
            .Returns(Task.CompletedTask);

        requestRepository
            .Setup(repository =>
                repository.SaveChangesAsync())
            .ReturnsAsync(1);

        var service = new AdoptionService(
            requestRepository.Object,
            petRepository.Object,
            Mock.Of<IRepository<Adopter>>()
        );

        await service.CancelAsync(
            request.Id,
            request.AdopterId
        );

        Assert.Equal(
            AdoptionRequestStatus.Cancelled,
            request.Status
        );

        Assert.Equal(
            PetStatus.Available,
            pet.Status
        );

        Assert.NotNull(request.DecisionAt);

        requestRepository.Verify(
            repository =>
                repository.UpdateAsync(request),
            Times.Once
        );

        petRepository.Verify(
            repository =>
                repository.UpdateAsync(pet),
            Times.Once
        );
    }

    [Fact]
    public async Task CancelAsync_WhenAdopterDoesNotOwnRequest_ThrowsUnauthorizedAccessException()
    {
        var pet = new Pet
        {
            Id = 1,
            Name = "Luna",
            Status = PetStatus.Reserved
        };

        var request =
            new AdoptionRequest
            {
                Id = 3,
                PetId = pet.Id,
                Pet = pet,
                AdopterId = 2,
                Status =
                    AdoptionRequestStatus.Pending
            };

        var requestRepository =
            new Mock<IRepository<AdoptionRequest>>();

        requestRepository
            .Setup(repository =>
                repository.ListAsync(
                    It.IsAny<
                        Expression<Func<AdoptionRequest, bool>>
                    >(),
                    It.IsAny<
                        Expression<
                            Func<AdoptionRequest, object>
                        >[]
                    >()
                ))
            .ReturnsAsync(
                new[] { request }
            );

        var service = new AdoptionService(
            requestRepository.Object,
            Mock.Of<IRepository<Pet>>(),
            Mock.Of<IRepository<Adopter>>()
        );

        await Assert.ThrowsAsync<
            UnauthorizedAccessException
        >(
            () => service.CancelAsync(
                request.Id,
                999
            )
        );
    }

    [Fact]
    public async Task CancelAsync_WhenRequestIsNotPending_ThrowsInvalidOperationException()
    {
        var pet = new Pet
        {
            Id = 1,
            Name = "Luna",
            Status = PetStatus.Adopted
        };

        var request =
            new AdoptionRequest
            {
                Id = 3,
                PetId = pet.Id,
                Pet = pet,
                AdopterId = 2,
                Status =
                    AdoptionRequestStatus.Approved
            };

        var requestRepository =
            new Mock<IRepository<AdoptionRequest>>();

        requestRepository
            .Setup(repository =>
                repository.ListAsync(
                    It.IsAny<
                        Expression<Func<AdoptionRequest, bool>>
                    >(),
                    It.IsAny<
                        Expression<
                            Func<AdoptionRequest, object>
                        >[]
                    >()
                ))
            .ReturnsAsync(
                new[] { request }
            );

        var service = new AdoptionService(
            requestRepository.Object,
            Mock.Of<IRepository<Pet>>(),
            Mock.Of<IRepository<Adopter>>()
        );

        await Assert.ThrowsAsync<
            InvalidOperationException
        >(
            () => service.CancelAsync(
                request.Id,
                request.AdopterId
            )
        );
    }

    [Fact]
    public async Task GetShelterIdForRequestAsync_ReturnsPetShelterId()
    {
        var pet = new Pet
        {
            Id = 1,
            Name = "Luna",
            ShelterId = 77
        };

        var request =
            new AdoptionRequest
            {
                Id = 3,
                PetId = pet.Id,
                Pet = pet
            };

        var requestRepository =
            new Mock<IRepository<AdoptionRequest>>();

        requestRepository
            .Setup(repository =>
                repository.ListAsync(
                    It.IsAny<
                        Expression<Func<AdoptionRequest, bool>>
                    >(),
                    It.IsAny<
                        Expression<
                            Func<AdoptionRequest, object>
                        >[]
                    >()
                ))
            .ReturnsAsync(
                new[] { request }
            );

        var service = new AdoptionService(
            requestRepository.Object,
            Mock.Of<IRepository<Pet>>(),
            Mock.Of<IRepository<Adopter>>()
        );

        var shelterId =
            await service.GetShelterIdForRequestAsync(
                request.Id
            );

        Assert.Equal(
            77,
            shelterId
        );
    }
}