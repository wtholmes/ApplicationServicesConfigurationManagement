using ListServiceManagement.Models;
using ListServiceManagement.ViewModels;
using System;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Http;
using System.Web.Http.Description;

namespace ListServiceManagement.Controllers
{
    /// <summary>
    /// Manage Elist Owner Transfers
    /// </summary>
    public class ElistOwnerTransfersController : ApiController
    {
        private ListServiceManagmentContext db = new ListServiceManagmentContext();

        // GET: api/ElistOwnerTransfers
        /// <summary>
        /// Get a list of all owner transfers regardless of status.
        /// </summary>
        /// <returns></returns>
        [Authorize(Roles = "ListServiceOwnerTransferWebAPIReadWrite")]
        [HttpGet]
        public IQueryable<ElistOwnerTransfer> GetElistOwnerTransfers()
        {
            return db.ElistOwnerTransfers;
        }

        // GET: api/ElistOwnerTransfers
        /// <summary>
        /// Get the list of approved owner list tranfers.
        /// </summary>
        /// <returns></returns>
        [Authorize(Roles = "ListServiceOwnerTransferWebAPIReadWrite")]
        [HttpGet]
        public IQueryable<ElistOwnerTransfer> ApprovedElistOwnerTransfers()
        {
            return db.ElistOwnerTransfers.Where(t => t.Status.Equals("APPROVED"));
        }

        // Post: api/ElistOwnerTransfers/5
        /// <summary>
        /// Update the the list owner transfer status to complete.
        /// </summary>
        /// <param name="Id">The Id of an approved owner transfer request.</param>
        /// <param name="updatedElistOwnerTransfer">An UpdatedElistOwnerTransfer Model</param>
        /// <returns>ElistOwnerTransfer</returns>
        [Authorize(Roles = "ListServiceOwnerTransferWebAPIReadWrite")]
        [HttpPost]
        [ResponseType(typeof(ElistOwnerTransfer))]
        public IHttpActionResult CompleteElistOwnerTransfer(int Id, UpdatedElistOwnerTransfer updatedElistOwnerTransfer)
        {
            ElistOwnerTransfer elistOwnerTransfer = db.ElistOwnerTransfers.Find(Id);
            if (elistOwnerTransfer == null)
            {
                return NotFound();
            }

            if (elistOwnerTransfer.Status.Equals("APPROVED"))
            {
                elistOwnerTransfer.Status = "COMPLETE";
                elistOwnerTransfer.WhenChanged = DateTime.UtcNow;

                if (updatedElistOwnerTransfer.RequestStatusDetail != null)
                {
                    if (updatedElistOwnerTransfer.RequestStatusDetail.Length > 0)
                    {
                        elistOwnerTransfer.RequestStatusDetail = updatedElistOwnerTransfer.RequestStatusDetail;
                    }
                }
                db.SaveChanges();
            }
            return Ok(elistOwnerTransfer);
        }

        /// <summary>
        /// Update the list owner status to ERROR.
        /// </summary>
        /// <param name="Id"></param>
        /// <param name="updatedElistOwnerTransfer"></param>
        /// <returns></returns>
        [Authorize(Roles = "ListServiceOwnerTransferWebAPIReadWrite")]
        [HttpPost]
        [ResponseType(typeof(ElistOwnerTransfer))]
        public IHttpActionResult SendErrorNotification(int Id, UpdatedElistOwnerTransfer updatedElistOwnerTransfer)
        {
            ElistOwnerTransfer elistOwnerTransfer = db.ElistOwnerTransfers.Find(Id);
            if (elistOwnerTransfer == null)
            {
                return NotFound();
            }

            if (elistOwnerTransfer.Status.Equals("APPROVED"))
            {
                elistOwnerTransfer.Status = "ERROR";
                elistOwnerTransfer.WhenChanged = DateTime.UtcNow;

                if (updatedElistOwnerTransfer.RequestStatusDetail != null)
                {
                    if (updatedElistOwnerTransfer.RequestStatusDetail.Length > 0)
                    {
                        elistOwnerTransfer.RequestStatusDetail = updatedElistOwnerTransfer.RequestStatusDetail;
                    }
                }
                db.SaveChanges();
            }
            return Ok(elistOwnerTransfer);
        }

