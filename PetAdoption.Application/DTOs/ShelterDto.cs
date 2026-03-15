using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PetAdoption.Application.DTOs
{
    public record ShelterDto
    (
        int Id, 
        string Name, 
        string Address, 
        string City, 
        string? Phone, 
        string? Email
    );
}