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
    public class RequestOffice365AccountTDXService : TDXTicketManager
    {
        public RequestOffice365AccountTDXService()
        {
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

            // Process the tickets returned from the report.
            foreach (Ticket ticket in this.TDXTickets)
            {
                if (ticket != null)
                {
                    String ticketStatus = ticket.StatusName;
                    if (!InactiveTicketsRegex.IsMatch(ticketStatus))
                    {
                        // Set the Active Ticket, this sets the scope for all fuctions and methods.
                        this.SetActiveTicket(ticket);

                        // Get Automation Status Details [TDX Custom Attribute: (S111-AUTOMATIONDETAILS)] in
                        // a StringBuilde so that we can update the automation details to the TDX Request.
                        StringBuilder AutomationDetails = new StringBuilder(this.TDXAutomationTicket.AutomationDetails);

                        // Ticket Comments StringBuilder.
                        StringBuilder TicketComments = new StringBuilder();

                        // ------
                        // The Automation Status Attribute [TDX Custom Attribute: (S111-AUTOMATIONSTATUS)] is used to direct
                        // automation processing. It is intended that it be updated by this class and by TeamDynamix Workflows.
                        // The standard configuration of TDX forms should not allow for manual updates to (S111-AUTOMATIONSTATUS) unless
                        // every possible state change can be handled by this class or its parent(s). As with allowing manual updates, when
                        // creating TeamDynamix workflow consideration must be given to setting (S111-AUTOMATIONSTATUS) such that the follow
                        // processing steps will run in the desired order or that the processing steps are order independent.
                        // ------

                        switch (this.TDXAutomationTicket.AutomationStatus)
                        {
                            case null:
                                {
                                    // Update the Automation Status and Automation Status Details.
                                    this.UpdateAutomationStatus(AUTOMATIONSTATUS.NEW);
                                    AutomationDetails.AppendFormat("[{0}]: AutomationStatus has been set to NEW.   ", DateTime.UtcNow.ToString());
                                    break;
                                }
                            case var value when value == AUTOMATIONSTATUS.NEW:
                                {
                                    // Update the Automation Status and Automation Status Details.
                                    this.UpdateAutomationStatus(AUTOMATIONSTATUS.NEW);
                                    AutomationDetails.AppendFormat("[{0}]: AutomationStatus is set to NEW.   ", DateTime.UtcNow.ToString());
                                    break;
                                }

                            case var value when value == AUTOMATIONSTATUS.INPROCESS:
                                {
                                    // Update the Automation Status and Automation Status Details.
                                    this.UpdateAutomationStatus(AUTOMATIONSTATUS.INPROCESS);
                                    AutomationDetails.AppendFormat("[{0}]: AutomationStatus is set to INPROCESS.   ", DateTime.UtcNow.ToString());
                                    break;
                                }
                            case var value when value == AUTOMATIONSTATUS.PENDINGAPPROVAL:
                                {
                                    // Update the Automation Status and Automation Status Details.
                                    this.UpdateAutomationStatus(AUTOMATIONSTATUS.PENDINGAPPROVAL);
                                    AutomationDetails.AppendFormat("[{0}]: AutomationStatus is set to PENDINGAPPROVAL.   ", DateTime.UtcNow.ToString());
                                    break;
                                }
                            case var value when value == AUTOMATIONSTATUS.COMPLETE:
                                {
                                    // Update the Automation Status and Automation Status Details.
                                    this.UpdateAutomationStatus(AUTOMATIONSTATUS.COMPLETE);
                                    AutomationDetails.AppendFormat("[{0}]: AutomationStatus is set to COMPLETE.   ", DateTime.UtcNow.ToString());
                                    break;
                                }
                            case var value when value == AUTOMATIONSTATUS.CANCELED:
                                {
                                    // Update the Automation Status and Automation Status Details.
                                    this.UpdateAutomationStatus(AUTOMATIONSTATUS.CANCELED);
                                    AutomationDetails.AppendFormat("[{0}]: AutomationStatus is set to CANCELED.   ", DateTime.UtcNow.ToString());
                                    break;
                                }
                            case var value when value == AUTOMATIONSTATUS.DECLINED:
                                {
                                   // Update the Automation Status and Automation Status Details.
                                    this.UpdateAutomationStatus(AUTOMATIONSTATUS.DECLINED);
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
