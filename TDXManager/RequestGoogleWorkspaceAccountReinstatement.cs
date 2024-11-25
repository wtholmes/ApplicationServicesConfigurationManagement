using GoogleWorkspaceManager;
using MicrosoftAzureManager;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TeamDynamix.Api.Forms;
using TeamDynamix.Api.Tickets;
using TeamDynamix.Api.Users;

namespace TDXManager
{
    public class RequestGoogleWorkspaceAccountReinstatement : TDXTicketManager
    {
        public RequestGoogleWorkspaceAccountReinstatement()
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            // Start a new Microsoft Graph Manager
            MicrosoftGraphManager microsoftGraphManager = new MicrosoftGraphManager();
            GoogleDirectoryManager googleDirectoryManager = new GoogleDirectoryManager(@"E:\GSuiteManagerCredentials\gsuitemanager-edit.json");

            // Inactive Ticket Statuses
            Regex InactiveTicketsRegex = new Regex(@"(Reopened|Resolved|Closed|Canceled)", RegexOptions.IgnoreCase);

            // ------
            // Get the list of tickets from TDX using the Automated  Request Google Workspace Account.
            // This report returns all of the tickets that are using the:
            //  Request Google Workspace Account TDX Form.
            // ------
            GetTicketsUsingReport("* Email and Calendar / Google Workspace Account Reinstatement Grace Period", InactiveTicketsRegex);

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
                                        StringBuilder RequestTitle = new StringBuilder("Google Workspace Account Reinstatement Request for:");
                                        RequestTitle.AppendFormat(" {0}", this.TDXAutomationTicket.TicketRequestor.UserPrincipalName);

                                        // Setup the request Description.
                                        StringBuilder RequestDescription = new StringBuilder();
                                        RequestDescription.Append("You have requested reinstatement of your Google Workspace Account.");

                                        // Update the Ticket Title and Description.
                                        this.UpdateTicketTitleAndDescription(RequestTitle, RequestDescription);

                                        // Update the Automation Status and Automation Status Details.
                                        this.UpdateAutomationStatus(AUTOMATIONSTATUS.INPROCESS);

