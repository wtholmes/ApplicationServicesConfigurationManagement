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
    public class RequestGoogleWorkspaceQuotaGracePeriod : TDXTicketManager
    {
        public RequestGoogleWorkspaceQuotaGracePeriod()
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            // Inactive Ticket Statuses
            Regex InactiveTicketsRegex = new Regex(@"(Reopened|Resolved|Closed|Canceled)", RegexOptions.IgnoreCase);

            // ------
            // Get the list of tickets from TDX using the Automated  Request Google Workspace Account.
            // This report returns all of the tickets that are using the:
            //  Request Google Workspace Account TDX Form.
            // ------
            GetTicketsUsingReport("* Email and Calendar / Google Workspace Email Delivery Grace Period", InactiveTicketsRegex);

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
                                        if (this.TDXAutomationTicket.StatusName.Equals("On Hold"))
                                        {
                                            this.UpdateAutomationStatus(AUTOMATIONSTATUS.DECLINED);
                                        }
                                        else
                                        {
                                            // Setup the Request Title.
                                            StringBuilder RequestTitle = new StringBuilder("Google Workspace Quota Grace Period Request for:");
                                            RequestTitle.AppendFormat(" {0}", this.TDXAutomationTicket.TicketRequestor.UserPrincipalName);

                                            // Setup the request Description.
                                            StringBuilder RequestDescription = new StringBuilder();
                                            RequestDescription.Append("You have requested a Google Workspace Quota Grace Period.");

                                            // Update the Ticket Title and Description.
                                            this.UpdateTicketTitleAndDescription(RequestTitle, RequestDescription);

                                            // Update the Automation Status and Automation Status Details.
                                            this.UpdateAutomationStatus(AUTOMATIONSTATUS.INPROCESS);

                                            // Update the ticket and notify the customer.
                                            TicketComments.AppendFormat("We have received your request for a Google Workspace Quota Grace Period. Your request is in process.");
                                            this.NotifyCreator = true;
                                            this.NotifyRequestor = true;
                                            // If there is an alternate address specified in the ticket make sure they get notified.
                                            if (this.TDXAutomationTicket.AlternateEmailAddress != null)
                                            {
                                                this.NotificationEmails.Add(this.TDXAutomationTicket.AlternateEmailAddress);
                                            }
                                            this.UpdateTicket(TicketComments, "In Process");
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
                                            AutomationDetails.AppendFormat(" , [{0}]: The creator {1} is not allowed to request a Google Workspace Quota Grace Period on behalf of. The request has been cancelled.",
                                                DateTime.UtcNow.ToString(),
                                                this.TDXAutomationTicket.TicketCreator.UserPrincipalName);

                                            // Update the ticket and notify the customer.
                                            TicketComments.AppendFormat("{0} {1} is not authorized to request a Google Workspace Quota Grace Period on your behalf. You must make these requests directly. No changes have been made to your account.",
                                                this.TDXAutomationTicket.TicketCreator.DisplayName,
                                                this.TDXAutomationTicket.TicketCreator.UserPrincipalName);

                                            this.NotifyCreator = true;
                                            this.NotifyRequestor = true;
                                            this.UpdateTicket(TicketComments, "Cancelled");
                                        }
                                        if(this.TDXAutomationTicket.StatusName.Equals("On Hold"))
                                        {
                                            RequestAllowed = false;
                                        }
                                        // If this is a valid request we can enable the grace period.
                                        if (RequestAllowed)
                                        {
                                            // Update the Automation Status and Automation Status Details.
                                            this.UpdateAutomationStatus(AUTOMATIONSTATUS.APPROVED);
                                            AutomationDetails.AppendFormat(" , [{0}]: The requested Google Workspace Quota Grace Period is allowed and is being processed.",
                                                DateTime.UtcNow.ToString());


                                            //TODO:  Add Microsoft Graph Code Here to add this customer to the Grace Period Group.


                                        }
                                        else
                                        {
                                            // Update the Automation Status and Automation Status Details.
                                            this.UpdateAutomationStatus(AUTOMATIONSTATUS.DECLINED);
                                            AutomationDetails.AppendFormat(" , [{0}]: The requested Google Workspace Quota Grace Period is not allowed. The customer has exceeded the allowed number of grace period requests.",
                                                DateTime.UtcNow.ToString());
                                        }
                                        break;
                                    }

                                // Automation Processing for PENDINGAPPROVAL Tickets.
                                case var value when value == AUTOMATIONSTATUS.PENDINGAPPROVAL:
                                    {
                                        // TODO:  Add reminder code or create the appropriate escalation as required.
                                        break;
                                    }

                                // Automation Processing for APPROVED Tickets.
                                case var value when value == AUTOMATIONSTATUS.APPROVED:
                                    {
                                       
                                        // Assign the resolved ticket to (L3).
                                        this.UpdateResponsibleGroup(45);

                                        // Update the Automation Status and Automation Status Details.
                                        this.UpdateAutomationStatus(AUTOMATIONSTATUS.COMPLETE);
                                        AutomationDetails.AppendFormat(" , [{0}]: The Google Workspace Quota Grace Period has been applied by adding: {1} to the overquota-grace Azure Security Group.", DateTime.UtcNow.ToString(), this.TDXAutomationTicket.TicketRequestor.UserPrincipalName);

                                        // Update the ticket and notify the customer.
                                        TicketComments.AppendFormat("Your Google Workspace Quota Grace Period Request is being processed. It will take up to one hour for your Google Workspace Email Sending and/or Receiving to be re-enabled.");
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
                                        // Assign the resolved ticket to (L3).
                                        this.UpdateResponsibleGroup(45);

                                        // Update the Automation Status and Automation Status Details.
                                        this.UpdateAutomationStatus(AUTOMATIONSTATUS.COMPLETE);
                                        AutomationDetails.AppendFormat(" , [{0}]: The Google Workspace Quota Grace Period has bee declined: {1} has exceeded the allowed number of requests.", DateTime.UtcNow.ToString(), this.TDXAutomationTicket.TicketRequestor.UserPrincipalName);

                                        // Update the ticket and notify the customer.
                                        TicketComments.AppendFormat("Your Google Workspace Quota Grace Period Request has been rejected. You are only permitted one such request.");
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
                catch (Exception exp)
                {

                }
            }
            stopwatch.Stop();

        }
    }
}
