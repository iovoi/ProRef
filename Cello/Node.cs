using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Fiddler;
using System.Diagnostics;

namespace Cello
{
    public class Node
    {
        // lock to ensure thread safe
        private readonly static object syncObject = new object();

        private int id;
        public int ID 
        { 
            get 
            { 
                lock (syncObject) 
                { 
                    return id; 
                } 
            }

            set
            {
                lock (syncObject)
                {
                    id = value;
                }
            }
        }

        private List<Node> parents = new List<Node>();
        public List<Node> Parents 
        { 
            get 
            { 
                lock (syncObject)
                {
                    return parents;
                }
            } 
            set 
            {
                lock (syncObject)
                {
                    if (null != value) parents = value;
                }
            } 
        }

        private List<Node> children = new List<Node>();
        public List<Node> Children 
        { 
            get 
            {
                lock (syncObject)
                {
                    return children;
                }
            } 
            set 
            {
                lock (syncObject)
                {
                    if (null != value) children = value;
                }
            } 
        }

        // all three parameters can not be null
        public Node(int t, Node t_parent, Node t_child)
        {
            Debug.Assert(null != t && null != t_parent && null != t_child);

            lock (syncObject)
            {
                ID = t;
                Parents.Add(t_parent);
                Children.Add(t_child);
            }
        }

        public Node(int t)
        {
            lock (syncObject)
            {
                ID = t;
            }
        }

        public bool update_parent(Node t_parent)
        {
            Debug.Assert(null != t_parent);

            lock (syncObject)
            {
                Parents.Add(t_parent);
                return update_parent();
            }
        }

        public bool update_children(Node t_child)
        {
            Debug.Assert(null != t_child);

            lock (syncObject)
            {
                Children.Add(t_child);
                return update_children();
            }
        }

        protected bool update_parent()
        {
            Parents.Sort(delegate(Node entry1, Node entry2)
            {
                if (entry1.ID == entry2.ID)
                    return 0;
                else if (entry1.ID > entry2.ID)
                    return 1;
                else
                    return -1;
            });
            return true;
        }

        protected bool update_children()
        {
            Children.Sort(delegate(Node entry1, Node entry2)
            {
                if (entry1.ID == entry2.ID)
                    return 0;
                else if (entry1.ID > entry2.ID)
                    return 1;
                else
                    return -1;
            });
            return true;
        }

        public bool remove_parent(Node n)
        {
            Debug.Assert(null != n);

            lock (syncObject)
            {
                Parents.Remove(Parents.Find(o => o.ID == n.ID));
                return true;
            }
        }

        public bool remove_child(Node n)
        {
            Debug.Assert(null != n);

            lock (syncObject)
            {
                Children.Remove(Children.Find(o => o.ID == n.ID));
                return true;
            }
        }
    }
}