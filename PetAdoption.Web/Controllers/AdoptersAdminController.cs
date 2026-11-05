using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetAdoption.Application.Interfaces;
using PetAdoption.Infrastructure.Data;
using PetAdoption.Web.Models.Adopters;

namespace PetAdoption.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("Admin/Adopters/{action=Index}/{id?}")]
    public class AdoptersAdminController : Controller
    {
        private readonly IAdopterService _adopters;
        private readonly AppDbContext _db;
        private readonly ILogger<AdoptersAdminController> _logger;

        public AdoptersAdminController(
            IAdopterService adopters,
            AppDbContext db,
            ILogger<AdoptersAdminController> logger
        )
        {
            _adopters = adopters;
            _db = db;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var adopters = await _adopters.GetAllAsync();

            return View(adopters);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var adopter = await _adopters.GetAsync(id);

            if (adopter == null)
            {
                return NotFound();
            }

            return View(adopter);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new AdopterFormVm());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AdopterFormVm model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var id = await _adopters.CreateAsync(
                    model.FullName,
                    model.Email,
                    model.Phone,
                    model.Address,
                    model.Notes
                );

                TempData["Ok"] = "Adopter created.";

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
                    "Database error while creating an adopter."
                );

                ModelState.AddModelError(
                    string.Empty,
                    "Unable to create the adopter. Please try again."
                );
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var adopter = await _adopters.GetAsync(id);

            if (adopter == null)
            {
                return NotFound();
            }

            var model = new AdopterFormVm
            {
                Id = adopter.Id,
                FullName = adopter.FullName,
                Email = adopter.Email,
                Phone = adopter.Phone,
                Address = adopter.Address,
                Notes = adopter.Notes
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(AdopterFormVm model)
        {
            if (model.Id <= 0)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                await _adopters.UpdateAsync(
                    model.Id,
                    model.FullName,
                    model.Email,
                    model.Phone,
                    model.Address,
                    model.Notes
                );

                TempData["Ok"] = "Adopter updated.";

                return RedirectToAction(nameof(Index));
            }
            catch (ArgumentException exception)
                when (exception.ParamName == "id")
            {
                return NotFound();
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
                    "Database error while updating adopter {AdopterId}.",
                    model.Id
                );

                ModelState.AddModelError(
                    string.Empty,
                    "Unable to update the adopter. Please try again."
                );
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var adopter = await _adopters.GetAsync(id);

            if (adopter == null)
            {
                return NotFound();
            }

            return View(adopter);
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

            var adopter = await _db.Adopters
                .AsNoTracking()
                .Where(profile => profile.Id == id)
                .Select(profile => new
                {
                    profile.Id,
                    profile.UserId
                })
                .FirstOrDefaultAsync();

            if (adopter == null)
            {
                return NotFound();
            }

            var linkedUserId = adopter.UserId;

            var hasLinkedAccount = await _db.Users.AnyAsync(
                user =>
                    user.AdopterId == id ||
                    (linkedUserId != null &&
                     user.Id == linkedUserId)
            );

            if (hasLinkedAccount)
            {
                TempData["Err"] =
                    "Cannot delete an adopter linked to a user account.";

                return RedirectToAction(nameof(Index));
            }

            try
            {
                await _adopters.DeleteAsync(id);

                TempData["Ok"] = "Adopter deleted.";
            }
            catch (ArgumentException exception)
                when (exception.ParamName == "id")
            {
                return NotFound();
            }
            catch (InvalidOperationException exception)
            {
                _logger.LogWarning(
                    exception,
                    "Deletion blocked for adopter {AdopterId}.",
                    id
                );

                TempData["Err"] =
                    "Cannot delete an adopter with existing adoption requests.";
            }
            catch (DbUpdateException exception)
            {
                _logger.LogError(
                    exception,
                    "Database error while deleting adopter {AdopterId}.",
                    id
                );

                TempData["Err"] =
                    "Unable to delete the adopter because of related records.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}