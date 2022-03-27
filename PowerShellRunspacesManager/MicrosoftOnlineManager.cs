using Microsoft.Online.Administration;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;

namespace PowerShellRunspaceManager
{
    /// <summary>
    /// This derived class implements functions from managing The Microsoft Online Directory.
    /// Version 2.0
    ///
    /// Copyright © 2010-2022 William T. Holmes All rights reserved
    ///
    /// </summary>
    public class MSOnlineManager : PowershellRunspaces
    {
        #region Private Properties

        private PSCredential psCredential;

        #endregion Private Properties

        #region Constructors

        /// <summary>
        ///     Class Constructor.
        /// </summary>
        /// <param name="UserName" type="string">
        ///     <para>
        ///         Username used to connect to the Microsoft Online Directory.
        ///     </para>
        /// </param>
        /// <param name="Password" type="string">
        ///     <para>
        ///         Password used to connect to the Microsoft Online Directory.
        ///     </para>
        /// </param>
        /// <param name="Connect" type="bool">
        ///     <para>
        ///         When true a RunSpace is opened and a connection is made
        ///         to the Microsoft Online Directory.
        ///     </para>
        /// </param>
        public MSOnlineManager(String UserName, String Password, Boolean Connect)
        {
            // Start a Microsoft Online PowerShell Session...
            Uri powershellURI = new Uri("https://ps.outlook.com/powershell");

            System.Security.SecureString O365SessionPass = new System.Security.SecureString();
            foreach (char passwordChar in Password.ToCharArray())
            {
                O365SessionPass.AppendChar(passwordChar);
            }
            O365SessionPass.MakeReadOnly();
            psCredential = new PSCredential(UserName, O365SessionPass);

            InitialSessionState initialSession = InitialSessionState.CreateDefault();

            // Create the runspace and import the MSOnline module into it.
            initialSession.ImportPSModule(new[] { "MSOnline" });
            psRunSpace = RunspaceFactory.CreateRunspace(initialSession);

            // Open the Runspace and connect to the MSOnline Service.
            if (Connect)
            {
                RunSpaceOpen();
                ConnectMSOnline();
            }
        }

        #endregion Constructors

        #region Public Methods

        /// <summary>
        ///     The Public Method Connects to the Microsoft Online Directory and Loads
        ///     the default MSOnline Module.
        /// </summary>
        public void ConnectMSOnline()
        {
            PowerShellCommand powerShellCommand = new PowerShellCommand("Connect-MsolService", "Credential", psCredential);
            InvokeCommand(powerShellCommand);
        }

        /// <summary>
        ///     This Public Method Retrieves all Licenses Associated with the
        ///     Microsoft Office 365 Tenant
        /// </summary>
        /// <returns>
        ///     A System.Data.DataTable that is populated with all the tenant
        ///     licensing data.
        /// </returns>
        public DataTable GetAvailableLicenses()
        {
            DataTable Office365Licenses = new DataTable("AvailableOffice365Licenses");
            Office365Licenses.Columns.Add("AccountName", typeof(String));
            Office365Licenses.Columns.Add("AccountSkuId", typeof(String));
            Office365Licenses.Columns.Add("SkuPartNumber", typeof(String));
            Office365Licenses.Columns.Add("ActiveUnits", typeof(Int32));
            Office365Licenses.Columns.Add("ConsumedUnits", typeof(Int32));
            Office365Licenses.Columns.Add("LockedOutUnits", typeof(Int32));
            Office365Licenses.Columns.Add("WarningUnits", typeof(Int32));
            Office365Licenses.Columns.Add("ServiceName", typeof(String));
            Office365Licenses.Columns.Add("ServiceType", typeof(String));
            //Office365Licenses.Columns.Add("ProvisioningStatus", typeof(String));

            PowerShellCommand powerShellCommand = new PowerShellCommand("Get-MsolAccountSku");
            Collection<PSObject> MsolAccountSkuResult = InvokeCommand(powerShellCommand);

            foreach (PSObject MsolAccountSku in MsolAccountSkuResult)
            {
                List<PSPropertyInfo> MsolAccountSkuInfo = MsolAccountSku.Properties.ToList();
                String AccountName = MsolAccountSkuInfo.Find(x => x.Name.Equals("AccountName")).Value.ToString();
                String AccountSkuID = MsolAccountSkuInfo.Find(x => x.Name.Equals("AccountSkuId")).Value.ToString();
                String SkuPartNumber = MsolAccountSkuInfo.Find(x => x.Name.Equals("SkuPartNumber")).Value.ToString();
                Int32 ActiveUnits = Convert.ToInt32(MsolAccountSkuInfo.Find(x => x.Name.Equals("ActiveUnits")).Value);
                Int32 ConsumedUnits = Convert.ToInt32(MsolAccountSkuInfo.Find(x => x.Name.Equals("ConsumedUnits")).Value);
                Int32 LockedOutUnits = Convert.ToInt32(MsolAccountSkuInfo.Find(x => x.Name.Equals("LockedOutUnits")).Value);
                Int32 SuspendedUnits = Convert.ToInt32(MsolAccountSkuInfo.Find(x => x.Name.Equals("SuspendedUnits")).Value);
                Int32 WarningUnits = Convert.ToInt32(MsolAccountSkuInfo.Find(x => x.Name.Equals("WarningUnits")).Value);

                System.Collections.Generic.List<ServiceStatus> ServiceStatuses = (System.Collections.Generic.List<ServiceStatus>)MsolAccountSkuInfo.Find(x => x.Name.Equals("ServiceStatus")).Value;
                foreach (ServiceStatus ServiceStatus in ServiceStatuses)
                {
                    String ServiceName = ServiceStatus.ServicePlan.ServiceName;
                    String ServiceType = ServiceStatus.ServicePlan.ServiceType;

                    DataRow Office365LicenseRow = Office365Licenses.NewRow();
                    Office365LicenseRow["AccountName"] = AccountName;
                    Office365LicenseRow["AccountSkuId"] = AccountSkuID;
                    Office365LicenseRow["SkuPartNumber"] = SkuPartNumber;
                    Office365LicenseRow["ActiveUnits"] = ActiveUnits;
                    Office365LicenseRow["ConsumedUnits"] = ConsumedUnits;
                    Office365LicenseRow["LockedOutUnits"] = LockedOutUnits;
                    Office365LicenseRow["WarningUnits"] = WarningUnits;
                    Office365LicenseRow["ServiceName"] = ServiceName;
                    Office365LicenseRow["ServiceType"] = ServiceType;

                    Office365Licenses.Rows.Add(Office365LicenseRow);
                }
            }

            return Office365Licenses;
        }

