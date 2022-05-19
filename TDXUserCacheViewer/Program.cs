using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ApplicationServicesConfigurationManagementDatabaseAccess;
using TeamDynamix.Api.Users;
using Newtonsoft.Json;

namespace TDXUserCacheViewer

{
    class Program
    {
        static void Main(string[] args)
        {
            TeamDynamixManagementContext context = new TeamDynamixManagementContext();

            List<User> tdxUsers = new List<User>();

            foreach(TeamDynamixUser teamDynamixUser in context.TeamDynamixUsers.ToList())
            {
                User cachedUser = JsonConvert.DeserializeObject<User>(teamDynamixUser.UserAsJSON);

                Console.WriteLine("CachedUser UserName: {0}  Email: {1}", cachedUser.UserName, cachedUser.PrimaryEmail);

            }


        }
    }
}
