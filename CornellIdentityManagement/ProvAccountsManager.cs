using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CornellIdentityManagement
{
    public class ProvAccountsManager
    {
        #region --- Private Properties ---
        private NetworkCredential networkCredential;
        #endregion

        #region --- Public Properties ---
        public List<String> CornelleduProvAccts { get; private set; }

        public List<String> CornelleduMailDelivery { get; private set; }

        #endregion

        #region --- Class Constructors
        public ProvAccountsManager()
        {
            // Create a network credential to access the web service.
            networkCredential = new NetworkCredential("messaging", "P7P7Hij*SvmAC9=e(bPqpcaROnUFZKDfl9$NXqD1");
        }
        #endregion

        #region --- Public Methods ---

        public void GetProvAccounts(String UserPrincipalName)
        {
            Uri uri = new Uri(String.Format("https://idmws.cit.cornell.edu/provacctsws/?netid={0}", UserPrincipalName.Split('@')[0]));
            CredentialCache myCredentialCache = new CredentialCache();
            myCredentialCache.Add(uri, "Basic", networkCredential);

            WebRequest webRequest = HttpWebRequest.Create(uri);
            webRequest.PreAuthenticate = true;
            webRequest.Credentials = myCredentialCache;
            webRequest.Method = "GET";

            WebResponse webResponse = webRequest.GetResponse();
            Stream webResponseStream = webResponse.GetResponseStream();

            StreamReader streamReader = new StreamReader(webResponseStream, Encoding.Default);
            String webRequestResponse = streamReader.ReadToEnd();

            webResponseStream.Close();
            webResponse.Close();
        }

        public void EnableFacultyA3(String UserPrincipalName)
        {

        }

        #endregion

    }

    public class CornellActiveDirectoryProperites
    {
        public List<String> provision_accts { get; set; }

        public List<String> maildelivery { get; set; }
    }
}
