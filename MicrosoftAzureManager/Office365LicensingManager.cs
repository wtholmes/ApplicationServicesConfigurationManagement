﻿using Microsoft.Graph;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

namespace MicrosoftAzureManager
{
    public class Office365LicensingManager
    {
        #region --- Private Class Properties ---

        private IConfidentialClientApplication confidentialClientApplication;
        private GraphServiceClient graphServiceClient;
        private String[] Scopes;

        #endregion --- Private Class Properties ---

        #region --- Public Class Properties ---

        public List<Office365Subscription> Office365Subscriptions { get; private set; }

        public List<Office365Subscription> Office365AssignedSubscriptions { get; private set; }

        #endregion --- Public Class Properties ---

        #region --- Constructors ----

        public Office365LicensingManager()
        {
            Scopes = new string[] { "https://graph.microsoft.com/.default" };

            confidentialClientApplication = ConfidentialClientApplicationBuilder
                    .Create("116576ba-4c7b-4211-a3bf-84ecaf05a608")
                    .WithTenantId("5d7e4366-1b9b-45cf-8e79-b14b27df46e1")
                    .WithClientSecret("bi-8Q~~Sv.DbAZmdPEsuNGWpCBxxA7J8eJDOsbxx") // or .WithCertificate(certificate)
                    .Build();

            graphServiceClient = new GraphServiceClient(new DelegateAuthenticationProvider(async (requestMessage) =>
            {
                // Retrieve an access token for Microsoft Graph (gets a fresh token if needed).
                var authResult = await confidentialClientApplication
                    .AcquireTokenForClient(Scopes)
                    .ExecuteAsync();

                // Add the access token in the Authorization header of the API request.
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResult.AccessToken);
            })
            );

            GetOffice365Subscriptions();
        }

        #endregion --- Constructors ----

        #region --- Public Methods ---

        /// <summary>
        /// Get the current Azure License Subscriptions for the Tenant.
        /// </summary>
        public void GetOffice365Subscriptions()
        {
            Office365Subscriptions = new List<Office365Subscription>();

            IGraphServiceSubscribedSkusCollectionPage SubscribedSkus = graphServiceClient.SubscribedSkus
                .Request()
                .GetAsync()
                .Result;

            foreach (SubscribedSku subscribedSku in SubscribedSkus)
            {
                Office365Subscription office365Subscription = new Office365Subscription
                {
                    SKU = subscribedSku.SkuPartNumber,
                    AvailableUnits = (Convert.ToInt32(subscribedSku.PrepaidUnits.Enabled) - Convert.ToInt32(subscribedSku.ConsumedUnits)),
                    EnabledUnits = Convert.ToInt32(subscribedSku.PrepaidUnits.Enabled),
                    ConsumedUnits = Convert.ToInt32(subscribedSku.ConsumedUnits),
                    WarningUnits = Convert.ToInt32(subscribedSku.PrepaidUnits.Warning),
                    SuspendedUnits = Convert.ToInt32(subscribedSku.PrepaidUnits.Suspended)
                };

                foreach (ServicePlanInfo servicePlanInfo in subscribedSku.ServicePlans)
                {
                    ServicePlan servicePlan = new ServicePlan
                    {
                        PlanName = servicePlanInfo.ServicePlanName,
                        ProvisioningStatus = servicePlanInfo.ProvisioningStatus
                    };
                    office365Subscription.ServicePlans.Add(servicePlan);
                }

                Office365Subscriptions.Add(office365Subscription);
            }
        }

