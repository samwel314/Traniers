using ERP.Application.Modules.Academy.AcademyInput;
using ERP.Application.Modules.Academy.AcademyOutput;

namespace ERP.Application.Modules.Academy.AcademyMapper
{
    public static class AcademyMappings
    {
        public static Domain.Modules.Academy.Entities.Academy ToEntity(this CreateAcademyRequest request)
        {
            return new  Domain.Modules.Academy.Entities.Academy
            {
                NameEn = request.NameEn,
                NameAr = request.NameAr,
                DescriptionEn = request.DescriptionEn,
                DescriptionAr = request.DescriptionAr,
                SportId = request.SportId,
                OrganizationId = request.OrganizationId,
                ClubId = request.ClubId,
                AdminId = request.AdminId,
                CityId = request.CityId,
                AreaId = request.AreaId,
                Address = request.Address,
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                FirstPhone = request.FirstPhone,
                SecondPhone = request.SecondPhone,
                Email = request.Email,
                WebsiteUrl = request.WebsiteUrl,
                FacebookUrl = request.FacebookUrl,
                InstagramUrl = request.InstagramUrl
            };
        }

        public static void UpdateFromRequest(
     this Domain.Modules.Academy.Entities.Academy academy,
     UpdateAcademyRequest request)
        {
            academy.NameEn = request.NameEn;
            academy.NameAr = request.NameAr;
            academy.DescriptionEn = request.DescriptionEn;
            academy.DescriptionAr = request.DescriptionAr;
            academy.SportId = request.SportId;
            academy.OrganizationId = request.OrganizationId;
            academy.ClubId = request.ClubId;
            academy.AdminId = request.AdminId;
            academy.CityId = request.CityId;
            academy.AreaId = request.AreaId;
            academy.Address = request.Address;
            academy.Latitude = request.Latitude;
            academy.Longitude = request.Longitude;
            academy.FirstPhone = request.FirstPhone;
            academy.SecondPhone = request.SecondPhone;
            academy.Email = request.Email;
            academy.WebsiteUrl = request.WebsiteUrl;
            academy.FacebookUrl = request.FacebookUrl;
            academy.InstagramUrl = request.InstagramUrl;
        }

        public static AcademyResponseDto ToResponse(this    Domain.Modules.Academy.Entities.Academy academy)
        {
            return new AcademyResponseDto
            {
                Id = academy.Id,
                NameEn = academy.NameEn,
                NameAr = academy.NameAr,
                DescriptionEn = academy.DescriptionEn,
                DescriptionAr = academy.DescriptionAr,
                SportId = academy.SportId,
                OrganizationId = academy.OrganizationId,
                ClubId = academy.ClubId,
                AdminId = academy.AdminId,
                CityId = academy.CityId,
                AreaId = academy.AreaId,
                Address = academy.Address,
                Latitude = academy.Latitude,
                Longitude = academy.Longitude,
                FirstPhone = academy.FirstPhone,
                SecondPhone = academy.SecondPhone,
                Email = academy.Email,
                WebsiteUrl = academy.WebsiteUrl,
                FacebookUrl = academy.FacebookUrl,
                InstagramUrl = academy.InstagramUrl,
                LogoPath = academy.LogoPath,    
            };
        }
    }

}
