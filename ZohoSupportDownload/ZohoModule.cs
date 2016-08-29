namespace ZohoSupportDownload
{
    public enum ZohoModule
    {
        Cases,
        Requests,
        Solutions,
        Accounts,
        Contacts,
        Contracts,
        Products,
        Tasks,
        TimeEntry
    }

    public static class ZohoModuleHelpers
    {
        public static string IdField(this ZohoModule module)
        {
            switch (module)
            {
                case ZohoModule.Requests:
                    return "CASEID";
                case ZohoModule.TimeEntry:
                    return "TIME_ENTRY_ID";
                case ZohoModule.Tasks:
                    return "ACTIVITYID";
                default:
                    return module.ToString().ToUpperInvariant().TrimEnd('S') + "ID";
            }
        }
    }
}