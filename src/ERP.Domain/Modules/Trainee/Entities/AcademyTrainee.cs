namespace ERP.Domain.Modules.Trainee.Entities
{
    public class AcademyTrainee 
    {
        public Guid AcademyId { get; set; }
        public Academy.Entities.Academy Academy { get; set; } = null!;

        public Guid TraineeId { get; set; }
        public Trainee Trainee { get; set; } = null!;

        public TraineeStatus Status { get; set; }
    }
}
