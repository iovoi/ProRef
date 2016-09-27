using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fiddler;
//using System.Security.Cryptography.X509Certificates;
using System.Collections;
using System.Diagnostics;
using System.Net;
using System.IO;
using System.IO.Compression;

namespace Cello
{
    public class Proxy
    {
        // lock to ensure thread safty
        private readonly static object syncObject = new object();

        // the form object that the Proxy attached to
        private MainForm mainForm = null;

        // fiddler core configuration
        private FiddlerCoreStartupFlags proxy_config = FiddlerCoreStartupFlags.Default;

        // set up domains to track
        private string[] domains = new string[2];
        private string[] removeTypes = new string[6];

        // the wood to construct the trees
        //private Woods sessionWoods = new Woods();
        private Woods sessionWoods = null;
        public Woods SessionWoods 
        {
            get 
            {
                lock (syncObject)
                {
                    return sessionWoods;
                }
            }
            set 
            {
                lock (syncObject)
                {
                    if (null != value) sessionWoods = value;
                }
            }
        }

        public List<Session> request_chain;
        public List<int> page_sessionID_chain;

        public Proxy(MainForm form)
        {
            mainForm = form;

            // set up the Woods that holds the sessions
            SessionWoods = new Woods(mainForm);

            // set up what domains to capture and what types to remove
            // later should be modified to load from configuration file
            //domains[0] = "facebook";
            domains[0] = "instagram";
            domains[1] = "akamaihd";
            removeTypes[0] = "jpg";
            removeTypes[1] = "png";
            removeTypes[2] = "jpeg";
            removeTypes[3] = "gif";
            removeTypes[4] = "ico";
            // also remove css file
            removeTypes[5] = "css";

            // set fiddler core configuration
            proxy_config = (proxy_config | FiddlerCoreStartupFlags.DecryptSSL);
            proxy_config = (proxy_config | FiddlerCoreStartupFlags.RegisterAsSystemProxy);

            // set to show debug messages
            //FiddlerApplication.OnNotification += delegate(object sender, NotificationEventArgs oNEA)
            //{
            //    mainForm.alert_message("OnNotification: " + oNEA.NotifyString);
            //};
            //FiddlerApplication.Log.OnLogString += delegate(object sender, LogEventArgs oLEA)
            //{
            //    mainForm.alert_message("OnLogString: " + oLEA.LogString);
            //};

            // do not use
            //FiddlerApplication.Prefs.SetStringPref("fiddler.certmaker.bc.cert", "DO_NOT_TRUST_FiddlerRoot");
            //FiddlerApplication.Prefs.SetStringPref("fiddler.certmaker.bc.key", "9fe03dff02f4610b5ae7d38e19b36990_339cb36d-5481-4643-a543-55d8b0fbee76");

            // check to ensure the decrypt https funtionality is working
            //mainForm.alert_message(CertMaker.rootCertExists() + "");
            if (!Install_Certificate())
                mainForm.alert_message("install certificate failed");

            //X509Store certStore = new X509Store(StoreName.CertificateAuthority, StoreLocation.LocalMachine);
            //certStore.Open(OpenFlags.OpenExistingOnly);

            // set up listeners to capture the traffic
            //FiddlerApplication.BeforeRequest += FiddlerApplication_BeforeRequest;
            FiddlerApplication.AfterSessionComplete 
                += FiddlerApplication_AfterSessionComplete;
        }

        public static bool Install_Certificate()
        {
            if (!CertMaker.rootCertExists())
            {
                if (!CertMaker.createRootCert())
                    return false;


                if (!CertMaker.trustRootCert())
                    return false;

                //return false;
            }

            if (!CertMaker.rootCertIsTrusted())
                return false;

            return true;
        }

        public static bool Uninstall_Certificate()
        {
            if (CertMaker.rootCertExists())
            {
                if (!CertMaker.removeFiddlerGeneratedCerts(true))
                    return false;
            }
            return true;
        }

        public void Start()
        {
            mainForm.WriteLine("Starting...");

            //FiddlerApplication.Startup(7777, true, true, true);
            FiddlerApplication.Startup(7777, proxy_config);

            mainForm.WriteLine("Started...");
            mainForm.WriteLine(" "); // print return to separate the output line later
            //mainForm.WriteLine("Press q to quit");

        }

