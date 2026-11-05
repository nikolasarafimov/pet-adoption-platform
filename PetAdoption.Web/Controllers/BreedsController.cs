using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using PetAdoption.Application.DTOs;
using PetAdoption.Application.Interfaces;

namespace PetAdoption.Web.Controllers
{
    public class BreedsController : Controller
    {
        private readonly IDogApiService _dogApiService;
        private readonly ILogger<BreedsController> _logger;

        public BreedsController(
            IDogApiService dogApiService,
            ILogger<BreedsController> logger
        )
        {
            _dogApiService = dogApiService;
            _logger = logger;
        }

        [HttpGet]
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

            IReadOnlyList<BreedDto> breeds;

            try
            {
                breeds = await _dogApiService.GetBreedsAsync(
                    query,
                    take: 1000
                );
            }
            catch (HttpRequestException exception)
            {
                _logger.LogWarning(
                    exception,
                    "The Dog API request failed."
                );

                return ServiceUnavailable(query, pageSize);
            }
            catch (TaskCanceledException exception)
                when (!HttpContext.RequestAborted.IsCancellationRequested)
            {
                _logger.LogWarning(
                    exception,
                    "The Dog API request timed out."
                );

                return ServiceUnavailable(query, pageSize);
            }
            catch (JsonException exception)
            {
                _logger.LogError(
                    exception,
                    "The Dog API returned invalid JSON."
                );

                return ServiceUnavailable(query, pageSize);
            }
            catch (NotSupportedException exception)
            {
                _logger.LogError(
                    exception,
                    "The Dog API returned an unsupported response."
                );

                return ServiceUnavailable(query, pageSize);
            }

            var total = breeds.Count;

            var totalPages = Math.Max(
                1,
                (int)Math.Ceiling(total / (double)pageSize)
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
            ViewBag.ApiUnavailable = false;

            return View(items);
        }

        private IActionResult ServiceUnavailable(
            string? query,
            int pageSize
        )
        {
            ViewBag.Query = query;
            ViewBag.Page = 1;
            ViewBag.PageSize = pageSize;
            ViewBag.Total = 0;
            ViewBag.TotalPages = 1;
            ViewBag.ApiUnavailable = true;

            TempData["Err"] =
                "Dog breed information is temporarily unavailable. Please try again later.";

            Response.StatusCode =
                StatusCodes.Status503ServiceUnavailable;

            return View(
                "Index",
                Array.Empty<BreedDto>()
            );
        }
    }
}