using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Cello.Utils;
using Fiddler;

namespace Cello
{
    public partial class DifferentialForm : Form
    {
        public Proxy proxy = null;
        /*private Object lockObject = new Object();
        public TextBox outputTextBox
        {
            get
            {
                lock (lockObject)
                {
                    return this.OutputTextBox;
                }
            }

            private set;
        }*/

        public DifferentialForm(Proxy p)
        {
            InitializeComponent();

            OutputTextBox.MouseDoubleClick += Start2Fuzz;

            this.proxy = p;
        }

        private void DifferentialForm_Load(object sender, EventArgs e)
        {

        }

        public bool WriteLine(string s)
        {
            if (this.Created)
            {
                BeginInvoke((Action<string>)((str) =>
                    {
                        try
                        {
                            OutputTextBox.AppendText(str + "\r\n");
                        }
                        catch (Exception e)
                        {
                            MessageBox.Show(e.ToString());
                        }
                    }), s);
                return true;
            }
            else
                return false;
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                WriteLine("Operation calceled!");
            }
            else if (null != e.Error)
            {
                string s = String.Format("An error occurred: {0}", e.Error.Message);
                WriteLine(s);
            }
            else
            {
                WriteLine(String.Format("Result: {0}", e.Result));
            }
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker backgroundWorker = sender as BackgroundWorker;
            Proxy proxy = (Proxy)e.Argument;
            WriteLine("Fuzzing started...");
            e.Result = RefineTraces(backgroundWorker, proxy);

            if (backgroundWorker.CancellationPending)
            {
                e.Cancel = true;
            }
        }

        public string RefineTraces(BackgroundWorker backgroundWorker, Proxy proxy)
        {
            List<int> reversed_page_sessionID_chain = new List<int>(proxy.page_sessionID_chain);
            reversed_page_sessionID_chain.Reverse();
            foreach (int session_id in reversed_page_sessionID_chain)
            {
                // ToDo:
                RequestDetails current_request_details = new RequestDetails(proxy.SessionWoods.SessionDict[session_id]);
                DifferentialRequest current_differentialRequest = new DifferentialRequest(current_request_details, this);
                current_differentialRequest.Fire_differential_request();

                SessionData session2write = new SessionData(proxy.SessionWoods.SessionDict[session_id]);

                using (System.IO.StreamWriter file = 
                    new System.IO.StreamWriter("D:\\data\\workspace\\VisualStudioProject\\Cello\\output\\refine_" 
                        + DateTime.Now.ToString("yyyyMMddHHmmss") + "_" + session_id + ".txt"))
                {
                    //proxy.SessionWoods.SessionDict[session_id].WriteRequestToStream(false, true, true, file);
                    file.Write(System.Text.Encoding.Default.GetString(session2write.arrRequest));
                    file.WriteLine("\n==========================================================");
                    file.WriteLine("");
                    file.WriteLine("");

                    file.WriteLine(current_differentialRequest.requestMethod + " " + current_differentialRequest.url + " " + "HTTP/" + current_differentialRequest.protocolVersion);
                    foreach (HTTPHeaderItem httpHeaderItem in current_differentialRequest.headers)
                    {
                        if (!"Cookie".Equals(httpHeaderItem.Name))
                        {
                            file.WriteLine(httpHeaderItem.Name + ": " + httpHeaderItem.Value);
                        }
                        else
                        {
                            int index = 0;
                            string cookieString = string.Empty;
                            foreach (KeyValuePair<string, string> cookieNameValue in current_differentialRequest.remaining_cookies)
                            {
                                if (0 == index)
                                {
                                    cookieString = cookieNameValue.Key + "=" + cookieNameValue.Value;
                                }
                                else
                                {
                                    cookieString += "; " + cookieNameValue.Key + "=" + cookieNameValue.Value;
                                }
                                index++;
                            }
                            if (cookieString.Length > 0)
                            {
                                file.WriteLine("Cookie: " + cookieString);
                            }
                        }
                    }

                    if ("POST".Equals(current_differentialRequest.requestMethod))
                    {
                        file.WriteLine("");

                        string body2write = string.Empty;

                        int index = 0;
                        foreach (KeyValuePair<string, string> bodyNameValue in current_differentialRequest.remaining_bodyParam)
                        {
                            if (0 == index)
                            {
                                body2write = bodyNameValue.Key + "=" + bodyNameValue.Value;
                            }
                            else
                            {
                                body2write += "&" + bodyNameValue.Key + "=" + bodyNameValue.Value;
                            }
                            index++;
                        }
                        if (body2write.Length > 0)
                        {
                            file.WriteLine(body2write);
                        }
                    }   
                }
            }


            return "Refinement finished...";
        }

        public void Start2Fuzz(Object sender, MouseEventArgs e)
        {
            if (MouseButtons.Left == e.Button)
            {
                this.backgroundWorker1.RunWorkerAsync(proxy);
            }
            else if (MouseButtons.Right == e.Button)
            {
                this.backgroundWorker1.CancelAsync();
            }
        }
    }
}
