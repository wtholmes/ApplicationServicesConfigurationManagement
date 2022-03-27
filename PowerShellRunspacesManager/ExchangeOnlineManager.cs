using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;

namespace PowerShellRunspaceManager
{
    /// <summary>
    /// This derived class implements functions from managing Office365 Microsoft Exchange.
    /// Version 2.0
    ///
    /// Copyright © 2010-2022 William T. Holmes All rights reserved
    ///
    /// </summary>
    public class ExchangeOnlineManager : PowershellRunspaces
    {
        #region Private Class Properties

        private PSCredential psCredential;
        private String DebugPath;
        private Int32 MethodResultCode;

        private enum ResultMessages
        {
            Success = 0,
            RecipientNotPresent = 1000,
            MailboxNotPresent = 1001,
            MailUserNotPresent = 1002,
            UserNotPresent = 1003,
            MailboxPolicyAppiedFailed = 2000
        }

        #endregion Private Class Properties

        #region Public Properties

        public String MethodResultDetail
        {
            get { return String.Format(Enum.GetName(typeof(ResultMessages), MethodResultCode)); }
        }

        public String DebugDirectory
        {
            get
            {
                return DebugDirectory;
            }
            set
            {
                DebugPath = String.Format(@"{0}\{1}_ExchangeOnlineDebug.log", value, DateTime.UtcNow.ToString("yyyyMMdd-HHZ"));
            }
        }

        #endregion Public Properties

        #region Class Constructors

        /// <summary>
        /// Start a new Exchange On-line Remote PowerShell Session.
        /// </summary>
        /// <param name="UserName"></param>
        /// <param name="Password"></param>
        /// <param name="Connect"></param>
        public ExchangeOnlineManager(String UserName, String Password, Boolean Connect)
        {
            // Debug Log.
            DebugPath = null;

            // Start an exchange remote powershell session.
            Uri exchangeURI = new Uri("https://ps.outlook.com/powershell");
            String powerShellSchema = "http://schemas.microsoft.com/powershell/Microsoft.Exchange";
            System.Security.SecureString O365SessionPass = new System.Security.SecureString();
            foreach (char passwordChar in Password.ToCharArray())
            {
                O365SessionPass.AppendChar(passwordChar);
            }
            O365SessionPass.MakeReadOnly();
            psCredential = new PSCredential(UserName, O365SessionPass);
            connectionInfo = new WSManConnectionInfo(exchangeURI, powerShellSchema, psCredential);


            // ------
            // Set connection parameters
            // ------
            connectionInfo.AuthenticationMechanism = AuthenticationMechanism.Basic; // Authentication Method.
            connectionInfo.MaximumConnectionRedirectionCount = 1000;                // Allowed Redirects.
            connectionInfo.OperationTimeout = new TimeSpan(0, 4, 0).Milliseconds;   // 4 minutes operation timeout.
            connectionInfo.OpenTimeout = new TimeSpan(0, 10, 0).Milliseconds;       // 10 minute connection timeout.

            psRunSpace = RunspaceFactory.CreateRunspace(connectionInfo);

            if (Connect)
            {
                RunSpaceOpen();
            }
        }

        #endregion Class Constructors

        #region ---- Public Methods ----

        //===========================================
        // Get-App Functions
        //===========================================

