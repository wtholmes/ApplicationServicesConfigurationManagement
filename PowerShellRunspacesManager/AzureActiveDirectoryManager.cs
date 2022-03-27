using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Management.Automation;
using System.Management.Automation.Runspaces;

namespace PowerShellRunspaceManager
{
    /// <summary>
    /// This derived class implements functions from managing Azure Active Directory
    ///
    /// Version 1.0
    ///
    /// Copyright © 2010-2022 William T. Holmes All rights reserved
    ///
    /// </summary>
    public class AzureActiveDirectoryManager : PowershellRunspaces
    {
        private PSCredential psCredential;

        #region Constructors

        public AzureActiveDirectoryManager(String UserName, String Password, Boolean Connect)
        {
            // Create a credential from our username and password for our Azure Session.
            System.Security.SecureString AzureSessionPass = new System.Security.SecureString();
            foreach (char passwordChar in Password.ToCharArray())
            {
                AzureSessionPass.AppendChar(passwordChar);
            }
            AzureSessionPass.MakeReadOnly();
            psCredential = new PSCredential(UserName, AzureSessionPass);

            // Configure an initial state for the local PowerShell Session.
            InitialSessionState initialSession = InitialSessionState.CreateDefault();

            // Load the AzureAD Module into the local PowerShell Session.
            initialSession.ImportPSModule(new[] { "AzureAD" });

            // Create a PowerShell Runspace using the initial session state.
            psRunSpace = RunspaceFactory.CreateRunspace(initialSession);

            if (Connect)
            {
                // Open the PowerShell runspace.
                RunSpaceOpen();

                // Connect to the Azure Active Directory Endpoint.
                ConnectAzure();
            }
        }

        #endregion Constructors

        #region Public Methods

        /// <summary>
        /// Connects to the Azure Active Directory Endpoint.
        /// </summary>

        public void ConnectAzure()
        {
            PowerShellCommand powerShellCommand = new PowerShellCommand("Connect-AzureAD", "Credential", psCredential);
            InvokeCommand(powerShellCommand);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="UserPrincipalName"></param>
        /// <returns></returns>
        public DataSet GetAzureADUser(String UserPrincipalName)
        {
            PowerShellCommand powerShellCommand = new PowerShellCommand("Get-AzureAdUser", "ObjectID", UserPrincipalName);
            Collection<PSObject> AzureADUser = InvokeCommand(powerShellCommand);
            return PSResultsToDataSet(AzureADUser);
        }

        public void RevokeAzureADUserAllRefreshToken(String UserPrincipalName)
        {
            PowerShellCommand powerShellCommand = new PowerShellCommand("Revoke-AzureADUserAllRefreshToken", "ObjectID", UserPrincipalName);
            Collection<PSObject> CommandResults = InvokeCommand(powerShellCommand);
        }

        #endregion Public Methods
    }
}