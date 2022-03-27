using System.Data.Entity;

namespace ApplicationServicesConfigurationManagementDatabaseAccess
{
    public partial class ApplicationServicesConfigurationManagement : DbContext
    {
        public ApplicationServicesConfigurationManagement()
            : base("data source=localhost;initial catalog=ApplicationServicesConfigurationManagement;integrated security=True;MultipleActiveResultSets=True;App=EntityFramework")
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