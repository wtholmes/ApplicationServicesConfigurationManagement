using ActiveDirectoryAccess;
using ListServiceManagement.Models;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using TDXManager;

namespace ListOwnerTransfersAutomation
{
    internal class ListOwnerTransfersAutomation
    {
        private static void Main(string[] args)
        {
            String LogFileName = String.Format(@".\LogFiles\{0}_Log.txt", DateTime.UtcNow.ToString("yyyyMMddhh"));

            Regex InactiveTicketsRegex = new Regex(@"(Reopened|Resolved|Closed|Canceled)", RegexOptions.IgnoreCase);

            TDXTicketManager tDXTicketManager = new TDXTicketManager();
            ListServiceManagementContext context = new ListServiceManagementContext();
            ActiveDirectoryContext activeDirectoryContext = new ActiveDirectoryContext();

            using (StreamWriter logfile = File.AppendText(LogFileName))
            {
                tDXTicketManager.GetTicketsUsingReport("Automated E-List Owner Transfer Requests");

                logfile.WriteLine("\n\n[{0} UTC]: There are {1} active tickets to process.", DateTime.UtcNow.ToString(), tDXTicketManager.TDXTickets.Count);
                Console.WriteLine("\n\n[{0} UTC]: There are {1} active tickets to process.", DateTime.UtcNow.ToString(), tDXTicketManager.TDXTickets.Count); ;
                foreach (var ticket in tDXTicketManager.TDXTickets)
                {
                    try
                    {
                        if (ticket != null)
                        {
                            tDXTicketManager.TDXTicket = ticket;

                            String ticketStatus = ticket.StatusName;
                            if (!InactiveTicketsRegex.IsMatch(ticketStatus))
                            {
                                logfile.WriteLine("\nProcessing Ticket [{0}]: {1}", ticket.ID, ticket.Title);
                                Console.WriteLine("\nProcessing Ticket [{0}]: {1}", ticket.ID, ticket.Title);

                                // Get the request properties.

                                TeamDynamix.Api.Users.User CreatedUser = tDXTicketManager.CreatingUser;
                                TeamDynamix.Api.Users.User RequestorUser = tDXTicketManager.RequestingUser;
                                String CurrentEListOwnerID = ticket.Attributes.Where(a => a.Name.Equals("S154-CURRENTLISTOWNER")).Select(a => a.Value).FirstOrDefault();
                                String NewElistOwnerID = ticket.Attributes.Where(a => a.Name.Equals("S154-NEWLISTOWNER")).Select(a => a.Value).FirstOrDefault();
                                String ElistName = ticket.Attributes.Where(a => a.Name.Equals("S154-LISTNAME")).Select(a => a.Value).FirstOrDefault();
                                ElistOwnerTransfer elistOwnerTransfer = context.ElistOwnerTransfers.Where(t => t.RequestTicketID.Equals(ticket.ID)).FirstOrDefault();
                                // S154-ListOwnerTransferAutomationStatus indicates the current status of work flows in the request ticket.
                                String AutomationStatus = ticket.Attributes.Where(a => a.Name.Equals("S154-ListOwnerTransferAutomationStatus")).Select(a => a.ValueText).FirstOrDefault();

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
                                        logfile.WriteLine("     --- Ticket Status: New");
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
                                            // Create a Patch document to update the Title and Description of this Request Ticket).
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

                                            logfile.WriteLine("     --- Ticket Patched: Updated Title and Description and Current E-List Owner.");
                                            Console.WriteLine("     --- Ticket Patched: Updated Title and Description and Current E-List Owner.");
                                        }
                                        else
                                        {
                                            // ============
                                            // Cancel this Request Ticket because no New Owner was specified.
                                            String TicketUpdate = File.ReadAllText(@".\Messages\RequestNoOwnerSpecifiedTemplate.txt");
                                            TicketUpdate = TicketUpdate.Replace("%%%-LISTNAME-%%%", ElistName);
                                            tDXTicketManager.NotificationEmails.Clear();
                                            tDXTicketManager.NotifyCreator = true;
                                            tDXTicketManager.NotifyRequestor = true;
                                            tDXTicketManager.UpdateTicket(TicketUpdate, "Cancelled");

                                            logfile.WriteLine("     --- Ticket Canceled: No new onwer provided.");
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
                                    String CreatedUserCompare = "";
                                    if (CreatedUser.UserName.Length > 0)
                                    {
                                        CreatedUserCompare = CreatedUser.UserName;
                                    }
                                    else
                                    {
                                        CreatedUserCompare = CreatedUser.PrimaryEmail;
                                    }

                                    if (String.Format("{0}@cornell.edu", elistContact.OwnerNetID).Equals(CreatedUserCompare, StringComparison.OrdinalIgnoreCase))
                                    {
                                        logfile.WriteLine("     --- The Request's Creator: {0} is the Elist owner.", ticket.CreatedEmail);
                                        Console.WriteLine("     --- The Request's Creator: {0} is the Elist owner.", ticket.CreatedEmail);
                                        TransferRequestValid = true;
                                    }

                                    // ============
                                    // Check if the Request Creator is the current Elist sponsor.
                                    if (!TransferRequestValid)
                                    {
                                        if (String.Format("{0}@cornell.edu", elistContact.SponsorNetID).Equals(CreatedUserCompare, StringComparison.OrdinalIgnoreCase))
                                        {
                                            logfile.WriteLine("     --- The Request's Creator: {0} is the Elist sponsor.", ticket.CreatedEmail);
                                            Console.WriteLine("     --- The Request's Creator: {0} is the Elist sponsor.", ticket.CreatedEmail);
                                            TransferRequestValid = true;
                                        }
                                    }
                                    // ============
                                    // Check if the Request Creator is the current EList owner's manager.
                                    if (!TransferRequestValid)
                                    {
                                        try
                                        {
                                            var activeDirectoryEntity = activeDirectoryContext.SearchDirectory("userPrincipalName", String.Format("{0}@cornell.edu", elistContact.OwnerNetID));
                                            if (activeDirectoryEntity != null)
                                            {
                                                if (activeDirectoryEntity.Count == 1)
                                                {
                                                    if (activeDirectoryEntity[0].directoryProperties.ContainsKey("manager"))
                                                    {
                                                        String ManagerDN = activeDirectoryEntity[0].directoryProperties["manager"][0].ToString();
                                                        String managerUserPrincipalName = activeDirectoryContext.SearchDirectory("distinguishedName", ManagerDN)[0].userPrincipalName[0].ToString();

                                                        if (managerUserPrincipalName.Equals(CreatedUser.UserName, StringComparison.OrdinalIgnoreCase))
                                                        {
                                                            logfile.WriteLine("     --- The Request's Creator: {0} is the Elist owner's manager.", ticket.CreatedEmail);
                                                            Console.WriteLine("     --- The Request's Creator: {0} is the Elist owner's manager.", ticket.CreatedEmail);
                                                            TransferRequestValid = true;
                                                        }
                                                    }
                                                    // If the current owner does not have a manager then check the affiliation of the person who created the request.
                                                    // If that person has an appropriate primary affiliation then allow the request. This change handles cases where
                                                    // the current owner is not available to initiate the transfer.
                                                    else
                                                    {
                                                        var createdUser = activeDirectoryContext.SearchDirectory("userPrincipalName", String.Format("{0}", CreatedUserCompare));
                                                        if (createdUser != null)
                                                        {
                                                            String PrimaryAffiliation = "NONE";
                                                            if (createdUser[0].directoryProperties.ContainsKey("cornelleduPrimaryAffiliation"))
                                                            {
                                                                PrimaryAffiliation = createdUser[0].directoryProperties["cornelleduPrimaryAffiliation"].ToString();
                                                                Regex trasferableFromRegex = new Regex(@"^(alumni|applicant|associate|exception|former postdoc|retired faculty|retiree|student|NONE)$", RegexOptions.IgnoreCase);
                                                                if (!trasferableFromRegex.IsMatch(PrimaryAffiliation))
                                                                {
                                                                    TransferRequestValid = true;
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }

                                        }
                                        catch (Exception exp)
                                        {

                                        }
                                    }

                                    // ============
                                    // Check if the current owner's affiliation is a student if so it can be transferred by any academic, faculty, or staff.
                                    if (!TransferRequestValid)
                                    {
                                        try
                                        {
                                            var currentOwnerDirectoryEntity = activeDirectoryContext.SearchDirectory("userPrincipalName", String.Format("{0}@cornell.edu", elistContact.OwnerNetID));
                                            if (currentOwnerDirectoryEntity != null)
                                            {
                                                if (currentOwnerDirectoryEntity.Count == 1)
                                                {
                                                    // This will handle the case of an abandoned list. If a user is unaffiliated we will
                                                    // allow the trasfer to be initiated by any affiliate|academic|emeritus|faculty|staff.

                                                    String PrimaryAffiliation = "NONE";
                                                    if (currentOwnerDirectoryEntity[0].directoryProperties.ContainsKey("cornelleduPrimaryAffiliation"))
                                                    {
                                                        PrimaryAffiliation = currentOwnerDirectoryEntity[0].directoryProperties["cornelleduPrimaryAffiliation"].ToString();
                                                    }

                                                    Regex trasferableFromRegex = new Regex(@"^(alumni|applicant|associate|exception|former postdoc|retired faculty|retiree|student|NONE)$", RegexOptions.IgnoreCase);
                                                    if (trasferableFromRegex.IsMatch(PrimaryAffiliation))
                                                    {
                                                        String CreatedUserLookupName = "";
                                                        if (CreatedUser.UserName.Length != 0)
                                                        {
                                                            CreatedUserLookupName = CreatedUser.UserName;
                                                        }
                                                        else
                                                        {
                                                            CreatedUserLookupName = CreatedUser.PrimaryEmail;
                                                        }
                                                        var creatorActiveDirectoryEntity = activeDirectoryContext.SearchDirectory("userPrincipalName", CreatedUserLookupName);
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
                                        catch (Exception exp)
                                        {
                                        }
                                    }

                                    // ============
                                    // Check that the new owner's affiliation is valid for list ownership.
                                    Boolean InvalidNewOwnerAffiliation = false;
                                    try
                                    {
                                        String LookupUserName = "";
                                        if (NewOwner.UserName.Length > 0)
                                        {
                                            LookupUserName = NewOwner.UserName;
                                        }
                                        else
                                        {
                                            LookupUserName = NewOwner.PrimaryEmail;
                                        }
                                        var proposedNewOwnerAdEntity = activeDirectoryContext.SearchDirectory("userPrincipalName", LookupUserName);
                                        if (proposedNewOwnerAdEntity != null)
                                        {
                                            if (proposedNewOwnerAdEntity.Count == 1)
                                            {
                                                if (proposedNewOwnerAdEntity[0].directoryProperties.ContainsKey("cornelleduPrimaryAffiliation"))
                                                {
                                                    Regex allowedAffiliations = new Regex(@"^(academic|affiliate|emeritus|faculty|staff|student|temporary)$", RegexOptions.IgnoreCase);
                                                    if (!allowedAffiliations.IsMatch(proposedNewOwnerAdEntity[0].directoryProperties["cornelleduPrimaryAffiliation"][0]))
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
                                    }
                                    catch (Exception exp)
                                    {
                                    }

                                    #endregion ------ Request Validation Checks ------

                                    #region ---- Automation Processing ----

                                    logfile.WriteLine("     --- TDX Automation Status: {0}", AutomationStatus);
                                    Console.WriteLine("     --- TDX Automation Status: {0}", AutomationStatus);

                                    switch (AutomationStatus.ToUpper())
                                    {
                                        #region ---- Automation Status New ----

                                        case "NEW":

                                            if (TransferRequestValid) // Proceed with this request.
                                            {
                                                logfile.WriteLine("     --- New Transfer Valid");
                                                Console.WriteLine("     --- New Transfer Valid");

                                                if (AutomationStatus.Equals("NEW", StringComparison.OrdinalIgnoreCase))
                                                {
                                                    String TicketUpdate = File.ReadAllText(@".\Messages\RequestApproved.txt");
                                                    TicketUpdate = TicketUpdate.Replace("%%%-NETID-%%%", NewOwner.UserName.Split('@')[0]);
                                                    TicketUpdate = TicketUpdate.Replace("%%%-LISTNAME-%%%", elistContact.ListName);

                                                    // Update the ticket and set the ticket workflow.
                                                    tDXTicketManager.NotificationEmails.Clear();
                                                    tDXTicketManager.NotifyCreator = true;
                                                    tDXTicketManager.NotifyRequestor = true;
                                                    tDXTicketManager.UpdateTicket(TicketUpdate, "In Process");

                                                    logfile.WriteLine("     --- Assigning New Owner Acceptance Workflow to the request and notifying.");
                                                    Console.WriteLine("     --- Assigning New Owner Acceptance Workflow to the request and notifying.");
                                                    tDXTicketManager.SetTicketWorkflow(455847);

                                                    TeamDynamix.Api.Tickets.Ticket patchedTicket = tDXTicketManager.UpdateDropDownChoiceAttribute("S154-ListOwnerTransferAutomationStatus", "APPROVED");
                                                    AutomationStatus = patchedTicket.Attributes.Where(a => a.Name.Equals("S154-ListOwnerTransferAutomationStatus")).Select(a => a.ValueText).FirstOrDefault();
                                                    logfile.WriteLine("     --- Valid Request. Now Awaiting New Owner Accpetance From: {0}  {1}", NewOwner.FullName, NewOwner.UserName);
                                                    Console.WriteLine("     --- Valid Request. Now Awaiting New Owner Accpetance From: {0}  {1}", NewOwner.FullName, NewOwner.UserName);
                                                }
                                            }
                                            else // Cancel this request it is invalid.
                                            {
                                                logfile.WriteLine("     --- New Transfer Is Not Valid");
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
                                                    TicketUpdate = TicketUpdate.Replace("%%%-CURRENTOWNERNETID-%%%", CurrentOwner.PrimaryEmail.Split('@')[0]);
                                                }
                                                else
                                                {
                                                    TicketUpdate = TicketUpdate.Replace("%%%-CURRENTOWNERNETID-%%%", elistContact.OwnerNetID);
                                                }
                                                TicketUpdate = TicketUpdate.Replace("%%%-CREATORFULLNAME-%%% ", CreatedUser.FullName);
                                                tDXTicketManager.NotificationEmails.Clear();
                                                tDXTicketManager.NotifyCreator = true;
                                                tDXTicketManager.NotifyRequestor = true;
                                                tDXTicketManager.UpdateTicket(TicketUpdate, "Cancelled");
                                                tDXTicketManager.UpdateResponsibleGroup(45);

                                                logfile.WriteLine("     --- Invalid request. Requestor is not authorized. Cancelling the request and sending notifications.");
                                                Console.WriteLine("     --- Invalid request. Requestor is not authorized. Cancelling the request and sending notifications.");
                                            }

                                            break;

                                        #endregion ---- Automation Status New ----

                                        #region ---- Automation Status Approved ----

                                        case "APPROVED":
                                            logfile.WriteLine("     --- Awaiting New Owner Accpetance From: {0}  {1}", NewOwner.FullName, NewOwner.UserName);
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
                                                logfile.WriteLine("     --- New Onwer has accepted adding transfer work item to backend queue.");
                                                Console.WriteLine("     --- New Onwer has accepted adding transfer work item to backend queue.");
                                            }
                                            else
                                            {
                                                #region ---- Check Backend Transfer Status ----

                                                logfile.WriteLine("     --- Backend Transfer Status: {0} Last Update: ({1} UTC)", elistOwnerTransfer.Status, elistOwnerTransfer.WhenChanged);
                                                Console.WriteLine("     --- Backend Transfer Status: {0} Last Update: ({1} UTC)", elistOwnerTransfer.Status, elistOwnerTransfer.WhenChanged);
                                                switch (elistOwnerTransfer.Status)
                                                {
                                                    case "APPROVED":
                                                        logfile.WriteLine("     --- Awaiting Backend Processing: In Queue: [{0:D4}] hours.)", Math.Truncate((DateTime.UtcNow - elistOwnerTransfer.WhenCreated).TotalHours).ToString());
                                                        Console.WriteLine("     --- Awaiting Backend Processing. In Queue: [{0:D4}] hours.)", Math.Truncate((DateTime.UtcNow - elistOwnerTransfer.WhenCreated).TotalHours).ToString());
                                                        break;

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
                                                                CompletionMessage = CompletionMessage.Replace("%%%-LISTINSTANCEURL-%%%", "https://www.bulk.mail.cornell.edu");
                                                                break;

                                                            case "bulk.list.cornell.edu":
                                                                CompletionMessage = CompletionMessage.Replace("%%%-LISTINSTANCEURL-%%%", "https://www.bulk.mail.cornell.edu");
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
                                                        logfile.WriteLine("     --- Backend transfer complete. Resolving the request and sending notifications.");
                                                        Console.WriteLine("     --- Backend transfer complete. Resolving the request and sending notifications.");
                                                        break;

                                                    #endregion ---- Check Backend Transfer Status ----

                                                    case "CANCELLED":
                                                        String CancelledUpdate = String.Format("The E-List Ownership Transfer Request for: {0} has been cancelled.\n\n{1}",
                                                            ElistName,
                                                            elistOwnerTransfer.RequestStatusDetail);

                                                        tDXTicketManager.NotificationEmails.Clear();
                                                        tDXTicketManager.NotifyCreator = true;
                                                        tDXTicketManager.NotifyRequestor = true;
                                                        tDXTicketManager.AddNotificationEmail(NewOwner.PrimaryEmail, false);
                                                        tDXTicketManager.UpdateTicket(CancelledUpdate, "Cancelled");
                                                        tDXTicketManager.UpdateResponsibleGroup(45);

                                                        logfile.WriteLine("     --- Backend transfer cancelled. Cancelling the request and sending notifications.");
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
                                            tDXTicketManager.NotificationEmails.Clear();
                                            tDXTicketManager.NotifyCreator = true;
                                            tDXTicketManager.NotifyRequestor = true;
                                            tDXTicketManager.UpdateTicket(DeclineUpdate, "Cancelled");
                                            tDXTicketManager.UpdateResponsibleGroup(45);

                                            logfile.WriteLine("     --- New owner declined transfer request. Cancelling the request and sending notifications.");
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

                                #region ---- Cancellation Conditions ----

                                #region ---- List has been removed ----

                                // The Elist Contact does not exist but there is already a valid transfer in progress.
                                else if (elistOwnerTransfer != null && elistContact == null)
                                {
                                    if (elistOwnerTransfer.Status.Equals("CANCELLED"))
                                    {
                                        tDXTicketManager.NotificationEmails.Clear();
                                        String BackEndError = String.Format("The following Error Has Occurred:\n\n {0}", elistOwnerTransfer.RequestStatusDetail);
                                        tDXTicketManager.UpdateTicket(BackEndError, true);

                                        String TicketUpdate = File.ReadAllText(@".\Messages\ListHasBeenRemoveAfterRequest.txt");
                                        TicketUpdate = TicketUpdate.Replace("%%%-LISTNAME-%%%", ElistName);
                                        TicketUpdate = TicketUpdate.Replace("%%%-CREATORNETID-%%%", CreatedUser.UserName.Split('@')[0]);
                                        TicketUpdate = TicketUpdate.Replace("%%%-CURRENTOWNERNETID-%%%", "N/A");
                                        TicketUpdate = TicketUpdate.Replace("%%%-REQUESTSTATUSDETAIL-%%%", elistOwnerTransfer.RequestStatusDetail);

                                        //Todo:  Investigate removing the workflow at this stage.

                                        tDXTicketManager.NotificationEmails.Clear();
                                        tDXTicketManager.AddNotificationEmail(tDXTicketManager.GetTDXUserByUID(NewElistOwnerID).PrimaryEmail, true);
                                        tDXTicketManager.NotifyCreator = true;
                                        tDXTicketManager.NotifyRequestor = true;
                                        tDXTicketManager.UpdateTicket(TicketUpdate, "Cancelled");
                                        tDXTicketManager.UpdateResponsibleGroup(45);
                                        logfile.WriteLine("     --- Invalid ListName received. Cancelling the request and sending notifications.");
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

                                // The Elist Contact does not exist but a valid request was never created.
                                else if (!AutomationStatus.Equals("NEW", StringComparison.OrdinalIgnoreCase) && elistContact == null)
                                {
                                    String TicketUpdate = File.ReadAllText(@".\Messages\ListHasBeenRemoveAfterRequest.txt");
                                    TicketUpdate = TicketUpdate.Replace("%%%-LISTNAME-%%%", ElistName);
                                    TicketUpdate = TicketUpdate.Replace("%%%-CREATORNETID-%%%", CreatedUser.UserName.Split('@')[0]);
                                    TicketUpdate = TicketUpdate.Replace("%%%-CURRENTOWNERNETID-%%%", "N/A");

                                    tDXTicketManager.NotificationEmails.Clear();
                                    tDXTicketManager.AddNotificationEmail(tDXTicketManager.GetTDXUserByUID(NewElistOwnerID).PrimaryEmail, true);
                                    tDXTicketManager.NotifyCreator = true;
                                    tDXTicketManager.NotifyRequestor = true;
                                    tDXTicketManager.UpdateTicket(TicketUpdate, "Cancelled");
                                    tDXTicketManager.UpdateResponsibleGroup(45);

                                    logfile.WriteLine("     --- Invalid ListName received. Cancelling the request and sending notifications.");
                                    Console.WriteLine("     --- Invalid ListName received. Cancelling the request and sending notifications.");
                                }

                                #endregion ---- List has been removed ----

                                #region ---- List does not exist ----

                                // The Elist contact does not exist and there is not a valid transfer in progress.
                                else
                                {
                                    String TicketUpdate = File.ReadAllText(@".\Messages\RequestWithInvalidListName.txt");
                                    TicketUpdate = TicketUpdate.Replace("%%%-LISTNAME-%%%", ElistName);
                                    TicketUpdate = TicketUpdate.Replace("%%%-CREATORNETID-%%%", CreatedUser.UserName.Split('@')[0]);
                                    TicketUpdate = TicketUpdate.Replace("%%%-CURRENTOWNERNETID-%%%", "N/A");

                                    tDXTicketManager.NotificationEmails.Clear();
                                    tDXTicketManager.NotifyCreator = true;
                                    tDXTicketManager.NotifyRequestor = true;
                                    tDXTicketManager.UpdateTicket(TicketUpdate, "Cancelled");
                                    tDXTicketManager.UpdateResponsibleGroup(45);

                                    logfile.WriteLine("     --- Invalid ListName received. Cancelling the request and sending notifications.");
                                    Console.WriteLine("     --- Invalid ListName received. Cancelling the request and sending notifications.");
                                }

                                #endregion ---- List does not exist ----

                                #endregion ---- Cancelation Conditions ----
                            }
                        }
                    }
                    catch(Exception exp)
                    {

                    }
                }
            }
            context.Dispose();
            activeDirectoryContext.Dispose();
        }
    }
}