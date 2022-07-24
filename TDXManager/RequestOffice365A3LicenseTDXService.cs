using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using TeamDynamix.Api.Tickets;
using TeamDynamix.Api.Users;

namespace TDXManager
{
    /// <summary>
    /// This derived class reads Office 365 A3 Licnese Requests from TeamDynamix and if
    /// appropriate assigns an Office 365 Faculty A3 Licnese to the Ticket's Requestor. 
    /// </summary>
    public class RequestOffice365A3LicenseTDXService : TDXTicketManager
    {
        public RequestOffice365A3LicenseTDXService()
        {
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
                        // Set the Active Ticket, this sets the scope for all fuctions and methods.
                        this.SetActiveTicket(ticket);

                        // Get Automation Status [TDX Custom Attribute: (S111-AUTOMATIONDETAILS)] in a StringBuilder 
                        // so that we can update the automation details to the TDX Request.
                        StringBuilder AutomationDetails = new StringBuilder(this.TDXAutomationTicket.AutomationDetails);
                        
                        // Get Automation ID [TDX Custom Attribute: (S111-AUTOMATIONID)] if this is NULL assign a new automation ID to the TDX Ticket.
                        if (this.TDXAutomationTicket.AutomationID == null)
                        {
                            this.UpdateAttribute("S111-AUTOMATIONID", Guid.NewGuid().ToString());
                            AutomationDetails.AppendFormat("[{0}]: Automation Status ID has been set.<br />", DateTime.UtcNow.ToString("yyyy:mm:dd:HH:mm"));
                        }

                        // Ticket Comments StringBuilder.
                        StringBuilder TicketComments = new StringBuilder();

                        // ------
                        // Get Automation Status [TDX Custom Attribute: (S111-AUTOMATIONSTATUS)]. The Automation Status Attribute is used
                        // to direct automation processing. It is intended that it be updated by this class and by TeamDynamix Workflows.
                        // The standard configuration of TDX forms should not allow for manual updates to (S111-AUTOMATIONSTATUS) unless
                        // every possible state change can be handled by this class or its parent(s). As with allowing manual updates, when
                        // creating TeamDynamix workflow consideration must be given to setting (S111-AUTOMATIONSTATUS) such that the follow
                        // processing steps will run in the desired order or that the processing steps are order independent. 
                        // ------

                        switch (this.TDXAutomationTicket.AutomationStatus)
                        {
                            // Set the intital automation state to new.
                            case null:
                                {
                                    this.UpdateDropDownChoiceAttribute("S111-AUTOMATIONSTATUS", AUTOMATIONSTATUS.NEW);
                                    AutomationDetails.AppendFormat("[{0}]: AutomationStatus has been set to NEW.   ", DateTime.UtcNow.ToString());
                                    break;
                                }
                            // Automation Processing for NEW Tickets.
                            case var value when value == AUTOMATIONSTATUS.NEW:
                                {
                                    this.UpdateDropDownChoiceAttribute("S111-AUTOMATIONSTATUS", AUTOMATIONSTATUS.INPROCESS);
                                    AutomationDetails.AppendFormat("[{0}]: AutomationStatus is set to NEW.   ", DateTime.UtcNow.ToString());
                                    TicketComments.AppendFormat("We have received your request for an Office 365 License. Your request is now in process.");
                                    this.UpdateTicket(TicketComments.ToString(), "InProcess");
                                    break;
                                }
                            // Automation Processing for INPROCESS Tickets.
                            case var value when value == AUTOMATIONSTATUS.INPROCESS:
                                {
                                    this.UpdateDropDownChoiceAttribute("S111-AUTOMATIONSTATUS", AUTOMATIONSTATUS.INPROCESS);
                                    AutomationDetails.AppendFormat("[{0}]: AutomationStatus is set to INPROCESS.   ", DateTime.UtcNow.ToString());

                                    Boolean RequestAllowed = false;

                                    // ---
                                    // Check if the requestor created this ticket.
                                    // ---
                                    if(this.TDXAutomationTicket.TicketCreator.UserPrincipalName == this.TDXAutomationTicket.TicketRequestor.UserPrincipalName)
                                    {
                                        RequestAllowed = true;
                                    }
                                    else
                                    {
                                        // ---
                                        // Check if the creator is can request on behalf of the specified requestor.
                                        // ---
                                        if(this.TDXAutomationTicket.TicketCreator.MemberOf.Contains("RequestFacultyA3LicenseDelegate"))
                                        {
                                            RequestAllowed = true;
                                        }
                                        else
                                        {
                                            this.UpdateDropDownChoiceAttribute("S111-AUTOMATIONSTATUS", AUTOMATIONSTATUS.DECLINED);
                                            TicketComments.AppendFormat("{0} {1} is not authorized to request an Office 365 Licnese on your behalf. No changes have been made to your account.",
                                                this.TDXAutomationTicket.TicketCreator.DisplayName,
                                                this.TDXAutomationTicket.TicketCreator.UserPrincipalName);
                                            this.UpdateTicket(TicketComments.ToString(), "Cancelled");
                                        }
                                    }

                                    
                                    if(RequestAllowed)
                                    {
                                        if (this.TDXAutomationTicket.TicketRequestor.ProvAccts.Contains("office365-a3"))
                                        {
                                            this.UpdateDropDownChoiceAttribute("S111-AUTOMATIONSTATUS", AUTOMATIONSTATUS.CANCELED);
                                            TicketComments.AppendFormat("Your account has already been provisioned with the Office 365 License you have requested. No changes have been made to your account.");
                                            this.UpdateTicket(TicketComments.ToString(), "Cancelled");
                                        }
                                        else
                                        {

                                        }

                                        
                                    }









                                    this.UpdateTicket(TicketComments.ToString(), "InProcess");
                                    break;
                                }
                            // Automation Processing for PENDINGAPPROVAL Tickets.
                            case var value when value == AUTOMATIONSTATUS.PENDINGAPPROVAL:
                                {
                                    this.UpdateDropDownChoiceAttribute("S111-AUTOMATIONSTATUS", AUTOMATIONSTATUS.PENDINGAPPROVAL);
                                    AutomationDetails.AppendFormat("[{0}]: AutomationStatus is set to PENDINGAPPROVAL.   ", DateTime.UtcNow.ToString());
                                    this.UpdateTicket(TicketComments.ToString(), "InProcess");
                                    break;
                                }
                            // Automation Processing for APPROVED Tickets.
                            case var value when value == AUTOMATIONSTATUS.APPROVED:
                                {
                                    this.UpdateDropDownChoiceAttribute("S111-AUTOMATIONSTATUS", AUTOMATIONSTATUS.PENDINGAPPROVAL);
                                    AutomationDetails.AppendFormat("[{0}]: AutomationStatus is set to PENDINGAPPROVAL.   ", DateTime.UtcNow.ToString());
                                    this.UpdateTicket(TicketComments.ToString(), "InProcess");
                                    break;
                                }
                            // Automation Processing for COMPLETE Tickets.
                            case var value when value == AUTOMATIONSTATUS.COMPLETE:
                                {
                                    this.UpdateDropDownChoiceAttribute("S111-AUTOMATIONSTATUS", AUTOMATIONSTATUS.COMPLETE);
                                    AutomationDetails.AppendFormat("[{0}]: AutomationStatus is set to COMPLETE.   ", DateTime.UtcNow.ToString());
                                    this.UpdateTicket(TicketComments.ToString(), "InProcess");
                                    break;
                                }
                            // Automation Processing for CANCELED Tickets.
                            case var value when value == AUTOMATIONSTATUS.CANCELED:
                                {
                                    this.UpdateDropDownChoiceAttribute("S111-AUTOMATIONSTATUS", AUTOMATIONSTATUS.CANCELED);
                                    AutomationDetails.AppendFormat("[{0}]: AutomationStatus is set to CANCELED.   ", DateTime.UtcNow.ToString());
                                    this.UpdateTicket(TicketComments.ToString(), "InProcess");
                                    break;
                                }
                            // Automation Processing for DECLINED Tickets.
                            case var value when value == AUTOMATIONSTATUS.DECLINED:
                                {
                                    this.UpdateDropDownChoiceAttribute("S111-AUTOMATIONSTATUS", AUTOMATIONSTATUS.DECLINED);
                                    AutomationDetails.AppendFormat("[{0}]: AutomationStatus is set to DECLINED.   ", DateTime.UtcNow.ToString());
                                    this.UpdateTicket(TicketComments.ToString(), "InProcess");
                                    break;
                                }
                            default:
                                break;
                        }

                        // Set Automation Status [TDX Custom Attribute: (S111-AUTOMATIONSTATUSDETAILS)]
                        AutomationDetails.Clear();
                        this.UpdateAttribute("S111-AUTOMATIONDETAILS", AutomationDetails.ToString());
                    }
                }
            }
            stopwatch.Stop();
        }
    }
}
