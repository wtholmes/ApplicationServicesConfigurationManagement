using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CornellIdentityManagement;
namespace ComponentTesting
{
    class Program
    {
        static void Main(string[] args)
        {
            ProvAccountsManager provAccountsManager = new ProvAccountsManager();
            provAccountsManager.UseTest();
            provAccountsManager.GetProvAccounts("wth1@cornell.edu");


        }
    }
}
