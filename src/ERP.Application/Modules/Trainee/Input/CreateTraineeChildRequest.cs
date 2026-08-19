using ERP.Domain.Modules.Academy;
using Microsoft.AspNetCore.Http;

namespace ERP.Application.Modules.Trainee.TraineeInput
{
    public class CreateTraineeChildRequest
    {
        public string FirstName { get; set; } = null!;

        public string LastName { get; set; } = null!;

        public string PhoneNumber { get; set; } = null!;

        public int Age { get; set; }

        public Gender Gender { get; set; }

        public IFormFile? Photo { get; set; }
    }
}
