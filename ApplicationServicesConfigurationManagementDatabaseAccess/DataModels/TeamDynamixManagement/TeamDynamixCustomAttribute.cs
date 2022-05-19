using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApplicationServicesConfigurationManagementDatabaseAccess
{
    public partial class TeamDynamixCustomAttribute
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int TeamDynamixCustomAttribute_Id { get; set; }

        public int AttributeId { get; set; }

        public string AtributeName { get; set; }

        public string Description { get; set; }

        public string FieldType { get; set; }

        public string DataType { get; set; }

        public bool IsActive { get; set; }

        public virtual ICollection<TeamDynamixForm> TeamDynamixForms{ get; set; }

    }
}
