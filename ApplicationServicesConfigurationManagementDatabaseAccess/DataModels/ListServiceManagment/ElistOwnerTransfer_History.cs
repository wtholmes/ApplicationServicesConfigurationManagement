using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.Spatial;

namespace ListServiceManagement.Models
{
    public class ElistOwnerTransfer_History
    {
        /// <summary>
        /// 
        /// </summary>
        [Key]
        public int ElistOwnerTransferHistory_Id { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public String Change { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public DateTime? ChangeTime { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public int? ElistOwnerTransfer_Id { get; set; }

        /// <summary>
        /// Unique Identifier for the request.
        /// </summary>
        public Guid RequestIdentifier { get; set; }

        /// <summary>
        /// The List name
        /// </summary>
        public String ListName { get; set; }

        /// <summary>
        /// The current List Owner
        /// </summary>
        public String CurrentOwner { get; set; }

        /// <summary>
        ///  The  new List Owner
        /// </summary>
        public String NewOwner { get; set; }
        // <summary>
        /// The new list owner's email address.
        /// </summary>
        [StringLength(128)]
        public String NewOwnerEmailAddress { get; set; }

        /// <summary>
        /// The new list owner's display name
        /// </summary>
        [StringLength(128)]
        public String NewOwnerDisplayName { get; set; }
        /// <summary>
        /// The status of this request. (NEW, REQUESTED, PENDING, APPROVED, COMPLETE, CANCELLED, OBSOLETE)
        /// </summary>
        public String Status { get; set; }

        /// <summary>
        /// The Ticket ID associated with this request.
        /// </summary>
        public Int32 RequestTicketID { get; set; }

        /// <summary>
        /// The Ticket ID associated with this request.
        /// </summary>
        public Int32 AcceptTicketID { get; set; }

        /// <summary>
        /// Request Detail/Description
        /// </summary>
        public String RequestStatusDetail { get; set; }

        /// <summary>
        /// A DateTime value indicating when this address was created.
        /// </summary>
        public DateTime WhenCreated { get; set; }

        /// <summary>
        /// A DateTime value indicating when the address was modified.
        /// </summary>
        public DateTime WhenChanged { get; set; }





    }
}