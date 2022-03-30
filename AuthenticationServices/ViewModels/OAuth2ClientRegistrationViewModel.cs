using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;

namespace AuthenticationServices
{
    public class OAuth2ClientRegistrationViewModel
    {
        public int ID { get; set; }

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
        [DisplayName("Client Roles")]
        public List<CheckBoxViewModel> OAuth2ClientRoles { get; set; }
    }
}