        void FiddlerApplication_AfterSessionComplete(Session oSession)
        {
            Debug.Assert(null != oSession);
            Debug.Assert(null != SessionWoods);

            //mainForm.WriteLine(oSession.ToString());
            /*string referer = "null";
            if (!oSession.RequestMethod.Equals("CONNECT"))
            {
                if (null != oSession.oRequest.headers)
                {
                    if (oSession.oRequest.headers.Exists("Referer"))
                    {
                        referer = string.IsNullOrEmpty(oSession.oRequest.headers["Referer"]) ? "initiating request" : oSession.oRequest.headers["Referer"];
                    }
                    else
                        referer = "no referer";
                }
                else
                    referer = "no headers";
            }
            else
            {
                referer = "CONNECT";
            }*/

            //mainForm.WriteLine(oSession.id.ToString() + ": " + referer);
            //mainForm.WriteLine(oSession.id.ToString() + ": " + oSession.ToString());
            mainForm.WriteLine(oSession.id.ToString() + ": " + oSession.RequestMethod + " " + oSession.fullUrl);

            // testing
            //mainForm.WriteLine(oSession.oRequest.headers.Exists("referer") ? "referer" + oSession.oRequest.headers["referer"] : "referer: null");
            //mainForm.WriteLine("requestHeaders.tostring(): " + oSession.RequestHeaders.ToString());
            //mainForm.WriteLine("osession.url: " + oSession.url);
            //mainForm.WriteLine("request.headers.tostring(): " + oSession.oRequest.headers.ToString());
            //mainForm.WriteLine("oSession.RequestBody(): " + oSession.RequestBody.ToString());
            
            // decode request and response
            oSession.utilDecodeRequest();
            oSession.utilDecodeResponse();

            //mainForm.WriteLine("oSession.RequestBody(): " + System.Text.Encoding.UTF8.GetString(oSession.RequestBody));
            //mainForm.WriteLine("oSession.RequestBody(): " + oSession.GetRequestBodyAsString());
            //mainForm.WriteLine("oSession.ResponseBody(): " + System.Text.Encoding.UTF8.GetString(oSession.ResponseBody));
            //mainForm.WriteLine("oSession.ResponseBody(): " + oSession.GetResponseBodyAsString());
            
            // construct the woods from completed sessions
            //SessionWoods.add(oSession);
            // add the non-filtered sessions to our tree
            if (!shouldOmit(oSession))
                SessionWoods.add(oSession);

            //Session p = GetSessionParent(oSession);
            //mainForm.Add2TreeView(p, oSession);
            //SessionHashTable.Add(oSession.url, oSession);
            //mainForm.Add2TreeView(null, oSession);

        }

        public void Stop()
        {
            mainForm.WriteLine("\r\nStopping Cello...");

            // de-register listeners
            //FiddlerApplication.BeforeRequest -= FiddlerApplication_BeforeRequest;
            FiddlerApplication.AfterSessionComplete -= FiddlerApplication_AfterSessionComplete;

            if (FiddlerApplication.IsStarted())
            {
                FiddlerApplication.Shutdown();
            }

            mainForm.WriteLine("Cello Stopped...");
        }

        public void FiddlerApplication_BeforeRequest(Session oSession)
        {
            //Console.WriteLine(oSession.hostname);
            mainForm.WriteLine(oSession.GetResponseBodyAsString());
        }

        protected bool shouldOmit(Session s)
        {
            // filtering out the CONNECT messages
            if (s.RequestMethod.Equals("CONNECT"))
            {
                return true;
            }

            // filtering out the external request to unrelted websites
            if (!(s.url.Contains(domains[0]) || s.url.Contains(domains[1])))
            {
                return true;
            }

            // filtering out the useless files such as image or css files
            if (s.url.EndsWith("." + removeTypes[0]) || s.url.EndsWith("." + removeTypes[1])
                || s.url.EndsWith("." + removeTypes[2]) || s.url.EndsWith("." + removeTypes[3])
                || s.url.EndsWith("." + removeTypes[4]) || s.url.EndsWith("." + removeTypes[5])) 
            {
                return true;
            }

            // filtering out the useless files such as imgaes
            if (null != s.oResponse && null != s.oResponse.headers &&
                (s.oResponse.headers.ExistsAndEquals("content-type", "image/" + removeTypes[0])
                || s.oResponse.headers.ExistsAndEquals("content-type", "image/" + removeTypes[1])
                || s.oResponse.headers.ExistsAndEquals("content-type", "image/" + removeTypes[2])
                || s.oResponse.headers.ExistsAndEquals("content-type", "image/" + removeTypes[3])
                || s.oResponse.headers.ExistsAndEquals("content-type", "image/" + "x-icon"))
                || s.oResponse.headers.ExistsAndEquals("content-type", "text/" + removeTypes[5]))
            {
                return true;
            }

            return false;
        }

        public void MakeRequest()
        {
            HttpWebResponse response;
            string responseText;

            if (FireRequest(out response))
            {
                responseText = ReadResponse(response);
                mainForm.WriteLine(responseText);
                response.Close();
            }
        }

        public bool FireRequest(out HttpWebResponse res)
        {
            res = null;

            try
            {
                // Create request to URL
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://www.facebook.com/");

                // Set request headers
                request.Accept = "text/html, application/xhtml+xml, */*";
                request.Headers.Set(HttpRequestHeader.AcceptLanguage, "en-SG");
                request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64; Trident/7.0; rv:11.0) like Gecko";
                request.Headers.Set(HttpRequestHeader.AcceptEncoding, "gzip, deflate");
                request.Headers.Add("DNT", @"1");

                //Get response to request.
                res = (HttpWebResponse)request.GetResponse();

            }
            catch (WebException e)
            {
                // ProtocolError indicates a valid HTTP response, but with a non-200 status code (e.g. 304 Not Modified, 404 Not Found)
                if (WebExceptionStatus.ProtocolError == e.Status)
                    res = (HttpWebResponse)e.Response;
                else
                    return false;
            }
            catch (Exception e)
            {
                if (null != res)
                {
                    res.Close();
                    //System.Console.WriteLine(e.StackTrace);
                }
                return false;
            }

