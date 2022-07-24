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
    public class RequestGoogleWorkspaceAccount : TDXTicketManager
    {
        public RequestGoogleWorkspaceAccount()
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            //TDXAutomationTickets = new List<TDXAutomationTicket>();

            // Inactive Ticket Statuses
            Regex InactiveTicketsRegex = new Regex(@"(Reopened|Resolved|Closed|Canceled)", RegexOptions.IgnoreCase);

            // ------
            // Get the list of tickets from TDX using the Automated E-List Owner Transfer Requests report.
            // This report returns all of the tickets that are using the:
            // Discussion and Announcement Email List / Transfer e-List Ownership (V2) TDX Form.
            // ------
            GetTicketsUsingReport("* Office 365 / Faculty A3 License Requests", InactiveTicketsRegex);

            // Populate ListOwnerTransferTickets ;
            foreach (Ticket ticket in this.TDXTickets)
            {
                if (ticket != null)
                {
                    String ticketStatus = ticket.StatusName;
                    if (!InactiveTicketsRegex.IsMatch(ticketStatus))
                    {
                        this.SetActiveTicket(ticket);

                        // Get Automation Status [TDX Custom Attribute: (S111-AUTOMATIONDETAILS)]
                        StringBuilder AutomationDetails = new StringBuilder(this.TDXAutomationTicket.AutomationDetails);

                        // Get Automation ID [TDX Custom Attribute: (S111-AUTOMATIONID)]
                        if (this.TDXAutomationTicket.AutomationID == null)
                        {
                            this.UpdateAttribute("S111-AUTOMATIONID", Guid.NewGuid().ToString());
                            AutomationDetails.AppendFormat("[{0}]: Automation Status ID has been set.<br />", DateTime.UtcNow.ToString("yyyy:mm:dd:HH:mm"));
                        }

                        // Get Automation Status [TDX Custom Attribute: (S111-AUTOMATIONSTATUS)]
                        switch (this.TDXAutomationTicket.AutomationStatus)
                        {
                            case null:
                                {
                                    this.UpdateDropDownChoiceAttribute("S111-AUTOMATIONSTATUS", AUTOMATIONSTATUS.NEW);
                                    AutomationDetails.AppendFormat("[{0}]: AutomationStatus has been set to NEW.   ", DateTime.UtcNow.ToString());
                                    break;
                                }
                            case var value when value == AUTOMATIONSTATUS.NEW:
                                {
                                    this.UpdateDropDownChoiceAttribute("S111-AUTOMATIONSTATUS", AUTOMATIONSTATUS.NEW);
                                    AutomationDetails.AppendFormat("[{0}]: AutomationStatus is set to NEW.   ", DateTime.UtcNow.ToString());
                                    break;
                                }

                            case var value when value == AUTOMATIONSTATUS.INPROCESS:
                                {
                                    this.UpdateDropDownChoiceAttribute("S111-AUTOMATIONSTATUS", AUTOMATIONSTATUS.INPROCESS);
                                    AutomationDetails.AppendFormat("[{0}]: AutomationStatus is set to INPROCESS.   ", DateTime.UtcNow.ToString());
                                    break;
                                }
                            case var value when value == AUTOMATIONSTATUS.PENDINGAPPROVAL:
                                {
                                    this.UpdateDropDownChoiceAttribute("S111-AUTOMATIONSTATUS", AUTOMATIONSTATUS.PENDINGAPPROVAL);
                                    AutomationDetails.AppendFormat("[{0}]: AutomationStatus is set to PENDINGAPPROVAL.   ", DateTime.UtcNow.ToString());
                                    break;
                                }
                            case var value when value == AUTOMATIONSTATUS.COMPLETE:
                                {
                                    this.UpdateDropDownChoiceAttribute("S111-AUTOMATIONSTATUS", AUTOMATIONSTATUS.COMPLETE);
                                    AutomationDetails.AppendFormat("[{0}]: AutomationStatus is set to COMPLETE.   ", DateTime.UtcNow.ToString());
                                    break;
                                }
                            case var value when value == AUTOMATIONSTATUS.CANCELED:
                                {
                                    this.UpdateDropDownChoiceAttribute("S111-AUTOMATIONSTATUS", AUTOMATIONSTATUS.CANCELED);
                                    AutomationDetails.AppendFormat("[{0}]: AutomationStatus is set to CANCELED.   ", DateTime.UtcNow.ToString());
                                    break;
                                }
                            case var value when value == AUTOMATIONSTATUS.DECLINED:
                                {
                                    this.UpdateDropDownChoiceAttribute("S111-AUTOMATIONSTATUS", AUTOMATIONSTATUS.DECLINED);
                                    AutomationDetails.AppendFormat("[{0}]: AutomationStatus is set to DECLINED.   ", DateTime.UtcNow.ToString());
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

