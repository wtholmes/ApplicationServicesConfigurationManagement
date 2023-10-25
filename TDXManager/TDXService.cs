﻿using ActiveDirectoryAccess;
using ApplicationServicesConfigurationManagementDatabaseAccess;
using Marvin.JsonPatch;
using Marvin.JsonPatch.Dynamic;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mail;
using System.Reflection;
using System.Text;
using System.Threading;
using TeamDynamix.Api.Accounts;
using TeamDynamix.Api.Apps;
using TeamDynamix.Api.CustomAttributes;
using TeamDynamix.Api.Feed;
using TeamDynamix.Api.Forms;
using TeamDynamix.Api.PriorityFactors;
using TeamDynamix.Api.Reporting;
using TeamDynamix.Api.ServiceCatalog;
using TeamDynamix.Api.Tickets;
using TeamDynamix.Api.Users;

namespace TDXManager
{
    public partial class TDXTicketManager
    {
        #region ---- Private Class Properties ----

        private HttpClient oHttpClientX;

        private String sLocationOrigin = "https://tdx.cornell.edu/";

        //private String sWebApiBasePathname = "SBTDWebApi/api/";
        private String sWebApiBasePathname = "TDWebApi/api/";

        private String ApplicationID = "32";
        private String sUsername = "messagingteam-api-01@cornell.edu";
        private String sPassword = "+o}Q6$(uE5Dj";
        private String BearerToken;
        private TeamDynamixManagementContext teamDynamixManagementContext;
        private Ticket _TDXTicket;

        #endregion ---- Private Class Properties ----

        #region ---- Public Class Properties ---

        #region ---- List or MultiValue Properties ----

        public Dictionary<String, User> TDXUserCache;

        public List<Account> TDXAccounts { get; private set; }

        public List<OrgApplication> TDXOrgApplications { get; private set; }

        public List<Service> TDXServices { get; private set; }

        public List<Group> TDXGroups { get; private set; }

        public List<Ticket> TDXTickets { get; private set; }

        public List<TDXAutomationTicket> TDXAutomationTickets { get; private set; }

        public List<TDXDomainUser> TDXDomainUsers { get; private set; }

        public List<Form> TDXTicketForms { get; private set; }

        public List<Exception> Exceptions { get; private set; }

        public Exception LastException
        { get { return Exceptions.LastOrDefault(); } }

        #endregion ---- List or MultiValue Properties ----

        #region ---- Ticket Specific Properties ----

        public TDXAutomationTicket TDXAutomationTicket { get; private set; }

        public Ticket TDXTicket
        {
            get
            {
                return _TDXTicket;
            }
            set
            {
                _TDXTicket = value;
                this.TDXAutomationTicket = new TDXAutomationTicket(_TDXTicket);

                this.TDXAutomationTicket.SetTicketCreator(new TDXDomainUser(GetTDXUserByUID(_TDXTicket.CreatedUid.ToString())));
                this.CreatingUser = GetTDXUserByUID(_TDXTicket.CreatedUid.ToString());

                this.TDXAutomationTicket.SetTicketRequestor(new TDXDomainUser(GetTDXUserByUID(_TDXTicket.RequestorUid.ToString())));
                this.RequestingUser = GetTDXUserByUID(_TDXTicket.RequestorUid.ToString());

                this.NotifyCreator = false;
                this.NotifyRequestor = false;
                this.UpdateTicketStatus = false;
                this.NotificationEmails.Clear();
            }
        }

        public Service TDXService { get; private set; }

        public Account TDXAccount { get; private set; }

        public OrgApplication TDXOrgAppication { get; private set; }

        public Boolean NotifyCreator { get; set; }

        public Boolean NotifyRequestor { get; set; }

        public List<String> NotificationEmails { get; private set; }

        public Boolean UpdateTicketStatus { get; private set; }

        public List<CustomAttribute> TDXTicketCustomAttributes { get; private set; }

        public CustomAttribute TDXTicketCustomAttribute { get; private set; }

        public Form TDXTicketForm { get; private set; }

        public List<ItemUpdate> TDXItemUpdates { get; private set; }

        public ItemUpdate TDXItemUpdate { get; private set; }

        public List<Priority> TDXTicketPriorities { get; private set; }

        public Priority TicketPriority { get; private set; }

        public List<Report> TDXReports { get; private set; }

        public List<TicketSource> TDXTicketSources { get; private set; }

        public TicketSource TicketSource { get; private set; }

        public List<TicketStatus> TDXTicketStatuses { get; private set; }

        public TicketStatus TicketStatus { get; private set; }

        public User CreatingUser { get; private set; }

        public User ModifyingUser { get; private set; }

        public User RequestingUser { get; private set; }

        public User RespondingUser { get; private set; }

        public User ResponsibleUser { get; private set; }

        public User ReviewingUser { get; private set; }

        public User CompletingUser { get; private set; }

        public User ConvertingToTaskUser { get; private set; }

        public User TDXUser { get; private set; }

        #endregion ---- Ticket Specific Properties ----

        #endregion ---- Public Class Properties ---

        #region ---- Class Constructor ---

        /// <summary>
        /// Default constructor.
        /// </summary>
        public TDXTicketManager()
        {
            NotificationEmails = new List<String>();
            Exceptions = new List<Exception>();
            UpdateTicketStatus = false;
            TDXUserCache = new Dictionary<String, User>();
            teamDynamixManagementContext = new TeamDynamixManagementContext();
            TDXAutomationTickets = new List<TDXAutomationTicket>();
            TDXDomainUsers = new List<TDXDomainUser>();

            try
            {
                oHttpClientX = new HttpClient();
                oHttpClientX.BaseAddress = new Uri(sLocationOrigin + sWebApiBasePathname);
                oHttpClientX.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                BearerToken = oHttpClientX.PostAsJsonAsync("auth/login", new { username = sUsername, password = sPassword }).Result.Content.ReadAsStringAsync().Result;
                oHttpClientX.DefaultRequestHeaders.Add("Authorization", "Bearer " + BearerToken);

                GetTDXForms();
                GetTDXAccounts();
                //GetTDXGroups();
                GetTDXReports();
                GetTDXServices();
                GetTDXCustomTicketAttributes();
                GetTDXTicketStatuses();
                GetTDXTicketPriorities();
                GetTDXTicketSources();
            }
            catch (Exception exp)
            {
                Exceptions.Add(exp);
            }
        }

        #endregion ---- Class Constructor ---

        #region ---- Private Methods ----

        /// <summary>
        /// Get the list of Accounts from TeamDynamix
        /// </summary>
        private void GetTDXAccounts()
        {
            try
            {
                String ApiUri = String.Format("accounts");
                HttpResponseMessage httpResponseMessage = oHttpClientX.GetAsync(ApiUri).Result;
                if (httpResponseMessage.IsSuccessStatusCode)
                {
                    String httpResponseMessageContent = httpResponseMessage.Content.ReadAsStringAsync().Result;
                    TDXAccounts = JsonConvert.DeserializeObject<List<Account>>(httpResponseMessageContent);
                }
            }
            catch (Exception exp)
            {
                Exceptions.Add(exp);
            }
        }

        /// <summary>
        /// Get the list of Applications from TeamDynamix
        /// </summary>
        private void GetTDXApplications()
        {
            try
            {
                String ApiUri = String.Format("applications", ApplicationID);
                HttpResponseMessage httpResponseMessage = oHttpClientX.GetAsync(ApiUri).Result;
                if (httpResponseMessage.IsSuccessStatusCode)
                {
                    String httpResponseMessageContent = httpResponseMessage.Content.ReadAsStringAsync().Result;
                    TDXOrgApplications = JsonConvert.DeserializeObject<List<OrgApplication>>(httpResponseMessageContent);
                }
            }
            catch (Exception exp)
            {
                Exceptions.Add(exp);
            }
        }

        /// <summary>
        /// Get the Custom Attributes from TDX.
        /// </summary>
        private void GetTDXCustomTicketAttributes()
        {
            try
            {
                String ApiUri = String.Format("attributes/custom?componentId={0}", CustomAttributeComponent.Ticket);
                HttpResponseMessage httpResponseMessage = oHttpClientX.GetAsync(ApiUri).Result;
                if (httpResponseMessage.IsSuccessStatusCode)
                {
                    String httpResponseMessageContent = httpResponseMessage.Content.ReadAsStringAsync().Result;
                    TDXTicketCustomAttributes = JsonConvert.DeserializeObject<List<CustomAttribute>>(httpResponseMessageContent);
                }
            }
            catch (Exception exp)
            {
            }
        }

