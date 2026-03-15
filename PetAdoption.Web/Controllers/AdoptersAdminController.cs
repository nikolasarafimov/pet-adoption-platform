using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PetAdoption.Application.Interfaces;
using PetAdoption.Web.Models.Adopters;

namespace PetAdoption.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("Admin/Adopters/{action=Index}/{id?}")]
    public class AdoptersAdminController : Controller
    {
        private readonly IAdopterService _adopters;

        public AdoptersAdminController(IAdopterService adopters)
        {
            _adopters = adopters;
        }

        // GET: /Admin/Adopters
        public async Task<IActionResult> Index()
        {
            var list = await _adopters.GetAllAsync();
            return View(list);
        }

        // GET: /Admin/Adopters/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var dto = await _adopters.GetAsync(id);
            if (dto == null) return NotFound();
            return View(dto);
        }

        // GET: /Admin/Adopters/Create
        public IActionResult Create() => View(new AdopterFormVm());

        // POST: /Admin/Adopters/Create
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AdopterFormVm model)
        {
            if (!ModelState.IsValid) return View(model);

            var id = await _adopters.CreateAsync(model.FullName, model.Email, model.Phone, model.Address, model.Notes);
            TempData["Ok"] = "Adopter created.";
            return RedirectToAction(nameof(Edit), new { id });
        }

        // GET: /Admin/Adopters/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var dto = await _adopters.GetAsync(id);
            if (dto == null) return NotFound();

            var vm = new AdopterFormVm
            {
                Id = dto.Id,
                FullName = dto.FullName,
                Email = dto.Email,
                Phone = dto.Phone,
                Address = dto.Address,
                Notes = dto.Notes
            };
            return View(vm);
        }

        // POST: /Admin/Adopters/Edit/5
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(AdopterFormVm model)
        {
            if (!ModelState.IsValid) return View(model);

            await _adopters.UpdateAsync(model.Id, model.FullName, model.Email, model.Phone, model.Address, model.Notes);
            TempData["Ok"] = "Adopter updated.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Admin/Adopters/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var dto = await _adopters.GetAsync(id);
            if (dto == null) return NotFound();
            return View(dto);
        }

        // POST: /Admin/Adopters/Delete/5
        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _adopters.DeleteAsync(id);
            TempData["Ok"] = "Adopter deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}