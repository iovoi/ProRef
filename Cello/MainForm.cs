using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Cello
{
    public partial class MainForm : Form
    {
        private static Proxy proxy = null;
        public MainForm()
        {
            InitializeComponent();

            this.FormClosing += new System.Windows.Forms
                .FormClosingEventHandler(this.MainForm_FormClosing);

            if (null == proxy)
            {
                proxy = new Proxy(this);
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            proxy.Start();
        }

        public bool WriteLine(string s)
        {
            BeginInvoke((Action<string>)((str) =>
                {
                    try
                    {
                        TerminalTextBox.AppendText(str + "\r\n");
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show(e.ToString());
                    }
                }), s);
            //this.TerminalTextBox.AppendText(s + "\r\n");
            return true;
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            proxy.Stop();
            //MessageBox.Show("Fiddler proxy stopped");
        }

        public void alert_message(string s)
        {
            MessageBox.Show(s);
        }
    }
}
