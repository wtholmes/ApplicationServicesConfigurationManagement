using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ListServiceManagement.ViewModels
{
    /// <summary>
    /// A new ElistContact Request.
    /// </summary>
    public class NewElistContact
    {
        /// <summary>
        /// The name of the List without its domain.
        /// </summary>
        [Required]
        [StringLength(128)]
        public string ListName { get; set; }

        /// <summary>
        /// The name of the List without its domain.
        /// </summary>
        [Required]
        [StringLength(255)]
        public string ListDisplayName { get; set; }

        /// <summary>
        /// The Cornell NetID of the Owner.
        /// </summary>
        [Required]
        [StringLength(50)]
        public string OwnerNetID { get; set; }

        /// <summary>
        /// The Email Address that the owner will use when managing the list.
        /// </summary>
        [Required]
        [StringLength(128)]
        public string OwnerEMailAddress { get; set; }

        /// <summary>
        /// The Display Name that will be used by the Owner when managing the list
        /// </summary>
        [Required]
        [StringLength(255)]
        public string OwnerDisplayName { get; set; }

        /// <summary>
        /// The domain that this list will use for routing email to the appropriate list manager instance.
        /// </summary>
        [Required]
        [StringLength(128)]
        public string ListDomainName { get; set; }

        /// <summary>
        /// The business purpose for this list (Enumerated Value)
        /// </summary>
        [Required]
        [StringLength(128)]
        public string Purpose { get; set; }

        /// <summary>
        /// The Cornell NetID for the List's Sponsor
        /// </summary>
        [Required]
        [StringLength(50)]
        public string SponsorNetID { get; set; }

        /// <summary>
        /// The Cornell Entity
        /// </summary>
        public string CornellEntity { get; set; }

        /// <summary>
        /// The Active Directory Object ID for the List Contact. This property will be populated on provisioning.
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Elist MetaData
        /// </summary>
        public Dictionary<string, object> MetaData { get; set; }

        /// <summary>
        /// A general notes field.
        /// </summary>
        public string Notes { get; set; }
    }
}