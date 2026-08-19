using ERP.Domain.Common;
using ERP.Domain.Modules.Academy;

namespace ERP.Domain.Modules.Trainee.Entities
{
    public class Trainee : Entity, IAuditable, ISoftDeletable
    {
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string ?PhoneNumber { get; set; } = null!;
        public int Age { get; set; }
        public Gender Gender { get; set; }
        public string? Photo { get; set; }
        public RegistrationType Type { get; set; }  
        public Guid? ParentId { get; set; }
        public Trainee? Parent { get; set; }
        public bool IsDeleted { get; set; }
        public DateTimeOffset? DeletedAtUtc { get; set; }
        public string? DeletedBy { get; set; }
        public DateTimeOffset CreatedAtUtc { get; set; }
        public string? CreatedBy { get; set; }
        public DateTimeOffset? ModifiedAtUtc { get; set; }
        public string? ModifiedBy { get; set; }
        public ICollection<Trainee> ?Trainees { get; set; } 
        public ICollection<ERP.Domain.Modules.Trainee.Entities.AcademyTrainee> Academies { get; set; } = new List<ERP.Domain.Modules.Trainee.Entities.AcademyTrainee>();

    }
}
