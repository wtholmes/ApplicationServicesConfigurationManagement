using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Linq;

namespace ApplicationServicesConfigurationManagementDatabaseAccess.DataModel
{
    public partial class ApplicationServicesConfigurationManagementModel : DbContext
    {
        public ApplicationServicesConfigurationManagementModel()
            : base("name=ApplicationServicesConfigurationManagement")
        {
        }

        public virtual DbSet<ActiveDirectoryDomainController> ActiveDirectoryDomainControllers { get; set; }
        public virtual DbSet<ConfigurationTaskOwner> ConfigurationTaskOwners { get; set; }
        public virtual DbSet<ConfigurationTask> ConfigurationTasks { get; set; }
        public virtual DbSet<ConfigurationTaskStatus> ConfigurationTaskStatuses { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ConfigurationTaskOwner>()
                .HasMany(e => e.ConfigurationTasks)
                .WithRequired(e => e.ConfigurationTaskOwner)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<ConfigurationTaskStatus>()
                .HasMany(e => e.ConfigurationTasks)
                .WithRequired(e => e.ConfigurationTaskStatus)
                .HasForeignKey(e => e.ConfigurationTaskStatus_Id)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<ConfigurationTaskStatus>()
                .HasMany(e => e.ConfigurationTasks1)
                .WithRequired(e => e.ConfigurationTaskStatus1)
                .HasForeignKey(e => e.RequestStatus_Id)
                .WillCascadeOnDelete(false);
        }
    }
}
