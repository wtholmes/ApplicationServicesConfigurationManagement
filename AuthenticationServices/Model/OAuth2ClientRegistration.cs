using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace AuthenticationServices
{
    public class OAuth2ClientRegistration
    {
        public OAuth2ClientRegistration()
        {
        }

        public int OAuth2ClientRegistrationID { get; set; }

        [DisplayName("Client Identifier")]
        public Guid ClientID { get; set; }

        [DisplayName("Client Secret")]
        public String ClientSecret { get; set; }

        [DisplayName("Client Description")]
        public String ClientDescription { get; set; }

        [DisplayName("Requesting UPN")]
        public String RequestingUPN { get; set; }

        [DisplayName("Client Creation Time")]
        public DateTime RequestTime { get; set; }

        [DisplayName("Client Expiration Time")]
        public DateTime ExpirationTime { get; set; }

        public virtual ICollection<OAuth2ClientRoleToOAuth2ClientRegistration> OAuth2ClientRoleToOAuth2ClientRegistrations { get; set; }
    }
}