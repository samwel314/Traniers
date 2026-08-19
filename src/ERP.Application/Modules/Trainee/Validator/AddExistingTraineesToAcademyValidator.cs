using ERP.Application.Modules.Trainee.TraineeInput;
using FluentValidation;

namespace ERP.Application.Modules.Trainee.Validator
{
    public class AddExistingTraineesToAcademyValidator
    : AbstractValidator<AddExistingTraineesRequest>
    {
        public AddExistingTraineesToAcademyValidator()
        {
            RuleFor(x => x.AcademyId)
                .NotEmpty()
                .WithMessage("Academy is required.");

            RuleFor(x => x.TraineeIds)
                .NotEmpty()
                .WithMessage("At least one trainee is required.");

            RuleForEach(x => x.TraineeIds)
                .NotEmpty()
                .WithMessage("Trainee ID is required.");
        }
    }
}
