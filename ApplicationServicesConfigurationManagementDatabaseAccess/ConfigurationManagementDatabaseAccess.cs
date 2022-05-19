using ApplicationServicesConfigurationManagementDatabaseAccess.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ApplicationServicesConfigurationManagementDatabaseAccess
{
    /// <summary>
    /// Class for managing Mangement Database Access
    /// </summary>
    public class ConfigurationManagementDatabaseAccess : IDisposable
    {
        #region ---- Public Properties ----

        public ApplicationServicesConfigurationManagementContext database { get; private set; }

        public String TaskThreadName { get; private set; }

        public List<Int32> ConfigurationTasks { get; private set; }

        #endregion ---- Public Properties ----

        #region ---- Private Properties ----

        protected Int32 ConfigurationTaskOwner_Id;

        #endregion ---- Private Properties ----

        #region ---- Default Constructor ----

        static ConfigurationManagementDatabaseAccess()
        {
            var type = typeof(System.Data.Entity.SqlServer.SqlProviderServices);
            if (type == null)
                throw new Exception("Do not remove, ensures static reference to System.Data.Entity.SqlServer");
        }

        public ConfigurationManagementDatabaseAccess()
        {
            database = new ApplicationServicesConfigurationManagementContext();
        }

        #endregion ---- Default Constructor ----

        #region ---- Public Methods ----

        public void Dispose()
        {
            database.Dispose();
        }

        public Int32 GetConfigurationTaskStatusID(String Status)
        {
            return database.ConfigurationTaskStatuses
           .Where(s => s.Status.Equals(Status))
           .Select(s => s.ConfigurationTaskStatus_Id)
           .FirstOrDefault();
        }

        /// <summary>
        /// Get this list of configuration tasks by id.
        /// </summary>
        public List<Int32> GetConfigurationTasks()
        {
            this.ConfigurationTasks = database.ConfigurationTasks
                .Where(t => t.ConfigurationTaskOwner_Id == ConfigurationTaskOwner_Id)
                .Select(t => t.ConfigurationTask_Id)
                .ToList();

            return this.ConfigurationTasks;
        }

        /// <summary>
        /// Get the configuration task specified by the
        /// </summary>
        /// <param name="ConfigurationTask_Id"></param>
        /// <returns></returns>
        public ConfigurationTask GetConfigurationTask(Int32 ConfigurationTask_Id)
        {
            ConfigurationTask configurationTask = database.ConfigurationTasks
                .Where(t => t.ConfigurationTask_Id.Equals(ConfigurationTask_Id))
                .FirstOrDefault();

            return configurationTask;
        }

        public void SetConfigurationTask(ConfigurationTask configurationTask)
        {
            Int32 configurationTask_Id = configurationTask.ConfigurationTask_Id;
            ConfigurationTask cTask = database.ConfigurationTasks
                .Where(t => t.ConfigurationTask_Id.Equals(configurationTask_Id)
                    && t.ConfigurationTaskOwner_Id.Equals(this.ConfigurationTaskOwner_Id))
                .FirstOrDefault();
            if (cTask != null)
            {
                PropertyInfo[] propertiesInfo = configurationTask.GetType().GetProperties();
                foreach (PropertyInfo propertyInfo in propertiesInfo)
                {
                    if (!propertyInfo.Name.Equals("ConfigurationTask_Id"))
                    {
                        propertyInfo.SetValue(cTask, propertyInfo.GetValue(configurationTask));
                    }
                }
                database.SaveChanges();
            }
        }

        public void SetConfigurationTaskStatus(ConfigurationTask configurationTask, String taskStatus)
        {
            // Retrieve the ConfigurationTask from the Database to confirm it is a valid ConfigurationTask.
            Int32 configurationTask_Id = configurationTask.ConfigurationTask_Id;
            configurationTask = database.ConfigurationTasks
               .Where(t => t.ConfigurationTask_Id.Equals(configurationTask_Id)
                   && t.ConfigurationTaskOwner_Id.Equals(this.ConfigurationTaskOwner_Id))
               .FirstOrDefault();
            // Retrieve the ConfigurationTaskStatus from the Database to confirm it is a valid ConfigurationTaskStatus.
            ConfigurationTaskStatus configurationTaskStatus = database.ConfigurationTaskStatuses
           .Where(s => s.Status.Equals(taskStatus))
           .FirstOrDefault();

            if (configurationTask != null && configurationTaskStatus != null)
            {
                configurationTask.WhenUpdated = DateTime.UtcNow;
                configurationTask.ConfigurationTaskStatus_Id = configurationTaskStatus.ConfigurationTaskStatus_Id;
                this.database.SaveChanges();
            }
        }

        public void RemoveConfigurationTask(Int32 ConfigurationTask_Id)
        {
            ConfigurationTask configurationTask = database.ConfigurationTasks
                .Where(t => t.ConfigurationTask_Id.Equals(ConfigurationTask_Id)
                    && t.ConfigurationTaskOwner_Id.Equals(this.ConfigurationTaskOwner_Id))
                .FirstOrDefault();

            database.ConfigurationTasks.Remove(configurationTask);
            database.SaveChanges();
        }

        #endregion ---- Public Methods ----
    }
}