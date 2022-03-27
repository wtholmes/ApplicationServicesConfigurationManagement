﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApplicationServicesConfigurationManagementDatabaseAccess
{
    /// <summary>
    /// Derived Class for managing On Premises Exchange Mangement Database Access
    /// </summary>
    public class OnPremisesExchangeManagementDatabaseAccess : ConfigurationManagementDatabaseAccess
    {
        #region ---- Public Properties ----

        #endregion

        #region ---- Private Properties ----

        #endregion

        #region ---- Explicit Constructors ----
        public OnPremisesExchangeManagementDatabaseAccess() : base()
        {
            // Configure the task subscription id.  We will only select tasks with this id.
            this.ConfigurationTaskOwner_Id = this.database.ConfigurationTaskOwners
                    .Where(t => t.TaskThreadName.Equals("OnPremisesExchangeManagement", StringComparison.OrdinalIgnoreCase))
                    .Select(t => t.ConfigurationTaskOwner_Id)
                    .FirstOrDefault();
        }
        #endregion

        #region ---- Public Methods ----
        new public void Dispose()
        {
            base.Dispose();
        }

        #endregion

        #region ---- Private Methods ----

        #endregion


    }
}
