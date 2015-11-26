using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fiddler;
//using System.Security.Cryptography.X509Certificates;
using System.Collections;

namespace Cello
{
    public class Proxy
    {
        private MainForm mainForm = null;

        private FiddlerCoreStartupFlags proxy_config = FiddlerCoreStartupFlags.Default;

        private Hashtable sessionHashTable = new Hashtable();
        public Hashtable SessionHashTable { get; set; }

        private string[] domains = new string[2];
        private string[] removeTypes = new string[4];

        public Proxy(MainForm form)
        {
            mainForm = form;

            // set up what domains to capture and what types to remove
            // later should be modified to load from configuration file
            domains[0] = "facebook";
            domains[1] = "akamaihd";
            removeTypes[0] = "jpg";
            removeTypes[1] = "png";
            removeTypes[2] = "jpeg";
            removeTypes[3] = "gif";

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
            mainForm.WriteLine(oSession.ToString());

            // testing
            //mainForm.WriteLine(oSession.oRequest.headers.Exists("referer") ? "referer" + oSession.oRequest.headers["referer"] : "referer: null");
            //mainForm.WriteLine("requestHeaders.tostring(): " + oSession.RequestHeaders.ToString());
            //mainForm.WriteLine("osession.url: " + oSession.url);
            //mainForm.WriteLine("request.headers.tostring(): " + oSession.oRequest.headers.ToString());

            Session p = GetSessionParent(oSession);
            mainForm.Add2TreeView(p, oSession);
            SessionHashTable.Add(oSession.url, oSession);
            //mainForm.Add2TreeView(null, oSession);

        }

        protected Session GetSessionParent(Session s)
        {
            if (SessionHashTable.Count == 0 || !s.oRequest.headers.Exists("referer"))
            {
                return null;
            }
            else
            {
                //for (SessionHashTable.Contains(s.oRequest.headers))
                //IEnumerator entr =  sessionHashTable.GetEnumerator();
                //while (entr.MoveNext())
                //{
                //    Session cur = (Session)entr.Current;
                //    if (cur.oRequest.headers.Exists("referer"))
                //    {
                //        if ()
                //    }
                //}
                if (SessionHashTable.Contains(s.oRequest.headers["referer"]))
                    return (Session)SessionHashTable[s.oRequest.headers["referer"]];
                else
                    return null;
            }
        }

        // deprecated
        //public void wait2Stop()
        //{
        //    ConsoleKeyInfo keypress;
        //    keypress = Console.ReadKey();

        //    if (keypress.Key == ConsoleKey.Q)
        //    {
        //        this.Stop();
        //    }
        //}

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
                || s.url.EndsWith("." + removeTypes[2]) || s.url.EndsWith("." + removeTypes[3])) 
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
                || s.oResponse.headers.ExistsAndEquals("content-type", "image/" + removeTypes[3])))
            {
                return true;
            }
            return false;
        }
    }
}
