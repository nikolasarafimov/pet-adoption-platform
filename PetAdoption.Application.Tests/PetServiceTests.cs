using System.Linq.Expressions;
using Moq;
using PetAdoption.Application.Services;
using PetAdoption.Domain.Abstractions;
using PetAdoption.Domain.Entities;
using PetAdoption.Domain.Enums;

namespace PetAdoption.Application.Tests;

public class PetServiceTests
{
    [Fact]
    public async Task CreateAsync_WhenValid_CreatesAvailablePetAndNormalizesValues()
    {
        var (service, pets, shelters, _) = CreateService();

        var shelter = CreateShelter();

        shelters
            .Setup(repository => repository.GetByIdAsync(shelter.Id))
            .ReturnsAsync(shelter);

        Pet? createdPet = null;

        pets
            .Setup(repository => repository.AddAsync(It.IsAny<Pet>()))
            .Callback<Pet>(pet =>
            {
                pet.Id = 42;
                createdPet = pet;
            })
            .ReturnsAsync((Pet pet) => pet);

        pets
            .Setup(repository => repository.SaveChangesAsync())
            .ReturnsAsync(1);

        var id = await service.CreateAsync(
            "  Luna  ",
            Species.Dog,
            "  Labrador  ",
            4,
            "  Friendly and playful  ",
            "  https://example.com/luna.jpg  ",
            shelter.Id
        );

        Assert.Equal(42, id);

        var savedPet = Assert.IsType<Pet>(createdPet);

        Assert.Equal("Luna", savedPet.Name);
        Assert.Equal(Species.Dog, savedPet.Species);
        Assert.Equal("Labrador", savedPet.Breed);
        Assert.Equal(4, savedPet.AgeYears);
        Assert.Equal("Friendly and playful", savedPet.Description);
        Assert.Equal("https://example.com/luna.jpg", savedPet.ImageUrl);
        Assert.Equal(shelter.Id, savedPet.ShelterId);
        Assert.Equal(PetStatus.Available, savedPet.Status);

        pets.Verify(
            repository => repository.AddAsync(It.IsAny<Pet>()),
            Times.Once
        );

        pets.Verify(
            repository => repository.SaveChangesAsync(),
            Times.Once
        );
    }

    [Fact]
    public async Task CreateAsync_WhenOptionalValuesAreWhitespace_StoresNull()
    {
        var (service, pets, shelters, _) = CreateService();

        shelters
            .Setup(repository => repository.GetByIdAsync(10))
            .ReturnsAsync(CreateShelter());

        Pet? createdPet = null;

        pets
            .Setup(repository => repository.AddAsync(It.IsAny<Pet>()))
            .Callback<Pet>(pet => createdPet = pet)
            .ReturnsAsync((Pet pet) => pet);

        await service.CreateAsync(
            "Luna",
            Species.Dog,
            "   ",
            3,
            "",
            " ",
            10
        );

        var savedPet = Assert.IsType<Pet>(createdPet);

        Assert.Null(savedPet.Breed);
        Assert.Null(savedPet.Description);
        Assert.Null(savedPet.ImageUrl);
    }

    [Fact]
    public async Task CreateAsync_WhenNameIsBlank_ThrowsArgumentException()
    {
        var (service, _, _, _) = CreateService();

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => service.CreateAsync(
                "   ",
                Species.Dog,
                null,
                3,
                null,
                null,
                10
            )
        );