        /// <summary>
        /// Collection<PSObject> GetRecipient(String Identity)
        /// </summary>
        /// <param name="Identity"></param>
        /// <returns>Collection<PSObject> with recipient data</returns>
        public Collection<PSObject> GetApp(String Identity)
        {
            PowerShellCommand powerShellCommand = new PowerShellCommand("Get-App", "Mailbox", Identity);
            Collection<PSObject> RecipientResult = InvokeCommand(powerShellCommand);
            return RecipientResult;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="Identity"></param>
        /// <returns></returns>
        public DataSet GetAppAsDataSet(String Identity)
        {
            return PSResultsToDataSet(GetApp(Identity));
        }

        //===========================================
        // Get-Recipients Functions
        //===========================================

        /// <summary>
        /// Collection<PSObject> GetRecipient(String Identity)
        /// </summary>
        /// <param name="Identity"></param>
        /// <returns>Collection<PSObject> with recipient data</returns>
        public Collection<PSObject> GetRecipient(String Identity)
        {
            PowerShellCommand powerShellCommand = new PowerShellCommand("Get-Recipient", "Identity", Identity);
            Collection<PSObject> RecipientResult = InvokeCommand(powerShellCommand);
            return RecipientResult;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="Identity"></param>
        /// <returns></returns>
        public DataSet GetRecipientAsDataSet(String Identity)
        {
            return PSResultsToDataSet(GetRecipient(Identity));
        }

        //===========================================
        // Get-Mailbox Functions
        //===========================================

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

        public Collection<PSObject> GetDistributionGroup(String Identity)
        {
            PowerShellCommand powerShellCommand = new PowerShellCommand("Get-DistributionGroup", "Identity", Identity);
            Collection<PSObject> DistributionGroupResult = InvokeCommand(powerShellCommand);
            return DistributionGroupResult;
        }

        public Collection<PSObject> GetDistributionGroupMember(String Identity, Boolean GetNested)
        {
            PowerShellCommand powerShellCommand = new PowerShellCommand("Get-DistributionGroupMember", "Identity", Identity);
            Collection<PSObject> DistributionGroupMemberResult = InvokeCommand(powerShellCommand);

            if (!GetNested)
            {
                return DistributionGroupMemberResult;
            }
            else
            {
                GetDistributionGroupRecursive gr = new GetDistributionGroupRecursive(psRunSpace);
                return gr.GetGroupMembers(Identity);
            }
        }

        public Collection<PSObject> GetMailContact(String Identity)
        {
            PowerShellCommand powerShellCommand = new PowerShellCommand("Get-MailContact", "Identity", Identity);
            Collection<PSObject> GetMailContactResult = InvokeCommand(powerShellCommand);
            return GetMailContactResult;
        }

        public DataSet GetMailboxAsDataSet(String Identity)
        {
            return PSResultsToDataSet(GetMailbox(Identity));
        }

        public String GetMailboxAsJSON(String Identity)
        {
            return PSResultsToJSON(GetMailbox(Identity));
        }

        public String GetMailboxAsJSONDirect(String Identity)
        {
            return PSResultsToJSONDirect(GetMailbox(Identity));
        }

        public DataSet GetDistributionGroupAsDataSet(String Identity)
        {
            return PSResultsToDataSet(GetDistributionGroup(Identity));
        }

        public DataSet GetDistributionGroupMemberAsDataSet(String Identity, Boolean GetNested)
        {
            return PSResultsToDataSet(GetDistributionGroupMember(Identity, GetNested));
        }

        public DataSet GetMailContactAsDataSet(String Identity)
        {
            return PSResultsToDataSet(GetMailContact(Identity));
        }

        /// <summary>
        /// DataTable GetMailboxAsDataTable(String Identity)
        ///
        /// Get-Mailbox as a DataTable.
        /// </summary>
        /// <param name="Identity">Identity of the mailbox to get</param>
        /// <returns>DataTable of mailbox properties.</returns>
        public DataTable GetMailboxAsDataTable(String Identity)
        {
            DataTable dt = new DataTable("Office365Mailboxes");
            PSResultsToDataTable(GetMailbox(Identity), dt, false);
            return dt;
        }

        /// <summary>
        /// DataTable GetMailboxAsDataTable(String Identity, List<String> Properties)
        ///
        /// Get-Mailbox as a DataTable with selected properties.
        /// </summary>
        /// <param name="Identity">Identity of the mailbox to get</param>
        /// <param name="Properties">List of properties to return</param>
        /// <param name="KeepOrRemoveProperties">If true keep the List of Properties if false remove the List of Properties</param>
        /// <returns>DataTable of specific mailbox properties</returns>
        public DataTable GetMailboxAsDataTable(String Identity, List<String> Properties, Boolean KeepOrRemoveProperties)
        {
            DataTable dt = new DataTable("Office365Mailboxes");
            PSResultsToDataTable(GetMailbox(Identity), dt, false);

            string[] ColumnNames = dt.Columns.Cast<DataColumn>()
                                     .Select(x => x.ColumnName)
                                     .ToArray();

            foreach (String ColumnName in ColumnNames)
            {
                if (KeepOrRemoveProperties)
                {
                    if (!Properties.Contains(ColumnName, StringComparer.OrdinalIgnoreCase))
                    {
                        dt.Columns.Remove(ColumnName);
                    }
                }
                else
                {
                    if (Properties.Contains(ColumnName, StringComparer.OrdinalIgnoreCase))
                    {
                        dt.Columns.Remove(ColumnName);
                    }
                }
            }

            return dt;
        }

        /// <summary>
        /// String GetMailboxAsXML(String Identity)
        ///
        /// Get-Mailbox as an XML Document String.
        /// </summary>
        /// <param name="Identity">Identity of mailbox to get</param>
        /// <returns>XML formatted String of mailbox properties</returns>
        public String GetMailboxAsXML(String Identity)
        {
            return PSResultsToXMLString(GetMailbox(Identity));
        }

        /// <summary>
        /// Get-Mailbox as a JSON Document String.
        /// </summary>
        /// <param name="Identity">Identity of mailbox to get</param>
        /// <returns>JSON formatted String of mailbox properties</returns>
        public String zzGetMailboxAsJSON(String Identity)
        {
            return PSResultsToJSONString(GetMailbox(Identity));
        }

        //===========================================
        // Get Hosted Quarantine Functions.
        //===========================================

        // DataSet for  Quarantined Message Details.

        private DataSet QuarantinedMessageDetail;

        /// <summary>
        /// Get the list of quarantined email messages from the Office 365 hosted Quarantine
        /// </summary>
        /// <param name="O365Recipient">Office 365 Recipient to get quarantined messages for</param>
        /// <param name="ExternallyRouted"></param>
        /// <returns></returns>
        public DataTable GetQuarantinedMessages(String O365Recipient, Boolean ExternallyRouted)
        {
            DataTable QuarantinedMessages = new DataTable("QuarantinedMessages");
            QuarantinedMessages.Columns.Add("ReleaseToRecipient", typeof(String));

            DataTable RecipientResults = new DataTable("Recipients");

            PowerShellCommand powerShellCommand = new PowerShellCommand("Get-Recipient", "Identity", O365Recipient);
            Collection<PSObject> RecipientResult = InvokeCommand(powerShellCommand);

            // Configure and populate the Lookup DataTable....
            DataTable RecipientLookup = new DataTable("RecipientLookup");
            RecipientLookup.Columns.Add("Name", typeof(String));
            RecipientLookup.Columns.Add("LookupCommand", typeof(String));

            foreach (PSObject Recipient in RecipientResult)
            {
                DataRow RecipientLookupRow = RecipientLookup.NewRow();
                String RecipientType = Recipient.Properties["RecipientType"].Value.ToString();
                switch (RecipientType)
                {
                    case "UserMailbox":

                        RecipientLookupRow["Name"] = Recipient.Properties["Name"].Value;
                        RecipientLookupRow["LookupCommand"] = "Get-Mailbox";
                        RecipientLookup.Rows.Add(RecipientLookupRow);
                        break;

                    case "MailUser":

                        RecipientLookupRow["Name"] = Recipient.Properties["Name"].Value;
                        RecipientLookupRow["LookupCommand"] = "Get-MailUser";
                        RecipientLookup.Rows.Add(RecipientLookupRow);
                        break;
                }
            }

            // Convert
            foreach (DataRow LookupRow in RecipientLookup.Rows)
            {
                powerShellCommand = new PowerShellCommand(LookupRow["LookupCommand"].ToString(), "Identity", LookupRow["Name"].ToString());
                Collection<PSObject> SpecificRecipientResult = InvokeCommand(powerShellCommand);
                PSResultsToDataTable(SpecificRecipientResult, RecipientResults, true);
            }

            // Remove unnecessary columns from the Recipient Results Table..
            List<String> Properties = new List<String>() { "userPrincipalName", "DisplayName", "ForwardingAddress", "ForwardingSmtpAddress", "ExternalEmailAddress", "DeliverToMailboxAndForward", "CustomAttribute2", "RecipientType" };
            string[] ColumnNames = RecipientResults.Columns.Cast<DataColumn>()
                                     .Select(x => x.ColumnName)
                                     .ToArray();

            foreach (String ColumnName in ColumnNames)
            {
                if (!Properties.Contains(ColumnName, StringComparer.OrdinalIgnoreCase))
                {
                    RecipientResults.Columns.Remove(ColumnName);
                }
            }

            foreach (DataRow Recipient in RecipientResults.Rows)
            {
                String AssociatedExternalAddress = null;
                switch (Recipient["RecipientType"].ToString())
                {
                    case "UserMailbox":
                        if (Recipient["ForwardingSmtpAddress"].GetType() != typeof(DBNull))
                        {
                            AssociatedExternalAddress = Recipient["ForwardingSmtpAddress"].ToString().Split(':')[1];
                        }
                        break;

                    case "MailUser":
                        if (Recipient["ExternalEmailAddress"] != null)
                        {
                            AssociatedExternalAddress = Recipient["ExternalEmailAddress"].ToString().Split(':')[1];
                        }
                        break;
                }

                if (AssociatedExternalAddress != null)
                {
                    Collection<PSObject> QuarantinedMessageResults;
                    Int32 Page = 1;

                    do
                    {
                        powerShellCommand = new PowerShellCommand("Get-QuarantineMessage");
                        powerShellCommand.AddCommandParameter("Type", "TransportRule");
                        powerShellCommand.AddCommandParameter("RecipientAddress", AssociatedExternalAddress);
                        powerShellCommand.AddCommandParameter("Page", Page);
                        QuarantinedMessageResults = InvokeCommand(powerShellCommand);

                        foreach (PSObject QuarantinedMessageResult in QuarantinedMessageResults)
                        {
                            DataRow QuarantinedMessage = QuarantinedMessages.NewRow();
                            foreach (PSProperty QuarantineMessageProperty in QuarantinedMessageResult.Properties)
                            {
                                DataColumnCollection columns = QuarantinedMessages.Columns;
                                if (!(columns.Contains(QuarantineMessageProperty.Name)))
                                {
                                    QuarantinedMessages.Columns.Add(QuarantineMessageProperty.Name, typeof(Object));
                                }
                                QuarantinedMessage[QuarantineMessageProperty.Name] = QuarantinedMessageResult.Properties[QuarantineMessageProperty.Name].Value;
                            }
                            QuarantinedMessage["ReleaseToRecipient"] = AssociatedExternalAddress;
                            QuarantinedMessages.Rows.Add(QuarantinedMessage);
                        }
                        Page++;
                    }
                    while (QuarantinedMessageResults.Count > 0);
                }
            }

            return QuarantinedMessages;
        }

        /// <summary>
        /// DataSet GetQuarantinedMessageDetail(String Identity)
        ///
        /// Get detailed information for a message in the Office 365 hosted Quarantine.
        /// </summary>
        /// <param name="Identity">Quarantined Message Unique Identity</param>
        /// <returns></returns>

        public DataSet GetQuarantinedMessageDetail(String Identity)
        {
            QuarantinedMessageDetail = new DataSet("QuarantinedMessageDetail");
            Collection<PSObject> CommandResults;

            // Get the Quarantined Message's Detailed Data.
            PowerShellCommand powerShellCommand = new PowerShellCommand("Get-QuarantineMessage", "Identity", Identity);
            CommandResults = InvokeCommand(powerShellCommand);
            QuarantinedMessageDetail.Tables.Add(PSResultsToDataTable(CommandResults, "MessageMetaData"));

            if (QuarantinedMessageDetail.Tables["MessageMetaData"].Rows.Count == 1)
            {
                // Get the Message Header

                powerShellCommand = new PowerShellCommand("Get-QuarantineMessage", "Identity", QuarantinedMessageDetail.Tables["MessageMetaData"].Rows[0]["Identity"].ToString());
                CommandResults = InvokeCommand(powerShellCommand);
                QuarantinedMessageDetail.Tables.Add(PSResultsToDataTable(CommandResults, "MessageHeader"));

                // Get the Message Body
                powerShellCommand = new PowerShellCommand("Preview-QuarantineMessage", "Identity", QuarantinedMessageDetail.Tables["MessageMetaData"].Rows[0]["Identity"].ToString());
                CommandResults = InvokeCommand(powerShellCommand);
                QuarantinedMessageDetail.Tables.Add(PSResultsToDataTable(CommandResults, "MessageBody"));
            }
            else
            {
                QuarantinedMessageDetail.Tables.Add(new DataTable("MessageHeader"));
                QuarantinedMessageDetail.Tables.Add(new DataTable("MessageBody"));
            }
            return QuarantinedMessageDetail;
        }

        /// <summary>
        /// Returns the current quarantined message body.
        /// </summary>
        /// <returns>String HTML message body</returns>
        public String QuarantinedMessageBody()
        {
            String Body = "Empty Body";
            if (QuarantinedMessageDetail.Tables["MessageBody"].Rows.Count == 1)
            {
                Body = QuarantinedMessageDetail.Tables["MessageBody"].Rows[0]["Body"].ToString();
            }
            return Body;
        }

        /// <summary>
        /// Returns the current quarantined message body with all links deactivated.
        /// </summary>
        /// <returns>String HTML message body with links deactivated</returns>
        public String QuarantinedMessageBodyLinksSupressed()
        {
            HtmlDocument doc = new HtmlDocument();
            if (QuarantinedMessageDetail.Tables["MessageBody"].Rows.Count == 1)
            {
                doc.LoadHtml(QuarantinedMessageDetail.Tables["MessageBody"].Rows[0]["Body"].ToString());

                // Nodes to replace with plain text...
                HtmlNodeCollection ReplaceHyperLinkNodes = doc.DocumentNode.SelectNodes("//a");
                //HtmlNodeCollection ReplaceHyperLinkNodes = doc.DocumentNode.SelectNodes("//tbody/descendant::a[starts-with(@href,'https://') or starts-with(@href,'http://')]");
                if (ReplaceHyperLinkNodes != null)
                {
                    for (int Index = 0; Index < ReplaceHyperLinkNodes.Count; Index++)
                    {
                        HtmlNode hyperlinkNode = ReplaceHyperLinkNodes[Index];
                        HtmlNode parent = hyperlinkNode.ParentNode;
                        HtmlNode newNode = HtmlNode.CreateNode(String.Format(@"<b style=""color: blue; text-decoration:underline;"">{0}</b>", hyperlinkNode.InnerText));
                        parent.ReplaceChild(newNode, hyperlinkNode);
                    }
                }

                // Nodes to remove...

                HtmlNodeCollection RemoveHyperLinkNodes = doc.DocumentNode.SelectNodes("//link");
                if (RemoveHyperLinkNodes != null)
                {
                    for (int Index = 0; Index < RemoveHyperLinkNodes.Count; Index++)
                    {
                        HtmlNode hyperlinkNode = RemoveHyperLinkNodes[Index];
                        HtmlNode parent = hyperlinkNode.ParentNode;
                        parent.RemoveChild(hyperlinkNode);
                    }
                }

                StringWriter sw = new StringWriter();
                doc.Save(sw);
                return sw.ToString();
            }
            else
            {
                return "Empty Body";
            }
        }

        /// <summary>
        /// DataTable ReleaseQuarantinedMessage(String Identity, String Recipient)
        ///
        /// Release the specified message to the specified recipient.
        /// </summary>
        /// <param name="Identity">Hosted Quarantine Message Identity</param>
        /// <param name="Recipient">The email address of the recipient to release the message to.</param>
        /// <returns>DataTable with results.</returns>
        public DataTable ReleaseQuarantinedMessage(String Identity, String Recipient)
        {
            Collection<PSObject> CommandResults;
            PowerShellCommand powerShellCommand = new PowerShellCommand("Release-QuarantineMessage");
            powerShellCommand.AddCommandParameter("Identity", Identity);
            powerShellCommand.AddCommandParameter("User", Recipient);
            CommandResults = InvokeCommand(powerShellCommand);
            DataTable dt = PSResultsToDataTable(CommandResults, "MessageMetaData");
            return dt;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="Identity"></param>
        /// <returns></returns>
        public DataSet GetMailboxAutoReplyConfigurationAsDataSet(String Identity)
        {
            PowerShellCommand powerShellCommand = new PowerShellCommand("Get-MailboxAutoReplyConfiguration", "Identity", Identity);
            Collection<PSObject> MailboxResult = InvokeCommand(powerShellCommand);
            return PSResultsToDataSet(MailboxResult);
        }

        /// <summary>
        /// Applies the "O365 Direct Policy" policy to the recipient specified by the UserPrinciapalName parameter.
        ///
        /// The "O365 Direct Policy" policy adds the ability to set email forwarding in Office 365 OWA.
        ///
        /// </summary>
        /// <param name="UserPrincipalName"></param>
        /// <returns>
        ///     true: if changes were sucessfully applied.
        ///     false: if the changes were not applied.
        /// </returns>
        public Boolean EnableO365ForwardingEdit(String UserPrincipalName)
        {
            PowerShellCommand powerShellCommand = new PowerShellCommand("Get-Recipient", "Identity", UserPrincipalName);
            this.ClearExceptions();
            this.ClearLoggedMessages();
            Collection<PSObject> RecipientResult = InvokeCommand(powerShellCommand);
            LogProcessMessages(true, true);
            if (RecipientResult.Count == 1)
            {
                if (RecipientResult[0].Properties["RecipientType"].Value.ToString().Equals("UserMailbox"))
                {
                    powerShellCommand = new PowerShellCommand("Set-Mailbox");
                    powerShellCommand.AddCommandParameter("Identity", UserPrincipalName);
                    powerShellCommand.AddCommandParameter("RoleAssignmentPolicy", "O365 Direct Policy");

                    try
                    {
                        this.ClearExceptions();
                        this.ClearLoggedMessages();
                        Collection<PSObject> MailboxResult = InvokeCommand(powerShellCommand);
                        LogProcessMessages(true, true);
                    }
                    catch (Exception exp)
                    {
                        _Exceptions.Add(exp);
                        return false;
                    }
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Applies the CornellOWAChanges policy to the recipient specified by the UserPrinciapalName parameter.
        ///
        /// The CornellOWAChanges policy removes the ability to set email forwarding in Office 365 OWA.
        ///
        /// </summary>
        /// <param name="UserPrincipalName"></param>
        /// <returns>
        ///     true: if changes were sucessfully applied.
        ///     false: if the changes were not applied.
        /// </returns>
        public Boolean DisableO365ForwardingEdit(String UserPrincipalName)
        {
            PowerShellCommand powerShellCommand = new PowerShellCommand("Get-Recipient", "Identity", UserPrincipalName);
            this.ClearExceptions();
            this.ClearLoggedMessages();
            Collection<PSObject> RecipientResult = InvokeCommand(powerShellCommand);
            LogProcessMessages(true, true);

            if (RecipientResult.Count == 1)
            {
                if (RecipientResult[0].Properties["RecipientType"].Value.ToString().Equals("UserMailbox"))
                {
                    powerShellCommand = new PowerShellCommand("Set-Mailbox");
                    powerShellCommand.AddCommandParameter("Identity", UserPrincipalName);
                    powerShellCommand.AddCommandParameter("RoleAssignmentPolicy", "CornellOWAChanges");

                    try
                    {
                        this.ClearExceptions();
                        this.ClearLoggedMessages();
                        Collection<PSObject> MailboxResult = InvokeCommand(powerShellCommand);
                        LogProcessMessages(true, true);
                    }
                    catch (Exception exp)
                    {
                        _Exceptions.Add(exp);
                        return false;
                    }

                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Set forwarding for an Office365 Recipient.
        /// </summary>
        /// <param name="UserPrincipalName">Recipient to set forwarding for.</param>
        /// <param name="ForwardingAddress">The SMTP address to set as the forwarding address.</param>
        /// <param name="DeliverToMailboxAndForward">Deliver to Mailbox as well as forwarding address.</param>
        public Boolean SetOffice365Forwarding(String UserPrincipalName, String ForwardingAddress, Boolean DeliverToMailboxAndForward)
        {
            PowerShellCommand powerShellCommand = new PowerShellCommand("Get-Recipient", "Identity", UserPrincipalName);
            this.ClearExceptions();
            this.ClearLoggedMessages();
            Collection<PSObject> RecipientResult = InvokeCommand(powerShellCommand);
            LogProcessMessages(true, true);

            if (RecipientResult.Count == 1)
            {
                if (RecipientResult[0].Properties["RecipientType"].Value.ToString().Equals("UserMailbox"))
                {
                    powerShellCommand = new PowerShellCommand("Set-Mailbox");
                    powerShellCommand.AddCommandParameter("Identity", UserPrincipalName);
                    powerShellCommand.AddCommandParameter("ForwardingAddress", null);
                    powerShellCommand.AddCommandParameter("ForwardingSmtpAddress", ForwardingAddress);
                    powerShellCommand.AddCommandParameter("DeliverToMailboxAndForward", DeliverToMailboxAndForward);

                    try
                    {
                        this.ClearExceptions();
                        this.ClearLoggedMessages();
                        Collection<PSObject> MailboxResult = InvokeCommand(powerShellCommand);
                        LogProcessMessages(true, true);
                    }
                    catch (Exception exp)
                    {
                        _Exceptions.Add(exp);
                        return false;
                    }
                }
                else if (RecipientResult[0].Properties["RecipientType"].Value.ToString().Equals("MailUser"))
                {
                    powerShellCommand = new PowerShellCommand("Set-MailUser");
                    powerShellCommand.AddCommandParameter("Identity", UserPrincipalName);
                    powerShellCommand.AddCommandParameter("ExternalEmailAddress", ForwardingAddress);

                    try
                    {
                        this.ClearExceptions();
                        this.ClearLoggedMessages();
                        Collection<PSObject> MailuserResult = InvokeCommand(powerShellCommand);
                        LogProcessMessages(true, true);

                        if (this.psCommandErrors.Count > 0)
                        {
                        }
                    }
                    catch (Exception exp)
                    {
                        _Exceptions.Add(exp);
                        return false;
                    }
                }
                MethodResultCode = 0;
                return true;
            }
            else
            {
                MethodResultCode = 1000;
                return false;
            }
        }

        /// <summary>
        /// Set forwarding for an Office365 Recipient.
        /// </summary>
        /// <param name="UserPrincipalName">Recipient to set forwarding for.</param>
        /// <param name="ForwardingAddress">The SMTP address to set as the forwarding address.</param>
        /// <param name="DeliverToMailboxAndForward">Deliver to Mailbox as well as forwarding address.</param>
        public Boolean SetOffice365Forwarding(String UserPrincipalName, String ForwardingAddress, Boolean DeliverToMailboxAndForward, Boolean EditForwardingEnabled)
        {
            PowerShellCommand powerShellCommand = new PowerShellCommand("Get-Recipient", "Identity", UserPrincipalName);
            this.ClearExceptions();
            this.ClearLoggedMessages();
            Collection<PSObject> RecipientResult = InvokeCommand(powerShellCommand);
            LogProcessMessages(true, true);

            if (RecipientResult.Count == 1)
            {
                if (RecipientResult[0].Properties["RecipientType"].Value.ToString().Equals("UserMailbox"))
                {
                    powerShellCommand = new PowerShellCommand("Set-Mailbox");
                    powerShellCommand.AddCommandParameter("Identity", UserPrincipalName);
                    powerShellCommand.AddCommandParameter("ForwardingAddress", null);
                    powerShellCommand.AddCommandParameter("ForwardingSmtpAddress", ForwardingAddress);
                    powerShellCommand.AddCommandParameter("DeliverToMailboxAndForward", DeliverToMailboxAndForward);

                    // Set the Role Assignment Policy for the user to enable or disable forwarding settings in Office 365.
                    if (EditForwardingEnabled)
                    {
                        powerShellCommand.AddCommandParameter("RoleAssignmentPolicy", "O365 Direct Policy");
                    }
                    else
                    {
                        powerShellCommand.AddCommandParameter("RoleAssignmentPolicy", "CornellOWAChanges");
                    }

                    try
                    {
                        this.ClearExceptions();
                        this.ClearLoggedMessages();
                        Collection<PSObject> MailboxResult = InvokeCommand(powerShellCommand);
                        LogProcessMessages(true, true);
                        return true;
                    }
                    catch (Exception exp)
                    {
                        _Exceptions.Add(exp);
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Set a mailbox auto reply message for the specified mailbox.
        /// </summary>
        /// <param name="Identity">Mailbox to set auto reply message for.</param>
        /// <param name="Message">AutoReply Message Text.</param>
        public void EnableMailboxAutoReply(String Identity, String Message, Boolean Enable)
        {
            PowerShellCommand powerShellCommand = new PowerShellCommand("Set-MailboxAutoReplyConfiguration");
            powerShellCommand.AddCommandParameter("Identity", Identity);
            powerShellCommand.AddCommandParameter("InternalMessage", Message);
            powerShellCommand.AddCommandParameter("ExternalMessage", Message);
            powerShellCommand.AddCommandParameter("AutoReplyState", "Enabled");

            try
            {
                Collection<PSObject> AutoReplyCommandResult = InvokeCommand(powerShellCommand);
            }
            catch (Exception exp)
            {
                _Exceptions.Add(exp);
            }
        }

        /// <summary>
        /// Disable Auto reply Message for the specified mailbox.
        /// </summary>
        /// <param name="Identity"></param>
        public void DisableMailboxAutoReply(String Identity)
        {
            PowerShellCommand powerShellCommand = new PowerShellCommand("Set-MailboxAutoReplyConfiguration");
            powerShellCommand.AddCommandParameter("Identity", Identity);
            powerShellCommand.AddCommandParameter("AutoReplyState", "Disabled");

            try
            {
                Collection<PSObject> MailboxResult = InvokeCommand(powerShellCommand);
            }
            catch (Exception exp)
            {
                _Exceptions.Add(exp);
            }
        }

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
    }

    #endregion ---- Public Methods ----

    #region Auxiliary Classes

    public class GetDistributionGroupRecursive
    {
        #region Private Class Properties

        private List<String> EnumeratedGroups;
        private PowershellRunspaces RunSpace;
        private Int32 Depth;

        #endregion Private Class Properties

        #region Constructors

        public GetDistributionGroupRecursive(Runspace runspace)
        {
            Depth = 0;
            EnumeratedGroups = new List<String>();
            RunSpace = new PowershellRunspaces();
            RunSpace.psRunSpace = runspace;
        }

        #endregion Constructors

        #region Public Methods

        public Collection<PSObject> GetGroupMembers(String GroupIdentity)
        {
            Depth++;
            PowerShellCommand powerShellCommand = new PowerShellCommand("Get-DistributionGroupMembers", "Identity", GroupIdentity);

            Collection<PSObject> DistributionGroupMemberResult = RunSpace.InvokeCommand(powerShellCommand);

            // PSObject Collection for the results.
            Collection<PSObject> NestedGroupMemberResult = new Collection<PSObject>();

            foreach (PSObject Member in DistributionGroupMemberResult)
            {
                if (Member.Properties["RecipientType"].Value.ToString().EndsWith("Group"))
                {
                    String GroupName = Member.Properties["Identity"].Value.ToString();
                    if (!EnumeratedGroups.Contains(GroupName))
                    {
                        Collection<PSObject> NestedGroup = GetGroupMembers(GroupName);
                        EnumeratedGroups.Add(GroupName);

                        foreach (PSObject NestedGroupMember in NestedGroup)
                        {
                            NestedGroupMemberResult.Add(NestedGroupMember);
                        }
                    }
                }
                else
                {
                    NestedGroupMemberResult.Add(Member);
                }
            }
            Depth--;
            if (Depth == 0)
            {
                return new Collection<PSObject>(NestedGroupMemberResult.GroupBy(x => x.Properties["Name"].Value).Select(x => x.First()).ToList());
            }
            else
            {
                return NestedGroupMemberResult;
            }
        }

        #endregion Public Methods
    }

    #endregion Auxiliary Classes
}