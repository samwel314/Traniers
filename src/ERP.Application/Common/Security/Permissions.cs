namespace ERP.Application.Common.Security;

/// <summary>
/// The permission catalogue. Declare every permission your modules use here so a
/// typo is a compile error at the call site instead of a silent security hole.
///
/// Services enforce these through <c>IPermissionGuard.Require(...)</c>; the
/// administrator role is seeded with everything in <see cref="All"/>.
/// </summary>
public static class Permissions
{
    public static class Academy
    {
        public const string ViewPlayers = "Academy.Players.View";
        public const string CreatePlayer = "Academy.Players.Create";
        public const string UpdatePlayer = "Academy.Players.Update";
        public const string DeletePlayer = "Academy.Players.Delete";
    }

    public static IReadOnlyList<string> All =>
    [
        Academy.ViewPlayers,
        Academy.CreatePlayer,
        Academy.UpdatePlayer,
        Academy.DeletePlayer
    ];
}

public static class Roles
{
    public const string Administrator = "Administrator";
    public const string Viewer = "Viewer";

    public static IReadOnlyList<string> All => [Administrator, Viewer];
}
