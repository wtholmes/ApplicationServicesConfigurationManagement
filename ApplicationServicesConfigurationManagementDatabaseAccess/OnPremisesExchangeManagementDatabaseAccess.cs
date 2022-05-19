using System;
using System.Linq;

namespace ApplicationServicesConfigurationManagementDatabaseAccess
{
    /// <summary>
    /// Derived Class for managing On Premises Exchange Mangement Database Access
    /// </summary>
    public class OnPremisesExchangeManagementDatabaseAccess : ConfigurationManagementDatabaseAccess
    {
        #region ---- Explicit Constructors ----

        public OnPremisesExchangeManagementDatabaseAccess() : base()
        {
            // Configure the task subscription id.  We will only select tasks with this id.
            this.ConfigurationTaskOwner_Id = this.database.ConfigurationTaskOwners
                    .Where(t => t.TaskThreadName.Equals("OnPremisesExchangeManagement", StringComparison.OrdinalIgnoreCase))
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