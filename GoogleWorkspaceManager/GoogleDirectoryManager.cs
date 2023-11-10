using Google.Apis.Admin.Directory.directory_v1;
using Google.Apis.Admin.Directory.directory_v1.Data;
using Google.Apis.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using GSuiteDirectory = Google.Apis.Admin.Directory.directory_v1;

namespace GoogleWorkspaceManager
{
    #region --- Google Directory Manager Class ---

    /// <summary>
    /// This class can be used to manage a Google Workspace Directory. Public
    /// </summary>
    public class GoogleDirectoryManager : GoogleWorkspaceManager
    {
        #region Public Properties

        /// <summary>
        /// Sets the maximum number of unique Google Workspace User Entires to Cache. Set to Zero (0) to disable the cache. Defaults to 100
        /// </summary>
        public int UserCacheSize { get; set; } = 100;

        /// <summary>
        /// Sets the maximum lifetime of a cached User Entry in seconds. Defaults to 900 (15 minutes).
        /// </summary>
        public int UserCacheTimeOut { get; set; } = 900;

        /// <summary>
        /// Gets the Current number of cached Google Workspace Users
        /// </summary>
        public int UserCacheCount
        { get { return CachedUsers.Count; } }

        #endregion Public Properties

        #region --- Private Properties ---

        private DirectoryService GSuiteDirectoryService = null;
        private List<CachedUser> CachedUsers = new List<CachedUser>();

        #endregion --- Private Properties ---

        #region --- Constructors & Finalaizers ---