        Assert.Equal("Pet name is required.", exception.Message);
    }

    [Fact]
    public async Task CreateAsync_WhenNameIsTooLong_ThrowsArgumentException()
    {
        var (service, _, _, _) = CreateService();

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => service.CreateAsync(
                new string('A', 129),
                Species.Dog,
                null,
                3,
                null,
                null,
                10
            )
        );

        Assert.Equal(
            "Pet name cannot exceed 128 characters.",
            exception.Message
        );
    }

    [Fact]
    public async Task CreateAsync_WhenAgeIsNegative_ThrowsArgumentOutOfRangeException()
    {
        var (service, _, _, _) = CreateService();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => service.CreateAsync(
                "Luna",
                Species.Dog,
                null,
                -1,
                null,
                null,
                10
            )
        );
    }

    [Fact]
    public async Task CreateAsync_WhenAgeExceedsMaximum_ThrowsArgumentOutOfRangeException()
    {
        var (service, _, _, _) = CreateService();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => service.CreateAsync(
                "Luna",
                Species.Dog,
                null,
                201,
                null,
                null,
                10
            )
        );
    }

    [Fact]
    public async Task CreateAsync_WhenSpeciesIsInvalid_ThrowsArgumentOutOfRangeException()
    {
        var (service, _, _, _) = CreateService();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => service.CreateAsync(
                "Luna",
                (Species)999,
                null,
                3,
                null,
                null,
                10
            )
        );
    }

    [Fact]
    public async Task CreateAsync_WhenBreedIsTooLong_ThrowsArgumentException()
    {
        var (service, _, _, _) = CreateService();

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => service.CreateAsync(
                "Luna",
                Species.Dog,
                new string('B', 129),
                3,
                null,
                null,
                10
            )
        );

        Assert.Contains("128 characters", exception.Message);
    }

    [Fact]
    public async Task CreateAsync_WhenDescriptionIsTooLong_ThrowsArgumentException()
    {
        var (service, _, _, _) = CreateService();

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => service.CreateAsync(
                "Luna",
                Species.Dog,
                null,
                3,
                new string('D', 1025),
                null,
                10
            )
        );

        Assert.Contains("1024 characters", exception.Message);
    }

    [Fact]
    public async Task CreateAsync_WhenImageUrlIsInvalid_ThrowsArgumentException()
    {
        var (service, _, _, _) = CreateService();

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => service.CreateAsync(
                "Luna",
                Species.Dog,
                null,
                3,
                null,
                "ftp://example.com/luna.jpg",
                10
            )
        );

        Assert.Equal(
            "Image URL must be a valid HTTP or HTTPS URL.",
            exception.Message
        );
    }

    [Fact]
    public async Task CreateAsync_WhenShelterDoesNotExist_ThrowsArgumentException()
    {
        var (service, pets, _, _) = CreateService();

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => service.CreateAsync(
                "Luna",
                Species.Dog,
                null,
                3,
                null,
                null,
                999
            )
        );

        Assert.Equal("Shelter not found.", exception.Message);

        pets.Verify(
            repository => repository.AddAsync(It.IsAny<Pet>()),
            Times.Never
        );
    }

    [Fact]
    public async Task UpdateAsync_WhenValid_UpdatesAndNormalizesPet()
    {
        var (service, pets, shelters, _) = CreateService();

        var pet = CreatePet(shelterId: 5);
        pet.Name = "Old Name";
        pet.Species = Species.Cat;
        pet.Breed = "Old Breed";
        pet.AgeYears = 2;
        pet.Description = "Old description";
        pet.ImageUrl = "https://example.com/old.jpg";

        var shelter = CreateShelter();

        pets
            .Setup(repository => repository.GetByIdAsync(pet.Id))
            .ReturnsAsync(pet);

        shelters
            .Setup(repository => repository.GetByIdAsync(shelter.Id))
            .ReturnsAsync(shelter);

        pets
            .Setup(repository => repository.UpdateAsync(pet))
            .Returns(Task.CompletedTask);

        pets
            .Setup(repository => repository.SaveChangesAsync())
            .ReturnsAsync(1);

        await service.UpdateAsync(
            pet.Id,
            "  Luna  ",
            Species.Dog,
            "  Labrador  ",
            5,
            "   ",
            "  https://example.com/luna.jpg  ",
            shelter.Id,
            PetStatus.Unavailable
        );

        Assert.Equal("Luna", pet.Name);
        Assert.Equal(Species.Dog, pet.Species);
        Assert.Equal("Labrador", pet.Breed);
        Assert.Equal(5, pet.AgeYears);
        Assert.Null(pet.Description);
        Assert.Equal("https://example.com/luna.jpg", pet.ImageUrl);
        Assert.Equal(shelter.Id, pet.ShelterId);
        Assert.Equal(PetStatus.Unavailable, pet.Status);

        pets.Verify(
            repository => repository.UpdateAsync(pet),
            Times.Once
        );

        pets.Verify(
            repository => repository.SaveChangesAsync(),
            Times.Once
        );
    }

    [Fact]
    public async Task UpdateAsync_WhenPetDoesNotExist_ThrowsArgumentException()
    {
        var (service, pets, _, _) = CreateService();

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => service.UpdateAsync(
                999,
                "Luna",
                Species.Dog,
                null,
                3,
                null,
                null,
                10,
                PetStatus.Available
            )
        );

        Assert.Equal("Pet not found.", exception.Message);

        pets.Verify(
            repository => repository.UpdateAsync(It.IsAny<Pet>()),
            Times.Never
        );
    }

    [Fact]
    public async Task UpdateAsync_WhenShelterDoesNotExist_ThrowsArgumentException()
    {
        var (service, pets, _, _) = CreateService();

        var pet = CreatePet();

        pets
            .Setup(repository => repository.GetByIdAsync(pet.Id))
            .ReturnsAsync(pet);

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => service.UpdateAsync(
                pet.Id,
                "Luna",
                Species.Dog,
                null,
                3,
                null,
                null,
                999,
                PetStatus.Available
            )
        );

        Assert.Equal("Shelter not found.", exception.Message);

        pets.Verify(
            repository => repository.UpdateAsync(It.IsAny<Pet>()),
            Times.Never
        );
    }

    [Fact]
    public async Task UpdateAsync_WhenStatusIsInvalid_ThrowsArgumentOutOfRangeException()
    {
        var (service, _, _, _) = CreateService();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => service.UpdateAsync(
                1,
                "Luna",
                Species.Dog,
                null,
                3,
                null,
                null,
                10,
                (PetStatus)999
            )
        );
    }

    [Theory]
    [InlineData(PetStatus.Available, PetStatus.Reserved)]
    [InlineData(PetStatus.Available, PetStatus.Adopted)]
    [InlineData(PetStatus.Reserved, PetStatus.Available)]
    [InlineData(PetStatus.Adopted, PetStatus.Available)]
    public async Task UpdateAsync_WhenAdoptionStatusChangedDirectly_ThrowsInvalidOperationException(
        PetStatus currentStatus,
        PetStatus newStatus
    )
    {
        var (service, pets, shelters, _) = CreateService();

        var pet = CreatePet(status: currentStatus);

        pets
            .Setup(repository => repository.GetByIdAsync(pet.Id))
            .ReturnsAsync(pet);

        shelters
            .Setup(repository => repository.GetByIdAsync(10))
            .ReturnsAsync(CreateShelter());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdateAsync(
                pet.Id,
                "Luna",
                Species.Dog,
                null,
                3,
                null,
                null,
                10,
                newStatus
            )
        );

        Assert.Equal(
            "Reserved and adopted statuses can only be changed through the adoption workflow.",
            exception.Message
        );

        Assert.Equal(currentStatus, pet.Status);

        pets.Verify(
            repository => repository.UpdateAsync(It.IsAny<Pet>()),
            Times.Never
        );
    }

    [Fact]
    public async Task UpdateAsync_WhenReservedPetChangesShelter_ThrowsInvalidOperationException()
    {
        var (service, pets, shelters, _) = CreateService();

        var pet = CreatePet(
            status: PetStatus.Reserved,
            shelterId: 5
        );

        pets
            .Setup(repository => repository.GetByIdAsync(pet.Id))
            .ReturnsAsync(pet);

        shelters
            .Setup(repository => repository.GetByIdAsync(10))
            .ReturnsAsync(CreateShelter());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdateAsync(
                pet.Id,
                "Luna",
                Species.Dog,
                null,
                3,
                null,
                null,
                10,
                PetStatus.Reserved
            )
        );

        Assert.Equal(
            "Cannot change the shelter of a reserved or adopted pet.",
            exception.Message
        );

        Assert.Equal(5, pet.ShelterId);
    }

    [Fact]
    public async Task UpdateAsync_WhenShelterChangesWithAdoptionHistory_ThrowsInvalidOperationException()
    {
        var (service, pets, shelters, requests) = CreateService();

        var pet = CreatePet(shelterId: 5);

        pets
            .Setup(repository => repository.GetByIdAsync(pet.Id))
            .ReturnsAsync(pet);

        shelters
            .Setup(repository => repository.GetByIdAsync(10))
            .ReturnsAsync(CreateShelter());

        ConfigureAdoptionHistory(requests, pet.Id);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdateAsync(
                pet.Id,
                "Luna",
                Species.Dog,
                null,
                3,
                null,
                null,
                10,
                PetStatus.Available
            )
        );

        Assert.Equal(
            "Cannot change the shelter of a pet with adoption history.",
            exception.Message
        );

        Assert.Equal(5, pet.ShelterId);

        pets.Verify(
            repository => repository.UpdateAsync(It.IsAny<Pet>()),
            Times.Never
        );
    }

    [Fact]
    public async Task UpdateAsync_WhenReservedStatusIsUnchanged_AllowsProfileEdit()
    {
        var (service, pets, shelters, _) = CreateService();

        var pet = CreatePet(status: PetStatus.Reserved);

        pets
            .Setup(repository => repository.GetByIdAsync(pet.Id))
            .ReturnsAsync(pet);

        shelters
            .Setup(repository => repository.GetByIdAsync(10))
            .ReturnsAsync(CreateShelter());

        await service.UpdateAsync(
            pet.Id,
            "  Luna Updated  ",
            Species.Dog,
            null,
            3,
            null,
            null,
            10,
            PetStatus.Reserved
        );

        Assert.Equal("Luna Updated", pet.Name);
        Assert.Equal(PetStatus.Reserved, pet.Status);

        pets.Verify(
            repository => repository.UpdateAsync(pet),
            Times.Once
        );
    }

    [Fact]
    public async Task DeleteAsync_WhenValid_DeletesPetAndSavesChanges()
    {
        var (service, pets, _, _) = CreateService();

        var pet = CreatePet();

        pets
            .Setup(repository => repository.GetByIdAsync(pet.Id))
            .ReturnsAsync(pet);

        pets
            .Setup(repository => repository.DeleteAsync(pet))
            .Returns(Task.CompletedTask);

        pets
            .Setup(repository => repository.SaveChangesAsync())
            .ReturnsAsync(1);

        await service.DeleteAsync(pet.Id);

        pets.Verify(
            repository => repository.DeleteAsync(pet),
            Times.Once
        );

        pets.Verify(
            repository => repository.SaveChangesAsync(),
            Times.Once
        );
    }

    [Fact]
    public async Task DeleteAsync_WhenPetDoesNotExist_ThrowsArgumentException()
    {
        var (service, pets, _, _) = CreateService();

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => service.DeleteAsync(999)
        );

        Assert.Equal("Pet not found.", exception.Message);

        pets.Verify(
            repository => repository.DeleteAsync(It.IsAny<Pet>()),
            Times.Never
        );
    }

    [Fact]
    public async Task DeleteAsync_WhenAdoptionHistoryExists_ThrowsInvalidOperationException()
    {
        var (service, pets, _, requests) = CreateService();

        var pet = CreatePet();

        pets
            .Setup(repository => repository.GetByIdAsync(pet.Id))
            .ReturnsAsync(pet);

        ConfigureAdoptionHistory(requests, pet.Id);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.DeleteAsync(pet.Id)
        );

        Assert.Equal(
            "Cannot delete a pet with existing adoption requests.",
            exception.Message
        );

        pets.Verify(
            repository => repository.DeleteAsync(It.IsAny<Pet>()),
            Times.Never
        );
    }

    [Theory]
    [InlineData(PetStatus.Reserved)]
    [InlineData(PetStatus.Adopted)]
    public async Task DeleteAsync_WhenPetHasProtectedStatus_ThrowsInvalidOperationException(
        PetStatus status
    )
    {
        var (service, pets, _, _) = CreateService();

        var pet = CreatePet(status: status);

        pets
            .Setup(repository => repository.GetByIdAsync(pet.Id))
            .ReturnsAsync(pet);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.DeleteAsync(pet.Id)
        );

        Assert.Equal(
            "Cannot delete a reserved or adopted pet.",
            exception.Message
        );

        pets.Verify(
            repository => repository.DeleteAsync(It.IsAny<Pet>()),
            Times.Never
        );
    }

    [Fact]
    public async Task GetAsync_WhenPetExists_ReturnsMappedDto()
    {
        var (service, pets, _, _) = CreateService();

        var pet = CreatePet();
        pet.Shelter = CreateShelter();

        pets
            .Setup(repository => repository.ListAsync(
                It.IsAny<Expression<Func<Pet, bool>>>(),
                It.IsAny<Expression<Func<Pet, object>>[]>()
            ))
            .ReturnsAsync(new[] { pet });

        var result = await service.GetAsync(pet.Id);

        Assert.NotNull(result);
        Assert.Equal(pet.Id, result.Id);
        Assert.Equal("Luna", result.Name);
        Assert.Equal("Happy Tails", result.ShelterName);
        Assert.Equal(PetStatus.Available, result.Status);
    }

    [Fact]
    public async Task GetAsync_WhenPetDoesNotExist_ReturnsNull()
    {
        var (service, pets, _, _) = CreateService();

        pets
            .Setup(repository => repository.ListAsync(
                It.IsAny<Expression<Func<Pet, bool>>>(),
                It.IsAny<Expression<Func<Pet, object>>[]>()
            ))
            .ReturnsAsync(Array.Empty<Pet>());

        var result = await service.GetAsync(999);

        Assert.Null(result);
    }

    private static (
        PetService Service,
        Mock<IRepository<Pet>> Pets,
        Mock<IRepository<Shelter>> Shelters,
        Mock<IRepository<AdoptionRequest>> Requests
    ) CreateService()
    {
        var pets = new Mock<IRepository<Pet>>();
        var shelters = new Mock<IRepository<Shelter>>();
        var requests = new Mock<IRepository<AdoptionRequest>>();

        requests
            .Setup(repository => repository.ListAsync(
                It.IsAny<Expression<Func<AdoptionRequest, bool>>>(),
                It.IsAny<Expression<Func<AdoptionRequest, object>>[]>()
            ))
            .ReturnsAsync(Array.Empty<AdoptionRequest>());

        var service = new PetService(
            pets.Object,
            shelters.Object,
            requests.Object
        );

        return (service, pets, shelters, requests);
    }

    private static Pet CreatePet(
        PetStatus status = PetStatus.Available,
        int shelterId = 10
    )
    {
        return new Pet
        {
            Id = 1,
            Name = "Luna",
            Species = Species.Dog,
            AgeYears = 3,
            ShelterId = shelterId,
            Status = status
        };
    }

    private static Shelter CreateShelter()
    {
        return new Shelter
        {
            Id = 10,
            Name = "Happy Tails",
            Address = "Test Address",
            City = "Skopje"
        };
    }

    private static void ConfigureAdoptionHistory(
        Mock<IRepository<AdoptionRequest>> requests,
        int petId
    )
    {
        var existingRequest = new AdoptionRequest
        {
            Id = 5,
            PetId = petId,
            AdopterId = 2,
            Status = AdoptionRequestStatus.Rejected
        };

        requests
            .Setup(repository => repository.ListAsync(
                It.IsAny<Expression<Func<AdoptionRequest, bool>>>(),
                It.IsAny<Expression<Func<AdoptionRequest, object>>[]>()
            ))
            .ReturnsAsync(new[] { existingRequest });
    }
}