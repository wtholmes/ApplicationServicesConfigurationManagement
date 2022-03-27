using Newtonsoft.Json;
using ListServiceManagement.Models;
using ListServiceManagement.ViewModels;
using System;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Web.Http;
using System.Web.Http.Description;
using AuthenticationServices;

namespace ListServiceManagement.Controllers
{
    /// <summary>
    ///  Exchange/Office365 Email Contact Provisioning
    /// </summary>
    public class ElistContactsController : ApiController
    {
        private ListServiceManagment db = new ListServiceManagment();

        /// <summary>
        /// Get a table (of type ElistContact) of all Elist Contacts.
        /// </summary>
        /// <returns>A table of ElistContact</returns>
        // GET: api/ElistContacts
        [Authorize(Roles = "COEAWebAPIReadWrite")]
        [HttpGet]
        [ResponseType(typeof(IQueryable<ElistContact>))]
        public IQueryable<ElistContact> GetElistContacts()
        {
            return db.ElistContacts;
        }

        /// <summary>
        /// Get a contact (of type ElistContact) by the Elist Contact Id.
        /// </summary>
        /// <param name="Id">The Id of the contact to get.</param>
        /// <returns>ElistContact</returns>
        // GET: api/ElistContacts/5
        [Authorize(Roles = "COEAWebAPIReadWrite")]
        [HttpGet]
        [ResponseType(typeof(ElistContact))]
        public IHttpActionResult GetElistContact(int Id)
        {
            ElistContact elistContact = db.ElistContacts.Find(Id);
            if (elistContact == null)
            {
                return NotFound();
            }

            return Ok(elistContact);
        }