        /// <summary>
        /// Default Class Constructor
        /// </summary>
        public GoogleDirectoryManager(String GoogleAPIConfiguration) : base(GoogleAPIConfiguration)
        {
            GSuiteDirectoryService = new DirectoryService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = GetOAuthCredential(),
                ApplicationName = ApplicationName
            });
        }

        #endregion --- Constructors & Finalaizers ---

        #region --- Public Methods ---

        /// <summary>
        /// Clears all Cached Users
        /// </summary>
        public void ClearUserCache()
        {
            CachedUsers.Clear();
        }

        /// <summary>
        /// Clear the specficed Cached User
        /// </summary>
        /// <param name="EmailAddress"></param>
        public void ClearUserCache(String EmailAddress)
        {
            CachedUser cachedUser = CachedUsers.Where(u => u.Emails.Select(n => n.Address).Contains(EmailAddress)).FirstOrDefault();
            if (cachedUser != null)
            {
                CachedUsers.Remove(cachedUser);
            }
        }

        /// <summary>
        /// Get the Google Workspace User's Directory Entry from an on of their Email Addresses
        /// </summary>
        /// <param name="EmailAddress">An SMPT Address</param>
        /// <returns>A Google Workspace User Object</returns>
        public GoogleWorkspaceUser GetGoogleUser(String EmailAddress)
        {
            if (GSuiteDirectoryService != null)
            {
                CachedUser cachedUser = null;
                // Check for valid cached user.
                cachedUser = CachedUsers.Where(u => u.Emails.Select(n => n.Address).Contains(EmailAddress) && u.WhenCached > DateTime.UtcNow.AddSeconds(-UserCacheTimeOut)).FirstOrDefault();
                if (cachedUser != null)
                {
                    //Console.WriteLine("Returning Cached User");
                    // Return the cached user.
                    return PopulateGoogleWorkspaceUser(cachedUser);
                }
                else
                {
                    List<KeyValuePair<String, Object>> UserProperties = new List<KeyValuePair<String, Object>>();
                    GSuiteDirectory.UsersResource.ListRequest request = GSuiteDirectoryService.Users.List();
                    request.Query = String.Format(@"email={0}", EmailAddress);
                    request.Customer = "my_customer";
                    request.MaxResults = 10;
                    request.OrderBy = GSuiteDirectory.UsersResource.ListRequest.OrderByEnum.Email;
                    IList<User> users = request.Execute().UsersValue;

                    if (users.Count == 1)
                    {
                        // Check for an expired cached user.
                        cachedUser = CachedUsers.Where(u => u.Emails.Select(n => n.Address).Contains(EmailAddress)).FirstOrDefault();
                        if (cachedUser != null)
                        {
                            // Remove the expired cached user
                            //Console.WriteLine("Removing Expired Cached User");
                            CachedUsers.Remove(cachedUser);
                        }
                        // Trim the cache if it has exceeded the max count
                        if (CachedUsers.Count >= UserCacheSize)
                        {
                            if (CachedUsers.Count > 0) { CachedUsers.RemoveAt(0); }
                        }
                        // Cache and return the new/updated user
                        //Console.WriteLine("Returning User from Google Workspace Directory and Caching");
                        cachedUser = new CachedUser(users.First());
                        if (UserCacheSize > 0) { CachedUsers.Add(cachedUser); }
                        return PopulateGoogleWorkspaceUser(cachedUser);
                    }
                }
            }
            return null;
        }


        #endregion --- Public Methods ---

        #region Private Methods

        /// <summary>
        /// Populate a new Google Workspace Object with the Given User
        /// </summary>
        /// <param name="user">A Google User Object</param>
        /// <returns></returns>
        private GoogleWorkspaceUser PopulateGoogleWorkspaceUser(CachedUser user)
        {
            GoogleWorkspaceUser googleWorkspaceUser = new GoogleWorkspaceUser();

            PropertyInfo[] propertiesInfo = user.GetType().GetProperties();
            foreach (PropertyInfo propertyInfo in propertiesInfo)
            {
                PropertyInfo thisPropertyInfo = googleWorkspaceUser.GetType().GetProperty(propertyInfo.Name);
                if (thisPropertyInfo != null)
                {
                    Object sourcePropertyValue = propertyInfo.GetValue(user, null);
                    switch (propertyInfo.PropertyType.Name)
                    {
                        case "IList`1":

                            switch (propertyInfo.Name)
                            {
                                // The Google Name property is of the Google Defined Type of Username.
                                case "Name":

                                    if (sourcePropertyValue != null)
                                    {
                                        UserName sourceUsername = (UserName)sourcePropertyValue;
                                        PropertyInfo[] usernameProperties = sourceUsername.GetType().GetProperties();
                                        foreach (PropertyInfo usernamePropertyInfo in usernameProperties)
                                        {
                                            PropertyInfo thisNameInfo = googleWorkspaceUser.GetType().GetProperty(usernamePropertyInfo.Name);
                                            if (thisNameInfo != null)
                                            {
                                                Object nameSourcePropertyValue = usernamePropertyInfo.GetValue(user, null);
                                                thisPropertyInfo.SetValue(googleWorkspaceUser, nameSourcePropertyValue, null);
                                            }
                                        }
                                    }
                                    break;

                                default:
                                    IEnumerable<Object> enumerable = sourcePropertyValue as IEnumerable<Object>;
                                    List<String> list = new List<String>();
                                    if (enumerable != null)
                                    {
                                        foreach (Object Item in enumerable)
                                        {
                                            if (Item != null)
                                            {
                                                list.Add(Item.ToString() ?? "");
                                            }
                                        }
                                    }
                                    thisPropertyInfo.SetValue(googleWorkspaceUser, list, null);
                                    break;
                            }
                            break;

                        default:

                            thisPropertyInfo.SetValue(googleWorkspaceUser, sourcePropertyValue, null);
                            break;
                    }
                }
                else
                {
                    // ----
                    // Properties that are not mapped one to one from a Google User to a 'this'
                    // ----

                    switch (propertyInfo.Name)
                    {
                        // The Google Name property is of the Google Defined Type of Username.
                        case "Name":
                            Object sourcePropertyValue = propertyInfo.GetValue(user, null);
                            if (sourcePropertyValue != null)
                            {
                                UserName sourceUsername = (UserName)sourcePropertyValue;
                                PropertyInfo[] usernameProperties = sourceUsername.GetType().GetProperties();
                                foreach (PropertyInfo usernamePropertyInfo in usernameProperties)
                                {
                                    PropertyInfo thisNameInfo = googleWorkspaceUser.GetType().GetProperty(usernamePropertyInfo.Name);
                                    if (thisNameInfo != null)
                                    {
                                        Object nameSourcePropertyValue = usernamePropertyInfo.GetValue(sourceUsername, null);
                                        thisNameInfo.SetValue(googleWorkspaceUser, nameSourcePropertyValue, null);
                                    }
                                }
                            }
                            break;

                        default:
                            break;
                    }
                }
            }
            return googleWorkspaceUser;
        }

        #endregion Private Methods
    }

    #endregion --- Google Directory Manager Class ---

    #region --- Google Workspace User Class ---

    public class GoogleWorkspaceUser
    {
        #region --- Public Class Properties ---

        public Boolean AgreedToTerms { get; set; }
        public List<String> Aliases { get; set; } = new List<String>();
        public Boolean Archived { get; set; }
        public Boolean ChangePasswordAtNextLogon { get; set; }
        public DateTime? CreationTime { get; set; } = null;
        //public DateTimeOffset? CreationTimeDateTimeOffset { get; set; } = null;
        public String CreationTimeRaw { get; set; } = "";
        public DateTime? DeletionTime { get; set; } = null;
        //public DateTimeOffset? DeletionTimeDateTimeOffset { get; set; } = null;
        public String DeletionTimeRaw { get; set; } = "";
        public String DisplayName { get; set; } = "";
        public String ETag { get; set; } = "";
        public List<String> Emails { get; set; } = new List<String>();
        public String FamilyName { get; set; } = "";
        public String FullName { get; set; } = "";
        public String GivenName { get; set; } = "";
        public String Id { get; set; } = "";
        public Boolean IsAdmin { get; set; }
        public Boolean IsDelegatedAdmin { get; set; }
        public Boolean IsEnforcedIn2Sv { get; set; }
        public Boolean IsEntrolledIn2Sv { get; set; }
        public Boolean IsMailboxSetup { get; set; }
        public String Kind { get; set; } = "";
        public DateTime? LastLoginTime { get; set; } = null;
        //public DateTimeOffset? LastLoginTimeDateTimeOffset { get; set; } = null;
        public String LastLoginTimeRaw { get; set; } = "";
        public List<String> NonEditableAliases { get; set; } = new List<String>();
        public String Notes { get; set; } = "";
        public String OrgUnitPath { get; set; } = "";
        public String PrimaryEmail { get; set; } = "";
        public String RecoveryPhone { get; set; } = "";
        public Boolean Suspended { get; set; }
        public String SuspensionReason { get; set; } = "";
        public DateTime WhenCached { get; set; }

        #endregion --- Public Class Properties ---
    }

    #endregion --- Google Workspace User Class ---

    #region --- Cached Google Workspace User Derived Class

    /// <summary>
    /// This class is inherited from the Google Directory User Class. It adds
    /// the WhenCached Property to facilitate cache entry timeouts.
    /// </summary>
    public class CachedUser : User
    {
        public DateTime WhenCached { get; } = DateTime.UtcNow;

        public CachedUser(User user)
        {
            Type ParentType = user.GetType();
            Type ChildType = this.GetType();
            foreach (PropertyInfo propertyInfo in ParentType.GetProperties())
            {
                Object propertyValue = propertyInfo.GetValue(user);

                this.GetType().GetProperty(propertyInfo.Name).SetValue(this, propertyValue);

                Object SetPropertyValue = propertyInfo.GetValue(this);

            }
        }
    }

    #endregion --- Cached Google Workspace User Derived Class
}
