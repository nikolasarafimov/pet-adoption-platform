using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PetAdoption.Application.Interfaces;
using PetAdoption.Domain.Enums;

namespace PetAdoption.Web.Controllers
{
    [Authorize]
    public class AdoptionRequestsController : Controller
    {
        private readonly IAdoptionService _adoptions;
        private readonly IAdopterService _adopters;
        private readonly IShelterService _shelters;

        public AdoptionRequestsController(IAdoptionService adoptions, IAdopterService adopters, IShelterService shelters)
        {
            _adoptions = adoptions;
            _adopters = adopters;
            _shelters = shelters;
        }

        [Authorize(Roles = "Adopter")]
        public async Task<IActionResult> Index()
        {
            var adopterId = await _adopters.GetMyAdopterIdAsync(User);
            var list = await _adoptions.GetForAdopterAsync(adopterId);
            return View(list);
        }

        [Authorize(Roles = "Shelter,Admin")]
        public async Task<IActionResult> Manage(int? shelterId)
        {
            int sid;

            if (User.IsInRole("Admin"))
            {
                sid = shelterId ?? await _shelters.GetMyShelterIdAsync(User);
            }
            else
            {
                sid = await _shelters.GetMyShelterIdAsync(User);
            }

            var list = await _adoptions.GetForShelterAsync(sid);
            ViewBag.ShelterId = sid;
            return View(list);
        }

        [Authorize(Roles = "Adopter")]
        public IActionResult Create(int petId) => View(model: petId);

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Adopter")]
        public async Task<IActionResult> Create(int petId, string? message)
        {
            var adopterId = await _adopters.GetMyAdopterIdAsync(User);
            try
            {
                await _adoptions.SubmitAsync(petId, adopterId, message);
                TempData["ok"] = "Request submitted!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["err"] = ex.Message;
                return View(model: petId);
            }
        }

        [HttpPost, Authorize(Roles = "Shelter,Admin"), ValidateAntiForgeryToken]
        public async Task<IActionResult> Decide(int id, AdoptionRequestStatus status)
        {
            if (!User.IsInRole("Admin"))
            {
                var myShelterId = await _shelters.GetMyShelterIdAsync(User);
                var requestShelterId = await _adoptions.GetShelterIdForRequestAsync(id);
                if (myShelterId != requestShelterId) return Forbid();
            }

            await _adoptions.SetStatusAsync(id, status);
            TempData["ok"] = "Request updated.";
            return RedirectToAction(nameof(Manage));
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Adopter")]
        public async Task<IActionResult> Cancel(int id)
        {
            var adopterId = await _adopters.GetMyAdopterIdAsync(User);
            try
            {
                await _adoptions.CancelAsync(id, adopterId);
                TempData["ok"] = "Request cancelled.";
            }
            catch (Exception ex)
            {
                TempData["err"] = ex.Message;
            }
            return RedirectToAction(nameof(Index));
        }
    }
}