namespace AuthenticationServices
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    public partial class OAuth2ClientRegistration
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public OAuth2ClientRegistration()
        {
            OAuth2ClientRoleAssignments = new HashSet<OAuth2ClientRoleAssignment>();
        }

        [Key]
        public int OAuth2ClientRegistration_Id { get; set; }

        [Required]
        [StringLength(50)]
        public string RequestingUPN { get; set; }

        [Required]
        public string Description { get; set; }

        public DateTime RequestTime { get; set; }

        public DateTime ExpirationTime { get; set; }

        public Guid ClientId { get; set; }

        [Required]
        public string ClientSecret { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<OAuth2ClientRoleAssignment> OAuth2ClientRoleAssignments { get; set; }
    }
}
