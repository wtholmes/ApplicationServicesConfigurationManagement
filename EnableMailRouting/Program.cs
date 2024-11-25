using CornellIdentityManagement;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnableMailRouting
{
    class Program
    {
        static void Main(string[] args)
        {
            ProvAccountsManager provAccountsManager = new ProvAccountsManager();
            provAccountsManager.EnableMailRouting("clm93@cornell.edu");
        }
    }
}
