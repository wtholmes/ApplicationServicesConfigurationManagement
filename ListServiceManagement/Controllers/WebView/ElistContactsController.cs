using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using ListServiceManagement.Models;

namespace ListServiceManagement.Controllers.WebView
{
    [Authorize (Roles = "ElistServiceAdminRole")]
    public class ElistContactsController : Controller
    {
        private ListServiceManagmentContext db = new ListServiceManagmentContext();

        // GET: ElistContacts
        public ActionResult Index()
        {
            return View(db.ElistContacts.OrderBy(l => l.ListName).ToList());
        }

        // GET: ElistContacts/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ElistContact elistContact = db.ElistContacts.Find(id);
            if (elistContact == null)
            {
                return HttpNotFound();
            }
            return View(elistContact);
        }

        // GET: ElistContacts/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: ElistContacts/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "ListContact_Id,ListName,ListDisplayName,OwnerNetID,OwnerEMailAddress,OwnerDisplayName,ListDomainName,Purpose,SponsorNetID,CornellEntity,ListContactDirectory_Id,RequestContactDirectory_Id,OwnerContactDirectory_Id,Enabled,SerializedMetaData,Notes,WhenCreated,WhenModified")] ElistContact elistContact)
        {
            if (ModelState.IsValid)
            {
                db.ElistContacts.Add(elistContact);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(elistContact);
        }

        // GET: ElistContacts/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ElistContact elistContact = db.ElistContacts.Find(id);
            if (elistContact == null)
            {
                return HttpNotFound();
            }
            return View(elistContact);
        }

        // POST: ElistContacts/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "ListContact_Id,ListName,ListDisplayName,OwnerNetID,OwnerEMailAddress,OwnerDisplayName,ListDomainName,Purpose,SponsorNetID,CornellEntity,ListContactDirectory_Id,RequestContactDirectory_Id,OwnerContactDirectory_Id,Enabled,SerializedMetaData,Notes,WhenCreated,WhenModified")] ElistContact elistContact)
        {
            if (ModelState.IsValid)
            {
                db.Entry(elistContact).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(elistContact);
        }

        // GET: ElistContacts/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ElistContact elistContact = db.ElistContacts.Find(id);
            if (elistContact == null)
            {
                return HttpNotFound();
            }
            return View(elistContact);
        }

        // POST: ElistContacts/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            ElistContact elistContact = db.ElistContacts.Find(id);
            db.ElistContacts.Remove(elistContact);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
