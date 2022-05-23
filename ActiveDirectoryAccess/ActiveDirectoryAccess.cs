using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.DirectoryServices;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Security.Principal;
using System.Text;

namespace ActiveDirectoryAccess
{
    public class ActiveDirectoryContext : DbContext
    {
        #region ---- Private Properties ----

        private DirectoryEntry rootDSE;
        private DirectoryEntry activeDirectory;

        #endregion ---- Private Properties ----

        #region ---- Constructors ----

        /// <summary>
        /// Public Constructor.
        /// </summary>
        public ActiveDirectoryContext()
        {
            rootDSE = new DirectoryEntry("LDAP://RootDSE");
            activeDirectory = new DirectoryEntry(String.Format("LDAP://{0}", rootDSE.Properties["defaultNamingContext"][0]));
            activeDirectoryEntities = new List<dynamic>();
        }

        #endregion ---- Constructors ----

        #region ---- Private Methods ----

        private List<dynamic> activeDirectoryEntities;

        #endregion ---- Private Methods ----

        #region ---- Public Properties ----

        /// <summary>
        /// The collection of entities returned from a search.
        /// </summary>
        public List<ActiveDirectoryEntity> ActiveDirectoryEntities
        {
            get
            {
                List<ActiveDirectoryEntity> results = new List<ActiveDirectoryEntity>();
                foreach (ActiveDirectoryEntity activeDirectoryEntity in activeDirectoryEntities)
                {
                    results.Add(activeDirectoryEntity);
                }
                return results;
            }
        }

        /// <summary>
        /// Include Group Membership for all entities.
        /// </summary>
        public Boolean IncludeGroupMembership { get; set; }

        /// <summary>
        /// Include Nested Group Membership for all entities.
        /// </summary>
        public Boolean IncludeNestedGroupMembership { get; set; }

        #endregion ---- Public Properties ----

        #region ---- Public Methods ----

        /// <summary>
        /// Dispose
        /// </summary>

        public new void Dispose()
        {
            ActiveDirectoryEntities.Clear();
            activeDirectory.Dispose();
        }

