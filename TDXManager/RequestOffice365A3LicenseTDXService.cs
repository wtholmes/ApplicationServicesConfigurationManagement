﻿using ActiveDirectoryAccess;
using CornellIdentityManagement;
using MicrosoftAzureManager;
using System;
using System.Diagnostics;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TeamDynamix.Api.Tickets;

namespace TDXManager
{
    /// <summary>
    /// This derived class reads Office 365 A3 License Requests from TeamDynamix and if
    /// appropriate assigns an Office 365 Faculty A3 License to the Ticket's Requester.
    /// </summary>
    public class RequestOffice365A3LicenseTDXService : TDXTicketManager
    {
        public RequestOffice365A3LicenseTDXService()
        {
            // Create an Active Directory Context.
            ActiveDirectoryContext activeDirectoryContext = new ActiveDirectoryContext();

            // Start the Office 365 Licensing Manager to Lookup License Usage in Azure.
            Office365LicensingManager office365LicensingManager = new Office365LicensingManager();

            // Start a ProvAccounts Manager.
            ProvAccountsManager provAccountsManager = new ProvAccountsManager();

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            // ------
            // Get the list of tickets from TDX using Email and Calendar / Request Office 365 Faculty A3 License report. This report
            // returns all of the tickets that are using the: Email and Calendar / Request Office 365 Faculty A3 License form. Filter
            // the report to only return requests that are in an active state by excluding inactive states.
            // ------
            Regex InactiveTicketsRegex = new Regex(@"(Reopened|Resolved|Closed|Canceled)", RegexOptions.IgnoreCase);
            GetTicketsUsingReport("* Email and Calendar / Request Office 365 Faculty A3 License", InactiveTicketsRegex);

            // Process the tickets returned from the reports.
            foreach (Ticket ticket in this.TDXTickets)
            {
                if (ticket != null)
                {
                    String ticketStatus = ticket.StatusName;
                    if (!InactiveTicketsRegex.IsMatch(ticketStatus))
                    {
                        // Set the Active Ticket, this sets the scope for all functions and methods.
                        this.SetActiveTicket(ticket);

                        // Get Automation Status Details [TDX Custom Attribute: (S111-AUTOMATIONDETAILS)] in a StringBuilder
                        // so that we can update the automation details to the TDX Request.
                        StringBuilder AutomationDetails = new StringBuilder(this.TDXAutomationTicket.AutomationDetails);

                        // Ticket Comments StringBuilder.
                        StringBuilder TicketComments = new StringBuilder();

                        // ------
                        // Get Automation Status [TDX Custom Attribute: (S111-AUTOMATIONSTATUS)]. The Automation Status Attribute is used
                        // to direct automation processing. It is intended that it be updated by this class and by TeamDynamix Work-flows.
                        // The standard configuration of TDX forms should not allow for manual updates to (S111-AUTOMATIONSTATUS) unless
                        // every possible state change can be handled by this class or its parent(s). As with allowing manual updates, when
                        // creating TeamDynamix work-flow consideration must be given to setting (S111-AUTOMATIONSTATUS) such that the follow
                        // processing steps will run in the desired order or that the processing steps are order independent.
                        // ------

                        switch (this.TDXAutomationTicket.AutomationStatus)
                        {
                            // Initiate Processing of newly submitted tickets.
                            case null:
                                {
                                    // Setup the Request Title.
                                    StringBuilder RequestTitle = new StringBuilder("Office 365 License Request for:");
                                    RequestTitle.AppendFormat(" {0}",
                                        this.TDXAutomationTicket.TicketRequestor.UserPrincipalName);

                                    // Setup the request Description.
                                    StringBuilder RequestDescription = new StringBuilder();
                                    if (this.TDXAutomationTicket.TicketCreator.UserPrincipalName == this.TDXAutomationTicket.TicketRequestor.UserPrincipalName)
                                    {
                                        RequestDescription.Append("You have requested a new Office 365 License. Your request is being processed.");
                                    }
                                    else
                                    {
                                        RequestDescription.AppendFormat("We have received a request submitted on your behalf by: {0} <{1}> for a new Office 365 License. The request is being processed",
                                            this.TDXAutomationTicket.TicketCreator.DisplayName,
                                            this.TDXAutomationTicket.TicketCreator.UserPrincipalName);
                                    }

                                    // Update the Ticket Title and Description.
                                    this.UpdateTicketTitleAndDescription(RequestTitle, RequestDescription);

                                    // Update the Automation Status and Automation Status Details.
                                    this.UpdateAutomationStatus(AUTOMATIONSTATUS.INPROCESS);

                                    // Update the ticket and notify the customer.
                                    TicketComments.AppendFormat("We have received your request for an Office 365 License change. Your request is now in process.");
                                    this.NotifyCreator = true;
                                    this.NotifyRequestor = true;
                                    this.UpdateTicket(TicketComments, "In Process");

                                    break;
                                }
                            // Automation Processing for NEW Tickets.
                            case var value when value == AUTOMATIONSTATUS.NEW:
                                {
                                    // This automation status currently has no actions associated with it.
                                    // Update the Automation Status and Automation Status Details to move the request into INPROCESS State.
                                    this.UpdateAutomationStatus(AUTOMATIONSTATUS.INPROCESS);
                                    AutomationDetails.AppendFormat("[{0}]: AutomationStatus has been set to INPROCESS.", DateTime.UtcNow.ToString());
                                    break;
                                }
                            // Automation Processing for INPROCESS Tickets.
                            case var value when value == AUTOMATIONSTATUS.INPROCESS:
                                {
                                    Boolean ProvisionBookings = false;
                                    if (this.TDXAutomationTicket.ServiceOfferingName.Equals("Bookings Access"))
                                    {
                                        ProvisionBookings = true;
                                    }

                                    Boolean RequestAllowed = false;

                                    // Is the creator of this ticket equal to the requester (Target).
                                    if (this.TDXAutomationTicket.TicketCreator.UserPrincipalName == this.TDXAutomationTicket.TicketRequestor.UserPrincipalName)
                                    {
                                        AutomationDetails.AppendFormat(" , [{0}]: The requester is the creator.   ", DateTime.UtcNow.ToString());
                                        RequestAllowed = true;
                                    }
                                    // The creator is not the requester (Target)
                                    else
                                    {
                                        // Update the automation status.
                                        AutomationDetails.AppendFormat(" , [{0}]: This request was made on behalf of the requester.", DateTime.UtcNow.ToString());

                                        // If the creator is in A3 Delegate (TSP) Group then the request is allowed.
                                        if(activeDirectoryContext.CheckGroupMembership(this.TDXAutomationTicket.TicketCreator.UserPrincipalName, "RequestFacultyA3LicenseDelegate", "cornell"))
                                        //if (this.TDXAutomationTicket.TicketCreator.MemberOf.Contains("RequestFacultyA3LicenseDelegate"))
                                        {
                                            RequestAllowed = true;
                                        }
                                        // Request denied as ticket creator equal to the requester and is not allowed to request on behalf of the customer.
                                        else
                                        {
                                            // Disallow the request.
                                            RequestAllowed = false;

                                            // Update the Automation Status and Automation Status Details.
                                            this.UpdateAutomationStatus(AUTOMATIONSTATUS.DECLINED);
                                            AutomationDetails.AppendFormat(" , [{0}]: The creator {1} is not allowed to request licenses on behalf of other users. The request has been cancelled.",
                                                DateTime.UtcNow.ToString(),
                                                this.TDXAutomationTicket.TicketCreator.UserPrincipalName);

                                            // Update the ticket and notify the customer.
                                            TicketComments.AppendFormat("{0} {1} is not authorized to request an Office 365 License on your behalf. No changes have been made to your account.",
                                                this.TDXAutomationTicket.TicketCreator.DisplayName,
                                                this.TDXAutomationTicket.TicketCreator.UserPrincipalName);

                                            this.NotifyCreator = true;
                                            this.NotifyRequestor = true;
                                            this.UpdateTicket(TicketComments, "Cancelled");

                                            // Assign the cancelled request to L3
                                            this.UpdateResponsibleGroup(45);
                                        }
                                    }

                                    // Does the target already have an Office 365 A3 License?
                                    if (RequestAllowed)
                                    {
                                        if (this.TDXAutomationTicket.TicketRequestor.ProvAccts.Contains("office365-a3")) // Requester already has an Office 365 A3 License.
                                        {
                                            // Bookings has been requested so add the user to the O365-Bookings Licensing Group.
                                            if (ProvisionBookings)
                                            {
                                                try
                                                {
                                                    using (PrincipalContext principalContext = new PrincipalContext(ContextType.Domain))
                                                    {
                                                        GroupPrincipal group = GroupPrincipal.FindByIdentity(principalContext, "O365-Bookings");
                                                        group.Members.Add(principalContext, IdentityType.UserPrincipalName, this.TDXAutomationTicket.TicketRequestor.UserPrincipalName);
                                                        group.Save();
                                                    }
                                                }
                                                catch (Exception exp)
                                                {
                                                    //doSomething with E.Message.ToString(); 
                                                }

                                                // Update the Automation Status and Automation Status Details.
                                                this.UpdateAutomationStatus(AUTOMATIONSTATUS.APPROVED);
                                                AutomationDetails.AppendFormat(" , [{0}]: Microsoft Bookings has been provisioned.",
                                                    DateTime.UtcNow.ToString());

                                                // Update the ticket and notify the customer.
                                                TicketComments.AppendFormat("Your Office 365 Bookings License Request has been approved and is currently being assigned. We will notify you when the license assignment is complete.");
                                                this.NotifyCreator = true;
                                                this.NotifyRequestor = true;
                                                this.UpdateTicket(TicketComments);
                                            }
                                            else
                                            {
                                                // Disallow the request
                                                RequestAllowed = false;

                                                // Update the Automation Status and Automation Status Details.
                                                this.UpdateAutomationStatus(AUTOMATIONSTATUS.CANCELED);
                                                AutomationDetails.AppendFormat(" , [{0}]: The requester already has an Office 365 A3 License. Their affiliation is: {1}. This request has been cancelled.",
                                                    DateTime.UtcNow.ToString(),
                                                    this.TDXAutomationTicket.TicketRequestor.PrimaryAffiliation);

                                                // Update the ticket and notify the customer.
                                                TicketComments.AppendFormat("Your account has already been provisioned with the Office 365 License you have requested. No changes have been made to your account.");
                                                this.NotifyCreator = true;
                                                this.NotifyRequestor = true;
                                                this.UpdateTicket(TicketComments, "Cancelled");

                                                // Assign the cancelled request to L3
                                                this.UpdateResponsibleGroup(45);
                                            }
                                        }
                                    }

                                    // Is the requester entitled to an Office 365 A3 License.
                                    if (RequestAllowed)
                                    {
                                        if (this.TDXAutomationTicket.TicketRequestor.Entitlements.Contains("office365-a3"))
                                        {
                                            RequestAllowed = true;
                                        }
                                        // Requester's affiliation does not include the Office 365 A3 License.
                                        else
                                        {
                                            // Disallow the request.
                                            RequestAllowed = false;

                                            // Update the Automation Status and Automation Status Details.
                                            this.UpdateAutomationStatus(AUTOMATIONSTATUS.CANCELED);
                                            AutomationDetails.AppendFormat(" , [{0}]: The requester does not qualify for an Office 365 A3 License. Their affiliation is: {1}. The request has been cancelled.",
                                                DateTime.UtcNow.ToString(),
                                                this.TDXAutomationTicket.TicketRequestor.PrimaryAffiliation);

                                            // Update the ticket and notify the customer.
                                            TicketComments.AppendFormat("Your Cornell affiliation does not include the Office 365 License you have requested. No changes have been made to your account.");
                                            this.NotifyCreator = true;
                                            this.NotifyRequestor = true;
                                            this.UpdateTicket(TicketComments, "Cancelled");

                                            // Assign the cancelled request to L3
                                            this.UpdateResponsibleGroup(45);
                                        }
                                    }

                                    // Check if we have an Office 365 A3 Licenses available.
                                    if (RequestAllowed)
                                    {
                                        // Get the current number of available A3 Licenses from Azure.
                                        Office365LicensingManager.Office365Subscription office365Subscription = office365LicensingManager.Office365Subscriptions
                                            .Where(s => s.SKU.Equals("ENTERPRISEPACKPLUS_FACULTY"))
                                            .FirstOrDefault();

                                        // Sufficient Licenses exist for automated processing.
                                        if (office365Subscription.AvailableUnits > 25)
                                        {
                                            RequestAllowed = true;
                                        }
                                        // There are insufficient licenses for automated provisioning.
                                        else
                                        {
                                            // Disallow the request.
                                            RequestAllowed = false;

                                            // Update the Automation Status and Automation Status Details.
                                            this.UpdateAutomationStatus(AUTOMATIONSTATUS.PENDINGAPPROVAL);
                                            AutomationDetails.AppendFormat(" , [{0}]: There are currently: {1} Office 365 A3 Licenses Available. This request has been assigned to Level 3 support.",
                                                DateTime.UtcNow.ToString(),
                                                office365Subscription.AvailableUnits);

                                            // Update the ticket and notify the customer.
                                            TicketComments.AppendFormat("There are currently insufficient Office 365 Licenses available. Automated license assignment is suspended. To fulfill this request the license will need to be manually assigned.");
                                            this.NotifyCreator = false;
                                            this.NotifyRequestor = false;
                                            this.UpdateTicket(TicketComments, "In Process", true);

                                            // Escalate the ticket to (L3).
                                            this.UpdateResponsibleGroup(45);
                                        }
                                    }

                                    // This is a valid request so we can assign the ENTERPRISEPACKPLUS_FACULTY (A3) to the customer.
                                    if (RequestAllowed)
                                    {
                                        // Bookings has been requested so add the user to the O365-Bookings Licensing Group.
                                        if (ProvisionBookings)
                                        {
                                            try
                                            {
                                                using (PrincipalContext principalContext = new PrincipalContext(ContextType.Domain))
                                                {
                                                    GroupPrincipal group = GroupPrincipal.FindByIdentity(principalContext, "O365-Bookings");
                                                    group.Members.Add(principalContext, IdentityType.UserPrincipalName, this.TDXAutomationTicket.TicketRequestor.UserPrincipalName);
                                                    group.Save();
                                                    TicketComments.AppendFormat("Microsoft Bookings has been added to your account. ");
                                                }
                                            }
                                            catch (Exception exp)
                                            {
                                                
                                            }
                                        }

                                        // Update the Automation Status and Automation Status Details.
                                        this.UpdateAutomationStatus(AUTOMATIONSTATUS.APPROVED);
                                        AutomationDetails.AppendFormat(" , [{0}]: An Office 365 ENTERPRISEPACKPLUS_FACULTY (A3) is being assigned.",
                                            DateTime.UtcNow.ToString());

                                        // Update the ticket and notify the customer.
                                        TicketComments.AppendFormat("Your Office 365 License Request has been approved and is currently being assigned. We will notify you when the license assignment is complete.");
                                        this.NotifyCreator = true;
                                        this.NotifyRequestor = true;
                                        this.UpdateTicket(TicketComments);
                                    }

                                    break;
                                }

                            // Automation Processing for PENDINGAPPROVAL Tickets.
                            case var value when value == AUTOMATIONSTATUS.PENDINGAPPROVAL:
                                {
                                    // Todo:  Add reminder code or create the appropriate escalation as required.
                                    break;
                                }

                            // Automation Processing for APPROVED Tickets.
                            case var value when value == AUTOMATIONSTATUS.APPROVED:
                                {
                                    //Call the ProvAccounts Web Service to add the office365-a3 value to it.
                                    provAccountsManager.EnableFacultyA3(this.TDXAutomationTicket.TicketRequestor.UserPrincipalName);

                                    // Check if the license has been assigned
                                    office365LicensingManager.GetAssignedSubscriptions(this.TDXAutomationTicket.TicketRequestor.UserPrincipalName);
                                    Office365LicensingManager.Office365Subscription office365Subscription = office365LicensingManager.Office365AssignedSubscriptions
                                        .Where(s => s.SKU.Equals("ENTERPRISEPACKPLUS_FACULTY"))
                                        .FirstOrDefault();

                                    // If the license has been successfully assigned then resolve the request.
                                    if (office365Subscription != null)
                                    {
                                        // Update the Automation Status and Automation Status Details.
                                        this.UpdateAutomationStatus(AUTOMATIONSTATUS.COMPLETE);
                                        AutomationDetails.AppendFormat(" , [{0}]: Office License Successfully Assigned.",
                                            DateTime.UtcNow.ToString());

                                        // Update the ticket and notify the customer.
                                        TicketComments.AppendFormat("The requested Office 365 License has been successfully assigned to your account. We are resolving this request.");
                                        this.NotifyCreator = true;
                                        this.NotifyRequestor = true;
                                        this.UpdateTicket(TicketComments, "Resolved");

                                        // Assign the resolved ticket to (L3).
                                        this.UpdateResponsibleGroup(45);
                                    }
                                    break;
                                }
                            // Automation Processing for COMPLETE Tickets.
                            case var value when value == AUTOMATIONSTATUS.COMPLETE:
                                {
                                    if (ticket.ResponsibleGroupID != 45)
                                    {
                                        // Escalate the ticket to (L3).
                                        this.UpdateResponsibleGroup(45);
                                    }
                                    break;
                                }
                            // Automation Processing for CANCELED Tickets.
                            case var value when value == AUTOMATIONSTATUS.CANCELED:
                                {
                                    if (ticket.ResponsibleGroupID != 45)
                                    {
                                        // Escalate the ticket to (L3).
                                        this.UpdateResponsibleGroup(45);
                                    }
                                    break;
                                }
                            // Automation Processing for DECLINED Tickets.
                            case var value when value == AUTOMATIONSTATUS.DECLINED:
                                {
                                    if(ticket.ResponsibleGroupID != 45)
                                    {
                                        // Escalate the ticket to (L3).
                                        this.UpdateResponsibleGroup(45);
                                    }

                                    break;
                                }
                            default:
                                break;

                        }
                        // Update the Automation Status Details [TDX Custom Attribute: (S111-AUTOMATIONSTATUSDETAILS)]
                        this.UpdateAutomationStatusDetails(AutomationDetails);
                        String logfile = String.Format(".\\LogFiles\\{0}_FacultyA3LicensingAutomation.log", DateTime.UtcNow.ToString("yyyyMMdd_hh"));
                        using (StreamWriter streamWriter = new StreamWriter(logfile, true))
                        {
                            streamWriter.WriteLine("\n[{0}] Processing Office 365 A3 License Request For: {1}", DateTime.UtcNow.ToString(), this.TDXAutomationTicket.TicketRequestor.UserPrincipalName);
                            streamWriter.WriteLine("TDX Request: {0}", this.TDXAutomationTicket.ID);
                            streamWriter.WriteLine(AutomationDetails.ToString());
                        }
                    }
                }
            }
            stopwatch.Stop();
        }
    }
}