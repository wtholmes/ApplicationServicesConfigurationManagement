using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Security.Principal;

namespace ActiveDirectoryAccess
{

    public class ActiveDirectoryEntity : DynamicObject
    {
        #region Constructors

        /// <summary>
        ///     Default Constructer
        /// </summary>
        public ActiveDirectoryEntity()
        {
            directoryProperties = new Dictionary<String, Object>(StringComparer.OrdinalIgnoreCase);
            memberOf = new List<ActiveDirectoryEntity>();
        }

        #endregion Constructors

        #region Dynamic Property Handling

        /// <summary>
        ///     Return the count of Active Directory Properties.
        /// </summary>
        /// <returns></returns>
        public Int32 Count()
        {
            return directoryProperties.Count();
        }

        /// <summary>
        ///    Get the named Active Directory Propery as a Dynamic Property.
        /// </summary>
        /// <param name="binder"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            result = null;
            try
            {
                if (directoryProperties.ContainsKey(binder.Name))
                {
                    result = directoryProperties[binder.Name];
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        ///  Set the named Active Directory Property.
        /// </summary>
        /// <param name="binder"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            try
            {
                // Update the value directory properites dictionary entry.
                if (directoryProperties.ContainsKey(binder.Name))
                {
                    directoryProperties[binder.Name] = value;
                }
                // Add a new value to the directory properties dictionary.
                else
                {
                    directoryProperties.Add(binder.Name, value);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        ///     Get the Active Directory Propery by index.
        /// </summary>
        /// <param name="binder"></param>
        /// <param name="indexes"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
        {
            int index = (int)indexes[0];
            result = null;
            try
            {
                if (index < directoryProperties.Count)
                {
                    result = directoryProperties.ElementAt(index);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="binder"></param>
        /// <param name="indexes"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public override bool TrySetIndex(SetIndexBinder binder, object[] indexes, object value)
        {
            int index = (int)indexes[0];
            try
            {
                // Check if we are updating an existing property by index.
                if (index < directoryProperties.Count)
                {
                    // Get the Key for the idexed property.
                    String ElementKey = directoryProperties.ElementAt(index).Key;

                    // See if we are updating a named property.
                    PropertyInfo NamedProperty = this.GetType().GetProperties().Where(p => p.Name.Equals(ElementKey, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

                    // The named property in the class so update the named property
                    // value as well as the directory properties dictionary. 
                    if (NamedProperty != null)
                    {
                        if (value.GetType().Equals(NamedProperty.PropertyType))
                        {
                            NamedProperty.SetValue(this, value);
                            directoryProperties[ElementKey] = value;
                        }
                    }
                    // This is not a named property in the class so only update
                    // the directory properties dictionary.
                    else
                    {
                        directoryProperties[ElementKey] = value;
                    }

                }
                // The requested index does not exist just create a new value in the dictionary.
                else
                {
                    if (value.GetType().Equals(typeof(KeyValuePair<String, Object>)))
                    {
                        KeyValuePair<String, Object> NewEntry = (KeyValuePair<String, Object>)value;

                        if (!directoryProperties.ContainsKey(NewEntry.Key))
                        {
                            directoryProperties.Add(NewEntry.Key, NewEntry.Value);
                        }
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion Dynamic Property Handling

        #region Public Properties

        [Key]
        public Guid objectGUID { get; set; }

        public string cn { get; set; }

        public string description { get; set; }

        public Dictionary<String, Object> directoryProperties { get; set; }

        public string displayName { get; set; }

        public string distinguishedName{ get; set; }

        public Int32 groupType { get; set; }

        public string givenName { get; set; }

        public string initials { get; set; }

        public string mail { get; set; }

        public string mailNickname { get; set; }
        public List<ActiveDirectoryEntity> memberOf { get; set; }

        public string name { get; set; }

        public SecurityIdentifier objectSid { get; set; }

        public String objectType { get; set; }

        public List<string> proxyAddresses { get; set; }

        public string samAccountName { get; set; }

        public string sn { get; set; }

        public string userprincipalName { get; set; }

        #endregion Public Properties
    }
}