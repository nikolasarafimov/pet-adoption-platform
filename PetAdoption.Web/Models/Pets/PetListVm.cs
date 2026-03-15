using System.Collections.Generic;
using PetAdoption.Application.DTOs;
using PetAdoption.Domain.Enums;

namespace PetAdoption.Web.Models.Pets
{
    public sealed class PetListVm
    {
        public IReadOnlyList<PetDto> Pets { get; set; } = new List<PetDto>();

        public bool IsAdmin { get; set; }
        public bool IsShelter { get; set; }
        public bool IsAdopter { get; set; }
        public int? MyShelterId { get; set; }

        public string? Search { get; set; }
        public Species? Species { get; set; }
        public PetStatus? Status { get; set; }
    }
}