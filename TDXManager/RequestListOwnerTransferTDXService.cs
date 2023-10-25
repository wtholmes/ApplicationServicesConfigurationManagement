using ListServiceManagement.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using TeamDynamix.Api.Tickets;
using TeamDynamix.Api.Users;

namespace TDXManager
{
    public partial class RequestListOwnerTransferTDXService : TDXTicketManager
    {
        #region ---- Private Class Properties ----

        private ListServiceManagementContext context;

        #endregion

        #region ---- Public Class Properties ----

        public ListOwnerTransferTicket TDXListOwnerTransferTicket
        {
            get 
            { 
                return GetListOwnerTransferTicket(this.TDXTicket); 
            }

            set
            {
                this.TDXTicket = new Ticket();

                // Use reflection to copy ListOwnerTransferTicket to the base ticket
                PropertyInfo[] ListOwnerTransferTicketProperties = value.GetType().GetProperties();

                foreach (PropertyInfo ListOwnerTransferTicketPropery in ListOwnerTransferTicketProperties)
                {
                    PropertyInfo TdxTicketPropery = this.TDXTicket.GetType().GetProperty(ListOwnerTransferTicketPropery.Name); 
                    if(TdxTicketPropery != null)
                    {
                        TdxTicketPropery.SetValue(this.TDXTicket, ListOwnerTransferTicketPropery.GetValue(value));
                    }
                }
            }
        }
        public List<ListOwnerTransferTicket> TDXListOwnerTransferTickets { get; private set; }
        
        #endregion ---- Public Class Properties ----

        #region ---- Class Constructors ----

        public RequestListOwnerTransferTDXService()
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            TDXListOwnerTransferTickets = new List<ListOwnerTransferTicket>();
            // Inactive Ticket Statuses
            Regex InactiveTicketsRegex = new Regex(@"(Reopened|Resolved|Closed|Canceled)", RegexOptions.IgnoreCase);

            // List services management database context.
            context = new ListServiceManagementContext();

            // ------
            // Get the list of tickets from TDX using the Automated E-List Owner Transfer Requests report.
            // This report returns all of the tickets that are using the:
            // Discussion and Announcement Email List / Transfer e-List Ownership (V2) TDX Form.
            // ------
            GetTicketsUsingReport("Automated E-List Owner Transfer Requests");

            // Populate ListOwnerTransferTickets ;
            foreach (Ticket ticket in this.TDXTickets)
            {
                String ticketStatus = ticket.StatusName;
                if (!InactiveTicketsRegex.IsMatch(ticketStatus))
                {
                    ListOwnerTransferTicket listOwnerTransferTicket = GetListOwnerTransferTicket(ticket);
                    TDXListOwnerTransferTickets.Add(listOwnerTransferTicket);
                }
            }
            stopwatch.Stop();

            Console.WriteLine("{0} List Owner Transfer Tickets Read in {1:N2} seconds.  Read Rate: {2:N2} Tickets/Second.",
                TDXListOwnerTransferTickets.Count,
                stopwatch.Elapsed.TotalSeconds,
                (TDXListOwnerTransferTickets.Count / stopwatch.Elapsed.TotalSeconds)
                ); 
        }

        #endregion ---- Class Constructors ----

        #region ---- Public Methods ----

        public String MergeTDXMessage(ListOwnerTransferTicket listOwnerTransferTicket, String message)
        {
            /*
            message = message.Replace("%%%-NEWOWNERNETID-%%%", listOwnerTransferTicket.NewListOwner.UserName);
            message = message.Replace("%%%-LISTNAME-%%%", listOwnerTransferTicket.ListName);

            message = message.Replace("%%%-LISTOWNERFULLNAME-%%%", listOwnerTransferTicket.message);
            message = message.Replace("%%%-BACKENDSTATUSMESSAGE-%%%", listOwnerTransferTicket.message);
            message = message.Replace("%%%-CREATORNETID-%%%", listOwnerTransferTicket.message);
            message = message.Replace("%%%-CREATORFULLNAME-%%%", listOwnerTransferTicket.message);
            message = message.Replace("%%%-CURRENTOWNERNETID-%%%", listOwnerTransferTicket.message);
            message = message.Replace("%%%-CURRENTOWNERFULLNAME-%%%", listOwnerTransferTicket.message);

            message = message.Replace("%%%-LISTINSTANCEURL-%%%", listOwnerTransferTicket.message);
            */
            return message;
        }

        #endregion ---- Public Methods ----

        #region ---- Private Methods ----

