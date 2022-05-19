using System.ServiceProcess;

namespace ActiveDirectoryManagementService
{
    internal static class ActiveDirectoryManagementServiceHost
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        private static void Main()
        {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new ActiveDirectoryManagementService()
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}