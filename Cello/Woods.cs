﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Fiddler;
using System.Diagnostics;

namespace Cello
{
    public class Woods
    {
        // lock to ensure thread safe
        private readonly static object syncObject = new object();

        // main ui form
        private MainForm mainForm = null;

        // used to hold all the sessions captured and using their ids as to identify them
        private SortedDictionary<int, Session> sessionDict = new SortedDictionary<int, Session>();
        public SortedDictionary<int, Session> SessionDict
        {
            get
            {
                lock (syncObject)
                {
                    return sessionDict;
                }
            }
        }

        // hold all the nodes corresponding to the session with id in the node
        private SortedDictionary<int, Node> nodeDict = new SortedDictionary<int, Node>();
        public SortedDictionary<int, Node> NodeDict
        {
            get
            {
                lock (syncObject)
                {
                    return nodeDict;
                }
            }
        }

        // the roots of the trees, first the root node id 
        // and the second is the number of node in the tree
        private SortedList<int, int> roots = new SortedList<int, int>();
        public SortedList<int, int> Roots
        {
            get
            {
                lock (syncObject)
                {
                    return roots;
                }
            }
            //set 
            //{ 
            //    SessionDict.Add(value.Data.id, value); 
            //    Root = SessionDict[value.Data.id]; 
            //} 
        }

        public Woods(MainForm m)
        {
            //lock (syncObject)
            //{
            //    // initialize GenNode<int>
            //    GenNode<int>.Compare = new Comparison<GenNode<int>>
            //    (delegate(GenNode<int> a, GenNode<int> b)
            //    {
            //        return a.Data.CompareTo(b.Data);
            //    });
            //}
            mainForm = m;
        }

        public bool add(Session s)
        {
            Debug.Assert(null != s);

            lock (syncObject)
            {

                SessionDict.Add(s.id, s);
                Node n = new Node(s.id);
                NodeDict.Add(s.id, n);

                Node parent = get_parent(s);
                if (null != parent)
                {
                    parent.Children.Add(n);

                    n.Parents.Add(parent);
                    Debug.Assert(1 == n.Parents.Count);

                    Roots[get_root(parent).ID]++;

                    return true;
                }
                else
                {
                    Roots.Add(s.id, 1);
                    return true;
                }
                //// there are totally 8 http methods: GET, HEAD, POST, PUT, DELETE, CONNECT, OPTIONS, TRACE
                //if (n.Data.HTTPMethodIs("GET") || n.Data.HTTPMethodIs("POST"))
                //{

                //    return true;
                //}
                //else
                //{
                //    SessionDict.Add(n.Data.id, n);
                //    return true;
                //}
            }
        }

        public Node get_root(Node node)
        {
            Debug.Assert(node.Parents.Count <= 1);

            Node p = node;
            Debug.Assert(p.Parents.Count <= 1);
            while (p.Parents.Count != 0)
            {
                Debug.Assert(1 == p.Parents.Count);
                p = p.Parents[0];
                Debug.Assert(p.Parents.Count <= 1);
            }

            return p;
        }

        // parent: find either an 302 that match the current address or session that match current referer
        // children: if current response is 302, find the nearest session (one and only one) whose address is exactly matched
        //           else find the sessions whose address match current address
        protected Node get_parent(Session s)
        {
            if (null != s.oRequest.headers && s.oRequest.headers.Exists("referer"))
            {
                //List<int> reverseKeyList = SessionDict.Keys.OrderByDescending(e => e).ToList();
                List<int> reverseKeyList = (from k in SessionDict.Keys
                                            //where k < s.id && !"connect".Equals(SessionDict[k].RequestMethod) 
                                            where k < s.id && !SessionDict[k].RequestMethod.Equals("CONNECT")
                                            select k).OrderByDescending(e => e).ToList();
                foreach (int i in reverseKeyList)
                {
                    Session tempSession = SessionDict[i];
                    if (tempSession.responseCode == 302
                        && null != tempSession.oResponse.headers
                        && tempSession.oResponse.headers.ExistsAndEquals("Location", s.fullUrl))
                    {
                        return NodeDict[tempSession.id];
                    }
                    else if (tempSession.fullUrl.Equals(s.oRequest.headers["Referer"]))
                    {
                        return NodeDict[tempSession.id];
                    }
                }
                return null;
            }
            else
            {
                return null;
            }
        }

        protected void update()
        {
            foreach (int k in Roots.Keys)
            {
                List<int> reverseKeyList = (from i in SessionDict.Keys 
                                            where i < k && !SessionDict[i].RequestMethod.Equals("CONNECT") 
                                            select i).OrderByDescending(e => e).ToList();

            }
        }
    }
}
