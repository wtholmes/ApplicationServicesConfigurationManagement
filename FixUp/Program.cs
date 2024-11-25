using ActiveDirectoryAccess;
using CornellIdentityManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FixUp
{
    class Program
    {
        static void Main(string[] args)
        {
            ProvAccountsManager provAccountsManager = new ProvAccountsManager();
      





            ActiveDirectoryContext activeDirectoryContext = new ActiveDirectoryContext();
            String directoryFilter = "(&(cornelleduProvAccts=office365-a3)(!(cornelleduentitlements=office365-a3)))";
            activeDirectoryContext.SearchDirectory(directoryFilter, true, 50);
            foreach (ActiveDirectoryEntity activeDirectoryEntity in activeDirectoryContext.ActiveDirectoryEntities)
            {
                Console.WriteLine("UserPrincipalName: {0}", activeDirectoryEntity.userprincipalName);
                provAccountsManager.DisableFacultyA3(activeDirectoryEntity.userprincipalName);
            }

        }
    }
}
