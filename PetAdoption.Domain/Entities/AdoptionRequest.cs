using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using PetAdoption.Domain.Enums;

namespace PetAdoption.Domain.Entities
{
    public class AdoptionRequest
    {
        public int Id { get; set; }
        public int PetId { get; set; }
        public Pet Pet { get; set; } = default!;

        public int AdopterId { get; set; }
        public Adopter Adopter { get; set; } = default!;

        public AdoptionRequestStatus Status { get; set; } = AdoptionRequestStatus.Pending;

        [MaxLength(512)]
        public string? Message { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? DecisionAt { get; set; }
    }
}