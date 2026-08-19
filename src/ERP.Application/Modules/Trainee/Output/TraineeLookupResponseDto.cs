using ERP.Domain.Modules.Academy;
using ERP.Domain.Modules.Trainee.Entities;

namespace ERP.Application.Modules.Trainee.TraineeOutput
{
    public class TraineeLookupResponseDto
    {
        public Guid Id { get; set; }

        public string FirstName { get; set; } = null!;

        public string LastName { get; set; } = null!;

        public string PhoneNumber { get; set; } = null!;

        public int Age { get; set; }

        public Gender Gender { get; set; }

        public string? Photo { get; set; }

        public RegistrationType Type { get; set; }

        public List<TraineeChildDto> Children { get; set; } = new();
    }
}
