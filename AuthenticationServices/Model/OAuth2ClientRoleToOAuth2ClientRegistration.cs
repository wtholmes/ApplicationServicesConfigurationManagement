namespace AuthenticationServices
{
    public class OAuth2ClientRoleToOAuth2ClientRegistration
    {
        public int OAuth2ClientRoleToOAuth2ClientRegistrationID { get; set; }
        public int OAuth2ClientRegistrationID { get; set; }
        public int OAuth2ClientRoleID { get; set; }

        public virtual OAuth2ClientRegistration OAuth2ClientRegistration { get; set; }

        public virtual OAuth2ClientRole OAuth2ClientRole { get; set; }
    }
}