        /// <summary>
        /// Get the Office 365 Licenses assigned to the user account specified by its UserPrincipalName
        /// </summary>
        /// <param name="UserPrincipalName">Microsoft Azure UserPricipalName</param>
        public void GetAssignedSubscriptions(String UserPrincipalName)
        {
            Office365AssignedSubscriptions = new List<Office365Subscription>();

            try
            {
                IUserLicenseDetailsCollectionPage Office365Licenses = graphServiceClient.Users[UserPrincipalName]
                        .LicenseDetails
                        .Request()
                        .GetAsync()
                        .Result;

                foreach (LicenseDetails licenseDetails in Office365Licenses)
                {
                    Office365Subscription office365Subscription = Office365Subscriptions
                        .Where(s => s.SKU.Equals(licenseDetails.SkuPartNumber)).FirstOrDefault();

                    office365Subscription.UserPrincipalName = UserPrincipalName;

                    foreach (ServicePlanInfo servicePlanInfo in licenseDetails.ServicePlans)
                    {
                        ServicePlan servicePlan = office365Subscription.ServicePlans
                            .Where(p => p.PlanName.Equals(servicePlanInfo.ServicePlanName))
                            .FirstOrDefault();

                        if (servicePlanInfo.ProvisioningStatus.Equals("Success", StringComparison.OrdinalIgnoreCase))
                        {
                            servicePlan.UserEnabled = true;
                        }
                        else
                        {
                            servicePlan.UserEnabled = false;
                        }
                    }

                    Office365AssignedSubscriptions.Add(office365Subscription);
                }
            }
            catch (Exception exp)
            {
                Console.WriteLine("Exception: {0}", exp);
            }
        }

        #endregion --- Public Methods ---

        public class Office365Subscription
        {
            public Office365Subscription()
            {
                ServicePlans = new List<ServicePlan>();
            }

            public String SKU { get; set; }

            public Int32 AvailableUnits { get; set; }

            public Int32 EnabledUnits { get; set; }

            public Int32 ConsumedUnits { get; set; }

            public Int32 WarningUnits { get; set; }

            public Int32 SuspendedUnits { get; set; }

            public List<ServicePlan> ServicePlans { get; set; }

            public String UserPrincipalName { get; set; }
        }

        public class ServicePlan
        {
            public String PlanName { get; set; }

            public String ProvisioningStatus { get; set; }

