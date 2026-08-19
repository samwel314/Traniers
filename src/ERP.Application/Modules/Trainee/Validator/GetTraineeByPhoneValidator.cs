using ERP.Application.Modules.Trainee.TraineeOutput;
using FluentValidation;

namespace ERP.Application.Modules.Trainee.Validator
{
    public class GetTraineeByPhoneValidator
    : AbstractValidator<GetTraineeByPhoneRequest>
    {
        public GetTraineeByPhoneValidator()
        {
            RuleFor(x => x.PhoneNumber)
                .NotEmpty()
                .WithMessage("Trainee phone number is required.")
                .Matches(@"^01[0125][0-9]{8}$")
                .WithMessage(
                    "Trainee phone number is invalid. Expected format: 01xxxxxxxxx.");
        }
    }
}
