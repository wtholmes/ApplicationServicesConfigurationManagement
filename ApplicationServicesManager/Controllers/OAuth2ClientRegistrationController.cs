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
        private OAuth2AuthenticationContext db = new OAuth2AuthenticationContext();

        // GET: OAuth2ClientRegistration
        public ActionResult Index()
        {
            return View(db.OAuth2ClientRegistrationViewModel.ToList());
        }

        // GET: OAuth2ClientRegistration/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            OAuth2ClientRegistrationViewModel oAuth2ClientRegistrationViewModel = db.OAuth2ClientRegistrationViewModel.Find(id);
            if (oAuth2ClientRegistrationViewModel == null)
            {
                return HttpNotFound();
            }
            return View(oAuth2ClientRegistrationViewModel);
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

            var ClientRoles = from r in db.OAuth2ClientRoles
                              select new
                              {
                                  r.OAuth2ClientRoleID,
                                  r.RoleName
                              };

            var MyCheckBoxList = new List<CheckBoxViewModel>();
            foreach(var clientRole in ClientRoles)
            {
                MyCheckBoxList.Add(
                    new CheckBoxViewModel
                    {
                        Id = clientRole.OAuth2ClientRoleID,
                        OAuth2Role = clientRole.RoleName,
                        IsChecked = false
                    }
                );
            }

            OAuth2ClientRegistrationViewModel newOAuth2ClientRegistrationViewModel = new OAuth2ClientRegistrationViewModel()
            {
                RequestingUPN = this.User.Identity.Name,
                RequestTime = dateTime,
                ExpirationTime = dateTime.AddDays(ClinetSecretLifetime),
                ClientID = ClientId,
                ClientSecret = ClientSecret,
                OAuth2ClientRoles = MyCheckBoxList
                
            };
            return View(newOAuth2ClientRegistrationViewModel);
        }

        // POST: OAuth2ClientRegistration/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(OAuth2ClientRegistrationViewModel oAuth2ClientRegistrationViewModel)
        {
            if (ModelState.IsValid)
            {

                OAuth2ClientRegistration oAuth2ClientRegistration = new OAuth2ClientRegistration()
                {
                    ClientID = oAuth2ClientRegistrationViewModel.ClientID,
                    ClientSecret = oAuth2ClientRegistrationViewModel.ClientSecret,
                    ClientDescription = oAuth2ClientRegistrationViewModel.ClientDescription,
                    RequestingUPN = oAuth2ClientRegistrationViewModel.RequestingUPN,
                    RequestTime = oAuth2ClientRegistrationViewModel.RequestTime,
                    ExpirationTime = oAuth2ClientRegistrationViewModel.ExpirationTime
                };
                db.OAuth2ClientRegistrations.Add(oAuth2ClientRegistration);
                db.SaveChanges();

                if(oAuth2ClientRegistration.OAuth2ClientRegistrationID != 0)
                {
                    List<OAuth2ClientRoleToOAuth2ClientRegistration> oAuth2ClientRoleToOAuth2ClientRegistrations = new List<OAuth2ClientRoleToOAuth2ClientRegistration>();

                    foreach(CheckBoxViewModel checkBoxViewModel in oAuth2ClientRegistrationViewModel.OAuth2ClientRoles)
                    {
                        if(checkBoxViewModel.IsChecked)
                        {
                            OAuth2ClientRoleToOAuth2ClientRegistration auth2ClientRoleToOAuth2ClientRegistration = new OAuth2ClientRoleToOAuth2ClientRegistration()
                            {
                                OAuth2ClientRegistrationID = oAuth2ClientRegistration.OAuth2ClientRegistrationID,
                                OAuth2ClientRoleID = checkBoxViewModel.Id
                            };
                            oAuth2ClientRoleToOAuth2ClientRegistrations.Add(auth2ClientRoleToOAuth2ClientRegistration);
                        }
                    }
                    oAuth2ClientRegistration.OAuth2ClientRoleToOAuth2ClientRegistrations = oAuth2ClientRoleToOAuth2ClientRegistrations;
                    db.SaveChanges();
                }
                return RedirectToAction("Index");
            }

            return View(oAuth2ClientRegistrationViewModel);
        }

        // GET: OAuth2ClientRegistration/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            OAuth2ClientRegistrationViewModel oAuth2ClientRegistrationViewModel = db.OAuth2ClientRegistrationViewModel.Find(id);
            if (oAuth2ClientRegistrationViewModel == null)
            {
                return HttpNotFound();
            }
            return View(oAuth2ClientRegistrationViewModel);
        }

        // POST: OAuth2ClientRegistration/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "ID,OAuth2ClientRegistrationID,ClientID,ClientSecret,ClientDescription,RequestingUPN,RequestTime,ExpirationTime")] OAuth2ClientRegistrationViewModel oAuth2ClientRegistrationViewModel)
        {
            if (ModelState.IsValid)
            {
                db.Entry(oAuth2ClientRegistrationViewModel).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(oAuth2ClientRegistrationViewModel);
        }

        // GET: OAuth2ClientRegistration/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            OAuth2ClientRegistrationViewModel oAuth2ClientRegistrationViewModel = db.OAuth2ClientRegistrationViewModel.Find(id);
            if (oAuth2ClientRegistrationViewModel == null)
            {
                return HttpNotFound();
            }
            return View(oAuth2ClientRegistrationViewModel);
        }

        // POST: OAuth2ClientRegistration/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            OAuth2ClientRegistrationViewModel oAuth2ClientRegistrationViewModel = db.OAuth2ClientRegistrationViewModel.Find(id);
            db.OAuth2ClientRegistrationViewModel.Remove(oAuth2ClientRegistrationViewModel);
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