        /// <summary>
        /// Get the list of forms from TeamDynamix
        /// </summary>
        private void GetTDXForms()
        {
            try
            {
                String ApiUri = String.Format("{0}/tickets/forms", ApplicationID);
                HttpResponseMessage httpResponseMessage = oHttpClientX.GetAsync(ApiUri).Result;
                if (httpResponseMessage.IsSuccessStatusCode)
                {
                    String httpResponseMessageContent = httpResponseMessage.Content.ReadAsStringAsync().Result;
                    TDXTicketForms = JsonConvert.DeserializeObject<List<Form>>(httpResponseMessageContent);
                }
            }
            catch (Exception exp)
            {
                Exceptions.Add(exp);
            }
        }

        /// <summary>
        /// Get the list of Services from TeamDynamix
        /// </summary>
        private void GetTDXServices()
        {
            try
            {
                String ApiUri = String.Format("services", ApplicationID);
                HttpResponseMessage httpResponseMessage = oHttpClientX.GetAsync(ApiUri).Result;

                var x = httpResponseMessage.Content.ReadAsStringAsync().Result;
                if (httpResponseMessage.IsSuccessStatusCode)
                {
                    String httpResponseMessageContent = httpResponseMessage.Content.ReadAsStringAsync().Result;
                    TDXServices = JsonConvert.DeserializeObject<List<Service>>(httpResponseMessageContent);
                }
            }
            catch (Exception exp)
            {
                Exceptions.Add(exp);
            }
        }

        /// <summary>
        /// Get the list of Services from TeamDynamix
        /// </summary>
        private void GetTDXGroups()
        {
            try
            {
                GroupSearch groupSearch = new GroupSearch
                {
                    NameLike = "CIT"
                };

                String ApiUri = String.Format("groups/search", ApplicationID);
                String JsonBody = JsonConvert.SerializeObject(groupSearch);
                HttpResponseMessage httpResponseMessage = oHttpClientX.PostAsJsonAsync(ApiUri, JsonBody).Result;
                if (httpResponseMessage.IsSuccessStatusCode)
                {
                    String httpResponseMessageContent = httpResponseMessage.Content.ReadAsStringAsync().Result;
                    TDXGroups = JsonConvert.DeserializeObject<List<Group>>(httpResponseMessageContent);
                }
            }
            catch (Exception exp)
            {
                Exceptions.Add(exp);
            }
        }

        /// <summary>
        /// Get the list of reports from TeamDynamix
        /// </summary>
        private void GetTDXReports()
        {
            try
            {
                String ApiUri = String.Format("reports", ApplicationID);
                HttpResponseMessage httpResponseMessage = oHttpClientX.GetAsync(ApiUri).Result;

                //var x = httpResponseMessage.Content.ReadAsStringAsync().Result;
                if (httpResponseMessage.IsSuccessStatusCode)
                {
                    String httpResponseMessageContent = httpResponseMessage.Content.ReadAsStringAsync().Result;
                    TDXReports = JsonConvert.DeserializeObject<List<Report>>(httpResponseMessageContent);
                }
            }
            catch (Exception exp)
            {
                Exceptions.Add(exp);
            }
        }

        /// <summary>
        /// Get the list of Ticket Priorities from TeamDynamix
        /// </summary>
        private void GetTDXTicketPriorities()
        {
            try
            {
                String ApiUri = String.Format("{0}/tickets/priorities", ApplicationID);
                HttpResponseMessage httpResponseMessage = oHttpClientX.GetAsync(ApiUri).Result;
                if (httpResponseMessage.IsSuccessStatusCode)
                {
                    String httpResponseMessageContent = httpResponseMessage.Content.ReadAsStringAsync().Result;
                    TDXTicketPriorities = JsonConvert.DeserializeObject<List<Priority>>(httpResponseMessageContent);
                }
            }
            catch (Exception exp)
            {
                Exceptions.Add(exp);
            }
        }

        /// <summary>
        /// Get the list of Ticket Statuses from Team Dynamix
        /// </summary>
        private void GetTDXTicketStatuses()
        {
            try
            {
                String ApiUri = String.Format("{0}/tickets/statuses", ApplicationID);
                HttpResponseMessage httpResponseMessage = oHttpClientX.GetAsync(ApiUri).Result;
                if (httpResponseMessage.IsSuccessStatusCode)
                {
                    String httpResponseMessageContent = httpResponseMessage.Content.ReadAsStringAsync().Result;
                    TDXTicketStatuses = JsonConvert.DeserializeObject<List<TicketStatus>>(httpResponseMessageContent);
                }
            }
            catch (Exception exp)
            {
                Exceptions.Add(exp);
            }
        }

        /// <summary>
        /// Get all  Ticket Sources from TDX
        /// </summary>
        private void GetTDXTicketSources()
        {
            try
            {
                String ApiUri = String.Format("{0}/tickets/sources", ApplicationID);
                HttpResponseMessage httpResponseMessage = oHttpClientX.GetAsync(ApiUri).Result;
                if (httpResponseMessage.IsSuccessStatusCode)
                {
                    String httpResponseMessageContent = httpResponseMessage.Content.ReadAsStringAsync().Result;
                    TDXTicketSources = JsonConvert.DeserializeObject<List<TicketSource>>(httpResponseMessageContent);
                }
            }
            catch (Exception exp)
            {
                Exceptions.Add(exp);
            }
        }

        /// <summary>
        /// Get a TDX User based on UID.
        /// </summary>
        /// <param name="TDXUserUID"></param>
        /// <returns></returns>
        private User GetUserByUid(Guid? TDXUserUID)
        {
            try
            {
                if (TDXUserUID != null)
                {
                    Guid userGuid = TDXUserUID.Value;
                    TeamDynamixUser cachedUser = teamDynamixManagementContext.TeamDynamixUsers
                        .Where(u => u.Uid == userGuid)
                        .FirstOrDefault();

                    if (cachedUser != null)
                    {
                        User user = JsonConvert.DeserializeObject<User>(cachedUser.UserAsJSON);
                        return user;
                    }

                    if (TDXUserCache.ContainsKey(TDXUserUID.ToString()))
                    {
                        return TDXUserCache[TDXUserUID.ToString()];
                    }
                    else
                    {
                        Boolean RetryRequest = false;
                        User user = null;
                        do
                        {
                            Thread.Sleep(100);
                            String ApiUri = String.Format("people/{0}", TDXUserUID.ToString());
                            HttpResponseMessage httpResponseMessage = oHttpClientX.GetAsync(ApiUri).Result;
                            if (httpResponseMessage.IsSuccessStatusCode)
                            {
                                String httpResponseMessageContent = httpResponseMessage.Content.ReadAsStringAsync().Result;
                                user = JsonConvert.DeserializeObject<User>(httpResponseMessageContent);
                                TDXUserCache.Add(TDXUserUID.ToString(), user);

                                if (cachedUser == null)
                                {
                                    TeamDynamixUser newCachedUser = new TeamDynamixUser
                                    {
                                        Uid = user.UID,
                                        UserPrincipalName = user.UserName,
                                        UserAsJSON = JsonConvert.SerializeObject(user)
                                    };
                                    teamDynamixManagementContext.TeamDynamixUsers.Add(newCachedUser);
                                    teamDynamixManagementContext.SaveChanges();
                                }
                                RetryRequest = false;
                            }
                            else if (httpResponseMessage.StatusCode.ToString().Equals("429"))
                            {
                                RetryRequest = true;
                            }
                        } while (RetryRequest);
                        return user;
                    }
                }
                else
                {
                    return null;
                }
            }
            catch (Exception exp)
            {
                Exceptions.Add(exp);
                return null;
            }
        }

        #endregion ---- Private Methods ----

        #region ---- Public Methods ----

