using ActiveDirectoryAccess;
using ApplicationServicesConfigurationManagementDatabaseAccess;
using AuthenticationServices;
using ServiceEventLoggingManager;
using System;
using System.Collections.Generic;
using System.Threading;

namespace ApplicationServicesConfigurationManagementTestSuite
{
    internal class TestSuite
    {
        private static void Main(string[] args)
        {
            if (true)
            {
                using (OAuth2AuthenticationContext context = new OAuth2AuthenticationContext())
                {
                    Boolean ProvisionRoles = true;

                    if (ProvisionRoles)
                    {
                        List<String> Roles = new List<String>()
                    {
                        "COEAWebAPIReadWrite",
                        "COEAWebAPIRead",
                        "ListServiceWebAPIContactReadWrite",
                        "ListServiceWebAPIContactRead",
                        "ListServiceWebAPIOwnerTransfer"
                    };

                        List<String> RoleDescriptions = new List<String>()
                    {
                        "COEA WebAPI Read Write Access",
                        "COEA WebAPI Read Access",
                        "List Manager Service Contact Read Write Access",
                        "List Manager Service Contact Read",
                        "List Manager Service Owner Transfer"
                    };

                        for (int index = 0; index < Roles.Count; index++)
                        {
                            OAuth2ClientRole oAuth2ClientRole = new OAuth2ClientRole()
                            {
                                RoleName = Roles[index],
                                RoleDescription = RoleDescriptions[index],
                                WhenCreated = DateTime.UtcNow
                            };

                            context.OAuth2ClientRoles.Add(oAuth2ClientRole);
                            context.SaveChanges();

                        }
                    }
                }
            }
        
    
            if (false)
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