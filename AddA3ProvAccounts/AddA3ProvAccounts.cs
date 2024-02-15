using CornellIdentityManagement;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AddA3ProvAccounts
{
    class AddA3ProvAccounts
    {
        static void Main(string[] args)
        {
            ProvAccountsManager provAccountsManager = new ProvAccountsManager();

            using (var fileStream = File.OpenRead("EnableA3Users.txt"))
            {
                using (var streamReader = new StreamReader(fileStream))
                {
                    String UserPrincipalName;
                    while ((UserPrincipalName = streamReader.ReadLine()) != null)
                    {
                        Console.WriteLine("Adding the office365-a3 value to ProvAccounts for: {0}", UserPrincipalName);
                        provAccountsManager.EnableFacultyA3(UserPrincipalName);
                    }
                }
            }


        }
    }
}
