using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApplicationServicesConfigurationManagementDatabaseAccess
{
    public partial class TeamDynamixForm
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]   
        public int TeamDynamixForm_Id { get; set; }
    
        public int FormId { get; set; }

        public string FormName { get; set; }

        public int AppID { get ; set; }

        public bool IsActive { get; set; }

        public List<TeamDynamixCustomAttribute> CustomAttributes { get; set; }

        [ForeignKey("TeamDynamixIntegration_Id")]
        public virtual ICollection<TeamDynamixIntegration> TeamDynamixIntegrations { get; set; }
    
    }
}
