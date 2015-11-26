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
    class Wood
    {
        private SortedDictionary<int, Session> sessionDict = new SortedDictionary<int,Session>();
        public SortedDictionary<int, Session> SessionDict { get; set; }

        private SortedDictionary<int, GenNode<int>> nodeDict = new SortedDictionary<int, GenNode<int>>();
        public SortedDictionary<int, GenNode<int>> NodeDict { get; }

        private List<GenNode<int>> roots = new List<GenNode<int>>();
        public List<GenNode<int>> Roots 
        {
            get;
            //set 
            //{ 
            //    SessionDict.Add(value.Data.id, value); 
            //    Root = SessionDict[value.Data.id]; 
            //} 
        }

        public Wood() 
        { 
            // initialize GenNode<int>
            GenNode<int>.Compare = new Comparison<GenNode<int>>
            (delegate(GenNode<int> a, GenNode<int> b)
            {
                return a.Data.CompareTo(b.Data);
            });
        }

        public bool add(Session s)
        {
            Debug.Assert(null != s);

            SessionDict.Add(s.id, s);

            NodeDict.Add(s.id, new GenNode<int>(s.id));

            // there are totally 8 http methods: GET, HEAD, POST, PUT, DELETE, CONNECT, OPTIONS, TRACE
            if (n.Data.HTTPMethodIs("GET") || n.Data.HTTPMethodIs("POST"))
            {

                return true;
            }
            else
            {
                SessionDict.Add(n.Data.id, n);
                return true;
            }
        }

        protected Node get_parent(Node n)
        {
            
            return ;
        }
    }
}
