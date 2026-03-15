using Microsoft.AspNetCore.Mvc;
using PetAdoption.Application.Interfaces;

namespace PetAdoption.Web.Controllers
{
    public class BreedsController : Controller
    {
        private readonly IDogApiService _dog;

        public BreedsController(IDogApiService dog)
        {
            _dog = dog;
        }

        public async Task<IActionResult> Index(string? q, int page = 1, int pageSize = 12)
        {
            var all = await _dog.GetBreedsAsync(q, take: 1000);
            var total = all.Count;
            var items = all.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            ViewBag.Query = q;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.Total = total;
            ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);

            return View(items); 
        }
    }
}