        /// <summary>
        /// Get a contact (of type ElistContact) by the Elist Contact ListName.
        /// </summary>
        /// <param name="ListName">The ListName of the contact to get.</param>
        /// <returns>ElistContact</returns>
        // GET: api/ElistContacts/5
        [Authorize(Roles = "COEAWebAPIReadWrite")]
        [HttpGet]
        [ResponseType(typeof(ElistContact))]
        public IHttpActionResult GetElistContact(string ListName)
        {
            ElistContact elistContact = db.ElistContacts
                .Where(c => c.ListName.Equals(ListName, System.StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault();

            if (elistContact == null)
            {
                return NotFound();
            }
            return Ok(elistContact);
        }

        /// <summary>
        /// Create a new Elist Contact using a Request Body of type NewElistContact.
        /// </summary>
        /// <param name="newElistContact">A request body of type NewElistContact.</param>
        /// <returns></returns>
        // POST: api/ElistContacts
        [HttpPost]
        [ResponseType(typeof(ElistContact))]
        public IHttpActionResult CreateElistContact(NewElistContact newElistContact)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Create a new ElistContact and get is list of properies.
            ElistContact elistContact = new ElistContact();
            PropertyInfo[] elistContactProperties = elistContact.GetType().GetProperties();

            // Copy any updated properties from the updated List Contact to the ElistContact;
            PropertyInfo[] newElistContactProperties = newElistContact.GetType().GetProperties();

            foreach (PropertyInfo elistContactProperty in elistContactProperties)
            {
                PropertyInfo newElistContactProperty = newElistContactProperties
                    .Where(p => p.Name.Equals(elistContactProperty.Name))
                    .FirstOrDefault();
                if (newElistContactProperty != null)
                {
                    object propertyValue = newElistContactProperty.GetValue(newElistContact, null);
                    if (propertyValue != null)
                    {
                        elistContactProperty.SetValue(elistContact, propertyValue);
                    }
                }
            }

            elistContact.WhenCreated = System.DateTime.UtcNow;
            elistContact.WhenModified = System.DateTime.UtcNow;
            db.ElistContacts.Add(elistContact);
            db.SaveChanges();

            return CreatedAtRoute("DefaultApi", new { id = elistContact.ListContact_Id }, elistContact);
        }

        /// <summary>
        /// Update an Elist Contact specified by its Id using a Request Body of type UpdatedElistContact.
        /// </summary>
        /// <param name="Id">The Id of the contact to be updated.</param>
        /// <param name="updatedElistContact">A request body of type UpdatedElistContact</param>
        /// <returns></returns>
        // PATCH: api/ElistContacts/5
        [HttpPatch]
        [ResponseType(typeof(void))]
        public IHttpActionResult UpdateElistContact(int Id, UpdatedElistContact updatedElistContact)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            ElistContact elistContact = db.ElistContacts.Find(Id);

            if (elistContact == null)
            {
                return NotFound();
            }

            if (Id != elistContact.ListContact_Id)
            {
                return BadRequest();
            }

            // Create a new ElistContact and get is list of properies.
            PropertyInfo[] elistContactProperties = elistContact.GetType().GetProperties();

            // Copy any updated properties from the updated List Contact to the ElistContact;
            PropertyInfo[] updatedElistContactProperties = updatedElistContact.GetType().GetProperties();

            foreach (PropertyInfo elistContactProperty in elistContactProperties)
            {
                PropertyInfo newElistContactProperty = updatedElistContactProperties
                    .Where(p => p.Name.Equals(elistContactProperty.Name))
                    .FirstOrDefault();
                if (newElistContactProperty != null)
                {
                    object propertyValue = newElistContactProperty.GetValue(updatedElistContact, null);
                    if (propertyValue != null)
                    {
                        elistContactProperty.SetValue(elistContact, propertyValue);
                    }
                }
            }

            if (db.Entry(elistContact).State == EntityState.Modified)
            {
                elistContact.WhenModified = System.DateTime.UtcNow;
                try
                {
                    db.SaveChanges();
                    return Ok(elistContact);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ElistContactExists(Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            else
            {
                DetailedErrorMessage detailedErrorMessage = new DetailedErrorMessage
                {
                    EventLevel = "WARNING",
                    EventID = 4001,
                    Description = "No update was performed.",
                    Details = "The data provided in this request did not require any changes to be made to list contact.",
                    CalledMethod = MethodBase.GetCurrentMethod().Name,
                    RequestURL = Request.RequestUri.ToString(),
                    RequestBody = updatedElistContact,
                    Entity = elistContact

                };
                return ResponseMessage(Request.CreateResponse<DetailedErrorMessage>(HttpStatusCode.MethodNotAllowed, detailedErrorMessage));
            }
        }

        /// <summary>
        /// Update an Elist Contact specified by its ListName using a Request Body of type UpdatedElistContact.
        /// </summary>
        /// <param name="ListName">The ListName of the contact to be updated.</param>
        /// <param name="updatedElistContact">A request body of type UpdatedElistContact</param>
        /// <returns></returns>
        // PATCH: api/ElistContacts/5
        [HttpPatch]
        [ResponseType(typeof(void))]
        public IHttpActionResult UpdateElistContact(String ListName, UpdatedElistContact updatedElistContact)
        {

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            ElistContact elistContact = db.ElistContacts
                .Where(c => c.ListName.Equals(ListName, System.StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault();

            if (elistContact == null)
            {
                return BadRequest(String.Format("Elist Contact for ListName: {0} not found", ListName));
            }

            // Create a new ElistContact and get is list of properies.
            PropertyInfo[] elistContactProperties = elistContact.GetType().GetProperties();

            // Copy any updated properties from the updated List Contact to the ElistContact;
            PropertyInfo[] updatedElistContactProperties = updatedElistContact.GetType().GetProperties();

            foreach (PropertyInfo elistContactProperty in elistContactProperties)
            {
                PropertyInfo newElistContactProperty = updatedElistContactProperties
                    .Where(p => p.Name.Equals(elistContactProperty.Name))
                    .FirstOrDefault();
                if (newElistContactProperty != null)
                {
                    object propertyValue = newElistContactProperty.GetValue(updatedElistContact, null);
                    if (propertyValue != null)
                    {
                        elistContactProperty.SetValue(elistContact, propertyValue);
                    }
                }
            }

            if (db.Entry(elistContact).State == EntityState.Modified)
            {
                elistContact.WhenModified = System.DateTime.UtcNow;
                try
                {
                    db.SaveChanges();
                    return Ok(elistContact);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ElistContactExists(elistContact.ListContact_Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            else
            {
                return BadRequest();
            }
        }

        /// <summary>
        /// Removes the elist contact specfied by its Id
        /// </summary>
        /// <param name="Id">The Id of the contact to be removed.</param>
        /// <returns></returns>
        // DELETE: api/ElistContacts/5
        [HttpDelete]
        [ResponseType(typeof(ElistContact))]
        public IHttpActionResult DeleteElistContact(int Id)
        {
            ElistContact elistContact = db.ElistContacts.Find(Id);
            if (elistContact == null)
            {
                return NotFound();
            }

            db.ElistContacts.Remove(elistContact);
            db.SaveChanges();

            return Ok(elistContact);
        }

        /// <summary>
        /// Removes the elist contact specfied the ListName
        /// </summary>
        /// <param name="ListName">The Listname of the contact to be removed.</param>
        /// <returns></returns>
        // DELETE: api/ElistContacts/5
        [HttpDelete]
        [ResponseType(typeof(ElistContact))]
        public IHttpActionResult DeleteElistContact(string ListName)
        {
            ElistContact elistContact = db.ElistContacts
                .Where(c => c.ListName.Equals(ListName, System.StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault();

            if (elistContact == null)
            {
                return NotFound();
            }

            db.ElistContacts.Remove(elistContact);
            db.SaveChanges();

            return Ok(elistContact);
        }

        /// <summary>
        /// Dispose Method
        /// </summary>
        /// <param name="disposing"></param>

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool ElistContactExists(int id)
        {
            return db.ElistContacts.Count(e => e.ListContact_Id == id) > 0;
        }
    }
}