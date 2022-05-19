using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using TeamDynamix.Api.Accounts;
using TeamDynamix.Api.CustomAttributes;
using TeamDynamix.Api.Apps;
using TeamDynamix.Api.Feed;
using TeamDynamix.Api.Forms;
using TeamDynamix.Api.PriorityFactors;
using TeamDynamix.Api.ServiceCatalog;
using TeamDynamix.Api.Tickets;
using TeamDynamix.Api.Users;

namespace TDXManager
{
    public partial class COEATicketManager : TDXTicketManager
    {
        public COEATicketManager()
        {
            this.SetAccountByName("CIO - CIT Enterprise Services");
            this.SetTicketFormByName("CIT - COEA Form");
            this.SetServiceByName("Email Alias");
            this.SetTicketStatusByName("New");
            this.SetTicketPriorityByName("Medium");
            this.SetTicketSourceByName("Direct Input");
        }

        public void NewPreApprovedCOEARequest(String UserPrincipalName, String EmailAddress)
        {
            // Lookup the TDX User
            User RequestingUser = this.GetTDXUserByUserPrincipalName(UserPrincipalName);

            // Set the request ticket's custom attributes.
            List<CustomAttribute> customAttributes = new List<CustomAttribute>();

            // Set the CIT - COEA RequestID
            Guid requestguid = Guid.NewGuid();
            CustomAttribute requestID = new CustomAttribute
            {
                ID = this.GetTicketAttributeByName("CIT - COEA - Request ID").ID,
                Value = requestguid.ToString(),
                ValueText = requestguid.ToString()
            };
            customAttributes.Add(requestID);

            // Set the Requested COEA Address
            CustomAttribute requestedCOEAAddress = new CustomAttribute
            {
                ID = this.GetTicketAttributeByName("CIT - COEA - Requested Address").ID,
                Value = EmailAddress,
                ValueText = EmailAddress
            };
            customAttributes.Add(requestedCOEAAddress);

            // Create a new TDX Ticket
            TDXTicket = new Ticket
            {
                RequestorUid = RequestingUser.UID,
                TypeID = ((int)TicketClass.ServiceRequest),
                Title = String.Format("Your COEA request for: {0} has been approved.", EmailAddress),
                Description = String.Format("The Cornell Optional Email Alais (COEA) you requested: {0} has beeen approved. It will take up to one hour for this request to complete the provisioning process. ",EmailAddress),
                Attributes = customAttributes
            };

            CreateNewTicket(false, true, false, false);

            //Thread.Sleep(Convert.ToInt32(new TimeSpan(0, 0, 15).TotalMilliseconds));

            this.SetTicketStatusByName("In Process");
            this.NotifyCreator = true;
            this.NotifyRequestor = true;
            UpdateTicket("The approved COEA request has been submitted for provisioning.");

            Thread.Sleep(Convert.ToInt32(new TimeSpan(0,2,0).TotalMilliseconds));

            this.SetTicketStatusByName("Resolved");
            this.NotifyCreator = true;
            this.NotifyRequestor = true;
            UpdateTicket("Your COEA request has completed.");

        }


        public void NewCOEARequest(String UserPrincipalName, String EmailAddress)
        {
            // Lookup the TDX User
            User RequestingUser = this.GetTDXUserByUserPrincipalName(UserPrincipalName);

            // Set the request ticket's custom attributes.
            List<CustomAttribute> customAttributes = new List<CustomAttribute>();

            // Set the CIT - COEA RequestID
            Guid requestguid = Guid.NewGuid();
            CustomAttribute requestID = new CustomAttribute
            {
                ID = this.GetTicketAttributeByName("CIT - COEA - Request ID").ID,
                Value = requestguid.ToString(),
                ValueText = requestguid.ToString()
            };
            customAttributes.Add(requestID);

            // Set the Requested COEA Address
            CustomAttribute requestedCOEAAddress = new CustomAttribute
            {
                ID = this.GetTicketAttributeByName("CIT - COEA - Requested Address").ID,
                Value = EmailAddress,
                ValueText = EmailAddress
            };
            customAttributes.Add(requestedCOEAAddress);

            // Create a new TDX Ticket
            TDXTicket = new Ticket
            {
                RequestorUid = RequestingUser.UID,
                TypeID = ((int)TicketClass.ServiceRequest),
                Title = String.Format("Your COEA request for: {0} has been been received and is pending approval.", EmailAddress),
                Description = String.Format("The Cornell Optional Email Alais (COEA) you requested: {0} has been received and is currently pending approval.", EmailAddress),
                Attributes = customAttributes
            };

            CreateNewTicket(false, true, false, false);

            //Thread.Sleep(Convert.ToInt32(new TimeSpan(0, 0, 15).TotalMilliseconds));

            this.SetTicketStatusByName("In Process");
            UpdateTicket("The is COEA requires approval before it can be provisioned.");

            //Thread.Sleep(Convert.ToInt32(new TimeSpan(0,0,15).TotalMilliseconds));

            //this.SetTicketStatusByName("Resolved");
            //AddTicketFeedEntry("Your COEA request has completed.", true, true);

        }




    }
}