        /// <summary>
        /// Search TeamDynamix for tickets using the specified Search Text.
        /// </summary>
        /// <param name="SearchText"></param>
        public void SearchTickets(String SearchText)
        {
            try
            {
                String ApiUri = String.Format("{0}/tickets/search", ApplicationID);
                HttpResponseMessage httpResponseMessage = oHttpClientX.PostAsJsonAsync(ApiUri, new { SearchText = SearchText }).Result;
                if (httpResponseMessage.IsSuccessStatusCode)
                {
                    String httpResponseMessageContent = httpResponseMessage.Content.ReadAsStringAsync().Result;
                    TDXTickets = JsonConvert.DeserializeObject<List<Ticket>>(httpResponseMessageContent);
                    TDXAutomationTickets.Clear();
                    foreach (Ticket ticket in TDXTickets)
                    {
                        TDXAutomationTickets.Add(new TDXAutomationTicket(ticket));
                        if (TDXDomainUsers.Where(u => u.TDXUserUID.Equals(ticket.CreatedUid)).FirstOrDefault() == null)
                        {
                            TDXDomainUsers.Add(new TDXDomainUser(GetUserByUid(ticket.CreatedUid)));
                        }

                        if (TDXDomainUsers.Where(u => u.TDXUserUID.Equals(ticket.RequestorUid)).FirstOrDefault() == null)
                        {
                            TDXDomainUsers.Add(new TDXDomainUser(GetUserByUid(ticket.RequestorUid)));
                        }

                        TDXAutomationTicket tDXAutomationTicket = new TDXAutomationTicket(ticket);
                        TDXDomainUser CreatedTDXDomainUser = TDXDomainUsers.Where(u => u.TDXUserUID.Equals(ticket.CreatedUid)).FirstOrDefault();
                        if (CreatedTDXDomainUser != null) { tDXAutomationTicket.SetTicketCreator(CreatedTDXDomainUser); }
                        TDXDomainUser RequestorTDXDomainUser = TDXDomainUsers.Where(u => u.TDXUserUID.Equals(ticket.RequestorUid)).FirstOrDefault();
                        if (CreatedTDXDomainUser != null) { tDXAutomationTicket.SetTicketRequestor(RequestorTDXDomainUser); }
                        TDXAutomationTickets.Add(tDXAutomationTicket);
                    }
                }
            }
            catch (Exception exp)
            {
                Exceptions.Add(exp);
            }
        }

        /// <summary>
        /// Get all tickets from TDX that are included in the specified report.
        /// </summary>
        /// <param name="ReportName">The name of an existing report in TDX</param>
        public void GetTicketsUsingReport(String ReportName)
        {
            try
            {
                String ApiUri = String.Format("reports/{0}?withData=true", this.TDXReports
                                        .Where(r => r.Name.Equals(ReportName))
                                        .Select(r => r.ID)
                                        .FirstOrDefault());

                HttpResponseMessage httpResponseMessage = oHttpClientX.GetAsync(ApiUri).Result;

                if (httpResponseMessage.IsSuccessStatusCode)
                {
                    this.TDXTickets = new List<Ticket>();
                    String httpResponseMessageContent = httpResponseMessage.Content.ReadAsStringAsync().Result;
                    Report report = JsonConvert.DeserializeObject<Report>(httpResponseMessageContent);

                    foreach (Dictionary<String, Object> result in report.DataRows)
                    {
                        int ticketID = Convert.ToInt32(result["TicketID"].ToString());
                        Ticket ticket = GetTicketByID(ticketID);
                        TDXTickets.Add(ticket);

                        if (TDXDomainUsers.Where(u => u.TDXUserUID.Equals(ticket.CreatedUid)).FirstOrDefault() == null)
                        {
                            TDXDomainUsers.Add(new TDXDomainUser(GetUserByUid(ticket.CreatedUid)));
                        }

                        if (TDXDomainUsers.Where(u => u.TDXUserUID.Equals(ticket.RequestorUid)).FirstOrDefault() == null)
                        {
                            TDXDomainUsers.Add(new TDXDomainUser(GetUserByUid(ticket.RequestorUid)));
                        }

                        TDXAutomationTicket tDXAutomationTicket = new TDXAutomationTicket(ticket);
                        TDXDomainUser CreatedTDXDomainUser = TDXDomainUsers.Where(u => u.TDXUserUID.Equals(ticket.CreatedUid)).FirstOrDefault();
                        if (CreatedTDXDomainUser != null) { tDXAutomationTicket.SetTicketCreator(CreatedTDXDomainUser); }
                        TDXDomainUser RequestorTDXDomainUser = TDXDomainUsers.Where(u => u.TDXUserUID.Equals(ticket.RequestorUid)).FirstOrDefault();
                        if (CreatedTDXDomainUser != null) { tDXAutomationTicket.SetTicketRequestor(RequestorTDXDomainUser); }
                        TDXAutomationTickets.Add(tDXAutomationTicket);
                    }
                }
            }
            catch (Exception exp)
            {
                Exceptions.Add(exp);
            }
        }

        /// <summary>
        /// Get all tickets from TDX that are included in the specified report whose status do not match the specified Regular Expression
        /// </summary>
        /// <param name="ReportName">The name of an existing report in TDX</param>
        /// <param name="TicketStatusRegex">A REGEX that matches the desired status names.</param>
        public void GetTicketsUsingReport(String ReportName, System.Text.RegularExpressions.Regex IgnoredTicketStatusRegex)
        {
            try
            {
                String ApiUri = String.Format("reports/{0}?withData=true", this.TDXReports
                                        .Where(r => r.Name.Equals(ReportName))
                                        .Select(r => r.ID)
                                        .FirstOrDefault());

                HttpResponseMessage httpResponseMessage = oHttpClientX.GetAsync(ApiUri).Result;

                if (httpResponseMessage.IsSuccessStatusCode)
                {
                    this.TDXTickets = new List<Ticket>();
                    String httpResponseMessageContent = httpResponseMessage.Content.ReadAsStringAsync().Result;
                    Report report = JsonConvert.DeserializeObject<Report>(httpResponseMessageContent);

                    foreach (Dictionary<String, Object> result in report.DataRows)
                    {
                        int ticketID = Convert.ToInt32(result["TicketID"].ToString());
                        Ticket ticket = GetTicketByID(ticketID);
                        if (!IgnoredTicketStatusRegex.IsMatch(ticket.StatusName))
                        {
                            TDXTickets.Add(ticket);

                            if (TDXDomainUsers.Where(u => u.TDXUserUID.Equals(ticket.CreatedUid)).FirstOrDefault() == null)
                            {
                                TDXDomainUsers.Add(new TDXDomainUser(GetUserByUid(ticket.CreatedUid)));
                            }

                            if (TDXDomainUsers.Where(u => u.TDXUserUID.Equals(ticket.RequestorUid)).FirstOrDefault() == null)
                            {
                                TDXDomainUsers.Add(new TDXDomainUser(GetUserByUid(ticket.RequestorUid)));
                            }

                            TDXAutomationTicket tDXAutomationTicket = new TDXAutomationTicket(ticket);
                            TDXDomainUser CreatedTDXDomainUser = TDXDomainUsers.Where(u => u.TDXUserUID.Equals(ticket.CreatedUid)).FirstOrDefault();
                            if (CreatedTDXDomainUser != null) { tDXAutomationTicket.SetTicketCreator(CreatedTDXDomainUser); }
                            TDXDomainUser RequestorTDXDomainUser = TDXDomainUsers.Where(u => u.TDXUserUID.Equals(ticket.RequestorUid)).FirstOrDefault();
                            if (CreatedTDXDomainUser != null) { tDXAutomationTicket.SetTicketRequestor(RequestorTDXDomainUser); }
                            TDXAutomationTickets.Add(tDXAutomationTicket);
                        }
                    }
                }
            }
            catch (Exception exp)
            {
                Exceptions.Add(exp);
            }
        }

        /// <summary>
        /// Set the current active ticket.
        /// </summary>
        /// <param name="Ticket"></param>
        public void SetActiveTicket(Ticket Ticket)
        {
            _TDXTicket = TDXTickets.Where(t => t.ID.Equals(Ticket.ID)).FirstOrDefault();
            TDXAutomationTicket = TDXAutomationTickets.Where(t => t.ID.Equals(Ticket.ID)).FirstOrDefault();

            // Check the Automation ID [TDX Custom Attribute: (S111-AUTOMATIONID)].
            // If this is NULL assign a new automation ID to the TDX Ticket.
            if (TDXAutomationTicket.AutomationID == null)
            {
                String automationID = Guid.NewGuid().ToString();
                UpdateAttribute("S111-AUTOMATIONID", automationID);
                TDXAutomationTicket.AutomationID = automationID;
            }
            NotifyCreator = false;
            NotifyRequestor = false;
            UpdateTicketStatus = false;
            NotificationEmails.Clear();
        }

        /// <summary>
        /// Get the active ticket's custom attribute by name.
        /// </summary>
        /// <param name="AttributeName"></param>
        public CustomAttribute GetTicketAttributeByName(String AttributeName)
        {
            try
            {
                TDXTicketCustomAttribute = TDXTicketCustomAttributes.Where(a => a.Name.Equals(AttributeName)).FirstOrDefault();
                return TDXTicketCustomAttribute;
            }
            catch (Exception exp)
            {
                Exceptions.Add(exp);
                return null;
            }
        }

