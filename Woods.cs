using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//===================================
using Fiddler;
using System.Diagnostics;
//===================================

namespace Cello
{
    public class Woods
    {
        // lock to ensure thread safe
        private readonly static object syncObject = new object();

        private SortedDictionary<int, Session> sessionDict = new SortedDictionary<int,Session>();
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

        private SortedDictionary<int, GenNode<int>> nodeDict = new SortedDictionary<int, GenNode<int>>();
        public SortedDictionary<int, GenNode<int>> NodeDict 
        { 
            get 
            {
                lock (syncObject)
                {
                    return nodeDict;
                }
            } 
        }

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

        public Woods() 
        {
            lock (syncObject)
            {
                // initialize GenNode<int>
                GenNode<int>.Compare = new Comparison<GenNode<int>>
                (delegate(GenNode<int> a, GenNode<int> b)
                {
                    return a.Data.CompareTo(b.Data);
                });
            }
        }

        public bool add(Session s)
        {
            Debug.Assert(null != s);

            lock (syncObject)
            {

                SessionDict.Add(s.id, s);
                GenNode<int> n = new GenNode<int>(s.id);
                NodeDict.Add(s.id, n);

                GenNode<int> parent = get_parent(s);
                if (null != parent)
                {
                    parent.Children.Add(n);

                    Debug.Assert(1 == n.Parents.Count);

                    n.Parents.Add(parent);

                    Roots[get_root(parent).Data]++;

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

        public GenNode<int> get_root(GenNode<int> node)
        {
            Debug.Assert(1 >= node.Parents.Count);

            GenNode<int> p = node;
            while (p.Parents.Count != 0)
            {
                Debug.Assert(1 >= p.Parents.Count);

                p = node.Parents[0];
            }

            return p;
        }

        // parent: find either an 302 that match the current address or session that match current referer
        // children: if current response is 302, find the nearest session (one and only one) whose address is exactly matched
        //           else find the sessions whose address match current address
        protected GenNode<int> get_parent(Session s)
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
                        && tempSession.oResponse.headers.ExistsAndEquals("Location", s.fullUrl))
                    {
                        return NodeDict[i];
                    }
                    else if (tempSession.fullUrl.Equals(s.oRequest.headers["Referer"]))
                    {
                        return NodeDict[i];
                    }
                }
                return null;
            }
            else
            {
                return null;
            }
        }
    }
}
