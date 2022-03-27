namespace AuthenticationServices
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    public partial class OAuth2ClientRoleAssignment
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int OAuth2ClientRoleAssignment_Id { get; set; }

        public int OAuth2ClientRegistration_Id { get; set; }

        public int OAuth2ClientRole_Id { get; set; }

        public virtual OAuth2ClientRegistration OAuth2ClientRegistration { get; set; }

        public virtual OAuth2ClientRole OAuth2ClientRoles { get; set; }
    }
}