        /// <summary>
        ///     Static method to create an empty DataTable with the correct schema
        ///     for collecting assigned license data.
        /// </summary>
        /// <returns>
        ///     An empty System.Data.DataTable with the correct schema assigned.
        /// </returns>
        public static DataTable AssignedLicenses()
        {
            DataTable Office365Licenses = new DataTable("AssignedOffice365Licenses");
            Office365Licenses.Columns.Add("UserPrincipalName", typeof(String));
            Office365Licenses.Columns.Add("AccountName", typeof(String));
            Office365Licenses.Columns.Add("SkuPartNumber", typeof(String));
            Office365Licenses.Columns.Add("AccountSkuId", typeof(String));
            Office365Licenses.Columns.Add("ServiceName", typeof(String));
            Office365Licenses.Columns.Add("ServiceType", typeof(String));
            Office365Licenses.Columns.Add("ProvisioningStatus", typeof(String));
            return Office365Licenses;
        }

        /// <summary>
        ///     This Public Method Populates an AssignedLicenses DataTable with the
        ///     License information for the Requested UserPrincipalName.
        /// </summary>
        /// <param name="UserPrincpalName" type="string">
        ///     <para>
        ///         The MSOnline/Office365 userPrincpalName
        ///     </para>
        /// </param>
        /// <returns>
        ///     A System.Data.DataTable populated with the assigned license information.
        /// </returns>
        public DataTable GetAssignedLicenses(String UserPrincpalName)
        {
            DataTable Office365Licenses = AssignedLicenses();

            PowerShellCommand powerShellCommand = new PowerShellCommand("Get-MsolUser", "UserPrincipalName", UserPrincpalName);
            Collection<PSObject> UserLicenseResult = InvokeCommand(powerShellCommand);

            foreach (PSObject userObject in UserLicenseResult)
            {
                List<PSPropertyInfo> userPropertyInfo = userObject.Properties.ToList();
                PSPropertyInfo Licenses = userPropertyInfo.Find(x => x.Name.Equals("Licenses"));
                System.Collections.Generic.List<UserLicense> UserLicenses = (System.Collections.Generic.List<UserLicense>)Licenses.Value;
                foreach (UserLicense UserLicense in UserLicenses)
                {
                    System.Collections.Generic.List<ServiceStatus> ProductStatuses = UserLicense.ServiceStatus;
                    foreach (ServiceStatus ProductStatus in ProductStatuses)
                    {
                        DataRow LicenseRow = Office365Licenses.NewRow();
                        LicenseRow["UserPrincipalName"] = UserPrincpalName;
                        LicenseRow["AccountName"] = UserLicense.AccountSku.AccountName;
                        LicenseRow["SkuPartNumber"] = UserLicense.AccountSku.SkuPartNumber;
                        LicenseRow["AccountSkuId"] = UserLicense.AccountSkuId;
                        LicenseRow["ServiceName"] = ProductStatus.ServicePlan.ServiceName;
                        LicenseRow["ServiceType"] = ProductStatus.ServicePlan.ServiceType;
                        LicenseRow["ProvisioningStatus"] = ProductStatus.ProvisioningStatus;
                        Office365Licenses.Rows.Add(LicenseRow);
                    }
                }
            }

            return Office365Licenses;
        }

        #endregion Public Methods
    }
}