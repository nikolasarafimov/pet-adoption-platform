using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using PetAdoption.Application.DTOs;
using PetAdoption.Application.Interfaces;

namespace PetAdoption.Infrastructure.External
{
    public class DogApiService : IDogApiService
    {
        private static readonly Regex NumberPattern = new(
            @"[0-9]+(?:[.,][0-9]+)?",
            RegexOptions.Compiled | RegexOptions.CultureInvariant,
            TimeSpan.FromSeconds(1)
        );

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
            var breeds = await FetchBreedsAsync();

            var searchTerm = nameLike?.Trim();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                breeds = breeds
                    .Where(breed =>
                        breed.Name.Contains(
                            searchTerm,
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

            var searchTerm = name.Trim();

            var breeds = await FetchBreedsAsync();

            var breed = breeds.FirstOrDefault(
                candidate => string.Equals(
                    candidate.Name.Trim(),
                    searchTerm,
                    StringComparison.OrdinalIgnoreCase
                )
            );

            return breed == null ? null : Map(breed);
        }

        private async Task<List<BreedRaw>> FetchBreedsAsync()
        {
            var breeds = await _httpClient
                .GetFromJsonAsync<List<BreedRaw>>("v1/breeds");

            if (breeds == null)
            {
                return new List<BreedRaw>();
            }

            return breeds
                .Where(breed =>
                    breed != null &&
                    !string.IsNullOrWhiteSpace(breed.Name)
                )
                .ToList();
        }

        private static BreedDto Map(BreedRaw breed)
        {
            var (minimumLifeSpan, maximumLifeSpan) =
                ParseLifeSpan(breed.LifeSpan);

            var averageWeight =
                ParseAverageWeight(breed.Weight?.Metric);

            var temperaments =
                (breed.Temperament ?? string.Empty)
                .Split(
                    ',',
                    StringSplitOptions.RemoveEmptyEntries |
                    StringSplitOptions.TrimEntries
                )
                .Where(value =>
                    !string.IsNullOrWhiteSpace(value)
                )
                .ToArray();

            return new BreedDto(
                breed.Name.Trim(),
                temperaments,
                minimumLifeSpan,
                maximumLifeSpan,
                averageWeight,
                NormalizeImageUrl(breed.Image?.Url)
            );
        }

        private static (int? Minimum, int? Maximum) ParseLifeSpan(
            string? lifeSpan
        )
        {
            if (string.IsNullOrWhiteSpace(lifeSpan))
            {
                return (null, null);
            }

            var matches = NumberPattern.Matches(lifeSpan);

            if (matches.Count == 0)
            {
                return (null, null);
            }

            int? minimum = null;
            int? maximum = null;

            if (int.TryParse(
                matches[0].Value,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var minimumValue
            ))
            {
                minimum = minimumValue;
            }

            if (matches.Count > 1 &&
                int.TryParse(
                    matches[1].Value,
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out var maximumValue
                ))
            {
                maximum = maximumValue;
            }

            return (minimum, maximum);
        }

        private static double? ParseAverageWeight(
            string? metricWeight
        )
        {
            if (string.IsNullOrWhiteSpace(metricWeight))
            {
                return null;
            }

            var values = new List<double>(2);

            foreach (Match match in NumberPattern.Matches(metricWeight))
            {
                var number = match.Value.Replace(',', '.');

                if (!double.TryParse(
                    number,
                    NumberStyles.AllowDecimalPoint,
                    CultureInfo.InvariantCulture,
                    out var value
                ))
                {
                    continue;
                }

                if (!double.IsFinite(value) || value <= 0)
                {
                    continue;
                }

                values.Add(value);

                if (values.Count == 2)
                {
                    break;
                }
            }

            return values.Count switch
            {
                0 => null,
                1 => values[0],
                _ => (values[0] + values[1]) / 2.0
            };
        }

        private static string? NormalizeImageUrl(
            string? imageUrl
        )
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                return null;
            }

            var normalized = imageUrl.Trim();

            if (!Uri.TryCreate(
                normalized,
                UriKind.Absolute,
                out var uri
            ))
            {
                return null;
            }

            if (uri.Scheme != Uri.UriSchemeHttp &&
                uri.Scheme != Uri.UriSchemeHttps)
            {
                return null;
            }

            return normalized;
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