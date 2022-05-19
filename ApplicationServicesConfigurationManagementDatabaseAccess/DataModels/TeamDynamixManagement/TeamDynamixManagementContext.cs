﻿using System.Data.Entity;

namespace ApplicationServicesConfigurationManagementDatabaseAccess
{
    public partial class TeamDynamixManagementContext : DbContext
    {
        public TeamDynamixManagementContext() : base("name=TeamDynamixManagement")
        {
        }

        public virtual DbSet<TeamDynamixIntegration> TeamDynamixIntegrations { get; set; }

        public virtual DbSet<TeamDynamixForm> TeamDynamixForms { get; set; }

        public virtual DbSet<TeamDynamixCustomAttribute> TeamDynamixCustomAttributes { get; set; }

        public virtual DbSet<TeamDynamixStatusClass> TeamDynamixStatusClasses { get; set; }

        public virtual DbSet<TicketStatusChangeMessage> TicketStatusChangeMessages { get; set; }

        public virtual DbSet<TeamDynamixUser> TeamDynamixUsers { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TicketStatusChangeMessage>()
                .HasRequired(t => t.CurrentTeamDynamixStatusClass)
                .WithMany()
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<TicketStatusChangeMessage>()
               .HasRequired(t => t.UpdatedTeamDynamixStatusClass)
               .WithMany()
               .WillCascadeOnDelete(false);
        }
    }
}