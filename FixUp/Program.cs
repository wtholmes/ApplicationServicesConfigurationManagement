using ActiveDirectoryAccess;
using CornellIdentityManagement;
using System;
using System.Collections.Generic;
using System.IO;
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
            string path = @"NoRoutingwithGsuite.txt";

            // Read all lines from the file into a string array
            string[] lines = File.ReadAllLines(path);

            // Display each line
            foreach (string line in lines)
            {
                Console.WriteLine(line);
                provAccountsManager.EnableMailRouting(line);

            }

           
            //provAccountsManager.EnableMailRouting("dy328@cornell.edu");

            







        }
    }
}
