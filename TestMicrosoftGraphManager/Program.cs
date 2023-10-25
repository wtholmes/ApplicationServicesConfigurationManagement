using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MicrosoftAzureManager;

namespace TestMicrosoftGraphManager
{
    class Program
    {
        static void Main(string[] args)
        {
            MicrosoftGraphManager microsoftGraphManager = new MicrosoftGraphManager();
            //var z = microsoftGraphManager.GetGroupMembers("1590ba97-d5c7-4b76-bef4-39d2c75f51ea");
            var x = microsoftGraphManager.GetUser("ch18@cornell.edu");
            microsoftGraphManager.AddGroupMember("90f484a5-6029-4460-902c-4f6c89f39dd8", x);
        }
    }
}
