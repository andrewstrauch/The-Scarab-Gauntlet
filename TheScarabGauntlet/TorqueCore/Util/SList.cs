//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using GarageGames.Torque.Core;
using System;



namespace GarageGames.Torque.Util
{
    /// <summary>
    /// Simple linked list implementation that pools nodes.
    /// </summary>
    /// <typeparam name="T">The type of values to use in the list.</typeparam>
    public class SList<T> : IDisposable
    {
        #region Static methods, fields, constructors

        /// <summary>
        /// Pushes a value onto the front of the list.
        /// </summary>
        /// <param name="head">The the list to add the new value to.</param>
        /// <param name="val">The value to add.</param>
        static public void InsertFront(ref SList<T> head, T val)
        {
            SList<T> link = _AllocNode();
            link.Val = val;
            link.Next = head;
            head = link;
        }

        /// <summary>
        /// Pops a value off of the front of the list.
        /// </summary>
        /// <param name="head">The list to remove the value from.</param>
        static public void RemoveFront(ref SList<T> head)
        {
            SList<T> newHead = head.Next;
            head.Next = null;
            _FreeNode(head);
            head = newHead;
        }

        /// <summary>
        /// Removs a value from the list.
        /// </summary>
        /// <typeparam name="S">The type of value to remove.</typeparam>
        /// <param name="head">The list to remove from.</param>
        /// <param name="val">The value to remove.</param>
        static public void Remove<S>(ref SList<S> head, S val) where S : class
        {
            if (head == null)
                return;

            if (head.Val == val)
            {
                SList<S>.RemoveFront(ref head);
                return;
            }

            SList<S> walk = head;
            while (walk.HasNext && walk.Next.Val != val)
                walk = walk.Next;

            if (walk.HasNext)
                walk.RemoveAfter();
        }

        /// <summary>
        /// Clears a list.
        /// </summary>
        /// <param name="head">The list to clear.</param>
        public static void ClearList(ref SList<T> head)
        {
            // Clear out all Val entries and hook the end of
            // our list up to free list (our head becomes new free
            // list head).
            SList<T> walk = head;
            while (walk != null)
            {
                walk.Val = default(T);
                if (walk.HasNext)
                {
                    walk = walk.Next;
                }

                else
                {
                    walk.Next = _freeList;
                    break;
                }
            }

            _freeList = head;
            head = null;
        }

        static void _FreeNode(SList<T> list)
        {
            Assert.Fatal(list._next == null, "SList._FreeNode - The node has not been removed from its list!");
            list.Val = default(T);
            list._next = _freeList;
            _freeList = list;
        }

        static SList<T> _AllocNode()
        {
            if (_freeList != null)
            {
                SList<T> ret = _freeList;
                _freeList = _freeList.Next;
                ret._next = null;
                return ret;
            }

            return new SList<T>();
        }

        // storage for allocated nodes that aren't in a list.
        static SList<T> _freeList = null;

        #endregion


        #region Public properties, operators, constants, and enums

        /// <summary>
        /// The value stored in this node.
        /// </summary>
        public T Val;

        /// <summary>
        /// The next node in the linked list.
        /// </summary>
        public SList<T> Next
        {
            get { return _next; }
            set { _next = value; }
        }

        /// <summary>
        /// Whether or not the node has a node following it in the list.
        /// </summary>
        public bool HasNext
        {
            get { return _next != null; }
        }

        #endregion


        #region Public methods

        /// <summary>
        /// Inserts a value in the list after this node.
        /// </summary>
        /// <param name="val">The value to insert.</param>
        public void InsertAfter(T val)
        {
            SList<T> link = _AllocNode();
            link.Val = val;
            link.Next = Next;
            Next = link;
        }

        /// <summary>
        /// Removes the node after this node from the list.
        /// </summary>
        public void RemoveAfter()
        {
            SList<T> remove = Next;
            if (remove == null)
                return;
            SList<T> next = remove.Next;

            Next = next;
            remove.Next = null;
            remove.Val = default(T);

            _FreeNode(remove);
        }

        /// <summary>
        /// Clears every node following this node in the list.
        /// </summary>
        public void ClearAfter()
        {
            ClearList(ref _next);
        }

        #endregion


        #region Private, protected, internal fields

        SList<T> _next;

        #endregion

        #region IDisposable Members

        public virtual void Dispose()
        {
            Val = default(T);
            _next = null;
        }

        #endregion
    }
}
