using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApplicationServicesConfigurationManagementDatabaseAccess
{
    public partial class TeamDynamixStatusClass
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int TeamDynamixStatusClass_Id { get; set; }

        public int TicketStatusID { get; set; }

        public string TicketStatusName { get; set; }

        public string TicketStatusDescription { get; set; }
    }
}