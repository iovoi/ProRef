using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fiddler;
using System.Security.Cryptography.X509Certificates;

namespace Cello
{
    public class Proxy
    {
        private MainForm mainForm = null;
        private FiddlerCoreStartupFlags proxy_config = FiddlerCoreStartupFlags.Default;

        public Proxy(MainForm form)
        {
            mainForm = form;

            proxy_config = (proxy_config | FiddlerCoreStartupFlags.DecryptSSL);
            proxy_config = (proxy_config | FiddlerCoreStartupFlags.RegisterAsSystemProxy);

            //FiddlerApplication.OnNotification += delegate(object sender, NotificationEventArgs oNEA)
            //{
            //    mainForm.alert_message("OnNotification: " + oNEA.NotifyString);
            //};
            //FiddlerApplication.Log.OnLogString += delegate(object sender, LogEventArgs oLEA)
            //{
            //    mainForm.alert_message("OnLogString: " + oLEA.LogString);
            //};

            //FiddlerApplication.Prefs.SetStringPref("fiddler.certmaker.bc.cert", "DO_NOT_TRUST_FiddlerRoot");
            //FiddlerApplication.Prefs.SetStringPref("fiddler.certmaker.bc.key", "9fe03dff02f4610b5ae7d38e19b36990_339cb36d-5481-4643-a543-55d8b0fbee76");

            //mainForm.alert_message(CertMaker.rootCertExists() + "");
            if (!Install_Certificate())
                mainForm.alert_message("install certificate failed");

            //X509Store certStore = new X509Store(StoreName.CertificateAuthority, StoreLocation.LocalMachine);
            //certStore.Open(OpenFlags.OpenExistingOnly);

            // set up listeners
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
            //mainForm.WriteLine("Press q to quit");

        }

        void FiddlerApplication_AfterSessionComplete(Session oSession)
        {
            mainForm.WriteLine(oSession.ToString());
        }

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
    }
}
