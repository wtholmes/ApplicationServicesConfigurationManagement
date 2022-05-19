using System.ServiceProcess;

namespace TeamDynamixManagementService
{
    internal static class TeamDynamixManagementServiceHost
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        private static void Main()
        {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new TeamDynamixManagementService()
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}