using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using ApplicationServicesConfigurationManagementDatabaseAccess;

namespace TeamDynamixManagement.Controllers
{
    public class TeamDynamixFormsController : Controller
    {
        private TeamDynamixManagementContext db = new TeamDynamixManagementContext();

        // GET: TeamDynamixForms
        public ActionResult Index()
        {
            return View(db.TeamDynamixForms.ToList());
        }

        // GET: TeamDynamixForms/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            TeamDynamixForm teamDynamixForm = db.TeamDynamixForms.Find(id);
            if (teamDynamixForm == null)
            {
                return HttpNotFound();
            }
            return View(teamDynamixForm);
        }

        // GET: TeamDynamixForms/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: TeamDynamixForms/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "TeamDynamixForm_Id,FormId,FormName,AppID,IsActive")] TeamDynamixForm teamDynamixForm)
        {
            if (ModelState.IsValid)
            {
                db.TeamDynamixForms.Add(teamDynamixForm);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(teamDynamixForm);
        }

        // GET: TeamDynamixForms/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            TeamDynamixForm teamDynamixForm = db.TeamDynamixForms.Find(id);
            if (teamDynamixForm == null)
            {
                return HttpNotFound();
            }
            return View(teamDynamixForm);
        }

        // POST: TeamDynamixForms/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "TeamDynamixForm_Id,FormId,FormName,AppID,IsActive")] TeamDynamixForm teamDynamixForm)
        {
            if (ModelState.IsValid)
            {
                db.Entry(teamDynamixForm).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(teamDynamixForm);
        }

        // GET: TeamDynamixForms/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            TeamDynamixForm teamDynamixForm = db.TeamDynamixForms.Find(id);
            if (teamDynamixForm == null)
            {
                return HttpNotFound();
            }
            return View(teamDynamixForm);
        }

        // POST: TeamDynamixForms/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            TeamDynamixForm teamDynamixForm = db.TeamDynamixForms.Find(id);
            db.TeamDynamixForms.Remove(teamDynamixForm);
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
