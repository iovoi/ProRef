using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//=================================
using System.Diagnostics;
using Fiddler;
//=================================

namespace Cello
{
    class Node
    {
        private Session data;
        public Session Data { get; set; }

        private List<Session> parents = new List<Session>();
        public List<Session> Parents { get; set; }

        private List<Session> children = new List<Session>();
        public List<Session> Children { get; set; }

        // all three parameters can not be null
        public Node(ref Session t, ref Session t_parent, ref Session t_child)
        {
            Data = t;
            Parents.Add(t_parent);
            Children.Add(t_child);
        }

        // all parameters can not be null
        public Node(ref Session t, ref Session t_parent)
        {
            Data = t;
            Parents.Add(t_parent);
        }

        public Node(ref Session t, ref Session t_child)
        {
            Data = t;
            Children.Add(t_child);
        }

        public Node(ref Session t)
        {
            Data = t;
        }

        public bool update_parent(ref Session t_parent)
        {
            Debug.Assert(null != t_parent); 
            Parents.Add(t_parent);

            return update_parent();
        }

        public bool update_children(ref Session t_child)
        {
            Debug.Assert(null != t_child);
            Children.Add(t_child);

            return update_children();
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

            Parents = Parents.OrderBy<Session, int>(s => s.id).ToList();
            return true;
        }

        protected bool update_children()
        {
            Children = Children.OrderBy<Session, int>(s => s.id).ToList();
            return true;
        }

        protected bool remove_parent(Session s)
        {
            Parents.Remove(Parents.Find(o => o.id == s.id));
            return true;
        }

        protected bool remove_child(Session s)
        {
            Children.Remove(Children.Find(o => o.id == s.id));
            return true;
        }
    }
}
