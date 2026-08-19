using ERP.Application.Modules.Trainee.TraineeInput;
using ERP.Domain.Modules.Trainee.Entities;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace ERP.Application.Modules.Trainee.Validator
{
    public class CreateTraineeValidator
        : AbstractValidator<CreateTraineeRequest>
    {
        public CreateTraineeValidator()
        {
            RuleFor(x => x.AcademyId)
                .NotEmpty()
                .WithMessage("Academy is required.");

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

            RuleFor(x => x.Type)
                .IsInEnum()
                .WithMessage("Trainee registration type is invalid.");

            RuleFor(x => x.Children)
                .Must((request, children) =>
                    request.Type == RegistrationType.Self
                        ? children.Count == 0
                        : children.Count > 0)
                .WithMessage(
                    "A parent must have at least one child, while a self registration cannot have children.");

            RuleForEach(x => x.Children)
                .SetValidator(
                    new CreateTraineeChildValidator());
        }
    }
}
