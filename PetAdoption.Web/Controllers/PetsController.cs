using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PetAdoption.Application.Interfaces;
using PetAdoption.Infrastructure.Data;
using PetAdoption.Web.Models.Pets;
using System.Threading.Tasks;
using PetAdoption.Domain.Enums;

namespace PetAdoption.Web.Controllers
{
    [Authorize]
    public class PetsController : Controller
    {
        private readonly IPetService _pets;
        private readonly IShelterService _shelters;
        private readonly UserManager<ApplicationUser> _userManager;

        private async Task LoadSheltersAsync()
        {
            var list = await _shelters.GetAllAsync();
            ViewBag.Shelters = list;
        }

        public PetsController(IPetService pets, IShelterService shelters, UserManager<ApplicationUser> userManager)
        {
            _pets = pets;
            _shelters = shelters;
            _userManager = userManager;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index(string? search, Species? species, PetStatus? status)
        {
            bool isAdmin = false, isShelter = false, isAdopter = false;
            int? myShelterId = null;

            if (User.Identity?.IsAuthenticated == true)
            {
                var u = await _userManager.GetUserAsync(User);
                if (u != null)
                {
                    isAdmin = await _userManager.IsInRoleAsync(u, "Admin");
                    isShelter = await _userManager.IsInRoleAsync(u, "Shelter");
                    isAdopter = await _userManager.IsInRoleAsync(u, "Adopter");

                    if (isShelter && !isAdmin)
                        myShelterId = await _shelters.GetMyShelterIdAsync(User);
                }
            }

            var pets = await _pets.GetAllAsync(
                search: search,
                species: species,
                shelterId: (isShelter && !isAdmin) ? myShelterId : null
            );

            if (status.HasValue)
                pets = pets.Where(p => p.Status == status.Value).ToList();

            var vm = new PetListVm
            {
                Pets = pets,
                IsAdmin = isAdmin,
                IsShelter = isShelter,
                IsAdopter = isAdopter,
                MyShelterId = myShelterId,
                Search = search,
                Species = species,
                Status = status
            };

            return View(vm);
        }

        [Authorize(Roles = "Admin,Shelter")]
        public async Task<IActionResult> Create()
        {
            var vm = new PetFormVm();

            if (User.IsInRole("Shelter") && !User.IsInRole("Admin"))
            {
                vm.ShelterId = await _shelters.GetMyShelterIdAsync(User); // preselect
            }

            await LoadSheltersAsync();
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Shelter")]
        public async Task<IActionResult> Create(PetFormVm model)
        {
            if (!ModelState.IsValid)
            {
                await LoadSheltersAsync();
                return View(model);
            }

            if (User.IsInRole("Shelter") && !User.IsInRole("Admin"))
            {
                var myShelterId = await _shelters.GetMyShelterIdAsync(User);
                if (model.ShelterId != myShelterId) return Forbid();
            }

            await _pets.CreateAsync(model.Name, model.Species, model.Breed, model.AgeYears,
                model.Description, model.ImageUrl, model.ShelterId);

            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin,Shelter")]
        public async Task<IActionResult> Edit(int id)
        {
            var dto = await _pets.GetAsync(id);
            if (dto == null) return NotFound();

            if (User.IsInRole("Shelter") && !User.IsInRole("Admin"))
            {
                var myShelterId = await _shelters.GetMyShelterIdAsync(User);
                if (dto.ShelterId != myShelterId) return Forbid();
            }

            await LoadSheltersAsync();
            return View(PetFormVm.FromDto(dto));
        }


        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Shelter")]
        public async Task<IActionResult> Edit(PetFormVm model)
        {
            if (!ModelState.IsValid)
            {
                await LoadSheltersAsync();
                return View(model);
            }

            if (User.IsInRole("Shelter") && !User.IsInRole("Admin"))
            {
                var myShelterId = await _shelters.GetMyShelterIdAsync(User);
                if (model.ShelterId != myShelterId) return Forbid();
            }

            await _pets.UpdateAsync(model.Id, model.Name, model.Species, model.Breed,
                model.AgeYears, model.Description, model.ImageUrl, model.ShelterId, model.Status);

            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin,Shelter")]
        public async Task<IActionResult> Delete(int id)
        {
            var dto = await _pets.GetAsync(id);
            if (dto == null) return NotFound();

            if (User.IsInRole("Shelter") && !User.IsInRole("Admin"))
            {
                var myShelterId = await _shelters.GetMyShelterIdAsync(User);
                if (dto.ShelterId != myShelterId) return Forbid();
            }

            return View(dto);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Shelter")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var dto = await _pets.GetAsync(id);
            if (dto == null) return NotFound();

            if (User.IsInRole("Shelter") && !User.IsInRole("Admin"))
            {
                var myShelterId = await _shelters.GetMyShelterIdAsync(User);
                if (dto.ShelterId != myShelterId) return Forbid();
            }

            await _pets.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }

        [AllowAnonymous]
        public async Task<IActionResult> Details(int id)
        {
            var dto = await _pets.GetAsync(id);
            if (dto == null) return NotFound();
            return View(dto);
        }
    }
}