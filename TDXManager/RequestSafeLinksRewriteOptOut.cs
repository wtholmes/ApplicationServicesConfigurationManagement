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
using PowerShellRunspaceManager;

namespace TDXManager
{
    public class RequestSafeLinksRewriteOptOut : TDXTicketManager
    {
        public RequestSafeLinksRewriteOptOut()
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            // Start an Exchange On-Premises Manager.
            Console.WriteLine("Starting Exchange Manager");
            ExchangeOnPremManager exchangeOnPremManager = new ExchangeOnPremManager("sf-ex-2019-01.exchange.cornell.edu", true);
          
            // Inactive Ticket Statuses
            Regex InactiveTicketsRegex = new Regex(@"(Reopened|Resolved|Closed|Canceled)", RegexOptions.IgnoreCase);

            // ------
            // Get the list of tickets from TDX using the Automated   Opt Out of Safe Links Rewrite report.
            // This report returns all of the tickets that are using the:
            // Request Opt Out of Safe Links Rewrite TDX Form.
            // ------
            Console.WriteLine("Getting Tickets");
            GetTicketsUsingReport("* Email and Calendar /  Opt Out of Safe Links Rewrite", InactiveTicketsRegex);

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

                            Boolean LogRequestUpdates = false;

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
                                        StringBuilder RequestTitle = new StringBuilder("Office 365 SafeLinks Rewrite Opt-Out For:");
                                        RequestTitle.AppendFormat(" {0}", this.TDXAutomationTicket.TicketRequestor.UserPrincipalName);

                                        // Setup the request Description.
                                        StringBuilder RequestDescription = new StringBuilder();
                                        RequestDescription.Append("You have requested to be opted out of Office 365 Safe Links Rewriting. This request is subject to approval by the IT Security Office.");

                                        // Update the Ticket Title and Description.
                                        this.UpdateTicketTitleAndDescription(RequestTitle, RequestDescription);

                                        // Update the Automation Status and Automation Status Details.
                                        this.UpdateAutomationStatus(AUTOMATIONSTATUS.INPROCESS);

                                        // Update the ticket and notify the customer.
                                        TicketComments.AppendFormat("We have received your request to be opted out of Office 365 Safe Links Rewriting. Your request is in process.");
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
                                        LogRequestUpdates = true;
                                        break;
                                    }
                                // Automation Processing for INPROCESS Tickets.
                                case var value when value == AUTOMATIONSTATUS.INPROCESS:
                                    {
                                        Boolean RequestAllowed = false;

                                        // Is the creator of this ticket equal to the requester (Target).
                                        if (this.TDXAutomationTicket.TicketCreator.UserPrincipalName == this.TDXAutomationTicket.TicketRequestor.UserPrincipalName)
                                        {
                                            AutomationDetails.AppendFormat(" , [{0}]: The requester is the creator: This request is allowed.", DateTime.UtcNow.ToString());
                                            RequestAllowed = true;
                                            LogRequestUpdates = true;
                                        }
                                        else
                                        {
                                            // Disallow the request.
                                            RequestAllowed = false;

                                            // Assign the cancelled request to L3
                                            this.UpdateResponsibleGroup(45);

                                            // Update the Automation Status and Automation Status Details.
                                            this.UpdateAutomationStatus(AUTOMATIONSTATUS.DECLINED);
                                            AutomationDetails.AppendFormat(" , [{0}]: The creator {1} is not allowed to request a Safe Links Opt-Out on behalf of. The request is not allowed has been cancelled.",
                                                DateTime.UtcNow.ToString(),
                                                this.TDXAutomationTicket.TicketCreator.UserPrincipalName);
                                            LogRequestUpdates = true;

                                            // Update the ticket and notify the customer.
                                            TicketComments.AppendFormat("{0} {1} is not authorized to request Office 365 Safe Links Opt-Out on your behalf. You must make these requests directly. No changes have been made to your account.",
                                                this.TDXAutomationTicket.TicketCreator.DisplayName,
                                                this.TDXAutomationTicket.TicketCreator.UserPrincipalName);

                                            this.NotifyCreator = true;
                                            this.NotifyRequestor = true;
                                            this.UpdateTicket(TicketComments, "Cancelled");
                                        }

                                        // If this is a valid request we can enable the grace period.
                                        if (RequestAllowed)
                                        {
                                            // Update the Automation Status and Automation Status Details.
                                            this.UpdateAutomationStatus(AUTOMATIONSTATUS.PENDINGAPPROVAL);
                                            AutomationDetails.AppendFormat(" , [{0}]: The Approve Opt-Out of Microsoft Safe Links WorkFlow has been assigned to the IT Security Office.",
                                                DateTime.UtcNow.ToString());
                                            LogRequestUpdates = true;
                                            this.SetTicketWorkflow(951001);
                                        }
                                        else
                                        {
                                            // Update the Automation Status and Automation Status Details.
                                            this.UpdateAutomationStatus(AUTOMATIONSTATUS.DECLINED);
                                            AutomationDetails.AppendFormat(" , [{0}]: The request to Opt-Out of Office 365 Safe Links is not allowed. The request has been declined.",
                                                DateTime.UtcNow.ToString());
                                            LogRequestUpdates = true;
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
                                        AutomationDetails.AppendFormat(" , [{0}]: The Office 365 Safe Links Rewrite Opt-Out has been approved.", DateTime.UtcNow.ToString(), this.TDXAutomationTicket.TicketRequestor.UserPrincipalName);
                                        LogRequestUpdates = true;
                                        // Update the ticket and notify the customer.
                                        TicketComments.AppendFormat("Your Office 365 Safe Links Opt-Out Request has been approved. It will take up to one hour for the process to complete.");
                                        this.NotifyCreator = true;
                                        this.NotifyRequestor = true;

                                        //Add the person to the Opt-Out Group. (CIT-M365-SafeLinks-OptOutRewrite - This is an on-premises distribution group.)
                                        exchangeOnPremManager.AddDistributionGroupMember
                                            ("CIT-M365-SafeLinks-OptOutRewrite",
                                            this.TDXAutomationTicket.TicketRequestor.UserPrincipalName
                                            );


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
                                        AutomationDetails.AppendFormat(" , [{0}]: The request to Opt-Out of Office 365 SafeLinks Rewriting as been declined. The request has been Resolved.", DateTime.UtcNow.ToString(), this.TDXAutomationTicket.TicketRequestor.UserPrincipalName);
                                        LogRequestUpdates = true;
                                        // Update the ticket and notify the customer.
                                        TicketComments.AppendFormat("Your request to Opt-Out of Office 365 SafeLinks rewriting as been declined.");
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
                            if (LogRequestUpdates)
                            {
                                this.UpdateAutomationStatusDetails(AutomationDetails);
                                String logfile = String.Format(".\\LogFiles\\{0}_SafeLinksOptOut.log", DateTime.UtcNow.ToString("yyyyMMdd_hh"));
                                using (StreamWriter streamWriter = new StreamWriter(logfile, true))
                                {
                                    streamWriter.WriteLine("\n[{0}] Processing Office 365 Safe Links Opt-Out Request For: {1}", DateTime.UtcNow.ToString(), this.TDXAutomationTicket.TicketRequestor.UserPrincipalName);
                                    streamWriter.WriteLine("TDX Request: {0}", this.TDXAutomationTicket.ID);
                                    streamWriter.WriteLine(AutomationDetails.ToString());
                                }
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