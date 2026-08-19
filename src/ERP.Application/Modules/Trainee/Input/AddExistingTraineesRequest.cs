namespace ERP.Application.Modules.Trainee.TraineeInput
{
    public class AddExistingTraineesRequest
    {
        public Guid AcademyId { get; set; }

        public List<Guid> TraineeIds { get; set; } = new();
    }
}
