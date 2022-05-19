using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ListServiceManagement.Models
{
    public partial class ElistContacts_History
    {
        [Key]
        public int ListContactHistory_Id { get; set; }

        [StringLength(50)]
        public string Change { get; set; }

        public DateTime? ChangeTime { get; set; }

        public int? ListContact_Id { get; set; }

        [StringLength(128)]
        public string ListName { get; set; }

        [StringLength(255)]
        public string ListDisplayName { get; set; }

        [StringLength(50)]
        public string OwnerNetID { get; set; }

        [StringLength(128)]
        public string OwnerEMailAddress { get; set; }

        [StringLength(128)]
        public string OwnerDisplayName { get; set; }

        [StringLength(128)]
        public string ListDomainName { get; set; }

        [StringLength(128)]
        public string Purpose { get; set; }

        [StringLength(50)]
        public string SponsorNetID { get; set; }

        public string CornellEntity { get; set; }

        public Guid? ListContactDirectory_Id { get; set; }

        public Guid? RequestContactDirectory_Id { get; set; }

        public Guid? OwnerContactDirectory_Id { get; set; }

        public bool? Enabled { get; set; }

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
                    MetaData = JsonConvert.DeserializeObject<Dictionary<string, object>>(value);
                }
                else
                {
                    MetaData = new Dictionary<string, object>();
                }
            }
        }

        public string Notes { get; set; }

        public DateTime? WhenCreated { get; set; }

        public DateTime? WhenModified { get; set; }
    }
}