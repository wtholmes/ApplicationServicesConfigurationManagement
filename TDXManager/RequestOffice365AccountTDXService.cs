using CornellIdentityManagement;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using TeamDynamix.Api.Tickets;

namespace TDXManager
{
    public class RequestOffice365AccountTDXService : TDXTicketManager
    {
        public RequestOffice365AccountTDXService()
        {
            // Start a ProvAccounts Manager.
            ProvAccountsManager provAccountsManager = new ProvAccountsManager();

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            // Inactive Ticket Statuses
            Regex InactiveTicketsRegex = new Regex(@"(Reopened|Resolved|Closed|Canceled)", RegexOptions.IgnoreCase);

            // ------
            // Get the list of tickets from TDX using the Automated Request Alumni Office 365 Account Requests report.
            // This report returns all of the tickets that are using the:
            //Request Alumni Office 365 Account form.
            // ------
            GetTicketsUsingReport("* Email and Calendar / Request Alumni Office 365 Account", InactiveTicketsRegex);

            // Process the Request Tickets;
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
                                    if (this.TDXAutomationTicket.TicketRequestor != null)
                                    {
                                        // Setup the Request Title.
                                        StringBuilder RequestTitle = new StringBuilder("Office 365 Exchange Account Request for:");
                                        RequestTitle.AppendFormat(" {0}", this.TDXAutomationTicket.TicketRequestor.UserPrincipalName);

                                        // Setup the request Description.
                                        StringBuilder RequestDescription = new StringBuilder();
                                        RequestDescription.Append("You have requested a new Office 365 Exchange Account. Your request is being processed.");

                                        // Update the Ticket Title and Description.
                                        this.UpdateTicketTitleAndDescription(RequestTitle, RequestDescription);

                                        // Update the Automation Status and Automation Status Details.
                                        this.UpdateAutomationStatus(AUTOMATIONSTATUS.INPROCESS);

                                        // Update the ticket and notify the customer.
                                        TicketComments.AppendFormat("We have received your request for a new Office 365 Exchange account. Your request is in process.");
                                        this.NotifyCreator = true;
                                        this.NotifyRequestor = true;
                                        // If there is an alternate address specified in the ticket make sure they get notified.
                                        if (this.TDXAutomationTicket.AlternateEmailAddress != null)
                                        {
                                            this.NotificationEmails.Add(this.TDXAutomationTicket.AlternateEmailAddress);
                                        }
                                        this.UpdateTicket(TicketComments, "In Process");
                                    }
                                    else
                                    {
                                        continue;
                                    }
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
                                    Boolean RequestAllowed = false;

                                    // Is the creator of this ticket equal to the requester (Target).
                                    if (this.TDXAutomationTicket.TicketCreator.UserPrincipalName == this.TDXAutomationTicket.TicketRequestor.UserPrincipalName)
                                    {
                                        AutomationDetails.AppendFormat(" , [{0}]: The requester is the creator.   ", DateTime.UtcNow.ToString());
                                        RequestAllowed = true;
                                    }
                                    else
                                    {
                                        // Disallow the request.
                                        RequestAllowed = false;

                                        // Assign the cancelled request to L3
                                        this.UpdateResponsibleGroup(45);

                                        // Update the Automation Status and Automation Status Details.
                                        this.UpdateAutomationStatus(AUTOMATIONSTATUS.DECLINED);
                                        AutomationDetails.AppendFormat(" , [{0}]: The creator {1} is not allowed to request an Office 365 Account on behalf of. The request has been cancelled.",
                                            DateTime.UtcNow.ToString(),
                                            this.TDXAutomationTicket.TicketCreator.UserPrincipalName);

                                        // Update the ticket and notify the customer.
                                        TicketComments.AppendFormat("{0} {1} is not authorized to request an Office 365 Account on your behalf. No changes have been made to your account.",
                                            this.TDXAutomationTicket.TicketCreator.DisplayName,
                                            this.TDXAutomationTicket.TicketCreator.UserPrincipalName);

                                        this.NotifyCreator = true;
                                        this.NotifyRequestor = true;
                                        this.UpdateTicket(TicketComments, "Cancelled");
                                    }
                                    // Does the target already have an Office 365 Exchange Account.
                                    if (RequestAllowed)
                                    {
                                        if (this.TDXAutomationTicket.TicketRequestor.ProvAccts.Contains("exchange")) // Requester already has an exchange account.
                                        {
                                            // Disallow the request
                                            RequestAllowed = false;

                                            // Assign the cancelled request to L3
                                            this.UpdateResponsibleGroup(45);

                                            // Update the Automation Status and Automation Status Details.
                                            this.UpdateAutomationStatus(AUTOMATIONSTATUS.CANCELED);
                                            AutomationDetails.AppendFormat(" , [{0}]: The requester already has an Office 365 Exchange Account. Their affiliation is: {1}. This request has been cancelled.",
                                                DateTime.UtcNow.ToString(),
                                                this.TDXAutomationTicket.TicketRequestor.PrimaryAffiliation);

                                            // Update the ticket and notify the customer.
                                            TicketComments.AppendLine("You already have an Office 365 Exchange account. No changes have been made to this existing account.\n");
                                            TicketComments.AppendLine("To login into Exchange please go to: https://outlook.office.com/ \n");
                                            TicketComments.AppendLine("Here's some information about migrating email to your existing Exchange account:\n");
                                            TicketComments.AppendLine("https://it.cornell.edu/gsuite-gsuite-student-file-storage-storage-program/move-your-personal-data \n");
                                            TicketComments.AppendLine("Here is some more information about alumni email... https://alumni.cornell.edu/services/alumni-email-services/ \n");
                                            TicketComments.AppendLine("Do you still have questions? You may reply to this email, and someone will get back to you shortly.\n");
                                            this.NotifyCreator = true;
                                            this.NotifyRequestor = true;

                                            // If there is an alternate address specified in the ticket make sure they get notified.
                                            if (this.TDXAutomationTicket.AlternateEmailAddress != null)
                                            {
                                                this.NotificationEmails.Add(this.TDXAutomationTicket.AlternateEmailAddress);
                                            }

                                            this.UpdateTicket(TicketComments, "Resolved");
                                        }
                                    }

                                    // Is the requester entitled to an Office 365 Exchange Account?
                                    if (RequestAllowed)
                                    {
                                        if (this.TDXAutomationTicket.TicketRequestor.Entitlements.Contains("exchange"))
                                        {
                                            RequestAllowed = true;
                                        }
                                        // Requester's affiliation does not include an Office 365 Exchange Account.
                                        else
                                        {
                                            // Disallow the request.
                                            RequestAllowed = false;

                                            // Assign the cancelled request to L3
                                            this.UpdateResponsibleGroup(45);

                                            // Update the Automation Status and Automation Status Details.
                                            this.UpdateAutomationStatus(AUTOMATIONSTATUS.CANCELED);
                                            AutomationDetails.AppendFormat(" , [{0}]: The requester does not qualify for Office 365 Exchange Account. Their affiliation is: {1}. The request has been cancelled.",
                                                DateTime.UtcNow.ToString(),
                                                this.TDXAutomationTicket.TicketRequestor.PrimaryAffiliation);

                                            // Update the ticket and notify the customer.
                                            TicketComments.AppendFormat("Your Cornell affiliation does not include an Office 365 Exchange Account. No changes have been made to your account.");
                                            this.NotifyCreator = true;
                                            this.NotifyRequestor = true;

                                            // If there is an alternate address specified in the ticket make sure they get notified.
                                            if (this.TDXAutomationTicket.AlternateEmailAddress != null)
                                            {
                                                this.NotificationEmails.Add(this.TDXAutomationTicket.AlternateEmailAddress);
                                            }

                                            this.UpdateTicket(TicketComments, "Cancelled");
                                        }
                                    }

                                    // This is a valid request so we can assign create an Office 365 Exchange account for the customer.
                                    if (RequestAllowed)
                                    {
                                        // Update the Automation Status and Automation Status Details.
                                        this.UpdateAutomationStatus(AUTOMATIONSTATUS.APPROVED);
                                        AutomationDetails.AppendFormat(" , [{0}]: An Office 365 Exchange Account is being provisioned.",
                                            DateTime.UtcNow.ToString());

                                        // Update the ticket and notify the customer.
                                        TicketComments.AppendFormat("Your Office 365 Exchange account request has been approved. We will notify you when your account has been created.");
                                        this.NotifyCreator = true;
                                        this.NotifyRequestor = true;

                                        // If there is an alternate address specified in the ticket make sure they get notified.
                                        if (this.TDXAutomationTicket.AlternateEmailAddress != null)
                                        {
                                            this.NotificationEmails.Add(this.TDXAutomationTicket.AlternateEmailAddress);
                                        }

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
                                    //Call the ProvAccounts Web Service to remove the "norouting" value for mail delivery if that has been set.
                                    provAccountsManager.EnableMailRouting(this.TDXAutomationTicket.TicketRequestor.UserPrincipalName);

                                    //Call the ProvAccounts Web Service to add the exchange value to it.
                                    provAccountsManager.EnableOffice365Exchange(this.TDXAutomationTicket.TicketRequestor.UserPrincipalName);

                                    // Assign the resolved ticket to (L3).
                                    this.UpdateResponsibleGroup(45);

                                    // Update the Automation Status and Automation Status Details.
                                    this.UpdateAutomationStatus(AUTOMATIONSTATUS.COMPLETE);
                                    AutomationDetails.AppendFormat(" , [{0}]: Office 365 Account has been successfully provisioned.", DateTime.UtcNow.ToString());

                                    // Update the ticket and notify the customer.
                                    TicketComments.AppendLine("Your Office365 Exchange account has been created.\n");
                                    TicketComments.AppendLine("To use this account, you must set up DUO first https://it.cornell.edu/twostep/two-step-login-setup-guide if you don't already have DUO set up.\n");
                                    TicketComments.AppendLine("To login into Exchange please go to: https://outlook.office.com/ \n");
                                    TicketComments.AppendLine("Here's some information about migrating email to your new Exchange account:\n");
                                    TicketComments.AppendLine("https://it.cornell.edu/gsuite-gsuite-student-file-storage-storage-program/move-your-personal-data \n");
                                    TicketComments.AppendLine("Here is some more information about alumni email... https://alumni.cornell.edu/services/alumni-email-services/ \n");
                                    TicketComments.AppendLine("By the way, your email is now being delivered to your new Exchange account.  If you also want email to be delivered to your Google Workspace account, here are some instructions... https://it.cornell.edu/gsuite-gsuite-student-outlook-web/forwarding-email-office-365-cornell-google-workspace \n");
                                    TicketComments.AppendLine("Do you still have questions? You may reply to this email, and someone will get back to you shortly.\n");
                                    TicketComments.AppendLine("This request is complete");
                                    this.NotifyCreator = true;
                                    this.NotifyRequestor = true;

                                    // If there is an alternate address specified in the ticket make sure they get notified.
                                    if (this.TDXAutomationTicket.AlternateEmailAddress != null)
                                    {
                                        this.NotificationEmails.Add(this.TDXAutomationTicket.AlternateEmailAddress);
                                    }

                                    this.UpdateTicket(TicketComments, "Resolved");

                                    break;
                                }
                            // Automation Processing for COMPLETE Tickets.
                            case var value when value == AUTOMATIONSTATUS.COMPLETE:
                                {
                                    // No actions required for this automation state.
                                    break;
                                }
                            // Automation Processing for CANCELED Tickets.
                            case var value when value == AUTOMATIONSTATUS.CANCELED:
                                {
                                    // No actions required for the automation state.
                                    break;
                                }
                            // Automation Processing for DECLINED Tickets.
                            case var value when value == AUTOMATIONSTATUS.DECLINED:
                                {
                                    // No Actions required for this automation state.
                                    break;
                                }
                            default:
                                break;
                        }
                        // Update the Automation Status Details [TDX Custom Attribute: (S111-AUTOMATIONSTATUSDETAILS)]
                        this.UpdateAutomationStatusDetails(AutomationDetails);
                        String logfile = String.Format(".\\LogFiles\\{0}_Office365ExchangeAccountAutomation.log", DateTime.UtcNow.ToString("yyyyMMdd_hh"));
                        using (StreamWriter streamWriter = new StreamWriter(logfile, true))
                        {
                            try
                            {
                                streamWriter.WriteLine("\n[{0}] Processing Office 365 Exchange Account Request For: {1}", DateTime.UtcNow.ToString(), this.TDXAutomationTicket.TicketRequestor.UserPrincipalName);
                                streamWriter.WriteLine("TDX Request: {0}", this.TDXAutomationTicket.ID);
                                streamWriter.WriteLine(AutomationDetails.ToString());
                            }
                            catch
                            {
                            }
                        }
                    }
                }
            }
            stopwatch.Stop();
        }
    }
}