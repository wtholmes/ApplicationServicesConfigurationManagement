using ListServiceManagement.Models;
using PowerShellRunspaceManager;
using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Threading;

namespace ProvisionElistContacts
{
    internal class ProvisionElistContacts
    {
        private static void Main(string[] args)
        {
            Boolean ProvisionContacts = true;
            Boolean DeltaSync = true;
            Boolean Unsynced = false;

            ListServiceManagementContext context = new ListServiceManagementContext();

            DirectoryEntry rootDSE = new DirectoryEntry("LDAP://RootDSE");
            DirectoryEntry activeDirectory = new DirectoryEntry(String.Format("LDAP://{0}", rootDSE.Properties["defaultNamingContext"][0]));
            ExchangeOnPremManager exchangeOnPremManager = new ExchangeOnPremManager("sf-ex-2019-02.exchange.cornell.edu", true);
            String LogFileName = String.Format(@".\LogFiles\{0}_Log.txt", DateTime.UtcNow.ToString("yyyyMMddhh"));
            using (StreamWriter logfile = File.AppendText(LogFileName))
            {
                try
                {
                    // This is the default Guid when a new database entry is created.
                    List<ElistContact> elistContacts;

                    if (DeltaSync)
                    {
                        DateTime UpdateSince = DateTime.UtcNow.AddSeconds(new TimeSpan(24, 0, 0).TotalSeconds * -1);
                        elistContacts = context.ElistContacts.Where(c => c.WhenModified > UpdateSince).ToList();
                    }
                    else if (Unsynced)
                    {
                        Guid NonSyncedGuid = Guid.Parse("00000000-0000-0000-0000-000000000000");
                        elistContacts = context.ElistContacts.Where(c => c.ListContactDirectory_Id == NonSyncedGuid).ToList();
                    }
                    else
                    {
                        elistContacts = context.ElistContacts.ToList();
                    }

                    foreach (ElistContact elistContact in elistContacts)
                    {
                        Thread.Sleep(new TimeSpan(0, 0, 5).Milliseconds);
                        try
                        {
                            logfile.WriteLine("[{0} UTC]: Synchronizing Elist Contact: {1}", DateTime.UtcNow.ToString(), elistContact.ListName);
                            Console.WriteLine("Synchronizing Elist Contact: {0}", elistContact.ListName);
                            Dictionary<String, String> DirectoryContactNames = new Dictionary<String, String>();
                            DirectoryContactNames.Add("ListContact", elistContact.ListName.Trim());
                            DirectoryContactNames.Add("OwnerContact", string.Format("OWNER-{0}", elistContact.ListName.Trim()));
                            DirectoryContactNames.Add("RequestContact", string.Format("{0}-REQUEST", elistContact.ListName.Trim()));

                            Guid objectGuid = new Guid();
                            foreach (String ContactUsage in DirectoryContactNames.Keys)
                            {
                                using (DirectorySearcher directorySearcher = new DirectorySearcher(activeDirectory))
                                {
                                    directorySearcher.PageSize = 1000;
                                    directorySearcher.ServerPageTimeLimit = TimeSpan.FromSeconds(4);
                                    directorySearcher.CacheResults = false;
                                    directorySearcher.Filter = String.Format("(&(objectClass=contact)(name={0}))", DirectoryContactNames[ContactUsage]);
                                    SearchResultCollection searchResults = directorySearcher.FindAll();

                                    if (searchResults.Count == 1) // Sync the objectGUID back to the Elist Contacts Database.
                                    {
                                        objectGuid = new Guid((byte[])searchResults[0].Properties["objectGUID"][0]);

                                        // Set the appropriate contact's objectGuid.
                                        switch (ContactUsage)
                                        {
                                            case "ListContact":
                                                elistContact.ListContactDirectory_Id = objectGuid;
                                                break;

                                            case "OwnerContact":
                                                elistContact.OwnerContactDirectory_Id = objectGuid;
                                                break;

                                            case "RequestContact":
                                                elistContact.RequestContactDirectory_Id = objectGuid;
                                                break;

                                            default:
                                                break;
                                        }

                                        Int32 msExchRecipientDisplayType = 0;
                                        if (searchResults[0].Properties["msExchRecipientDisplayType"].Count != 0)
                                        {
                                            msExchRecipientDisplayType = Convert.ToInt32(searchResults[0].Properties["msExchRecipientDisplayType"][0]);
                                        }
                                        else
                                        {
                                            msExchRecipientDisplayType = 0;
                                        }

                                        // Get the expected target address and current target address for this list contact.
                                        if (elistContact.Enabled)
                                        {
                                            String ExpectedContactExternalEmailAddress = String.Format("{0}@{1}", DirectoryContactNames[ContactUsage], elistContact.ListDomainName);
                                            String CurrentContactExternalEmailAddress = "";

                                            if (msExchRecipientDisplayType.Equals(6))
                                            {
                                                if (searchResults[0].Properties["targetAddress"].Count != 0)
                                                {
                                                    CurrentContactExternalEmailAddress = searchResults[0].Properties["targetAddress"][0].ToString().Split(':')[1];
                                                }

                                                // Check if the expected target email address is differnt than the current target address and if so update the contact.
                                                if (!ExpectedContactExternalEmailAddress.Equals(CurrentContactExternalEmailAddress, StringComparison.OrdinalIgnoreCase))
                                                {
                                                    Console.WriteLine("--- Updating External Email Address to: {0}", ExpectedContactExternalEmailAddress);
                                                    exchangeOnPremManager.SetMailContactNewExternalEmailAddress(searchResults[0].Properties["distinguishedName"][0].ToString(), ExpectedContactExternalEmailAddress, true);
                                                }
                                            }
                                            else
                                            {
                                                if (msExchRecipientDisplayType.Equals(0))
                                                {
                                                    exchangeOnPremManager.EnableMailContact(DirectoryContactNames[ContactUsage], ExpectedContactExternalEmailAddress);
                                                }
                                            }
                                        }

                                        if (msExchRecipientDisplayType.Equals(6))
                                        {
                                            if (elistContact.Enabled.Equals(false))
                                            {
                                                exchangeOnPremManager.DisableMailContact(DirectoryContactNames[ContactUsage]);
                                            }
                                        }
                                        else
                                        {
                                            if (elistContact.Enabled.Equals(true))
                                            {
                                                MailAddress externalEmailAddress = new MailAddress(String.Format("{0}@{1}", DirectoryContactNames[ContactUsage], elistContact.ListDomainName));
                                                exchangeOnPremManager.EnableMailContact(DirectoryContactNames[ContactUsage], externalEmailAddress.Address);
                                            }
                                        }
                                    }
                                    else if (searchResults.Count == 0) // Provision a new mail contact.
                                    {
                                        if (elistContact.Enabled.Equals(true))
                                        {
                                            Console.WriteLine("Contact for {0} does not exist", DirectoryContactNames[ContactUsage]);
                                            if (ProvisionContacts)
                                            {
                                                MailAddress externalEmailAddress = new MailAddress(String.Format("{0}@{1}", DirectoryContactNames[ContactUsage], elistContact.ListDomainName));
                                                var x = exchangeOnPremManager.NewMailContact(externalEmailAddress.Address, elistContact.ListDisplayName, "cornell.edu/CITExchangeObjects/List Service Objects");
                                                if (externalEmailAddress.User.StartsWith("OWNER-") || externalEmailAddress.User.EndsWith("-REQUEST"))
                                                {
                                                    var z = exchangeOnPremManager.SetMailContactHidden(externalEmailAddress.Address);
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            context.SaveChanges();
                        }
                        catch (Exception exp)
                        {
                            logfile.WriteLine("      %%%%%%% -An exception has occurred -%%%%%%\n\n");
                            logfile.WriteLine("{0}", exp);
                            logfile.WriteLine("      %%%%%%% -An exception has occurred -%%%%%%\n\n");
                        }
                    }

                    // E-list Contact De-provisioning...




                }
                catch (Exception exp)
                {
                    logfile.WriteLine("      %%%%%%% -An exception has occurred -%%%%%%\n\n");
                    logfile.WriteLine("{0}", exp);
                    logfile.WriteLine("      %%%%%%% -An exception has occurred -%%%%%%\n\n");
                }
            }
        }
    }
}