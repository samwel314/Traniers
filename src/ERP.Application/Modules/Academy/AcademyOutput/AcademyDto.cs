namespace ERP.Application.Modules.Academy.AcademyOutput
{
    public class AcademyDto
    {
        public Guid Id { get; set; }
        public int SerialNumber { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public Guid SportId { get; set; }
        public Guid? OrganizationId { get; set; }
        public Guid? ClubId { get; set; }
        public Guid? AdminId { get; set; }
        public Guid CityId { get; set; }
        public Guid AreaId { get; set; }
        public string ? LogUrl { get; set; }    
        public string Address { get; set; } = null!;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string FirstPhone { get; set; } = null!;
        public string? SecondPhone { get; set; }
        public string? Email { get; set; }
        public string? WebsiteUrl { get; set; }
        public string? FacebookUrl { get; set; }
        public string? InstagramUrl { get; set; }
    }
}
