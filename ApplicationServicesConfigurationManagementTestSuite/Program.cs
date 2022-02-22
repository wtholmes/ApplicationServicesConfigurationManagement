using ServiceEventLoggingManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApplicationServicesConfigurationManagementTestSuite
{
    internal class Program
    {
        static void Main(string[] args)
        {
            WindowsEventLogClient windowsEventLogClient = new WindowsEventLogClient("ApplicationServicesConfigurationManagement", "ApplicationServicesConfigurationManagement");
            windowsEventLogClient.AddEventDetail("UserprincipalName", "William");
            windowsEventLogClient.AddEventDetail("Affiliation", "Staff");
            windowsEventLogClient.WriteEventLogEntry(System.Diagnostics.EventLogEntryType.Warning, 1000, "This is a message");
        
        }
    }
}
