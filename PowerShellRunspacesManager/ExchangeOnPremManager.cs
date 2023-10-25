using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.DirectoryServices;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Net;
using System.Text.RegularExpressions;

namespace PowerShellRunspaceManager
{
    /// <summary>
    /// This derived class implements functions from managing On-Premises Microsoft Exchange
    /// Version 2.0
    ///
    /// Copyright © 2010-2022 William T. Holmes All rights reserved
    ///
    /// </summary>
    public class ExchangeOnPremManager : PowershellRunspaces
    {
        #region ---- Private Properties ----

        private String _ExchangeServer;
        private String DebugPath;

        #endregion ---- Private Properties ----

        #region ---- Public Properties ----

        public String RemoteRoutingDomain
        {
            get;
        }

        public String DefaultDomain
        {
            get;
        }

        public String DebugDirectory
        {
            get
            {
                return DebugDirectory;
            }
            set
            {
                DebugPath = String.Format(@"{0}\{1}_ExchangeOnPremDebug.log", value, DateTime.UtcNow.ToString("yyyyMMdd-HHZ"));
            }
        }

        #endregion ---- Public Properties ----

        #region ---- Class Constructors ----

        public ExchangeOnPremManager(String ExchangeServer, Boolean Connect)
        {
            // Debug Log.
            DebugPath = null;

            _ExchangeServer = ExchangeServer;

            StartOnPremExchangeSession(_ExchangeServer, Connect);

            // Get the default domain and the office 365 remote routing domain for the configured office 365 tenant.

            DefaultDomain = null;
            RemoteRoutingDomain = null;

            PowerShellCommand powerShellCommand = new PowerShellCommand("Get-AcceptedDomain");
            this.ClearExceptions();
            this.ClearLoggedMessages();
            Collection<PSObject> AcceptedDomains = InvokeCommand(powerShellCommand);

            LogProcessMessages(true, true);
            Regex RemoteRoutingDomainRegex = new Regex("mail.onmicrosoft.com$");

            foreach (PSObject AcceptedDomain in AcceptedDomains)
            {
                if (RemoteRoutingDomainRegex.IsMatch(AcceptedDomain.Properties["DomainName"].Value.ToString()))
                {
                    RemoteRoutingDomain = AcceptedDomain.Properties["DomainName"].Value.ToString();
                }

                if (Convert.ToBoolean(AcceptedDomain.Properties["Default"].Value))
                {
                    DefaultDomain = AcceptedDomain.Properties["DomainName"].Value.ToString();
                }
            }
        }

        #endregion ---- Class Constructors ----

        #region ---- Public Methods ----

        public void AddDistributionGroupMember(String DistributionGroupName, String MemberIdentity)
        {
            List<PSCommandParameter<String, Object>> pSCommandParameters = new List<PSCommandParameter<String, Object>>();
            PowerShellCommand powerShellCommand = new PowerShellCommand("Add-DistributionGroupMember");
            powerShellCommand.AddCommandParameter("Identity", DistributionGroupName);
            powerShellCommand.AddCommandParameter("Member", MemberIdentity);
            powerShellCommand.AddCommandParameter("BypassSecurityGroupManagerCheck", true);
            Collection<PSObject> addDistributionGroupMemberResult = InvokeCommand(powerShellCommand);
        }

        /// <summary>
        /// Collection<PSObject> GetMailbox(String Identity)
        ///
        /// Get-User as PSObject Collection.
        /// </summary>
        /// <param name="UserPrincpalName">Identity of the mailbox to get</param>
        /// <returns>Collection<PSObject> with mailbox data</returns>
        ///

        public Collection<PSObject> GetUser(String Identity)
        {
            PowerShellCommand powerShellCommand = new PowerShellCommand("Get-User", "Identity", Identity);
            Collection<PSObject> MailboxResult = InvokeCommand(powerShellCommand);
            return MailboxResult;
        }

        public DataSet GetUserAsDataSet(String Identity)
        {
            return PSResultsToDataSet(GetUser(Identity));
        }

        public String GetUserAsJSON(String Identity)
        {
            return DataSetToJSON(GetUserAsDataSet(Identity));
        }

        /// <summary>
        /// Collection<PSObject> GetMailbox(String Identity)
        ///
        /// Get-Recipient as PSObject Collection.
        /// </summary>
        /// <param name="UserPrincpalName">Identity of the mailbox to get</param>
        /// <returns>Collection<PSObject> with mailbox data</returns>
        public Collection<PSObject> GetRecipient(String Identity)
        {
            PowerShellCommand powerShellCommand = new PowerShellCommand("Get-Recipient", "Identity", Identity);
            Collection<PSObject> MailboxResult = InvokeCommand(powerShellCommand);
            return MailboxResult;
        }

        public DataSet GetRecipientAsDataSet(String Identity)
        {
            return PSResultsToDataSet(GetRecipient(Identity));
        }

        public String GetRecipientAsJSON(String Identity)
        {
            return DataSetToJSON(GetRecipientAsDataSet(Identity));
        }

        /// <summary>
        /// Collection<PSObject> GetMailbox(String Identity)
        ///
        /// Get-MailUser as PSObject Collection.
        /// </summary>
        /// <param name="UserPrincpalName">Identity of the mailbox to get</param>
        /// <returns>Collection<PSObject> with mailbox data</returns>
        public Collection<PSObject> GetMailUser(String Identity)
        {
            PowerShellCommand powerShellCommand = new PowerShellCommand("Get-MailUser", "Identity", Identity);
            Collection<PSObject> MailboxResult = InvokeCommand(powerShellCommand);
            return MailboxResult;
        }

        public DataSet GetMailUserAsDataSet(String Identity)
        {
            return PSResultsToDataSet(GetMailUser(Identity));
        }

        public String GetMailUserAsJSON(String Identity)
        {
            return DataSetToJSON(GetMailUserAsDataSet(Identity));
        }

        /// <summary>
        /// Collection<PSObject> GetMailbox(String Identity)
        ///
        /// Get-Mailbox as PSObject Collection.
        /// </summary>
        /// <param name="UserPrincpalName">Identity of the mailbox to get</param>
        /// <returns>Collection<PSObject> with mailbox data</returns>
        public Collection<PSObject> GetMailbox(String Identity)
        {
            PowerShellCommand powerShellCommand = new PowerShellCommand("Get-Mailbox", "Identity", Identity);
            Collection<PSObject> MailboxResult = InvokeCommand(powerShellCommand);
            return MailboxResult;
        }

        public DataSet GetMailboxAsDataSet(String Identity)
        {
            return PSResultsToDataSet(GetMailbox(Identity));
        }

        public String GetMailboxAsJSON(String Identity)
        {
            return DataSetToJSON(GetMailboxAsDataSet(Identity));
        }

        /// <summary>
        /// Collection<PSObject> GetMailbox(String Identity)
        ///
        /// Get-RemoteMailbox as PSObject Collection.
        /// </summary>
        /// <param name="UserPrincpalName">Identity of the mailbox to get</param>
        /// <returns>Collection<PSObject> with mailbox data</returns>
        public Collection<PSObject> GetRemoteMailbox(String Identity)
        {
            PowerShellCommand powerShellCommand = new PowerShellCommand("Get-RemoteMailbox", "Identity", Identity);
            Collection<PSObject> MailboxResult = InvokeCommand(powerShellCommand);
            return MailboxResult;
        }

        public DataSet GetRemoteMailboxAsDataSet(String Identity)
        {
            return PSResultsToDataSet(GetRemoteMailbox(Identity));
        }

        public String GetRemoteMailboxAsJSON(String Identity)
        {
            return DataSetToJSON(GetRemoteMailboxAsDataSet(Identity));
        }

        public Collection<PSObject> GetMailContact(String Identity)
        {
            PowerShellCommand powerShellCommand = new PowerShellCommand("Get-MailContact", "Identity", Identity);
            Collection<PSObject> MailboxResult = InvokeCommand(powerShellCommand);
            return MailboxResult;
        }

        public DataSet GetMailContactAsDataSet(String Identity)
        {
            return PSResultsToDataSet(GetMailContact(Identity));
        }

        public String GetMailContactAsJSON(String Identity)
        {
            return DataSetToJSON(GetMailContactAsDataSet(Identity));
        }

