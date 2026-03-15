using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PetAdoption.Application.Interfaces;

namespace PetAdoption.Web.Controllers
{
    [Authorize]
    public class AdoptersController : Controller
    {
        private readonly IAdopterService _adopters;

        public AdoptersController(IAdopterService adopters)
        {
            _adopters = adopters;
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var list = await _adopters.GetAllAsync();
            return View(list);
        }

        [Authorize(Roles = "Admin,Adopter")]
        public async Task<IActionResult> Details(int id)
        {
            if (User.IsInRole("Adopter"))
            {
                var myId = await _adopters.GetMyAdopterIdAsync(User);
                if (myId != id) return Forbid();
            }

            var a = await _adopters.GetAsync(id);
            if (a == null) return NotFound();
            return View(a);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Create() => View();

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(string fullName, string? email, string? phone, string? address, string? notes)
        {
            if (!ModelState.IsValid) return View();

            await _adopters.CreateAsync(fullName, email, phone, address, notes);
            TempData["ok"] = "Adopter created.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin,Adopter")]
        public async Task<IActionResult> Edit(int id)
        {
            if (User.IsInRole("Adopter"))
            {
                var myId = await _adopters.GetMyAdopterIdAsync(User);
                if (myId != id) return Forbid();
            }

            var a = await _adopters.GetAsync(id);
            if (a == null) return NotFound();
            return View(a);
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin,Adopter")]
        public async Task<IActionResult> Edit(int id, string fullName, string? email, string? phone, string? address, string? notes)
        {
            if (User.IsInRole("Adopter"))
            {
                var myId = await _adopters.GetMyAdopterIdAsync(User);
                if (myId != id) return Forbid();
            }

            try
            {
                await _adopters.UpdateAsync(id, fullName, email, phone, address, notes);
                TempData["ok"] = "Adopter updated.";
                return RedirectToAction(User.IsInRole("Admin") ? nameof(Index) : nameof(Details), new { id });
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError("", ex.Message);
                var a = await _adopters.GetAsync(id);
                return View(a);
            }
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var a = await _adopters.GetAsync(id);
            if (a == null) return NotFound();
            return View(a);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken, Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                await _adopters.DeleteAsync(id);
                TempData["ok"] = "Adopter deleted.";
            }
            catch (ArgumentException ex)
            {
                TempData["err"] = ex.Message;
            }
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Adopter")]
        public async Task<IActionResult> Me()
        {
            var myId = await _adopters.GetMyAdopterIdAsync(User);
            return RedirectToAction(nameof(Details), new { id = myId });
        }

        [Authorize(Roles = "Adopter")]
        public async Task<IActionResult> EditMe()
        {
            var myId = await _adopters.GetMyAdopterIdAsync(User);
            return RedirectToAction(nameof(Edit), new { id = myId });
        }
    }
}