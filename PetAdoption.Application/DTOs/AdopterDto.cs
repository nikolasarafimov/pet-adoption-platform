using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PetAdoption.Application.DTOs
{
    public record AdopterDto
    (
        int Id, 
        string FullName, 
        string? Email, 
        string? Phone, 
        string? Address, 
        string? Notes
    );
}