using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TDXManager;
using TeamDynamix.Api.Forms;
using TeamDynamix.Api.Users;
using TeamDynamix.Api.Tickets;


namespace TeamDynamixTestSuite
{
    class TeamDynamixTestSuite
    {
        static void Main(string[] args)
        {
            TDXTicketManager tDXTicketManager = new TDXTicketManager();
            Form form = tDXTicketManager.TDXTicketForms.Where(f => f.Name.Equals("Google Workspace Account Reinstatement Grace Period")).FirstOrDefault();
            User user = tDXTicketManager.GetTDXUserByUserPrincipalName("erd53@cornell.edu");
            tDXTicketManager.GetAllRequestorTicketsByForm(new Guid[] { user.UID }, new Int32[] { form.ID });
            List<Ticket> PreviouslyCompletedRequests = (from ticket in tDXTicketManager.AllRequestorTickets
                     from attribute in ticket.Attributes
                     where attribute.Name.Equals("S111-AUTOMATIONSTATUS") && attribute.ValueText.Equals("COMPLETE")
                     select ticket).ToList();

        }
    }
}
