using ERP.Application.Modules.Trainee.TraineeInput;
using ERP.Application.Modules.Trainee.TraineeOutput;
using ERP.Domain.Modules.Trainee.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ERP.Application.Modules.Trainee.Mapper
{
    public static class TraineeMapper
    {
        public static ERP.Domain.Modules.Trainee.Entities.Trainee ToEntity(
            this CreateTraineeRequest request)
        {
            return new ERP.Domain.Modules.Trainee.Entities.Trainee
            {
                Id = Guid.NewGuid(),
                FirstName = request.FirstName,
                LastName = request.LastName,
                PhoneNumber = request.PhoneNumber,
                Age = request.Age,
                Gender = request.Gender,
                Type = request.Type
            };
        }

        public static ERP.Domain.Modules.Trainee.Entities.Trainee ToEntity(
            this CreateTraineeChildRequest request,
            Guid parentId)
        {
            return new ERP.Domain.Modules.Trainee.Entities.Trainee
            {
                Id = Guid.NewGuid(),
                FirstName = request.FirstName,
                LastName = request.LastName,
                PhoneNumber = request.PhoneNumber,
                Age = request.Age,
                Gender = request.Gender,
                Type = RegistrationType.Self,
                ParentId = parentId
            };
        }

        public static TraineeResponseDto ToResponse(
            this ERP.Domain.Modules.Trainee.Entities.Trainee trainee)
        {
            return new TraineeResponseDto
            {
                Id = trainee.Id,
                FirstName = trainee.FirstName,
                LastName = trainee.LastName,
                PhoneNumber = trainee.PhoneNumber,
                Age = trainee.Age,
                Gender = trainee.Gender,
                Photo = trainee.Photo,
                Type = trainee.Type
            };
        }

        public static TraineeChildDto ToChildDto(
            this ERP.Domain.Modules.Trainee.Entities.Trainee trainee)
        {
            return new TraineeChildDto
            {
                Id = trainee.Id,
                FirstName = trainee.FirstName,
                LastName = trainee.LastName,
                PhoneNumber = trainee.PhoneNumber,
                Age = trainee.Age,
                Gender = trainee.Gender,
                Photo = trainee.Photo
            };
        }
    }
}