        /// <summary>
        /// Create a mail contact
        /// </summary>
        /// <param name="ExternalEmailAddress"></param>
        /// <param name="DisplayName"></param>
        /// <param name="OrganizationalUnit"></param>
        /// <returns></returns>
        public DataSet NewMailContact(String ExternalEmailAddress, String DisplayName, String OrganizationalUnit)
        {
            ObjectProvisioningState objectProvisioningState = new ObjectProvisioningState();
            objectProvisioningState.RecordTime = DateTime.UtcNow;
            objectProvisioningState.DesiredState = "MailContact";

            PowerShellCommand powerShellCommand;
            powerShellCommand = new PowerShellCommand("Get-MailContact", "Identity", ExternalEmailAddress);
            this.ClearExceptions();
            this.ClearLoggedMessages();
            Collection<PSObject> MailContacts = InvokeCommand(powerShellCommand);
            LogProcessMessages(true, true);

            if (MailContacts.Count == 0)
            {
                objectProvisioningState.CurrentState = "None";
                String ContactName = ExternalEmailAddress.Split('@')[0];

                powerShellCommand = new PowerShellCommand("New-MailContact");
                powerShellCommand.AddCommandParameter("Name", ContactName);
                powerShellCommand.AddCommandParameter("DisplayName", DisplayName);
                powerShellCommand.AddCommandParameter("ExternalEmailAddress", ExternalEmailAddress);
                powerShellCommand.AddCommandParameter("OrganizationalUnit", OrganizationalUnit);

                this.ClearExceptions();
                this.ClearLoggedMessages();
                Collection<PSObject> CommandResults = InvokeCommand(powerShellCommand);
                LogProcessMessages(true, true);
            }
            else
            {
                objectProvisioningState.CurrentState = "MailContact";
            }

            return GetMailContactAsDataSet(ExternalEmailAddress);
        }

        /// <summary>
        /// Remove a mail contact
        /// </summary>
        /// <param name="ExternalEmailAddress"></param>
        /// <returns></returns>
        public ObjectProvisioningState RemoveMailContact(String ExternalEmailAddress)
        {
            ObjectProvisioningState objectProvisioningState = new ObjectProvisioningState();
            objectProvisioningState.RecordTime = DateTime.UtcNow;
            objectProvisioningState.DesiredState = "None";

            PowerShellCommand powerShellCommand;
            powerShellCommand = new PowerShellCommand("Get-MailContact", "Identity", ExternalEmailAddress);
            this.ClearExceptions();
            this.ClearLoggedMessages();
            Collection<PSObject> MailContacts = InvokeCommand(powerShellCommand);
            LogProcessMessages(true, true);

            if (MailContacts.Count == 1)
            {
                objectProvisioningState.CurrentState = "MailContact";

                String ContactName = ExternalEmailAddress.Split('@')[0];

                powerShellCommand = new PowerShellCommand("Remove-MailContact");
                powerShellCommand.AddCommandParameter("Identity", ExternalEmailAddress);
                powerShellCommand.AddCommandParameter("Confirm", false);
                this.ClearExceptions();
                this.ClearLoggedMessages();
                Collection<PSObject> CommandResults = InvokeCommand(powerShellCommand);
                LogProcessMessages(true, true);
            }
            else
            {
                objectProvisioningState.CurrentState = "None";
            }

            return objectProvisioningState;
        }

        /// <summary>
        /// Hides the specified mail contact from the address book.
        /// </summary>
        /// <param name="ExternalEmailAddress"></param>
        /// <returns></returns>
        public ObjectProvisioningState SetMailContactHidden(String ExternalEmailAddress)
        {
            ObjectProvisioningState objectProvisioningState = new ObjectProvisioningState();
            objectProvisioningState.RecordTime = DateTime.UtcNow;
            objectProvisioningState.DesiredState = "HiddentMailContact";

            PowerShellCommand powerShellCommand;
            powerShellCommand = new PowerShellCommand("Get-MailContact", "Identity", ExternalEmailAddress);
            this.ClearExceptions();
            this.ClearLoggedMessages();
            Collection<PSObject> MailContacts = InvokeCommand(powerShellCommand);
            LogProcessMessages(true, true);

            if (MailContacts.Count == 1)
            {
                objectProvisioningState.CurrentState = "MailContact";

                String ContactName = ExternalEmailAddress.Split('@')[0];

                powerShellCommand = new PowerShellCommand("Set-MailContact");
                powerShellCommand.AddCommandParameter("Identity", ExternalEmailAddress);
                powerShellCommand.AddCommandParameter("HiddenFromAddressListsEnabled", true);
                this.ClearExceptions();
                this.ClearLoggedMessages();
                Collection<PSObject> CommandResults = InvokeCommand(powerShellCommand);
                LogProcessMessages(true, true);
            }
            else
            {
                objectProvisioningState.CurrentState = "None";
            }

            return objectProvisioningState;
        }

        /// <summary>
        /// Sets the Maximum allowed message size for the specified email contact.
        /// </summary>
        /// <param name="ExternalEmailAddress"></param>
        /// <param name="MaxMessageSize"></param>
        /// <returns></returns>
        public ObjectProvisioningState SetMailContactMaxMessageSize(String ExternalEmailAddress, String MaxMessageSize)
        {
            ObjectProvisioningState objectProvisioningState = new ObjectProvisioningState();
            objectProvisioningState.RecordTime = DateTime.UtcNow;
            objectProvisioningState.DesiredState = "MailContactWithMaxMessageSize";

            PowerShellCommand powerShellCommand;
            powerShellCommand = new PowerShellCommand("Get-MailContact", "Identity", ExternalEmailAddress);
            this.ClearExceptions();
            this.ClearLoggedMessages();
            Collection<PSObject> MailContacts = InvokeCommand(powerShellCommand);
            LogProcessMessages(true, true);

            if (MailContacts.Count == 1)
            {
                objectProvisioningState.CurrentState = "MailContact";

                powerShellCommand = new PowerShellCommand("Set-MailContact");
                powerShellCommand.AddCommandParameter("Identity", ExternalEmailAddress);
                powerShellCommand.AddCommandParameter("MaxReceiveSize", MaxMessageSize);
                this.ClearExceptions();
                this.ClearLoggedMessages();
                Collection<PSObject> CommandResults = InvokeCommand(powerShellCommand);
                LogProcessMessages(true, true);
            }
            else
            {
                objectProvisioningState.CurrentState = "None";
            }

            return objectProvisioningState;
        }

        /// <summary>
        /// Sets a the external email address for an existing mail contact and optionally retains the current external email address as a proxy address.
        /// </summary>
        /// <param name="Identity"></param>
        /// <param name="NewExternalEmailAddress"></param>
        /// <param name="RetainCurrentAddressAsProxy"></param>
        /// <returns></returns>
        public ObjectProvisioningState SetMailContactNewExternalEmailAddress(String Identity, String NewExternalEmailAddress, Boolean RetainCurrentAddressAsProxy)
        {
            ObjectProvisioningState objectProvisioningState = new ObjectProvisioningState();
            objectProvisioningState.RecordTime = DateTime.UtcNow;
            objectProvisioningState.DesiredState = "HiddentMailContact";

            PowerShellCommand powerShellCommand;
            powerShellCommand = new PowerShellCommand("Get-MailContact", "Identity", Identity);
            this.ClearExceptions();
            this.ClearLoggedMessages();
            Collection<PSObject> MailContacts = InvokeCommand(powerShellCommand);

            LogProcessMessages(true, true);

            if (MailContacts.Count == 1)
            {
                objectProvisioningState.CurrentState = "MailContact";

                powerShellCommand = new PowerShellCommand("Set-MailContact");
                powerShellCommand.AddCommandParameter("Identity", Identity);
                powerShellCommand.AddCommandParameter("ExternalEmailAddress", NewExternalEmailAddress);
                if (!RetainCurrentAddressAsProxy)
                {
                    String CurrentExternalEmailAddress = MailContacts[0].Properties["ExternalEmailAddress"].Value.ToString().Split(':')[1];
                    Hashtable ProxyUpdate = new Hashtable();
                    ProxyUpdate.Add("Remove", CurrentExternalEmailAddress);
                    powerShellCommand.AddCommandParameter("EmailAddresses", ProxyUpdate);
                }
                this.ClearExceptions();
                this.ClearLoggedMessages();
                Collection<PSObject> CommandResults = InvokeCommand(powerShellCommand);
                LogProcessMessages(true, true);
            }
            else
            {
                objectProvisioningState.CurrentState = "None";
            }

            return objectProvisioningState;
        }

