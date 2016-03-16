using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fiddler;
//using System.Security.Cryptography.X509Certificates;
using System.Collections;
using System.Diagnostics;

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
        private string[] removeTypes = new string[5];

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

        public Proxy(MainForm form)
        {
            mainForm = form;

            // set up the Woods that holds the sessions
            SessionWoods = new Woods(mainForm);

            // set up what domains to capture and what types to remove
            // later should be modified to load from configuration file
            domains[0] = "facebook";
            domains[1] = "akamaihd";
            removeTypes[0] = "jpg";
            removeTypes[1] = "png";
            removeTypes[2] = "jpeg";
            removeTypes[3] = "gif";
            removeTypes[4] = "ico";

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
            mainForm.WriteLine("Starting Cello...");

            //FiddlerApplication.Startup(7777, true, true, true);
            FiddlerApplication.Startup(7777, proxy_config);

            mainForm.WriteLine("Cello started...");
            mainForm.WriteLine(" "); // print return to separate the output line later
            //mainForm.WriteLine("Press q to quit");

        }

        void FiddlerApplication_AfterSessionComplete(Session oSession)
        {
            Debug.Assert(null != oSession);
            Debug.Assert(null != SessionWoods);

            //mainForm.WriteLine(oSession.ToString());
            string referer = "null";
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
            }

            //mainForm.WriteLine(oSession.id.ToString() + ": " + referer);
            //mainForm.WriteLine(oSession.id.ToString() + ": " + oSession.ToString());
            mainForm.WriteLine(oSession.id.ToString() + ": " + oSession.RequestMethod + " " + oSession.fullUrl);

            // testing
            //mainForm.WriteLine(oSession.oRequest.headers.Exists("referer") ? "referer" + oSession.oRequest.headers["referer"] : "referer: null");
            //mainForm.WriteLine("requestHeaders.tostring(): " + oSession.RequestHeaders.ToString());
            //mainForm.WriteLine("osession.url: " + oSession.url);
            //mainForm.WriteLine("request.headers.tostring(): " + oSession.oRequest.headers.ToString());
            //mainForm.WriteLine("oSession.RequestBody(): " + oSession.RequestBody.ToString());
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
            if (s.url.EndsWith("." + removeTypes[0]) || s.url.EndsWith("." + removeTypes[1])
                || s.url.EndsWith("." + removeTypes[2]) || s.url.EndsWith("." + removeTypes[3])
                || s.url.EndsWith("." + removeTypes[4])) 
            {
                return true;
            }
            if (!(s.url.Contains(domains[0]) || s.url.Contains(domains[1])))
            {
                return true;
            }
            if (null != s.oResponse && null != s.oResponse.headers &&
                (s.oResponse.headers.ExistsAndEquals("content-type", "image/" + removeTypes[0])
                || s.oResponse.headers.ExistsAndEquals("content-type", "image/" + removeTypes[1])
                || s.oResponse.headers.ExistsAndEquals("content-type", "image/" + removeTypes[2])
                || s.oResponse.headers.ExistsAndEquals("content-type", "image/" + removeTypes[3])
                || s.oResponse.headers.ExistsAndEquals("content-type", "image/" + "x-icon")))
            {
                return true;
            }
            return false;
        }
    }
}
