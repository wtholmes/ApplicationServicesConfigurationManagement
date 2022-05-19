using System.Data.Entity;

namespace AuthenticationServices
{
    public class OAuth2AuthenticationContext : DbContext
    {
        public OAuth2AuthenticationContext()
    : base("OAuth2ClientAuthorization")
        { }

        public DbSet<OAuth2ClientRegistration> OAuth2ClientRegistrations { get; set; }

        public DbSet<OAuth2ClientRole> OAuth2ClientRoles { get; set; }

        public DbSet<OAuth2ClientRoleToOAuth2ClientRegistration> OAuth2ClientRoleToOAuth2ClientRegistrations { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }

        public System.Data.Entity.DbSet<AuthenticationServices.OAuth2ClientRegistrationViewModel> OAuth2ClientRegistrationViewModel { get; set; }
    }
}