        /// <summary>
        /// Enables an existing mail contact specified by its identity and sets its external email address as specified by the ExternalEmailAddress
        /// </summary>
        /// <param name="Identity"></param>
        /// <param name="ExternalEmailAddress"></param>
        /// <returns></returns>
        public ObjectProvisioningState EnableMailContact(String Identity, String ExternalEmailAddress)
        {
            ObjectProvisioningState objectProvisioningState = new ObjectProvisioningState();
            objectProvisioningState.RecordTime = DateTime.UtcNow;
            objectProvisioningState.DesiredState = "None";

            PowerShellCommand powerShellCommand;
            powerShellCommand = new PowerShellCommand("Enable-MailContact");
            powerShellCommand.AddCommandParameter("Identity", Identity);
            powerShellCommand.AddCommandParameter("ExternalEmailAddress", ExternalEmailAddress);
            this.ClearExceptions();
            this.ClearLoggedMessages();
            Collection<PSObject> MailContacts = InvokeCommand(powerShellCommand);
            LogProcessMessages(true, true);

            if (MailContacts.Count == 1)
            {
                objectProvisioningState.CurrentState = "MailContact";
            }
            else
            {
                objectProvisioningState.CurrentState = "None";
            }

            return objectProvisioningState;
        }

        /// <summary>
        ///  Disables the mailcontact specified by by identity
        /// </summary>
        /// <param name="Identity"></param>
        /// <returns></returns>
        public ObjectProvisioningState DisableMailContact(String Identity)
        {
            ObjectProvisioningState objectProvisioningState = new ObjectProvisioningState();
            objectProvisioningState.RecordTime = DateTime.UtcNow;
            objectProvisioningState.DesiredState = "None";

            PowerShellCommand powerShellCommand;
            powerShellCommand = new PowerShellCommand("Disable-MailContact");
            powerShellCommand.AddCommandParameter("Identity", Identity);
            powerShellCommand.AddCommandParameter("Confirm", false);
            this.ClearExceptions();
            this.ClearLoggedMessages();
            Collection<PSObject> MailContact = InvokeCommand(powerShellCommand);
            LogProcessMessages(true, true);

            if (MailContact.Count == 1)
            {
                objectProvisioningState.CurrentState = "Contact";
            }
            else
            {
                objectProvisioningState.CurrentState = "None";
            }

            return objectProvisioningState;
        }

        /// <summary>
        /// Enable the specified Active Directory User as a RemoteMailbox.
        /// </summary>
        /// <param name="Identity">The identity of the Active Directory User</param>
        /// <returns></returns>
        public ObjectProvisioningState EnableRemoteMailbox(String Identity)
        {
            ObjectProvisioningState ObjectProvisioningState = new ObjectProvisioningState();
            ObjectProvisioningState.RecordTime = DateTime.UtcNow;
            ObjectProvisioningState.DesiredState = "RemoteUserMailbox";

            if (_psRunSpace == null)
            {
                StartOnPremExchangeSession(_ExchangeServer, true);
            }

            PowerShellCommand powerShellCommand;
            powerShellCommand = new PowerShellCommand("Get-User", "Identity", Identity);
            this.ClearExceptions();
            this.ClearLoggedMessages();
            Collection<PSObject> UserResult = InvokeCommand(powerShellCommand);
            LogProcessMessages(true, true);

            if (UserResult.Count == 1)
            {
                String UserPrincipalName = UserResult[0].Properties["UserPrincipalName"].Value.ToString();
                String RecipientTypeDetails = UserResult[0].Properties["RecipientTypeDetails"].Value.ToString();
                String RemoteRoutingAddress = String.Format("{0}@{1}", UserPrincipalName.Split('@')[0], RemoteRoutingDomain);

                ObjectProvisioningState.CurrentState = RecipientTypeDetails;

                switch (RecipientTypeDetails)
                {
                    case "UserMailbox":

                        ObjectProvisioningState.ProvisioningAction = String.Format("NOTIMPLEMENTED no action taken for object: {0}", UserPrincipalName);

                        // Todo: Implement Move Mailbox Request.

                        break;

                    case "MailUser":

                        ObjectProvisioningState.ProvisioningAction = String.Format("Disable-MailUser; Enable-RemoteMailbox for object: {0}", UserPrincipalName);

                        // Disable the existing MailUser object.
                        powerShellCommand = new PowerShellCommand("Disable-MailUser", "Identity", UserPrincipalName);
                        powerShellCommand.AddCommandParameter("Confirm", false);
                        this.ClearExceptions();
                        this.ClearLoggedMessages();
                        InvokeCommand(powerShellCommand);
                        LogProcessMessages(true, true);
                        // Enable the RemoteMailbox.
                        powerShellCommand = new PowerShellCommand("Enable-RemoteMailbox");
                        powerShellCommand.AddCommandParameter("Identity", UserPrincipalName);
                        powerShellCommand.AddCommandParameter("RemoteRoutingAddress", RemoteRoutingAddress);
                        this.ClearExceptions();
                        this.ClearLoggedMessages();
                        InvokeCommand(powerShellCommand);
                        LogProcessMessages(true, true);

                        break;

                    case "User":

                        // Enable the RemoteMailbox.

                        ObjectProvisioningState.ProvisioningAction = String.Format("Enable-RemoteMailbox for object: {0}", UserPrincipalName);

                        powerShellCommand = new PowerShellCommand("Enable-RemoteMailbox");
                        powerShellCommand.AddCommandParameter("Identity", UserPrincipalName);
                        powerShellCommand.AddCommandParameter("RemoteRoutingAddress", RemoteRoutingAddress);
                        this.ClearExceptions();
                        this.ClearLoggedMessages();
                        InvokeCommand(powerShellCommand);
                        LogProcessMessages(true, true);
                        break;

                    case "DisabledUser":

                        ObjectProvisioningState.ProvisioningAction = String.Format("Disabled-MailUser; Enable-RemoteMailbox for object: {0}", UserPrincipalName);

                        powerShellCommand = new PowerShellCommand("Enable-RemoteMailbox");
                        powerShellCommand.AddCommandParameter("Identity", UserPrincipalName);
                        powerShellCommand.AddCommandParameter("RemoteRoutingAddress", RemoteRoutingAddress);
                        this.ClearExceptions();
                        this.ClearLoggedMessages();
                        InvokeCommand(powerShellCommand);
                        LogProcessMessages(true, true);
                        break;

                    case "LinkedUser":

                        ObjectProvisioningState.ProvisioningAction = String.Format("NOTIMPLEMENTED no action taken for object: {0}", UserPrincipalName);

                        // Not Implemented

                        break;

                    case "RemoteUserMailbox":

                        ObjectProvisioningState.ProvisioningAction = String.Format("No action required for object: {0}", UserPrincipalName);
                        // Nothing to do the principal has already been enabled as a RemoteMailbox.

                        break;

                    case "RemoteRoomMailbox":

                        ObjectProvisioningState.ProvisioningAction = String.Format("NOTIMPLEMENTED no action taken for object: {0}", UserPrincipalName);

                        // Nothing to do the principal has already been enabled as a RemoteRoomMailbox.

                        break;

                    case "RemoteEquipmentMailbox":

                        ObjectProvisioningState.ProvisioningAction = String.Format("NOTIMPLEMENTED no action taken for object: {0}", UserPrincipalName);

                        // Nothing to do the principal has already been enabled as a RemoteEquipmentMailbox.

                        break;

                    default:
                        ObjectProvisioningState.ProvisioningAction = String.Format("Object Type NOTIMPLEMENTED no action taken for object: {0}", UserPrincipalName);

                        // Not Implemented
                        break;
                }
            }

            return ObjectProvisioningState;
        }

