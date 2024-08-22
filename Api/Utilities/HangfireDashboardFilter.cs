using Hangfire.Dashboard;

public class UseHangfireDashboardFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext dashboardContext)
        {
            return true;
        }
    }