using Google.Apis.Admin.Directory.directory_v1;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Util.Store;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace GoogleWorkspaceManager
{
    public class GoogleWorkspaceManager : IDisposable
    {
        // -----
        // Google Application Scopes
        // -----
        private String[] Scopes = {
            DirectoryService.Scope.AdminDirectoryCustomer,
            DirectoryService.Scope.AdminDirectoryDomain,
            DirectoryService.Scope.AdminDirectoryGroup,
            DirectoryService.Scope.AdminDirectoryGroupMember,
            DirectoryService.Scope.AdminDirectoryOrgunit,
            DirectoryService.Scope.AdminDirectoryUser,
            DirectoryService.Scope.AdminDirectoryUserAlias,
            GmailService.Scope.GmailSettingsBasic,
            GmailService.Scope.GmailSettingsSharing
        };

        // -----
        // Google Application Settings
        // -----

        private Dictionary<String, String> GoogleAPIClientConfiguration = new Dictionary<string, string>();

        protected String CredentialPath = "";
        protected String ApplicationName = "";
        protected String ApplicationOAuthEmail = "";
        protected String ApplicationOAuthCredential;
        protected FileInfo ConfigurationFileInfo;

        #region Constructors & Finalizers

        /// <summary>
        /// Default Constructor
        /// </summary>
        /// <param name="GoogleAPIConfig">The Google API Client Application's OAuuth2 Client Secret JSON File used to authenticate this Google Workspace Manager</param>
        public GoogleWorkspaceManager(String GoogleAPIConfiguration)
        {
            ConfigurationFileInfo = new FileInfo(GoogleAPIConfiguration);
            ApplicationOAuthCredential = ConfigurationFileInfo.Name;

            if (ConfigurationFileInfo != null)
            {
                String DirectoryPath = ConfigurationFileInfo.DirectoryName;
                if (DirectoryPath != null)
                {
                    CredentialPath = Path.Combine(DirectoryPath, ".credentials", ApplicationOAuthCredential);

                    IConfigurationRoot config = new ConfigurationBuilder()
                        .AddJsonFile(ConfigurationFileInfo.FullName)
                        .Build();
                    if (config != null)
                    {
                        GoogleAPIClientConfiguration = config.GetSection("installed").Get<Dictionary<String, String>>();
                        if (GoogleAPIClientConfiguration != null)
                        {
                            ApplicationName = GoogleAPIClientConfiguration["project_id"] as String;
                            ApplicationOAuthEmail = GoogleAPIClientConfiguration["client_id"] as String;
                        }
                    }
                }
            }
        }

        ~GoogleWorkspaceManager()
        {
        }

        #endregion Constructors & Finalizers

        #region Public Methods

        public void Dispose()
        {
        }


        /// <summary>
        /// Get an OAuth2 User Credential for the Google API Client Application.
        /// </summary>
        /// <returns></returns>
        public UserCredential GetOAuthCredential()
        {
            if (ConfigurationFileInfo != null)
            {
                using (var stream = new FileStream(ConfigurationFileInfo.FullName, FileMode.Open, FileAccess.Read))
                {
                    UserCredential GSuiteOAuthCredential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                                                                GoogleClientSecrets.FromStream(stream).Secrets,
                                                                Scopes,
                                                                ApplicationOAuthEmail,
                                                                CancellationToken.None,
                                                                new FileDataStore(CredentialPath, true)
                                                            ).Result;

                    if (GSuiteOAuthCredential != null)
                    {
                        return GSuiteOAuthCredential;
                    }
                }
            }
            return null;
        }
    }

    #endregion Public Methods
}
