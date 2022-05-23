using ActiveDirectoryAccess;
using ApplicationServicesConfigurationManagementDatabaseAccess;
using ApplicationServicesConfigurationManagementDatabaseAccess.Models;
using AuthenticationServices;
using ListServiceManagement.Models;
using ServiceEventLoggingManager;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using TDXManager;

namespace ApplicationServicesConfigurationManagementTestSuite
{
    internal class TestSuite
    {
        private static void Main(string[] args)
        {
            Boolean Run = true;
            Boolean DoNotRun = false;
            Regex InactiveTicketsRegex = new Regex(@"(Reopened|Resolved|Closed|Canceled)", RegexOptions.IgnoreCase);

            if(Run)
            {
                ListOwnerTransferTDXService listOwnerTransferTDXService = new ListOwnerTransferTDXService();
            }



            if (DoNotRun)
            {
                while (true)
                {
                    TDXTicketManager tDXTicketManager = new TDXTicketManager();
                    ListServiceManagmentContext context = new ListServiceManagmentContext();
                    ActiveDirectoryContext activeDirectoryContext = new ActiveDirectoryContext();

                    tDXTicketManager.GetTicketsUsingReport("Automated Transfer e-List Owner Transfer Requests");

                    Console.WriteLine("There are {0} active tickets to process.", tDXTicketManager.TDXTickets.Count);
                    foreach (var ticket in tDXTicketManager.TDXTickets)
                    {
                        String ticketStatus = ticket.StatusName;
                        if (!InactiveTicketsRegex.IsMatch(ticketStatus))
                        {
                            Console.WriteLine("Processing Ticket [{0}]: {1}", ticket.ID, ticket.Title);

                            // Get the request properties.
                            TeamDynamix.Api.Users.User CreatedUser = tDXTicketManager.GetTDXUserByUID(ticket.CreatedUid.ToString());
                            TeamDynamix.Api.Users.User RequestorUser = tDXTicketManager.GetTDXUserByUID(ticket.RequestorUid.ToString());
                            String CurrentEListOwnerID = ticket.Attributes.Where(a => a.Name.Equals("S154-CURRENTLISTOWNER")).Select(a => a.Value).FirstOrDefault();
                            String NewElistOwnerID = ticket.Attributes.Where(a => a.Name.Equals("S154-NEWLISTOWNER")).Select(a => a.Value).FirstOrDefault();
                            String ElistName = ticket.Attributes.Where(a => a.Name.Equals("S154-LISTNAME")).Select(a => a.Value).FirstOrDefault();
                            ElistOwnerTransfer elistOwnerTransfer = context.ElistOwnerTransfers.Where(t => t.RequestTicketID.Equals(ticket.ID)).FirstOrDefault();

                            // If we are given an Elist Email Address update it to its local part only.
                            if (ElistName.Contains('@')) { ElistName = ElistName.Split('@')[0]; }

                            // Lookup the Elist contact in the ElistContacts Data Context.
                            ElistContact elistContact = context.ElistContacts
                                .Where(l => l.ListName.Equals(ElistName, StringComparison.OrdinalIgnoreCase))
                                .FirstOrDefault();

                            // The Elist Contact exists so we can proceed with processing the request.
                            if (elistContact != null)
                            {
                                TeamDynamix.Api.Users.User NewOwner = null;
                                TeamDynamix.Api.Users.User CurrentOwner = null;

                                // Get the New Owner from TDX.
                                if (NewElistOwnerID != null)
                                {
                                    NewOwner = tDXTicketManager.GetTDXUserByUID(NewElistOwnerID);
                                }
                                // Get the Current Owner:
                                if (CurrentEListOwnerID != null) // from TDX if it was previously set.
                                {
                                    CurrentOwner = tDXTicketManager.GetTDXUserByUID(CurrentEListOwnerID);
                                }
                                else // from the Elist Contact if this is a new ticket.
                                {
                                    CurrentOwner = tDXTicketManager.GetTDXUserByUserPrincipalName(String.Format("{0}@cornell.edu", elistContact.OwnerNetID));
                                }

                                #region ------ New TDX Request Ticket Processing ------

                                if (ticketStatus.Equals("NEW", StringComparison.OrdinalIgnoreCase))
                                {
                                    Console.WriteLine("     --- Ticket Status: New");

                                    // ============
                                    // Check that a new owner has been specified.
                                    if (NewOwner != null)
                                    {
                                        // ============
                                        // Create the new title for this ticket (Based on the Request Title Template).
                                        String Title = File.ReadAllText(@".\Messages\RequestTitleTemplate.txt");
                                        Title = Title.Replace("%%%-LISTNAME-%%%", ElistName);
                                        Title = Title.Replace("%%%-NETID-%%%", NewOwner.UserName.Split('@')[0]);

                                        // ============
                                        // Create the new description for this ticket (Based on the Request Description Template).
                                        String Description = File.ReadAllText(@".\Messages\RequestDescription.txt");
                                        Description = Description.Replace("%%%-LISTOWNERFULLNAME-%%%", NewOwner.FullName);
                                        Description = Description.Replace("%%%-NETID-%%%", NewOwner.UserName.Split('@')[0]);
                                        Description = Description.Replace("%%%-LISTNAME-%%%", elistContact.ListName);
                                        Description = Description.Replace("%%%-CREATORFULLNAME-%%%", CreatedUser.FullName);
                                        Description = Description.Replace("%%%-CREATORNETID-%%%", CreatedUser.UserName.Split('@')[0]);

                                        // ============
                                        // Create a Patch document to update the Title and Decription of this Request Ticket).
                                        tDXTicketManager.TDXTicket = ticket;
                                        var patch = new TeamDynamix.Api.Tickets.Ticket()
                                        {
                                            Title = Title,
                                            Description = Description
                                        };

                                        // ============
                                        // Patch the request ticket
                                        tDXTicketManager.PatchTicket(patch);

                                        // ============
                                        // Update the Current Owner Property for this ticket.

                                        if (CurrentOwner != null)
                                        {
                                            tDXTicketManager.UpdateAttribute("S154-CURRENTLISTOWNER", CurrentOwner.UID.ToString());
                                        }

                                        Console.WriteLine("     --- Ticket Patched: Updated Title and Description and Current E-List Owner.");
                                    }
                                    else
                                    {
                                        // ============
                                        // Cancel this Request Ticket because no New Owner was specified.
                                        String TicketUpdate = File.ReadAllText(@".\Messages\RequestNoOwnerSpecifiedTemplate.txt");
                                        TicketUpdate = TicketUpdate.Replace("%%%-LISTNAME-%%%", ElistName);
                                        tDXTicketManager.TDXTicket = ticket;
                                        tDXTicketManager.NotificationEmails.Clear();
                                        tDXTicketManager.NotifyCreator = true;
                                        tDXTicketManager.NotifyRequestor = true;
                                        tDXTicketManager.UpdateTicket(TicketUpdate, "Cancelled");
                                        Console.WriteLine("     --- Ticket Canceled: No new onwer provided.");
                                    }

                                    #endregion ------ New TDX Request Ticket Processing ------
                                }

                                #region ------ Request Validation Checks ------

                                // ==================================================================================================================================
                                // Check the the validity of the request (It must have been created by the owner, the sponsor, or the owner's manager.
                                // ============
                                Boolean TransferRequestValid = false;
                                // ============
                                // Check if the Request Creator is the current Elist owner.
                                if (String.Format("{0}@cornell.edu", elistContact.OwnerNetID).Equals(CreatedUser.UserName, StringComparison.OrdinalIgnoreCase))
                                {
                                    Console.WriteLine("     --- The Request's Creator: {0} is the Elist owner.", ticket.CreatedEmail);
                                    TransferRequestValid = true;
                                }

                                // ============
                                // Check if the Request Creator is the current Elist sponsor.
                                if (String.Format("{0}@cornell.edu", elistContact.SponsorNetID).Equals(CreatedUser.UserName, StringComparison.OrdinalIgnoreCase))
                                {
                                    Console.WriteLine("     --- The Request's Creator: {0} is the Elist sponsor.", ticket.CreatedEmail);
                                    TransferRequestValid = true;
                                }

                                // ============
                                // Check if the Request Creator is the current EList owner's manager.
                                var activeDirectoryEntity = activeDirectoryContext.SearchDirectory("userPrincipalName", String.Format("{0}@cornell.edu", elistContact.OwnerNetID));
                                if (activeDirectoryEntity != null)
                                {
                                    if (activeDirectoryEntity.Count == 1)
                                    {
                                        if (activeDirectoryEntity[0].directoryProperties.ContainsKey("manager"))
                                        {
                                            String managerUserPrincipalName = activeDirectoryContext.SearchDirectory("distinguishedName", activeDirectoryEntity[0].directoryProperties["manager"].ToString())[0].userPrincipalName;
                                            if (managerUserPrincipalName.Equals(CreatedUser.UserName, StringComparison.OrdinalIgnoreCase))
                                            {
                                                Console.WriteLine("     --- The Request's Creator: {0} is the Elist owner's manager.", ticket.CreatedEmail);
                                                TransferRequestValid = true;
                                            }
                                        }
                                    }
                                }

                                // ============
                                // Check if the current owner's affiliation is a student if so it can be transferred by any academic, faculty, or staff.
                                var currentOwnerDirectoryEntity = activeDirectoryContext.SearchDirectory("userPrincipalName", String.Format("{0}@cornell.edu", elistContact.OwnerNetID));
                                if (currentOwnerDirectoryEntity != null)
                                {
                                    if (activeDirectoryEntity.Count == 1)
                                    {
                                        if (currentOwnerDirectoryEntity[0].directoryProperties["cornelleduPrimaryAffiliation"] != null)
                                        {
                                            Regex trasferableFromRegex = new Regex(@"^(alumni|applicant|associate|exception|former postdoc|retired faculty|retiree|student)$", RegexOptions.IgnoreCase);
                                            if (trasferableFromRegex.IsMatch(currentOwnerDirectoryEntity[0].directoryProperties["cornelleduPrimaryAffiliation"].ToString()))
                                            {
                                                var creatorActiveDirectoryEntity = activeDirectoryContext.SearchDirectory("userPrincipalName", CreatedUser.UserName);
                                                if (creatorActiveDirectoryEntity != null)
                                                {
                                                    if (creatorActiveDirectoryEntity.Count == 1)
                                                    {
                                                        if (creatorActiveDirectoryEntity[0].directoryProperties["cornelleduPrimaryAffiliation"] != null)
                                                        {
                                                            Regex transferableByRegex = new Regex(@"^(affiliate|academic|emeritus|faculty|staff)$", RegexOptions.IgnoreCase);
                                                            if (transferableByRegex.IsMatch(creatorActiveDirectoryEntity[0].directoryProperties["cornelleduPrimaryAffiliation"].ToString()))
                                                            {
                                                                TransferRequestValid = true;
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }

                                // ============
                                // Check that the new owner's affiliation is valid for list ownership.
                                Boolean InvalidNewOwnerAffiliation = false;
                                var proposedNewOwnerAdEntity = activeDirectoryContext.SearchDirectory("userPrincipalName", NewOwner.UserName);
                                if (proposedNewOwnerAdEntity != null)
                                {
                                    if (activeDirectoryEntity.Count == 1)
                                    {
                                        if (activeDirectoryEntity[0].directoryProperties["cornelleduPrimaryAffiliation"] != null)
                                        {
                                            Regex allowedAffiliations = new Regex(@"^(academic|affiliate|emeritus|faculty|staff|student|temporary)$", RegexOptions.IgnoreCase);
                                            if (!allowedAffiliations.IsMatch(activeDirectoryEntity[0].directoryProperties["cornelleduPrimaryAffiliation"]))
                                            {
                                                TransferRequestValid = false;
                                                InvalidNewOwnerAffiliation = true;
                                            }
                                        }
                                        else
                                        {
                                            TransferRequestValid = false;
                                            InvalidNewOwnerAffiliation = true;
                                        }
                                    }
                                }

                                #endregion ------ Request Validation Checks ------

                                #region ---- Automation Processing ----

                                // ============
                                // S154-ListOwnerTransferAutomationStatus indicates the current status of workflows in the request ticket.
                                String AutomationStatus = ticket.Attributes.Where(a => a.Name.Equals("S154-ListOwnerTransferAutomationStatus")).Select(a => a.ValueText).FirstOrDefault();

                                switch (AutomationStatus.ToUpper())
                                {
                                    #region ---- Automation Status New ----

                                    case "NEW":

                                        if (TransferRequestValid) // Proceed with this request.
                                        {
                                            Console.WriteLine("     --- New Transfer Valid");

                                            if (AutomationStatus.Equals("NEW", StringComparison.OrdinalIgnoreCase))
                                            {
                                                String TicketUpdate = File.ReadAllText(@".\Messages\RequestApproved.txt");
                                                TicketUpdate = TicketUpdate.Replace("%%%-NETID-%%%", NewOwner.UserName.Split('@')[0]);
                                                TicketUpdate = TicketUpdate.Replace("%%%-LISTNAME-%%%", elistContact.ListName);

                                                // Update the ticket and set the ticket workflow.
                                                tDXTicketManager.TDXTicket = ticket;
                                                tDXTicketManager.NotificationEmails.Clear();
                                                tDXTicketManager.NotifyCreator = true;
                                                tDXTicketManager.NotifyRequestor = true;
                                                tDXTicketManager.UpdateTicket(TicketUpdate, "In Process");
                                                Console.WriteLine("     --- Assigning New Owner Acceptance Workflow to the request and notifying.");
                                                tDXTicketManager.SetTicketWorkflow(455847);

                                                TeamDynamix.Api.Tickets.Ticket patchedTicket = tDXTicketManager.UpdateDropDownChoiceAttribute("S154-ListOwnerTransferAutomationStatus", "APPROVED");
                                                AutomationStatus = patchedTicket.Attributes.Where(a => a.Name.Equals("S154-ListOwnerTransferAutomationStatus")).Select(a => a.ValueText).FirstOrDefault();
                                                Console.WriteLine("     --- Valid Request. Now Awaiting New Owner Accpetance From: {0}  {1}", NewOwner.FullName, NewOwner.UserName);
                                            }
                                        }
                                        else // Cancel this request it is invalid.
                                        {
                                            Console.WriteLine("     --- New Transfer Is Not Valid");

                                            String TicketUpdate = "";
                                            if (InvalidNewOwnerAffiliation)
                                            {
                                                TicketUpdate = File.ReadAllText(@".\Messages\InvalidNewOwnerAffiliation.txt");
                                            }
                                            else
                                            {
                                                TicketUpdate = File.ReadAllText(@".\Messages\RequestReceivedFromUnauthorizedPerson.txt");
                                            }
                                            TicketUpdate = TicketUpdate.Replace("%%%-NEWOWNERFULLNAME-%%%", NewOwner.FullName);
                                            TicketUpdate = TicketUpdate.Replace("%%%-NETID-%%%", NewOwner.UserName.Split('@')[0]);
                                            TicketUpdate = TicketUpdate.Replace("%%%-LISTNAME-%%%", elistContact.ListName);
                                            TicketUpdate = TicketUpdate.Replace("%%%-CREATORNETID-%%%", CreatedUser.UserName.Split('@')[0]);
                                            if (CurrentOwner != null)
                                            {
                                                TicketUpdate = TicketUpdate.Replace("%%%-CURRENTOWNERNETID-%%%", CurrentOwner.UserName.Split('@')[0]);
                                            }
                                            TicketUpdate = TicketUpdate.Replace("%%%-CREATORFULLNAME-%%% ", CreatedUser.FullName);

                                            tDXTicketManager.TDXTicket = ticket;
                                            tDXTicketManager.NotificationEmails.Clear();
                                            tDXTicketManager.NotifyCreator = true;
                                            tDXTicketManager.NotifyRequestor = true;
                                            tDXTicketManager.UpdateTicket(TicketUpdate, "Cancelled");
                                            tDXTicketManager.UpdateResponsibleGroup(45);
                                            Console.WriteLine("     --- Invalid request. Requestor is not authorized. Cancelling the request and sending notifications.");
                                        }

                                        break;

                                    #endregion ---- Automation Status New ----

                                    #region ---- Automation Status Approved ----

                                    case "APPROVED":
                                        Console.WriteLine("     --- Awaiting New Owner Accpetance From: {0}  {1}", NewOwner.FullName, NewOwner.UserName);
                                        break;

                                    #endregion ---- Automation Status Approved ----

                                    #region ---- Automation Status New Owner Accepted ----

                                    case "NEWOWNERACCEPTED":
                                        if (elistOwnerTransfer == null)
                                        {
                                            DateTime CurrentTimeUTC = DateTime.UtcNow;
                                            ElistOwnerTransfer newElistOnwerTransfer = new ElistOwnerTransfer()
                                            {
                                                AcceptTicketID = ticket.ID,
                                                CurrentOwner = elistContact.OwnerNetID,
                                                ListName = elistContact.ListName,
                                                NewOwner = NewOwner.UserName.Split('@')[0],
                                                NewOwnerEmailAddress = NewOwner.PrimaryEmail,
                                                NewOwnerDisplayName = NewOwner.FullName,
                                                RequestIdentifier = Guid.NewGuid(),
                                                RequestStatusDetail = "",
                                                RequestTicketID = ticket.ID,
                                                Status = "APPROVED",
                                                WhenChanged = CurrentTimeUTC,
                                                WhenCreated = CurrentTimeUTC
                                            };
                                            context.ElistOwnerTransfers.Add(newElistOnwerTransfer);
                                            context.SaveChanges();

                                            String NewOwnerAcceptanceMessage = File.ReadAllText(@".\Messages\RequestNewOwnerAcceptanceMessage.txt");
                                            NewOwnerAcceptanceMessage = NewOwnerAcceptanceMessage.Replace("%%%-LISTOWNERFULLNAME-%%%", NewOwner.FullName);
                                            NewOwnerAcceptanceMessage = NewOwnerAcceptanceMessage.Replace("%%%-LISTNAME-%%%", ElistName);
                                            tDXTicketManager.NotificationEmails.Clear();
                                            tDXTicketManager.NotifyCreator = true;
                                            tDXTicketManager.NotifyRequestor = true;
                                            tDXTicketManager.AddNotificationEmail(CurrentOwner.PrimaryEmail, true);
                                            tDXTicketManager.AddNotificationEmail(NewOwner.PrimaryEmail, true);
                                            tDXTicketManager.UpdateTicket(NewOwnerAcceptanceMessage);

                                            Console.WriteLine("     --- New Onwer has accepted adding transfer work item to backend queue.");
                                        }
                                        else
                                        {
                                            #region ---- Check Backend Transfer Status ----

                                            switch (elistOwnerTransfer.Status)
                                            {
                                                case "ERROR":
                                                    // A back end error has occured. Add a private entry to the feed and assign the ticket
                                                    // to the messaging group update the transfer status to ERROR Logged.
                                                    String backEndErrorMessage = elistOwnerTransfer.RequestStatusDetail;
                                                    tDXTicketManager.AddTicketFeedEntry(backEndErrorMessage, false, true);
                                                    tDXTicketManager.UpdateResponsibleGroup(45);
                                                    elistOwnerTransfer.Status = "ERRORLOGGED";
                                                    context.SaveChanges();
                                                    break;

                                                case "COMPLETE":

                                                    tDXTicketManager.TDXTicket = ticket;

                                                    // ============
                                                    // Notify the new List Owner that the trasfer has completed.
                                                    String CompletionMessage = File.ReadAllText(@".\Messages\NewListOwnerTransferCompleted.txt");

                                                    CompletionMessage = CompletionMessage.Replace("%%%-NETID-%%%", NewOwner.UserName.Split('@')[0]);    // New owner name from the ticket.
                                                    CompletionMessage = CompletionMessage.Replace("%%%-LISTOWNERFULLNAME-%%%", NewOwner.FullName);      // New owner full name from the ticket.
                                                    CompletionMessage = CompletionMessage.Replace("%%%-LISTNAME-%%%", ElistName);                       // Elist name from the ticket.

                                                    // This data comes from the Elist Contact Database.
                                                    switch (elistContact.ListDomainName.ToLower())
                                                    {
                                                        case "list.cornell.edu":
                                                            CompletionMessage = CompletionMessage.Replace("%%%-LISTINSTANCEURL-%%%", "https://www.list.cornell.edu");
                                                            break;

                                                        case "hp.list.cornell.edu":
                                                            CompletionMessage = CompletionMessage.Replace("%%%-LISTINSTANCEURL-%%%", "https://www.hp.list.cornell.edu");
                                                            break;

                                                        case "appgen.list.cornell.edu":
                                                            CompletionMessage = CompletionMessage.Replace("%%%-LISTINSTANCEURL-%%%", "https://www.appgenlist.cornell.edu");
                                                            break;

                                                        case "mm.list.cornell.edu":
                                                            CompletionMessage = CompletionMessage.Replace("%%%-LISTINSTANCEURL-%%%", "https://www.bulk.cornell.edu");
                                                            break;

                                                        case "bulk.list.cornell.edu":
                                                            CompletionMessage = CompletionMessage.Replace("%%%-LISTINSTANCEURL-%%%", "https://www.bulk.cornell.edu");
                                                            break;

                                                        default:
                                                            break;
                                                    }
                                                    tDXTicketManager.NotificationEmails.Clear();
                                                    tDXTicketManager.NotifyCreator = false;
                                                    tDXTicketManager.NotifyRequestor = false;
                                                    tDXTicketManager.AddNotificationEmail(NewOwner.PrimaryEmail, true);  // New owner from the ticket.
                                                    tDXTicketManager.UpdateTicket(CompletionMessage);

                                                    // ============
                                                    // Notify the Creator, Requestor, and Previous Owner that the transfer has completed and resolve the ticket.
                                                    CompletionMessage = File.ReadAllText(@".\Messages\PreviousListOwnerTransferCompleted.txt");
                                                    CompletionMessage = CompletionMessage.Replace("%%%-NETID-%%%", NewOwner.UserName.Split('@')[0]);        // New owner name from the ticket.
                                                    CompletionMessage = CompletionMessage.Replace("%%%-LISTOWNERFULLNAME-%%%", NewOwner.FullName);          // New owner full name from the ticket.
                                                    CompletionMessage = CompletionMessage.Replace("%%%-LISTNAME-%%%", ElistName);                           // Elist name from the ticket.
                                                    CompletionMessage = CompletionMessage.Replace("%%%-CURRENTOWNERFULLNAME-%%%", CurrentOwner.FullName);   // The current owner from the ticket's perspecitve. Now the previous owner.
                                                    CompletionMessage = CompletionMessage.Replace("%%%-CREATORFULLNAME-%%%", CreatedUser.FullName);         //

                                                    tDXTicketManager.NotificationEmails.Clear();
                                                    tDXTicketManager.NotifyCreator = true;
                                                    tDXTicketManager.NotifyRequestor = true;
                                                    tDXTicketManager.AddNotificationEmail(CurrentOwner.PrimaryEmail, true);  // From the ticket's perspective the current owner is the previous owner.
                                                    tDXTicketManager.UpdateTicket(CompletionMessage, "Resolved");
                                                    tDXTicketManager.UpdateResponsibleGroup(45);

                                                    Console.WriteLine("     --- Backend transfer complete. Resolving the request and sending notifications.");
                                                    break;

                                                #endregion ---- Check Backend Transfer Status ----

                                                case "CANCELLED":
                                                    tDXTicketManager.TDXTicket = ticket;
                                                    String CancelledUpdate = String.Format("The E-List Ownership Transfer Request for: {0} has been cancelled.\n\n{1}",
                                                        ElistName,
                                                        elistOwnerTransfer.RequestStatusDetail);

                                                    tDXTicketManager.NotificationEmails.Clear();
                                                    tDXTicketManager.NotifyCreator = true;
                                                    tDXTicketManager.NotifyRequestor = true;
                                                    tDXTicketManager.AddNotificationEmail(NewOwner.PrimaryEmail, false);
                                                    tDXTicketManager.UpdateTicket(CancelledUpdate, "Cancelled");
                                                    tDXTicketManager.UpdateResponsibleGroup(45);
                                                    Console.WriteLine("     --- Backend transfer cancelled. Cancelling the request and sending notifications.");
                                                    break;

                                                default:
                                                    break;
                                            }
                                        }
                                        break;

                                    #endregion ---- Automation Status New Owner Accepted ----

                                    #region ---- Automation Status New Owner Declined ----

                                    case "NEWOWNERDECLINED": // The new owner has declined the request.
                                        String DeclineUpdate = File.ReadAllText(@".\Messages\RequestDeclinedByNewOwner.txt");
                                        DeclineUpdate = DeclineUpdate.Replace("%%%-LISTNAME-%%%", ElistName);
                                        DeclineUpdate = DeclineUpdate.Replace("%%%-LISTOWNERFULLNAME-%%%", NewOwner.UserName.Split('@')[0]);
                                        DeclineUpdate = DeclineUpdate.Replace("%%%-CREATORFULLNAME-%%%", CreatedUser.FullName);
                                        DeclineUpdate = DeclineUpdate.Replace("%%%-CURRENTOWNERFULLNAME-%%%", CurrentOwner.FullName);
                                        tDXTicketManager.TDXTicket = ticket;
                                        tDXTicketManager.NotificationEmails.Clear();
                                        tDXTicketManager.NotifyCreator = true;
                                        tDXTicketManager.NotifyRequestor = true;
                                        tDXTicketManager.UpdateTicket(DeclineUpdate, "Cancelled");
                                        tDXTicketManager.UpdateResponsibleGroup(45);
                                        Console.WriteLine("     --- New owner declined transfer request. Cancelling the request and sending notifications.");
                                        break;

                                    #endregion ---- Automation Status New Owner Declined ----

                                    #region ---- Automation Status Other ----

                                    default:
                                        break;

                                        #endregion ---- Automation Status Other ----
                                }

                                #endregion ---- Automation Processing ----
                            }

                            #region ---- Cancelation Conditions ----

                            #region ---- List has been removed ----

                            // The Elist Contact does not exist but there is already a valid transfer in progress.
                            else if (elistOwnerTransfer != null && elistContact == null)

                            {
                                if (elistOwnerTransfer.Status.Equals("CANCELLED"))
                                {
                                    tDXTicketManager.TDXTicket = ticket;

                                    String BackEndError = String.Format("The following Error Has Occurred:\n\n {0}", elistOwnerTransfer.RequestStatusDetail);
                                    tDXTicketManager.UpdateTicket(BackEndError, true);

                                    String TicketUpdate = File.ReadAllText(@".\Messages\ListHasBeenRemoveAfterRequest.txt");
                                    TicketUpdate = TicketUpdate.Replace("%%%-LISTNAME-%%%", ElistName);
                                    TicketUpdate = TicketUpdate.Replace("%%%-CREATORNETID-%%%", CreatedUser.UserName.Split('@')[0]);
                                    TicketUpdate = TicketUpdate.Replace("%%%-CURRENTOWNERNETID-%%%", "N/A");
                                    TicketUpdate = TicketUpdate.Replace("%%%-REQUESTSTATUSDETAIL-%%%", elistOwnerTransfer.RequestStatusDetail);

                                    tDXTicketManager.TDXTicket = ticket;
                                    tDXTicketManager.NotificationEmails.Clear();
                                    tDXTicketManager.AddNotificationEmail(tDXTicketManager.GetTDXUserByUID(NewElistOwnerID).PrimaryEmail, true);
                                    tDXTicketManager.NotifyCreator = true;
                                    tDXTicketManager.NotifyRequestor = true;
                                    tDXTicketManager.UpdateTicket(TicketUpdate, "Cancelled");
                                    tDXTicketManager.UpdateResponsibleGroup(45);
                                    Console.WriteLine("     --- Invalid ListName received. Cancelling the request and sending notifications.");
                                }
                                else if (elistOwnerTransfer.Status.Equals("ERROR"))
                                {
                                    String backEndErrorMessage = elistOwnerTransfer.RequestStatusDetail;
                                    tDXTicketManager.AddTicketFeedEntry(backEndErrorMessage, false, true);
                                    tDXTicketManager.UpdateResponsibleGroup(45);

                                    elistOwnerTransfer.Status = "ERRORLOGGED";
                                    context.SaveChanges();
                                }
                            }

                            #endregion ---- List has been removed ----

                            else if (elistOwnerTransfer == null && elistContact == null)
                            {
                                tDXTicketManager.TDXTicket = ticket;

                                String TicketUpdate = File.ReadAllText(@".\Messages\ListHasBeenRemoveAfterRequest.txt");
                                TicketUpdate = TicketUpdate.Replace("%%%-LISTNAME-%%%", ElistName);
                                TicketUpdate = TicketUpdate.Replace("%%%-CREATORNETID-%%%", CreatedUser.UserName.Split('@')[0]);
                                TicketUpdate = TicketUpdate.Replace("%%%-CURRENTOWNERNETID-%%%", "N/A");

                                tDXTicketManager.TDXTicket = ticket;
                                tDXTicketManager.NotificationEmails.Clear();
                                tDXTicketManager.AddNotificationEmail(tDXTicketManager.GetTDXUserByUID(NewElistOwnerID).PrimaryEmail, true);
                                tDXTicketManager.NotifyCreator = true;
                                tDXTicketManager.NotifyRequestor = true;
                                tDXTicketManager.UpdateTicket(TicketUpdate, "Cancelled");
                                tDXTicketManager.UpdateResponsibleGroup(45);
                                Console.WriteLine("     --- Invalid ListName received. Cancelling the request and sending notifications.");
                            }

                            #region ---- List does not exist ----

                            // The Elist contact does not exist and there is not a valid transfer in progress.
                            else
                            {
                                String TicketUpdate = File.ReadAllText(@".\Messages\RequestWithInvalidListName.txt");
                                TicketUpdate = TicketUpdate.Replace("%%%-LISTNAME-%%%", ElistName);
                                TicketUpdate = TicketUpdate.Replace("%%%-CREATORNETID-%%%", CreatedUser.UserName.Split('@')[0]);
                                TicketUpdate = TicketUpdate.Replace("%%%-CURRENTOWNERNETID-%%%", "N/A");

                                tDXTicketManager.TDXTicket = ticket;
                                tDXTicketManager.NotificationEmails.Clear();
                                tDXTicketManager.NotifyCreator = true;
                                tDXTicketManager.NotifyRequestor = true;
                                tDXTicketManager.UpdateTicket(TicketUpdate, "Cancelled");
                                tDXTicketManager.UpdateResponsibleGroup(45);
                                Console.WriteLine("     --- Invalid ListName received. Cancelling the request and sending notifications.");
                            }

                            #endregion ---- List does not exist ----

                            #endregion ---- Cancelation Conditions ----
                        }
                    }

                    context.Dispose();
                    activeDirectoryContext.Dispose();

                    Thread.Sleep(Convert.ToInt32(new TimeSpan(0, 0, 15).TotalMilliseconds));
                }
            }
            if (Run)
            {
                TeamDynamixManagementContext db = new TeamDynamixManagementContext();
                TDXTicketManager tdxTicketManager = new TDXTicketManager();

                foreach (TeamDynamix.Api.CustomAttributes.CustomAttribute customAttribute in tdxTicketManager.TDXTicketCustomAttributes)
                {
                    TeamDynamixCustomAttribute teamDynamixCustomAttribute = db.TeamDynamixCustomAttributes
                        .Where(a => a.AttributeId.Equals(customAttribute.ID))
                        .FirstOrDefault();

                    if (teamDynamixCustomAttribute == null)
                    {
                        TeamDynamixCustomAttribute newteamDynamixCustomAttribute = new TeamDynamixCustomAttribute()
                        {
                            AtributeName = customAttribute.Name,
                            AttributeId = customAttribute.ID,
                            DataType = customAttribute.DataType,
                            Description = customAttribute.Description,
                            FieldType = customAttribute.FieldType
                        };

                        db.TeamDynamixCustomAttributes.Add(newteamDynamixCustomAttribute);
                    }
                    else
                    {
                        teamDynamixCustomAttribute.AtributeName = customAttribute.Name;
                        teamDynamixCustomAttribute.DataType = customAttribute.DataType;
                        teamDynamixCustomAttribute.Description = customAttribute.Description;
                        teamDynamixCustomAttribute.FieldType = customAttribute.FieldType;
                    }
                    db.SaveChanges();
                }

                foreach (TeamDynamix.Api.Forms.Form form in tdxTicketManager.TDXTicketForms)
                {
                    TeamDynamixForm teamDynamixForm = db.TeamDynamixForms
                        .Where(f => f.FormId.Equals(form.ID))
                        .FirstOrDefault();

                    if (teamDynamixForm == null)
                    {
                        List<TeamDynamixCustomAttribute> teamDynamixCustomAttributes = new List<TeamDynamixCustomAttribute>();

                        TeamDynamixForm newTeamDynamixForm = new TeamDynamixForm()
                        {
                            AppID = form.AppID,
                            FormId = form.ID,
                            IsActive = form.IsActive,
                            FormName = form.Name
                        };
                        db.TeamDynamixForms.Add(newTeamDynamixForm);
                    }
                    else
                    {
                        teamDynamixForm.AppID = form.AppID;
                        teamDynamixForm.IsActive = form.IsActive;
                        teamDynamixForm.FormName = form.Name;
                    }
                    db.SaveChanges();
                }

                foreach (TeamDynamix.Api.Tickets.TicketStatus ticketStatus in tdxTicketManager.TDXTicketStatuses)
                {
                    TeamDynamixStatusClass teamDynamixStatusClass = db.TeamDynamixStatusClasses
                        .Where(c => c.TicketStatusID.Equals(ticketStatus.ID))
                        .FirstOrDefault();

                    if (teamDynamixStatusClass == null)
                    {
                        TeamDynamixStatusClass newTeamDynamixStatusClass = new TeamDynamixStatusClass()
                        {
                            TicketStatusID = ticketStatus.ID,
                            TicketStatusName = ticketStatus.Name,
                            TicketStatusDescription = ticketStatus.Description
                        };
                        db.TeamDynamixStatusClasses.Add(newTeamDynamixStatusClass);
                    }
                    else
                    {
                        teamDynamixStatusClass.TicketStatusName = ticketStatus.Name;
                        teamDynamixStatusClass.TicketStatusDescription = ticketStatus.Description;
                    }
                    db.SaveChanges();
                }
            }

            if (DoNotRun)
            {
                using (ListServiceManagmentContext context = new ListServiceManagmentContext())
                {
                    ElistOwnerTransfer elistOwnerTransfer;

                    elistOwnerTransfer = new ElistOwnerTransfer
                    {
                        ListName = "cit-sys-srvc-L",
                        CurrentOwner = "tco2",
                        NewOwner = "wth1",
                        RequestIdentifier = Guid.NewGuid(),
                        RequestTicketID = -10000,
                        AcceptTicketID = -10008,
                        WhenCreated = DateTime.UtcNow,
                        WhenChanged = DateTime.UtcNow.AddMinutes(18),
                        Status = "APPROVED",
                        RequestStatusDetail = "wth1 has accepted the transfer request."
                    };

                    context.ElistOwnerTransfers.Add(elistOwnerTransfer);

                    elistOwnerTransfer = new ElistOwnerTransfer
                    {
                        ListName = "cit-msg-L",
                        CurrentOwner = "tco2",
                        NewOwner = "pb10",
                        RequestIdentifier = Guid.NewGuid(),
                        RequestTicketID = -10000,
                        AcceptTicketID = -10008,
                        WhenCreated = DateTime.UtcNow,
                        WhenChanged = DateTime.UtcNow.AddMinutes(0),
                        Status = "PENDING",
                        RequestStatusDetail = "pb10 has not yet accepted the transfer request."
                    };

                    context.ElistOwnerTransfers.Add(elistOwnerTransfer);

                    elistOwnerTransfer = new ElistOwnerTransfer
                    {
                        ListName = "todd-test-todd-L",
                        CurrentOwner = "tco2",
                        NewOwner = "wth1",
                        RequestIdentifier = Guid.NewGuid(),
                        RequestTicketID = -10024,
                        AcceptTicketID = -10038,
                        WhenCreated = DateTime.UtcNow,
                        WhenChanged = DateTime.UtcNow.AddMinutes(78),
                        Status = "CANCELLED",
                        RequestStatusDetail = "wht1 has declined the transfer request."
                    };

                    context.ElistOwnerTransfers.Add(elistOwnerTransfer);

                    context.SaveChanges();
                }
            }

            if (DoNotRun)
            {
                using (OAuth2AuthenticationContext context = new OAuth2AuthenticationContext())
                {
                    Boolean ProvisionRoles = true;

                    if (ProvisionRoles)
                    {
                        List<String> Roles = new List<String>()
                    {
                        "COEAWebAPIReadWrite",
                        "COEAWebAPIRead",
                        "TDXWorkflowCOEAWebAPI",
                        "EmailRecipientManagementWebAPIReadWrite",
                        "EmailRecipientWebAPIRead",
                        "EmailSharedAccountsWebAPIReadWrite",
                        "EmailSharedAccountWebAPIRead",
                        "Office365ContactManagementWebAPIReadWrite",
                        "Office365ContactManagementWebAPIRead",
                        "Office365DistributionGroupsWebAPIReadWrite",
                        "Office365DistributionWebAPIGroupsRead",
                        "Office365LicensingWebAPIReadWrite",
                        "Office365LicensingWebAPIRead",
                        "ListServiceContactWebAPIReadWrite",
                        "ListServiceContactWebAPIRead",
                        "ListServiceOwnerTransferWebAPIReadWrite",
                        "TDXWorkflowListServiceWebAPI"
                    };

                        List<String> RoleDescriptions = new List<String>()
                    {
                        "COEA WebAPI Read Write Access",
                        "COEA WebAPI Read Access",
                        "COEA TDX Workflow Read Write Access",
                        "Email Recipient Configuration Management Read Write Access",
                        "Email Recipient Configuration Management Read Access",
                        "Email Shared Account Management Read Write Access",
                        "Email Shared Account Management Read Access",
                        "Office 365 Contact Management Read Write Access",
                        "Office 365 Contact Management Read Access",
                        "Office 365 Distribution Group Management Read Write Access",
                        "Office 365 Distribution Group Management Read Access",
                        "Office 365 Licensing Read Write Access",
                        "Office 365 Licensing Read Access",
                        "List Manager Service Contact Read Write Access",
                        "List Manager Service Contact Read",
                        "List Manager Service Owner Transfer",
                        "List Manager Service TDX Workflow Read Write Access"
                    };

                        for (int index = 0; index < Roles.Count; index++)
                        {
                            OAuth2ClientRole oAuth2ClientRole = new OAuth2ClientRole()
                            {
                                RoleName = Roles[index],
                                RoleDescription = RoleDescriptions[index],
                                WhenCreated = DateTime.UtcNow
                            };

                            context.OAuth2ClientRoles.Add(oAuth2ClientRole);
                            context.SaveChanges();
                        }
                    }
                }
            }

            if (DoNotRun)
            {
                while (true)
                {
                    using (ActiveDirectoryTopology topology = new ActiveDirectoryTopology())
                    {
                        using (ActiveDirectoryManagementDatabaseAccess activeDirectoryManagementDatabaseAccess = new ActiveDirectoryManagementDatabaseAccess(topology))
                        {
                            DomainControllerUSNQueryRange domainControllerUSNQueryRange = activeDirectoryManagementDatabaseAccess.SelectSiteDomainControler();

                            ActiveDirectoryContext activeDirectoryContext = new ActiveDirectoryContext();
                            Console.WriteLine("------------------------");
                            Console.WriteLine("Querying Domain Controller: {0} with {1}", domainControllerUSNQueryRange.DomainControllerName, domainControllerUSNQueryRange.ADearchFilter);

                            activeDirectoryManagementDatabaseAccess.CreateConfigurationTasksForActiveDirecoryObjects(activeDirectoryContext.SearchDirectory(domainControllerUSNQueryRange));

                            foreach (ActiveDirectoryEntity activeDirectoryEntity in activeDirectoryContext.SearchDirectory(domainControllerUSNQueryRange))
                            {
                                Console.WriteLine("{0}  :  {1}", activeDirectoryEntity.objectGUID, activeDirectoryEntity.distinguishedName);
                            }
                        }
                        ///------------
                        using (OnPremisesExchangeManagementDatabaseAccess onPremisesExchangeManagementDatabaseAccess = new OnPremisesExchangeManagementDatabaseAccess())
                        {
                            {
                                foreach (Int32 configurationTask_Id in onPremisesExchangeManagementDatabaseAccess.GetConfigurationTasks())
                                {
                                    ConfigurationTask configurationTask = onPremisesExchangeManagementDatabaseAccess.GetConfigurationTask(configurationTask_Id);

                                    switch (configurationTask.ConfigurationTaskStatus.Status)
                                    {
                                        case "NEW":
                                            break;

                                        case "PENDING":
                                            break;

                                        case "INPROCESS":
                                            break;

                                        case "TEMPFAIL":
                                            break;

                                        case "RETRY":
                                            break;

                                        case "COMPLETE":
                                            break;

                                        case "FAILED":
                                            break;

                                        case "CANCELLED":
                                            break;
                                        // Unimplmented Task Status.
                                        default:
                                            break;
                                    }
                                }
                            }
                        }
                        Thread.Sleep(Convert.ToInt32(new TimeSpan(0, 0, 15).TotalMilliseconds));
                    }
                }
            }

            if (DoNotRun)
            {
                WindowsEventLogClient windowsEventLogClient = new WindowsEventLogClient("ApplicationServicesConfigurationManagement", "ApplicationServicesConfigurationManagement");
                windowsEventLogClient.AddEventDetail("UserprincipalName", "William");
                windowsEventLogClient.AddEventDetail("Affiliation", "Staff");
                windowsEventLogClient.WriteEventLogEntry(System.Diagnostics.EventLogEntryType.Warning, 1000, "This is a message");
            }
        }
    }
}