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
                RequestDetails current_request = new RequestDetails(proxy.SessionWoods.SessionDict[session_id]);
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
