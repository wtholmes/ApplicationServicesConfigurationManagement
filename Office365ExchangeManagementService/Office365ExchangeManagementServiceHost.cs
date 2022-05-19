using System.ServiceProcess;

namespace Office365ExchangeManagementService
{
    internal static class Office365ExchangeManagementServiceHost
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        private static void Main()
        {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new Office365ExchangeManagementService()
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}