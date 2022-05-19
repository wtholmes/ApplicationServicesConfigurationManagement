using System.ServiceProcess;

namespace OnPremisesExchangeManagementService
{
    internal static class OnPremisesExchangeManagementServiceHost
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        private static void Main()
        {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new OnPremisesExchangeManagementService()
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}