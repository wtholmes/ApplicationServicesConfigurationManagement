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
    public class RequestNomailroutingTDXService : TDXTicketManager
    {
        public RequestNomailroutingTDXService()
        {
            // Start a ProvAccounts Manager.
            ProvAccountsManager provAccountsManager = new ProvAccountsManager();

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            // Inactive Ticket Statuses
            Regex InactiveTicketsRegex = new Regex(@"(Reopened|Resolved|Closed|Canceled)", RegexOptions.IgnoreCase);

            // ------
            // Get the list of tickets from TDX using the Automated Request Alumni Disable Cornell Email report.
            // This report returns all of the tickets that are using the:
            // Request Alumni Disable Cornell Email Form.
            // ------
            GetTicketsUsingReport("* Email and Calendar / Disable Cornell Email Account", InactiveTicketsRegex);

            // Process the Requests
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
                                    StringBuilder RequestTitle = new StringBuilder("Disable Email Routing For:");
                                    RequestTitle.AppendFormat(" {0}", this.TDXAutomationTicket.TicketRequestor.UserPrincipalName);

                                    // Setup the request Description.
                                    StringBuilder RequestDescription = new StringBuilder();
                                    RequestDescription.Append("You have requested that your Cornell Email routing should be disabled. Your request is being processed.");

                                    // Update the Ticket Title and Description.
                                    this.UpdateTicketTitleAndDescription(RequestTitle, RequestDescription);

                                    // Update the Automation Status and Automation Status Details.
                                    this.UpdateAutomationStatus(AUTOMATIONSTATUS.INPROCESS);

                                    // Update the ticket and notify the customer.
                                    TicketComments.AppendFormat("We have received your request to disable your Cornell Email. Your request is in process.");
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

                                    if (this.TDXAutomationTicket.TicketRequestor.MailDelivery.Contains("norouting")) // Requester has already disable their routing.
                                    {
                                        // Disallow the request
                                        RequestAllowed = false;

                                        // Assign the cancelled request to L3
                                        this.UpdateResponsibleGroup(45);

                                        // Update the Automation Status and Automation Status Details.
                                        this.UpdateAutomationStatus(AUTOMATIONSTATUS.CANCELED);
                                        AutomationDetails.AppendFormat(" , [{0}]: The requester has already disabled their routing. Their affiliation is: {1}. This request has been cancelled.",
                                            DateTime.UtcNow.ToString(),
                                            this.TDXAutomationTicket.TicketRequestor.PrimaryAffiliation);

                                        // Update the ticket and notify the customer.
                                        TicketComments.AppendFormat("Your Cornell Email Routing has already been disabled. No changes have been made to your account.");
                                        this.NotifyCreator = true;
                                        this.NotifyRequestor = true;

                                        // If there is an alternate address specified in the ticket make sure they get notified.
                                        if (this.TDXAutomationTicket.AlternateEmailAddress != null)
                                        {
                                            this.NotificationEmails.Add(this.TDXAutomationTicket.AlternateEmailAddress);
                                        }

                                        this.UpdateTicket(TicketComments, "Cancelled");

                                    }
                                    // Is the requestor entitled to no mail routing.
                                    if (RequestAllowed)
                                    {
                                        if (this.TDXAutomationTicket.TicketRequestor.Entitlements.Contains("norouting"))
                                        { 
                                            List<String> allowedAffiliations = new List<String>() { "alumni", "retiree" };
                                            if(allowedAffiliations.Contains(this.TDXAutomationTicket.TicketRequestor.PrimaryAffiliation))
                                            {
                                                RequestAllowed = true;
                                            }
                                            else
                                            {
                                                RequestAllowed = false;

                                                // Assign the cancelled request to L3
                                                this.UpdateResponsibleGroup(45);

                                                // Update the Automation Status and Automation Status Details.
                                                this.UpdateAutomationStatus(AUTOMATIONSTATUS.CANCELED);
                                                AutomationDetails.AppendFormat(" , [{0}]: The requester is not permitted to disable mail routing. Their affiliation is: {1}. The request has been cancelled.",
                                                    DateTime.UtcNow.ToString(),
                                                    this.TDXAutomationTicket.TicketRequestor.PrimaryAffiliation);

                                                // Update the ticket and notify the customer.
                                                TicketComments.AppendFormat("Your Cornell affiliation does not allow you to disable mail routing. No changes have been made to your account.");
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
                                        // Requester's affiliation does not allow no mail routing..
                                        else
                                        {
                                            // Disallow the request.
                                            RequestAllowed = false;

                                            // Assign the cancelled request to L3
                                            this.UpdateResponsibleGroup(45);

                                            // Update the Automation Status and Automation Status Details.
                                            this.UpdateAutomationStatus(AUTOMATIONSTATUS.CANCELED);
                                            AutomationDetails.AppendFormat(" , [{0}]: The requester is not permitted to disable mail routing. Their affiliation is: {1}. The request has been cancelled.",
                                                DateTime.UtcNow.ToString(),
                                                this.TDXAutomationTicket.TicketRequestor.PrimaryAffiliation);

                                            // Update the ticket and notify the customer.
                                            TicketComments.AppendFormat("Your Cornell affiliation does not allow you to disable mail routing. No changes have been made to your account.");
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


                                    // This is a valid request so we can assign the ENTERPRISEPACKPLUS_FACULTY (A3) to the customer.
                                    if (RequestAllowed)
                                    {
                                        // Update the Automation Status and Automation Status Details.
                                        this.UpdateAutomationStatus(AUTOMATIONSTATUS.APPROVED);
                                        AutomationDetails.AppendFormat(" , [{0}]: Email Routing will be disabled..",
                                            DateTime.UtcNow.ToString());

                                        // Update the ticket and notify the customer.
                                        TicketComments.AppendFormat("Your request to disable your Cornell Email has been approved. We will notify you when this has been completed, if you have provided us with an alternate email address.");
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
                                    // To-do:  Add reminder code or create the appropriate escalation as required.
                                    break;
                                }

                            // Automation Processing for APPROVED Tickets.
                            case var value when value == AUTOMATIONSTATUS.APPROVED:
                                {
                                    // Call the ProvAccounts Web Service to remove Google Workspace.
                                    // ------
                                    // Note: This should be uncommented once Google Workspace de-provisioning is in place.
                                    // ------
                                    //provAccountsManager.DisableGoogleWorkspaceAccount(this.TDXAutomationTicket.TicketRequestor.UserPrincipalName);

                                    // Call the ProvAccounts Web Service to remove mail routing.
                                    // ------
                                    // Note:  This implicitly removes the Office 365 Mailbox or MailUser.  The removal is handled
                                    //        by the Exchange Email Account Provisioning process.  When this process finds the 
                                    //        norouting value in maildelivery, it updates the on-premises recipient type to a User.
                                    //        This is not ideal.  It would make more sense to have an entitlement value for a
                                    //        MailUser or base it on the existence of a Google Workspace Account.  However this 
                                    //        Another consideration would be use cases for MailUsers that route mail to something
                                    //        other than Google Workspace.  This would be ideal for forwarding only scenarios for 
                                    //        alumni or other users such as retirees or former post docs. (06/18/2022).
                                    // ------          
                                    provAccountsManager.DisableMailRouting(this.TDXAutomationTicket.TicketRequestor.UserPrincipalName);

                                    // Assign the resolved ticket to (L3).
                                    this.UpdateResponsibleGroup(45);

                                    // Update the Automation Status and Automation Status Details.
                                    this.UpdateAutomationStatus(AUTOMATIONSTATUS.COMPLETE);
                                    AutomationDetails.AppendFormat(" , [{0}]: Email routing to this account has been disabled.", DateTime.UtcNow.ToString());

                                    // Update the ticket and notify the customer.
                                    TicketComments.AppendFormat("Your Cornell Email has been disabled and your Cornell mailbox has been deleted. You will no longer receive email addressed to: {0}. Senders will receive a non-delivery message if they send a message to this address.", this.TDXAutomationTicket.TicketRequestor.UserPrincipalName);
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
                        String logfile = String.Format(".\\LogFiles\\{0}_DisableMailRouting.log", DateTime.UtcNow.ToString("yyyyMMdd_hh"));
                        using (StreamWriter streamWriter = new StreamWriter(logfile, true))
                        {
                            streamWriter.WriteLine("\n[{0}] Processing Disable Mail Routing Request for: {1}", DateTime.UtcNow.ToString(), this.TDXAutomationTicket.TicketRequestor.UserPrincipalName);
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

