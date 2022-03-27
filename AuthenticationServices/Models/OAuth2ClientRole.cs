namespace AuthenticationServices
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    public partial class OAuth2ClientRole
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public OAuth2ClientRole()
        {
            OAuth2ClientRoleAssignments = new HashSet<OAuth2ClientRoleAssignment>();
        }

        [Key]
        public int OAuth2ClientRole_Id { get; set; }

        [Required]
        [StringLength(50)]
        public string RoleName { get; set; }

        public string RoleDescription { get; set; }

        public DateTime WhenCreated { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<OAuth2ClientRoleAssignment> OAuth2ClientRoleAssignments { get; set; }
    }
}
