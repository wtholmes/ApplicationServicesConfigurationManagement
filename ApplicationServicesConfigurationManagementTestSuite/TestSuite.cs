﻿using ActiveDirectoryAccess;
using ApplicationServicesConfigurationManagementDatabaseAccess;
using ServiceEventLoggingManager;
using System;
using System.Threading;

namespace ApplicationServicesConfigurationManagementTestSuite
{
    internal class TestSuite
    {
        private static void Main(string[] args)
        {
            if (true)
            {
                while (true)
                {
                    using (ActiveDirectoryTopology topology = new ActiveDirectoryTopology())
                    {
                        using (ActiveDirectoryManagementDatabaseAccess activeDirectoryManagementDatabaseAccess = new ActiveDirectoryManagementDatabaseAccess(topology))
                        {
                            DomainControllerUSNQueryRange domainControllerUSNQueryRange = activeDirectoryManagementDatabaseAccess.SelectSiteDomainControler();

                            ActiveDirectoryContext activeDirectoryContext = new ActiveDirectoryContext();
                            Console.WriteLine("------------------------");
                            Console.WriteLine("Querying Domain Controller: {0} with {1}", domainControllerUSNQueryRange.DomainControllerName, domainControllerUSNQueryRange.ADearchFilter);

                            activeDirectoryManagementDatabaseAccess.CreateConfigurationTasksForActiveDirecoryObjects(activeDirectoryContext.SearchDirectory(domainControllerUSNQueryRange));

                            foreach (ActiveDirectoryEntity activeDirectoryEntity in activeDirectoryContext.SearchDirectory(domainControllerUSNQueryRange))
                            {
                                Console.WriteLine("{0}  :  {1}", activeDirectoryEntity.objectGUID, activeDirectoryEntity.distinguishedName);
                            }
                        }
                        ///------------
                        using (OnPremisesExchangeManagementDatabaseAccess onPremisesExchangeManagementDatabaseAccess = new OnPremisesExchangeManagementDatabaseAccess())
                        {
                            {
                                foreach (Int32 configurationTask_Id in onPremisesExchangeManagementDatabaseAccess.GetConfigurationTasks())
                                {
                                    ConfigurationTask configurationTask = onPremisesExchangeManagementDatabaseAccess.GetConfigurationTask(configurationTask_Id);

                                    switch (configurationTask.ConfigurationTaskStatus.Status)
                                    {
                                        case "NEW":
                                            break;

                                        case "PENDING":
                                            break;

                                        case "INPROCESS":
                                            break;

                                        case "TEMPFAIL":
                                            break;

                                        case "RETRY":
                                            break;

                                        case "COMPLETE":
                                            break;

                                        case "FAILED":
                                            break;

                                        case "CANCELLED":
                                            break;
                                        // Unimplmented Task Status.
                                        default:
                                            break;
                                    }
                                }
                            }
                        }
                        Thread.Sleep(Convert.ToInt32(new TimeSpan(0, 0, 15).TotalMilliseconds));
                    }
                }

                if (false)
                {
                    WindowsEventLogClient windowsEventLogClient = new WindowsEventLogClient("ApplicationServicesConfigurationManagement", "ApplicationServicesConfigurationManagement");
                    windowsEventLogClient.AddEventDetail("UserprincipalName", "William");
                    windowsEventLogClient.AddEventDetail("Affiliation", "Staff");
                    windowsEventLogClient.WriteEventLogEntry(System.Diagnostics.EventLogEntryType.Warning, 1000, "This is a message");
                }
            }
        }
    }
}