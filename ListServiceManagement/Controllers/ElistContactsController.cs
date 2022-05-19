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

namespace ListServiceManagement.Controllers
{
    /// <summary>
    ///  Exchange/Office365 Email Contact Provisioning
    /// </summary>
    public class ElistContactsController : ApiController
    {
        private ListServiceManagmentContext db = new ListServiceManagmentContext();

        /// <summary>
        /// Get a table (of type ElistContact) of all Elist Contacts.
        /// </summary>
        /// <returns>A table of ElistContact</returns>
        // GET: api/ElistContacts
        [Authorize(Roles = "ListServiceContactWebAPIRead,ListServiceContactWebAPIReadWrite")]
        [HttpGet]
        [ResponseType(typeof(IQueryable<ElistContact>))]
        public IQueryable<ElistContact> GetElistContacts()
        {
            return db.ElistContacts;
        }

        /// <summary>
        /// Get a table of type ElistContact of all Elist Contacts with the specfied owner.
        /// </summary>
        /// <param name="NetID">The owner NetID to search</param>
        /// <returns>A table of ElistContact</returns>
        [Authorize(Roles = "ListServiceContactWebAPIRead,ListServiceContactWebAPIReadWrite")]
        [HttpGet]
        [ResponseType(typeof(IQueryable<ElistContact>))]
        public IQueryable<ElistContact> GetElistContactsByOwner(String NetID)
        {
            return db.ElistContacts.Where(c => c.OwnerNetID.Equals(NetID));
        }

        /// <summary>
        /// Get a table of type ElistContact of all Elist Contacts with the specfied sponsor.
        /// </summary>
        /// <param name="NetID">The sponsor NetID to Search</param>
        /// <returns>A table of ElistContact</returns>
        [Authorize(Roles = "ListServiceContactWebAPIRead,ListServiceContactWebAPIReadWrite")]
        [HttpGet]
        [ResponseType(typeof(IQueryable<ElistContact>))]
        public IQueryable<ElistContact> GetElistContactsBySponsor(String NetID)
        {
            return db.ElistContacts.Where(c => c.SponsorNetID.Equals(NetID));
        }

        /// <summary>
        /// Get a contact (of type ElistContact) by the Elist Contact Id.
        /// </summary>
        /// <param name="Id">The Id of the contact to get.</param>
        /// <returns>ElistContact</returns>
        // GET: api/ElistContacts/5
        [Authorize(Roles = "ListServiceContactWebAPIRead,ListServiceContactWebAPIReadWrite")]
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
        [Authorize(Roles = "ListServiceContactWebAPIRead,ListServiceContactWebAPIReadWrite")]
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
        [Authorize(Roles = "ListServiceContactWebAPIReadWrite")]
        [HttpPost]
        [ResponseType(typeof(ElistContact))]
        public IHttpActionResult CreateElistContact(NewElistContact newElistContact)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            ElistContact existingContact = db.ElistContacts
                .Where(c => c.ListName.Equals(newElistContact.ListName, System.StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault();

            if (existingContact != null)
            {
                HttpResponseMessage httpResponseMessage = new HttpResponseMessage(HttpStatusCode.Conflict);
                httpResponseMessage.ReasonPhrase = String.Format("[DuplicateListName]: {0} - ListName must be unique.", newElistContact.ListName);
                return ResponseMessage(httpResponseMessage);
            }

            // Create a new ElistContact and get is list of properies.
            ElistContact elistContact = new ElistContact();
            PropertyInfo[] elistContactProperties = elistContact.GetType().GetProperties();

            // Map the ViewModel properties to the DataModel Properties
            try
            {
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
            }
            // Property mapping exception.
            catch (Exception ex)
            {
                HttpResponseMessage httpResponseMessage = new HttpResponseMessage(HttpStatusCode.InternalServerError);
                httpResponseMessage.ReasonPhrase = String.Format("[ViewModelMappingError]: {0} - Error mapping Viewmodel properties.", ex.Message);
                return ResponseMessage(httpResponseMessage);
            }
            // Add the new Entity to the DataModel and write it to the Database.
            try
            {
                db.ElistContacts.Add(elistContact);
                db.SaveChanges();
                return CreatedAtRoute("DefaultApi", new { id = elistContact.ListContact_Id }, elistContact);
            }
            // Database Exception.
            catch (Exception ex)
            {
                HttpResponseMessage httpResponseMessage = new HttpResponseMessage(HttpStatusCode.InternalServerError);
                httpResponseMessage.ReasonPhrase = String.Format("[DatabaseError]: {0} - Error creating List Contact.", ex.Message);
                return ResponseMessage(httpResponseMessage);
            }
        }

        /// <summary>
        ///     Update an Elist Contact specified by its Id using a Request Body of type UpdatedElistContact.
        /// </summary>
        /// <param name="Id">The Id of the contact to be updated.</param>
        /// <param name="updatedElistContact">A request body of type UpdatedElistContact</param>
        /// <returns></returns>
        // PATCH: api/ElistContacts/5
        [Authorize(Roles = "ListServiceContactWebAPIReadWrite")]
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
        /// <returns>ElistContact</returns>
        // PATCH: api/ElistContacts/5
        [Authorize(Roles = "ListServiceContactWebAPIReadWrite")]
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
                return Ok(elistContact);
            }
        }

        /// <summary>
        /// Removes the elist contact specfied by its Id
        /// </summary>
        /// <param name="Id">The Id of the contact to be removed.</param>
        /// <returns></returns>
        // DELETE: api/ElistContacts/5
        [Authorize(Roles = "ListServiceContactWebAPIReadWrite")]
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
        [Authorize(Roles = "ListServiceContactWebAPIReadWrite")]
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

        [Authorize(Roles = "ListServiceContactWebAPIRead,ListServiceContactWebAPIReadWrite")]
        private bool ElistContactExists(int id)
        {
            return db.ElistContacts.Count(e => e.ListContact_Id == id) > 0;
        }
    }
}