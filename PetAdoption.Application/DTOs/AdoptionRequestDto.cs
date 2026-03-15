using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PetAdoption.Domain.Enums;

namespace PetAdoption.Application.DTOs
{
    public record AdoptionRequestDto
    (
        int Id,
        int PetId, 
        string PetName, 
        int AdopterId, 
        string AdopterName,
        AdoptionRequestStatus Status, 
        string? Message, 
        DateTime CreatedAt, 
        DateTime? DecisionAt
    );
}