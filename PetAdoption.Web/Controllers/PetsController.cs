using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
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

        public PetsController(
            IPetService petService,
            IShelterService shelterService,
            UserManager<ApplicationUser> userManager
        )
        {
            _petService = petService;
            _shelterService = shelterService;
            _userManager = userManager;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index(
            string? search,
            Species? species,
            PetStatus? status
        )
        {
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

            var normalizedSearch =
                string.IsNullOrWhiteSpace(search)
                    ? null
                    : search.Trim();

            var pets = await _petService.GetAllAsync(
                search: normalizedSearch,
                species: species,
                shelterId: shelterId
            );

            if (status.HasValue)
            {
                pets = pets
                    .Where(pet =>
                        pet.Status == status.Value
                    )
                    .ToList();
            }

            var viewModel = new PetListVm
            {
                Pets = pets,
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
            var viewModel = new PetFormVm();

            if (User.IsInRole("Shelter")
                && !User.IsInRole("Admin"))
            {
                var shelterId =
                    await GetCurrentShelterIdAsync();

                if (!shelterId.HasValue)
                {
                    return Forbid();
                }

                viewModel.ShelterId =
                    shelterId.Value;
            }

            await LoadSheltersAsync();

            return View(viewModel);
        }

        [Authorize(Roles = "Admin,Shelter")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            PetFormVm model
        )
        {
            if (User.IsInRole("Shelter")
                && !User.IsInRole("Admin"))
            {
                var shelterId =
                    await GetCurrentShelterIdAsync();

                if (!shelterId.HasValue)
                {
                    return Forbid();
                }

                model.ShelterId =
                    shelterId.Value;
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

                TempData["ok"] =
                    "Pet created.";

                return RedirectToAction(
                    nameof(Index)
                );
            }
            catch (ArgumentException exception)
            {
                ModelState.AddModelError(
                    string.Empty,
                    exception.Message
                );

                await LoadSheltersAsync();

                return View(model);
            }
        }

        [Authorize(Roles = "Admin,Shelter")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var pet =
                await _petService.GetAsync(id);

            if (pet == null)
            {
                return NotFound();
            }

            if (!await CanManagePetAsync(
                pet.ShelterId
            ))
            {
                return Forbid();
            }

            await LoadSheltersAsync();

            return View(
                PetFormVm.FromDto(pet)
            );
        }

        [Authorize(Roles = "Admin,Shelter")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            PetFormVm model
        )
        {
            var existingPet =
                await _petService.GetAsync(
                    model.Id
                );

            if (existingPet == null)
            {
                return NotFound();
            }

            if (!await CanManagePetAsync(
                existingPet.ShelterId
            ))
            {
                return Forbid();
            }

            if (User.IsInRole("Shelter")
                && !User.IsInRole("Admin"))
            {
                var shelterId =
                    await GetCurrentShelterIdAsync();

                if (!shelterId.HasValue)
                {
                    return Forbid();
                }

                model.ShelterId =
                    shelterId.Value;
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

                TempData["ok"] =
                    "Pet updated.";

                return RedirectToAction(
                    nameof(Index)
                );
            }
            catch (ArgumentException exception)
            {
                ModelState.AddModelError(
                    string.Empty,
                    exception.Message
                );

                await LoadSheltersAsync();

                return View(model);
            }
        }

        [Authorize(Roles = "Admin,Shelter")]
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var pet =
                await _petService.GetAsync(id);

            if (pet == null)
            {
                return NotFound();
            }

            if (!await CanManagePetAsync(
                pet.ShelterId
            ))
            {
                return Forbid();
            }

            return View(pet);
        }

        [Authorize(Roles = "Admin,Shelter")]
        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(
            int id
        )
        {
            var pet =
                await _petService.GetAsync(id);

            if (pet == null)
            {
                return NotFound();
            }

            if (!await CanManagePetAsync(
                pet.ShelterId
            ))
            {
                return Forbid();
            }

            try
            {
                await _petService.DeleteAsync(id);

                TempData["ok"] =
                    "Pet deleted.";
            }
            catch (ArgumentException exception)
            {
                TempData["err"] =
                    exception.Message;
            }

            return RedirectToAction(
                nameof(Index)
            );
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var pet =
                await _petService.GetAsync(id);

            if (pet == null)
            {
                return NotFound();
            }

            return View(pet);
        }

        private async Task LoadSheltersAsync()
        {
            ViewBag.Shelters =
                await _shelterService.GetAllAsync();
        }

        private async Task<int?> GetCurrentShelterIdAsync()
        {
            var user =
                await _userManager.GetUserAsync(User);

            return user?.ShelterId;
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

            var currentShelterId =
                await GetCurrentShelterIdAsync();

            return currentShelterId.HasValue
                && currentShelterId.Value
                == petShelterId;
        }
    }
}