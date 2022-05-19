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
    [Authorize(Roles = "ApplicationServicesManagerAdminRole")]
    public class OAuth2ClientRegistrationController : Controller
    {
        private OAuth2AuthenticationContext db = new OAuth2AuthenticationContext();

        // GET: OAuth2ClientRegistration
        [Authorize(Roles = "ApplicationServicesManagerAdminRole")]
        public ActionResult Index()
        {
            List<OAuth2ClientRegistrationViewModel> oAuth2ClientRegistrations = new List<OAuth2ClientRegistrationViewModel>();   
            foreach(OAuth2ClientRegistration oAuth2ClientRegistration in db.OAuth2ClientRegistrations)
            {
                OAuth2ClientRegistrationViewModel oAuth2ClientRegistrationViewModel = new OAuth2ClientRegistrationViewModel()
                {
                    ID = oAuth2ClientRegistration.OAuth2ClientRegistrationID,
                    OAuth2ClientRegistrationID = oAuth2ClientRegistration.OAuth2ClientRegistrationID,
                    ClientID = oAuth2ClientRegistration.ClientID,
                    ClientDescription = oAuth2ClientRegistration.ClientDescription,
                    RequestingUPN = oAuth2ClientRegistration.RequestingUPN,
                    RequestTime = oAuth2ClientRegistration.RequestTime,
                    ExpirationTime = oAuth2ClientRegistration.ExpirationTime
                };
                oAuth2ClientRegistrations.Add(oAuth2ClientRegistrationViewModel);
            }
            
            return View(oAuth2ClientRegistrations);
        }

        // GET: OAuth2ClientRegistration/Details/5
        [Authorize(Roles = "ApplicationServicesManagerAdminRole")] // This method requires authorization.
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

            OAuth2ClientRegistrationViewModel oAuth2ClientRegistrationViewModel = new OAuth2ClientRegistrationViewModel()
            {
                OAuth2ClientRegistrationID =oAuth2ClientRegistration.OAuth2ClientRegistrationID,
                ClientID=oAuth2ClientRegistration.ClientID,
                ClientDescription=oAuth2ClientRegistration.ClientDescription,
                RequestingUPN=oAuth2ClientRegistration.RequestingUPN,
                RequestTime=oAuth2ClientRegistration.RequestTime,
                ExpirationTime=oAuth2ClientRegistration.ExpirationTime
            };

            if(this.User.Identity.Name.Equals(oAuth2ClientRegistration.RequestingUPN))
            {
                oAuth2ClientRegistrationViewModel.ClientSecret = oAuth2ClientRegistration.ClientSecret;
            }
            else
            {
                oAuth2ClientRegistrationViewModel.ClientSecret = String.Format("{0}.....", oAuth2ClientRegistration.ClientSecret.Substring(0, 6));
            }
                
            return View(oAuth2ClientRegistrationViewModel);
        }

        // GET: OAuth2ClientRegistration/Create
        [Authorize(Roles = "ApplicationServicesManagerAdminCreateRegistrationRole")] // This method requires authorization.
        public ActionResult Create()
        {
            ViewBag.ExpandClientRoles = true;
            Int32 ClinetSecretLifetime = 365;
            DateTime dateTime = DateTime.Now;
            Guid ClientId = Guid.NewGuid();

            RNGCryptoServiceProvider RandomDataGenerator = new RNGCryptoServiceProvider();
            byte[] buffer = new byte[64];
            RandomDataGenerator.GetBytes(buffer);
            String ClientSecret = Convert.ToBase64String(buffer);

            var ClientRoles = from r in db.OAuth2ClientRoles.OrderBy(r => r.RoleName)
                              select new
                              {
                                  r.OAuth2ClientRoleID,
                                  r.RoleName,
                                  r.RoleDescription
                              };

            var MyCheckBoxList = new List<CheckBoxViewModel>();
            foreach(var clientRole in ClientRoles)
            {
                MyCheckBoxList.Add(
                    new CheckBoxViewModel
                    {
                        Id = clientRole.OAuth2ClientRoleID,
                        OAuth2Role = clientRole.RoleName,
                        OAuth2RoleDescription = clientRole.RoleDescription,
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
        [Authorize(Roles = "ApplicationServicesManagerAdminCreateRegistrationRole")]  // This method requires authorization.
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
        [Authorize(Roles = "ApplicationServicesManagerAdminCreateRegistrationRole")]  // This method requires authorization.
        // GET: OAuth2ClientRegistration/Edit/5

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

            var ClientRoles = from r in db.OAuth2ClientRoles.OrderBy(r => r.RoleName)
                              select new
                              {
                                  r.OAuth2ClientRoleID,
                                  r.RoleDescription,
                                  r.RoleName
                              };

            var MyCheckBoxList = new List<CheckBoxViewModel>();
            foreach (var clientRole in ClientRoles)
            {
                MyCheckBoxList.Add(
                    new CheckBoxViewModel
                    {
                        Id = clientRole.OAuth2ClientRoleID,
                        OAuth2Role = clientRole.RoleName,
                        OAuth2RoleDescription = clientRole.RoleDescription,
                        IsChecked = ((from ab in db.OAuth2ClientRoleToOAuth2ClientRegistrations
                                      where (ab.OAuth2ClientRegistrationID == id) 
                                      && (ab.OAuth2ClientRoleID == clientRole.OAuth2ClientRoleID)
                                      select ab).Count() > 0)
                    }
                );
            }

            OAuth2ClientRegistrationViewModel oAuth2ClientRegistrationViewModel = new OAuth2ClientRegistrationViewModel()
            {
                OAuth2ClientRegistrationID = oAuth2ClientRegistration.OAuth2ClientRegistrationID,
                ClientID = oAuth2ClientRegistration.ClientID,
                ClientSecret = oAuth2ClientRegistration.ClientSecret,
                ClientDescription = oAuth2ClientRegistration.ClientDescription,
                RequestingUPN = oAuth2ClientRegistration.RequestingUPN,
                RequestTime = oAuth2ClientRegistration.RequestTime,
                ExpirationTime = oAuth2ClientRegistration.ExpirationTime,
                OAuth2ClientRoles = MyCheckBoxList
            };

            if (this.User.Identity.Name.Equals(oAuth2ClientRegistration.RequestingUPN))
            {
                oAuth2ClientRegistrationViewModel.ClientSecret = oAuth2ClientRegistration.ClientSecret;
            }
            else
            {
                oAuth2ClientRegistrationViewModel.ClientSecret = String.Format("{0}.....", oAuth2ClientRegistration.ClientSecret.Substring(0, 6));
            }
            return View(oAuth2ClientRegistrationViewModel);
        }

        // POST: OAuth2ClientRegistration/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "ApplicationServicesManagerAdminCreateRegistrationRole")]  // This method requires authorization.
        public ActionResult Edit([Bind(Include = "ID,OAuth2ClientRegistrationID,ClientID,ClientSecret,ClientDescription,RequestingUPN,RequestTime,ExpirationTime,OAuth2ClientRoles")] OAuth2ClientRegistrationViewModel oAuth2ClientRegistrationViewModel)
        {
            if (ModelState.IsValid)
            {
                //db.Entry(oAuth2ClientRegistrationViewModel).State = EntityState.Modified;
                //db.SaveChanges();
                //return RedirectToAction("Index");
            }

            OAuth2ClientRegistration oAuth2ClientRegistration = db.OAuth2ClientRegistrations.Find(oAuth2ClientRegistrationViewModel.ID);
            if (oAuth2ClientRegistration == null)
            {
                return HttpNotFound();
            }

            // Update the client description if it has been changed.
            oAuth2ClientRegistration.ClientDescription = oAuth2ClientRegistrationViewModel.ClientDescription;


            foreach (CheckBoxViewModel checkBoxViewModel in oAuth2ClientRegistrationViewModel.OAuth2ClientRoles)
            {
                OAuth2ClientRoleToOAuth2ClientRegistration oAuth2ClientRoleToOAuth2ClientRegistration = db.OAuth2ClientRoleToOAuth2ClientRegistrations
                    .Where(r => r.OAuth2ClientRoleID.Equals(checkBoxViewModel.Id) && r.OAuth2ClientRegistrationID.Equals(oAuth2ClientRegistrationViewModel.ID))
                    .FirstOrDefault();

                if (checkBoxViewModel.IsChecked)
                {
                    if (oAuth2ClientRoleToOAuth2ClientRegistration == null)
                    {
                        OAuth2ClientRoleToOAuth2ClientRegistration newOAuth2ClientRoleToOAuth2ClientRegistration = new OAuth2ClientRoleToOAuth2ClientRegistration()
                        {
                            OAuth2ClientRegistrationID = oAuth2ClientRegistration.OAuth2ClientRegistrationID,
                            OAuth2ClientRoleID = checkBoxViewModel.Id
                        };
                        db.OAuth2ClientRoleToOAuth2ClientRegistrations.Add(newOAuth2ClientRoleToOAuth2ClientRegistration);
                    }
                }
                else
                {
                    if (oAuth2ClientRoleToOAuth2ClientRegistration != null)
                    {
                        db.OAuth2ClientRoleToOAuth2ClientRegistrations.Remove(oAuth2ClientRoleToOAuth2ClientRegistration);
                    }

                }
            }
            db.SaveChanges();
            return View(oAuth2ClientRegistrationViewModel);
        }

        // GET: OAuth2ClientRegistration/Delete/5
        [Authorize(Roles = "ApplicationServicesManagerAdminCreateRegistrationRole")]  // This method requires authorization.
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

            OAuth2ClientRegistrationViewModel oAuth2ClientRegistrationViewModel = new OAuth2ClientRegistrationViewModel()
            {
                OAuth2ClientRegistrationID = oAuth2ClientRegistration.OAuth2ClientRegistrationID,
                ClientID = oAuth2ClientRegistration.ClientID,
                ClientDescription = oAuth2ClientRegistration.ClientDescription,
                RequestingUPN = oAuth2ClientRegistration.RequestingUPN,
                RequestTime = oAuth2ClientRegistration.RequestTime,
                ExpirationTime = oAuth2ClientRegistration.ExpirationTime
            };

            if (this.User.Identity.Name.Equals(oAuth2ClientRegistrationViewModel.RequestingUPN))
            {
                return View(oAuth2ClientRegistrationViewModel);
            }
            else
            {
                return RedirectToAction("Index");
            }
        }

        // POST: OAuth2ClientRegistration/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "ApplicationServicesManagerAdminCreateRegistrationRole")]  // This method requires authorization.
        public ActionResult DeleteConfirmed(int id)
        {
            OAuth2ClientRegistration oAuth2ClientRegistration = db.OAuth2ClientRegistrations.Find(id);
            if(oAuth2ClientRegistration == null)
            {
                return HttpNotFound();
            }

            if (this.User.Identity.Name.Equals(oAuth2ClientRegistration.RequestingUPN))
            {
                db.OAuth2ClientRegistrations.Remove(oAuth2ClientRegistration);
                db.SaveChanges();
            }

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
