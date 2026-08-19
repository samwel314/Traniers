using ERP.Domain.Modules.Academy;
using ERP.Domain.Modules.Trainee.Entities;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;

namespace ERP.Application.Modules.Trainee.TraineeInput
{
    public class CreateTraineeRequest
    {
        public Guid AcademyId { get; set; }

        public string FirstName { get; set; } = null!;

        public string LastName { get; set; } = null!;

        public string PhoneNumber { get; set; } = null!;

        public int Age { get; set; }

        public Gender Gender { get; set; }

        public IFormFile? Photo { get; set; }

        public RegistrationType Type { get; set; }
       

        public List<CreateTraineeChildRequest> Children { get; set; }
            = new();
    }
}
