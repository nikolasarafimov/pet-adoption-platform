using System.Net.Http.Json;
using System.Text.Json.Serialization;
using PetAdoption.Application.DTOs;
using PetAdoption.Application.Interfaces;

namespace PetAdoption.Infrastructure.External
{
    public class DogApiService : IDogApiService
    {
        private readonly HttpClient _httpClient;

        public DogApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<IReadOnlyList<BreedDto>> GetBreedsAsync(
            string? nameLike = null,
            int take = 20
        )
        {
            var breeds =
                await _httpClient.GetFromJsonAsync<List<BreedRaw>>(
                    "v1/breeds"
                )
                ?? new List<BreedRaw>();

            if (!string.IsNullOrWhiteSpace(nameLike))
            {
                breeds = breeds
                    .Where(breed =>
                        breed.Name.Contains(
                            nameLike.Trim(),
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                    .ToList();
            }

            var limit = Math.Clamp(take, 1, 1000);

            return breeds
                .Take(limit)
                .Select(Map)
                .ToList();
        }

        public async Task<BreedDto?> GetBreedByExactNameAsync(
            string name
        )
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            var breeds = await GetBreedsAsync(
                nameLike: name.Trim(),
                take: 1000
            );

            return breeds.FirstOrDefault(breed =>
                string.Equals(
                    breed.Name,
                    name.Trim(),
                    StringComparison.OrdinalIgnoreCase
                )
            );
        }

        private static BreedDto Map(BreedRaw breed)
        {
            int? minimumLifeSpan = null;
            int? maximumLifeSpan = null;

            var lifeSpanParts =
                (breed.LifeSpan ?? string.Empty)
                .Split(
                    ' ',
                    StringSplitOptions.RemoveEmptyEntries
                );

            if (lifeSpanParts.Length >= 1
                && int.TryParse(
                    lifeSpanParts[0],
                    out var minimum
                ))
            {
                minimumLifeSpan = minimum;
            }

            if (lifeSpanParts.Length >= 3
                && int.TryParse(
                    lifeSpanParts[2],
                    out var maximum
                ))
            {
                maximumLifeSpan = maximum;
            }

            double? averageWeight = null;

            var weights =
                (breed.Weight?.Metric ?? string.Empty)
                .Split(
                    '-',
                    StringSplitOptions.RemoveEmptyEntries
                )
                .Select(value =>
                    double.TryParse(
                        value.Trim(),
                        out var parsed
                    )
                        ? parsed
                        : (double?)null
                )
                .Where(value => value.HasValue)
                .Select(value => value!.Value)
                .ToList();

            if (weights.Count == 1)
            {
                averageWeight = weights[0];
            }
            else if (weights.Count >= 2)
            {
                averageWeight =
                    (weights[0] + weights[1]) / 2.0;
            }

            var temperaments =
                (breed.Temperament ?? string.Empty)
                .Split(
                    ',',
                    StringSplitOptions.RemoveEmptyEntries
                )
                .Select(value => value.Trim())
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .ToArray();

            return new BreedDto(
                breed.Name,
                temperaments,
                minimumLifeSpan,
                maximumLifeSpan,
                averageWeight,
                breed.Image?.Url
            );
        }

        private sealed class BreedRaw
        {
            [JsonPropertyName("name")]
            public string Name { get; set; } = string.Empty;

            [JsonPropertyName("temperament")]
            public string? Temperament { get; set; }

            [JsonPropertyName("life_span")]
            public string? LifeSpan { get; set; }

            [JsonPropertyName("weight")]
            public WeightRaw? Weight { get; set; }

            [JsonPropertyName("image")]
            public ImageRaw? Image { get; set; }
        }

        private sealed class WeightRaw
        {
            [JsonPropertyName("metric")]
            public string? Metric { get; set; }
        }

        private sealed class ImageRaw
        {
            [JsonPropertyName("url")]
            public string? Url { get; set; }
        }
    }
}