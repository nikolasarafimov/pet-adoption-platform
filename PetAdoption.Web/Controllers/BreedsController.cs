using Microsoft.AspNetCore.Mvc;
using PetAdoption.Application.Interfaces;

namespace PetAdoption.Web.Controllers
{
    public class BreedsController : Controller
    {
        private readonly IDogApiService _dogApiService;

        public BreedsController(
            IDogApiService dogApiService
        )
        {
            _dogApiService = dogApiService;
        }

        public async Task<IActionResult> Index(
            string? q,
            int page = 1,
            int pageSize = 12
        )
        {
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var query = string.IsNullOrWhiteSpace(q)
                ? null
                : q.Trim();

            var breeds =
                await _dogApiService.GetBreedsAsync(
                    query,
                    take: 1000
                );

            var total = breeds.Count;
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

            var items = breeds
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.Query = query;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.Total = total;
            ViewBag.TotalPages = totalPages;

            return View(items);
        }
    }
}