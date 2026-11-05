using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetAdoption.Application.Interfaces;
using PetAdoption.Infrastructure.Data;
using PetAdoption.Web.Models.Shelters;

namespace PetAdoption.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class SheltersController : Controller
    {
        private readonly IShelterService _shelters;
        private readonly AppDbContext _db;
        private readonly ILogger<SheltersController> _logger;

        public SheltersController(
            IShelterService shelters,
            AppDbContext db,
            ILogger<SheltersController> logger
        )
        {
            _shelters = shelters;
            _db = db;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var shelters = await _shelters.GetAllAsync();

            return View(
                shelters.OrderBy(shelter => shelter.Name).ToList()
            );
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new ShelterFormVm());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ShelterFormVm model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var id = await _shelters.CreateAsync(
                    model.Name,
                    model.Address,
                    model.City,
                    model.Phone,
                    model.Email
                );

                TempData["Ok"] = "Shelter created successfully.";

                return RedirectToAction(nameof(Edit), new { id });
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
                    "Database error while creating a shelter."
                );

                ModelState.AddModelError(
                    string.Empty,
                    "Unable to create the shelter. Please try again."
                );
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            if (id <= 0)
            {
                return NotFound();
            }

            var shelter = await _shelters.GetAsync(id);

            if (shelter == null)
            {
                return NotFound();
            }

            var model = new ShelterFormVm
            {
                Id = shelter.Id,
                Name = shelter.Name,
                Address = shelter.Address,
                City = shelter.City,
                Phone = shelter.Phone,
                Email = shelter.Email
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ShelterFormVm model)
        {
            if (model.Id <= 0)
            {
                return NotFound();
            }

            var existingShelter = await _shelters.GetAsync(model.Id);

            if (existingShelter == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                await _shelters.UpdateAsync(
                    model.Id,
                    model.Name,
                    model.Address,
                    model.City,
                    model.Phone,
                    model.Email
                );

                TempData["Ok"] = "Shelter updated successfully.";

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
                    "Database error while updating shelter {ShelterId}.",
                    model.Id
                );

                ModelState.AddModelError(
                    string.Empty,
                    "Unable to update the shelter. Please try again."
                );
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            if (id <= 0)
            {
                return NotFound();
            }

            var shelter = await _shelters.GetAsync(id);

            if (shelter == null)
            {
                return NotFound();
            }

            return View(shelter);
        }

        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (id <= 0)
            {
                return NotFound();
            }

            var shelter = await _shelters.GetAsync(id);

            if (shelter == null)
            {
                return NotFound();
            }

            var hasLinkedAccount = await _db.Users
                .AnyAsync(user => user.ShelterId == id);

            if (hasLinkedAccount)
            {
                TempData["Err"] =
                    "Cannot delete a shelter linked to a user account.";

                return RedirectToAction(nameof(Index));
            }

            var hasPets = await _db.Pets
                .AnyAsync(pet => pet.ShelterId == id);

            if (hasPets)
            {
                TempData["Err"] =
                    "Cannot delete a shelter that has pets. Move or delete the pets first.";

                return RedirectToAction(nameof(Index));
            }

            try
            {
                await _shelters.DeleteAsync(id);

                TempData["Ok"] = "Shelter deleted successfully.";
            }
            catch (ArgumentException)
            {
                return NotFound();
            }
            catch (InvalidOperationException exception)
            {
                _logger.LogWarning(
                    exception,
                    "Deletion blocked for shelter {ShelterId}.",
                    id
                );

                TempData["Err"] =
                    "The shelter cannot be deleted in its current state.";
            }
            catch (DbUpdateException exception)
            {
                _logger.LogError(
                    exception,
                    "Database error while deleting shelter {ShelterId}.",
                    id
                );

                TempData["Err"] =
                    "Unable to delete the shelter because of related records or a database error.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}