            public Boolean UserEnabled { get; set; }
        }
    }

    public class MicrosoftGraphManager
    {
        private IConfidentialClientApplication confidentialClientApplication;
        private GraphServiceClient graphServiceClient;
        private String[] Scopes;

        //Connect-ExchangeOnline -Organization cornellprod.onmicrosoft.com -AppId 5523ac52-e369-4815-b277-b88849bda983 -Certificate $Certificate -CertificatePassword $CertificatePass

        private String TennantID = "5d7e4366-1b9b-45cf-8e79-b14b27df46e1";
        private String ApplicationID = "5523ac52-e369-4815-b277-b88849bda983";
        private String CertificateFile = @"E:\Office365PowerShellRemoting.pfx";
        private List<Office365Subscription> Office365Subscriptions;

        public MicrosoftGraphManager()
        {

            X509Certificate2 x509Certificate2 = new X509Certificate2(CertificateFile);

            Scopes = new string[] { "https://graph.microsoft.com/.default" };

            confidentialClientApplication = ConfidentialClientApplicationBuilder
                    .Create(ApplicationID)
                    .WithTenantId(TennantID)
                    .WithCertificate(x509Certificate2)
                    .WithAuthority(AadAuthorityAudience.AzureAdMyOrg, true)
                    .Build();

            graphServiceClient = new GraphServiceClient(new DelegateAuthenticationProvider(async (requestMessage) =>
            {
                // Retrieve an access token for Microsoft Graph (gets a fresh token if needed).

                AuthenticationResult authenticationResult = await confidentialClientApplication
                        .AcquireTokenForClient(Scopes)
                        .ExecuteAsync();

                // Add the access token in the Authorization header of the API request.
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authenticationResult.AccessToken);
            }));
        }

        /// <summary>
        /// Get Group Members using the GroupID
        /// </summary>
        /// <param name="GroupID">The Group's ID</param>
        /// <returns></returns>
        public List<String> GetGroupMembers(String GroupID)
        {
            List<String> GroupMembers = new List<String>();
            try
            {
                try
                {
                    var result = graphServiceClient.Groups[GroupID].Members.Request().GetAsync().Result;
                    do
                    {
                        foreach (var groupMember in result)
                        {
                            GroupMembers.Add(groupMember.Id);
                        }
                    }
                    while (result.NextPageRequest != null && (result = result.NextPageRequest.GetAsync().Result).Count > 0);
                    return GroupMembers;
                }
                catch
                {
                    return null;
                }
            }
            catch (Exception exp)
            {
                return null;
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="GroupID"></param>
        /// <param name="MemberID"></param>
        public void AddGroupMember(String GroupID, String MemberID)
        {
            List<String> CurrentGroupMembers = GetGroupMembers(GroupID);
            if (!CurrentGroupMembers.Contains(MemberID))
            {
                var requestBody = new Group
                {
                    AdditionalData = new Dictionary<string, object>
                {
                    {
                        "members@odata.bind" , new List<string>
                        {
                            String.Format("https://graph.microsoft.com/v1.0/directoryObjects/{0}", MemberID)
                        }
                    },
                },
                };
                try
                {
                    var result = graphServiceClient.Groups[GroupID].Request().UpdateAsync(requestBody).Result;
                }
                catch (Exception exp)
                {

                }
            }

        }

        /// <summary>
        /// Find the User's ID using the given search string.
        /// </summary>
        /// <param name="SearchString"></param>
        /// <returns></returns>
        public String GetUser(String SearchString)
        {
            User user = null;
            try
            {
                user = graphServiceClient
                    .Users[SearchString]
                    .Request()
                    .GetAsync().Result;
                if (user != null)
                {
                    return user.Id;
                }
                else
                {
                    return null;
                }
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the list of Office365 Subscriptions
        /// </summary>
        public void GetOffice365Subscriptions()
        {
            Office365Subscriptions = new List<Office365Subscription>();

            IGraphServiceSubscribedSkusCollectionPage SubscribedSkus = graphServiceClient.SubscribedSkus
                .Request()
                .GetAsync()
                .Result;

            foreach (SubscribedSku subscribedSku in SubscribedSkus)
            {
                Office365Subscription office365Subscription = new Office365Subscription
                {
                    SKU = subscribedSku.SkuPartNumber,
                    AvailableUnits = (Convert.ToInt32(subscribedSku.PrepaidUnits.Enabled) - Convert.ToInt32(subscribedSku.ConsumedUnits)),
                    EnabledUnits = Convert.ToInt32(subscribedSku.PrepaidUnits.Enabled),
                    ConsumedUnits = Convert.ToInt32(subscribedSku.ConsumedUnits),
                    WarningUnits = Convert.ToInt32(subscribedSku.PrepaidUnits.Warning),
                    SuspendedUnits = Convert.ToInt32(subscribedSku.PrepaidUnits.Suspended)
                };

                foreach (ServicePlanInfo servicePlanInfo in subscribedSku.ServicePlans)
                {
                    ServicePlan servicePlan = new ServicePlan
                    {
                        PlanName = servicePlanInfo.ServicePlanName,
                        ProvisioningStatus = servicePlanInfo.ProvisioningStatus
                    };
                    office365Subscription.ServicePlans.Add(servicePlan);
                }

                Office365Subscriptions.Add(office365Subscription);
            }
        }

        public class Office365Subscription
        {
            public Office365Subscription()
            {
                ServicePlans = new List<ServicePlan>();
            }

            public String SKU { get; set; }

            public Int32 AvailableUnits { get; set; }

            public Int32 EnabledUnits { get; set; }

            public Int32 ConsumedUnits { get; set; }

            public Int32 WarningUnits { get; set; }

            public Int32 SuspendedUnits { get; set; }

            public List<ServicePlan> ServicePlans { get; set; }

            public String UserPrincipalName { get; set; }
        }

        public class ServicePlan
        {
            public String PlanName { get; set; }

            public String ProvisioningStatus { get; set; }

            public Boolean UserEnabled { get; set; }
        }
    }
}