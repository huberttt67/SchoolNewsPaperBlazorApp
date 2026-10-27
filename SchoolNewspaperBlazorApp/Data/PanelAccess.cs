namespace SchoolNewspaperBlazorApp.Data
{
    // Jedno miejsce, które mówi, jakie uprawnienia wpuszczają do panelu
    // administracyjnego. Korzysta z niego panel, układ strony i zastępstwa,
    // dzięki czemu przyciski „Panel administratora” i „Wróć do panelu”
    // pokazują się tylko osobom, które faktycznie mogą do panelu wejść.
    public static class PanelAccess
    {
        public static readonly EnumPermission[] Permissions =
        {
            EnumPermission.Master,
            EnumPermission.AccessAdminPanel,
            EnumPermission.CreateArticles,
            EnumPermission.EditArticles,
            EnumPermission.DeleteArticles,
            EnumPermission.ViewUsers,
            EnumPermission.ManageUsers,
            EnumPermission.CreateSubstitutions,
            EnumPermission.EditSubstitutions,
            EnumPermission.DeleteSubstitutions
        };

        public static bool HasAccess(IEnumerable<EnumPermission> userPermissions)
            => userPermissions.Any(p => Permissions.Contains(p));
    }
}