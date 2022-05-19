using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApplicationServicesConfigurationManagementDatabaseAccess
{
    public partial class TeamDynamixIntegration
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int TeamDynamixIntegration_Id { get; set; }

        [Required]
        [Display(Name = "Integration Name")]
        public String IntegrationName { get; set; }

        public String Description { get; set; }

        [ScaffoldColumn(false)]
        public Guid OwnerObjectGuid { get; set; }

        [Display(Name = "Owner")]
        [NotMapped]
        public String UserPrincipalName { get; set; }

        [ForeignKey("TeamDynamixForm")]
        public int FormID { get; set; }

        [ForeignKey("TeamDynamixForm_Id")]
        public TeamDynamixForm TeamDynamixForm { get; set; }

        [ForeignKey("TicketStatusChangeMessage")]
        public virtual ICollection<TicketStatusChangeMessage> TicketStatusChangeMessages { get; set; }
    }
}