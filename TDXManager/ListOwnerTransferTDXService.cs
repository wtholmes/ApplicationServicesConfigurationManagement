using ListServiceManagement.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using TeamDynamix.Api.Tickets;
using TeamDynamix.Api.Users;

namespace TDXManager
{
    public partial class ListOwnerTransferTDXService : TDXTicketManager
    {
        #region ---- Public Class Properties ----

        public List<ListOwnerTransferTicket> ListOwnerTransferTickets { get; private set; }

        #endregion ---- Public Class Properties ----

        #region ---- Class Constructors ----

        public ListOwnerTransferTDXService()
        {
            // Inactive Ticket Statuses
            Regex InactiveTicketsRegex = new Regex(@"(Reopened|Resolved|Closed|Canceled)", RegexOptions.IgnoreCase);

            // List services management database context.
            ListServiceManagmentContext context = new ListServiceManagmentContext();

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
;

                    // Automation ID [TDX Custom Attribute: (S111-AUTOMATIONID)]
                    String automationID = ticket.Attributes.Where(attrib => attrib.Name.Equals("S111-AUTOMATIONID")).Select(attrib => attrib.Value).FirstOrDefault();

                    // The List Name [TDX Custom Attribute: (S154-LISTNAME)]
                    String listName = ticket.Attributes.Where(attrib => attrib.Name.Equals("S154-LISTNAME")).Select(attrib => attrib.Value).FirstOrDefault();

                    // Get the Elist Contact from the Elist Contacts Database.
                    ElistContact elistContact = context.ElistContacts.Where(contact => contact.ListName.Equals(listName)).FirstOrDefault();

                    // Current List Owner [TDX Custom Attribute: (S154-CURRENTLISTOWNER)]
                    User currentListOwner = GetTDXUserByUID(ticket.Attributes.Where(attrib => attrib.Name.Equals("S154-CURRENTLISTOWNER")).Select(attrib => attrib.Value).FirstOrDefault());
                    if (currentListOwner == null) // If null get the current list owner from the List Contacts Database.
                    {
                        if (elistContact != null)
                        {
                            currentListOwner = GetTDXUserByUserPrincipalName(String.Format("{0}@cornell.edu", elistContact.OwnerNetID));
                            UpdateAttribute("S154-CURRENTLISTOWNER", currentListOwner.UID.ToString());
                        }
                    }

                    // New List Owner [TDX Custom Attribute: (S154-NEWLISTOWNER)]
                    User newListOnwer = GetTDXUserByUID(ticket.Attributes.Where(attrib => attrib.Name.Equals("S154-NEWLISTOWNER")).Select(attrib => attrib.Value).FirstOrDefault());

                    // Automation Status [TDX Custom Attribute: (S154-ListOwnerTransferAutomationStatus)]
                    String automationStatus = ticket.Attributes.Where(a => a.Name.Equals("S154-ListOwnerTransferAutomationStatus")).Select(a => a.ValueText).FirstOrDefault();


                    ListOwnerTransferTicket listOwnerTransferTicket = new ListOwnerTransferTicket(ticket)
                    {
                        AutomationID = automationID,
                        ListName = listName,
                        CurrentListOwner = new TDXDomainUser(currentListOwner),
                        CurrentListSponsor = new TDXDomainUser(GetTDXUserByUserPrincipalName(String.Format("{0}@cornell.edu", elistContact.OwnerNetID))),
                        NewListOwner = new TDXDomainUser(newListOnwer),
                        AutomationStatus = automationStatus
                    };

                    ListOwnerTransferTickets.Add(listOwnerTransferTicket);
                }
            }
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
    }

    public class ListOwnerTransferTicket : Ticket
    {
        #region --- Public Properties ---

        // TDX Custom Attribute: (S111-AUTOMATIONID)
        public String AutomationID { get; set; }

        // TDX Custom Attribute: (S154-LISTNAME)
        public String ListName { get; set; }

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