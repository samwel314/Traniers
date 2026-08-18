using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;

namespace ERP.Application.Modules.Academy.AcademyOutput
{
    public class AcademyLockupDto
    {
        public Guid Id { get; set; }
        public int SerialNumber { get; set; }
        public string Name { get; set; } = null!;
        public string FirstPhone { get; set; } = null!;
        public string Location { get; set; } = null!;   
    }
}
