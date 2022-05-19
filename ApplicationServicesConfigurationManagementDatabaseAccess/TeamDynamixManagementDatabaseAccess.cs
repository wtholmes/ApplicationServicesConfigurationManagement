using System;
using System.Linq;

namespace ApplicationServicesConfigurationManagementDatabaseAccess
{
    /// <summary>
    /// Derived Class for managing TeamDynamix Mangement Database Access
    /// </summary>
    public class TeamDynamixManagementDatabaseAccess : ConfigurationManagementDatabaseAccess
    {
        #region ---- Explicit Constructors ----

        public TeamDynamixManagementDatabaseAccess() : base()
        {
            // Configure the task subscription id.  We will only select tasks with this id.
            this.ConfigurationTaskOwner_Id = this.database.ConfigurationTaskOwners
                .Where(t => t.TaskThreadName.Equals("TeamDynamixManagement", StringComparison.OrdinalIgnoreCase))
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