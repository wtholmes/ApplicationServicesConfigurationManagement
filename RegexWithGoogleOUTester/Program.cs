using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using GoogleWorkspaceManager;

namespace RegexWithGoogleOUTester
{
    class Program
    {
        static void Main(string[] args)
        {
            GoogleDirectoryManager googleDirectoryManager = new GoogleDirectoryManager(@"E:\GSuiteManagerCredentials\gsuitemanager-edit.json");
            GoogleWorkspaceUser googleWorkspaceUser = googleDirectoryManager.GetGoogleUser("tco2@cornell.edu");
            if (Regex.IsMatch(googleWorkspaceUser.OrgUnitPath, @"(/PendingDeletion/Stage1|/PendingDeletion/Stage2)$", RegexOptions.IgnoreCase))
            {
                Console.WriteLine ("Hey This Worked") ;
            }
        }
    }
}
