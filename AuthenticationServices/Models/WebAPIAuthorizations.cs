using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Linq;

namespace AuthenticationServices
{
    public partial class WebAPIAuthorizations : DbContext
    {
        public WebAPIAuthorizations()
            : base("name=WebAPIAuthorizations")
        {
        }

        public virtual DbSet<OAuth2ClientRegistration> OAuth2ClientRegistrations { get; set; }
        public virtual DbSet<OAuth2ClientRoleAssignment> OAuth2ClientRoleAssignments { get; set; }
        public virtual DbSet<OAuth2ClientRole> OAuth2ClientRoles { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OAuth2ClientRegistration>()
                .HasMany(e => e.OAuth2ClientRoleAssignments)
                .WithRequired(e => e.OAuth2ClientRegistration)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<OAuth2ClientRole>()
                .HasMany(e => e.OAuth2ClientRoleAssignments)
                .WithRequired(e => e.OAuth2ClientRoles)
                .WillCascadeOnDelete(false);
        }
    }
}
