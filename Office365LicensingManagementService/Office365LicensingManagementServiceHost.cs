using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace Office365LicensingManagementService
{
    internal static class Office365LicensingManagementServiceHost
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
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
