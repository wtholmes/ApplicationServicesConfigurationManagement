using CornellIdentityManagement;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using TeamDynamix.Api.Tickets;
using TeamDynamix.Api.Users;

namespace TDXManager
{
    public class RequestGoogleWorkspaceAccount : TDXTicketManager
    {
        public RequestGoogleWorkspaceAccount()
        {
            // Start a ProvAccounts Manager.
            ProvAccountsManager provAccountsManager = new ProvAccountsManager();

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            // Inactive Ticket Statuses
            Regex InactiveTicketsRegex = new Regex(@"(Reopened|Resolved|Closed|Canceled)", RegexOptions.IgnoreCase);

            // ------
            // Get the list of tickets from TDX using the Automated  Request Google Workspace Account.
            // This report returns all of the tickets that are using the:
            //  Request Google Workspace Account TDX Form.
            // ------
            GetTicketsUsingReport("* Email and Calendar / Request Google Workspace Account", InactiveTicketsRegex);

            // Process the tickets.
            foreach (Ticket ticket in this.TDXTickets)
            {
                try
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
                                        StringBuilder RequestTitle = new StringBuilder("Google Workspace Account Request for:");
                                        RequestTitle.AppendFormat(" {0}", this.TDXAutomationTicket.TicketRequestor.UserPrincipalName);

                                        // Setup the request Description.
                                        StringBuilder RequestDescription = new StringBuilder();
                                        RequestDescription.Append("You have requested a new Google Workspace Account. Your request is being processed.");

                                        // Update the Ticket Title and Description.
                                        this.UpdateTicketTitleAndDescription(RequestTitle, RequestDescription);

                                        // Update the Automation Status and Automation Status Details.
                                        this.UpdateAutomationStatus(AUTOMATIONSTATUS.INPROCESS);

                                        // Update the ticket and notify the customer.
                                        TicketComments.AppendFormat("We have received your request for a new Google Workspace account. Your request is in process.");
                                        this.NotifyCreator = true;
                                        this.NotifyRequestor = true;
                                        // If there is an alternate address specified in the ticket make sure they get notified.
                                        if (this.TDXAutomationTicket.AlternateEmailAddress != null)
                                        {
                                            this.NotificationEmails.Add(this.TDXAutomationTicket.AlternateEmailAddress);
                                        }
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
                                            AutomationDetails.AppendFormat(" , [{0}]: The creator {1} is not allowed to request a Google Workspace Account on behalf of. The request has been cancelled.",
                                                DateTime.UtcNow.ToString(),
                                                this.TDXAutomationTicket.TicketCreator.UserPrincipalName);

                                            // Update the ticket and notify the customer.
                                            TicketComments.AppendFormat("{0} {1} is not authorized to request a Google Workspace Account on your behalf. No changes have been made to your account.",
                                                this.TDXAutomationTicket.TicketCreator.DisplayName,
                                                this.TDXAutomationTicket.TicketCreator.UserPrincipalName);

                                            this.NotifyCreator = true;
                                            this.NotifyRequestor = true;
                                            this.UpdateTicket(TicketComments, "Cancelled");
                                        }
                                        // Does the target already have an a Google Workspace Account.
                                        if (RequestAllowed)
                                        {
                                            if (this.TDXAutomationTicket.TicketRequestor.ProvAccts.Contains("gsuite")) // Requester already has a Google Workspace account.
                                            {
                                                // Disallow the request
                                                RequestAllowed = false;

                                                // Assign the cancelled request to L3
                                                this.UpdateResponsibleGroup(45);

                                                // Update the Automation Status and Automation Status Details.
                                                this.UpdateAutomationStatus(AUTOMATIONSTATUS.CANCELED);
                                                AutomationDetails.AppendFormat(" , [{0}]: The requester already has an Google Workspace Account. Their affiliation is: {1}. This request has been cancelled.",
                                                    DateTime.UtcNow.ToString(),
                                                    this.TDXAutomationTicket.TicketRequestor.PrimaryAffiliation);

                                                // Call the ProvAccounts Web Service to remove the norouting flag.  This may be set for some people.
                                                provAccountsManager.EnableMailRouting(this.TDXAutomationTicket.TicketRequestor.UserPrincipalName);


                                                // Update the ticket and notify the customer.
                                                TicketComments.AppendFormat("Your Google Workspace Account has already been provisioned. No changes have been made to your account.");
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

                                        // Is the requester entitled to a Google Workspace Account
                                        if (RequestAllowed)
                                        {
                                            if (this.TDXAutomationTicket.TicketRequestor.Entitlements.Contains("gsuite"))
                                            {
                                                RequestAllowed = true;
                                            }
                                            // Requester's affiliation does not included a Google Workspace Account.
                                            else
                                            {
                                                // Disallow the request.
                                                RequestAllowed = false;

                                                // Assign the cancelled request to L3
                                                this.UpdateResponsibleGroup(45);

                                                // Update the Automation Status and Automation Status Details.
                                                this.UpdateAutomationStatus(AUTOMATIONSTATUS.CANCELED);
                                                AutomationDetails.AppendFormat(" , [{0}]: The requester does not qualify for a Google Workspace Account. Their affiliation is: {1}. The request has been cancelled.",
                                                    DateTime.UtcNow.ToString(),
                                                    this.TDXAutomationTicket.TicketRequestor.PrimaryAffiliation);

                                                // Update the ticket and notify the customer.
                                                TicketComments.AppendFormat("Your Cornell affiliation does not include a Google Workspace Account. No changes have been made to your account.");
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


                                        // This is a valid request so we can provision a Google Workspace Account for the person.
                                        if (RequestAllowed)
                                        {
                                            // Update the Automation Status and Automation Status Details.
                                            this.UpdateAutomationStatus(AUTOMATIONSTATUS.APPROVED);
                                            AutomationDetails.AppendFormat(" , [{0}]: A Google Workspace Account is being provisioned.",
                                                DateTime.UtcNow.ToString());

                                            // Update the ticket and notify the customer.
                                            TicketComments.AppendFormat("Your Google Workspace account request has been approved. We will notify you when your account has been created.");
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
                                        //Call the ProvAccounts Web Service to add the gsuite (Google Workspace) value.
                                        provAccountsManager.EnableGoogleWorkspaceAccount(this.TDXAutomationTicket.TicketRequestor.UserPrincipalName);
                                        
                                        // Call the ProvAccounts Web Service to remove the norouting flag.  This may be set for some people.
                                        provAccountsManager.EnableMailRouting(this.TDXAutomationTicket.TicketRequestor.UserPrincipalName);

                                        // Assign the resolved ticket to (L3).
                                        this.UpdateResponsibleGroup(45);

                                        // Update the Automation Status and Automation Status Details.
                                        this.UpdateAutomationStatus(AUTOMATIONSTATUS.COMPLETE);
                                        AutomationDetails.AppendFormat(" , [{0}]: The Google Workspace Account has been successfully provisioned.", DateTime.UtcNow.ToString());

                                        // Update the ticket and notify the customer.
                                        //TicketComments.AppendFormat("Your Google Workspace account has been created. To login, please go to https://mail.google.com/ This request is complete.");
                                        TicketComments.AppendFormat("A new Google Workspace account has been created for you, which will soon be available to you at https://mail.google.com . It can take up to 24 hours for your new account to be available in Google. Please be patient. This is your final message.");
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
                            String logfile = String.Format(".\\LogFiles\\{0}_GoogleWorspaceAccountAutomation.log", DateTime.UtcNow.ToString("yyyyMMdd_hh"));
                            using (StreamWriter streamWriter = new StreamWriter(logfile, true))
                            {
                                streamWriter.WriteLine("\n[{0}] Processing Google Workspace Account Request For: {1}", DateTime.UtcNow.ToString(), this.TDXAutomationTicket.TicketRequestor.UserPrincipalName);
                                streamWriter.WriteLine("TDX Request: {0}", this.TDXAutomationTicket.ID);
                                streamWriter.WriteLine(AutomationDetails.ToString());
                            }
                        }
                    }
                }
                catch(Exception exp)
                {
                   
                }
            }
            stopwatch.Stop();
        }
    }
}

