using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace CornellIdentityManagement
{
    public class ProvAccountsManager
    {
        #region --- Private Properties ---

        private NetworkCredential networkCredential = new NetworkCredential("messaging", "P7P7Hij*SvmAC9=e(bPqpcaROnUFZKDfl9$NXqD1");
        private String BaseURI = "https://idmws.cit.cornell.edu";

        #endregion --- Private Properties ---

        #region --- Public Properties ---

        public Boolean Test { get; set; } = false;

        public List<String> CornelleduProvAccts { get; private set; }

        public List<String> CornelleduMailDelivery { get; private set; }

        #endregion --- Public Properties ---

        #region --- Class Constructors
        public ProvAccountsManager()
        {
            // Create a network credential to access the web service.
            //networkCredential = new NetworkCredential("messaging", "P7P7Hij*SvmAC9=e(bPqpcaROnUFZKDfl9$NXqD1");
        }

        #endregion --- Class Constructors

        #region --- Public Methods ---
        public void UseTest()
        {
            BaseURI = "https://idmws-test.cit.cornell.edu";
        }

        public NetIDProperties GetProvAccounts(String UserPrincipalName)
        {
            Uri uri = new Uri(String.Format("{0}/provacctsws/?netid={1}", BaseURI, UserPrincipalName.Split('@')[0]));
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

            NetIDProperties netIDProperties = JsonConvert.DeserializeObject<NetIDProperties>(webRequestResponse);
            return netIDProperties;
        }

        public void EnableFacultyA3(String UserPrincipalName)
        {
            if (!GetProvAccounts(UserPrincipalName).provision_accts.Contains("office365-a3"))
            {
                Uri uri = new Uri(String.Format("https://idmws.cit.cornell.edu/provacctsws/?netid={0}&action=add&attribute=provision_acct&value=office365-a3", UserPrincipalName.Split('@')[0]));
                CredentialCache myCredentialCache = new CredentialCache();
                myCredentialCache.Add(uri, "Basic", networkCredential);

                WebRequest webRequest = HttpWebRequest.Create(uri);
                webRequest.PreAuthenticate = true;
                webRequest.Credentials = myCredentialCache;
                webRequest.Method = "POST";

                try
                {
                    WebResponse webResponse = webRequest.GetResponse();
                    Stream webResponseStream = webResponse.GetResponseStream();
                    StreamReader streamReader = new StreamReader(webResponseStream, Encoding.Default);
                    String webRequestResponse = streamReader.ReadToEnd();

                    webResponseStream.Close();
                    webResponse.Close();
                }
                catch (WebException exp)
                {
                }
            }
        }

        public void DisableFacultyA3(String UserPrincipalName)
        {
            if (GetProvAccounts(UserPrincipalName).provision_accts.Contains("office365-a3"))
            {
                Uri uri = new Uri(String.Format("https://idmws.cit.cornell.edu/provacctsws/?netid={0}&action=remove&attribute=provision_acct&value=office365-a3", UserPrincipalName.Split('@')[0]));
                CredentialCache myCredentialCache = new CredentialCache();
                myCredentialCache.Add(uri, "Basic", networkCredential);

                WebRequest webRequest = HttpWebRequest.Create(uri);
                webRequest.PreAuthenticate = true;
                webRequest.Credentials = myCredentialCache;
                webRequest.Method = "POST";

                try
                {
                    WebResponse webResponse = webRequest.GetResponse();
                    Stream webResponseStream = webResponse.GetResponseStream();
                    StreamReader streamReader = new StreamReader(webResponseStream, Encoding.Default);
                    String webRequestResponse = streamReader.ReadToEnd();

                    webResponseStream.Close();
                    webResponse.Close();
                }
                catch (WebException exp)
                {
                }
            }
        }

        public void EnableOffice365Exchange(String UserPrincipalName)
        {
            Uri uri = new Uri(String.Format("https://idmws.cit.cornell.edu/provacctsws/?netid={0}&action=add&attribute=provision_acct&value=exchange", UserPrincipalName.Split('@')[0]));
            CredentialCache myCredentialCache = new CredentialCache();
            myCredentialCache.Add(uri, "Basic", networkCredential);

            WebRequest webRequest = HttpWebRequest.Create(uri);
            webRequest.PreAuthenticate = true;
            webRequest.Credentials = myCredentialCache;
            webRequest.Method = "POST";

            try
            {
                WebResponse webResponse = webRequest.GetResponse();
                Stream webResponseStream = webResponse.GetResponseStream();
                StreamReader streamReader = new StreamReader(webResponseStream, Encoding.Default);
                String webRequestResponse = streamReader.ReadToEnd();

                webResponseStream.Close();
                webResponse.Close();
            }
            catch (WebException exp)
            {
            }
        }

        public void EnableGoogleWorkspaceAccount(String UserPrincipalName)
        {
            Uri uri = new Uri(String.Format("https://idmws.cit.cornell.edu/provacctsws/?netid={0}&action=add&attribute=provision_acct&value=gsuite", UserPrincipalName.Split('@')[0]));
            CredentialCache myCredentialCache = new CredentialCache();
            myCredentialCache.Add(uri, "Basic", networkCredential);

            WebRequest webRequest = HttpWebRequest.Create(uri);
            webRequest.PreAuthenticate = true;
            webRequest.Credentials = myCredentialCache;
            webRequest.Method = "POST";

            WebResponse webResponse;
            try
            {
                webResponse = webRequest.GetResponse();
                Stream webResponseStream = webResponse.GetResponseStream();
                StreamReader streamReader = new StreamReader(webResponseStream, Encoding.Default);
                String webRequestResponse = streamReader.ReadToEnd();

                webResponseStream.Close();
                webResponse.Close();
            }
            catch (WebException exp)
            {

            }
        }

        public void DisableGoogleWorkspaceAccount(String UserPrincipalName)
        {
            Uri uri = new Uri(String.Format("https://idmws.cit.cornell.edu/provacctsws/?netid={0}&action=remove&attribute=provision_acct&value=gsuite", UserPrincipalName.Split('@')[0]));
            CredentialCache myCredentialCache = new CredentialCache();
            myCredentialCache.Add(uri, "Basic", networkCredential);

            WebRequest webRequest = HttpWebRequest.Create(uri);
            webRequest.PreAuthenticate = true;
            webRequest.Credentials = myCredentialCache;
            webRequest.Method = "POST";

            WebResponse webResponse;
            try
            {
                webResponse = webRequest.GetResponse();
                Stream webResponseStream = webResponse.GetResponseStream();
                StreamReader streamReader = new StreamReader(webResponseStream, Encoding.Default);
                String webRequestResponse = streamReader.ReadToEnd();

                webResponseStream.Close();
                webResponse.Close();
            }
            catch (WebException exp)
            {

            }
        }

        public void EnableMailRouting(String UserPrincipalName)
        {
            if (GetProvAccounts(UserPrincipalName).maildelivery.Contains("norouting"))
            {
                Uri uri = new Uri(String.Format("https://idmws.cit.cornell.edu/provacctsws/?netid={0}&action=remove&attribute=maildelivery&value=norouting", UserPrincipalName.Split('@')[0]));
                CredentialCache myCredentialCache = new CredentialCache();
                myCredentialCache.Add(uri, "Basic", networkCredential);

                WebRequest webRequest = HttpWebRequest.Create(uri);
                webRequest.PreAuthenticate = true;
                webRequest.Credentials = myCredentialCache;
                webRequest.Method = "POST";

                try
                {
                    WebResponse webResponse = webRequest.GetResponse();
                    Stream webResponseStream = webResponse.GetResponseStream();
                    StreamReader streamReader = new StreamReader(webResponseStream, Encoding.Default);
                    String webRequestResponse = streamReader.ReadToEnd();

                    webResponseStream.Close();
                    webResponse.Close();
                }
                catch (WebException exp)
                {
                }
            }
        }

        public void DisableMailRouting(String UserPrincipalName)
        {
            Uri uri = new Uri(String.Format("https://idmws.cit.cornell.edu/provacctsws/?netid={0}&action=add&attribute=maildelivery&value=norouting", UserPrincipalName.Split('@')[0]));
            CredentialCache myCredentialCache = new CredentialCache();
            myCredentialCache.Add(uri, "Basic", networkCredential);

            WebRequest webRequest = HttpWebRequest.Create(uri);
            webRequest.PreAuthenticate = true;
            webRequest.Credentials = myCredentialCache;
            webRequest.Method = "POST";

            try
            {
                WebResponse webResponse = webRequest.GetResponse();
                Stream webResponseStream = webResponse.GetResponseStream();
                StreamReader streamReader = new StreamReader(webResponseStream, Encoding.Default);
                String webRequestResponse = streamReader.ReadToEnd();

                webResponseStream.Close();
                webResponse.Close();
            }
            catch (WebException exp)
            {
            }
        }

        #endregion --- Public Methods ---
    }

    public class NetIDProperties
    {
        public String  netid { get; set; }
        public List<String> provision_accts { get; set; }
        public List<String> maildelivery { get; set; }
    }
}