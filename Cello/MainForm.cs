﻿using System;
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
using System.Threading;
using System.Runtime.Remoting.Messaging;
//==================================

namespace Cello
{
    public partial class MainForm : Form
    {
        public static Proxy proxy = null;
        public Form diff_Form = null;

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
            //treeView1.NodeMouseClick += GetNodeOnClick;
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

        //public delegate void updateTree(SortedDictionary<int, Session> sD, SortedDictionary<int, Node> nD, SortedList<int, int> r, ref TreeView treeView1); 

        public bool Add2TreeView(SortedDictionary<int, Session> sessionDict, 
            SortedDictionary<int, Node> nodeDict, SortedList<int, int> roots)
        {
            if (this.Created)
            {

                Debug.Assert(null != sessionDict && null != nodeDict && null != roots);
                Object[] param = new Object[3] { sessionDict, nodeDict, roots };

                Action<SortedDictionary<int, Session>, SortedDictionary<int, Node>, 
                    SortedList<int, int>> action = (sD, nD, r) =>
                //updateTree action = (SortedDictionary<int, Session> sD, SortedDictionary<int, Node> nD, SortedList<int, int> r, ref TreeView treeView1) =>
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
                //IAsyncResult result = action.BeginInvoke(sessionDict, nodeDict, roots, ref this.treeView1, new AsyncCallback(Add2TreeView_AsyncCallBack), null);

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
                Action action = delegate()
                {
                    /*//TreeView tv = sender as TreeView;
                    using (System.IO.StreamWriter file
                        = new System.IO.StreamWriter(
                            "D:\\data\\workspace\\VisualStudioProject\\Cello\\output\\" 
                            + DateTime.Now.ToString("yyyyMMddHHmmss") + ".txt"))
                    {
                        //file.WriteLine(e.Node.Name);
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
                    }*/
                    proxy.request_chain = new List<Session>();
                    proxy.page_sessionID_chain = new List<int>();
                    //List<int> request_session_ids = new List<int>();
                    List<int> request_session_ids = proxy.page_sessionID_chain;
                    int session_id;
                    if (Int32.TryParse(e.Node.Name, out session_id))
                    {
                        Node node = proxy.SessionWoods.NodeDict[session_id];
                        Session session = proxy.SessionWoods.SessionDict[session_id];
                        //WriteLine(node.ID.ToString());
                        while (null != node)
                        {
                            proxy.request_chain.Insert(0, session);
                            request_session_ids.Insert(0, node.ID);
                            node = proxy.SessionWoods.get_parent(session);
                            if (null != node)
                            {
                                session = proxy.SessionWoods.SessionDict[node.ID];
                                //WriteLine(node.ID.ToString());
                            }
                        }
                    }
                    else
                    {
                        throw new ArgumentException("Node ID invalid");
                    }

                    string requestFile2Write = ".\\output\\"
                            + "request_" + DateTime.Now.ToString("yyyyMMddHHmmss"); 
                    string scriptFile2Write = ".\\output\\"
                            + "script_" + DateTime.Now.ToString("yyyyMMddHHmmss"); 
                    int num = 0;
                    foreach (Session s in proxy.request_chain)
                    {
                        //s.SaveSession(file2Write + num.ToString() + ".txt", false);
                        //s.SaveRequest(file2Write + num.ToString() + ".txt" , false);
                        /*WriteLine("s.oRequest.host: " + s.oRequest.host);
                        WriteLine("s.host: " + s.host);
                        WriteLine("s.hostname: " + s.hostname);
                        WriteLine("s.RequestMethod: " + s.RequestMethod);
                        WriteLine("s.port: " + s.port);
                        WriteLine("s.fullUrl: " + s.fullUrl);
                        WriteLine("s.RequestHeaders.HTTPVersion: " + s.RequestHeaders.HTTPVersion);
                        foreach (HTTPHeaderItem httpHeaderItem in s.RequestHeaders)
                            WriteLine("httpHeaderItem.Name: " + httpHeaderItem.Name);*/
                        s.SaveRequest(requestFile2Write + num.ToString() + ".txt" , false, true);
                        num++;
                        //WriteLine("writing s to file");
                    }
                    //proxy.MakeRequest();
                    for (int req_i = 0; req_i < request_session_ids.Count() - 1; req_i++)
                    {
                        Node cur = proxy.SessionWoods.NodeDict[request_session_ids[req_i]];
                        List<Node> children_list = cur.Children;
                        foreach (Node cur_node in children_list)
                        {
                            if (cur_node.ID != request_session_ids[req_i])
                            {
                                proxy.SessionWoods.SessionDict[cur_node.ID].SaveSession(scriptFile2Write + proxy.SessionWoods.SessionDict[cur_node.ID].fullUrl.Replace("?", "/").Split(new char [] {'/'}).Last(), false);
                                //WriteLine(proxy.SessionWoods.SessionDict[cur_node.ID].fullUrl.Replace("?", "/").Split(new char [] {'/'}).Last());
                            }
                        }
                    }
                };
                BeginInvoke(action);

                this.diff_Form = new DifferentialForm(proxy);
                diff_Form.Show();
            }
            else if (MouseButtons.Left == e.Button)
            {
                Action action = delegate()
                {
                    proxy.request_chain = new List<Session>();
                    proxy.page_sessionID_chain = new List<int>();
                    List<int> request_session_ids = proxy.page_sessionID_chain;
                    int session_id;
                    if (Int32.TryParse(e.Node.Name, out session_id))
                    {
                        Node node = proxy.SessionWoods.NodeDict[session_id];
                        Session session = proxy.SessionWoods.SessionDict[session_id];
                        proxy.request_chain.Insert(0, session);
                        request_session_ids.Insert(0, node.ID);
                    }
                    else
                    {
                        throw new ArgumentException("Node ID invalid");
                    }

                    string requestFile2Write = ".\\output\\"
                            + "request_" + DateTime.Now.ToString("yyyyMMddHHmmss");
                    string scriptFile2Write = ".\\output\\"
                            + "script_" + DateTime.Now.ToString("yyyyMMddHHmmss");
                    int num = 0;
                    foreach (Session s in proxy.request_chain)
                    {
                        s.SaveSession(requestFile2Write + num.ToString() + ".txt", false);
                        num++;
                    }
                };
                BeginInvoke(action);

                this.diff_Form = new DifferentialForm(proxy);
                diff_Form.Show();
            }
        }

        /*public void Add2TreeView_AsyncCallBack(IAsyncResult iar)
        {
            AsyncResult result = (AsyncResult)iar;
            updateTree caller = (updateTree)result.AsyncDelegate;
            caller.EndInvoke(ref this.treeView1, result);
        }*/

        public void GetNodeOnClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (MouseButtons.Right == e.Button)
            {
                Action action = delegate()
                {
                    proxy.request_chain = new List<Session>();
                    proxy.page_sessionID_chain = new List<int>();
                    List<int> request_session_ids = proxy.page_sessionID_chain;
                    int session_id;
                    if (Int32.TryParse(e.Node.Name, out session_id))
                    {
                        Node node = proxy.SessionWoods.NodeDict[session_id];
                        Session session = proxy.SessionWoods.SessionDict[session_id];
                        proxy.request_chain.Insert(0, session);
                        request_session_ids.Insert(0, node.ID);
                    }
                    else
                    {
                        throw new ArgumentException("Node ID invalid");
                    }
                };
                BeginInvoke(action);

                this.diff_Form = new DifferentialForm(proxy);
                diff_Form.Show();
            }
        }

    }
}
