using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetAdoption.Application.Interfaces;
using PetAdoption.Domain.Enums;
using PetAdoption.Infrastructure.Data;
using PetAdoption.Web.Models.Pets;

namespace PetAdoption.Web.Controllers
{
    [Authorize]
    public class PetsController : Controller
    {
        private readonly IPetService _petService;
        private readonly IShelterService _shelterService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<PetsController> _logger;

        public PetsController(
            IPetService petService,
            IShelterService shelterService,
            UserManager<ApplicationUser> userManager,
            ILogger<PetsController> logger
        )
        {
            _petService = petService;
            _shelterService = shelterService;
            _userManager = userManager;
            _logger = logger;
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Index(
            string? search,
            Species? species,
            PetStatus? status
        )
        {
            if (!ModelState.IsValid ||
                (species.HasValue &&
                 !Enum.IsDefined(species.Value)) ||
                (status.HasValue &&
                 !Enum.IsDefined(status.Value)))
            {
                return BadRequest("Invalid filter parameters.");
            }

            var isAdmin = User.IsInRole("Admin");
            var isShelter = User.IsInRole("Shelter");
            var isAdopter = User.IsInRole("Adopter");

            int? shelterId = null;

            if (isShelter && !isAdmin)
            {
                shelterId = await GetCurrentShelterIdAsync();

                if (!shelterId.HasValue)
                {
                    return Forbid();
                }
            }

            var normalizedSearch = string.IsNullOrWhiteSpace(search)
                ? null
                : search.Trim();

            var pets = await _petService.GetAllAsync(
                search: normalizedSearch,
                species: species,
                shelterId: shelterId
            );

            var filteredPets = pets.AsEnumerable();

            if (status.HasValue)
            {
                filteredPets = filteredPets.Where(
                    pet => pet.Status == status.Value
                );
            }

            var viewModel = new PetListVm
            {
                Pets = filteredPets
                    .OrderBy(
                        pet => pet.Name,
                        StringComparer.OrdinalIgnoreCase
                    )
                    .ThenBy(pet => pet.Id)
                    .ToList(),

                IsAdmin = isAdmin,
                IsShelter = isShelter,
                IsAdopter = isAdopter,
                MyShelterId = shelterId,
                Search = normalizedSearch,
                Species = species,
                Status = status
            };

            return View(viewModel);
        }

        [Authorize(Roles = "Admin,Shelter")]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new PetFormVm();

            if (IsShelterOnly())
            {
                var shelterId = await GetCurrentShelterIdAsync();

                if (!shelterId.HasValue)
                {
                    return Forbid();
                }

                model.ShelterId = shelterId.Value;
            }

            await LoadSheltersAsync();

            return View(model);
        }

        [Authorize(Roles = "Admin,Shelter")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PetFormVm model)
        {
            if (IsShelterOnly())
            {
                var shelterId = await GetCurrentShelterIdAsync();

                if (!shelterId.HasValue)
                {
                    return Forbid();
                }

                model.ShelterId = shelterId.Value;

                ModelState.Remove(nameof(PetFormVm.ShelterId));
            }

            if (!ModelState.IsValid)
            {
                await LoadSheltersAsync();

                return View(model);
            }

            try
            {
                await _petService.CreateAsync(
                    model.Name,
                    model.Species,
                    model.Breed,
                    model.AgeYears,
                    model.Description,
                    model.ImageUrl,
                    model.ShelterId
                );

                TempData["Ok"] = "Pet created successfully.";

                return RedirectToAction(nameof(Index));
            }
            catch (ArgumentException exception)
            {
                ModelState.AddModelError(
                    string.Empty,
                    exception.Message
                );
            }
            catch (DbUpdateException exception)
            {
                _logger.LogError(
                    exception,
                    "Database error while creating a pet for shelter {ShelterId}.",
                    model.ShelterId
                );

                ModelState.AddModelError(
                    string.Empty,
                    "Unable to create the pet. Please try again."
                );
            }

            await LoadSheltersAsync();

            return View(model);
        }