        private ListOwnerTransferTicket GetListOwnerTransferTicket(Ticket ticket)
        {
            if (ticket != null)
            {

                // Automation ID [TDX Custom Attribute: (S111-AUTOMATIONID)]
                String automationID = ticket.Attributes.Where(attrib => attrib.Name.Equals("S111-AUTOMATIONID")).Select(attrib => attrib.Value).FirstOrDefault();

                // The List Name [TDX Custom Attribute: (S154-LISTNAME)]
                String listName = ticket.Attributes.Where(attrib => attrib.Name.Equals("S154-LISTNAME")).Select(attrib => attrib.Value).FirstOrDefault();

                // Get the Elist Contact from the Elist Contacts Database.
                String ElistName = listName.Split('@')[0];

                ElistContact elistContact = context.ElistContacts.Where(contact => contact.ListName.Equals(ElistName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                Boolean ValidElist = false;
                TDXDomainUser CurrentListOwner = null;
                TDXDomainUser CurrentListSponsor = null;

                if (elistContact != null)
                {
                    ValidElist = true;
                    // Current List Owner [TDX Custom Attribute: (S154-CURRENTLISTOWNER)]
                    User currentListOwner = GetTDXUserByUID(ticket.Attributes.Where(attrib => attrib.Name.Equals("S154-CURRENTLISTOWNER")).Select(attrib => attrib.Value).FirstOrDefault());
                    if (currentListOwner != null)
                    {
                        CurrentListOwner = new TDXDomainUser(currentListOwner);
                    }
                    else
                    {
                        if (elistContact != null)
                        {
                            CurrentListOwner = new TDXDomainUser(GetTDXUserByUserPrincipalName(String.Format("{0}@cornell.edu", elistContact.OwnerNetID)));
                            UpdateAttribute("S154-CURRENTLISTOWNER", currentListOwner.UID.ToString());
                        }
                        ValidElist = true;
                        CurrentListSponsor = new TDXDomainUser(GetTDXUserByUserPrincipalName(String.Format("{0}@cornell.edu", elistContact.OwnerNetID)));
                    }
                }
                // New List Owner [TDX Custom Attribute: (S154-NEWLISTOWNER)]
                User newListOnwer = GetTDXUserByUID(ticket.Attributes.Where(attrib => attrib.Name.Equals("S154-NEWLISTOWNER")).Select(attrib => attrib.Value).FirstOrDefault());

                // Automation Status [TDX Custom Attribute: (S154-ListOwnerTransferAutomationStatus)]
                String automationStatus = ticket.Attributes.Where(a => a.Name.Equals("S154-ListOwnerTransferAutomationStatus")).Select(a => a.ValueText).FirstOrDefault();

                TDXDomainUser CreatingUser = new TDXDomainUser(GetTDXUserByUID(ticket.CreatedUid.ToString()));
                TDXDomainUser RequestingUser = new TDXDomainUser(GetTDXUserByUID(ticket.RequestorUid.ToString()));


                ListOwnerTransferTicket listOwnerTransferTicket = new ListOwnerTransferTicket(ticket)
                {
                    ValidElist = ValidElist,
                    AutomationID = automationID,
                    ListName = listName,
                    CurrentListOwner = CurrentListOwner,
                    CurrentListSponsor = CurrentListSponsor,
                    NewListOwner = new TDXDomainUser(newListOnwer),
                    AutomationStatus = automationStatus,
                    CreatingUser = CreatingUser,
                    RequestingUser = RequestingUser
                };
                return listOwnerTransferTicket;
            }
            else
            {
                return null;
            }
        }

        #endregion
    }

    public class ListOwnerTransferTicket : Ticket
    {
        #region --- Public Properties ---
        // Indicates if this Elist given in the ticket is valid.
        public Boolean ValidElist { get; set; }
        
        // TDX Custom Attribute: (S111-AUTOMATIONID)
        public String AutomationID { get; set; }

        // TDX Custom Attribute: (S154-LISTNAME)
        public String ListName { get; set; }

        public TDXDomainUser CreatingUser { get; set; }

        public TDXDomainUser RequestingUser { get; set; }

        // TDX Custom Attribute: (S154-CURRENTLISTOWNER)
        public TDXDomainUser CurrentListOwner { get; set; }

        // Current List Sponsor.
        public TDXDomainUser CurrentListSponsor { get; set; }

        // TDX Custom Attribute: (S154-NEWLISTOWNER)
        public TDXDomainUser NewListOwner { get; set; }

        // TDX Custom Attribute: (S154-ListOwnerTransferAutomationStatus)
        public String AutomationStatus { get; set; }

        // The domain part of
        public String ListDomain { get; set; }

        #endregion --- Public Properties ---

        #region ---- Public Constructor ----

        public ListOwnerTransferTicket(Ticket ticket)
        {
            // Use reflection to copy the base ticket to the ListOwnerTransferTicket
            PropertyInfo[] ticketProperties = ticket.GetType().GetProperties();
            foreach (PropertyInfo ticketProperty in ticketProperties)
            {
                PropertyInfo listOwnerTransferTicketProperty = this.GetType().GetProperty(ticketProperty.Name);
                listOwnerTransferTicketProperty.SetValue(this, ticketProperty.GetValue(ticket));
            }
        }
        #endregion
    }
}