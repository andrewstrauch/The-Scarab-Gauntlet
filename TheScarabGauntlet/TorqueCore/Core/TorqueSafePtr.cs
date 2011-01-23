//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using GarageGames.Torque.Util;



namespace GarageGames.Torque.Core
{
    internal class TorqueRef : IDisposable
    {
        #region Public properties, operators, constants, and enums

        public TorqueBase Ref
        {
            get { return _ref; }
            set
            {
                Assert.Fatal(_ref == null || value == null, "TorqueRef.Ref_set - Ref or value must be null");

                if (value == null && _ref != null)
                    // clear out notifies
                    DoNotify();

                _ref = value;
            }

        }

        #endregion


        #region Public methods

        public void AddNotify(TorqueBase obj)
        {
            SList<TorqueRef> notify = _notify;

            while (notify != null)
            {
                if (notify.Val == obj.Ref)
                    // already on the list, not an error but don't duplicate entry
                    return;

                // check to see if next entry is empty...if so, get rid of it now
                // Note: can have empty entry when someone calls AddNotify but doesn't
                // call RemoveNotify when they are removed from engine (which is fine...
                // ...call RemoveNotify if you track your notifies, don't if you don't).
                while (notify.HasNext && notify.Next.Val.Ref == null)
                    notify.RemoveAfter();

                notify = notify.Next;
            }

            // add new entry
            SList<TorqueRef>.InsertFront(ref _notify, obj.Ref);
        }



        public void RemoveNotify(TorqueBase obj)
        {
            // Clearing notifies is completely optional and is only
            // generally expected from TorqueSet's because they already
            //track the items they reference.
            if (_notify == null)
                return;

            if (_notify.Val.Ref == obj)
            {
                SList<TorqueRef>.RemoveFront(ref _notify);
                return;
            }

            SList<TorqueRef> walk = _notify;

            while (walk.HasNext && walk.Next.Val.Ref != obj)
            {
                while (walk.HasNext && walk.Next.Val.Ref == null)
                    // clear out empty references
                    walk.RemoveAfter();

                walk = walk.Next;
            }

            if (walk.HasNext)
                walk.RemoveAfter();

            return;
        }



        public void DoNotify()
        {
            SList<TorqueRef> notify = _notify;

            while (notify != null)
            {
                TorqueBase obj = notify.Val.Ref as TorqueBase;

                if (obj != null)
                    obj.OnRemoveNotify(Ref);

                notify = notify.Next;
            }

            SList<TorqueRef>.ClearList(ref _notify);
        }

        #endregion


        #region Private, protected, internal fields

        TorqueBase _ref;
        SList<TorqueRef> _notify;

        #endregion

        #region IDisposable Members

        public virtual void Dispose()
        {
            if (_ref != null)
                _ref.Reset();
            _ref = null;
            _notify = null;
        }

        #endregion
    }



    /// <summary>
    /// A weak reference to an object of type TorqueBase.  For TorqueObjects,
    /// the value goes to null when the object is unregistered from the 
    /// TorqueObjectDatabase in which it was originally registered.
    /// </summary>
    /// <typeparam name="T">Type of safe pointer.</typeparam>
    public struct TorqueSafePtr<T> where T : TorqueBase
    {
        #region Static methods, fields, constructors

        public static implicit operator T(TorqueSafePtr<T> tptr)
        {
            return tptr.Object;
        }

        #endregion


        #region Public properties, operators, constants, and enums

        /// <summary>
        /// Object pointed to by safe pointer.
        /// </summary>
        public T Object
        {
            get { return _ref != null ? _ref.Ref as T : null; }
            set { _ref = value != null ? value.Ref : null; }
        }



        /// <summary>
        /// True if safe pointer has ever pointed to an object.
        /// </summary>
        public bool Initialized
        {
            get { return _ref != null; }
        }

        #endregion


        #region Private, protected, internal fields

        internal TorqueRef _ref;

        #endregion
    }

}
