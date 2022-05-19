using ActiveDirectoryAccess;
using ApplicationServicesConfigurationManagementDatabaseAccess.Models;
using System;
using System.Collections.Generic;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;

namespace ApplicationServicesConfigurationManagementDatabaseAccess
{
    /// <summary>
    /// Derived Class for managing Active Directory Mangement Database Access
    /// </summary>
    public class ActiveDirectoryManagementDatabaseAccess : ConfigurationManagementDatabaseAccess
    {
        #region ---- Public Properties ----

        public List<DomainController> DomainControllers { get; set; }

        public List<DomainControllerUSNQueryRange> DomainControllerUSNQueryRanges { get; private set; }

        public List<DomainControllerUSNQueryRange> SiteDomainControllerUSNQueryRanges { get; private set; }

        #endregion ---- Public Properties ----

        #region ---- Explicit Constructors ----

        public ActiveDirectoryManagementDatabaseAccess() : base()
        {
            // Configure the task subscription id.  We will only select tasks with this id.
            this.ConfigurationTaskOwner_Id = this.database.ConfigurationTaskOwners
                .Where(t => t.TaskThreadName.Equals("ActiveDirectoryMonitor", StringComparison.OrdinalIgnoreCase))
                .Select(t => t.ConfigurationTaskOwner_Id)
                .FirstOrDefault();

            DomainControllers = new List<DomainController>();

            DomainControllerUSNQueryRanges = new List<DomainControllerUSNQueryRange>();

            SiteDomainControllerUSNQueryRanges = new List<DomainControllerUSNQueryRange>();
        }

        public ActiveDirectoryManagementDatabaseAccess(ActiveDirectoryTopology activeDirectoryTopology) : base()
        {
            DomainControllers = new List<DomainController>();

            DomainControllerUSNQueryRanges = new List<DomainControllerUSNQueryRange>();

            SiteDomainControllerUSNQueryRanges = new List<DomainControllerUSNQueryRange>();

            GetDomainControllers(activeDirectoryTopology);
        }

        #endregion ---- Explicit Constructors ----

        #region ---- Public Methods ----

        public new void Dispose()
        {
            this.DomainControllers.Clear();
            this.DomainControllerUSNQueryRanges.Clear();
            this.SiteDomainControllerUSNQueryRanges.Clear();

            base.Dispose();
        }

        /// <summary>
        /// Get all active directory domain controllers that may be used for queries.
        /// </summary>
        /// <param name="domainController"></param>
        public void GetDomainControllers(ActiveDirectoryTopology activeDirectoryTopology)
        {
            foreach (DomainController domainController in activeDirectoryTopology.DomainControllers)
            {
                ActiveDirectoryDomainController activeDirectoryDomainController = this.database.ActiveDirectoryDomainControllers
                    .Where(dc => dc.Name.Equals(domainController.Name))
                    .FirstOrDefault();

                DomainControllerUSNQueryRange domainControllerUSNQueryRange = null;

                if (activeDirectoryDomainController == null)
                {
                    DomainControllers.Add(domainController);

                    domainControllerUSNQueryRange = new DomainControllerUSNQueryRange()
                    {
                        DomainControllerName = domainController.Name,
                        StartUSN = domainController.HighestCommittedUsn,
                        EndUSN = domainController.HighestCommittedUsn
                    };

                    ActiveDirectoryDomainController newActiveDirectoryDomainController = new ActiveDirectoryDomainController()
                    {
                        IPAddresss = domainController.IPAddress,
                        CurrentTime = domainController.CurrentTime,
                        Enabled = true,
                        HighestCommittedUSN = domainController.HighestCommittedUsn,
                        IsGlobalCatalog = domainController.IsGlobalCatalog(),
                        Name = domainController.Name,
                        OSVersion = domainController.OSVersion,
                        SiteName = domainController.SiteName
                    };

                    this.database.ActiveDirectoryDomainControllers.Add(newActiveDirectoryDomainController);
                }
                else
                {
                    domainControllerUSNQueryRange = new DomainControllerUSNQueryRange()
                    {
                        DomainControllerName = domainController.Name,
                        StartUSN = activeDirectoryDomainController.HighestCommittedUSN,
                        EndUSN = domainController.HighestCommittedUsn
                    };

                    activeDirectoryDomainController.IPAddresss = domainController.IPAddress;
                    activeDirectoryDomainController.CurrentTime = domainController.CurrentTime;
                    activeDirectoryDomainController.Enabled = true;
                    activeDirectoryDomainController.HighestCommittedUSN = domainController.HighestCommittedUsn;
                    activeDirectoryDomainController.IsGlobalCatalog = domainController.IsGlobalCatalog();
                    activeDirectoryDomainController.OSVersion = domainController.OSVersion;
                    activeDirectoryDomainController.SiteName = domainController.SiteName;
                }

                DomainControllerUSNQueryRanges.Add(domainControllerUSNQueryRange);

                if (activeDirectoryTopology.SiteDomainControllers.Where(dc => dc.Name.Equals(domainController.Name)).FirstOrDefault() != null)
                {
                    SiteDomainControllerUSNQueryRanges.Add(domainControllerUSNQueryRange);
                }
            }

            this.database.SaveChanges();
        }