            return true;
        }

        private bool Request_www_facebook_com(out HttpWebResponse response)
        {
            response = null;

            try
            {
                //Create request to URL.
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://www.facebook.com/login.php?login_attempt=1&lwv=110");

                //Set request headers.
                request.Accept = "text/html, application/xhtml+xml, */*";
                request.Referer = "https://www.facebook.com/";
                request.Headers.Set(HttpRequestHeader.AcceptLanguage, "en-SG");
                request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64; Trident/7.0; rv:11.0) like Gecko";
                request.ContentType = "application/x-www-form-urlencoded";
                request.Headers.Set(HttpRequestHeader.AcceptEncoding, "gzip, deflate");
                request.Headers.Add("DNT", @"1");
                request.Headers.Set(HttpRequestHeader.CacheControl, "no-cache");
                request.Headers.Set(HttpRequestHeader.Cookie, @"_js_datr=7zbxVjb5o-oqgWo4XdJIBGpu; _js_reg_fb_ref=https%3A%2F%2Fwww.facebook.com%2F; _js_reg_fb_gate=https%3A%2F%2Fwww.facebook.com%2F");

                //Set request method
                request.Method = "POST";

                // Disable 'Expect: 100-continue' behavior. More info: http://haacked.com/archive/2004/05/15/http-web-request-expect-100-continue.aspx
                request.ServicePoint.Expect100Continue = false;

                //Set request body.
                string body = @"lsd=AVrnwsrh&email=alice.pat.tester@gmail.com&pass=patssotest1&default_persistent=0&timezone=-480&lgndim=eyJ3IjoxOTIwLCJoIjoxMDgwLCJhdyI6MTkyMCwiYWgiOjEwNDAsImMiOjI0fQ%3D%3D&lgnrnd=051335_Gm0o&lgnjs=1458648814&locale=en_US&qsstamp=W1tbNywxOSw0MCw0Myw0NCw2OCw2OSwxMTEsMTUyLDE2NywxNzIsMTc3LDE4MywyMDksMjI1LDIzMCwyMzEsMjM2LDI0NSwyNjQsMjY4LDI3MSwyODMsMzcxLDM4NiwzOTcsNDIxLDQyNSw0MzAsNDUxLDQ3Miw1MTEsNTEyLDUyNSw1NjEsNTkwLDYwMiw2MDgsNjIyLDY1Miw3MzgsNzgxXV0sIkFabGFqRG1FZWxpcGZwd25lZS1KMm51Mjd4RU1JNzBHc2tnTzI2RkhFMVp3X3FXRlkydG10SmpUV0d0Ml9YczEtQWtjSUJRcWg4dEJMV0hUcUg2elVmVFgzVC12UTg3Snl0Q0F0UnRYY3ZrbGJjdjM2bm1xN1pYcGgwVldIVmpSY01mOWE4WERvTThLLXlzb21iSUFULUIxWGNKRV96akdnZnJLZGdBVnNyWEp6OHVvVnVfNUZDNW1tV0JXMmVrdUh2MjNRUGdhNU53TURKZWw4Q2hYRHVVOXQ3SVRweXlFTDVuU3VmOEk1TGQ3RGciXQ%3D%3D";
                byte[] postBytes = System.Text.Encoding.UTF8.GetBytes(body);
                request.ContentLength = postBytes.Length;
                Stream stream = request.GetRequestStream();
                stream.Write(postBytes, 0, postBytes.Length);
                stream.Close();

                //Get response to request.
                response = (HttpWebResponse)request.GetResponse();
            }
            catch (WebException e)
            {
                //ProtocolError indicates a valid HTTP response, but with a non-200 status code (e.g. 304 Not Modified, 404 Not Found)
                if (e.Status == WebExceptionStatus.ProtocolError) response = (HttpWebResponse)e.Response;
                else return false;
            }
            catch (Exception)
            {
                if (response != null) response.Close();
                return false;
            }

            return true;
        }

        public string ReadResponse(HttpWebResponse res)
        {
            using (Stream responseStream = res.GetResponseStream())
            {
                Stream stream2Read = responseStream;
                if (res.ContentEncoding.ToLower().Contains("gzip"))
                {
                    stream2Read = new GZipStream(stream2Read, CompressionMode.Decompress);
                }
                else if (res.ContentEncoding.ToLower().Contains("deflate"))
                {
                    stream2Read = new DeflateStream(stream2Read, CompressionMode.Decompress);
                }

                using (StreamReader streamReader = new StreamReader(stream2Read, Encoding.UTF8))
                {
                    return streamReader.ReadToEnd();
                }
            }
        }
    }
}
