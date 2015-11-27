using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
//==================================
using Fiddler;
//==================================

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
                        OutputTextBox.AppendText(str + "\r\n");
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show(e.ToString());
                    }
                }), s);
            //this.TerminalTextBox.AppendText(s + "\r\n");
            return true;
        }

        public bool Add2TreeView(SortedDictionary<int, Session> sessionDict, SortedDictionary<int, Node> nodeDict, SortedDictionary<int, int> roots)
        {
            //if (null != parent)
            //{
            //    treeView1.Nodes
            //}
            //else
            //{

            //}
            //BeginInvoke((Action<string>)((str) =>
            //    {
            //        try
            //        {

            //            treeView1.Nodes.Add(child.url);
            //            //treeView1.Update();
            //        }
            //        catch (Exception e)
            //        {
            //            MessageBox.Show(e.ToString());
            //        }
            //    }), child.url);

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
