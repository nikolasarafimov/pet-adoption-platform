using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PetAdoption.Application.Interfaces;
using PetAdoption.Domain.Enums;
using PetAdoption.Web.Models;

namespace PetAdoption.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly IPetService _pets;

        public HomeController(IPetService pets)
        {
            _pets = pets;
        }

        public async Task<IActionResult> Welcome()
        {
            var featured = (await _pets.GetAllAsync())
                .OrderByDescending(p => p.Status == PetStatus.Available)
                .ThenBy(p => p.Name)
                .Take(3)
                .ToList();

            return View(featured);
        }

        public async Task<IActionResult> Index(string? search, Species? species, int page = 1, int pageSize = 9)
        {
            if (page < 1) page = 1;
            if (pageSize <= 0 || pageSize > 30) pageSize = 9;

            var all = await _pets.GetAllAsync(search, species, null);
            var total = all.Count;
            var items = all.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            ViewBag.Search = search;
            ViewBag.Species = species;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.Total = total;
            ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);

            return View(items);
        }

        [HttpGet]
        public IActionResult Breeds(string? q, int page = 1, int pageSize = 12)
            => RedirectToAction("Index", "Breeds", new { q, page, pageSize });

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}