//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;



namespace GarageGames.Torque.Core
{

    /// <summary>
    /// TorqueInterfaces are exposed by TorqueComponents for use by other TorqueComponents.
    /// An interface can contain arbitrary functionality and data.  Interfaces are
    /// looked up on a TorqueComponentInterface using typename and name, e.g: 
    /// TorqueInterface iface = obj.Components.FindInterface("float","rotation");
    /// </summary>
    public class TorqueInterface
    {
        #region Public properties, operators, constants, and enums

        /// <summary>
        /// The TorqueComponent which owns this interface.  This value can become invalidated
        /// and go to null if the object owning the component is unregistered.
        /// </summary>
        public TorqueComponent Owner
        {
            get { return _owner; }
        }



        /// <summary>
        ///  If owner has been wiped out we are no longer valid, otherwise we are.
        /// </summary>
        public bool IsValid
        {
            get { return _owner.Object != null; }
        }

        #endregion


        #region Private, protected, internal fields

        public TorqueSafePtr<TorqueComponent> _owner;

        #endregion
    }



    /// <summary>
    /// A TorqueInterface which wraps another object and exposes it via the Wrap property.
    /// This can be used to wrap a TorqueComponent or a .NET interface.
    /// </summary>
    /// <typeparam name="T">The object type to wrap.</typeparam>
    public class TorqueInterfaceWrap<T> : TorqueInterface where T : class
    {
        #region Public properties, operators, constants, and enums

        /// <summary>
        /// Returns a reference to the object being wrapped by this interface.
        /// </summary>
        public T Wrap
        {
            get
            {
                Assert.Fatal(Owner is T, "TorqueInterfaceWrap.Wrap_get - Owner not of type " + typeof(T));
                return Owner as T;
            }
        }

        #endregion
    }



    /// <summary>
    /// A TorqueInterface which exposes a variable of type T.
    /// </summary>
    /// <typeparam name="T">Type of variable to expose.</typeparam>
    abstract public class ValueInterface<T> : TorqueInterface
    {
        #region Public properties, operators, constants, and enums

        abstract public T Value { get; set; }
        abstract public T DefaultValue { get; }

        #endregion


        #region Public methods

        virtual public bool isLocked() { return false; }
        virtual public bool doLock(TorqueComponent locker) { return false; }
        virtual public bool doUnlock(TorqueComponent locker) { return true; }

        #endregion
    }



    /// <summary>
    /// A TorqueInterface which exposes a variable of type T and
    /// holds that value internally.  Generally, components which
    /// expose a variable do so by holding that variable in a
    /// ValueInPlaceInterface whereas components looking up an
    /// interface to a variable look it up as a ValueInterface.
    /// </summary>
    /// <typeparam name="T">Type of variable to expose.</typeparam>
    public class ValueInPlaceInterface<T> : ValueInterface<T>
    {
        #region Constructors

        public ValueInPlaceInterface() { }



        public ValueInPlaceInterface(T val)
        {
            _value = val;
        }

        #endregion


        #region Public properties, operators, constants, and enums

        override public T Value
        {
            get { return _value; }
            set { _value = value; }
        }



        public override T DefaultValue
        {
            get { return default(T); }
        }

        #endregion


        #region Public fields

        // Note: one of the rare times when we want a public field.
        // The reason is that otherwise we cannot pass by reference,
        // and for internal value interfaces this presents a problem.
        // Note that only the owner of the interface is likely to know
        // it as a ValueInPlaceInterface, so encapsulation still preserved.
        public T _value;

        #endregion
    }
}
