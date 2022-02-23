namespace ApplicationServicesConfigurationManagementDatabaseAccess.DataModel
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    public partial class ConfigurationTask
    {
        [Key]
        public int ConfigurationTask_Id { get; set; }

        public int ConfigurationTaskOwner_Id { get; set; }

        public int ConfigurationTaskStatus_Id { get; set; }

        public Guid? DirectoryObjectIdentifier { get; set; }

        public Guid RequestIdentifier { get; set; }

        [Required]
        [StringLength(50)]
        public string RequestType { get; set; }

        public int RequestStatus_Id { get; set; }

        public DateTime WhenCreated { get; set; }

        public DateTime WhenUpdated { get; set; }

        public int RetryCount { get; set; }

        public string LastTaskActionDetail { get; set; }

        public virtual ConfigurationTaskOwner ConfigurationTaskOwner { get; set; }

        public virtual ConfigurationTaskStatus ConfigurationTaskStatus { get; set; }

        public virtual ConfigurationTaskStatus ConfigurationTaskStatus1 { get; set; }
    }
}
