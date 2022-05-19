using System.Linq;

namespace ApplicationServicesConfigurationManagementDatabaseAccess
{
    /// <summary>
    /// Derived Class for managing Office 365 Licensing Mangement Database Access
    /// </summary>
    public class Office365LicensingManagementDatabaseAccess : ConfigurationManagementDatabaseAccess
    {
        #region ---- Explicit Constructors ----

        public Office365LicensingManagementDatabaseAccess() : base()
        {
            // Configure the task subscription id.  We will only select tasks with this id.
            this.ConfigurationTaskOwner_Id = this.database.ConfigurationTaskOwners
                    .Where(t => t.TaskThreadName.Equals("Office365LicensingManagement", System.StringComparison.OrdinalIgnoreCase))
                    .Select(t => t.ConfigurationTaskOwner_Id)
                    .FirstOrDefault();
        }

        #endregion ---- Explicit Constructors ----

        #region ---- Public Methods ----

        public new void Dispose()
        {
            base.Dispose();
        }

        #endregion ---- Public Methods ----
    }
}