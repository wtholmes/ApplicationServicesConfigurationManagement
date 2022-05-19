using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace AuthenticationServices
{
    public class OAuth2ClientRole
    {
        public OAuth2ClientRole()
        {
        }

        public int OAuth2ClientRoleID { get; set; }

        [DisplayName("Role Name")]
        public String RoleName { get; set; }

        [DisplayName("Role Description")]
        public String RoleDescription { get; set; }

        [DisplayName("Role Creation Time")]
        public DateTime WhenCreated { get; set; }

        public virtual ICollection<OAuth2ClientRoleToOAuth2ClientRegistration> OAuth2ClientRoleToOAuth2ClientRegistrations { get; set; }
    }
}