        public DomainControllerUSNQueryRange SelectSiteDomainControler()
        {
            Random random = new Random();
            DomainControllerUSNQueryRange domainControllerUSNQueryRange = this.SiteDomainControllerUSNQueryRanges[random.Next(this.SiteDomainControllerUSNQueryRanges.Count)];
            return domainControllerUSNQueryRange;
        }

        public DomainControllerUSNQueryRange SelectDomainControler()
        {
            Random random = new Random();
            DomainControllerUSNQueryRange domainControllerUSNQueryRange = this.DomainControllerUSNQueryRanges[random.Next(this.DomainControllerUSNQueryRanges.Count)];
            return domainControllerUSNQueryRange;
        }

        public void CreateConfigurationTasksForActiveDirecoryObjects(List<dynamic> activeDirectoryEntities)
        {
            // Get the current list of task owners from the Configuration Management Database.
            List<ConfigurationTaskOwner> configurationTaskOwners = this.database.ConfigurationTaskOwners.ToList();
            foreach (ActiveDirectoryEntity activeDirectoryEntity in activeDirectoryEntities)
            {
                Guid RequestIdentifier = Guid.NewGuid();
                Int32 ConfigurationTaskStatusID = this.database.ConfigurationTaskStatuses
                                            .Where(s => s.Status.Equals("NEW", StringComparison.OrdinalIgnoreCase))
                                            .Select(s => s.ConfigurationTaskStatus_Id)
                                            .FirstOrDefault();

                foreach (ConfigurationTaskOwner configurationTaskOwner in configurationTaskOwners)
                {
                    // Check if there is an existing task for this Directory Object with a Status of NEW for current task owner.
                    ConfigurationTask existingTask = this.database.ConfigurationTasks
                        .Where(t => t.DirectoryObjectIdentifier == activeDirectoryEntity.objectGUID
                                    && t.ConfigurationTaskStatus_Id.Equals(ConfigurationTaskStatusID)
                                    && t.ConfigurationTaskOwner_Id.Equals(configurationTaskOwner.ConfigurationTaskOwner_Id))
                        .FirstOrDefault();

                    // If there is not an existing new task for this owner create one.
                    if (existingTask == null)
                    {
                        ConfigurationTask configurationTask = new ConfigurationTask()
                        {
                            ConfigurationTaskOwner_Id = configurationTaskOwner.ConfigurationTaskOwner_Id,
                            ConfigurationTaskStatus_Id = ConfigurationTaskStatusID,
                            DirectoryObjectIdentifier = activeDirectoryEntity.objectGUID,
                            RequestIdentifier = RequestIdentifier,
                            RequestType = "DIRECTORYOBJECTCHANGE",
                            RequestStatus_Id = ConfigurationTaskStatusID,
                            WhenCreated = DateTime.UtcNow,
                            WhenUpdated = DateTime.UtcNow
                        };

                        this.database.ConfigurationTasks.Add(configurationTask);
                    }
                }
            }
            this.database.SaveChanges();
        }

        #endregion ---- Public Methods ----
    }
}