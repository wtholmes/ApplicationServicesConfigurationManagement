using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Web.Http;
using System.Web.Http.Description;
using ListServiceManagement.Models;

namespace ListServiceManagement.Controllers
{
    /// <summary>
    /// Manage Elist Owner Transfers
    /// </summary>
    public class ElistOwnerTransfersController : ApiController
    {
        private ListServiceManagment db = new ListServiceManagment();

        // GET: api/ElistOwnerTransfers
        /// <summary>
        /// Get a list of all owner transfers regardless of status.
        /// </summary>
        /// <returns></returns>
        public IQueryable<ElistOwnerTransfer> GetElistOwnerTransfers()
        {
            return db.ElistOwnerTransfers;
        }
        // GET: api/ElistOwnerTransfers
        /// <summary>
        /// Get the list of approved owner list tranfers.
        /// </summary>
        /// <returns></returns>
        public IQueryable<ElistOwnerTransfer> ApprovedElistOwnerTransfers()
        {
            return db.ElistOwnerTransfers.Where(t => t.Status.Equals("APPROVED"));
        }

        // GET: api/ElistOwnerTransfers/5
        /// <summary>
        /// Update the the list owner transfer status to complete.
        /// </summary>
        /// <param name="Id">The Id of an approved owner transfer request.</param>
        /// <param name="RequestStatusDetail">Optional Status Detail Message</param>
        /// <returns>ElistOwnerTransfer</returns>
        [ResponseType(typeof(ElistOwnerTransfer))]
        public IHttpActionResult CompleteElistOwnerTransfer(int Id, String RequestStatusDetail=null)
        {
            ElistOwnerTransfer elistOwnerTransfer = db.ElistOwnerTransfers.Find(Id);
            if (elistOwnerTransfer == null)
            {
                return NotFound();
            }

            if(elistOwnerTransfer.Status.Equals("APPROVED"))
            {
                elistOwnerTransfer.Status = "COMPLETE";
                elistOwnerTransfer.WhenChanged = DateTime.UtcNow;
                if(RequestStatusDetail != null && RequestStatusDetail.Length > 0)
                {
                    elistOwnerTransfer.RequestStatusDetail = RequestStatusDetail;
                }
                db.SaveChanges();
            }
            return Ok(elistOwnerTransfer);
        }

        // GET: api/ElistOwnerTransfers/5
        /// <summary>
        /// Cancel the list owner transfer request.
        /// </summary>
        /// <param name="Id">The Id transfer request that is in a state that can be canceled: NEW, REQUESTED, PENDING, APPROVED</param>
        /// <param name="RequestStatusDetail">Optional Status Detail Message</param>
        /// <returns></returns>
        [ResponseType(typeof(ElistOwnerTransfer))]
        public IHttpActionResult CancelElistOwnerTransfer(int Id, String RequestStatusDetail=null)
        {
            ElistOwnerTransfer elistOwnerTransfer = db.ElistOwnerTransfers.Find(Id);
            if (elistOwnerTransfer == null)
            {
                return NotFound();
            }

            if (!Regex.Match(@"(OBSOLETE|CANCELED|COMPLETE)",elistOwnerTransfer.Status).Success)
            {
                elistOwnerTransfer.Status = "CANCELED";
                elistOwnerTransfer.WhenChanged = DateTime.UtcNow;
                if (RequestStatusDetail != null && RequestStatusDetail.Length > 0)
                {
                    elistOwnerTransfer.RequestStatusDetail = RequestStatusDetail;
                }
                db.SaveChanges();
                return Ok(elistOwnerTransfer);
            }
            else
            {
                return BadRequest(String.Format("This request can not be canceled because its status is: {0}", elistOwnerTransfer.Status));
            }

        }


        // GET: api/ElistOwnerTransfers/5
        /// <summary>
        /// Get a list owner transfer by Id
        /// </summary>
        /// <param name="id">The Id of the List Owner Transfer request.</param>
        /// <returns></returns>
        [ResponseType(typeof(ElistOwnerTransfer))]
        public IHttpActionResult GetElistOwnerTransfer(int id)
        {
            ElistOwnerTransfer elistOwnerTransfer = db.ElistOwnerTransfers.Find(id);
            if (elistOwnerTransfer == null)
            {
                return NotFound();
            }

            return Ok(elistOwnerTransfer);
        }

 
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool ElistOwnerTransferExists(int id)
        {
            return db.ElistOwnerTransfers.Count(e => e.ElistOwnerTransfer_Id == id) > 0;
        }
    }
}