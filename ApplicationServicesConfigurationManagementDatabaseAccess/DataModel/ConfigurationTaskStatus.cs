namespace ApplicationServicesConfigurationManagementDatabaseAccess
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("ConfigurationTaskStatuses")]
    public partial class ConfigurationTaskStatus
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public ConfigurationTaskStatus()
        {
            ConfigurationTasks = new HashSet<ConfigurationTask>();
            ConfigurationTasks1 = new HashSet<ConfigurationTask>();
        }

        [Key]
        public int ConfigurationTaskStatus_Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; }

        public string Description { get; set; }

        public DateTime WhenCreated { get; set; }

        public DateTime WhenChanged { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<ConfigurationTask> ConfigurationTasks { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<ConfigurationTask> ConfigurationTasks1 { get; set; }
    }
}