        [Authorize(Roles = "Admin,Shelter")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            if (id <= 0)
            {
                return NotFound();
            }

            var pet = await _petService.GetAsync(id);

            if (pet == null)
            {
                return NotFound();
            }

            if (!await CanManagePetAsync(pet.ShelterId))
            {
                return Forbid();
            }

            await LoadSheltersAsync();

            return View(PetFormVm.FromDto(pet));
        }

        [Authorize(Roles = "Admin,Shelter")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(PetFormVm model)
        {
            if (model.Id <= 0)
            {
                return NotFound();
            }

            var existingPet = await _petService.GetAsync(
                model.Id
            );

            if (existingPet == null)
            {
                return NotFound();
            }

            if (!await CanManagePetAsync(existingPet.ShelterId))
            {
                return Forbid();
            }

            if (IsShelterOnly())
            {
                model.ShelterId = existingPet.ShelterId;

                ModelState.Remove(nameof(PetFormVm.ShelterId));
            }

            if (!ModelState.IsValid)
            {
                await LoadSheltersAsync();

                return View(model);
            }

            try
            {
                await _petService.UpdateAsync(
                    model.Id,
                    model.Name,
                    model.Species,
                    model.Breed,
                    model.AgeYears,
                    model.Description,
                    model.ImageUrl,
                    model.ShelterId,
                    model.Status
                );

                TempData["Ok"] = "Pet updated successfully.";

                return RedirectToAction(nameof(Index));
            }
            catch (ArgumentException exception)
            {
                ModelState.AddModelError(
                    string.Empty,
                    exception.Message
                );
            }
            catch (InvalidOperationException exception)
            {
                ModelState.AddModelError(
                    string.Empty,
                    exception.Message
                );
            }
            catch (DbUpdateException exception)
            {
                _logger.LogError(
                    exception,
                    "Database error while updating pet {PetId}.",
                    model.Id
                );

                ModelState.AddModelError(
                    string.Empty,
                    "Unable to update the pet. Please try again."
                );
            }

            await LoadSheltersAsync();

            return View(model);
        }

        [Authorize(Roles = "Admin,Shelter")]
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            if (id <= 0)
            {
                return NotFound();
            }

            var pet = await _petService.GetAsync(id);

            if (pet == null)
            {
                return NotFound();
            }

            if (!await CanManagePetAsync(pet.ShelterId))
            {
                return Forbid();
            }

            return View(pet);
        }

        [Authorize(Roles = "Admin,Shelter")]
        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (id <= 0)
            {
                return NotFound();
            }

            var pet = await _petService.GetAsync(id);

            if (pet == null)
            {
                return NotFound();
            }

            if (!await CanManagePetAsync(pet.ShelterId))
            {
                return Forbid();
            }

            try
            {
                await _petService.DeleteAsync(id);

                TempData["Ok"] = "Pet deleted successfully.";
            }
            catch (ArgumentException exception)
            {
                TempData["Err"] = exception.Message;
            }
            catch (InvalidOperationException exception)
            {
                TempData["Err"] = exception.Message;
            }
            catch (DbUpdateException exception)
            {
                _logger.LogError(
                    exception,
                    "Database error while deleting pet {PetId}.",
                    id
                );

                TempData["Err"] =
                    "Unable to delete the pet because of related records or a database error.";
            }

            return RedirectToAction(nameof(Index));
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            if (id <= 0)
            {
                return NotFound();
            }

            var pet = await _petService.GetAsync(id);

            if (pet == null)
            {
                return NotFound();
            }

            return View(pet);
        }

        private async Task LoadSheltersAsync()
        {
            ViewBag.Shelters = await _shelterService.GetAllAsync();
        }

        private async Task<int?> GetCurrentShelterIdAsync()
        {
            var user = await _userManager.GetUserAsync(User);

            return user?.ShelterId;
        }

        private bool IsShelterOnly()
        {
            return User.IsInRole("Shelter") &&
                   !User.IsInRole("Admin");
        }

        private async Task<bool> CanManagePetAsync(
            int petShelterId
        )
        {
            if (User.IsInRole("Admin"))
            {
                return true;
            }

            if (!User.IsInRole("Shelter"))
            {
                return false;
            }

            var currentShelterId = await GetCurrentShelterIdAsync();

            return currentShelterId.HasValue &&
                   currentShelterId.Value == petShelterId;
        }
    }
}