        /// <summary>
        ///  Update the the list owner transfer status to complete.
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        [Authorize(Roles = "ListServiceOwnerTransferWebAPIReadWrite")]
        [HttpGet]
        [ResponseType(typeof(ElistOwnerTransfer))]
        public IHttpActionResult CompleteElistOwnerTransfer(int Id)
        {
            ElistOwnerTransfer elistOwnerTransfer = db.ElistOwnerTransfers.Find(Id);
            if (elistOwnerTransfer == null)
            {
                return NotFound();
            }

            elistOwnerTransfer.Status = "COMPLETE";
            elistOwnerTransfer.WhenChanged = DateTime.UtcNow;
            db.SaveChanges();

            return Ok(elistOwnerTransfer);
        }

        // GET: api/ElistOwnerTransfers/5
        /// <summary>
        /// Cancel the list owner transfer request.
        /// </summary>
        /// <param name="Id">The Id transfer request that is in a state that can be canceled: NEW, REQUESTED, PENDING, APPROVED</param>
        /// <param name="updatedElistOwnerTransfer">Optional Status Detail Message</param>
        /// <returns></returns>
        [Authorize(Roles = "ListServiceOwnerTransferWebAPIReadWrite")]
        [HttpPost]
        [ResponseType(typeof(ElistOwnerTransfer))]
        public IHttpActionResult CancelElistOwnerTransfer(int Id, UpdatedElistOwnerTransfer updatedElistOwnerTransfer)
        {
            ElistOwnerTransfer elistOwnerTransfer = db.ElistOwnerTransfers.Find(Id);
            if (elistOwnerTransfer == null)
            {
                return NotFound();
            }

            if (!Regex.Match(@"(OBSOLETE|CANCELLED|COMPLETE)", elistOwnerTransfer.Status).Success)
            {
                elistOwnerTransfer.Status = "CANCELLED";
                elistOwnerTransfer.WhenChanged = DateTime.UtcNow;
                if (updatedElistOwnerTransfer.RequestStatusDetail != null)
                {
                    if (updatedElistOwnerTransfer.RequestStatusDetail.Length > 0)
                    {
                        elistOwnerTransfer.RequestStatusDetail = updatedElistOwnerTransfer.RequestStatusDetail;
                    }
                }
                db.SaveChanges();
                return Ok(elistOwnerTransfer);
            }
            else
            {
                return BadRequest(String.Format("This request can not be canceled because its status is: {0}", elistOwnerTransfer.Status));
            }
        }

        /// <summary>
        /// Cancel the list owner transfer request.
        /// </summary>
        /// <param name="Id">The Id transfer request that is in a state that can be canceled: NEW, REQUESTED, PENDING, APPROVED</param>
        /// <returns></returns>
        [Authorize(Roles = "ListServiceOwnerTransferWebAPIReadWrite")]
        [HttpGet]
        [ResponseType(typeof(ElistOwnerTransfer))]
        public IHttpActionResult CancelElistOwnerTransfer(int Id)
        {
            ElistOwnerTransfer elistOwnerTransfer = db.ElistOwnerTransfers.Find(Id);
            if (elistOwnerTransfer == null)
            {
                return NotFound();
            }

            if (!Regex.Match(@"(OBSOLETE|CANCELLED|COMPLETE)", elistOwnerTransfer.Status).Success)
            {
                elistOwnerTransfer.Status = "CANCELLED";
                elistOwnerTransfer.WhenChanged = DateTime.UtcNow;
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
        [Authorize(Roles = "ListServiceOwnerTransferWebAPIReadWrite")]
        [HttpGet]
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