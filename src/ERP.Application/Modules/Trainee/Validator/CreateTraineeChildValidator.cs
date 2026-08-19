using ERP.Application.Modules.Trainee.TraineeInput;
using FluentValidation;

namespace ERP.Application.Modules.Trainee.Validator
{
    public class CreateTraineeChildValidator
        : AbstractValidator<CreateTraineeChildRequest>
    {
        public CreateTraineeChildValidator()
        {
            RuleFor(x => x.FirstName)
                .NotEmpty()
                .WithMessage("Trainee first name is required.")
                .MaximumLength(100)
                .WithMessage(
                    "Trainee first name cannot exceed 100 characters.");

            RuleFor(x => x.LastName)
                .NotEmpty()
                .WithMessage("Trainee last name is required.")
                .MaximumLength(100)
                .WithMessage(
                    "Trainee last name cannot exceed 100 characters.");

            RuleFor(x => x.PhoneNumber)
                .NotEmpty()
                .WithMessage("Trainee phone number is required.")
                .Matches(@"^01[0125][0-9]{8}$")
                .WithMessage(
                    "Trainee phone number is invalid. Expected format: 01xxxxxxxxx.");

            RuleFor(x => x.Age)
                .InclusiveBetween(1, 100)
                .WithMessage(
                    "Trainee age must be between {0} and {1} years.");

            RuleFor(x => x.Gender)
                .IsInEnum()
                .WithMessage("Trainee gender is invalid.");
        }
    }
}
