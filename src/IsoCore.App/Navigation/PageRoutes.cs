namespace IsoCore.App.Navigation
{
    public enum PageRoute
    {
        Dashboard,
        Projects,
        CalculationsAndPriceLists,
        Controlling,
        PeopleAndCompanies,
        SettingsUsers,
        SettingsBranding,
        SettingsPaths,
        SettingsSystem
    }

    public static class PageRouteNames
    {
        public const string Dashboard = "dashboard";
        public const string Projects = "projects";
        public const string CalculationsAndPriceLists = "calculations";
        public const string Controlling = "controlling";
        public const string PeopleAndCompanies = "people";
        public const string SettingsUsers = "settings.users";
        public const string SettingsBranding = "settings.branding";
        public const string SettingsPaths = "settings.paths";
        public const string SettingsSystem = "settings.system";

        public static string ToName(PageRoute route)
        {
            return route switch
            {
                PageRoute.Dashboard => Dashboard,
                PageRoute.Projects => Projects,
                PageRoute.CalculationsAndPriceLists => CalculationsAndPriceLists,
                PageRoute.Controlling => Controlling,
                PageRoute.PeopleAndCompanies => PeopleAndCompanies,
                PageRoute.SettingsUsers => SettingsUsers,
                PageRoute.SettingsBranding => SettingsBranding,
                PageRoute.SettingsPaths => SettingsPaths,
                PageRoute.SettingsSystem => SettingsSystem,
                _ => Dashboard
            };
        }

        public static PageRoute? FromName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            return name.Trim().ToLowerInvariant() switch
            {
                Dashboard => PageRoute.Dashboard,
                Projects => PageRoute.Projects,
                CalculationsAndPriceLists => PageRoute.CalculationsAndPriceLists,
                Controlling => PageRoute.Controlling,
                PeopleAndCompanies => PageRoute.PeopleAndCompanies,
                SettingsUsers => PageRoute.SettingsUsers,
                SettingsBranding => PageRoute.SettingsBranding,
                SettingsPaths => PageRoute.SettingsPaths,
                SettingsSystem => PageRoute.SettingsSystem,
                _ => null
            };
        }
    }
}