        /// <summary>
        /// Enable the specified Active Directory User as an On-Prem Mailbox.
        /// </summary>
        /// <param name="Identity">The identity of the Active Directory User</param>
        /// <returns></returns>
        public ObjectProvisioningState EnableMailbox(String Identity)
        {
            ObjectProvisioningState ObjectProvisioningState = new ObjectProvisioningState();
            ObjectProvisioningState.RecordTime = DateTime.UtcNow;
            ObjectProvisioningState.DesiredState = "Mailbox";

            if (_psRunSpace == null)
            {
                StartOnPremExchangeSession(_ExchangeServer, true);
            }

            PowerShellCommand powerShellCommand;
            powerShellCommand = new PowerShellCommand("Get-User", "Identity", Identity);
            this.ClearExceptions();
            this.ClearLoggedMessages();
            Collection<PSObject> UserResult = InvokeCommand(powerShellCommand);
            LogProcessMessages(true, true);
            if (UserResult.Count == 1)
            {
                String UserPrincipalName = UserResult[0].Properties["UserPrincipalName"].Value.ToString();
                String RecipientTypeDetails = UserResult[0].Properties["RecipientTypeDetails"].Value.ToString();

                ObjectProvisioningState.CurrentState = RecipientTypeDetails;

                switch (RecipientTypeDetails)
                {
                    case "UserMailbox":

                        ObjectProvisioningState.ProvisioningAction = String.Format("No action required for object: {0}", UserPrincipalName);
                        // Nothing to do the principal has already been enabled as a Mailbox.
                        break;

                    case "MailUser":

                        ObjectProvisioningState.ProvisioningAction = String.Format("Disable-MailUser; Enable-RemoteMailbox for object: {0}", UserPrincipalName);

                        // Disable the existing MailUser object.
                        powerShellCommand = new PowerShellCommand("Disable-MailUser", "Identity", UserPrincipalName);
                        powerShellCommand.AddCommandParameter("Confirm", false);
                        this.ClearExceptions();
                        this.ClearLoggedMessages();
                        InvokeCommand(powerShellCommand);
                        LogProcessMessages(true, true);
                        // Enable the Mailbox.
                        powerShellCommand = new PowerShellCommand("Enable-Mailbox");
                        powerShellCommand.AddCommandParameter("Identity", UserPrincipalName);
                        this.ClearExceptions();
                        this.ClearLoggedMessages();
                        InvokeCommand(powerShellCommand);
                        LogProcessMessages(true, true);
                        break;

                    case "User":

                        // Enable the Mailbox.

                        ObjectProvisioningState.ProvisioningAction = String.Format("Enable-Mailbox for object: {0}", UserPrincipalName);

                        powerShellCommand = new PowerShellCommand("Enable-Mailbox");
                        powerShellCommand.AddCommandParameter("Identity", UserPrincipalName);
                        this.ClearExceptions();
                        this.ClearLoggedMessages();
                        InvokeCommand(powerShellCommand);
                        LogProcessMessages(true, true);
                        break;

                    case "DisabledUser":

                        ObjectProvisioningState.ProvisioningAction = String.Format("Disabled-MailUser; Enable-Mailbox for object: {0}", UserPrincipalName);

                        powerShellCommand = new PowerShellCommand("Enable-Mailbox");
                        powerShellCommand.AddCommandParameter("Identity", UserPrincipalName);
                        this.ClearExceptions();
                        this.ClearLoggedMessages();
                        InvokeCommand(powerShellCommand);
                        LogProcessMessages(true, true);
                        break;

                    case "LinkedUser":

                        ObjectProvisioningState.ProvisioningAction = String.Format("NOTIMPLEMENTED no action taken for object: {0}", UserPrincipalName);

                        // Not Implemented

                        break;

                    case "RemoteUserMailbox":

                        ObjectProvisioningState.ProvisioningAction = String.Format("NOTIMPLEMENTED no action taken for object: {0}", UserPrincipalName);

                        //Todo: Implement mailbox move request.

                        break;

                    case "RemoteRoomMailbox":

                        ObjectProvisioningState.ProvisioningAction = String.Format("NOTIMPLEMENTED no action taken for object: {0}", UserPrincipalName);

                        // Nothing to do the principal has already been enabled as a RemoteRoomMailbox.

                        break;

                    case "RemoteEquipmentMailbox":

                        ObjectProvisioningState.ProvisioningAction = String.Format("NOTIMPLEMENTED no action taken for object: {0}", UserPrincipalName);

                        // Nothing to do the principal has already been enabled as a RemoteEquipmentMailbox.

                        break;

                    default:
                        ObjectProvisioningState.ProvisioningAction = String.Format("Object Type NOTIMPLEMENTED no action taken for object: {0}", UserPrincipalName);

                        // Not Implemented
                        break;
                }
            }

            return ObjectProvisioningState;
        }

        /// <summary>
        /// Enable the specified Active Directory User as a MailUser.
        /// </summary>
        /// <param name="Identity">The identity of the active directory user to enable.</param>
        /// <param name="ExternalEmailAddress">The external email address to route the user's email to.</param>
        /// <returns></returns>

        public ObjectProvisioningState EnableMailUser(String Identity, String ExternalEmailAddress)
        {
            ObjectProvisioningState ObjectProvisioningState = new ObjectProvisioningState();
            ObjectProvisioningState.RecordTime = DateTime.UtcNow;
            ObjectProvisioningState.DesiredState = "MailUser";

            if (_psRunSpace == null)
            {
                StartOnPremExchangeSession(_ExchangeServer, true);
            }

            PowerShellCommand powerShellCommand;
            powerShellCommand = new PowerShellCommand("Get-User", "Identity", Identity);
            this.ClearExceptions();
            this.ClearLoggedMessages();
            Collection<PSObject> UserResult = InvokeCommand(powerShellCommand);
            LogProcessMessages(true, true);
            if (UserResult.Count == 1)
            {
                String UserPrincipalName = UserResult[0].Properties["UserPrincipalName"].Value.ToString();
                String RecipientTypeDetails = UserResult[0].Properties["RecipientTypeDetails"].Value.ToString();

                ObjectProvisioningState.CurrentState = RecipientTypeDetails;

                switch (RecipientTypeDetails)
                {
                    case "UserMailbox":

                        ObjectProvisioningState.ProvisioningAction = String.Format("Disable-Mailbox; Enable-MailUser for object: {0}", UserPrincipalName);

                        // Disable the existing Mailbox object.
                        powerShellCommand = new PowerShellCommand("Disable-Mailbox", "Identity", UserPrincipalName);
                        powerShellCommand.AddCommandParameter("Confirm", false);
                        this.ClearExceptions();
                        this.ClearLoggedMessages();
                        InvokeCommand(powerShellCommand);
                        LogProcessMessages(true, true);
                        // Enable the MailUser.
                        powerShellCommand = new PowerShellCommand("Enable-MailUser");
                        powerShellCommand.AddCommandParameter("Identity", UserPrincipalName);
                        powerShellCommand.AddCommandParameter("ExternalEmailAddress", ExternalEmailAddress);
                        this.ClearExceptions();
                        this.ClearLoggedMessages();
                        InvokeCommand(powerShellCommand);
                        LogProcessMessages(true, true);
                        break;

                    case "MailUser":

                        powerShellCommand = new PowerShellCommand("Get-MailUser", "Identity", Identity);
                        this.ClearExceptions();
                        this.ClearLoggedMessages();
                        Collection<PSObject> MailUsers = InvokeCommand(powerShellCommand);
                        LogProcessMessages(true, true);

                        if (MailUsers.Count == 1)
                        {
                            if (!MailUsers[0].Properties["ExternalEmailAddress"].ToString().Equals(ExternalEmailAddress, StringComparison.CurrentCultureIgnoreCase))
                            {
                                powerShellCommand = new PowerShellCommand("Set-MailUser");
                                powerShellCommand.AddCommandParameter("Identity", Identity);
                                powerShellCommand.AddCommandParameter("ExternalEmailAddress", ExternalEmailAddress);
                                this.ClearExceptions();
                                this.ClearLoggedMessages();
                                Collection<PSObject> CommandResults = InvokeCommand(powerShellCommand);
                                LogProcessMessages(true, true);
                                ObjectProvisioningState.ProvisioningAction = String.Format("Set-MailUser -ExternalEmailAddress {0} for object: {1}", ExternalEmailAddress, UserPrincipalName);
                            }
                            else
                            {
                                ObjectProvisioningState.ProvisioningAction = String.Format("No action required for object: {0}", UserPrincipalName);
                            }
                        }
                        else
                        {
                            ObjectProvisioningState.ProvisioningAction = String.Format("Error: MailUser: {0} was not found.", UserPrincipalName);
                        }

                        break;

                    case "User":

                        ObjectProvisioningState.ProvisioningAction = String.Format("Enable-MailUser for object: {0}", UserPrincipalName);

                        // Enable the MailUser.
                        powerShellCommand = new PowerShellCommand("Enable-MailUser");
                        powerShellCommand.AddCommandParameter("Identity", UserPrincipalName);
                        powerShellCommand.AddCommandParameter("ExternalEmailAddress", ExternalEmailAddress);
                        this.ClearExceptions();
                        this.ClearLoggedMessages();
                        InvokeCommand(powerShellCommand);
                        using (StreamWriter sw = File.AppendText(DebugPath))
                        {
                            foreach (String LoggedMessage in this.LoggedMessages)
                            {
                                sw.WriteLine(LoggedMessage);
                            }
                            foreach (Exception exp in this.Exceptions)
                            {
                                sw.WriteLine("\n\n{0}", exp);
                            }
                        }

                        break;

                    case "DisabledUser":

                        ObjectProvisioningState.ProvisioningAction = String.Format("Enable-MailUser for object: {0}", UserPrincipalName);

                        // Enable the MailUser.
                        powerShellCommand = new PowerShellCommand("Enable-MailUser");
                        powerShellCommand.AddCommandParameter("Identity", UserPrincipalName);
                        powerShellCommand.AddCommandParameter("ExternalEmailAddress", ExternalEmailAddress);
                        this.ClearExceptions();
                        this.ClearLoggedMessages();
                        InvokeCommand(powerShellCommand);
                        LogProcessMessages(true, true);
                        break;

                    case "LinkedUser":

                        ObjectProvisioningState.ProvisioningAction = String.Format("NOTIMPLEMENTED no action taken for object: {0}", UserPrincipalName);
                        // Not Implemented

                        break;

                    case "RemoteUserMailbox":

                        ObjectProvisioningState.ProvisioningAction = String.Format("Disable-RemoteMailbox; Enable-MailUser for object: {0}", UserPrincipalName);

                        // Disable the existing RemoteMailbox object.
                        powerShellCommand = new PowerShellCommand("Disable-RemoteMailbox", "Identity", UserPrincipalName);
                        powerShellCommand.AddCommandParameter("Confirm", false);
                        this.ClearExceptions();
                        this.ClearLoggedMessages();
                        InvokeCommand(powerShellCommand);
                        LogProcessMessages(true, true);
                        // Enable the MailUser.
                        powerShellCommand = new PowerShellCommand("Enable-MailUser");
                        powerShellCommand.AddCommandParameter("Identity", UserPrincipalName);
                        powerShellCommand.AddCommandParameter("ExternalEmailAddress", ExternalEmailAddress);
                        this.ClearExceptions();
                        this.ClearLoggedMessages();
                        InvokeCommand(powerShellCommand);
                        LogProcessMessages(true, true);
                        break;

                    case "RemoteRoomMailbox":

                        ObjectProvisioningState.ProvisioningAction = String.Format("NOTIMPLEMENTED no action taken for object: {0}", UserPrincipalName);
                        // Not Implemented

                        break;

                    case "RemoteEquipmentMailbox":

                        ObjectProvisioningState.ProvisioningAction = String.Format("NOTIMPLEMENTED no action taken for object: {0}", UserPrincipalName);
                        // Not Implemented

                        break;

                    default:
                        ObjectProvisioningState.ProvisioningAction = String.Format("Object Type NOTIMPLEMENTED no action taken for object: {0}", UserPrincipalName);
                        // Not Implemented

                        break;
                }
            }
            return ObjectProvisioningState;
        }

