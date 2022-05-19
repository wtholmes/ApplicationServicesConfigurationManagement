using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using ActiveDirectoryAccess;
using ApplicationServicesConfigurationManagementDatabaseAccess;

namespace TeamDynamixManagement.Controllers
{
    [Authorize]
    public class TeamDynamixIntegrationsController : Controller
    {
        private TeamDynamixManagementContext db = new TeamDynamixManagementContext();
        private ActiveDirectoryContext  ad = new ActiveDirectoryContext();

        // GET: TeamDynamixIntegrations
        public ActionResult Index()
        {
            List<TeamDynamixIntegration> teamDynamixIntegrations = db.TeamDynamixIntegrations.Include(t => t.TeamDynamixForm)
                .ToList();
            
            foreach(TeamDynamixIntegration teamDynamixIntegration in teamDynamixIntegrations)
            {
                ActiveDirectoryEntity activeDirectoryEntity = ad.SearchDirectory(teamDynamixIntegration.OwnerObjectGuid);
                teamDynamixIntegration.UserPrincipalName = activeDirectoryEntity.userprincipalName;
            }

            return View(teamDynamixIntegrations);
        }

        // GET: TeamDynamixIntegrations/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            TeamDynamixIntegration teamDynamixIntegration = db.TeamDynamixIntegrations.Find(id);
            ActiveDirectoryEntity activeDirectoryEntity = ad.SearchDirectory(teamDynamixIntegration.OwnerObjectGuid);
            teamDynamixIntegration.UserPrincipalName = activeDirectoryEntity.userprincipalName;

            if (teamDynamixIntegration == null)
            {
                return HttpNotFound();
            }
            return View(teamDynamixIntegration);
        }

        // GET: TeamDynamixIntegrations/Create
        public ActionResult Create()
        {
            ActiveDirectoryEntity activeDirectoryEntity = ad.SearchDirectory(User.Identity.Name);

            TeamDynamixIntegration teamDynamixIntegration = new TeamDynamixIntegration()
            {
                UserPrincipalName = activeDirectoryEntity.userprincipalName,
                OwnerObjectGuid = activeDirectoryEntity.objectGUID
            };

            ViewBag.FormID = new SelectList(db.TeamDynamixForms, "TeamDynamixForm_Id", "FormName");
            return View(teamDynamixIntegration);
        }

        // POST: TeamDynamixIntegrations/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "TeamDynamixIntegration_Id,IntegrationName,Description,OwnerObjectGuid,FormID")] TeamDynamixIntegration teamDynamixIntegration)
        {
            if (ModelState.IsValid)
            {
                db.TeamDynamixIntegrations.Add(teamDynamixIntegration);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.FormID = new SelectList(db.TeamDynamixForms, "TeamDynamixForm_Id", "FormName", teamDynamixIntegration.FormID);
            return View(teamDynamixIntegration);
        }

        // GET: TeamDynamixIntegrations/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            TeamDynamixIntegration teamDynamixIntegration = db.TeamDynamixIntegrations.Find(id);
            if (teamDynamixIntegration == null)
            {
                return HttpNotFound();
            }
            ViewBag.FormID = new SelectList(db.TeamDynamixForms, "TeamDynamixForm_Id", "FormName", teamDynamixIntegration.FormID);
            return View(teamDynamixIntegration);
        }

        // POST: TeamDynamixIntegrations/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "TeamDynamixIntegration_Id,IntegrationName,Description,OwnerObjectGuid,FormID")] TeamDynamixIntegration teamDynamixIntegration)
        {
            if (ModelState.IsValid)
            {
                db.Entry(teamDynamixIntegration).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.FormID = new SelectList(db.TeamDynamixForms, "TeamDynamixForm_Id", "FormName", teamDynamixIntegration.FormID);
            return View(teamDynamixIntegration);
        }

        // GET: TeamDynamixIntegrations/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            TeamDynamixIntegration teamDynamixIntegration = db.TeamDynamixIntegrations.Find(id);
            if (teamDynamixIntegration == null)
            {
                return HttpNotFound();
            }
            return View(teamDynamixIntegration);
        }

        // POST: TeamDynamixIntegrations/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            TeamDynamixIntegration teamDynamixIntegration = db.TeamDynamixIntegrations.Find(id);
            db.TeamDynamixIntegrations.Remove(teamDynamixIntegration);
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
