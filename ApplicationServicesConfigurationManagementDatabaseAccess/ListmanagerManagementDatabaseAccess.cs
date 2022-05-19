using System;
using System.Linq;

namespace ApplicationServicesConfigurationManagementDatabaseAccess
{
    /// <summary>
    /// Derived Class for managing Listmanager Mangement Database Access
    /// </summary>

    public class ListmanagerManagementDatabaseAccess : ConfigurationManagementDatabaseAccess
    {
        #region ---- Explicit Constructors ----

        public ListmanagerManagementDatabaseAccess() : base()
        {
            // Configure the task subscription id.  We will only select tasks with this id.
            this.ConfigurationTaskOwner_Id = this.database.ConfigurationTaskOwners
                .Where(t => t.TaskThreadName.Equals("ListOwnerTransferManagement", StringComparison.OrdinalIgnoreCase))
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