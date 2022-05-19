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
    public class TeamDynamixStatusClassesController : Controller
    {
        private TeamDynamixManagementContext db = new TeamDynamixManagementContext();

        // GET: TeamDynamixStatusClasses
        public ActionResult Index()
        {
            return View(db.TeamDynamixStatusClasses.ToList());
        }

        // GET: TeamDynamixStatusClasses/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            TeamDynamixStatusClass teamDynamixStatusClass = db.TeamDynamixStatusClasses.Find(id);
            if (teamDynamixStatusClass == null)
            {
                return HttpNotFound();
            }
            return View(teamDynamixStatusClass);
        }

        // GET: TeamDynamixStatusClasses/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: TeamDynamixStatusClasses/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "TeamDynamixStatusClass_Id,TicketStatusID,TicketStatusName,TicketStatusDescription")] TeamDynamixStatusClass teamDynamixStatusClass)
        {
            if (ModelState.IsValid)
            {
                db.TeamDynamixStatusClasses.Add(teamDynamixStatusClass);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(teamDynamixStatusClass);
        }

        // GET: TeamDynamixStatusClasses/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            TeamDynamixStatusClass teamDynamixStatusClass = db.TeamDynamixStatusClasses.Find(id);
            if (teamDynamixStatusClass == null)
            {
                return HttpNotFound();
            }
            return View(teamDynamixStatusClass);
        }

        // POST: TeamDynamixStatusClasses/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "TeamDynamixStatusClass_Id,TicketStatusID,TicketStatusName,TicketStatusDescription")] TeamDynamixStatusClass teamDynamixStatusClass)
        {
            if (ModelState.IsValid)
            {
                db.Entry(teamDynamixStatusClass).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(teamDynamixStatusClass);
        }

        // GET: TeamDynamixStatusClasses/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            TeamDynamixStatusClass teamDynamixStatusClass = db.TeamDynamixStatusClasses.Find(id);
            if (teamDynamixStatusClass == null)
            {
                return HttpNotFound();
            }
            return View(teamDynamixStatusClass);
        }

        // POST: TeamDynamixStatusClasses/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            TeamDynamixStatusClass teamDynamixStatusClass = db.TeamDynamixStatusClasses.Find(id);
            db.TeamDynamixStatusClasses.Remove(teamDynamixStatusClass);
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
