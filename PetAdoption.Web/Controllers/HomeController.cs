using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PetAdoption.Application.Interfaces;
using PetAdoption.Domain.Enums;
using PetAdoption.Web.Models;

namespace PetAdoption.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly IPetService _petService;

        public HomeController(IPetService petService)
        {
            _petService = petService;
        }

        public async Task<IActionResult> Welcome()
        {
            var featuredPets =
                (await _petService.GetAllAsync())
                .OrderByDescending(
                    pet => pet.Status == PetStatus.Available
                )
                .ThenBy(pet => pet.Name)
                .Take(3)
                .ToList();

            return View(featuredPets);
        }

        public async Task<IActionResult> Index(
            string? search,
            Species? species,
            int page = 1,
            int pageSize = 9
        )
        {
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, 30);

            var normalizedSearch =
                string.IsNullOrWhiteSpace(search)
                    ? null
                    : search.Trim();

            var pets =
                await _petService.GetAllAsync(
                    normalizedSearch,
                    species,
                    null
                );

            var total = pets.Count;

            var totalPages =
                Math.Max(
                    1,
                    (int)Math.Ceiling(
                        total / (double)pageSize
                    )
                );

            if (page > totalPages)
            {
                page = totalPages;
            }

            var items = pets
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.Search = normalizedSearch;
            ViewBag.Species = species;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.Total = total;
            ViewBag.TotalPages = totalPages;

            return View(items);
        }

        [HttpGet]
        public IActionResult Breeds(
            string? q,
            int page = 1,
            int pageSize = 12
        )
        {
            return RedirectToAction(
                "Index",
                "Breeds",
                new
                {
                    q,
                    page,
                    pageSize
                }
            );
        }

        [ResponseCache(
            Duration = 0,
            Location = ResponseCacheLocation.None,
            NoStore = true
        )]
        public IActionResult Error()
        {
            return View(
                new ErrorViewModel
                {
                    RequestId =
                        Activity.Current?.Id
                        ?? HttpContext.TraceIdentifier
                }
            );
        }
    }
}