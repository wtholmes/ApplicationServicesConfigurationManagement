using AuthenticationServices;
using System;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace ApplicationServicesManager.Controllers
{
    public class OAuth2ClientRoleController : Controller
    {
        private OAuth2AuthenticationContext db = new OAuth2AuthenticationContext();

        // GET: OAuth2ClientRole
        [Authorize]
        public ActionResult Index()
        {
            return View(db.OAuth2ClientRoles
                .OrderBy(r => r.RoleName)
                .ToList());
        }

        // GET: OAuth2ClientRole/Details/5
        [Authorize]
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            OAuth2ClientRole oAuth2ClientRole = db.OAuth2ClientRoles.Find(id);
            if (oAuth2ClientRole == null)
            {
                return HttpNotFound();
            }
            return View(oAuth2ClientRole);
        }

        // GET: OAuth2ClientRole/Create
        [Authorize]
        public ActionResult Create()
        {
            return View();
        }

        // POST: OAuth2ClientRole/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "OAuth2ClientRoleID,RoleName,RoleDescription,WhenCreated")] OAuth2ClientRole oAuth2ClientRole)
        {
            if (ModelState.IsValid)
            {
                db.OAuth2ClientRoles.Add(oAuth2ClientRole);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(oAuth2ClientRole);
        }

        // GET: OAuth2ClientRole/Edit/5
        [Authorize]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            OAuth2ClientRole oAuth2ClientRole = db.OAuth2ClientRoles.Find(id);
            if (oAuth2ClientRole == null)
            {
                return HttpNotFound();
            }
            return View(oAuth2ClientRole);
        }

        // POST: OAuth2ClientRole/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "OAuth2ClientRoleID,RoleName,RoleDescription,WhenCreated")] OAuth2ClientRole oAuth2ClientRole)
        {
            if (ModelState.IsValid)
            {
                db.Entry(oAuth2ClientRole).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(oAuth2ClientRole);
        }

        // GET: OAuth2ClientRole/Delete/5
        [Authorize]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            OAuth2ClientRole oAuth2ClientRole = db.OAuth2ClientRoles.Find(id);
            if (oAuth2ClientRole == null)
            {
                return HttpNotFound();
            }

            // Role Deletion is currently disabled.
            Boolean DisableDelete = true;
            if (DisableDelete)
            {
                return RedirectToAction("Index");
            }
            else
            {
                return View(oAuth2ClientRole);
            }
        }

        // POST: OAuth2ClientRole/Delete/5
        [Authorize]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            // Role deletion is currently disabled.
            Boolean DisableDelete = true;
            if (DisableDelete)
            {
                return RedirectToAction("Index");
            }
            else
            {
                OAuth2ClientRole oAuth2ClientRole = db.OAuth2ClientRoles.Find(id);
                if (oAuth2ClientRole == null)
                {
                    return HttpNotFound();
                }

                db.OAuth2ClientRoles.Remove(oAuth2ClientRole);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
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