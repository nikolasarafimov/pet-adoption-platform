using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
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

        public AdoptionRequestsController(
            IAdoptionService adoptionService,
            IShelterService shelterService,
            UserManager<ApplicationUser> userManager
        )
        {
            _adoptionService = adoptionService;
            _shelterService = shelterService;
            _userManager = userManager;
        }

        [Authorize(Roles = "Adopter")]
        public async Task<IActionResult> Index()
        {
            var adopterId =
                await GetCurrentAdopterIdAsync();

            if (!adopterId.HasValue)
            {
                return Forbid();
            }

            var requests =
                await _adoptionService.GetForAdopterAsync(
                    adopterId.Value
                );

            return View(requests);
        }

        [Authorize(Roles = "Shelter,Admin")]
        public async Task<IActionResult> Manage(
            int? shelterId
        )
        {
            int effectiveShelterId;

            if (User.IsInRole("Admin"))
            {
                if (shelterId.HasValue)
                {
                    var shelter =
                        await _shelterService.GetAsync(
                            shelterId.Value
                        );

                    if (shelter == null)
                    {
                        return NotFound();
                    }

                    effectiveShelterId =
                        shelter.Id;
                }
                else
                {
                    var shelters =
                        await _shelterService.GetAllAsync();

                    var firstShelter =
                        shelters.FirstOrDefault();

                    if (firstShelter == null)
                    {
                        return NotFound();
                    }

                    effectiveShelterId =
                        firstShelter.Id;
                }
            }
            else
            {
                var currentShelterId =
                    await GetCurrentShelterIdAsync();

                if (!currentShelterId.HasValue)
                {
                    return Forbid();
                }

                effectiveShelterId =
                    currentShelterId.Value;
            }

            var requests =
                await _adoptionService.GetForShelterAsync(
                    effectiveShelterId
                );

            ViewBag.ShelterId =
                effectiveShelterId;

            return View(requests);
        }

        [Authorize(Roles = "Adopter")]
        [HttpGet]
        public IActionResult Create(int petId)
        {
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
            var adopterId =
                await GetCurrentAdopterIdAsync();

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

                TempData["ok"] =
                    "Request submitted.";

                return RedirectToAction(
                    nameof(Index)
                );
            }
            catch (
                Exception exception
            ) when (
                exception is ArgumentException
                or InvalidOperationException
            )
            {
                TempData["err"] =
                    exception.Message;

                return View(model: petId);
            }
        }

        [Authorize(Roles = "Shelter,Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Decide(
            int id,
            AdoptionRequestStatus status
        )
        {
            if (status != AdoptionRequestStatus.Approved
                && status != AdoptionRequestStatus.Rejected)
            {
                return BadRequest(
                    "Only approval or rejection is allowed."
                );
            }

            int requestShelterId;

            try
            {
                requestShelterId =
                    await _adoptionService
                        .GetShelterIdForRequestAsync(id);
            }
            catch (ArgumentException)
            {
                return NotFound();
            }

            if (!User.IsInRole("Admin"))
            {
                var currentShelterId =
                    await GetCurrentShelterIdAsync();

                if (!currentShelterId.HasValue
                    || currentShelterId.Value
                    != requestShelterId)
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

                TempData["ok"] =
                    "Request updated.";
            }
            catch (
                Exception exception
            ) when (
                exception is ArgumentException
                or InvalidOperationException
            )
            {
                TempData["err"] =
                    exception.Message;
            }

            return RedirectToAction(
                nameof(Manage),
                new
                {
                    shelterId = requestShelterId
                }
            );
        }

        [Authorize(Roles = "Adopter")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var adopterId =
                await GetCurrentAdopterIdAsync();

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

                TempData["ok"] =
                    "Request cancelled.";
            }
            catch (
                Exception exception
            ) when (
                exception is ArgumentException
                or InvalidOperationException
                or UnauthorizedAccessException
            )
            {
                TempData["err"] =
                    exception.Message;
            }

            return RedirectToAction(
                nameof(Index)
            );
        }

        private async Task<int?> GetCurrentAdopterIdAsync()
        {
            var user =
                await _userManager.GetUserAsync(User);

            return user?.AdopterId;
        }

        private async Task<int?> GetCurrentShelterIdAsync()
        {
            var user =
                await _userManager.GetUserAsync(User);

            return user?.ShelterId;
        }
    }
}