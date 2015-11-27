using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//====================================
using System.Diagnostics;
using Fiddler;
//====================================

//namespace Cello
//{
//    class Tree
//    {
//        //private SortedDictionary<int, GenNode<int>> sessionDict = new SortedDictionary<int,GenNode<int>>();
//        //public SortedDictionary<int, GenNode<int>> SessionDict { get; set; }
//        private List<int> sessionList = new List<int>();
//        public List<int> SessionList { get; set; }

//        private GenNode<int> root = null;
//        public GenNode<int> Root
//        {
//            get;
//            set
//            {
//                SessionDict.Add(value.Data, value);
//                root = SessionDict[value.Data];
//            }
//        }

//        public Tree(GenNode<int> n)
//        {
//            Debug.Assert(null != n);

//            Root = n;
//        }

//        public bool add(GenNode<int> n)
//        {
//            Debug.Assert(null != n);
//            Debug.Assert(null != Root);

//            //// there are totally 8 http methods: GET, HEAD, POST, PUT, DELETE, CONNECT, OPTIONS, TRACE
//            //if (n.Data.HTTPMethodIs("GET") || n.Data.HTTPMethodIs("POST"))
//            //{
//            //    return true;
//            //}
//            //else
//            //{
//            //    SessionDict.Add(n.Data.id, n);
//            //    return true;
//            //}


//        }

//        protected GenNode<int> get_parent(GenNode<int> n)
//        {

//            return;
//        }
//    }
//}





//namespace Cello
//{
//    class Tree
//    {
//        private SortedDictionary<int, Node> sessionDict;
//        public SortedDictionary<int, Node> SessionDict { get; set; }

//        private Node root = null;
//        public Node Root 
//        {
//            get;
//            set 
//            { 
//                SessionDict.Add(value.Data.id, value); 
//                Root = SessionDict[value.Data.id]; 
//            } 
//        }

//        public Tree(ref Node n)
//        {
//            Debug.Assert(null != n);

//            Root = n;
//        }

//        public bool add(ref Node n)
//        {
//            Debug.Assert(null != n);

//            // there are totally 8 http methods: GET, HEAD, POST, PUT, DELETE, CONNECT, OPTIONS, TRACE
//            if (n.Data.HTTPMethodIs("GET") || n.Data.HTTPMethodIs("POST"))
//            {

//                return true;
//            }
//            else
//            {
//                SessionDict.Add(n.Data.id, n);
//                return true;
//            }
//        }

//        protected Node get_parent(Node n)
//        {
            
//            return ;
//        }
//    }
//}
