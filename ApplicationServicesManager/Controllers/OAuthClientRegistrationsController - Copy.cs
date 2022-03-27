using AuthenticationServices;
using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Web.Mvc;

namespace ApplicationServicesManager.Controllers
{
public class OAuthClientRegistrationsController : Controller
    {
        private WebAPIAuthorization db = new WebAPIAuthorization();

        // GET: OAuthClientRegistrations
        [Authorize]
        public ActionResult Index()
        {
            return View(db.OAuth2ClientRegistrations.ToList());
        }

        // GET: OAuthClientRegistrations/Details/5
        [Authorize]
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            OAuth2ClientRegistration oAuthClientRegistration = db.OAuth2ClientRegistrations.Find(id);
            if (oAuthClientRegistration == null)
            {
                return HttpNotFound();
            }
            return View(oAuthClientRegistration);
        }

        // GET: OAuthClientRegistrations/Create
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

        // POST: OAuthClientRegistrations/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "RequestID,RequestingUPN,Description,RequestTime,ExpirationTime,ClientId,ClientSecret")] OAuth2ClientRegistration oAuthClientRegistration)
        {
            if (ModelState.IsValid)
            {
                db.OAuth2ClientRegistrations.Add(oAuthClientRegistration);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(oAuthClientRegistration);
        }

        // GET: OAuthClientRegistrations/Edit/5
        [Authorize]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            OAuth2ClientRegistration oAuthClientRegistration = db.OAuth2ClientRegistrations.Find(id);
            if (oAuthClientRegistration == null)
            {
                return HttpNotFound();
            }
            return View(oAuthClientRegistration);
        }

        // POST: OAuthClientRegistrations/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public ActionResult Edit([Bind(Include = "RequestID,RequestingUPN,Description,RequestTime,ExpirationTime,ClientId,ClientSecret")] OAuth2ClientRegistration oAuthClientRegistration)
        {
            if (ModelState.IsValid)
            {
                db.Entry(oAuthClientRegistration).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(oAuthClientRegistration);
        }

        // GET: OAuthClientRegistrations/Delete/5
        [Authorize]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            OAuth2ClientRegistration oAuthClientRegistration = db.OAuth2ClientRegistrations.Find(id);
            if (oAuthClientRegistration == null)
            {
                return HttpNotFound();
            }
            return View(oAuthClientRegistration);
        }

        // POST: OAuthClientRegistrations/Delete/5
        [Authorize]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            OAuth2ClientRegistration oAuthClientRegistration = db.OAuth2ClientRegistrations.Find(id);
            db.OAuth2ClientRegistrations.Remove(oAuthClientRegistration);
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