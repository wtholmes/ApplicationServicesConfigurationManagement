using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Web;
using System.Web.Mvc;
using AuthenticationServices;

namespace ApplicationServicesManager.Controllers
{
    public class OAuth2ClientRegistrationController : Controller
    {
        private WebAPIAuthorizations db = new WebAPIAuthorizations();

        // GET: OAuth2ClientRegistration
        [Authorize]
        public ActionResult Index()
        {
            return View(db.OAuth2ClientRegistrations.ToList());
        }

        // GET: OAuth2ClientRegistration/Details/5
        [Authorize]
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            OAuth2ClientRegistration oAuth2ClientRegistration = db.OAuth2ClientRegistrations.Find(id);
            if (oAuth2ClientRegistration == null)
            {
                return HttpNotFound();
            }
            return View(oAuth2ClientRegistration);
        }

        // GET: OAuth2ClientRegistration/Create
        [Authorize]
        public ActionResult Create()
        {
            Int32 ClinetSecretLifetime = 365;
            DateTime dateTime = DateTime.Now;
            Guid ClientId = Guid.NewGuid();

            RNGCryptoServiceProvider RandomDataGenerator = new RNGCryptoServiceProvider();
            byte[] buffer = new byte[64];
            RandomDataGenerator.GetBytes(buffer);
            String ClientSecret = Convert.ToBase64String(buffer);

            OAuth2ClientRegistration oAuthClientRegistration = new OAuth2ClientRegistration
            {
                RequestingUPN = this.User.Identity.Name,
                RequestTime = dateTime,
                ExpirationTime = dateTime.AddDays(ClinetSecretLifetime),
                ClientId = ClientId,
                ClientSecret = ClientSecret
            };
            return View(oAuthClientRegistration);
        }

        // POST: OAuth2ClientRegistration/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "OAuth2ClientRegistration_Id,RequestingUPN,Description,RequestTime,ExpirationTime,ClientId,ClientSecret")] OAuth2ClientRegistration oAuth2ClientRegistration)
        {
            if (ModelState.IsValid)
            {
                db.OAuth2ClientRegistrations.Add(oAuth2ClientRegistration);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(oAuth2ClientRegistration);
        }

        // GET: OAuth2ClientRegistration/Edit/5
        [Authorize]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            OAuth2ClientRegistration oAuth2ClientRegistration = db.OAuth2ClientRegistrations.Find(id);
            if (oAuth2ClientRegistration == null)
            {
                return HttpNotFound();
            }
            return View(oAuth2ClientRegistration);
        }

        // POST: OAuth2ClientRegistration/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "OAuth2ClientRegistration_Id,RequestingUPN,Description,RequestTime,ExpirationTime,ClientId,ClientSecret")] OAuth2ClientRegistration oAuth2ClientRegistration)
        {
            if (ModelState.IsValid)
            {
                db.Entry(oAuth2ClientRegistration).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(oAuth2ClientRegistration);
        }

        // GET: OAuth2ClientRegistration/Delete/5
        [Authorize]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            OAuth2ClientRegistration oAuth2ClientRegistration = db.OAuth2ClientRegistrations.Find(id);
            if (oAuth2ClientRegistration == null)
            {
                return HttpNotFound();
            }
            return View(oAuth2ClientRegistration);
        }

        // POST: OAuth2ClientRegistration/Delete/5
        [Authorize]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            OAuth2ClientRegistration oAuth2ClientRegistration = db.OAuth2ClientRegistrations.Find(id);
            db.OAuth2ClientRegistrations.Remove(oAuth2ClientRegistration);
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
