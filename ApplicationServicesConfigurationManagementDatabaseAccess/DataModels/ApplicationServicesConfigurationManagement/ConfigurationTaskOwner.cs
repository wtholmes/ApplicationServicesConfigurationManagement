namespace ApplicationServicesConfigurationManagementDatabaseAccess.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    public partial class ConfigurationTaskOwner
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public ConfigurationTaskOwner()
        {
            ConfigurationTasks = new HashSet<ConfigurationTask>();
        }

        [Key]
        public int ConfigurationTaskOwner_Id { get; set; }

        [Required]
        [StringLength(50)]
        public string TaskThreadName { get; set; }

        public string DisplayName { get; set; }

        public string Description { get; set; }

        public string Notes { get; set; }

        public DateTime WhenCreated { get; set; }

        public DateTime WhenChanged { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<ConfigurationTask> ConfigurationTasks { get; set; }
    }
}
