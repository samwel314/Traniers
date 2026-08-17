namespace ERP.Domain.Modules.Academy.Entities;

public class AcademyAdministrator
{
    public Guid AcademyId { get; private set; }
    public Guid AdministratorId { get; private set; }

    public Academy Academy { get; private set; } = null!;

    public AcademyAdministrator(
        Guid academyId,
        Guid administratorId)
    {
        AcademyId = academyId;
        AdministratorId = administratorId;
    }
}