using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PetAdoption.Application.Interfaces;
using PetAdoption.Domain.Enums;
using PetAdoption.Infrastructure.Data;

namespace PetAdoption.Web.Controllers
{
    [Authorize]
    public class AdoptionRequestsController : Controller
    {
        private readonly IAdoptionService _adoptionService;
        private readonly IShelterService _shelterService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<AdoptionRequestsController> _logger;

        public AdoptionRequestsController(
            IAdoptionService adoptionService,
            IShelterService shelterService,
            UserManager<ApplicationUser> userManager,
            ILogger<AdoptionRequestsController> logger
        )
        {
            _adoptionService = adoptionService;
            _shelterService = shelterService;
            _userManager = userManager;
            _logger = logger;
        }

        [Authorize(Roles = "Adopter")]
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var adopterId = await GetCurrentAdopterIdAsync();

            if (!adopterId.HasValue)
            {
                return Forbid();
            }

            var requests = await _adoptionService.GetForAdopterAsync(
                adopterId.Value
            );

            return View(requests);
        }

        [Authorize(Roles = "Shelter,Admin")]
        [HttpGet]
        public async Task<IActionResult> Manage(int? shelterId)
        {
            int effectiveShelterId;

            if (User.IsInRole("Admin"))
            {
                if (shelterId.HasValue)
                {
                    var shelter = await _shelterService.GetAsync(
                        shelterId.Value
                    );

                    if (shelter == null)
                    {
                        return NotFound();
                    }

                    effectiveShelterId = shelter.Id;
                }
                else
                {
                    var shelters = await _shelterService.GetAllAsync();

                    var firstShelter = shelters.FirstOrDefault();

                    if (firstShelter == null)
                    {
                        return NotFound();
                    }

                    effectiveShelterId = firstShelter.Id;
                }
            }
            else
            {
                var currentShelterId = await GetCurrentShelterIdAsync();

                if (!currentShelterId.HasValue)
                {
                    return Forbid();
                }

                effectiveShelterId = currentShelterId.Value;
            }

            var requests = await _adoptionService.GetForShelterAsync(
                effectiveShelterId
            );

            ViewBag.ShelterId = effectiveShelterId;

            return View(requests);
        }

        [Authorize(Roles = "Adopter")]
        [HttpGet]
        public IActionResult Create(int petId)
        {
            if (petId <= 0)
            {
                return NotFound();
            }

            return View(model: petId);
        }

        [Authorize(Roles = "Adopter")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            int petId,
            string? message
        )
        {
            if (petId <= 0)
            {
                return NotFound();
            }

            var adopterId = await GetCurrentAdopterIdAsync();

            if (!adopterId.HasValue)
            {
                return Forbid();
            }

            try
            {
                await _adoptionService.SubmitAsync(
                    petId,
                    adopterId.Value,
                    message
                );

                TempData["Ok"] = "Request submitted successfully.";

                return RedirectToAction(nameof(Index));
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
                when (IsUniqueConstraintViolation(exception))
            {
                _logger.LogWarning(
                    exception,
                    "Duplicate adoption request prevented for pet {PetId}.",
                    petId
                );

                TempData["Err"] =
                    "This pet already has a pending adoption request.";
            }
            catch (DbUpdateException exception)
            {
                _logger.LogError(
                    exception,
                    "Database error while submitting an adoption request for pet {PetId}.",
                    petId
                );

                TempData["Err"] =
                    "Unable to submit the request. Please try again later.";
            }

            return View(model: petId);
        }

        [Authorize(Roles = "Shelter,Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Decide(
            int id,
            AdoptionRequestStatus status
        )
        {
            if (id <= 0)
            {
                return NotFound();
            }

            if (status != AdoptionRequestStatus.Approved &&
                status != AdoptionRequestStatus.Rejected)
            {
                return BadRequest(
                    "Only approval or rejection is allowed."
                );
            }

            int requestShelterId;

            try
            {
                requestShelterId = await _adoptionService
                    .GetShelterIdForRequestAsync(id);
            }
            catch (ArgumentException)
            {
                return NotFound();
            }

            if (!User.IsInRole("Admin"))
            {
                var currentShelterId = await GetCurrentShelterIdAsync();

                if (!currentShelterId.HasValue ||
                    currentShelterId.Value != requestShelterId)
                {
                    return Forbid();
                }
            }

            try
            {
                await _adoptionService.SetStatusAsync(
                    id,
                    status
                );

                TempData["Ok"] = "Request updated successfully.";
            }
            catch (ArgumentException exception)
            {
                TempData["Err"] = exception.Message;
            }
            catch (InvalidOperationException exception)
            {
                TempData["Err"] = exception.Message;
            }
            catch (DbUpdateConcurrencyException exception)
            {
                _logger.LogWarning(
                    exception,
                    "Concurrency conflict while deciding adoption request {RequestId}.",
                    id
                );

                TempData["Err"] =
                    "The request was changed by another operation. Please refresh and try again.";
            }
            catch (DbUpdateException exception)
            {
                _logger.LogError(
                    exception,
                    "Database error while deciding adoption request {RequestId}.",
                    id
                );

                TempData["Err"] =
                    "Unable to update the request. Please try again later.";
            }

            return RedirectToAction(
                nameof(Manage),
                new { shelterId = requestShelterId }
            );
        }

        [Authorize(Roles = "Adopter")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            if (id <= 0)
            {
                return NotFound();
            }

            var adopterId = await GetCurrentAdopterIdAsync();

            if (!adopterId.HasValue)
            {
                return Forbid();
            }

            try
            {
                await _adoptionService.CancelAsync(
                    id,
                    adopterId.Value
                );

                TempData["Ok"] = "Request cancelled successfully.";
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (ArgumentException)
            {
                return NotFound();
            }
            catch (InvalidOperationException exception)
            {
                TempData["Err"] = exception.Message;
            }
            catch (DbUpdateConcurrencyException exception)
            {
                _logger.LogWarning(
                    exception,
                    "Concurrency conflict while cancelling adoption request {RequestId}.",
                    id
                );

                TempData["Err"] =
                    "The request was changed by another operation. Please refresh and try again.";
            }
            catch (DbUpdateException exception)
            {
                _logger.LogError(
                    exception,
                    "Database error while cancelling adoption request {RequestId}.",
                    id
                );

                TempData["Err"] =
                    "Unable to cancel the request. Please try again later.";
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task<int?> GetCurrentAdopterIdAsync()
        {
            var user = await _userManager.GetUserAsync(User);

            return user?.AdopterId;
        }

        private async Task<int?> GetCurrentShelterIdAsync()
        {
            var user = await _userManager.GetUserAsync(User);

            return user?.ShelterId;
        }

        private static bool IsUniqueConstraintViolation(
            DbUpdateException exception
        )
        {
            return exception.InnerException is SqliteException sqliteException
                && sqliteException.SqliteExtendedErrorCode == 2067;
        }
    }
}