using ApplicationServicesConfigurationManagementDatabaseAccess;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Web.Mvc;

namespace TeamDynamixManagement.Controllers
{
    public class TicketStatusChangeMessagesController : Controller
    {
        private TeamDynamixManagementContext db = new TeamDynamixManagementContext();

        // GET: TicketStatusChangeMessages
        public ActionResult Index(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ViewBag.IntegrationID = id;
            var ticketStatusChangeMessages = db.TicketStatusChangeMessages
                .Where(t => t.IntegrationID == id)
                .Include(t => t.CurrentTeamDynamixStatusClass)
                .Include(t => t.TeamDynamixIntegration).Include(t => t.UpdatedTeamDynamixStatusClass);
            return View(ticketStatusChangeMessages.ToList());
        }

        // GET: TicketStatusChangeMessages/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            TicketStatusChangeMessage ticketStatusChangeMessage = db.TicketStatusChangeMessages.Find(id);
            if (ticketStatusChangeMessage == null)
            {
                return HttpNotFound();
            }
            return View(ticketStatusChangeMessage);
        }

        // GET: TicketStatusChangeMessages/Create
        public ActionResult Create(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            TeamDynamixIntegration teamDynamixIntegration = db.TeamDynamixIntegrations
                        .Where(i => i.TeamDynamixIntegration_Id == id)
                        .FirstOrDefault();

            if (teamDynamixIntegration == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            ViewBag.IntegrationName = teamDynamixIntegration.IntegrationName;

            TicketStatusChangeMessage ticketStatusChangeMessage = new TicketStatusChangeMessage()
            {
                IntegrationID = Convert.ToInt32(id)
            };

            ViewBag.CurrentStatusID = new SelectList(db.TeamDynamixStatusClasses, "TeamDynamixStatusClass_Id", "TicketStatusName");
            ViewBag.IntegrationID = new SelectList(db.TeamDynamixIntegrations, "TeamDynamixIntegration_Id", "IntegrationName");
            ViewBag.UpdatedStatusID = new SelectList(db.TeamDynamixStatusClasses, "TeamDynamixStatusClass_Id", "TicketStatusName");

            List<SelectListItem> ObjList = new List<SelectListItem>();
            ObjList.Add(new SelectListItem() { Text = "(Automation): Message", Value = String.Format("[%%-AutomationMessage-%%]") });
            ObjList.Add(new SelectListItem() { Text = "(Automation): Status", Value = String.Format("[%%-AutomationStatus-%%]") });
            ObjList.Add(new SelectListItem() { Text = "(Automation): Error", Value = String.Format("[%%-AutomationError-%%]") });

            List<PropertyInfo> propertyInfos = typeof(TeamDynamix.Api.Tickets.Ticket).GetProperties().ToList();
            foreach (PropertyInfo propertyInfo in propertyInfos.OrderBy(p => p.Name))
            {
                SelectListItem selectListItem = new SelectListItem()
                {
                    Text = String.Format("(TDX Default): {0}", propertyInfo.Name),
                    Value = String.Format("[%%-{0}-%%]", propertyInfo.Name)
                };
                ObjList.Add(selectListItem);
            }

            foreach (TeamDynamixCustomAttribute teamDynamixCustomAttribute in db.TeamDynamixCustomAttributes.OrderBy(a => a.AtributeName))
            {
                SelectListItem selectListItem = new SelectListItem()
                {
                    Text = String.Format("(TDX Custom): {0}", teamDynamixCustomAttribute.AtributeName),
                    Value = String.Format("[%%-{0}-%%]", teamDynamixCustomAttribute.AtributeName)
                };
                ObjList.Add(selectListItem);
            }

            //Assigning generic list to ViewBag
            ViewBag.MessageToken = ObjList ;
            return View(ticketStatusChangeMessage);
        }

        // POST: TicketStatusChangeMessages/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "TicketStatusChangeMessage_Id,IntegrationID,CurrentStatusID,UpdatedStatusID,Message")] TicketStatusChangeMessage ticketStatusChangeMessage)
        {
            if (ModelState.IsValid)
            {
                db.TicketStatusChangeMessages.Add(ticketStatusChangeMessage);
                db.SaveChanges();
                return RedirectToAction("Index", new { id = ticketStatusChangeMessage.IntegrationID });
            }
            ViewBag.CurrentStatusID = new SelectList(db.TeamDynamixStatusClasses, "TeamDynamixStatusClass_Id", "TicketStatusName", ticketStatusChangeMessage.CurrentStatusID);
            ViewBag.UpdatedStatusID = new SelectList(db.TeamDynamixStatusClasses, "TeamDynamixStatusClass_Id", "TicketStatusName", ticketStatusChangeMessage.UpdatedStatusID);

            return View(ticketStatusChangeMessage);
        }

        // GET: TicketStatusChangeMessages/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            TicketStatusChangeMessage ticketStatusChangeMessage = db.TicketStatusChangeMessages
                .Include(t => t.TeamDynamixIntegration)
                .Where(t => t.TicketStatusChangeMessage_Id == id)
                .FirstOrDefault();

            if (ticketStatusChangeMessage == null)
            {
                return HttpNotFound();
            }

            ViewBag.IntegrationName = ticketStatusChangeMessage.TeamDynamixIntegration.IntegrationName;
            ViewBag.CurrentStatusID = new SelectList(db.TeamDynamixStatusClasses, "TeamDynamixStatusClass_Id", "TicketStatusName", ticketStatusChangeMessage.CurrentStatusID);
            ViewBag.IntegrationID = new SelectList(db.TeamDynamixIntegrations, "TeamDynamixIntegration_Id", "IntegrationName", ticketStatusChangeMessage.IntegrationID);
            ViewBag.UpdatedStatusID = new SelectList(db.TeamDynamixStatusClasses, "TeamDynamixStatusClass_Id", "TicketStatusName", ticketStatusChangeMessage.UpdatedStatusID);

            List<SelectListItem> ObjList = new List<SelectListItem>();
            ObjList.Add(new SelectListItem() { Text = "(Automation): Message", Value = String.Format("[%%-AutomationMessage-%%]") });
            ObjList.Add(new SelectListItem() { Text = "(Automation): Status)", Value = String.Format("[%%-AutomationStatus-%%]") });
            ObjList.Add(new SelectListItem() { Text = "(Automation): Error)", Value = String.Format("[%%-AutomationError-%%]") });

            List<PropertyInfo> propertyInfos = typeof(TeamDynamix.Api.Tickets.Ticket).GetProperties().ToList();
            foreach (PropertyInfo propertyInfo in propertyInfos.OrderBy(p => p.Name))
            {
                SelectListItem selectListItem = new SelectListItem()
                {
                    Text = String.Format("(TDX Default): {0}", propertyInfo.Name),
                    Value = String.Format("[%%-{0}-%%]", propertyInfo.Name)
                };
                ObjList.Add(selectListItem);
            }

            foreach (TeamDynamixCustomAttribute teamDynamixCustomAttribute in db.TeamDynamixCustomAttributes.OrderBy(a => a.AtributeName))
            {
                SelectListItem selectListItem = new SelectListItem()
                {
                    Text = String.Format("(TDX Custom): {0}", teamDynamixCustomAttribute.AtributeName),
                    Value = String.Format("[%%-{0}-%%]", teamDynamixCustomAttribute.AtributeName)
                };
                ObjList.Add(selectListItem);
            }

            //Assigning generic list to ViewBag
            ViewBag.MessageToken = ObjList;

            return View(ticketStatusChangeMessage);
        }

