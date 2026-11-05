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
        var (service, requests, pets, adopters) = CreateService();

        var pet = CreatePet();
        var adopter = CreateAdopter();

        SetupPetAndAdopter(pets, adopters, pet, adopter);

        AdoptionRequest? createdRequest = null;

        requests
            .Setup(repository =>
                repository.AddAsync(It.IsAny<AdoptionRequest>()))
            .Callback<AdoptionRequest>(
                request => createdRequest = request
            )
            .ReturnsAsync((AdoptionRequest request) => request);

        await service.SubmitAsync(
            pet.Id,
            adopter.Id,
            "  I would love to adopt Luna.  "
        );

        var savedRequest =
            Assert.IsType<AdoptionRequest>(createdRequest);

        Assert.Equal(pet.Id, savedRequest.PetId);
        Assert.Equal(adopter.Id, savedRequest.AdopterId);
        Assert.Equal(
            AdoptionRequestStatus.Pending,
            savedRequest.Status
        );
        Assert.Equal(
            "I would love to adopt Luna.",
            savedRequest.Message
        );
        Assert.Equal(PetStatus.Reserved, pet.Status);

        requests.Verify(
            repository =>
                repository.AddAsync(It.IsAny<AdoptionRequest>()),
            Times.Once
        );

        requests.Verify(
            repository => repository.SaveChangesAsync(),
            Times.Once
        );
    }

    [Fact]
    public async Task SubmitAsync_WhenMessageIsWhitespace_StoresNullMessage()
    {
        var (service, requests, pets, adopters) = CreateService();

        var pet = CreatePet();
        var adopter = CreateAdopter();

        SetupPetAndAdopter(pets, adopters, pet, adopter);

        AdoptionRequest? createdRequest = null;

        requests
            .Setup(repository =>
                repository.AddAsync(It.IsAny<AdoptionRequest>()))
            .Callback<AdoptionRequest>(
                request => createdRequest = request
            )
            .ReturnsAsync((AdoptionRequest request) => request);

        await service.SubmitAsync(
            pet.Id,
            adopter.Id,
            "   "
        );

        Assert.NotNull(createdRequest);
        Assert.Null(createdRequest.Message);
        Assert.Equal(PetStatus.Reserved, pet.Status);
    }

    [Fact]
    public async Task SubmitAsync_WhenMessageIsExactly512Characters_Succeeds()
    {
        var (service, requests, pets, adopters) = CreateService();

        var pet = CreatePet();
        var adopter = CreateAdopter();

        SetupPetAndAdopter(pets, adopters, pet, adopter);

        AdoptionRequest? createdRequest = null;

        requests
            .Setup(repository =>
                repository.AddAsync(It.IsAny<AdoptionRequest>()))
            .Callback<AdoptionRequest>(
                request => createdRequest = request
            )
            .ReturnsAsync((AdoptionRequest request) => request);

        await service.SubmitAsync(
            pet.Id,
            adopter.Id,
            new string('A', 512)
        );

        Assert.NotNull(createdRequest);
        Assert.Equal(512, createdRequest.Message?.Length);
    }

    [Fact]
    public async Task SubmitAsync_WhenMessageExceeds512Characters_ThrowsArgumentException()
    {
        var (service, requests, _, _) = CreateService();

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => service.SubmitAsync(
                1,
                2,
                new string('A', 513)
            )
        );

        Assert.Equal("message", exception.ParamName);

        requests.Verify(
            repository =>
                repository.AddAsync(It.IsAny<AdoptionRequest>()),
            Times.Never
        );
    }

    [Fact]
    public async Task SubmitAsync_WhenPetDoesNotExist_ThrowsArgumentException()
    {
        var (service, requests, _, _) = CreateService();

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => service.SubmitAsync(999, 2, null)
        );

        Assert.Equal("petId", exception.ParamName);
        Assert.StartsWith("Pet not found.", exception.Message);

        requests.Verify(
            repository =>
                repository.AddAsync(It.IsAny<AdoptionRequest>()),
            Times.Never
        );
    }

    [Theory]
    [InlineData(PetStatus.Reserved)]
    [InlineData(PetStatus.Adopted)]
    [InlineData(PetStatus.Unavailable)]
    public async Task SubmitAsync_WhenPetIsNotAvailable_ThrowsInvalidOperationException(
        PetStatus status
    )
    {
        var (service, requests, pets, _) = CreateService();

        var pet = CreatePet(status: status);

        pets
            .Setup(repository => repository.GetByIdAsync(pet.Id))
            .ReturnsAsync(pet);

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.SubmitAsync(pet.Id, 2, null)
            );

        Assert.Equal(
            "This pet is not available for adoption.",
            exception.Message
        );

        requests.Verify(
            repository =>
                repository.AddAsync(It.IsAny<AdoptionRequest>()),
            Times.Never
        );
    }

    [Fact]
    public async Task SubmitAsync_WhenAdopterDoesNotExist_ThrowsArgumentException()
    {
        var (service, requests, pets, _) = CreateService();

        var pet = CreatePet();

        pets
            .Setup(repository => repository.GetByIdAsync(pet.Id))
            .ReturnsAsync(pet);

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => service.SubmitAsync(pet.Id, 999, null)
        );

        Assert.Equal("adopterId", exception.ParamName);
        Assert.StartsWith("Adopter not found.", exception.Message);

        Assert.Equal(PetStatus.Available, pet.Status);

        requests.Verify(
            repository =>
                repository.AddAsync(It.IsAny<AdoptionRequest>()),
            Times.Never
        );
    }

    [Fact]
    public async Task SubmitAsync_WhenSameAdopterHasPendingRequest_ThrowsInvalidOperationException()
    {
        var (service, requests, pets, adopters) = CreateService();

        var pet = CreatePet();
        var adopter = CreateAdopter();

        SetupPetAndAdopter(pets, adopters, pet, adopter);

        SetRequests(
            requests,
            CreateRequest(5, pet, adopter.Id)
        );

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
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

        Assert.Equal(PetStatus.Available, pet.Status);

        requests.Verify(
            repository =>
                repository.AddAsync(It.IsAny<AdoptionRequest>()),
            Times.Never
        );
    }

    [Fact]
    public async Task SubmitAsync_WhenAnotherAdopterHasPendingRequest_ThrowsInvalidOperationException()
    {
        var (service, requests, pets, adopters) = CreateService();

        var pet = CreatePet();
        var adopter = CreateAdopter();

        SetupPetAndAdopter(pets, adopters, pet, adopter);

        SetRequests(
            requests,
            CreateRequest(5, pet, adopterId: 3)
        );

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.SubmitAsync(
                    pet.Id,
                    adopter.Id,
                    null
                )
            );

        Assert.Equal(
            "Another adoption request is already pending for this pet.",
            exception.Message
        );

        Assert.Equal(PetStatus.Available, pet.Status);

        requests.Verify(
            repository =>
                repository.AddAsync(It.IsAny<AdoptionRequest>()),
            Times.Never
        );
    }

    [Theory]
    [InlineData(
        AdoptionRequestStatus.Approved,
        PetStatus.Adopted
    )]
    [InlineData(
        AdoptionRequestStatus.Rejected,
        PetStatus.Available
    )]
    public async Task SetStatusAsync_WhenValid_UpdatesRequestAndPet(
        AdoptionRequestStatus newStatus,
        PetStatus expectedPetStatus
    )
    {
        var (service, requests, pets, _) = CreateService();

        var pet = CreatePet(status: PetStatus.Reserved);
        var request = CreateRequest(3, pet);

        SetRequests(requests, request);

        await service.SetStatusAsync(request.Id, newStatus);

        Assert.Equal(newStatus, request.Status);
        Assert.Equal(expectedPetStatus, pet.Status);
        Assert.NotNull(request.DecisionAt);

        requests.Verify(
            repository => repository.UpdateAsync(request),
            Times.Once
        );

        pets.Verify(
            repository => repository.UpdateAsync(pet),
            Times.Once
        );

        requests.Verify(
            repository => repository.SaveChangesAsync(),
            Times.Once
        );
    }

    [Theory]
    [InlineData(AdoptionRequestStatus.Pending)]
    [InlineData(AdoptionRequestStatus.Cancelled)]
    public async Task SetStatusAsync_WhenStatusIsInvalid_ThrowsArgumentException(
        AdoptionRequestStatus status
    )
    {
        var (service, _, _, _) = CreateService();

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => service.SetStatusAsync(1, status)
        );

        Assert.Equal("status", exception.ParamName);
        Assert.StartsWith(
            "Only approval or rejection is allowed here.",
            exception.Message
        );
    }

    [Fact]
    public async Task SetStatusAsync_WhenRequestDoesNotExist_ThrowsArgumentException()
    {
        var (service, _, _, _) = CreateService();

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => service.SetStatusAsync(
                999,
                AdoptionRequestStatus.Approved
            )
        );

        Assert.Equal("requestId", exception.ParamName);
        Assert.StartsWith(
            "Adoption request not found.",
            exception.Message
        );
    }

    [Theory]
    [InlineData(AdoptionRequestStatus.Approved)]
    [InlineData(AdoptionRequestStatus.Rejected)]
    [InlineData(AdoptionRequestStatus.Cancelled)]
    public async Task SetStatusAsync_WhenRequestAlreadyDecided_ThrowsInvalidOperationException(
        AdoptionRequestStatus currentStatus
    )
    {
        var (service, requests, pets, _) = CreateService();

        var pet = CreatePet(status: PetStatus.Adopted);

        var request = CreateRequest(
            3,
            pet,
            status: currentStatus
        );

        SetRequests(requests, request);

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.SetStatusAsync(
                    request.Id,
                    AdoptionRequestStatus.Rejected
                )
            );

        Assert.Equal(
            "This request has already been decided.",
            exception.Message
        );

        requests.Verify(
            repository =>
                repository.UpdateAsync(It.IsAny<AdoptionRequest>()),
            Times.Never
        );

        pets.Verify(
            repository => repository.UpdateAsync(It.IsAny<Pet>()),
            Times.Never
        );
    }

    [Theory]
    [InlineData(PetStatus.Available)]
    [InlineData(PetStatus.Adopted)]
    [InlineData(PetStatus.Unavailable)]
    public async Task SetStatusAsync_WhenPetIsNotReserved_ThrowsInvalidOperationException(
        PetStatus petStatus
    )
    {
        var (service, requests, pets, _) = CreateService();

        var pet = CreatePet(status: petStatus);
        var request = CreateRequest(3, pet);

        SetRequests(requests, request);

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.SetStatusAsync(
                    request.Id,
                    AdoptionRequestStatus.Approved
                )
            );

        Assert.Equal(
            "The pet is no longer reserved for this request.",
            exception.Message
        );

        Assert.Equal(AdoptionRequestStatus.Pending, request.Status);
        Assert.Equal(petStatus, pet.Status);
        Assert.Null(request.DecisionAt);

        requests.Verify(
            repository =>
                repository.UpdateAsync(It.IsAny<AdoptionRequest>()),
            Times.Never
        );

        pets.Verify(
            repository => repository.UpdateAsync(It.IsAny<Pet>()),
            Times.Never
        );
    }

    [Fact]
    public async Task CancelAsync_WhenValid_CancelsRequestAndMakesPetAvailable()
    {
        var (service, requests, pets, _) = CreateService();

        var pet = CreatePet(status: PetStatus.Reserved);
        var request = CreateRequest(3, pet);

        SetRequests(requests, request);

        await service.CancelAsync(
            request.Id,
            request.AdopterId
        );

        Assert.Equal(
            AdoptionRequestStatus.Cancelled,
            request.Status
        );

        Assert.Equal(PetStatus.Available, pet.Status);
        Assert.NotNull(request.DecisionAt);

        requests.Verify(
            repository => repository.UpdateAsync(request),
            Times.Once
        );

        pets.Verify(
            repository => repository.UpdateAsync(pet),
            Times.Once
        );

        requests.Verify(
            repository => repository.SaveChangesAsync(),
            Times.Once
        );
    }

    [Fact]
    public async Task CancelAsync_WhenAdopterDoesNotOwnRequest_ThrowsUnauthorizedAccessException()
    {
        var (service, requests, pets, _) = CreateService();

        var pet = CreatePet(status: PetStatus.Reserved);
        var request = CreateRequest(3, pet, adopterId: 2);

        SetRequests(requests, request);

        var exception =
            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => service.CancelAsync(request.Id, 999)
            );

        Assert.Equal(
            "You cannot cancel another adopter's request.",
            exception.Message
        );

        Assert.Equal(AdoptionRequestStatus.Pending, request.Status);
        Assert.Equal(PetStatus.Reserved, pet.Status);

        requests.Verify(
            repository =>
                repository.UpdateAsync(It.IsAny<AdoptionRequest>()),
            Times.Never
        );

        pets.Verify(
            repository => repository.UpdateAsync(It.IsAny<Pet>()),
            Times.Never
        );
    }

    [Theory]
    [InlineData(AdoptionRequestStatus.Approved)]
    [InlineData(AdoptionRequestStatus.Rejected)]
    [InlineData(AdoptionRequestStatus.Cancelled)]
    public async Task CancelAsync_WhenRequestIsNotPending_ThrowsInvalidOperationException(
        AdoptionRequestStatus status
    )
    {
        var (service, requests, _, _) = CreateService();

        var pet = CreatePet(status: PetStatus.Adopted);
        var request = CreateRequest(3, pet, status: status);

        SetRequests(requests, request);

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.CancelAsync(
                    request.Id,
                    request.AdopterId
                )
            );

        Assert.Equal(
            "Only pending requests can be cancelled.",
            exception.Message
        );
    }

    [Theory]
    [InlineData(PetStatus.Available)]
    [InlineData(PetStatus.Adopted)]
    [InlineData(PetStatus.Unavailable)]
    public async Task CancelAsync_WhenPetIsNotReserved_ThrowsInvalidOperationException(
        PetStatus petStatus
    )
    {
        var (service, requests, pets, _) = CreateService();

        var pet = CreatePet(status: petStatus);
        var request = CreateRequest(3, pet);

        SetRequests(requests, request);

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.CancelAsync(
                    request.Id,
                    request.AdopterId
                )
            );

        Assert.Equal(
            "The pet is no longer reserved for this request.",
            exception.Message
        );

        Assert.Equal(AdoptionRequestStatus.Pending, request.Status);
        Assert.Equal(petStatus, pet.Status);
        Assert.Null(request.DecisionAt);

        requests.Verify(
            repository =>
                repository.UpdateAsync(It.IsAny<AdoptionRequest>()),
            Times.Never
        );

        pets.Verify(
            repository => repository.UpdateAsync(It.IsAny<Pet>()),
            Times.Never
        );
    }

    [Fact]
    public async Task CancelAsync_WhenRequestDoesNotExist_ThrowsArgumentException()
    {
        var (service, _, _, _) = CreateService();

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => service.CancelAsync(999, 2)
        );

        Assert.Equal("requestId", exception.ParamName);
        Assert.StartsWith(
            "Adoption request not found.",
            exception.Message
        );
    }

    [Fact]
    public async Task GetShelterIdForRequestAsync_WhenValid_ReturnsShelterId()
    {
        var (service, requests, _, _) = CreateService();

        var pet = CreatePet(shelterId: 77);
        var request = CreateRequest(3, pet);

        SetRequests(requests, request);

        var shelterId = await service.GetShelterIdForRequestAsync(
            request.Id
        );

        Assert.Equal(77, shelterId);
    }

    [Fact]
    public async Task GetShelterIdForRequestAsync_WhenRequestDoesNotExist_ThrowsArgumentException()
    {
        var (service, _, _, _) = CreateService();

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => service.GetShelterIdForRequestAsync(999)
        );

        Assert.Equal("requestId", exception.ParamName);
        Assert.StartsWith(
            "Adoption request not found.",
            exception.Message
        );
    }

    [Fact]
    public async Task GetForAdopterAsync_ReturnsOnlyOwnRequestsInDescendingOrder()
    {
        var (service, requests, _, _) = CreateService();

        var pet = CreatePet();

        var olderRequest = CreateRequest(
            1,
            pet,
            adopterId: 2,
            createdAt: new DateTime(2026, 1, 1)
        );

        var newerRequest = CreateRequest(
            2,
            pet,
            adopterId: 2,
            createdAt: new DateTime(2026, 2, 1)
        );

        var otherAdopterRequest = CreateRequest(
            3,
            pet,
            adopterId: 99,
            createdAt: new DateTime(2026, 3, 1)
        );

        SetRequests(
            requests,
            olderRequest,
            newerRequest,
            otherAdopterRequest
        );

        var result = await service.GetForAdopterAsync(2);

        Assert.Equal(2, result.Count);
        Assert.Equal(2, result[0].Id);
        Assert.Equal(1, result[1].Id);

        Assert.All(
            result,
            request => Assert.Equal(2, request.AdopterId)
        );

        Assert.Equal("Luna", result[0].PetName);
        Assert.Equal("Test Adopter", result[0].AdopterName);
    }

    [Fact]
    public async Task GetForShelterAsync_ReturnsOnlyShelterRequests()
    {
        var (service, requests, _, _) = CreateService();

        var shelterPet = CreatePet(
            id: 1,
            shelterId: 10
        );

        var otherShelterPet = CreatePet(
            id: 2,
            shelterId: 20
        );

        var matchingRequest = CreateRequest(
            3,
            shelterPet
        );

        var otherRequest = CreateRequest(
            4,
            otherShelterPet
        );

        SetRequests(
            requests,
            matchingRequest,
            otherRequest
        );

        var result = await service.GetForShelterAsync(10);

        var request = Assert.Single(result);

        Assert.Equal(matchingRequest.Id, request.Id);
        Assert.Equal(shelterPet.Id, request.PetId);
    }

    private static (
        AdoptionService Service,
        Mock<IRepository<AdoptionRequest>> Requests,
        Mock<IRepository<Pet>> Pets,
        Mock<IRepository<Adopter>> Adopters
    ) CreateService()
    {
        var requests = new Mock<IRepository<AdoptionRequest>>();
        var pets = new Mock<IRepository<Pet>>();
        var adopters = new Mock<IRepository<Adopter>>();

        SetRequests(requests);

        var service = new AdoptionService(
            requests.Object,
            pets.Object,
            adopters.Object
        );

        return (service, requests, pets, adopters);
    }

    private static void SetupPetAndAdopter(
        Mock<IRepository<Pet>> pets,
        Mock<IRepository<Adopter>> adopters,
        Pet pet,
        Adopter adopter
    )
    {
        pets
            .Setup(repository => repository.GetByIdAsync(pet.Id))
            .ReturnsAsync(pet);

        adopters
            .Setup(repository => repository.GetByIdAsync(adopter.Id))
            .ReturnsAsync(adopter);
    }

    private static void SetRequests(
        Mock<IRepository<AdoptionRequest>> repository,
        params AdoptionRequest[] items
    )
    {
        repository
            .Setup(requests => requests.ListAsync(
                It.IsAny<
                    Expression<Func<AdoptionRequest, bool>>
                >(),
                It.IsAny<
                    Expression<Func<AdoptionRequest, object>>[]
                >()
            ))
            .Returns((
                Expression<Func<AdoptionRequest, bool>> predicate,
                Expression<Func<AdoptionRequest, object>>[] _
            ) =>
                Task.FromResult<IReadOnlyList<AdoptionRequest>>(
                    items
                        .Where(predicate.Compile())
                        .ToArray()
                )
            );
    }

    private static Pet CreatePet(
        int id = 1,
        PetStatus status = PetStatus.Available,
        int shelterId = 10
    )
    {
        return new Pet
        {
            Id = id,
            Name = "Luna",
            Status = status,
            ShelterId = shelterId
        };
    }

    private static Adopter CreateAdopter(int id = 2)
    {
        return new Adopter
        {
            Id = id,
            FullName = "Test Adopter"
        };
    }

    private static AdoptionRequest CreateRequest(
        int id,
        Pet pet,
        int adopterId = 2,
        AdoptionRequestStatus status = AdoptionRequestStatus.Pending,
        DateTime? createdAt = null
    )
    {
        var adopter = CreateAdopter(adopterId);

        return new AdoptionRequest
        {
            Id = id,
            PetId = pet.Id,
            Pet = pet,
            AdopterId = adopter.Id,
            Adopter = adopter,
            Status = status,
            CreatedAt = createdAt ?? DateTime.UtcNow
        };
    }
}