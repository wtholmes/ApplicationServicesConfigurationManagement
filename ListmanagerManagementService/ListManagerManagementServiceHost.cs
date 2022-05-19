using System.ServiceProcess;

namespace ListmanagerManagementService
{
    internal static class ListmanagerManagementServiceHost
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        private static void Main()
        {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new ListmanagerManagementService()
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}