using System;
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
        }

        public Woods(MainForm m)
        {
            mainForm = m;
        }

        public bool add(Session s)
        {
            Debug.Assert(null != s);
            Debug.Assert(!SessionDict.Keys.Contains(s.id));

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

                    // update the number of nodes that link to the root node
                    Roots[get_root(parent).ID]++;

                    //return true;
                }
                else
                {
                    Roots.Add(s.id, 1);
                    //return true;
                }

                return update();
            }
        }

        public Node get_root(Node node)
        {
            Debug.Assert(node.Parents.Count <= 1);

            lock (syncObject)
            {
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
        }

        // parent: Has zero or one. If no referer header, check the nearest 302 redirection address which match the current one.
        //         If no header and no 302 redirected address matched, no parent.
        //         If referer header exits, find either an 302 redirection address that match the current address 
        //         or session that match current referer, depends whichever is nearer.
        // children: Find until the next GET with the same url. If current response is 302, then it has only one child,
        //           and the child has the exactly matched url with the redirected url or no child
        //           (the nearest session, one and only one, whose address is exactly matched).
        //           If not 302 response, then find the sessions who has referer header 
        //           and whose referer header address match current address
        protected Node get_parent(Session s)
        {
            lock (syncObject)
            {
                //if (null != s.oRequest.headers && s.oRequest.headers.Exists("referer"))
                if (null != s.oRequest.headers)
                {
                    // there are totally 8 http methods: GET, HEAD, POST, PUT, DELETE, CONNECT, OPTIONS, TRACE
                    // we only excludes CONNECT here
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
                        else if (s.oRequest.headers.Exists("referer") 
                            && tempSession.fullUrl.Equals(s.oRequest.headers["Referer"]))
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
        }

        // update the root node to see whether they still don't have parent
        // some of the session maybe slower than the other, so after a while
        // the session complete and now the node might have parent and become 
        // not a root node anymore
        protected bool update()
        {
            lock (syncObject)
            {
                List<int> rootList = Roots.Keys.ToList();
                foreach (int k in rootList)
                {
                    Session rootSession = SessionDict[k];
                    Node rootNode = NodeDict[k];
                    Node parent = get_parent(rootSession);
                    if (null != parent)
                    {
                        parent.Children.Add(rootNode);

                        rootNode.Parents.Add(parent);
                        Debug.Assert(1 == rootNode.Parents.Count);

                        Roots[get_root(parent).ID] += Roots[k];
                        Roots.Remove(k);
                    }
                }
                if (mainForm.Created)
                    mainForm.Add2TreeView(SessionDict, NodeDict, Roots);
                return true;
            }
        }

    }
}
