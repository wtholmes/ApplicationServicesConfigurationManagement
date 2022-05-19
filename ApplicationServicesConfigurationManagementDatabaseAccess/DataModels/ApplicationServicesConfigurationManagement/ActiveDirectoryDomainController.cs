using System;
using System.ComponentModel.DataAnnotations;

namespace ApplicationServicesConfigurationManagementDatabaseAccess
{
    public partial class ActiveDirectoryDomainController
    {
        [Key]
        public int DomainController_Id { get; set; }

        public bool Enabled { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; }

        [Required]
        [StringLength(512)]
        public string IPAddresss { get; set; }

        [Required]
        [StringLength(128)]
        public string OSVersion { get; set; }

        public DateTime CurrentTime { get; set; }

        public bool IsGlobalCatalog { get; set; }

        public long HighestCommittedUSN { get; set; }

        [Required]
        [StringLength(50)]
        public string SiteName { get; set; }

        [StringLength(50)]
        public string SubNet { get; set; }
    }
}