                                        // Update the ticket and notify the customer.
                                        TicketComments.AppendFormat("We have received your request to reinstate your Google Workspace Account. If approved your account will be reinstated for a period of seven days.");
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
                                        // Is the creator of this ticket equal to the requester (Target).
                                        if (this.TDXAutomationTicket.TicketCreator.UserPrincipalName == this.TDXAutomationTicket.TicketRequestor.UserPrincipalName)
                                        {
                                            //AutomationDetails.AppendFormat(" , [{0}]: The requester is the creator.   ", DateTime.UtcNow.ToString());

                                            // Check if a request has already been completed for this user using this form.
                                            User requestingUser = this.GetTDXUserByUserPrincipalName(this.TDXAutomationTicket.TicketRequestor.UserPrincipalName);
                                            Form requestForm = this.TDXTicketForms.Where(f => f.Name.Equals("Email Accounts / Google Workspace Account Reinstatement Grace Period")).FirstOrDefault();
                                            this.GetAllRequestorTicketsByForm(new Guid[] { requestingUser.UID }, new Int32[] { requestForm.ID });
                                            List<Ticket> PreviouslyCompletedRequests = (from t in this.AllRequestorTickets
                                                                                        from attribute in t.Attributes
                                                                                        where attribute.Name.Equals("S111-AUTOMATIONSTATUS")
                                                                                        && attribute.ValueText.Equals("COMPLETE")
                                                                                        select ticket).ToList();

                                            if (PreviouslyCompletedRequests.Count.Equals(0))
                                            {
                                                // Check the current Google Directory OU for this customer
                                                GoogleWorkspaceUser googleWorkspaceUser = googleDirectoryManager.GetGoogleUser(this.TDXAutomationTicket.TicketRequestor.UserPrincipalName);
                                                if (googleWorkspaceUser != null)
                                                {
                                                    // Customer is in stage one or stage two deletion and can be restored.
                                                    if (Regex.IsMatch(googleWorkspaceUser.OrgUnitPath, @"(/PendingDeletion/Stage1|/PendingDeletion/Stage2)$", RegexOptions.IgnoreCase))
                                                    {
                                                        // Update the Automation Status and Automation Status Details.
                                                        this.UpdateAutomationStatus(AUTOMATIONSTATUS.APPROVED);
                                                        AutomationDetails.AppendFormat(" , [{0}]: The requested Google Workspace Account is eligible for reinstatement. Adding this user to the reinstatement Group",
                                                            DateTime.UtcNow.ToString());

                                                        //Add Microsoft Graph Code Here to add this customer to the Grace Period Group.
                                                        String GroupID = "90f484a5-6029-4460-902c-4f6c89f39dd8";
                                                        String MemberID = microsoftGraphManager.GetUser(this.TDXAutomationTicket.TicketCreator.UserPrincipalName);
                                                        microsoftGraphManager.AddGroupMember(GroupID, MemberID);
                                                    }
                                                    // Customer's Google Workspace Account has entered Stage 2 deletion and can no longer be reinstated. 
                                                    else if (Regex.IsMatch(googleWorkspaceUser.OrgUnitPath, @"/PendingDeletion/Stage2", RegexOptions.IgnoreCase))
                                                    {
                                                        this.UpdateAutomationStatus(AUTOMATIONSTATUS.CANCELED);
                                                        AutomationDetails.AppendFormat(" , [{0}]: {1} is no longer eligible for reinstatement. The request has been cancelled.",
                                                            DateTime.UtcNow.ToString(),
                                                            this.TDXAutomationTicket.TicketRequestor.UserPrincipalName);

                                                        // Update the ticket and notify the customer.
                                                        TicketComments.AppendFormat("Your Google Workspace Account is no longer eligible for reinstatement. This request has been cancelled",
                                                            this.TDXAutomationTicket.TicketRequestor.UserPrincipalName);

                                                        this.NotifyCreator = true;
                                                        this.NotifyRequestor = true;
                                                        this.UpdateTicket(TicketComments);
                                                    }
                                                    else
                                                    {
                                                        if (this.TDXAutomationTicket.TicketRequestor.ProvAccts.Contains("gsuite"))
                                                        {
                                                            this.UpdateAutomationStatus(AUTOMATIONSTATUS.CANCELED);
                                                            AutomationDetails.AppendFormat(" , [{0}]: {1} is not yet eligible for reinstatement. The account is not currently disabled. The request has been cancelled.",
                                                                DateTime.UtcNow.ToString(),
                                                                this.TDXAutomationTicket.TicketRequestor.UserPrincipalName);

                                                            // Update the ticket and notify the customer.
                                                            TicketComments.AppendFormat("Your Google Workspace Account is not yet eligible for reinstatement. Your account is not currently disabled. This request has been cancelled.",
                                                                this.TDXAutomationTicket.TicketRequestor.UserPrincipalName);
                                                            this.NotifyCreator = true;
                                                            this.NotifyRequestor = true;
                                                            this.UpdateTicket(TicketComments);
                                                        }
                                                        // 
                                                        else
                                                        {
                                                            if (this.TDXAutomationTicket.CreatedDate < DateTime.Now.AddDays(-3))
                                                            {
                                                                // Assign the cancelled request to L3
                                                                this.UpdateResponsibleGroup(45);
                                                                this.UpdateAutomationStatus(AUTOMATIONSTATUS.CANCELED);
                                                                AutomationDetails.AppendFormat(" , [{0}]: {1} is missing the GSuite value in cornelleduProvAccounts but has not yet started the deletion process.",
                                                                    DateTime.UtcNow.ToString(),
                                                                    this.TDXAutomationTicket.TicketRequestor.UserPrincipalName);

                                                                // Update the ticket and notify the customer.
                                                                TicketComments.AppendFormat("A time out has occurred processing your request. Please respond back to this ticket so we may investigate the issue.",
                                                                this.TDXAutomationTicket.TicketRequestor.UserPrincipalName);
                                                                this.NotifyCreator = true;
                                                                this.NotifyRequestor = true;
                                                                this.UpdateTicket(TicketComments);
                                                            }
                                                        }
                                                    }
                                                }
                                                // The customer does not currently have a Google Workspace Account.
                                                else
                                                {
                                                    // Assign the cancelled request to L3
                                                    this.UpdateResponsibleGroup(45);

                                                    this.UpdateAutomationStatus(AUTOMATIONSTATUS.CANCELED);
                                                    AutomationDetails.AppendFormat(" , [{0}]: {1} does not currently have a Google Workspace Account. Canceling this request.",
                                                        DateTime.UtcNow.ToString(),
                                                        this.TDXAutomationTicket.TicketRequestor.UserPrincipalName);

                                                    // Update the ticket and notify the customer.
                                                    TicketComments.AppendFormat("You do not currently have a Google Workspace Account. This request will be cancelled.",
                                                        this.TDXAutomationTicket.TicketRequestor.UserPrincipalName);
                                                    this.NotifyCreator = true;
                                                    this.NotifyRequestor = true;
                                                    this.UpdateTicket(TicketComments);
                                                }
                                            }
                                            // Customer may only may only request reinstatement once.
                                            else
                                            {
                                                // Assign the resolved ticket to (L3).
                                                this.UpdateResponsibleGroup(45);

                                                // Update the Automation Status and Automation Status Details.
                                                this.UpdateAutomationStatus(AUTOMATIONSTATUS.COMPLETE);
                                                AutomationDetails.AppendFormat(" , [{0}]: The Google Workspace Account reinstatement has been declined: {1} has exceeded the allowed number of requests.", DateTime.UtcNow.ToString(), this.TDXAutomationTicket.TicketRequestor.UserPrincipalName);

                                                // Update the ticket and notify the customer.
                                                TicketComments.AppendFormat("Your Google Workspace Account Reinstatement has been declined. You are only permitted one such request.");
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
                                        // This request may not be submitted on behalf of the customer.
                                        else
                                        {
                                            // Assign the cancelled request to L3
                                            this.UpdateResponsibleGroup(45);

                                            // Update the Automation Status and Automation Status Details.
                                            this.UpdateAutomationStatus(AUTOMATIONSTATUS.CANCELED);
                                            AutomationDetails.AppendFormat(" , [{0}]: The creator {1} is not allowed to request to reinstatement of your Google Workspace Account on behalf of. The request has been cancelled.",
                                                DateTime.UtcNow.ToString(),
                                                this.TDXAutomationTicket.TicketCreator.UserPrincipalName);

                                            // Update the ticket and notify the customer.
                                            TicketComments.AppendFormat("{0} {1} is not authorized to request to request to reinstatement of your Google Workspace Account on your behalf. You must make these requests directly. No changes have been made to your account.",
                                                this.TDXAutomationTicket.TicketCreator.DisplayName,
                                                this.TDXAutomationTicket.TicketCreator.UserPrincipalName);

                                            this.NotifyCreator = true;
                                            this.NotifyRequestor = true;
                                            this.UpdateTicket(TicketComments, "Cancelled");
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
                                        AutomationDetails.AppendFormat(" , [{0}]: The Google Workspace Quota Grace Period has been reinstated by adding: {1} to the googleworkspaceaccountaccessgraceperiod Azure Security Group.", DateTime.UtcNow.ToString(), this.TDXAutomationTicket.TicketRequestor.UserPrincipalName);

                                        // Update the ticket and notify the customer.
                                        TicketComments.AppendFormat("Your Google Workspace Reinstatement is being processed. It will take up to one hour for your Google Workspace account to be reinstated.");
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
                                        // Assign the resolved ticket to (L3).
                                        this.UpdateResponsibleGroup(45);

                                        // Update the ticket and notify the customer.
                                        TicketComments.AppendFormat("Your Google Workspace Account Reinstatement has been cancelled.");
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
                                // Automation Processing for DECLINED Tickets.
                                case var value when value == AUTOMATIONSTATUS.DECLINED:
                                    {
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