        /// <summary>
        /// Get a single ticket from TeamDynamix By ID.
        /// </summary>
        /// <param name="TDXTicketID"></param>
        public Ticket GetTicketByID(Int32 TDXTicketID)
        {
            try
            {
                String ApiUri = String.Format("{0}/tickets/{1}", ApplicationID, TDXTicketID);
                HttpResponseMessage httpResponseMessage = oHttpClientX.GetAsync(ApiUri).Result;
                if (httpResponseMessage.IsSuccessStatusCode)
                {
                    String httpResponseMessageContent = httpResponseMessage.Content.ReadAsStringAsync().Result;
                    Ticket SelectedTicket = JsonConvert.DeserializeObject<Ticket>(httpResponseMessageContent);
                    return SelectedTicket;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception exp)
            {
                Exceptions.Add(exp);
                return null;
            }
        }

        /// <summary>
        /// Gets the TDX Ticket's feed.
        /// </summary>
        public List<ItemUpdate> GetItemUpdates()
        {
            try
            {
                String ApiUri = String.Format("{0}/tickets/{1}/feed", ApplicationID, _TDXTicket.ID);
                HttpResponseMessage httpResponseMessage = oHttpClientX.GetAsync(ApiUri).Result;
                if (httpResponseMessage.IsSuccessStatusCode)
                {
                    String httpResponseMessageContent = httpResponseMessage.Content.ReadAsStringAsync().Result;
                    TDXItemUpdates = JsonConvert.DeserializeObject<List<ItemUpdate>>(httpResponseMessageContent);
                    return TDXItemUpdates;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception exp)
            {
                Exceptions.Add(exp);
                return null;
            }
        }

        /// <summary>
        /// Add a notification address to the current ticket.
        /// </summary>
        /// <param name="EmailAddress">A valid email address.</param>
        /// <returns>Boolean indicating that the given address is also a TDX User</returns>
        public Boolean AddNotificationEmail(String EmailAddress, Boolean AcceptExternalAddress)
        {
            try
            {
                String ApiUri = String.Format("people/lookup?searchText={0}", EmailAddress);
                HttpResponseMessage httpResponseMessage = oHttpClientX.GetAsync(ApiUri).Result;
                if (httpResponseMessage.IsSuccessStatusCode)
                {
                    String httpResponseMessageContent = httpResponseMessage.Content.ReadAsStringAsync().Result;
                    List<User> TDXUsers = JsonConvert.DeserializeObject<List<User>>(httpResponseMessageContent);
                    if (TDXUsers.Count.Equals(1))
                    {
                        NotificationEmails.Add(TDXUsers[0].PrimaryEmail);
                        return true;
                    }
                    else
                    {
                        if (AcceptExternalAddress)
                        {
                            try
                            {
                                // Check that the given email address has a valid format.
                                MailAddress mailAddress = new MailAddress(EmailAddress);
                                NotificationEmails.Add(mailAddress.Address.ToString());
                            }
                            catch (Exception exp)
                            {
                                Exceptions.Add(exp);
                            }
                        }
                    }
                }
            }
            catch (Exception exp)
            {
                Exceptions.Add(exp);
            }
            return false;
        }

        /// <summary>
        /// Updates a an custom attribute's value.
        /// </summary>
        /// <param name="AttributeName"></param>
        /// <param name="Choice"></param>
        /// <returns></returns>
        public Ticket UpdateAttribute(String AttributeName, String AttributeValue)
        {
            try
            {
                if (_TDXTicket != null)
                {
                    String ApiUri = String.Format("{0}/tickets/{1}?notifyNewResponsible=false", ApplicationID, _TDXTicket.ID);

                    CustomAttribute customAttribute = this.TDXTicketCustomAttributes
                                    .Where(a => a.Name.Equals(AttributeName, StringComparison.OrdinalIgnoreCase))
                                    .FirstOrDefault();

                    if (customAttribute != null)
                    {
                        int customAttributeID = customAttribute.ID;

                        // Create a patch document.
                        JsonPatchDocument jsonPatchDocument = new JsonPatchDocument();
                        jsonPatchDocument.Replace(String.Format("/attributes/{0}", customAttributeID), AttributeValue);
                        HttpMethod method = new HttpMethod("PATCH");
                        HttpRequestMessage httpRequestMessage = new HttpRequestMessage(method, ApiUri)
                        {
                            Content = new StringContent(JsonConvert.SerializeObject(jsonPatchDocument), Encoding.Unicode, "application/json")
                        };
                        HttpResponseMessage httpResponseMessage = oHttpClientX.SendAsync(httpRequestMessage).Result;
                        if (httpResponseMessage.IsSuccessStatusCode)
                        {
                            String httpResponseMessageContent = httpResponseMessage.Content.ReadAsStringAsync().Result;
                            _TDXTicket = JsonConvert.DeserializeObject<Ticket>(httpResponseMessageContent);
                        }
                        else
                        {
                            String httpResponseMessageContent = httpResponseMessage.Content.ReadAsStringAsync().Result;
                        }

                        return _TDXTicket;
                    }
                }
            }
            catch (Exception exp)
            {
                Exceptions.Add(exp);
            }
            return null;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="automationStatus"></param>
        /// <returns></returns>
        public Ticket UpdateAutomationStatus(String automationStatus)
        {
            //Set Automation Status [TDX Custom Attribute: (S111-AUTOMATIONSTATUS)]

            return UpdateDropDownChoiceAttribute("S111-AUTOMATIONSTATUS", automationStatus);
        }

        /// <summary>
        /// Updates the automation status details custom attribute for the active ticket.
        /// </summary>
        /// <param name="automationStatusDetail"></param>
        /// <returns></returns>
        public Ticket UpdateAutomationStatusDetails(String automationStatusDetails)
        {
            // Set Automation Status Details [TDX Custom Attribute: (S111-AUTOMATIONSTATUSDETAILS)]
            return UpdateAttribute("S111-AUTOMATIONDETAILS", automationStatusDetails);
        }

        /// <summary>
        /// Updates the automation status details custom attribute for the active ticket.
        /// </summary>
        /// <param name="automationStatusDetail"></param>
        /// <returns></returns>
        public Ticket UpdateAutomationStatusDetails(StringBuilder automationStatusDetails)
        {
            return UpdateAutomationStatusDetails(automationStatusDetails.ToString());
        }

        /// <summary>
        /// Updates the selected choice in a custom drop-down custom attribute.
        /// </summary>
        /// <param name="AttributeName"></param>
        /// <param name="Choice"></param>
        /// <returns></returns>
        public Ticket UpdateDropDownChoiceAttribute(String AttributeName, String Choice)
        {
            try
            {
                if (_TDXTicket != null)
                {
                    String ApiUri = String.Format("{0}/tickets/{1}?notifyNewResponsible=false", ApplicationID, _TDXTicket.ID);

                    CustomAttribute customAttribute = this.TDXTicketCustomAttributes
                                    .Where(a => a.Name.Equals(AttributeName, StringComparison.OrdinalIgnoreCase))
                                    .FirstOrDefault();

                    if (customAttribute != null)
                    {
                        int customAttributeID = customAttribute.ID;
                        CustomAttributeChoice customAttributeChoice = customAttribute.Choices.Where(c => c.Name.Equals(Choice, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                        if (customAttributeChoice != null)
                        {
                            int customAttributeChoiceID = customAttributeChoice.ID;

                            // Create a patch document.
                            JsonPatchDocument jsonPatchDocument = new JsonPatchDocument();
                            jsonPatchDocument.Replace(String.Format("/attributes/{0}", customAttributeID), customAttributeChoiceID);
                            HttpMethod method = new HttpMethod("PATCH");
                            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(method, ApiUri)
                            {
                                Content = new StringContent(JsonConvert.SerializeObject(jsonPatchDocument), Encoding.Unicode, "application/json")
                            };
                            HttpResponseMessage httpResponseMessage = oHttpClientX.SendAsync(httpRequestMessage).Result;
                            if (httpResponseMessage.IsSuccessStatusCode)
                            {
                                String httpResponseMessageContent = httpResponseMessage.Content.ReadAsStringAsync().Result;
                                _TDXTicket = JsonConvert.DeserializeObject<Ticket>(httpResponseMessageContent);
                            }
                            else
                            {
                                String httpResponseMessageContent = httpResponseMessage.Content.ReadAsStringAsync().Result;
                            }

                            return _TDXTicket;
                        }
                    }
                }
            }
            catch (Exception exp)
            {
                Exceptions.Add(exp);
            }
            return null;
        }

        /// <summary>
        /// Send a Patch Request to update the current TDX ticket's title.
        /// </summary>
        /// <param name="TicketTitle">The new ticket title.</param>
        /// <returns></returns>
        public Ticket UpdateResponsibleGroup(int GroupID)
        {
            try
            {
                if (_TDXTicket != null)
                {
                    String ApiUri = String.Format("{0}/tickets/{1}?notifyNewResponsible=false", ApplicationID, _TDXTicket.ID);

                    JsonPatchDocument<Ticket> jsonPatchDocument = new JsonPatchDocument<Ticket>();

                    jsonPatchDocument.Replace(g => g.ResponsibleGroupID, GroupID);
                    HttpMethod method = new HttpMethod("PATCH");
                    HttpRequestMessage httpRequestMessage = new HttpRequestMessage(method, ApiUri)
                    {
                        Content = new StringContent(JsonConvert.SerializeObject(jsonPatchDocument), Encoding.Unicode, "application/json")
                    };
                    HttpResponseMessage httpResponseMessage = oHttpClientX.SendAsync(httpRequestMessage).Result;
                    if (httpResponseMessage.IsSuccessStatusCode)
                    {
                        String httpResponseMessageContent = httpResponseMessage.Content.ReadAsStringAsync().Result;
                        _TDXTicket = JsonConvert.DeserializeObject<Ticket>(httpResponseMessageContent);
                    }
                    else
                    {
                        String httpResponseMessageContent = httpResponseMessage.Content.ReadAsStringAsync().Result;
                    }

                    return _TDXTicket;
                }
            }
            catch (Exception exp)
            {
                Exceptions.Add(exp);
            }
            return null;
        }

        /// <summary>
        /// Send a Patch Request to update the current TDX ticket's title.
        /// </summary>
        /// <param name="TicketTitle">The new ticket title.</param>
        /// <returns></returns>
        public Ticket UpdateTicketTitle(String TicketTitle)
        {
            try
            {
                if (_TDXTicket != null)
                {
                    String ApiUri = String.Format("{0}/tickets/{1}?notifyNewResponsible=false", ApplicationID, _TDXTicket.ID);

                    JsonPatchDocument<Ticket> jsonPatchDocument = new JsonPatchDocument<Ticket>();

                    jsonPatchDocument.Replace(t => t.Title, TicketTitle);
                    HttpMethod method = new HttpMethod("PATCH");
                    HttpRequestMessage httpRequestMessage = new HttpRequestMessage(method, ApiUri)
                    {
                        Content = new StringContent(JsonConvert.SerializeObject(jsonPatchDocument), Encoding.Unicode, "application/json")
                    };
                    HttpResponseMessage httpResponseMessage = oHttpClientX.SendAsync(httpRequestMessage).Result;
                    if (httpResponseMessage.IsSuccessStatusCode)
                    {
                        String httpResponseMessageContent = httpResponseMessage.Content.ReadAsStringAsync().Result;
                        _TDXTicket = JsonConvert.DeserializeObject<Ticket>(httpResponseMessageContent);
                    }
                    else
                    {
                        String httpResponseMessageContent = httpResponseMessage.Content.ReadAsStringAsync().Result;
                    }

                    return _TDXTicket;
                }
            }
            catch (Exception exp)
            {
                Exceptions.Add(exp);
            }
            return null;
        }

        /// <summary>
        /// Send a Patch Request to update the current TDX ticket's description.
        /// </summary>
        /// <param name="TicketDescription"></param>
        /// <returns></returns>
        public Ticket UpdateTicketDescription(String TicketDescription)
        {
            try
            {
                if (_TDXTicket != null)
                {
                    String ApiUri = String.Format("{0}/tickets/{1}?notifyNewResponsible=false", ApplicationID, _TDXTicket.ID);

                    JsonPatchDocument<Ticket> jsonPatchDocument = new JsonPatchDocument<Ticket>();
                    jsonPatchDocument.Replace(t => t.Description, TicketDescription);
                    HttpMethod method = new HttpMethod("PATCH");
                    HttpRequestMessage httpRequestMessage = new HttpRequestMessage(method, ApiUri)
                    {
                        Content = new StringContent(JsonConvert.SerializeObject(jsonPatchDocument), Encoding.Unicode, "application/json")
                    };
                    HttpResponseMessage httpResponseMessage = oHttpClientX.SendAsync(httpRequestMessage).Result;
                    if (httpResponseMessage.IsSuccessStatusCode)
                    {
                        String httpResponseMessageContent = httpResponseMessage.Content.ReadAsStringAsync().Result;
                        _TDXTicket = JsonConvert.DeserializeObject<Ticket>(httpResponseMessageContent);
                    }
                    else
                    {
                        String httpResponseMessageContent = httpResponseMessage.Content.ReadAsStringAsync().Result;
                    }

                    return _TDXTicket;
                }
            }
            catch (Exception exp)
            {
                Exceptions.Add(exp);
            }
            return null;
        }

        /// <summary>
        /// Updates the Active Ticket's Title and Description.
        /// </summary>
        /// <param name="TicketTitle"></param>
        /// <param name="TicketDescription"></param>
        /// <returns></returns>
        public Ticket UpdateTicketTitleAndDescription(String TicketTitle, String TicketDescription)
        {
            Ticket patchTicket = new Ticket
            {
                Title = TicketTitle,
                Description = TicketDescription
            };

            return PatchTicket(patchTicket);
        }

        /// <summary>
        /// Updates the Active Ticket's Title and Description.
        /// </summary>
        /// <param name="TicketTitle"></param>
        /// <param name="TicketDescription"></param>
        /// <returns></returns>
        public Ticket UpdateTicketTitleAndDescription(StringBuilder TicketTitle, StringBuilder TicketDescription)
        {
            return UpdateTicketTitleAndDescription(TicketTitle.ToString(), TicketDescription.ToString());
        }

        /// <summary>
        /// Send a Patch Request to update the current TDX ticket.
        /// </summary>
        /// <param name="UpdatedTicket">A TeamDynamix ticket with the updated properties.t</param>
        /// <returns></returns>
        public Ticket PatchTicket(Ticket UpdatedTicket)
        {
            // List of properties in a TeamDynamix Ticket.
            List<PropertyInfo> properties = typeof(Ticket).GetProperties().ToList();

            // List of properties that can be updated in a TeamDynamix Ticket.
            List<String> UpdatableProperties = new List<String>() { "Title", "Description", "Attributes" };

            // Create a patch document.
            JsonPatchDocument jsonPatchDocument = new JsonPatchDocument();

            foreach (PropertyInfo property in properties)
            {
                if (UpdatableProperties.Contains(property.Name))
                {
                    Boolean PatchProperty = false;
                    Object PropertyValue = property.GetValue(UpdatedTicket, null);
                    if (PropertyValue != null)
                    {
                        PatchProperty = true;

                        if (PropertyValue.GetType().Name.Equals("string", StringComparison.OrdinalIgnoreCase))
                        {
                            if (PropertyValue.ToString().Length == 0)
                            {
                                PatchProperty = false;
                            }
                        }
                    }
                    if (PatchProperty)
                    {
                        jsonPatchDocument.Replace(property.Name, PropertyValue);
                    }
                }
            }

            try
            {
                if (_TDXTicket != null)
                {
                    String ApiUri = String.Format("{0}/tickets/{1}?notifyNewResponsible=false", ApplicationID, _TDXTicket.ID);

                    //jsonPatchDocument.Replace(t => t.Description, TicketDescription);
                    HttpMethod method = new HttpMethod("PATCH");
                    HttpRequestMessage httpRequestMessage = new HttpRequestMessage(method, ApiUri)
                    {
                        Content = new StringContent(JsonConvert.SerializeObject(jsonPatchDocument), Encoding.Unicode, "application/json")
                    };
                    HttpResponseMessage httpResponseMessage = oHttpClientX.SendAsync(httpRequestMessage).Result;
                    if (httpResponseMessage.IsSuccessStatusCode)
                    {
                        String httpResponseMessageContent = httpResponseMessage.Content.ReadAsStringAsync().Result;
                        _TDXTicket = JsonConvert.DeserializeObject<Ticket>(httpResponseMessageContent);
                    }
                    else
                    {
                        String httpResponseMessageContent = httpResponseMessage.Content.ReadAsStringAsync().Result;
                    }

                    return _TDXTicket;
                }
            }
            catch (Exception exp)
            {
                Exceptions.Add(exp);
            }
            return null;
        }

        /// <summary>
        /// Update the TDX Ticket with a public comments retain the current ticket status.
        /// This method always notifies the customer.
        /// </summary>
        /// <param name="Comments">The ticket comment to add to the feed.</param>
        public void UpdateTicket(String Comments)
        {
            AddTicketFeedEntry(Comments, true, false);
        }

        /// <summary>
        /// Update the TDX Ticket with a public comments retain the current ticket status.
        /// This method always notifies the customer.
        /// </summary>
        /// <param name="Comments"></param>
        public void UpdateTicket(StringBuilder Comments)
        {
            AddTicketFeedEntry(Comments.ToString(), true, false);
        }

        /// <summary>
        /// Update the current TDX ticket by adding a new public ticket feed entry and set the ticket's status.
        /// This method always notifies the customer.
        /// </summary>
        /// <param name="Comments">The ticket comment to add to the feed.</param>
        /// <param name="StatusName">The new ticket status.</param>
        public void UpdateTicket(String Comments, String StatusName)
        {
            // If the ticket status name is not null then update the status.
            if (StatusName != null)
            {
                SetTicketStatusByName(StatusName);
            }

            // If the comments are not null then update the ticket by adding a feed entry.
            // The status will be updated automatically and notifications will be sent.
            if (Comments != null)
            {
                AddTicketFeedEntry(Comments, true, false);
            }
            // If no comments are added update the ticket status only.
            // Do not notify and make this feed entry private.
            else
            {
                AddTicketFeedEntry("Ticket Status Updated", false, true);
            }
        }

        /// <summary>
        /// Update the current TDX ticket by adding a new public ticket feed entry and set the ticket's status.
        /// This method always notifies the customer.
        /// </summary>
        /// <param name="Comments"></param>
        /// <param name="StatusName"></param>
        public void UpdateTicket(StringBuilder Comments, String StatusName)
        {
            UpdateTicket(Comments.ToString(), StatusName);
        }

        /// <summary>
        /// Update the current TDX ticket by adding a new ticket feed entry specifying as public or private.
        /// </summary>
        /// <param name="Comments">The ticket comment to add to the feed.</param>
        /// <param name="IsPrivate">Private flag.</param>
        public void UpdateTicket(String Comments, Boolean IsPrivate)
        {
            Boolean Notify = true;
            if (IsPrivate) { Notify = false; }
            AddTicketFeedEntry(Comments, Notify, IsPrivate);
        }

        public void UpdateTicket(StringBuilder Comments, Boolean IsPrivate)
        {
            UpdateTicket(Comments.ToString(), IsPrivate);
        }

        /// <summary>
        /// Update the current TDX ticket by adding a new ticket feed entry, set the
        /// status and specify the update as public or private.
        /// </summary>
        /// <param name="Comments">The ticket comment to add to the feed.</param>
        /// <param name="StatusName">The ticket status</param>
        /// <param name="Private">Private flag.</param>
        public void UpdateTicket(String Comments, String StatusName, Boolean IsPrivate)
        {
            // If the ticket status name is not null then update the status.
            if (StatusName != null)
            {
                SetTicketStatusByName(StatusName);
            }

            // If the comments are not null then update the ticket by adding a feed entry.
            // The status will be updated automatically
            if (Comments != null)
            {
                AddTicketFeedEntry(Comments, true, IsPrivate);
            }
            // If no comments are added then update the ticket noting the status change.
            else
            {
                AddTicketFeedEntry("Ticket Status Updated", false, true);
            }
        }

        /// <summary>
        /// Update the current TDX ticket by adding a new ticket feed entry, set the
        /// status and specify the update as public or private.
        /// </summary>
        /// <param name="Comments"></param>
        /// <param name="StatusName"></param>
        /// <param name="IsPrivate"></param>
        public void UpdateTicket(StringBuilder Comments, String StatusName, Boolean IsPrivate)
        {
            UpdateTicket(Comments.ToString(), StatusName, IsPrivate);
        }

        /// <summary>
        /// Adds a ticket feed entry with new comments and optionally sends notifications.
        /// </summary>
        /// <param name="Comments">The comment you wish to add to the feed.</param>
        /// <param name="Notify">Specify if you want notifications sent.</param>
        /// <returns></returns>
        public ItemUpdate AddTicketFeedEntry(String Comments, Boolean Notify, Boolean IsPrivate)
        {
            if (_TDXTicket != null && Comments != null)
            {
                TicketFeedEntry ticketFeedEntry = new TicketFeedEntry
                {
                    Comments = Comments,
                    IsPrivate = IsPrivate
                };

                if (NotifyRequestor)
                {
                    try
                    {
                        String UserEmail = GetUserByUid(_TDXTicket.RequestorUid).PrimaryEmail;
                        if (!NotificationEmails.Contains(UserEmail))
                        {
                            NotificationEmails.Add(UserEmail);
                        }
                    }
                    catch
                    {
                    }
                }

                if (NotifyCreator)
                {
                    String UserEmail = GetUserByUid(_TDXTicket.CreatedUid).PrimaryEmail;
                    if (!NotificationEmails.Contains(UserEmail))
                    {
                        NotificationEmails.Add(UserEmail);
                    }
                }

                if (Notify & NotificationEmails.Count > 0)
                {
                    ticketFeedEntry.Notify = NotificationEmails.ToArray();
                }

                if (UpdateTicketStatus)
                {
                    ticketFeedEntry.NewStatusID = TicketStatus.ID;
                }

                try
                {
                    if (_TDXTicket != null)
                    {
                        String ApiUri = String.Format("{0}/tickets/{1}/feed", ApplicationID, _TDXTicket.ID);

                        HttpResponseMessage httpResponseMessage = oHttpClientX.PostAsJsonAsync<TicketFeedEntry>(ApiUri, ticketFeedEntry).Result;
                        if (httpResponseMessage.IsSuccessStatusCode)
                        {
                            String httpResponseMessageContent = httpResponseMessage.Content.ReadAsStringAsync().Result;
                            TDXItemUpdate = JsonConvert.DeserializeObject<ItemUpdate>(httpResponseMessageContent);
                            return TDXItemUpdate;
                        }
                        else
                        {
                            String httpResponseMessageContent = httpResponseMessage.Content.ReadAsStringAsync().Result;
                        }
                    }
                    return null;
                }
                catch (Exception exp)
                {
                    Exceptions.Add(exp);
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Sets the  OgraniApplicaton By Name
        /// </summary>
        /// <param name="ApplicationName"></param>
        public OrgApplication SetOrgAppicationByName(String ApplicationName)
        {
            try
            {
                TDXOrgAppication = TDXOrgApplications.Where(a => a.Name.Equals(ApplicationName)).FirstOrDefault();
                return TDXOrgAppication;
            }
            catch (Exception exp)
            {
                Exceptions.Add(exp);
                return null;
            }
        }

        /// <summary>
        /// Sets the Ticket Form Name
        /// </summary>
        /// <param name="FormName"></param>
        public Form SetTicketFormByName(String FormName)
        {
            try
            {
                TDXTicketForm = TDXTicketForms.Where(f => f.Name.Equals(FormName)).FirstOrDefault();
                return TDXTicketForm;
            }
            catch (Exception exp)
            {
                Exceptions.Add(exp);
                return null;
            }
        }

        /// <summary>
        /// Sets the Service Name
        /// </summary>
        /// <param name="ServiceName"></param>
        public Service SetServiceByName(String ServiceName)
        {
            try
            {
                TDXService = TDXServices.Where(s => s.Name.Equals(ServiceName)).FirstOrDefault();
                return TDXService;
            }
            catch (Exception exp)
            {
                Exceptions.Add(exp);
                return null;
            }
        }

        /// <summary>
        /// Set the Ticket Source by name.
        /// </summary>
        /// <param name="SourceName"></param>
        /// <returns></returns>
        public TicketSource SetTicketSourceByName(String SourceName)
        {
            try
            {
                TicketSource = TDXTicketSources.Where(s => s.Name.Equals(SourceName)).FirstOrDefault();
                return TicketSource;
            }
            catch (Exception exp)
            {
                Exceptions.Add(exp);
                return null;
            }
        }

        /// <summary>
        /// Sets the  Account Name
        /// </summary>
        /// <param name="AccountName"></param>
        public Account SetAccountByName(String AccountName)
        {
            try
            {
                TDXAccount = TDXAccounts.Where(a => a.Name.Equals(AccountName)).FirstOrDefault();
                return TDXAccount;
            }
            catch (Exception exp)
            {
                Exceptions.Add(exp);
                return null;
            }
        }

        /// <summary>
        /// Sets the Active Ticket's Status
        /// </summary>
        /// <param name="StatusName"></param>
        public TicketStatus SetTicketStatusByName(String StatusName)
        {
            try
            {
                TicketStatus = TDXTicketStatuses.Where(s => s.Name.Equals(StatusName)).FirstOrDefault();
                if (TicketStatus != null && _TDXTicket != null)
                {
                    _TDXTicket.StatusID = TicketStatus.ID;
                    UpdateTicketStatus = true;
                }
                else
                {
                    UpdateTicketStatus = false;
                }

                return TicketStatus;
            }
            catch (Exception exp)
            {
                Exceptions.Add(exp);
                return null;
            }
        }

        /// <summary>
        /// Set The  Ticket's Priority
        /// </summary>
        /// <param name="PriorityName"></param>
        public Priority SetTicketPriorityByName(String PriorityName)
        {
            try
            {
                TicketPriority = TDXTicketPriorities.Where(p => p.Name.Equals(PriorityName)).FirstOrDefault();
                return TicketPriority;
            }
            catch (Exception exp)
            {
                Exceptions.Add(exp);
                return null;
            }
        }

        /// <summary>
        /// Set the current TDX user by UserPrincipalName
        /// </summary>
        /// <param name="UserPrincipalName"></param>
        public void SetTicketWorkflow(int WorkflowId)
        {
            if (_TDXTicket != null)
            {
                String ApiUri = String.Format("{0}/tickets/{1}/workflow?newWorkflowId={2}&allowRemoveExisting=true", ApplicationID, _TDXTicket.ID, WorkflowId);
                HttpResponseMessage httpResponseMessage = oHttpClientX.PutAsync(ApiUri, null).Result;
                if (httpResponseMessage.IsSuccessStatusCode)
                {
                    String httpResponseMessageContent = httpResponseMessage.Content.ReadAsStringAsync().Result;
                }
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="UserPrincipalName"></param>
        /// <returns></returns>
        public User GetTDXUserByUserPrincipalName(String UserPrincipalName)
        {
            try
            {
                TeamDynamixUser cachedUser = teamDynamixManagementContext.TeamDynamixUsers
                    .Where(u => u.UserPrincipalName.Equals(UserPrincipalName, StringComparison.OrdinalIgnoreCase))
                    .FirstOrDefault();

                if (cachedUser != null)
                {
                    User user = JsonConvert.DeserializeObject<User>(cachedUser.UserAsJSON);
                    return user;
                }

                User CachedUser = TDXUserCache.Values
                    .Where(u => u.UserName.Equals(UserPrincipalName, StringComparison.OrdinalIgnoreCase))
                    .FirstOrDefault();

                if (CachedUser != null)
                {
                    return CachedUser;
                }
                else
                {
                    String ApiUri = String.Format("people/lookup?searchText={0}", UserPrincipalName);
                    HttpResponseMessage httpResponseMessage = oHttpClientX.GetAsync(ApiUri).Result;
                    if (httpResponseMessage.IsSuccessStatusCode)
                    {
                        String httpResponseMessageContent = httpResponseMessage.Content.ReadAsStringAsync().Result;
                        List<User> TDXUsers = JsonConvert.DeserializeObject<List<User>>(httpResponseMessageContent);
                        if (TDXUsers.Count.Equals(1))
                        {
                            TDXUser = TDXUsers[0];

                            if (!TDXUserCache.ContainsKey(TDXUser.UID.ToString()))
                            {
                                TDXUserCache.Add(TDXUser.UID.ToString(), TDXUser);
                            }

                            if (cachedUser == null)
                            {
                                TeamDynamixUser newCachedUser = new TeamDynamixUser
                                {
                                    Uid = TDXUser.UID,
                                    UserPrincipalName = TDXUser.UserName,
                                    UserAsJSON = JsonConvert.SerializeObject(TDXUser)
                                };
                                teamDynamixManagementContext.TeamDynamixUsers.Add(newCachedUser);
                                teamDynamixManagementContext.SaveChanges();
                            }

                            return TDXUser;
                        }
                        else
                        {
                            return null;
                        }
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            catch (Exception exp)
            {
                Exceptions.Add(exp);
                return null;
            }
        }

        /// <summary>
        /// Set the current TDX user by UserPrincipalName
        /// </summary>
        /// <param name="UserPrincipalName"></param>
        ///
        public User GetTDXUserByUID(String Uid)
        {
            try
            {
                Guid userGuid = Guid.Parse(Uid);
                TeamDynamixUser cachedUser = teamDynamixManagementContext.TeamDynamixUsers
                    .Where(u => u.Uid == userGuid)
                    .FirstOrDefault();

                if (cachedUser != null)
                {
                    User user = JsonConvert.DeserializeObject<User>(cachedUser.UserAsJSON);
                    return user;
                }

                if (TDXUserCache.ContainsKey(Uid))
                {
                    return TDXUserCache[Uid];
                }
                else
                {
                    Boolean RetryRequest = false;
                    User user = null;
                    do
                    {
                        Thread.Sleep(5000);
                        String ApiUri = String.Format("people/{0}", Uid);
                        HttpResponseMessage httpResponseMessage = oHttpClientX.GetAsync(ApiUri).Result;
                        if (httpResponseMessage.IsSuccessStatusCode)
                        {
                            String httpResponseMessageContent = httpResponseMessage.Content.ReadAsStringAsync().Result;
                            user = JsonConvert.DeserializeObject<User>(httpResponseMessageContent);
                            TDXUserCache.Add(Uid, user);

                            if (cachedUser == null)
                            {
                                TeamDynamixUser newCachedUser = new TeamDynamixUser
                                {
                                    Uid = user.UID,
                                    UserPrincipalName = user.UserName,
                                    UserAsJSON = JsonConvert.SerializeObject(user)
                                };
                                teamDynamixManagementContext.TeamDynamixUsers.Add(newCachedUser);
                                teamDynamixManagementContext.SaveChanges();
                            }
                            RetryRequest = false;
                        }
                        else if (httpResponseMessage.StatusCode.ToString().Equals("429"))
                        {
                            RetryRequest = true;
                        }
                    } while (RetryRequest);

                    return user;
                }
            }
            catch (Exception exp)
            {
                Exceptions.Add(exp);
                return null;
            }
        }

        /// <summary>
        /// Creates a ticket in the TDX Application using the current TDXTicket.
        /// </summary>
        /// <param name="EnableNotifyReviewer"></param>
        /// <param name="NotifyRequestor"></param>
        /// <param name="NotifyResponsible"></param>
        /// <param name="AllowRequestorCreation"></param>
        public void CreateNewTicket(Boolean EnableNotifyReviewer, Boolean NotifyRequestor, Boolean NotifyResponsible, Boolean AllowRequestorCreation)
        {
            try
            {
                String ApiUri = String.Format("{0}/tickets?EnableNotifyReviewer={1}&NotifyRequestor={2}&NotifyResponsible={3}&AllowRequestorCreation={4}",
                    ApplicationID, EnableNotifyReviewer, NotifyRequestor, NotifyResponsible, AllowRequestorCreation);

                if (_TDXTicket != null)
                {
                    // Set default ticket properties.
                    if (TDXAccount != null) { _TDXTicket.AccountID = TDXAccount.ID; }
                    if (TDXTicketForm != null) { _TDXTicket.FormID = TDXTicketForm.ID; }
                    if (TicketPriority != null) { _TDXTicket.PriorityID = TicketPriority.ID; }
                    if (TDXService != null) { _TDXTicket.ServiceID = TDXService.ID; }
                    if (TicketSource != null) { _TDXTicket.SourceID = TicketSource.ID; }
                    _TDXTicket.TypeID = ((int)TicketClass.ServiceRequest);

                    HttpResponseMessage httpResponseMessage = oHttpClientX.PostAsJsonAsync<Ticket>(ApiUri, _TDXTicket).Result;
                    if (httpResponseMessage.IsSuccessStatusCode)
                    {
                        String httpResponseMessageContent = httpResponseMessage.Content.ReadAsStringAsync().Result;
                        _TDXTicket = JsonConvert.DeserializeObject<Ticket>(httpResponseMessageContent);
                    }
                    else
                    {
                        String httpResponseMessageContent = httpResponseMessage.Content.ReadAsStringAsync().Result;
                    }
                }
            }
            catch (Exception exp)
            {
                Exceptions.Add(exp);
            }
        }

        #endregion ---- Public Methods ----
    }

    #region ---- TDX Automation Ticket ----

    public class TDXAutomationTicket : Ticket
    {
        // TDX Custom Attribute: (S111-AUTOMATIONID)
        public String AutomationID { get; set; }

        // TDX Custom Attribute: (S111-AUTOMATIONSTATUS)
        public String AutomationStatus { get; set; }

        // TDX Custom Attribute: (S111-AUTOMATIONDETAILS)
        public String AutomationDetails { get; set; }

        // TDX Custom Attribute: (Alternate Email Address)
        public String AlternateEmailAddress { get; set; }

        public TDXDomainUser TicketCreator { get; private set; }

        public TDXDomainUser TicketRequestor { get; private set; }

        public TDXAutomationTicket(Ticket ticket)
        {
            // Use reflection to copy the base ticket to the Automation Ticket.
            PropertyInfo[] ticketProperties = ticket.GetType().GetProperties();
            foreach (PropertyInfo ticketProperty in ticketProperties)
            {
                PropertyInfo AutomationTicketProperty = this.GetType().GetProperty(ticketProperty.Name);
                AutomationTicketProperty.SetValue(this, ticketProperty.GetValue(ticket));
            }

            // Automation ID [TDX Custom Attribute: (S111-AUTOMATIONID)]
            AutomationID = ticket.Attributes.Where(attrib => attrib.Name.Equals("S111-AUTOMATIONID")).Select(attrib => attrib.Value).FirstOrDefault();

            // Automation Status [TDX Custom Attribute: (S111-AUTOMATIONSTATUS)]
            AutomationStatus = ticket.Attributes.Where(a => a.Name.Equals("S111-AUTOMATIONSTATUS")).Select(a => a.ValueText).FirstOrDefault();

            // Automation Status [TDX Custom Attribute: (S111-AUTOMATIONDETAILS)]
            AutomationDetails = ticket.Attributes.Where(a => a.Name.Equals("S111-AUTOMATIONDETAILS")).Select(a => a.ValueText).FirstOrDefault();

            // Alternate Email Address [TDX Custom Attribute: (Alternate Email Address)]
            AlternateEmailAddress = ticket.Attributes.Where(a => a.Name.Equals("Alternate Email Address")).Select(a => a.ValueText).FirstOrDefault();
        }

        public void SetTicketCreator(TDXDomainUser tDXDomainUser)
        {
            TicketCreator = tDXDomainUser;
        }

        public void SetTicketRequestor(TDXDomainUser tDXDomainUser)
        {
            TicketRequestor = tDXDomainUser;
        }
    }

    #endregion ---- TDX Automation Ticket ----

    #region ---- TDX Domain User ----

    public class TDXDomainUser
    {
        #region ---- Public Properties ----

        public Boolean IsValid { get; private set; }

        public String UserID
        { get { return UserPrincipalName.Split('@')[0]; } }

        public String UserPrincipalName { get; private set; }

        public String DisplayName { get; private set; }

        public String EmailAddress { get; private set; }

        public String PrimaryAffiliation { get; private set; }

        public List<String> Affiliations { get; private set; }

        public List<String> Entitlements { get; private set; }

        public List<String> ProvAccts { get; private set; }

        public List<String> MailDelivery { get; private set; }

        public List<String> MemberOf { get; private set; }

        public Guid ActiveDirectoryObjectGUID { get; private set; }

        public String ManagerUserPrincipalName { get; private set; }

        public String ManagerUserID
        { get { if (ManagerUserPrincipalName != null) { return ManagerUserPrincipalName.Split('@')[0]; } else { return null; } } }

        public Guid TDXUserUID { get; private set; }

        public String TDXPrimaryEmail { get; private set; }

        public List<Exception> Exceptions { get; private set; }

        public Exception LastException
        { get { if (Exceptions.Count > 0) { return Exceptions.Last(); } else { return null; } } }

        #endregion ---- Public Properties ----

        #region --- Class Constructor ---

        public TDXDomainUser(TeamDynamix.Api.Users.User tdxUser)
        {
            Exceptions = new List<Exception>();

            using (ActiveDirectoryContext activeDirectoryContext = new ActiveDirectoryContext())
            {
                activeDirectoryContext.IncludeNestedGroupMembership = true;
                activeDirectoryContext.PropertiesToLoad = new List<string>();
                activeDirectoryContext.PropertiesToLoad.Add("cornelleduPrimaryAffiliation");
                activeDirectoryContext.PropertiesToLoad.Add("cornelleduAffiliation");
                activeDirectoryContext.PropertiesToLoad.Add("cornelleduEntitlements");
                activeDirectoryContext.PropertiesToLoad.Add("cornelleduProvAccts");
                activeDirectoryContext.PropertiesToLoad.Add("cornelleduMailDelivery");
                activeDirectoryContext.PropertiesToLoad.Add("manager");

                String userPrincipalName = tdxUser.UserName;
                if (userPrincipalName.Length == 0)
                {
                    userPrincipalName = tdxUser.PrimaryEmail;
                }
                if (userPrincipalName.Length > 0)
                {
                    try
                    {
                        ActiveDirectoryEntity activeDirectoryEntity = activeDirectoryContext.SearchDirectory(userPrincipalName);
                        if (activeDirectoryEntity != null)
                        {
                            // This is a valid Active Directory User.
                            this.IsValid = true;

                            // Cornell Primary Affiliation
                            if (activeDirectoryEntity.directoryProperties.ContainsKey("cornelleduPrimaryAffiliation"))
                            {
                                PrimaryAffiliation = ((List<Object>)activeDirectoryEntity.directoryProperties["cornelleduPrimaryAffiliation"])
                                    .Select(i => i.ToString())
                                    .FirstOrDefault();
                            }

                            // Cornell Affiliations
                            if (activeDirectoryEntity.directoryProperties.ContainsKey("cornelleduAffiliation"))
                            {
                                Affiliations = ((List<Object>)activeDirectoryEntity.directoryProperties["cornelleduAffiliation"])
                                    .DefaultIfEmpty(new List<String>())
                                    .Select(i => i.ToString())
                                    .ToList();
                            }
                            else
                            {
                                Affiliations = new List<String>();
                            }

                            // Cornell Entitlements
                            if (activeDirectoryEntity.directoryProperties.ContainsKey("cornelleduEntitlements"))
                            {
                                Entitlements = ((List<Object>)activeDirectoryEntity.directoryProperties["cornelleduEntitlements"])
                                    .DefaultIfEmpty(new List<String>())
                                    .Select(i => i.ToString())
                                    .ToList();
                            }
                            else
                            {
                                Entitlements = new List<String>();
                            }

                            // Cornell ProvAccounts
                            if (activeDirectoryEntity.directoryProperties.ContainsKey("cornelleduProvAccts"))
                            {
                                ProvAccts = ((List<Object>)activeDirectoryEntity.directoryProperties["cornelleduProvAccts"])
                                    .DefaultIfEmpty(new List<String>())
                                    .Select(i => i.ToString())
                                    .ToList();
                            }
                            else
                            {
                                ProvAccts = new List<String>();
                            }

                            // Cornell MailDelivery
                            if (activeDirectoryEntity.directoryProperties.ContainsKey("cornelleduMailDelivery"))
                            {
                                MailDelivery = ((List<Object>)activeDirectoryEntity.directoryProperties["cornelleduMailDelivery"])
                                    .DefaultIfEmpty(new List<String>())
                                    .Select(i => i.ToString())
                                    .ToList();
                            }
                            else
                            {
                                MailDelivery = new List<String>();
                            }

                            // Group Membership

                            MemberOf = activeDirectoryEntity.memberOf;

                            // Manager
                            if (activeDirectoryEntity.directoryProperties.ContainsKey("manager"))
                            {
                                String ManagerDN = ((List<Object>)activeDirectoryEntity.directoryProperties["manager"])
                                    .Select(i => i.ToString())
                                    .FirstOrDefault();

                                ActiveDirectoryEntity managerEntity = activeDirectoryContext.SearchDirectoryByDN(ManagerDN);
                                if (managerEntity != null)
                                {
                                    ManagerUserPrincipalName = managerEntity.userprincipalName;
                                }
                            }

                            // Active Directory Object GUID [Must Exist]
                            ActiveDirectoryObjectGUID = activeDirectoryEntity.objectGUID;

                            // Active Directory User Principal Name [Must Exist]
                            UserPrincipalName = activeDirectoryEntity.userprincipalName;

                            // Active Directory Email Address
                            EmailAddress = activeDirectoryEntity.mail;

                            // Active Directory Display Name
                            DisplayName = activeDirectoryEntity.displayName;

                            // TDX UID [Must Exist]
                            TDXUserUID = tdxUser.UID;

                            // TDX Primary Email [Must Exist]
                            TDXPrimaryEmail = tdxUser.PrimaryEmail;
                        }
                        else
                        {
                            this.IsValid = false;
                        }
                    }
                    catch (Exception exp)
                    {
                        this.IsValid = false;
                        Exceptions.Add(exp);
                    }
                }
            }
        }
    }

    #endregion --- Class Constructor ---

    #endregion ---- TDX Domain User ----

    #region ---- S111-AUTOMATIONSTATUS Enumeration ----

    // Automation Status [TDX Custom Attribute: (S111-AUTOMATIONSTATUS)]
    public static class AUTOMATIONSTATUS
    {
        public static readonly String NEW = "NEW";
        public static readonly String INPROCESS = "INPROCESS";
        public static readonly String PENDINGAPPROVAL = "PENDINGAPPROVAL";
        public static readonly String APPROVED = "APPROVED";
        public static readonly String DECLINED = "DECLINED";
        public static readonly String CANCELED = "CANCELED";
        public static readonly String COMPLETE = "COMPLETE";
    }

    #endregion ---- S111-AUTOMATIONSTATUS Enumeration ----
}