        /// <summary>
        /// Disable the specified Active Directory Users's mail flow.
        /// </summary>
        /// <param name="Identity">The identity of the Active Directory User</param>
        /// <returns></returns>
        public ObjectProvisioningState DisableUserMailFlow(String Identity)
        {
            ObjectProvisioningState ObjectProvisioningState = new ObjectProvisioningState();
            ObjectProvisioningState.RecordTime = DateTime.UtcNow;
            ObjectProvisioningState.DesiredState = "User";

            if (_psRunSpace == null)
            {
                StartOnPremExchangeSession(_ExchangeServer, true);
            }
            try
            {
                PowerShellCommand powerShellCommand;
                powerShellCommand = new PowerShellCommand("Get-User", "Identity", Identity);
                this.ClearExceptions();
                this.ClearLoggedMessages();
                Collection<PSObject> UserResult = InvokeCommand(powerShellCommand);
                LogProcessMessages(true, true);
                if (UserResult != null)
                {
                    if (UserResult.Count == 1)
                    {
                        String UserPrincipalName = UserResult[0].Properties["UserPrincipalName"].Value.ToString();
                        String RecipientTypeDetails = UserResult[0].Properties["RecipientTypeDetails"].Value.ToString();

                        ObjectProvisioningState.CurrentState = RecipientTypeDetails;

                        switch (RecipientTypeDetails)
                        {
                            case "UserMailbox":

                                ObjectProvisioningState.ProvisioningAction = String.Format("Disable-Mailbox for object: {0}", UserPrincipalName);

                                // Disable the existing Mailbox object.

                                powerShellCommand = new PowerShellCommand("Disable-Mailbox", "Identity", UserPrincipalName);
                                powerShellCommand.AddCommandParameter("Confirm", false);
                                this.ClearExceptions();
                                this.ClearLoggedMessages();
                                InvokeCommand(powerShellCommand);
                                LogProcessMessages(true, true);
                                break;

                            case "MailUser":

                                ObjectProvisioningState.ProvisioningAction = String.Format("Disable-MailUser for object: {0}", UserPrincipalName);

                                // Disable the existing MailUser object.

                                powerShellCommand = new PowerShellCommand("Disable-MailUser", "Identity", UserPrincipalName);
                                powerShellCommand.AddCommandParameter("Confirm", false);
                                this.ClearExceptions();
                                this.ClearLoggedMessages();
                                InvokeCommand(powerShellCommand);
                                LogProcessMessages(true, true);
                                break;

                            case "User":
                                ObjectProvisioningState.ProvisioningAction = String.Format("No action required for object: {0}", UserPrincipalName);

                                // Nothing to do, the principal has no mail-flow configured.

                                break;

                            case "DisabledUser":

                                ObjectProvisioningState.ProvisioningAction = String.Format("NOTIMPLEMENTED no action taken for object: {0}", UserPrincipalName);

                                // Not Implemented

                                break;

                            case "LinkedUser":
                                ObjectProvisioningState.ProvisioningAction = String.Format("NOTIMPLEMENTED no action taken for object: {0}", UserPrincipalName);

                                // Not Implemented

                                break;

                            case "RemoteUserMailbox":

                                ObjectProvisioningState.ProvisioningAction = String.Format("Disable-RemoteMailbox for object: {0}", UserPrincipalName);

                                // Disable the existing RemoteMailbox object.
                                powerShellCommand = new PowerShellCommand("Disable-RemoteMailbox", "Identity", UserPrincipalName);
                                powerShellCommand.AddCommandParameter("Confirm", false);
                                InvokeCommand(powerShellCommand); this.ClearExceptions();
                                this.ClearLoggedMessages();
                                InvokeCommand(powerShellCommand);
                                LogProcessMessages(true, true);
                                break;

                            case "RemoteRoomMailbox":
                                ObjectProvisioningState.ProvisioningAction = String.Format("NOTIMPLEMENTED no action taken for object: {0}", UserPrincipalName);

                                // Not Implemented

                                break;

                            case "RemoteEquipmentMailbox":

                                ObjectProvisioningState.ProvisioningAction = String.Format("NOTIMPLEMENTED no action taken for object: {0}", UserPrincipalName);
                                // Not Implemented

                                break;

                            default:
                                ObjectProvisioningState.ProvisioningAction = String.Format("Object Type NOTIMPLEMENTED no action taken for object: {0}", UserPrincipalName);
                                // Not Implemented
                                break;
                        }
                    }
                }
                else
                {
                }
            }
            catch (Exception exp)
            {
                if (DebugPath != null)
                {
                    using (StreamWriter sw = File.AppendText(DebugPath))
                    {
                        sw.WriteLine("[{0}]General Exception:\n\n{1}", DateTime.UtcNow, exp);
                    }
                }
            }

            return ObjectProvisioningState;
        }

