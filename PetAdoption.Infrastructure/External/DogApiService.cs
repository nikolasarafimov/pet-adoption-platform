using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using PetAdoption.Application.DTOs;
using PetAdoption.Application.Interfaces;

namespace PetAdoption.Infrastructure.External
{
    public class DogApiService : IDogApiService
    {
        private readonly HttpClient _http;

        public DogApiService(HttpClient http)
        {
            _http = http;
        }

        private sealed class BreedRaw
        {
            public string name { get; set; } = "";
            public string temperament { get; set; } = "";
            public string life_span { get; set; } = "";
            public WeightRaw weight { get; set; } = new();
            public ImageRaw? image { get; set; }
        }
        private sealed class WeightRaw { public string metric { get; set; } = ""; }
        private sealed class ImageRaw { public string url { get; set; } = ""; }

        public async Task<IReadOnlyList<BreedDto>> GetBreedsAsync(string? nameLike = null, int take = 20)
        {
            var q = "v1/breeds";
            var list = await _http.GetFromJsonAsync<List<BreedRaw>>(q) ?? new();
            if (!string.IsNullOrWhiteSpace(nameLike))
                list = list.Where(b => b.name.Contains(nameLike, StringComparison.OrdinalIgnoreCase)).ToList();

            return list.Take(take).Select(Map).ToList();
        }

        public async Task<BreedDto?> GetBreedByExactNameAsync(string name)
        {
            var list = await GetBreedsAsync(nameLike: name, take: 1000);
            return list.FirstOrDefault(b => string.Equals(b.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        private static BreedDto Map(BreedRaw b)
        {
            int? min = null, max = null;
            var parts = b.life_span?.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
            if (parts.Length >= 1 && int.TryParse(parts[0], out var mi)) min = mi;
            if (parts.Length >= 3 && int.TryParse(parts[2], out var ma)) max = ma;

            double? avg = null;
            var weights = (b.weight?.metric ?? "")
                .Split('-', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => double.TryParse(s.Trim(), out var d) ? d : (double?)null)
                .Where(d => d.HasValue)
                .Select(d => d!.Value)
                .ToList();
            if (weights.Count == 1) avg = weights[0];
            if (weights.Count == 2) avg = (weights[0] + weights[1]) / 2.0;

            var temps = (b.temperament ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim())
                .ToArray();

            return new BreedDto(
                b.name,
                temps,
                min,
                max,
                avg,
                b.image?.url
            );
        }
    }
}