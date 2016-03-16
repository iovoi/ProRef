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
using System.Diagnostics;
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

            treeView1.NodeMouseDoubleClick += write2file_OnDoubleClick;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            proxy.Start();
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

        public bool Add2TreeView(SortedDictionary<int, Session> sessionDict, 
            SortedDictionary<int, Node> nodeDict, SortedList<int, int> roots)
        {
            if (this.Created)
            {

                Debug.Assert(null != sessionDict && null != nodeDict && null != roots);
                Object[] param = new Object[3] { sessionDict, nodeDict, roots };

                Action<SortedDictionary<int, Session>, SortedDictionary<int, Node>, 
                    SortedList<int, int>> action = (sD, nD, r) =>
                {
                    try
                    {
                        SortedDictionary<int, Session> sDict = sD;
                        SortedDictionary<int, Node> nDict = nD;
                        SortedList<int, int> rDict = r;
                        if (null == sDict || null == nDict || null == rDict)
                            throw new ArgumentException("argument error");

                        //Queue<int> traverseQueue = new Queue<int>(rDict.Keys.ToList());
                        Queue<int> traverseQueue = new Queue<int>(proxy.SessionWoods.Roots.Keys.ToList());
                        treeView1.Nodes.Clear();
                        TreeNodeCollection current = treeView1.Nodes;
                        Queue<TreeNode> treeViewNodeQueue = new Queue<TreeNode>();
                        int node_i = 0;
                        foreach (int n in traverseQueue)
                        {
                            //current.Add(sDict[n].id.ToString() + ": " + sDict[n].RequestMethod + " " + sDict[n].fullUrl);
                            current.Add(proxy.SessionWoods.SessionDict[n].id.ToString() + ": " 
                                + proxy.SessionWoods.SessionDict[n].RequestMethod + " " +proxy.SessionWoods.SessionDict[n].fullUrl);
                            // set the id of the session the treeViewItem representing
                            current[node_i].Name = n.ToString();
                            treeViewNodeQueue.Enqueue(current[node_i]);
                            node_i++;
                        }

                        while (traverseQueue.Count > 0)
                        {
                            int head = traverseQueue.Dequeue();
                            TreeNode t = treeViewNodeQueue.Dequeue();
                            current = t.Nodes;
                            node_i = 0;
                            //foreach (Node n in nDict[head].Children)
                            //foreach (Node n in proxy.SessionWoods.NodeDict[head].Children)
                            List<Node> childList = new List<Node>(proxy.SessionWoods.NodeDict[head].Children);
                            foreach (Node n in childList)
                            {
                                //current.Add(sDict[n.ID].id.ToString() + ": " + sDict[n.ID].RequestMethod + " " + sDict[n.ID].fullUrl);
                                current.Add(proxy.SessionWoods.SessionDict[n.ID].id.ToString() + ": " 
                                    + proxy.SessionWoods.SessionDict[n.ID].RequestMethod + " " + proxy.SessionWoods.SessionDict[n.ID].fullUrl);
                                // set the id of the session the treeViewItem representing
                                current[node_i].Name = n.ID.ToString();
                                traverseQueue.Enqueue(n.ID);
                                treeViewNodeQueue.Enqueue(current[node_i]);
                                node_i++;
                            }
                        }

                    }
                    catch (Exception e)
                    {
                        MessageBox.Show(e.ToString());
                    }
                };
                BeginInvoke(action, sessionDict, nodeDict, roots);

                return true;
            }
            else
                return false;
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            //MessageBox.Show("Fiddler proxy stopped");
            proxy.Stop();
        }

        public void alert_message(string s)
        {
            MessageBox.Show(s);
        }

        public void write2file_OnDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (MouseButtons.Right == e.Button)
            {
                /*TreeView tv = sender as TreeView;
                using (System.IO.StreamWriter file
                    = new System.IO.StreamWriter(
                        "D:\\data\\workspace\\VisualStudioProject\\Cello\\output\\" 
                        + DateTime.Now.ToString("yyyyMMddHHmmss") + ".txt"))
                {
                    //file.WriteLine(e.Node.Name);
                    int root_id;
                    if (Int32.TryParse(e.Node.Name, out root_id))
                    {
                    }
                    else
                    {
                        throw new ArgumentException("Node ID invalid");
                    }
                }*/
                int root_id;
                if (Int32.TryParse(e.Node.Name, out root_id))
                {
                    Session rootSession = proxy.SessionWoods.SessionDict[root_id];
                    rootSession.SaveSession("D:\\data\\workspace\\VisualStudioProject\\Cello\\output\\"
                        + DateTime.Now.ToString("yyyyMMddHHmmss") + ".txt", false);
                }
                else
                {
                    throw new ArgumentException("Node ID invalid");
                }
           }
        }
    }
}
