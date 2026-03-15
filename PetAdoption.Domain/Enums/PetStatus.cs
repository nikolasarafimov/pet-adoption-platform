using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PetAdoption.Domain.Enums
{
    public enum PetStatus
    {
        Available = 0,
        Reserved = 1,
        Adopted = 2,
        Unavailable = 3
    }
}