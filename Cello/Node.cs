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
            //Debug.Assert(null != t);

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
            //Parents.Sort(delegate(ref Session t1, ref Session t2)
            //{ 
            //    return t1.id <= t2.id ? -1 : 1; 
            //});
            //Parents = Parents.OrderBy<Session, int>((ref s) => s.id).ToList();
            //Parents.OrderBy<Session, int>(delegate(ref Session s)
            //{
            //    return s.id;
            //});

            //Parents = Parents.OrderBy<Session, int>(s => s.id).ToList();
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
            //Children = Children.OrderBy<Session, int>(s => s.id).ToList();
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
                //Parents.Remove(Parents.Find(o => o.id == s.id));
                Parents.Remove(Parents.Find(o => o.ID == n.ID));
                return true;
            }
        }

        public bool remove_child(Node n)
        {
            Debug.Assert(null != n);

            lock (syncObject)
            {
                //Children.Remove(Children.Find(o => o.id == s.id));
                Children.Remove(Children.Find(o => o.ID == n.ID));
                return true;
            }
        }

        //public override bool Equals(object obj)
        //{
        //    if (null == obj)
        //        return false;

        //    GenNode<T> node = obj as GenNode<T>;
        //    if (null == node)
        //        throw new ArgumentException("object is not a GenNode<T> type");

        //    return 0 == Compare(this, node);
        //    //return base.Equals(node);
        //}

        //public bool Equals(GenNode<T> node)
        //{
        //    if (null == node)
        //        return false;

        //    return 0 == Compare(this, node);
        //}

        //public override int GetHashCode()
        //{
        //    return Data.GetHashCode();
        //}

        //public int CompareTo(object obj)
        //{
        //    if (null == obj)
        //        return 1;

        //    GenNode<T> node = obj as GenNode<T>;
        //    if (null != node)
        //        return Compare(this, node);
        //    else
        //        throw new ArgumentException("object is not a GenNode<T> type");
        //}

        //public int CompareTo(GenNode<T> node)
        //{
        //    if (null == node)
        //        return 1;

        //    return Compare(this, node);
        //}
    }
}