        public ActiveDirectoryEntity Find(Guid? Id)
        {
            if (Id != null)
            {
                return SearchDirectory((Guid)Id);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Search the Active Directory for a specific objectGUID
        /// </summary>
        /// <param name="ObjectGUID"></param>
        public ActiveDirectoryEntity SearchDirectory(Guid ObjectGUID)
        {
            StringBuilder directoryFilter = new StringBuilder("(&(objectGUID=");
            byte[] guidBytes = ObjectGUID.ToByteArray();
            foreach (byte guidByte in guidBytes)
            {
                directoryFilter.Append(String.Format(@"\{0}", guidByte.ToString("x2")));
            }
            directoryFilter.Append("))");
            return SearchDirectory(directoryFilter.ToString(), false, null).FirstOrDefault();
        }

        /// <summary>
        /// Search the Active Directory using the UserPrincipalName
        /// </summary>
        /// <param name="UserPrincipalName"></param>
        public ActiveDirectoryEntity SearchDirectory(String UserPrincipalName)
        {
            String directorySearchFilter = String.Format("(&(userPrincipalName={0}))", UserPrincipalName);
            return SearchDirectory(directorySearchFilter, false, null).FirstOrDefault();
        }

        /// <summary>
        /// Search the Active Directory using the Distinguished Name.
        /// </summary>
        /// <param name="DistinguishedName"></param>
        /// <returns></returns>
        public ActiveDirectoryEntity SearDirectoryByDN(String DistinguishedName)
        {
            String directorySearchFilter = String.Format("(&(distinguishedName={0}))", DistinguishedName);
            return SearchDirectory(directorySearchFilter, false, null).FirstOrDefault();
        }


        /// <summary>
        /// Search the Active Directory for a Property equal to Value
        /// </summary>
        /// <param name="Property"></param>
        /// <param name="Value"></param>
        public List<dynamic> SearchDirectory(String Property, String Value)
        {
            return SearchDirectory(String.Format("(&({0}={1}))", Property, Value), true, null);
        }

        /// <summary>
        /// Search the Active Directory using the SearchFilter
        /// </summary>
        /// <param name="SearchFilter"></param>
        public List<dynamic> SearchDirectory(string SearchFilter, Boolean ReturnMany, Int32? MaxResults)
        {
            activeDirectoryEntities.Clear();

            using (DirectorySearcher directorySearcher = new DirectorySearcher(activeDirectory))
            {
                if (!ReturnMany) { directorySearcher.SizeLimit = 1; }
                else { if (MaxResults != null) { directorySearcher.SizeLimit = Convert.ToInt32(MaxResults); } }
                directorySearcher.PageSize = 1000;
                directorySearcher.ServerPageTimeLimit = TimeSpan.FromSeconds(4);
                directorySearcher.CacheResults = true;
                directorySearcher.Filter = SearchFilter;

                // Peform the search and save the result.
                SearchResultCollection searchResults = directorySearcher.FindAll();

                if (searchResults.Count > 0) // Save the results to the Dynamic Collection and Return.
                {
                    // Peform the search and save the result to the ActiveDirectory Entities Collection.
                    foreach (SearchResult searchResult in searchResults)
                    {
                        dynamic activeDirectoryEntity = PopulateEntity(searchResult);
                        activeDirectoryEntities.Add(activeDirectoryEntity);
                    }
                    return activeDirectoryEntities;
                }
                else // No results return Null.
                {
                    return null;
                }
            }
        }

        public List<dynamic> SearchDirectory(DomainControllerUSNQueryRange domainControllerUSNQueryRange)
        {
            activeDirectoryEntities.Clear();

            using (DirectoryEntry SearchRoot = new DirectoryEntry(String.Format("LDAP://{0}/{1}", domainControllerUSNQueryRange.DomainControllerName, rootDSE.Properties["defaultNamingContext"][0])))
            {
                using (DirectorySearcher directorySearcher = new DirectorySearcher(SearchRoot))
                {
                    directorySearcher.PageSize = 1000;
                    directorySearcher.ServerPageTimeLimit = TimeSpan.FromSeconds(4);
                    directorySearcher.CacheResults = false;
                    directorySearcher.Filter = domainControllerUSNQueryRange.ADearchFilter;

                    // Peform the search and save the result.
                    SearchResultCollection searchResults = directorySearcher.FindAll();

                    if (searchResults.Count > 0) // Save the results to the Dynamic Collection and Return.
                    {
                        // Peform the search and save the result to the ActiveDirectory Entities Collection.
                        foreach (SearchResult searchResult in searchResults)
                        {
                            dynamic activeDirectoryEntity = PopulateEntity(searchResult);
                            activeDirectoryEntities.Add(activeDirectoryEntity);
                        }
                        return activeDirectoryEntities;
                    }
                    else // No results return Null.
                    {
                        return null;
                    }
                }
            }
        }

        #endregion ---- Public Methods ----

        #region ---- Private Methods ----

        /// <summary>
        /// Add the DirectoryEntry of the given Distinguished Name to the List of Entities.
        /// </summary>
        /// <param name="DistinguishedName"></param>
        /// <returns></returns>
        private dynamic PopulateEntity(String DistinguishedName)
        {
            DirectoryEntry directoryEntry = new DirectoryEntry(String.Format("LDAP://{0}", DistinguishedName));
            return PopulateEntity(directoryEntry);
        }

        /// <summary>
        /// Add the SearchResult to the List of Entities.
        /// </summary>
        /// <param name="searchResult"></param>
        /// <returns></returns>
        private dynamic PopulateEntity(SearchResult searchResult)
        {
            return PopulateEntity(searchResult.GetDirectoryEntry());
        }

        /// <summary>
        /// Add the DirectoryEntry to the list of Entities.
        /// </summary>
        /// <param name="directoryEntry"></param>
        /// <returns></returns>
        private dynamic PopulateEntity(DirectoryEntry directoryEntry)
        {
            dynamic activeDirectoryEntity = new ActiveDirectoryEntity();
            PropertyInfo[] EntityProperties = activeDirectoryEntity.GetType().GetProperties();
            foreach (PropertyInfo EntityProperty in EntityProperties)
            {
                //Console.WriteLine("Entity Property Name: {0}", EntityProperty.Name);
                if (directoryEntry.Properties.Contains(EntityProperty.Name))
                {
                    PropertyValueCollection directoryEntryProperty = directoryEntry.Properties[EntityProperty.Name];

                    switch (EntityProperty.Name)
                    {
                        // ObjectGUID property requires special handling to convert to System GUID.
                        case "objectGUID":
                            EntityProperty.SetValue(activeDirectoryEntity, new Guid((byte[])directoryEntryProperty[0]));
                            break;
                        // ObjectSid property requires special handling to convert to a SecurityIdentifier
                        case "objectSid":
                            if (directoryEntryProperty.Count == 1)
                            {
                                EntityProperty.SetValue(activeDirectoryEntity, new SecurityIdentifier((byte[])directoryEntryProperty[0], 0));
                            }
                            break;
                        // MemberOf property requires special handling to find all nested memberships using the matching rule OID:
                        // (1.2.840.113556.1.4.1941) LDAP_MATCHING_RULE_IN_CHAIN. For detailed information see:
                        // https://docs.microsoft.com/en-us/windows/win32/adsi/search-filter-syntax
                        case "memberOf":

                            if (IncludeNestedGroupMembership)
                            {
                                using (DirectorySearcher directorySearcher = new DirectorySearcher(activeDirectory))
                                {
                                    directorySearcher.PageSize = 1000;
                                    directorySearcher.ServerPageTimeLimit = TimeSpan.FromSeconds(2);
                                    directorySearcher.CacheResults = false;
                                    directorySearcher.Filter = String.Format("(member:1.2.840.113556.1.4.1941:={0})", directoryEntry.Properties["distinguishedName"][0].ToString());
                                    SearchResultCollection memberOfSearchResults = directorySearcher.FindAll();
                                    foreach (SearchResult searchResult in memberOfSearchResults)
                                    {
                                        activeDirectoryEntity.memberOf.Add(PopulateEntity(searchResult.GetDirectoryEntry()));
                                    }
                                }
                            }
                            else if (IncludeGroupMembership)
                            {
                                foreach (String memberDN in directoryEntry.Properties["memberOf"])
                                {
                                    activeDirectoryEntity.memberOf.Add(PopulateEntity(memberDN));
                                }
                            }
                            else
                            {
                                activeDirectoryEntity.memberOf = null;
                            }
                            break;

                        // Properties that do not require special handling are processed here.
                        default:
                            if (directoryEntryProperty.Count > 0)
                            {
                                // Multi-Value Properties.
                                if (EntityProperty.PropertyType.UnderlyingSystemType.IsGenericType)
                                {
                                    EntityProperty.SetValue(activeDirectoryEntity, MultiValuePropertyToList(directoryEntryProperty));
                                }
                                // Singleton Properties.
                                else
                                {
                                    EntityProperty.SetValue(activeDirectoryEntity, directoryEntryProperty[0]);
                                }
                            }
                            break;
                    }
                }
                else
                {
                    switch (EntityProperty.Name)
                    {
                        case "objectType":
                            activeDirectoryEntity.objectType = GetObjectType(directoryEntry);
                            break;

                        case "directoryProperties":

                            foreach (String PropertyName in directoryEntry.Properties.PropertyNames)
                            {
                                Object propertyValue = directoryEntry.Properties[PropertyName].Value;
                                activeDirectoryEntity.directoryProperties.Add(PropertyName, propertyValue);
                            }
                            break;
                    }
                }
            }
            return activeDirectoryEntity;
        }

        /// <summary>
        /// Get the Active Directory Object Type for the given SearchResult
        /// </summary>
        /// <param name="searchResult"></param>
        /// <returns></returns>
        private String GetObjectType(SearchResult searchResult)
        {
            return GetObjectType(searchResult.GetDirectoryEntry());
        }

        /// <summary>
        ///     Get the Active Directory Object Type for the given Directory Entry.
        /// </summary>
        /// <param name="searchResult"></param>
        /// <returns></returns>
        private String GetObjectType(DirectoryEntry directoryEntry)
        {
            List<String> objectClass = MultiValuePropertyToList(directoryEntry.Properties["objectClass"]);
            // All object types derive from top.
            if (objectClass.Contains("top"))
            {
                // Person Objects (Subclass of Top)
                if (objectClass.Contains("person"))
                {
                    if (objectClass.Contains("organizationalPerson"))
                    {
                        // User Objects (Subclass of OrganizationalPerson)
                        if (objectClass.Contains("user"))
                        {
                            // Computer Objects. (Subclass of User).
                            if (objectClass.Contains("computer"))
                            {
                                return "computer";
                            }
                            // InetOrgPerson Objects (Subclass of User).
                            if (objectClass.Contains("inetOrgPerson"))
                            {
                                return "inetOrgPerson";
                            }

                            return "user";
                        }
                        // Contact Objects (Subclass of OrganizationalPerson)
                        if (objectClass.Contains("contact"))
                        {
                            return "contact";
                        }
                    }
                }
                // Group Objects (Subclass of Top)
                if (objectClass.Contains("group"))
                {
                    return "group";
                }
            }
            return "";
        }

        /// <summary>
        /// Get the ResultProperyValueCollections Values as a List of String.
        /// </summary>
        /// <param name="resultPropertyValueCollection"></param>
        /// <returns></returns>
        private List<String> MultiValuePropertyToList(ResultPropertyValueCollection resultPropertyValueCollection)
        {
            List<String> properyValues = new List<String>();
            for (int resultPropertyValueCollectionIndex = 0; resultPropertyValueCollectionIndex < resultPropertyValueCollection.Count; resultPropertyValueCollectionIndex++)
            {
                properyValues.Add(resultPropertyValueCollection[resultPropertyValueCollectionIndex].ToString());
            }
            return properyValues;
        }

        /// <summary>
        /// Get the ProperyValueCollections Values as a List of String.
        /// </summary>
        /// <param name="resultPropertyValueCollection"></param>
        /// <returns></returns>
        private List<String> MultiValuePropertyToList(PropertyValueCollection resultPropertyValueCollection)
        {
            List<String> properyValues = new List<String>();
            for (int resultPropertyValueCollectionIndex = 0; resultPropertyValueCollectionIndex < resultPropertyValueCollection.Count; resultPropertyValueCollectionIndex++)
            {
                properyValues.Add(resultPropertyValueCollection[resultPropertyValueCollectionIndex].ToString());
            }
            return properyValues;
        }

        #endregion ---- Private Methods ----
    }

    public class ActiveDirectoryTopology : IDisposable
    {
        public List<DomainController> SiteDomainControllers { get; private set; }
        public String CurrentSiteName { get; private set; }

        public List<ActiveDirectorySite> ActiveDirectorySites { get; private set; }
        public List<DomainController> DomainControllers { get; private set; }

        public String DefaultNamingContext { get; private set; }

        public ActiveDirectoryTopology()
        {
            if (NetworkInterface.GetIsNetworkAvailable())
            {
                // Get this host's IP information.
                IPHostEntry iPHostEntry = Dns.GetHostEntry(Dns.GetHostName());

                // Get the current ActiveDirectory domain information.
                Domain domain = Domain.GetCurrentDomain();

                // Get the Distinguished Name for the Directory.

                DirectoryEntry RootDirEntry = new DirectoryEntry("LDAP://RootDSE");
                DefaultNamingContext = RootDirEntry.Properties["defaultNamingContext"].Value.ToString();

                // Initialize the SiteDomainControllers List.
                SiteDomainControllers = new List<DomainController>();
                CurrentSiteName = null;
                ActiveDirectorySites = new List<ActiveDirectorySite>();
                DomainControllers = new List<DomainController>();

                foreach (ActiveDirectorySite activeDirectorySite in domain.Forest.Sites)
                {
                    ActiveDirectorySites.Add(activeDirectorySite);

                    // Collect all the domain controllers for every site.
                    foreach (DomainController domainController in activeDirectorySite.Servers)
                    {
                        if (!DomainControllers.Contains(domainController))
                        {
                            DomainControllers.Add(domainController);
                        }
                    }

                    Boolean HostIsInActiveDirectorySite = false;
                    foreach (ActiveDirectorySubnet directorySubnet in activeDirectorySite.Subnets)
                    {
                        Int32 networkPrefixLength = Convert.ToInt32(directorySubnet.Name.Split('/')[1]);
                        Int32 maskedSubnet = BitConverter.ToInt32(IPAddress.Parse(directorySubnet.Name.Split('/')[0]).GetAddressBytes().Reverse().ToArray(), 0) >> networkPrefixLength << networkPrefixLength;

                        foreach (IPAddress iPAddress in iPHostEntry.AddressList)
                        {
                            Int32 maskedClientAddress = BitConverter.ToInt32(iPAddress.GetAddressBytes().Reverse().ToArray(), 0) >> networkPrefixLength << networkPrefixLength;
                            if (maskedSubnet == maskedClientAddress)
                            {
                                HostIsInActiveDirectorySite = true;
                            }
                        }

                        if (HostIsInActiveDirectorySite)
                        {
                            CurrentSiteName = activeDirectorySite.Name;
                            foreach (DomainController domainController in activeDirectorySite.Servers)
                            {
                                if (!SiteDomainControllers.Contains(domainController))
                                {
                                    SiteDomainControllers.Add(domainController);
                                }
                            }
                        }
                    }
                }
            }
        }

        public void Dispose()
        {
            SiteDomainControllers.Clear();
            CurrentSiteName = null;
            ActiveDirectorySites.Clear();
            DomainControllers.Clear();
            DefaultNamingContext = null;
        }
    }

    public class DomainControllerUSNQueryRange
    {
        public String DomainControllerName { get; set; }

        public long? StartUSN { get; set; }

        public long? EndUSN { get; set; }

        public String ADearchFilter
        {
            get
            {
                if (this.StartUSN != null && this.EndUSN != null)
                {
                    if (EndUSN >= StartUSN)
                    {
                        return String.Format("(&(uSNChanged>={0})(uSNChanged<={1}))", StartUSN - 100, EndUSN);
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    return null;
                }
            }
        }
    }
}