        /// <summary>
        /// Add an Email Address to the specified Email Recipient.
        /// </summary>
        /// <param name="Identity">The identity of the email recipient</param>
        /// <param name="EmailAddress">The email address to add to the recipient</param>
        /// <param name="IsPrimary">Sets whether or not the address specified should be the primary address for the recipient</param>
        /// <returns></returns>
        public ObjectProvisioningState AddEmailAddress(String Identity, String EmailAddress, Boolean IsPrimary)
        {
            ObjectProvisioningState ObjectProvisioningState = new ObjectProvisioningState();
            ObjectProvisioningState.RecordTime = DateTime.UtcNow;
            ObjectProvisioningState.DesiredState = "Recipient with additional email address.";

            if (_psRunSpace == null)
            {
                StartOnPremExchangeSession(_ExchangeServer, true);
            }

            PowerShellCommand powerShellCommand;
            powerShellCommand = new PowerShellCommand("Get-User", "Identity", Identity);
            this.ClearExceptions();
            this.ClearLoggedMessages();
            Collection<PSObject> UserResult = InvokeCommand(powerShellCommand);
            LogProcessMessages(true, true);

            if (UserResult.Count == 1)
            {
                String UserPrincipalName = UserResult[0].Properties["UserPrincipalName"].Value.ToString();
                String RecipientTypeDetails = UserResult[0].Properties["RecipientTypeDetails"].Value.ToString();
                ObjectProvisioningState.CurrentState = RecipientTypeDetails;

                // Create the Add List.
                Hashtable EmailAddressesToAdd = new Hashtable();
                EmailAddressesToAdd.Add("add", EmailAddress);

                switch (RecipientTypeDetails)
                {
                    case "UserMailbox":

                        powerShellCommand = new PowerShellCommand("Get-Mailbox", "Identity", Identity);
                        this.ClearExceptions();
                        this.ClearLoggedMessages();
                        Collection<PSObject> MailboxResult = InvokeCommand(powerShellCommand);
                        LogProcessMessages(true, true);
                        ObjectProvisioningState.ProvisioningAction = String.Format("Set-Mailbox for object: {0}", UserPrincipalName);

                        // Set Mailbox
                        powerShellCommand = new PowerShellCommand("Set-Mailbox", "Identity", UserPrincipalName);
                        powerShellCommand.AddCommandParameter("EmailAddresses", EmailAddressesToAdd);

                        if (IsPrimary)
                        {
                            powerShellCommand.AddCommandParameter("PrimarySmtpAddress", EmailAddress);
                            powerShellCommand.AddCommandParameter("EmailAddressPolicyEnabled", false);
                        }

                        this.ClearExceptions();
                        this.ClearLoggedMessages();
                        InvokeCommand(powerShellCommand);
                        LogProcessMessages(true, true);
                        break;

                    case "MailUser":

                        powerShellCommand = new PowerShellCommand("Get-MailUser", "Identity", Identity);
                        this.ClearExceptions();
                        this.ClearLoggedMessages();
                        Collection<PSObject> MailUserResult = InvokeCommand(powerShellCommand);
                        LogProcessMessages(true, true);

                        // Set MailUser
                        powerShellCommand = new PowerShellCommand("Set-MailUser", "Identity", UserPrincipalName);
                        powerShellCommand.AddCommandParameter("EmailAddresses", EmailAddressesToAdd);
                        if (IsPrimary)
                        {
                            powerShellCommand.AddCommandParameter("PrimarySmtpAddress", EmailAddress);
                            powerShellCommand.AddCommandParameter("EmailAddressPolicyEnabled", false);
                        }

                        this.ClearExceptions();
                        this.ClearLoggedMessages();
                        InvokeCommand(powerShellCommand);
                        LogProcessMessages(true, true);
                        break;

                    case "User":

                        // No action to take on a user.
                        ObjectProvisioningState.ProvisioningAction = String.Format("NOTIMPLEMENTED no action taken for object: {0}", UserPrincipalName);
                        break;

                    case "DisabledUser":

                        // No action to take on a disabled user
                        ObjectProvisioningState.ProvisioningAction = String.Format("NOTIMPLEMENTED no action taken for object: {0}", UserPrincipalName);
                        break;

                    case "LinkedUser":

                        ObjectProvisioningState.ProvisioningAction = String.Format("NOTIMPLEMENTED no action taken for object: {0}", UserPrincipalName);
                        // Not Implemented

                        break;

                    case "RemoteUserMailbox":

                        powerShellCommand = new PowerShellCommand("Get-RemoteMailbox", "Identity", Identity);
                        this.ClearExceptions();
                        this.ClearLoggedMessages();
                        Collection<PSObject> RemoteMailboxResult = InvokeCommand(powerShellCommand);
                        LogProcessMessages(true, true);
                        // Set RemoteMailbox
                        powerShellCommand = new PowerShellCommand("Set-RemoteMailbox", "Identity", UserPrincipalName);
                        powerShellCommand.AddCommandParameter("EmailAddresses", EmailAddressesToAdd);
                        if (IsPrimary)
                        {
                            powerShellCommand.AddCommandParameter("PrimarySmtpAddress", EmailAddress);
                            powerShellCommand.AddCommandParameter("EmailAddressPolicyEnabled", false);
                        }

                        this.ClearExceptions();
                        this.ClearLoggedMessages();
                        InvokeCommand(powerShellCommand);
                        LogProcessMessages(true, true);
                        break;

                    case "RemoteRoomMailbox":

                        // Set RemoteMailbox
                        powerShellCommand = new PowerShellCommand("Set-RemoteMailbox", "Identity", UserPrincipalName);
                        powerShellCommand.AddCommandParameter("EmailAddresses", EmailAddressesToAdd);
                        if (IsPrimary)
                        {
                            powerShellCommand.AddCommandParameter("PrimarySmtpAddress", EmailAddress);
                            powerShellCommand.AddCommandParameter("EmailAddressPolicyEnabled", false);
                        }

                        break;

                    case "RemoteEquipmentMailbox":

                        // Set RemoteMailbox
                        powerShellCommand = new PowerShellCommand("Set-RemoteMailbox", "Identity", UserPrincipalName);
                        powerShellCommand.AddCommandParameter("Identity", UserPrincipalName);
                        powerShellCommand.AddCommandParameter("EmailAddresses", EmailAddressesToAdd);
                        if (IsPrimary)
                        {
                            powerShellCommand.AddCommandParameter("PrimarySmtpAddress", EmailAddress);
                            powerShellCommand.AddCommandParameter("EmailAddressPolicyEnabled", false);
                        }

                        break;

                    default:
                        ObjectProvisioningState.ProvisioningAction = String.Format("Object Type NOTIMPLEMENTED no action taken for object: {0}", UserPrincipalName);
                        // Not Implemented

                        break;
                }
            }
            return ObjectProvisioningState;
        }

