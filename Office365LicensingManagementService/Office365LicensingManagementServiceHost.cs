using System.ServiceProcess;

namespace Office365LicensingManagementService
{
    internal static class Office365LicensingManagementServiceHost
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        private static void Main()
        {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new Office365LicensingManagementService()
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}