using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.Spatial;

namespace ListServiceManagement.ViewModels
{
    /// <summary>
    /// An Updated Elist Contact Request. Only updated properties are required.
    /// </summary>
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class UpdatedElistContact
    {
        /// <summary>
        /// The name of the List without its domain.
        /// </summary>

        [StringLength(128)]
        public string ListName { get; set; }

        /// <summary>
        /// The List Display Name.
        /// </summary>

        [StringLength(255)]
        public string ListDisplayName { get; set; }

        /// <summary>
        /// The Cornell NetID of the Owner.
        /// </summary>

        [StringLength(50)]
        public string OwnerNetID { get; set; }
        /// <summary>
        /// The Email Address that the owner will use when managing the list.
        /// </summary>

        [StringLength(128)]
        public string OwnerEMailAddress { get; set; }
        /// <summary>
        /// The Display Name that will be used by the Owner when managing the list
        /// </summary>

        [StringLength(128)]
        public string OwnerDisplayName { get; set; }
        /// <summary>
        /// The domain that this list will use for routing email to the appropriate list manager instance.
        /// </summary>

        [StringLength(128)]
        public string ListDomainName { get; set; }
        /// <summary>
        /// The business purpose for this list (Enumerated Value)
        /// </summary>

        [StringLength(128)]
        public string Purpose { get; set; }
        /// <summary>
        /// The Cornell NetID for the List's Sponsor
        /// </summary>

        [StringLength(50)]
        public string SponsorNetID { get; set; }
        /// <summary>
        /// The Cornell Entity 
        /// </summary>
        public string CornellEntity { get; set; }
        /// <summary>
        /// Specifies if the Contact should be Mail Enabled. When disabled the contact will not process email. 
        /// </summary>
        public bool Enabled { get; set; }
        /// <summary>
        /// A general notes field.
        /// </summary>
        public string Notes { get; set; }
    }
}