        /// <summary>
        /// Set the specified EmailAddress as the primary SMTP address for the recipient.
        /// </summary>
        /// <param name="Identity">Identity of the recipient to set the primary SMTP address for</param>
        /// <param name="EmailAddress">Address you are setting as primary</param>
        /// <returns></returns>
        public ObjectProvisioningState SetEmailAddressPrimary(String Identity, String EmailAddress, Boolean EnableByPolicy)
        {
            ObjectProvisioningState ObjectProvisioningState = new ObjectProvisioningState();
            ObjectProvisioningState.RecordTime = DateTime.UtcNow;
            ObjectProvisioningState.DesiredState = "RecipientWithSpecifPrimaryAddress";

            if (_psRunSpace == null)
            {
                StartOnPremExchangeSession(_ExchangeServer, true);
            }

            PowerShellCommand powerShellCommand;
            powerShellCommand = new PowerShellCommand("Get-User", "Identity", Identity);
            Collection<PSObject> UserResult = InvokeCommand(powerShellCommand);
            if (UserResult.Count == 1)
            {
                String UserPrincipalName = UserResult[0].Properties["UserPrincipalName"].Value.ToString();
                String RecipientTypeDetails = UserResult[0].Properties["RecipientTypeDetails"].Value.ToString();
                ObjectProvisioningState.CurrentState = String.Format("{0}WithDefaultPrimaryAddress", RecipientTypeDetails);

                String CurrentPrimarySMTPAdddress;
                Boolean CurrentEmailAddressPolicyEnabled;
                String CurrentAlias;
                Boolean UpdateRequired;

                switch (RecipientTypeDetails)
                {
                    case "UserMailbox":

                        ObjectProvisioningState.ProvisioningAction = String.Format("Set PrimarySMTPAddress {0}", UserPrincipalName);

                        powerShellCommand = new PowerShellCommand("Get-Mailbox", "Identity", Identity);
                        this.ClearExceptions();
                        this.ClearLoggedMessages();
                        Collection<PSObject> MailboxResult = InvokeCommand(powerShellCommand);
                        LogProcessMessages(true, true);

                        // Get the recipient's current configuration values.
                        CurrentPrimarySMTPAdddress = MailboxResult[0].Properties["PrimarySmtpAddress"].Value.ToString();
                        CurrentEmailAddressPolicyEnabled = Convert.ToBoolean(MailboxResult[0].Properties["EmailAddressPolicyEnabled"].Value.ToString());
                        CurrentAlias = MailboxResult[0].Properties["Alias"].Value.ToString();
                        UpdateRequired = false;

                        if (CurrentPrimarySMTPAdddress.Equals(EmailAddress, StringComparison.InvariantCultureIgnoreCase))
                        {
                            String[] EmailAddressParts = EmailAddress.Split('@');

                            if (CurrentAlias.Equals(EmailAddressParts[0], StringComparison.InvariantCultureIgnoreCase) && DefaultDomain.Equals(EmailAddressParts[1], StringComparison.InvariantCultureIgnoreCase))
                            {
                                // The current PrimarySmtpAddress matches the default address policy pattern.
                                if (CurrentEmailAddressPolicyEnabled && EnableByPolicy)
                                {
                                    UpdateRequired = false;
                                }
                                else
                                {
                                    UpdateRequired = true;
                                }
                            }
                            else
                            {
                                if (!CurrentEmailAddressPolicyEnabled && EnableByPolicy)
                                {
                                    UpdateRequired = true;
                                }
                                else
                                {
                                    UpdateRequired = false;
                                }
                            }
                        }
                        else
                        {
                            UpdateRequired = true;
                        }

                        if (UpdateRequired)
                        {
                            // Set Mailbox
                            powerShellCommand = new PowerShellCommand("Set-Mailbox", "Identity", UserPrincipalName);
                            powerShellCommand.AddCommandParameter("PrimarySmtpAddress", EmailAddress);
                            powerShellCommand.AddCommandParameter("EmailAddressPolicyEnabled", false);
                            this.ClearExceptions();
                            this.ClearLoggedMessages();
                            InvokeCommand(powerShellCommand);
                            LogProcessMessages(true, true);
                            if (EnableByPolicy)
                            {
                                String LocalPart = EmailAddress.Split('@')[0];
                                powerShellCommand = new PowerShellCommand("Set-Mailbox", "Identity", UserPrincipalName);
                                powerShellCommand.AddCommandParameter("Alias", LocalPart);
                                powerShellCommand.AddCommandParameter("EmailAddressPolicyEnabled", true);
                                this.ClearExceptions();
                                this.ClearLoggedMessages();
                                InvokeCommand(powerShellCommand);
                                LogProcessMessages(true, true);
                            }
                        }
                        break;

                    case "MailUser":

                        powerShellCommand = new PowerShellCommand("Get-MailUser", "Identity", Identity);
                        this.ClearExceptions();
                        this.ClearLoggedMessages();
                        Collection<PSObject> MailUserResult = InvokeCommand(powerShellCommand);
                        LogProcessMessages(true, true);
                        // Get the recipient's current configuration values.
                        CurrentPrimarySMTPAdddress = MailUserResult[0].Properties["PrimarySmtpAddress"].Value.ToString();
                        CurrentEmailAddressPolicyEnabled = Convert.ToBoolean(MailUserResult[0].Properties["EmailAddressPolicyEnabled"].Value.ToString());
                        CurrentAlias = MailUserResult[0].Properties["Alias"].Value.ToString();
                        UpdateRequired = false;

                        if (CurrentPrimarySMTPAdddress.Equals(EmailAddress, StringComparison.InvariantCultureIgnoreCase))
                        {
                            String[] EmailAddressParts = EmailAddress.Split('@');

                            if (CurrentAlias.Equals(EmailAddressParts[0], StringComparison.InvariantCultureIgnoreCase) && DefaultDomain.Equals(EmailAddressParts[1], StringComparison.InvariantCultureIgnoreCase))
                            {
                                // The current PrimarySmtpAddress matches the default address policy pattern.
                                if (CurrentEmailAddressPolicyEnabled && EnableByPolicy)
                                {
                                    UpdateRequired = false;
                                }
                                else
                                {
                                    UpdateRequired = true;
                                }
                            }
                            else
                            {
                                if (!CurrentEmailAddressPolicyEnabled && EnableByPolicy)
                                {
                                    UpdateRequired = true;
                                }
                                else
                                {
                                    UpdateRequired = false;
                                }
                            }
                        }
                        else
                        {
                            UpdateRequired = true;
                        }

                        // Set MailUser
                        if (UpdateRequired)
                        {
                            ObjectProvisioningState.ProvisioningAction = String.Format("Set PrimarySMTPAddress {0}", UserPrincipalName);
                            powerShellCommand = new PowerShellCommand("Set-MailUser", "Identity", UserPrincipalName);
                            powerShellCommand.AddCommandParameter("PrimarySmtpAddress", EmailAddress);
                            powerShellCommand.AddCommandParameter("EmailAddressPolicyEnabled", false);
                            this.ClearExceptions();
                            this.ClearLoggedMessages();
                            InvokeCommand(powerShellCommand);
                            using (StreamWriter sw = File.AppendText(DebugPath))
                            {
                                foreach (String LoggedMessage in this.LoggedMessages)
                                {
                                    sw.WriteLine(LoggedMessage);
                                }
                                foreach (Exception exp in this.Exceptions)
                                {
                                    sw.WriteLine("\n\n{0}", exp);
                                }
                            }

                            if (EnableByPolicy)
                            {
                                String LocalPart = EmailAddress.Split('@')[0];
                                powerShellCommand = new PowerShellCommand("Set-MailUser", "Identity", UserPrincipalName);
                                powerShellCommand.AddCommandParameter("Alias", LocalPart);
                                powerShellCommand.AddCommandParameter("EmailAddressPolicyEnabled", true);
                                this.ClearExceptions();
                                this.ClearLoggedMessages();
                                InvokeCommand(powerShellCommand);
                                LogProcessMessages(true, true);
                            }
                        }
                        break;

                    case "User":

                        // No action to take on a user.
                        ObjectProvisioningState.ProvisioningAction = String.Format("NOTIMPLEMENTED no action taken for object: {0}", UserPrincipalName);
                        break;

                    case "DisabledUser":

                        // No action to take on a disabled user
                        ObjectProvisioningState.ProvisioningAction = String.Format("NOTIMPLEMENTED no action taken for object: {0}", UserPrincipalName);
                        break;

                    case "LinkedUser":

                        ObjectProvisioningState.ProvisioningAction = String.Format("NOTIMPLEMENTED no action taken for object: {0}", UserPrincipalName);
                        // Not Implemented

                        break;

                    case "RemoteUserMailbox":

                        powerShellCommand = new PowerShellCommand("Get-RemoteMailbox", "Identity", Identity);
                        this.ClearExceptions();
                        this.ClearLoggedMessages();
                        Collection<PSObject> RemoteMailboxResult = InvokeCommand(powerShellCommand);
                        LogProcessMessages(true, true);
                        // Get the recipient's current configuration values.
                        CurrentPrimarySMTPAdddress = RemoteMailboxResult[0].Properties["PrimarySmtpAddress"].Value.ToString();
                        CurrentEmailAddressPolicyEnabled = Convert.ToBoolean(RemoteMailboxResult[0].Properties["EmailAddressPolicyEnabled"].Value.ToString());
                        CurrentAlias = RemoteMailboxResult[0].Properties["Alias"].Value.ToString();
                        UpdateRequired = false;

                        if (CurrentPrimarySMTPAdddress.Equals(EmailAddress, StringComparison.InvariantCultureIgnoreCase))
                        {
                            String[] EmailAddressParts = EmailAddress.Split('@');

                            if (CurrentAlias.Equals(EmailAddressParts[0], StringComparison.InvariantCultureIgnoreCase) && DefaultDomain.Equals(EmailAddressParts[1], StringComparison.InvariantCultureIgnoreCase))
                            {
                                // The current PrimarySmtpAddress matches the default address policy pattern.
                                if (CurrentEmailAddressPolicyEnabled && EnableByPolicy)
                                {
                                    UpdateRequired = false;
                                }
                                else
                                {
                                    UpdateRequired = true;
                                }
                            }
                            else
                            {
                                if (!CurrentEmailAddressPolicyEnabled && EnableByPolicy)
                                {
                                    UpdateRequired = true;
                                }
                                else
                                {
                                    UpdateRequired = false;
                                }
                            }
                        }
                        else
                        {
                            UpdateRequired = true;
                        }

                        if (UpdateRequired)
                        {
                            // Set RemoteMailbox
                            ObjectProvisioningState.ProvisioningAction = String.Format("Set PrimarySMTPAddress {0}", UserPrincipalName);
                            powerShellCommand = new PowerShellCommand("Set-RemoteMailbox", "Identity", UserPrincipalName);
                            powerShellCommand.AddCommandParameter("PrimarySmtpAddress", EmailAddress);
                            powerShellCommand.AddCommandParameter("EmailAddressPolicyEnabled", false);
                            this.ClearExceptions();
                            this.ClearLoggedMessages();
                            InvokeCommand(powerShellCommand);
                            LogProcessMessages(true, true);
                            if (EnableByPolicy)
                            {
                                String LocalPart = EmailAddress.Split('@')[0];
                                powerShellCommand = new PowerShellCommand("Set-RemoteMailbox", "Identity", UserPrincipalName);
                                powerShellCommand.AddCommandParameter("Alias", LocalPart);
                                powerShellCommand.AddCommandParameter("EmailAddressPolicyEnabled", true);
                                this.ClearExceptions();
                                this.ClearLoggedMessages();
                                InvokeCommand(powerShellCommand);
                                LogProcessMessages(true, true);
                            }
                        }
                        break;

                    case "RemoteRoomMailbox":

                        // Set RemoteMailbox
                        ObjectProvisioningState.ProvisioningAction = String.Format("Set PrimarySMTPAddress {0}", UserPrincipalName);
                        powerShellCommand = new PowerShellCommand("Set-RemoteMailbox", "Identity", UserPrincipalName);
                        powerShellCommand.AddCommandParameter("PrimarySmtpAddress", EmailAddress);
                        powerShellCommand.AddCommandParameter("EmailAddressPolicyEnabled", false);

                        break;

                    case "RemoteEquipmentMailbox":

                        // Set RemoteMailbox
                        ObjectProvisioningState.ProvisioningAction = String.Format("Set PrimarySMTPAddress {0}", UserPrincipalName);
                        powerShellCommand = new PowerShellCommand("Set-RemoteMailbox", "Identity", UserPrincipalName);
                        powerShellCommand.AddCommandParameter("PrimarySmtpAddress", EmailAddress);
                        powerShellCommand.AddCommandParameter("EmailAddressPolicyEnabled", false);

                        break;

                    default:
                        ObjectProvisioningState.ProvisioningAction = String.Format("Object Type NOTIMPLEMENTED no action taken for object: {0}", UserPrincipalName);
                        // Not Implemented

                        break;
                }
            }
            return ObjectProvisioningState;
        }