        // POST: TicketStatusChangeMessages/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "TicketStatusChangeMessage_Id,IntegrationID,CurrentStatusID,UpdatedStatusID,Message")] TicketStatusChangeMessage ticketStatusChangeMessage)
        {
            if (ModelState.IsValid)
            {
                db.Entry(ticketStatusChangeMessage).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index", new { id = ticketStatusChangeMessage.IntegrationID });
            }
            ViewBag.CurrentStatusID = new SelectList(db.TeamDynamixStatusClasses, "TeamDynamixStatusClass_Id", "TicketStatusName", ticketStatusChangeMessage.CurrentStatusID);
            ViewBag.IntegrationID = new SelectList(db.TeamDynamixIntegrations, "TeamDynamixIntegration_Id", "IntegrationName", ticketStatusChangeMessage.IntegrationID);
            ViewBag.UpdatedStatusID = new SelectList(db.TeamDynamixStatusClasses, "TeamDynamixStatusClass_Id", "TicketStatusName", ticketStatusChangeMessage.UpdatedStatusID);
            return View(ticketStatusChangeMessage);
        }

        // GET: TicketStatusChangeMessages/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            TicketStatusChangeMessage ticketStatusChangeMessage = db.TicketStatusChangeMessages.Find(id);
            if (ticketStatusChangeMessage == null)
            {
                return HttpNotFound();
            }
            return View(ticketStatusChangeMessage);
        }

        // POST: TicketStatusChangeMessages/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            TicketStatusChangeMessage ticketStatusChangeMessage = db.TicketStatusChangeMessages.Find(id);
            db.TicketStatusChangeMessages.Remove(ticketStatusChangeMessage);
            db.SaveChanges();
            return RedirectToAction("Index", new { id = ticketStatusChangeMessage.IntegrationID });
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