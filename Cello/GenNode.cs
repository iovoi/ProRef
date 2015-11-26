using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//==================================
using Fiddler;
using System.Diagnostics;
//==================================

namespace Cello
{
    class GenNode<T> : IComparable<GenNode<T>> 
        //where T : 
    {
        private T data;
        public T Data { get; set; }

        private static Comparison<GenNode<T>> compare;
        public static Comparison<GenNode<T>> Compare { get; set; }

        private List<GenNode<T>> parents = new List<GenNode<T>>();
        public List<GenNode<T>> Parents { get; set; }

        private List<GenNode<T>> children = new List<GenNode<T>>();
        public List<GenNode<T>> Children { get; set; }

        // all three parameters can not be null
        public GenNode(ref T t, ref GenNode<T> t_parent, ref GenNode<T> t_child)
        {
            Debug.Assert(null != t && null != t_parent && null != t_child);

            Data = t;
            Parents.Add(t_parent);
            Children.Add(t_child);
        }

        // all parameters can not be null
        //public GenNode(ref T t, ref GenNode<T> t_parent)
        //{
        //    Data = t;
        //    Parents.Add(t_parent);
        //}

        //public GenNode(ref T t, ref GenNode<T> t_child)
        //{
        //    Data = t;
        //    Children.Add(t_child);
        //}

        public GenNode(ref T t)
        {
            Debug.Assert(null != t);

            Data = t;
        }

        public bool update_parent(ref GenNode<T> t_parent, Comparison<GenNode<T>> c)
        {
            Debug.Assert(null != t_parent); 

            Parents.Add(t_parent);
            return update_parent(c);
        }

        public bool update_children(ref GenNode<T> t_child, Comparison<GenNode<T>> c)
        {
            Debug.Assert(null != t_child);

            Children.Add(t_child);
            return update_children(c);
        }

        protected bool update_parent(Comparison<GenNode<T>> c)
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
            Parents.Sort(c);
            return true;
        }

        protected bool update_children(Comparison<GenNode<T>> c)
        {
            //Children = Children.OrderBy<Session, int>(s => s.id).ToList();
            Children.Sort(c);
            return true;
        }

        public bool remove_parent(GenNode<T> s)
        {
            //Parents.Remove(Parents.Find(o => o.id == s.id));
            Parents.Remove(Parents.Find(o => o.Equals(s)));
            return true;
        }

        public bool remove_child(GenNode<T> s)
        {
            //Children.Remove(Children.Find(o => o.id == s.id));
            Children.Remove(Children.Find(o => o.Equals(s)));
            return true;
        }

        public override bool Equals(object obj)
        {
            if (null == obj)
                return false;

            GenNode<T> node = obj as GenNode<T>;
            if (null == node)
                throw new ArgumentException("object is not a GenNode<T> type");

            return 0 == Compare(this, node);
            //return base.Equals(node);
        }

        public bool Equals(GenNode<T> node)
        {
            if (null == node)
                return false;

            return 0 == Compare(this, node);
        }

        public int CompareTo(object obj)
        {
            if (null == obj)
                return 1;

            GenNode<T> node = obj as GenNode<T>;
            if (null != node)
                return Compare(this, node);
            else
                throw new ArgumentException("object is not a GenNode<T> type");
        }

        public int CompareTo(GenNode<T> node)
        {
            if (null == node)
                return 1;

            return Compare(this, node);
        }
    }
}