        #endregion ---- Public Methods ----

        #region ---- Private Methods ----

        private void LogProcessMessages(Boolean ToFile, Boolean ToSyslog)
        {
            try
            {
                if (ToFile)
                {
                    if (DebugPath != null)
                    {
                        using (StreamWriter sw = File.AppendText(DebugPath))
                        {
                            foreach (String LoggedMessage in this.LoggedMessages)
                            {
                                sw.WriteLine(LoggedMessage);
                            }
                            foreach (Exception exp in this.Exceptions)
                            {
                                sw.WriteLine("\n\n{0}", exp);
                            }
                        }
                    }
                }
            }
            catch
            {
                // Don't let a log failure cause a fatal exception.
            }
        }

        private void StartOnPremExchangeSession(String ExchangeServer, Boolean Connect)
        {
            // Start an exchange remote powershell session.
            Uri exchangeURI = new Uri(String.Format("http://{0}/PowerShell", ExchangeServer));
            String powerShellSchema = "http://schemas.microsoft.com/powershell/Microsoft.Exchange";
            PSCredential psCredential = (PSCredential)null;
            connectionInfo = new WSManConnectionInfo(exchangeURI, powerShellSchema, psCredential);

            // ------
            // Set connection parameters
            // ------
            connectionInfo.AuthenticationMechanism = AuthenticationMechanism.Default;   // Authentication Method.
            connectionInfo.MaximumConnectionRedirectionCount = 1000;                    // Allowed Redirects.
            connectionInfo.OperationTimeout = new TimeSpan(0, 4, 0).Milliseconds;       // 4 minutes operation timeout.
            connectionInfo.OpenTimeout = new TimeSpan(0, 10, 0).Milliseconds;           // 10 second connection timeout.

            // Create the Runspace
            psRunSpace = RunspaceFactory.CreateRunspace(connectionInfo);

            if (Connect)
            {
                RunSpaceOpen();
            }
        }

        #endregion ---- Private Methods ----

        #region ---- Exchange Service Configuration ----

        public class ExchangeServers
        {
            public int Count
            {
                get; private set;
            }

            public List<String> MailboxRoleServers
            {
                get;
            }

            public List<String> ClientAccessRoleServers
            {
                get;
            }

            public List<String> UnifiedMessangingRoleServers
            {
                get;
            }

            public List<String> HubTransportRoleServers
            {
                get;
            }

            public List<String> EdgeTransportRoleServers
            {
                get;
            }

            public ExchangeServers()
            {
                try
                {
                    MailboxRoleServers = new List<String>();
                    ClientAccessRoleServers = new List<String>();
                    UnifiedMessangingRoleServers = new List<String>();
                    HubTransportRoleServers = new List<String>();
                    EdgeTransportRoleServers = new List<String>();

                    DirectoryEntry DSE = new DirectoryEntry("LDAP://RootDSE");
                    string dseName = Convert.ToString(DSE.Properties["configurationNamingContext"][0]);
                    DSE.Dispose();

                    DirectoryEntry SearchScope = new DirectoryEntry("LDAP://" + dseName);
                    System.DirectoryServices.DirectorySearcher adSearch = new DirectorySearcher(SearchScope);
                    System.DirectoryServices.SearchResultCollection adSearchResults;

                    // Set the Active Directory Search Scope and create a Directory Searcher Object ...
                    //adSearch.Filter = "(&(objectClass=msExchExchangeServer)(versionNumber>=1937801568))";
                    adSearch.Filter = "(&(objectClass=msExchExchangeServer)(versionNumber>=1942127251))";

                    adSearch.PropertiesToLoad.Add("networkaddress");
                    adSearch.PropertiesToLoad.Add("msExchCurrentServerRoles");
                    adSearchResults = adSearch.FindAll();
                    Count = adSearchResults.Count;
                    foreach (SearchResult result in adSearchResults)
                    {
                        foreach (object address in result.Properties["networkaddress"])
                        {
                            if (address.ToString().Contains("ncacn_ip_tcp"))
                            {
                                IPAddress[] ipAddresses = Dns.GetHostAddresses(address.ToString().Split(':')[1]);
                                if ((Convert.ToInt32(result.Properties["msExchCurrentServerRoles"][0]) & 2) != 0) { MailboxRoleServers.Add(address.ToString().Split(':')[1]); }
                                if ((Convert.ToInt32(result.Properties["msExchCurrentServerRoles"][0]) & 4) != 0) { ClientAccessRoleServers.Add(address.ToString().Split(':')[1]); }
                                if ((Convert.ToInt32(result.Properties["msExchCurrentServerRoles"][0]) & 16) != 0) { UnifiedMessangingRoleServers.Add(address.ToString().Split(':')[1]); }
                                if ((Convert.ToInt32(result.Properties["msExchCurrentServerRoles"][0]) & 32) != 0) { HubTransportRoleServers.Add(address.ToString().Split(':')[1]); }
                                if ((Convert.ToInt32(result.Properties["msExchCurrentServerRoles"][0]) & 64) != 0) { EdgeTransportRoleServers.Add(address.ToString().Split(':')[1]); }
                            }
                        }
                    }
                    adSearchResults.Dispose();
                    adSearch.Dispose();
                    SearchScope.Dispose();
                }
                catch
                {
                }
            }
        }

        #endregion ---- Exchange Service Configuration ----
    }
}