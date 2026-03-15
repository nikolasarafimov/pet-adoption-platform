using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PetAdoption.Application.Interfaces;
using PetAdoption.Web.Models.Shelters;

namespace PetAdoption.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class SheltersController : Controller
    {
        private readonly IShelterService _shelters;

        public SheltersController(IShelterService shelters)
        {
            _shelters = shelters;
        }

        // GET: /Shelters
        public async Task<IActionResult> Index()
        {
            var list = await _shelters.GetAllAsync();
            return View(list);
        }

        // GET: /Shelters/Create
        public IActionResult Create() => View(new ShelterFormVm());

        // POST: /Shelters/Create
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ShelterFormVm model)
        {
            if (!ModelState.IsValid) return View(model);

            var id = await _shelters.CreateAsync(model.Name, model.Address, model.City, model.Phone, model.Email);
            TempData["Ok"] = "Shelter created.";
            return RedirectToAction(nameof(Edit), new { id });
        }

        // GET: /Shelters/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var dto = await _shelters.GetAsync(id);
            if (dto == null) return NotFound();

            var vm = new ShelterFormVm
            {
                Id = dto.Id,
                Name = dto.Name,
                Address = dto.Address,
                City = dto.City,
                Phone = dto.Phone,
                Email = dto.Email
            };
            return View(vm);
        }

        // POST: /Shelters/Edit/5
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ShelterFormVm model)
        {
            if (!ModelState.IsValid) return View(model);

            await _shelters.UpdateAsync(model.Id, model.Name, model.Address, model.City, model.Phone, model.Email);
            TempData["Ok"] = "Shelter updated.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Shelters/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var dto = await _shelters.GetAsync(id);
            if (dto == null) return NotFound();
            return View(dto);
        }

        // POST: /Shelters/Delete/5
        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                await _shelters.DeleteAsync(id);
                TempData["Ok"] = "Shelter deleted.";
            }
            catch
            {
                TempData["Err"] = "Shelter cannot be deleted because it has pets. Move or delete the pets first.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
