using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApplicationServicesConfigurationManagementDatabaseAccess
{
    public class TeamDynamixUser
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int TeamDynamixUser_Id { get; set; }

        public Guid Uid { get; set; }

        public string UserPrincipalName { get; set; }

        public string UserAsJSON { get; set; }
    }
}
