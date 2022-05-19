using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ListServiceManagement.Models
{
    /// <summary>
    /// Defines the provisioning of an Exchange Mail Contact for use by the List Manager Service. These Mail Contacts provide
    /// a Directory Presence for Elists in Microsoft Exchange and Microsoft Exchange Online.
    /// </summary>
    public partial class ElistContact
    {
        /// <summary>
        /// This property is automatically generated when creating a new contact.
        /// </summary>
        [Key]
        public int ListContact_Id { get; set; }

        /// <summary>
        /// The name of the List without its domain.
        /// </summary>
        [Required]
        [StringLength(128)]
        [Index(nameof(ListName), IsUnique = true)]
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
        [StringLength(128)]
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
        public Guid ListContactDirectory_Id { get; set; }

        /// <summary>
        /// The Active Directory Object ID for the List Request Contact. This property will be populated on provisioning.
        /// </summary>
        public Guid RequestContactDirectory_Id { get; set; }

        /// <summary>
        /// The Active Directory Object ID for the List Owner Contact. This property will be populated on provisioning.
        /// </summary>
        public Guid OwnerContactDirectory_Id { get; set; }

        /// <summary>
        /// Specifies if the Contact should be Mail Enabled. When disabled the contact will not process email.
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Elist MetaData
        /// </summary>
        [NotMapped]
        public Dictionary<string, object> MetaData { get; set; }

        /// <summary>
        /// Serialized Metadata to be stored as JSON
        /// </summary>
        [Column("MetaData")]
        public string SerializedMetaData
        {

            get { return JsonConvert.SerializeObject(MetaData); }
            set
            {
                if (value != null)
                {
                    try
                    {
                        MetaData = JsonConvert.DeserializeObject<Dictionary<string, object>>(value);
                    }
                    catch
                    {
                        MetaData= new Dictionary<string, object>();
                    }
                }
                else
                {
                    MetaData = new Dictionary<string, object>();
                }
            }
        }

        /// <summary>
        /// A general notes field.
        /// </summary>
        public string Notes { get; set; }

        /// <summary>
        /// Specifies when the contact was created. This will match the contact creation time in Active Directory.
        /// </summary>
        public DateTime WhenCreated { get; set; }

        /// <summary>
        /// Spefices when the contact was last modified. This will match the contact modification time in Active Directory.
        /// </summary>
        public DateTime WhenModified { get; set; }
    }
}