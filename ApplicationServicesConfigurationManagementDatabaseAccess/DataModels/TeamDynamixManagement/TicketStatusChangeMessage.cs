using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApplicationServicesConfigurationManagementDatabaseAccess
{
    public partial class TicketStatusChangeMessage
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int TicketStatusChangeMessage_Id { get; set; }

        [Required]
        [Display(Name = "IntegrationID")]
        [ForeignKey("TeamDynamixIntegration")]
        public int IntegrationID { get; set; }

        [Required]
        [Display(Name = "Current Ticket Status")]
        [ForeignKey("CurrentTeamDynamixStatusClass")]
        public int CurrentStatusID { get; set; }

        [Required]
        [Display(Name = "Updated Ticket Status")]
        [ForeignKey("UpdatedTeamDynamixStatusClass")]
        public int UpdatedStatusID { get; set; }

        [Required]
        [DataType(DataType.MultilineText)]
        public String Message { get; set; }

        [Display(Name = "CurrentStatus")]
        public virtual TeamDynamixStatusClass CurrentTeamDynamixStatusClass { get; set; }

        [Display(Name = "UpdatedStatus")]
        public virtual TeamDynamixStatusClass UpdatedTeamDynamixStatusClass { get; set; }

        [ForeignKey("TeamDynamixIntegration_Id")]
        public virtual TeamDynamixIntegration TeamDynamixIntegration { get; set; }
    }
}