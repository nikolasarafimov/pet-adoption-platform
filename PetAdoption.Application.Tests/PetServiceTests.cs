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
        var shelter = new Shelter
        {
            Id = 10,
            Name = "Happy Tails",
            Address = "Test Address",
            City = "Skopje"
        };

        var petRepository =
            new Mock<IRepository<Pet>>();

        var shelterRepository =
            new Mock<IRepository<Shelter>>();

        shelterRepository
            .Setup(repository =>
                repository.GetByIdAsync(shelter.Id))
            .ReturnsAsync(shelter);

        Pet? createdPet = null;

        petRepository
            .Setup(repository =>
                repository.AddAsync(
                    It.IsAny<Pet>()
                ))
            .Callback<Pet>(
                pet =>
                {
                    pet.Id = 42;
                    createdPet = pet;
                }
            )
            .ReturnsAsync(
                (Pet pet) => pet
            );

        petRepository
            .Setup(repository =>
                repository.SaveChangesAsync())
            .ReturnsAsync(1);

        var service = new PetService(
            petRepository.Object,
            shelterRepository.Object
        );

        var id = await service.CreateAsync(
            "  Luna  ",
            Species.Dog,
            "  Labrador  ",
            4,
            "  Friendly and playful  ",
            "  https://example.com/luna.jpg  ",
            shelter.Id
        );

        Assert.Equal(
            42,
            id
        );

        Assert.NotNull(createdPet);

        Assert.Equal(
            "Luna",
            createdPet.Name
        );

        Assert.Equal(
            Species.Dog,
            createdPet.Species
        );

        Assert.Equal(
            "Labrador",
            createdPet.Breed
        );

        Assert.Equal(
            4,
            createdPet.AgeYears
        );

        Assert.Equal(
            "Friendly and playful",
            createdPet.Description
        );

        Assert.Equal(
            "https://example.com/luna.jpg",
            createdPet.ImageUrl
        );

        Assert.Equal(
            shelter.Id,
            createdPet.ShelterId
        );

        Assert.Equal(
            PetStatus.Available,
            createdPet.Status
        );

        petRepository.Verify(
            repository =>
                repository.AddAsync(
                    It.IsAny<Pet>()
                ),
            Times.Once
        );

        petRepository.Verify(
            repository =>
                repository.SaveChangesAsync(),
            Times.Once
        );
    }

    [Fact]
    public async Task CreateAsync_WhenOptionalValuesAreWhitespace_StoresNull()
    {
        var shelter = new Shelter
        {
            Id = 10,
            Name = "Happy Tails",
            Address = "Test Address",
            City = "Skopje"
        };

        var petRepository =
            new Mock<IRepository<Pet>>();

        var shelterRepository =
            new Mock<IRepository<Shelter>>();

        shelterRepository
            .Setup(repository =>
                repository.GetByIdAsync(shelter.Id))
            .ReturnsAsync(shelter);

        Pet? createdPet = null;

        petRepository
            .Setup(repository =>
                repository.AddAsync(
                    It.IsAny<Pet>()
                ))
            .Callback<Pet>(
                pet => createdPet = pet
            )
            .ReturnsAsync(
                (Pet pet) => pet
            );

        petRepository
            .Setup(repository =>
                repository.SaveChangesAsync())
            .ReturnsAsync(1);

        var service = new PetService(
            petRepository.Object,
            shelterRepository.Object
        );

        await service.CreateAsync(
            "Luna",
            Species.Dog,
            "   ",
            3,
            "",
            " ",
            shelter.Id
        );

        Assert.NotNull(createdPet);

        Assert.Null(createdPet.Breed);
        Assert.Null(createdPet.Description);
        Assert.Null(createdPet.ImageUrl);
    }

    [Fact]
    public async Task CreateAsync_WhenNameIsBlank_ThrowsArgumentException()
    {
        var service = new PetService(
            Mock.Of<IRepository<Pet>>(),
            Mock.Of<IRepository<Shelter>>()
        );

        var exception =
            await Assert.ThrowsAsync<ArgumentException>(
                () => service.CreateAsync(
                    "   ",
                    Species.Dog,
                    null,
                    3,
                    null,
                    null,
                    1
                )
            );

        Assert.Equal(
            "Pet name is required.",
            exception.Message
        );
    }

    [Fact]
    public async Task CreateAsync_WhenAgeIsNegative_ThrowsArgumentOutOfRangeException()
    {
        var service = new PetService(
            Mock.Of<IRepository<Pet>>(),
            Mock.Of<IRepository<Shelter>>()
        );

        await Assert.ThrowsAsync<
            ArgumentOutOfRangeException
        >(
            () => service.CreateAsync(
                "Luna",
                Species.Dog,
                null,
                -1,
                null,
                null,
                1
            )
        );
    }

    [Fact]
    public async Task CreateAsync_WhenShelterDoesNotExist_ThrowsArgumentException()
    {
        var shelterRepository =
            new Mock<IRepository<Shelter>>();

        shelterRepository
            .Setup(repository =>
                repository.GetByIdAsync(999))
            .ReturnsAsync((Shelter?)null);

        var service = new PetService(
            Mock.Of<IRepository<Pet>>(),
            shelterRepository.Object
        );

        var exception =
            await Assert.ThrowsAsync<ArgumentException>(
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

        Assert.Equal(
            "Shelter not found.",
            exception.Message
        );
    }

    [Fact]
    public async Task UpdateAsync_WhenValid_UpdatesAndNormalizesPet()
    {
        var pet = new Pet
        {
            Id = 1,
            Name = "Old Name",
            Species = Species.Cat,
            Breed = "Old Breed",
            AgeYears = 2,
            Description = "Old description",
            ImageUrl = "https://example.com/old.jpg",
            ShelterId = 5,
            Status = PetStatus.Available
        };

        var shelter = new Shelter
        {
            Id = 10,
            Name = "New Shelter",
            Address = "Test Address",
            City = "Skopje"
        };

        var petRepository =
            new Mock<IRepository<Pet>>();

        var shelterRepository =
            new Mock<IRepository<Shelter>>();

        petRepository
            .Setup(repository =>
                repository.GetByIdAsync(pet.Id))
            .ReturnsAsync(pet);

        shelterRepository
            .Setup(repository =>
                repository.GetByIdAsync(shelter.Id))
            .ReturnsAsync(shelter);

        petRepository
            .Setup(repository =>
                repository.UpdateAsync(pet))
            .Returns(Task.CompletedTask);

        petRepository
            .Setup(repository =>
                repository.SaveChangesAsync())
            .ReturnsAsync(1);

        var service = new PetService(
            petRepository.Object,
            shelterRepository.Object
        );

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

        Assert.Equal(
            "Luna",
            pet.Name
        );

        Assert.Equal(
            Species.Dog,
            pet.Species
        );

        Assert.Equal(
            "Labrador",
            pet.Breed
        );

        Assert.Equal(
            5,
            pet.AgeYears
        );

        Assert.Null(
            pet.Description
        );

        Assert.Equal(
            "https://example.com/luna.jpg",
            pet.ImageUrl
        );

        Assert.Equal(
            shelter.Id,
            pet.ShelterId
        );

        Assert.Equal(
            PetStatus.Unavailable,
            pet.Status
        );

        petRepository.Verify(
            repository =>
                repository.UpdateAsync(pet),
            Times.Once
        );

        petRepository.Verify(
            repository =>
                repository.SaveChangesAsync(),
            Times.Once
        );
    }

    [Fact]
    public async Task UpdateAsync_WhenPetDoesNotExist_ThrowsArgumentException()
    {
        var petRepository =
            new Mock<IRepository<Pet>>();

        petRepository
            .Setup(repository =>
                repository.GetByIdAsync(999))
            .ReturnsAsync((Pet?)null);

        var service = new PetService(
            petRepository.Object,
            Mock.Of<IRepository<Shelter>>()
        );

        var exception =
            await Assert.ThrowsAsync<ArgumentException>(
                () => service.UpdateAsync(
                    999,
                    "Luna",
                    Species.Dog,
                    null,
                    3,
                    null,
                    null,
                    1,
                    PetStatus.Available
                )
            );

        Assert.Equal(
            "Pet not found.",
            exception.Message
        );
    }

    [Fact]
    public async Task UpdateAsync_WhenShelterDoesNotExist_ThrowsArgumentException()
    {
        var pet = new Pet
        {
            Id = 1,
            Name = "Luna",
            Species = Species.Dog,
            AgeYears = 3,
            ShelterId = 1,
            Status = PetStatus.Available
        };

        var petRepository =
            new Mock<IRepository<Pet>>();

        var shelterRepository =
            new Mock<IRepository<Shelter>>();

        petRepository
            .Setup(repository =>
                repository.GetByIdAsync(pet.Id))
            .ReturnsAsync(pet);

        shelterRepository
            .Setup(repository =>
                repository.GetByIdAsync(999))
            .ReturnsAsync((Shelter?)null);

        var service = new PetService(
            petRepository.Object,
            shelterRepository.Object
        );

        var exception =
            await Assert.ThrowsAsync<ArgumentException>(
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

        Assert.Equal(
            "Shelter not found.",
            exception.Message
        );

        petRepository.Verify(
            repository =>
                repository.UpdateAsync(
                    It.IsAny<Pet>()
                ),
            Times.Never
        );
    }

    [Fact]
    public async Task DeleteAsync_WhenValid_DeletesPetAndSavesChanges()
    {
        var pet = new Pet
        {
            Id = 1,
            Name = "Luna"
        };

        var petRepository =
            new Mock<IRepository<Pet>>();

        petRepository
            .Setup(repository =>
                repository.GetByIdAsync(pet.Id))
            .ReturnsAsync(pet);

        petRepository
            .Setup(repository =>
                repository.DeleteAsync(pet))
            .Returns(Task.CompletedTask);

        petRepository
            .Setup(repository =>
                repository.SaveChangesAsync())
            .ReturnsAsync(1);

        var service = new PetService(
            petRepository.Object,
            Mock.Of<IRepository<Shelter>>()
        );

        await service.DeleteAsync(pet.Id);

        petRepository.Verify(
            repository =>
                repository.DeleteAsync(pet),
            Times.Once
        );

        petRepository.Verify(
            repository =>
                repository.SaveChangesAsync(),
            Times.Once
        );
    }

    [Fact]
    public async Task DeleteAsync_WhenPetDoesNotExist_ThrowsArgumentException()
    {
        var petRepository =
            new Mock<IRepository<Pet>>();

        petRepository
            .Setup(repository =>
                repository.GetByIdAsync(999))
            .ReturnsAsync((Pet?)null);

        var service = new PetService(
            petRepository.Object,
            Mock.Of<IRepository<Shelter>>()
        );

        var exception =
            await Assert.ThrowsAsync<ArgumentException>(
                () => service.DeleteAsync(999)
            );

        Assert.Equal(
            "Pet not found.",
            exception.Message
        );
    }
}