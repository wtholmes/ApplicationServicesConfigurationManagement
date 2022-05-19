using System;
using System.Data;

namespace PowerShellRunspaceManager
{
    public class AsyncExchageOnlineManager
    {
        #region Constructors

        public void AsyncExchangeOnlineManager()
        {
        }

        #endregion Constructors

        #region Public Methods

        public DataSet GetAppAsDataSet(String Identity)
        {
            AsyncPSCommand asyncPSCommand = new AsyncPSCommand();
            PowerShellCommand powerShellCommand = new PowerShellCommand("Get-App", "Mailbox", Identity);
            asyncPSCommand.TargetService = "ExchangeOnline";
            asyncPSCommand.PowerShellCommand = powerShellCommand;
            asyncPSCommand.QueueCommand();
            return asyncPSCommand.CommandResults;
        }

        public DataSet GetMailboxAsDataSet(String Identity)
        {
            AsyncPSCommand asyncPSCommand = new AsyncPSCommand();
            PowerShellCommand powerShellCommand = new PowerShellCommand("Get-Mailbox", "Identity", Identity);
            asyncPSCommand.TargetService = "ExchangeOnline";
            asyncPSCommand.PowerShellCommand = powerShellCommand;
            asyncPSCommand.QueueCommand();
            return asyncPSCommand.CommandResults;
        }

        public String GetMailboxAsJSON(String Identity)
        {
            AsyncPSCommand asyncPSCommand = new AsyncPSCommand();
            PowerShellCommand powerShellCommand = new PowerShellCommand("Get-Mailbox", "Identity", Identity);
            asyncPSCommand.TargetService = "ExchangeOnline";
            asyncPSCommand.PowerShellCommand = powerShellCommand;
            asyncPSCommand.QueueCommand();
            return asyncPSCommand.CommandResultsAsJSON;
        }

        public void SetOffice365Forwarding(String UserPrincipalName, String ForwardingAddress, Boolean DeliverToMailboxAndForward)
        {
            AsyncPSCommand asyncPSCommand = new AsyncPSCommand();
            PowerShellCommand powerShellCommand = new PowerShellCommand("Get-Mailbox", "Identity", UserPrincipalName);
            asyncPSCommand.TargetService = "ExchangeOnline";
            asyncPSCommand.PowerShellCommand = powerShellCommand;
            asyncPSCommand.QueueCommand();

            DataSet Recipient = asyncPSCommand.CommandResults.Copy();

            if (Recipient.Tables["Objects"].Rows.Count == 1)
            {
                if (Recipient.Tables["Objects"].Rows[0]["RecipientType"].ToString().Equals("UserMailbox"))
                {
                    powerShellCommand = new PowerShellCommand("Set-Mailbox");
                    powerShellCommand.AddCommandParameter("Identity", UserPrincipalName);
                    powerShellCommand.AddCommandParameter("ForwardingAddress", null);
                    powerShellCommand.AddCommandParameter("ForwardingSmtpAddress", ForwardingAddress);
                    powerShellCommand.AddCommandParameter("DeliverToMailboxAndForward", DeliverToMailboxAndForward);
                    asyncPSCommand.TargetService = "ExchangeOnline";
                    asyncPSCommand.PowerShellCommand = powerShellCommand;
                    asyncPSCommand.QueueCommand();
                }
                else if (Recipient.Tables["Objects"].Rows[0]["RecipientType"].ToString().Equals("MailUser"))
                {
                    powerShellCommand = new PowerShellCommand("Set-MailUser");
                    powerShellCommand.AddCommandParameter("Identity", UserPrincipalName);
                    powerShellCommand.AddCommandParameter("ExternalEmailAddress", ForwardingAddress);
                    asyncPSCommand.TargetService = "ExchangeOnline";
                    asyncPSCommand.PowerShellCommand = powerShellCommand;
                    asyncPSCommand.QueueCommand();
                }
            }
        }

        public void EnableMailboxAutoReply(String Identity, String Message, Boolean Enable)
        {
            AsyncPSCommand asyncPSCommand = new AsyncPSCommand();
            PowerShellCommand powerShellCommand = new PowerShellCommand("Set-MailboxAutoReplyConfiguration");
            powerShellCommand.AddCommandParameter("Identity", Identity);
            powerShellCommand.AddCommandParameter("InternalMessage", Message);
            powerShellCommand.AddCommandParameter("ExternalMessage", Message);
            powerShellCommand.AddCommandParameter("AutoReplyState", "Enabled");
            asyncPSCommand.TargetService = "ExchangeOnline";
            asyncPSCommand.PowerShellCommand = powerShellCommand;
            asyncPSCommand.QueueCommand();
        }

        public void DisableMailboxAutoReply(String Identity)
        {
            AsyncPSCommand asyncPSCommand = new AsyncPSCommand();
            PowerShellCommand powerShellCommand = new PowerShellCommand("Set-MailboxAutoReplyConfiguration");
            powerShellCommand.AddCommandParameter("Identity", Identity);
            powerShellCommand.AddCommandParameter("AutoReplyState", "Disabled");
            asyncPSCommand.TargetService = "ExchangeOnline";
            asyncPSCommand.PowerShellCommand = powerShellCommand;
            asyncPSCommand.QueueCommand();
        }

        public DataSet GetMailboxAutoReplyConfigurationAsDataSet(String Identity)
        {
            AsyncPSCommand asyncPSCommand = new AsyncPSCommand();
            PowerShellCommand powerShellCommand = new PowerShellCommand("Get-MailboxAutoReplyConfiguration", "Identity", Identity);
            asyncPSCommand.TargetService = "ExchangeOnline";
            asyncPSCommand.PowerShellCommand = powerShellCommand;
            asyncPSCommand.QueueCommand();

            return asyncPSCommand.CommandResults;
        }

        #endregion Public Methods
    }
}