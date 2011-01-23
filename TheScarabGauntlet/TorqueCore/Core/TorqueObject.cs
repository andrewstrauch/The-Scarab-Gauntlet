//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using System.Diagnostics;
using GarageGames.Torque.Util;
using System.ComponentModel;



namespace GarageGames.Torque.Core
{
    /// <summary>
    /// Stores data on a TorqueObject which can be retrieved using a TorqueCookieType.
    /// </summary>
    public struct TorqueCookie
    {
        #region Constructors

        public TorqueCookie(int val)
        {
            Value = val;
        }

        #endregion


        #region Public properties, operators, constants, and enums

        public int Value;

        #endregion
    }



    /// <summary>
    /// Optional delegate, fired when an object is added
    /// </summary>
    public delegate void OnRegisteredDelegate(TorqueBase obj);



    /// <summary>
    /// Base class which provides naming and object reference capability.
    /// TorqueBase can be looked up in a TorqueObjectDatabase by name.
    /// Reference allows them to be pointed at by TorqueSafePtr.
    /// </summary>
    public class TorqueBase : ObjectPooler.IResetable, IDisposable
    {
        #region Constructors

        public TorqueBase() { }

        #endregion


        #region Public properties, operators, constants, and enums

        /// <summary>
        /// Name of object.  Object can be looked up in TorqueObjectDatabase by name.
        /// </summary>
        [TorqueCloneIgnore]
        public string Name
        {
            get { return _name; }
            set
            {
                // cafTODO: this shoudn't be static...object case can get manager (from folder)
                // but what to do for things derived from TorqueBase but not TorqueObject?
                TorqueObjectDatabase.Instance.OnObjectNameUpdated(this, value);
                _name = value;
            }
        }


        /// <summary>
        /// Has the object been disposed.
        /// </summary>
        [BrowsableAttribute(false)]
        public bool IsDisposed
        {
            get { return _IsDisposed; }
        }

        /// <summary>
        /// Automatically generated name for object. AutoNames are not stored in the object name database, so you cannot find an object
        /// using this name. This exists purely for debugging purposes: when you are inspecting an object in the debugger, it makes it easier
        /// to tell what kind of object you are dealing with. This can be overriden in subclasses to provide more information in the name
        /// that the base implementation below. See T2DStaticSprite for an example override.
        /// </summary>
        [BrowsableAttribute(false)]
        [TorqueCloneIgnore]
        [XmlIgnore]
        public virtual string AutoName
        {
            get { return this.GetType().Name + "_" + this.GetHashCode(); }
        }

        #endregion


        #region Public methods

        /// <summary>
        /// Reset all references to this object.  In particular, TorqueSafePtr's pointing
        /// to us will be nulled out.
        /// </summary>
        public virtual void Reset()
        {
            _ResetRefs();
        }



        /// <summary>
        /// Callback for when object is loaded into engine (usually after deserialization)
        /// </summary>
        public virtual void OnLoaded() { }



        /// <summary>
        /// Callback for when object is unloaded from engine.
        /// </summary>
        public virtual void OnUnloaded()
        {
            // remove my name from the object database by setting it to empty.
            if (this.Name != string.Empty)
                this.Name = string.Empty;
        }



        /// <summary>
        /// Allows one object to be notified when another has it's
        /// references reset.
        /// </summary>
        /// <param name="notifyme">The object which receives the notification</param>
        public void RequestRemoveNotify(TorqueBase notifyme)
        {
            Ref.AddNotify(notifyme);
        }



        /// <summary>
        /// Cancel a RequestRemoveNotify.
        /// </summary>
        /// <param name="cancelNotify">Object which requested the notify.</param>
        public void CancelRemoveNotify(TorqueBase cancelNotify)
        {
            Ref.RemoveNotify(cancelNotify);
        }



        /// <summary>
        /// Called on an object who has requested to be notified of another object's removal
        /// by calling RequestRemoveNotify.
        /// </summary>
        /// <param name="removed">Object which was removed.</param>
        public virtual void OnRemoveNotify(TorqueBase removed) { }

        #endregion


        #region Private, protected, internal methods

        internal TorqueRef Ref
        {
            get
            {
                if (_ref == null)
                {
                    _ref = new TorqueRef();
                    _ref.Ref = this;
                }
                else if (_ref.Ref == null)
                    _ref.Ref = this;

                return _ref;
            }
        }



        protected void _ResetRefs()
        {
            if (_ref != null)
                _ref.Ref = null;

            _ref = null;
        }



        internal void _SetName(String name)
        {
            _name = name;
        }

        #endregion


        #region Private, protected, internal fields

        internal TorqueRef _ref;
        String _name = String.Empty;

        #endregion

        #region IDisposable Members

        protected bool _IsDisposed = false;

        public virtual void Dispose()
        {
            _IsDisposed = true;
            _ResetRefs();
        }

        #endregion
    }



    /// <summary>
    /// Central class for Torque X Engine.  TorqueObject's should be registered with
    /// TorqueObjectDatabase before use and unregistered after they are used.
    /// </summary>
    public class TorqueObject : TorqueBase, ICloneable, IDisposable
    {
        #region Public properties, operators, constants, and enums

        /// <summary>
        /// Bitfield representing various TorqueObject states.
        /// </summary>
        [Flags]
        public enum TorqueObjectFlags
        {
            None = 0,

            /// <summary>
            /// This object has been registered with the object system.
            /// </summary>
            Registered = 1,

            /// <summary>
            /// This object has been unregistered from the object system.
            /// </summary>
            Unregistered = 2,

            /// <summary>
            /// This object has been marked as a template object.
            /// </summary>
            Template = 4,

            /// <summary>
            /// This object is marked for deletion.
            /// </summary>
            DeleteWhenReady = 8,

            /// <summary>
            /// This object is stored in a pool when unregistered.
            /// </summary>
            Poolable = 16,

            /// <summary>
            /// This object is stored in a pool with all it's components still added.
            /// </summary>
            PoolWithComponents = 32,

            /// <summary>
            /// This object is not removed when the scene it was loaded with is unloaded.
            /// </summary>
            Persistent = 64,

            /// <summary>
            /// Mark an object as special in some way.  This can be used to short-cut time consuming
            /// processing.  Many systems can use this, so once an object is notched it cannot be cleared.
            /// </summary>
            Notch = 128,

            /// <summary>
            /// Mark an object as special in some way.  This can be used to short-cut time consuming
            /// processing.  Many systems can use this, so once an object is notched it cannot be cleared.
            /// This flag is reserved for internal use; see Notch flag for publicly available version.
            /// </summary>
            InternalNotch = 256,

            NoCloneFlags = Registered | Unregistered | Template | DeleteWhenReady | Notch | InternalNotch,
            LastObjectFlag = InternalNotch
        }



        /// <summary>
        /// True if object has a component container.
        /// </summary>
        [BrowsableAttribute(false)]
        public bool HasComponents
        {
            get { return (_components != null) && (_components.GetNumComponents() != 0); }
        }



        /// <summary>
        /// Container for TorqueComponents held by this object.  Generally want to test HasComponents property before
        /// accessing the component container since otherwise the container will be created if it doesn't exist already.
        /// </summary>
        [TorqueCloneIgnore]
        [BrowsableAttribute(false)]
        public TorqueComponentContainer Components
        {
            get
            {
                if (_components == null)
                    _components = Util.ObjectPooler.CreateObject<TorqueComponentContainer>();

                return _components;
            }
            internal set // this is ONLY for use by the XML deserializer. changing components on the fly will cause headaches for you. don't do it!
            {
                _components = value;
            }
        }



        /// <summary>
        /// Folder which this object is in. Every object which is added to the TorqueObjectDatabase belongs in
        /// exactly one folder. Objects can be moved between folders by setting this property. If this property
        /// is set to null it will fail silently.
        /// </summary>
        [TorqueCloneIgnore]
        [BrowsableAttribute(false)]
        [XmlIgnore]
        public TorqueFolder Folder
        {
            get { return _folder; }
            set
            {
                if (_folder == value)
                    // early out if we are already in new folder
                    return;

                if (value == null && IsRegistered)
                    // if we are added, cannot remove from all folders
                    return;

                if (_folder == this)
                    // don't move root folder (root is only folder which is inside itself)
                    return;

                if (_folder != null && !_folder._RemoveObject(this))
                {
                    // only way for remove to fail is if we aren't really in folder, so assert here
                    Assert.Fatal(false, "Object not in folder");
                    return;
                }

                _folder = value;

                if (_folder != null)
                    _folder._AddObject(this);
            }
        }



        /// <summary>
        /// The TorqueObjectDatabase which owns this object.
        /// </summary>
        [BrowsableAttribute(false)]
        public TorqueObjectDatabase Manager
        {
            // Folder tracks manager for us (this works even if we are a folder).
            get { return _folder != null ? _folder.Manager : null; }
        }



        /// <summary>
        /// The TorqueObjectType for this object.
        /// </summary>
        [Editor("TorqueXBuilder3D.TorqueObjectTypeUIConverter, TorqueXBuilder3D", typeof(System.Drawing.Design.UITypeEditor))]
        public TorqueObjectType ObjectType
        {
            get
            {
                TorqueObjectType objType = new TorqueObjectType();
                objType._bits = _objectType;
                objType._typeName = "unknown";

                return objType;
            }
            set
            {
                _objectType = value._bits;

                if (Manager != null)
                    _objectType |= Manager.GenericObjectType._bits;
            }
        }



        /// <summary>
        /// Integer id of this object assigned by TorqueObjectDatabase.
        /// </summary>
        [XmlIgnore]
        [BrowsableAttribute(false)]
        public uint ObjectId
        {
            get { return _id; }
        }



        /// <summary>
        /// True if object is registered with TorqueObjectDatabase.
        /// </summary>
        [XmlIgnore]
        [BrowsableAttribute(false)]
        public bool IsRegistered
        {
            get { return TestFlag(TorqueObjectFlags.Registered); }
        }



        /// <summary>
        /// True if object has been registered and unregistered with TorqueObjectDatabase.
        /// </summary>
        [BrowsableAttribute(false)]
        [XmlIgnore]
        public bool IsUnregistered
        {
            get { return TestFlag(TorqueObjectFlags.Unregistered); }
        }



        /// <summary>
        /// True if object pooling is turned on for the object.
        /// </summary>
        public bool Pool
        {
            get { return TestFlag(TorqueObjectFlags.Poolable); }
            set { _SetFlag(TorqueObjectFlags.Poolable, value); }
        }



        /// <summary>
        /// True if object pooling with components is turned on for the object. Pooling with components
        /// means that the object is stored in a pool with all it's components still added.
        /// </summary>
        public bool PoolWithComponents
        {
            get { return TestFlag(TorqueObjectFlags.PoolWithComponents); }
            set { _SetFlag(TorqueObjectFlags.PoolWithComponents, value); }
        }



        /// <summary>
        /// True if this object is a template. Templates cannot be registered with a
        /// object database, but must be cloned and the clone added.
        /// </summary>
        [TorqueCloneIgnore]
        public bool IsTemplate
        {
            get { return TestFlag(TorqueObjectFlags.Template); }
            set
            {
                if (!IsTemplate && value)
                    // making a template out of this...make sure we don't
                    // pool with other objects created with a template we
                    // were cloned from.
                    _sharedPool = null;

                _SetFlag(TorqueObjectFlags.Template, value);
            }
        }



        /// <summary>
        /// True if this object persists through level unloads.  If true, and if this object is loaded from a scene file,
        /// the object will not be unloaded when the scene file unloads.
        /// </summary>
        [TorqueCloneIgnore]
        public bool IsPersistent
        {
            get { return TestFlag(TorqueObjectFlags.Persistent); }
            set { _SetFlag(TorqueObjectFlags.Persistent, value); }
        }



        /// <summary>
        /// Mark an object as special in some way. This can be used to short-cut time consuming
        /// processing. Many systems can use this, so once an object is notched it cannot be cleared.
        /// </summary>
        [BrowsableAttribute(false)]
        [XmlIgnore]
        [TorqueCloneIgnore]
        public bool Notched
        {
            get { return TestFlag(TorqueObjectFlags.Notch); }
            set { if (value) _SetFlag(TorqueObjectFlags.Notch, true); }
        }



        /// <summary>
        /// Set to true if you want the object unregistered in short order.
        /// </summary>
        [BrowsableAttribute(false)]
        [TorqueCloneIgnore]
        [XmlIgnore]
        public bool MarkForDelete
        {
            get { return TestFlag(TorqueObjectFlags.DeleteWhenReady); }
            set
            {
                if (Manager == null)
                {
                    Assert.Fatal(false, "TorqueObject.MarkForDelete - Cannot mark object for delete if not added to database.");
                    return;
                }

                if (MarkForDelete == value)
                    return;

                if (value)
                    Manager._AddMarkedObject(this);
                else
                    Manager._RemoveMarkedObject(this);

                _SetFlag(TorqueObjectFlags.DeleteWhenReady, value);
            }
        }



        /// <summary>
        /// Delegate to call right after an object is registered.
        /// </summary>
        [BrowsableAttribute(false)]
        public OnRegisteredDelegate OnRegistered
        {
            get { return _onRegisteredDelegate; }
            set { _onRegisteredDelegate = value; }
        }



        /// <summary>
        /// Called when object is loaded.
        /// </summary>
        public override void OnLoaded()
        {
            base.OnLoaded();

            if (HasComponents)
                _components.OnLoaded();
        }



        /// <summary>
        /// Called when object is unloaded.
        /// </summary>
        public override void OnUnloaded()
        {
            base.OnUnloaded();

            _sharedPool = null;
        }

        #endregion


        #region Public methods

        /// <summary>
        /// Called when a TorqueObject is being registered with the TorqueObjectDatabase. OnRegister should
        /// be used to perform any initialization for the object.  Derived classes must be sure 
        /// to call base class (and respect base class return value) or else an error will
        /// result.
        /// </summary>
        /// <returns>True if successfully added.</returns>
        public virtual bool OnRegister()
        {
            Assert.Fatal(!IsRegistered, "TorqueObject.OnRegister - TorqueObject already added.");
            Assert.Fatal(!IsTemplate, "TorqueObject.OnRegister - TorqueObject marked as template added to sim.");

            _SetFlag(TorqueObjectFlags.Registered, true);
            _SetFlag(TorqueObjectFlags.Unregistered, false);

            if (HasComponents)
            {
                if (_sharedPool != null && _sharedPool.HasNext && _sharedPool.Next.Val.Components.GetNumComponents() != Components.GetNumComponents())
                    // must have added components since first cloning
                    _sharedPool = null;

                Components._RegisterInterfaces(this);

                if (!Components._RegisterComponents(this))
                    return false;

                Components._PostRegisterComponents();
            }

            return true;
        }



        /// <summary>
        /// Called when a TorqueObject is being unregistered from a TorqueObjectDatabase.  OnUnregister
        /// should be used to perform any cleanup on the object.  Derived classes must be sure to call
        /// the base class or else an error will result.
        /// </summary>
        public virtual void OnUnregister()
        {
            Assert.Fatal(IsRegistered, "TorqueObject.OnUnregister - TorqueObject not added.");
            Assert.Fatal(!IsUnregistered, "TorqueObject.OnUnregister - TorqueObject already removed.");

            if (HasComponents)
                Components._UnregisterComponents();

            SList<InterfaceCache>.ClearList(ref _interfaceCache);

            Name = String.Empty;
            _id = 0;

            // Clear the cookies without freeing the array
            // as that would generate more garbage when the
            // object is being pooled.
            if (_cookies != null)
            {
                for (int c = 0; c < _cookies.Length; c++)
                    _cookies[c].Value = 0;
            }

            if (_folder != null)
            {
                _folder._RemoveObject(this);
                _folder = null;
            }

            _SetFlag(TorqueObjectFlags.Unregistered, true);
            _SetFlag(TorqueObjectFlags.Registered, false);
        }



        /// <summary>
        /// Called whenever an object is added to a folder.
        /// </summary>
        public virtual void OnAddToFolder() { }



        /// <summary>
        /// Called whenever an object is removed from a folder.
        /// </summary>
        public virtual void OnRemoveFromFolder() { }



        /// <summary>
        /// Implements IResetable interface.  Resets object to a state
        /// analogous to state after construction.  Used to create clean
        /// copy of objects for object pooling.
        /// </summary>
        public override void Reset()
        {
            Assert.Fatal(!IsRegistered, "TorqueObject still added");

            base.Reset();

            if (HasComponents)
                Components._Reset(false);

            _id = 0;
            _objectType = 0;
            _folder = null;
            Name = String.Empty;
        }



        /// <summary>
        /// Clone a TorqueObject and return as a TorqueObject.
        /// </summary>
        /// <returns>Cloned object.</returns>
        public TorqueObject CloneT()
        {
            return (TorqueObject)Clone();
        }



        /// <summary>
        /// Implements IClonable.  If object is marked as a pooling object (Pool or PoolWithComponents true)
        /// then we first attempt to get an object from the appropriate object pool.  In debug mode, clone
        /// tests to make sure all public properties are either properly copied or marked at TorqueCloneIgnore.
        /// </summary>
        /// <returns>Cloned copy of object.</returns>
        public virtual object Clone()
        {
            if (_IsDisposed)
                return null;

            TorqueObject obj;
            bool sharedPool = false;

            if (TestFlag(TorqueObjectFlags.PoolWithComponents) && _sharedPool != null && _sharedPool.HasNext)
            {
                obj = _sharedPool.Next.Val;
                Assert.Fatal(obj != null, "TorqueObject.Clone - Null object in object pool");
                _sharedPool.RemoveAfter();
                sharedPool = true;
            }
            else
            {
                obj = (TorqueObject)Util.ObjectPooler.CreateObject(GetType());
            }

            obj._ref = null;
            obj._flags &= ~TorqueObjectFlags.NoCloneFlags;
            obj.Name = String.Empty;
            obj._objectType = _objectType;
            obj._id = 0;
            obj._cookies = null;
            obj._folder = null;

            if (HasComponents)
            {
                Assert.Fatal(!sharedPool || obj.HasComponents, "TorqueObject.Clone - Object recovered from shared pool has no components");
                if (sharedPool && obj.HasComponents)
                    _components.CopyTo(obj._components);
                else
                    obj._components = _components.Clone();
            }

            if (!HasComponents && obj.HasComponents)
                // This is here for special case in which object creates components in constructor by default
                // but we are cloning an object without these components.  We try not to do this, but just in case.
                obj._components = null;

            Assert.Fatal(HasComponents == obj.HasComponents, "TorqueObject.Clone - Clone must have components if and only if parent object does");

            if (HasComponents)
                Assert.Fatal(Components.GetNumComponents() == obj.Components.GetNumComponents(), "TorqueObject.Clone - Clone must have same components as parent object");

            CopyTo(obj);

            if (TestFlag(TorqueObjectFlags.PoolWithComponents) && _sharedPool == null)
                SList<TorqueObject>.InsertFront(ref _sharedPool, null);

            // Clones share their object pool (if pooling with components)
            obj._sharedPool = _sharedPool;

            TestCopy(obj);
            return obj;
        }



        [Conditional("TRACE")]
        internal void TestCopy(TorqueObject obj)
        {
#if !XBOX // JMQ: this test not supported on Xbox
            Util.TestObjectCopy.Test(this, obj);

            if (HasComponents)
                Components.TestCopy(obj.Components);
#endif
        }



        /// <summary>
        /// Called by Clone. Objects need to implement this method to copy all public properties not marked
        /// with TorqueCloneIgnore attribute.
        /// </summary>
        /// <param name="obj">Target of copy operation.</param>
        public virtual void CopyTo(TorqueObject obj)
        {
#if USE_REFLECTED_COPY
            Util.ObjectPooler.CopyTo(obj, this);
#else
            TorqueObject obj2 = (TorqueObject)obj;
            obj2.PoolWithComponents = PoolWithComponents;
            obj2.Pool = Pool;
            obj2.OnRegistered = OnRegistered;
#endif
        }



        /// <summary>
        /// Tests whether any of the passed flags are set on the object.
        /// </summary>
        /// <param name="flag">Flags to test.</param>
        /// <returns>True if any of the flags are set.</returns>
        public bool TestFlag(TorqueObjectFlags flag)
        {
            return (_flags & flag) != 0;
        }



        /// <summary>
        /// Set (or clear) object type on a TorqueObject.
        /// </summary>
        /// <param name="objType">TorqueObjectType to set.</param>
        /// <param name="val">True to set, false to clear.</param>
        public void SetObjectType(TorqueObjectType objType, bool val)
        {
            if (val)
                _objectType |= objType._bits;
            else
                _objectType &= ~objType._bits;
        }



        /// <summary>
        /// Test whether an object is of a particular object type.  If the passed
        /// object type includes multiple object types, the test returns true if any
        /// of our object types matches any of the passed types.
        /// </summary>
        /// <param name="objType">The TorqueObjectType to test against.</param>
        /// <returns>True if we are of this type.</returns>
        public bool TestObjectType(TorqueObjectType objType)
        {
            return (_objectType & objType._bits) != 0;
        }



        /// <summary>
        /// Find the TorqueCookie of the specified TorqueCookieType. TorqueCookies are
        /// used to associate information with a TorqueObject.
        /// </summary>
        /// <param name="cookieType">TorqueCookieType to look up.</param>
        /// <param name="cookie">The returned cookie.</param>
        /// <returns>True if valid cookie found.</returns>
        public bool GetCookie(TorqueCookieType cookieType, out TorqueCookie cookie)
        {
            if (_cookies != null && cookieType._cookieIndex - 1 < _cookies.Length)
            {
                cookie = _cookies[cookieType._cookieIndex - 1];

                return true;
            }

            cookie = new TorqueCookie(); // invalid cookie

            return false;
        }



        /// <summary>
        /// Set TorqueCookie of specified TorqueCookieType.TorqueCookies are used
        /// to associate information with a TorqueObject.
        /// </summary>
        /// <param name="cookieType">TorqueCookieType to store.</param>
        /// <param name="cookie">The cookie to store.</param>
        public void SetCookie(TorqueCookieType cookieType, TorqueCookie cookie)
        {
            if (cookieType._cookieIndex <= 0)
            {
                Assert.Fatal(false, "TorqueObject.SetCookie - Illegal TorqueCookieType.");
                return;
            }

            if (_cookies == null)
                _cookies = new TorqueCookie[TorqueObjectDatabase.Instance.GetNumCookieTypes()];

            else if (cookieType._cookieIndex - 1 >= _cookies.Length)
                TorqueUtil.GrowArray(ref _cookies, cookieType._cookieIndex);

            _cookies[cookieType._cookieIndex - 1] = cookie;
        }



        /// <summary>
        /// Register an interface from a particular component with the object.  All interfaces must be registered
        /// with RegisterInterface or RegisterCachedInterface before they are exposed to other components.
        /// </summary>
        /// <param name="component">The component which owns the interface.</param>
        /// <param name="iface">The interface to register.</param>
        /// <returns>The interface being registered.  This is for convenience only.</returns>
        public TorqueInterface RegisterInterface(TorqueComponent component, TorqueInterface iface)
        {
            Assert.Fatal(component != null, "TorqueObject.RegisterInterface - Must supply component owner");

            iface._owner.Object = component;

            // return interface for convenience
            return iface;
        }



        /// <summary>
        /// Tell the object about components of this type and name that can be found on the component.  If there
        /// is only one such interface it can be supplied in this call and the component need not be searched.
        /// If an interface is not supplied, then GetInterfaces will be called on the component when the
        /// name and type of an interface search matches the values passed here.
        /// </summary>
        /// <param name="type">Name of interface type we are storing.  If null is passed it will match all types.</param>
        /// <param name="name">Name of interface we are storing.  If null is passed it will match all names.</param>
        /// <param name="component">Component which owns the interface.</param>
        /// <param name="iface">Interface being cached.  If null, then GetInterfaces will be called on the component.</param>
        public void RegisterCachedInterface(String type, String name, TorqueComponent component, TorqueInterface iface)
        {
            Assert.Fatal(component != null, "TorqueObject.RegisterCachedInterface - Must supply component owner.");

            InterfaceCache entry = new InterfaceCache();
            entry.Type = type;
            entry.Name = name;
            entry.Component = component;
            entry.Interface = iface;

            if (iface != null)
                iface._owner.Object = component;

            SList<InterfaceCache>.InsertFront(ref _interfaceCache, entry);
        }



        /// <summary>
        /// Set value on this object for the given key.
        /// </summary>
        /// <typeparam name="T">Type of value.</typeparam>
        /// <param name="key">Key to store value under.</param>
        /// <param name="t">Value to set.</param>
        public void SetValue<T>(String key, T t)
        {
            TorqueObjectDatabase.Instance.Dictionary.SetValue<T>(this, key, t);
        }



        /// <summary>
        /// Set value on this object for the given key and secondary key.
        /// </summary>
        /// <typeparam name="T">Type of value.</typeparam>
        /// <param name="key">Key to store value under.</param>
        /// <param name="secondaryKey">Secondary key to store value under.</param>
        /// <param name="t">Value to set.</param>
        public void SetValue<T>(String key, object secondaryKey, T t)
        {
            TorqueObjectDatabase.Instance.Dictionary.SetValue<T>(this, key, secondaryKey, t);
        }



        /// <summary>
        /// Get value stored on this object for the given key.
        /// </summary>
        /// <typeparam name="T">Type of value.</typeparam>
        /// <param name="key">Key under which value is stored.</param>
        /// <returns>Stored value.</returns>
        public T GetValue<T>(String key)
        {
            return TorqueObjectDatabase.Instance.Dictionary.GetValue<T>(this, key);
        }



        /// <summary>
        /// Get value stored on this object for the given key and secondary key.
        /// </summary>
        /// <typeparam name="T">Type of value.</typeparam>
        /// <param name="key">Key under which value is stored.</param>
        /// <param name="secondaryKey">Secondary key under which value is stored.</param>
        /// <returns>Stored value.</returns>
        public T GetValue<T>(String key, object secondaryKey)
        {
            return TorqueObjectDatabase.Instance.Dictionary.GetValue<T>(this, key, secondaryKey);
        }



        /// <summary>
        /// Get value stored on this object for the given key.
        /// </summary>
        /// <typeparam name="T">Type of value.</typeparam>
        /// <param name="key">Key under which value is stored.</param>
        /// <param name="val">Stored value.</param>
        /// <returns>True if value retrieved.</returns>
        public bool GetValue<T>(String key, out T val)
        {
            return TorqueObjectDatabase.Instance.Dictionary.GetValue<T>(this, key, out val);
        }



        /// <summary>
        /// Get value stored on this object for the given key and secondary key.
        /// </summary>
        /// <typeparam name="T">Type of value.</typeparam>
        /// <param name="key">Key under which value is stored.</param>
        /// <param name="secondaryKey">Secondary key under which value is stored.</param>
        /// <param name="val">Stored value.</param>
        /// <returns>True if value retrieved.</returns>
        public bool GetValue<T>(String key, object secondaryKey, out T val)
        {
            return TorqueObjectDatabase.Instance.Dictionary.GetValue<T>(this, key, secondaryKey, out val);
        }



        /// <summary>
        /// Remove value stored on this object associated with key but not associated with a secondary key.
        /// </summary>
        /// <param name="key">Key to remove stored value from.</param>
        public void RemoveValue(String key)
        {
            TorqueObjectDatabase.Instance.Dictionary.RemoveValue(this, key);
        }



        /// <summary>
        /// Remove value stored on this object associated with key and secondary key.
        /// </summary>
        /// <param name="key">Key from which to remove stored value.</param>
        /// <param name="secondaryKey">Secondary key to remove stored value from.</param>
        public void RemoveValue(String key, object secondaryKey)
        {
            TorqueObjectDatabase.Instance.Dictionary.RemoveValue(this, key, secondaryKey);
        }



        /// <summary>
        /// Remove all values stored on this object associated with specified key.
        /// </summary>
        /// <param name="key">Key from which to remove stored value.</param>
        public void RemoveAllValues(String key)
        {
            TorqueObjectDatabase.Instance.Dictionary.RemoveAllValues(this, key);
        }



        /// <summary>
        /// Remove all values associated with this object from the dictionary.
        /// </summary>
        public void RemoveAllValues()
        {
            TorqueObjectDatabase.Instance.Dictionary.RemoveAllValues(this);
        }



        /// <summary>
        /// Returns an iterator which iterates over all values stored on this object for a given key and type.
        /// Each value retrieved will have been stored using the same key but a different secondary key.
        /// </summary>
        /// <typeparam name="T">Type of values to iterate over.</typeparam>
        /// <param name="key">Search for values using this key.</param>
        /// <returns>Iterator.</returns>
        public TorqueDictionary.TorqueDictionaryEnumerator<T> ItrValues<T>(String key)
        {
            return TorqueObjectDatabase.Instance.Dictionary.Itr<T>(this, key);
        }



        [BrowsableAttribute(false)]
        [XmlIgnore]
        public SList<TorqueObject> PoolData
        {
            get
            {
                return _sharedPool;
            }
            set
            {
                _sharedPool = value;
            }
        }

        #endregion


        #region Private, protected, internal methods

        internal void _SetId(uint id)
        {
            _id = id;
        }



        protected void _SetFlag(TorqueObjectFlags flag, bool val)
        {
            if (val)
                _flags |= flag;
            else
                _flags &= ~flag;
        }



        internal SList<InterfaceCache> _GetCachedInterfaces()
        {
            return _interfaceCache;
        }



        /// <summary>
        /// Mark an object as special in some way.  This can be used to short-cut time consuming
        /// processing.  Many systems can use this, so once an object is notched it cannot be cleared.
        /// This flag is reserved for internal use; see Notch flag for publicly available version.
        /// </summary>
        internal bool _InternalNotched
        {
            get { return TestFlag(TorqueObjectFlags.InternalNotch); }
            set { if (value) _SetFlag(TorqueObjectFlags.InternalNotch, true); }
        }

        #endregion


        #region Private, protected, internal fields

        internal struct InterfaceCache
        {
            public String Name;
            public String Type;
            public TorqueInterface Interface;
            public TorqueComponent Component;
        }



        uint _id;
        TorqueObjectFlags _flags;
        internal ulong _objectType;
        TorqueComponentContainer _components;
        internal SList<InterfaceCache> _interfaceCache;
        TorqueFolder _folder;
        SList<TorqueObject> _sharedPool;
        TorqueCookie[] _cookies;
        OnRegisteredDelegate _onRegisteredDelegate;
        #endregion

        #region IDisposable Members

        public override void Dispose()
        {
            _IsDisposed = true;
            RemoveAllValues();
            if (HasComponents)
            {
                foreach (TorqueComponent c in this.Components)
                {
                    c._owner = null;
                    c.Dispose();
                }
            }
            base.Dispose();
        }

        #endregion
    }



    /// <summary>
    /// TorqueSet holds a collection of TorqueBase but unlike a TorqueFolder a TorqueSet
    /// does not own the contained items and items can belong to more than one TorqueSet at
    /// at a time.
    /// </summary>
    public class TorqueSet : TorqueObject, IDisposable
    {
        #region Public properties, operators, constants, and enums

        [Flags]
        public enum TorqueSetFlags
        {
            Ordered = TorqueObjectFlags.LastObjectFlag << 1,
            LastSetFlag = Ordered
        }



        /// <summary>
        /// If true then objects in the set are kept in order.
        /// </summary>
        public bool Ordered
        {
            get { return TestFlag((TorqueObjectFlags)TorqueSetFlags.Ordered); }
            set { _SetFlag((TorqueObjectFlags)TorqueSetFlags.Ordered, value); }
        }

        #endregion


        #region Public methods

        /// <summary>
        /// Called on TorqueSets when unregistered from TorqueObjectDatabase.
        /// </summary>
        public override void OnUnregister()
        {
            for (int index = 0; index < _items.Count; index++)
                _items[index].RemoveNotify(this);

            _items.Clear();
            _items = null;
            base.OnUnregister();
        }



        /// <summary>
        /// Adds item to a TorqueSet.
        /// </summary>
        /// <param name="item">Item to add.</param>
        public virtual void AddItem(TorqueBase item)
        {
            TorqueObject obj = item as TorqueObject;

            if (obj != null && !obj.IsRegistered)
                // don't add object to set if object not added to sim
                return;

            item.RequestRemoveNotify(this);
            _items.Add(item.Ref);
        }



        /// <summary>
        /// Removes an item from a TorqueSet.
        /// </summary>
        /// <param name="item">Item to remove.</param>
        public virtual void RemoveItem(TorqueBase item)
        {
            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i].Ref == item)
                {
                    _items[i].Ref.CancelRemoveNotify(this);
                    _RemoveItem(i);
                }
            }
        }



        /// <summary>
        /// Finds an item in a TorqueSet by name.
        /// </summary>
        /// <param name="name">Name of item to find.</param>
        /// <returns>Found item or null if none found.</returns>
        public TorqueBase FindItem(String name)
        {
            return FindItem<TorqueBase>(name);
        }



        /// <summary>
        /// Finds an item in a TorqueSet by name.
        /// </summary>
        /// <typeparam name="T">Type of item to find.</typeparam>
        /// <param name="name">Name of item to find.</param>
        /// <returns>Found item or null if none found.</returns>
        public T FindItem<T>(String name) where T : TorqueBase
        {
            for (int i = 0; i < _items.Count; i++)
            {
                T item = _items[i].Ref as T;

                if (item != null && item.Name == name)
                    return item;
            }

            return null;
        }



        /// <summary>
        /// Find an item in a set by index.
        /// </summary>
        /// <param name="idx">Index of item to be found.</param>
        /// <returns>Found item or null if none found.</returns>
        public TorqueBase GetItem(int idx)
        {
            Assert.Fatal(idx >= 0 && idx < _items.Count, "TorqueSet.GetItem - Index out of range.");
            return _items[idx].Ref;
        }



        /// <summary>
        /// Number of items in set.
        /// </summary>
        /// <returns>Number of items in set.</returns>
        public int GetNumItems()
        {
            return _items.Count;
        }



        /// <summary>
        /// Enumerator for iterating through all items in a set.
        /// </summary>
        /// <returns>The enumerator.</returns>
        public IEnumerator<TorqueObject> GetEnumerator()
        {
            TorqueObject obj;

            for (int index = 0; index < _items.Count; index++)
            {
                obj = _items[index].Ref as TorqueObject;

                if (obj != null)
                    yield return obj;
            }
        }



        /// <summary>
        /// Enumerator for iterating through all items of a particular type in a set.
        /// </summary>
        /// <typeparam name="T">Type of items to iteratate on.</typeparam>
        /// <returns>The enumerator.</returns>
        public IEnumerable<T> Itr<T>() where T : TorqueBase
        {
            for (int index = 0; index < _items.Count; index++)
            {
                T t = _items[index].Ref as T;

                if (t != null)
                    yield return t;
            }
        }



        /// <summary>
        /// Called when item in a set is being unregistered.
        /// </summary>
        /// <param name="beingRemoved">Item being unregistered.</param>
        public override void OnRemoveNotify(TorqueBase beingRemoved)
        {
            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i].Ref == beingRemoved)
                {
                    _RemoveItem(i);
                    return;
                }
            }
        }



        /// <summary>
        /// Clears out references to this set.
        /// </summary>
        public override void Reset()
        {
            base.Reset();

            for (int index = 0; index < _items.Count; index++)
                _items[index].RemoveNotify(this);

            _items.Clear();
        }



        /// <summary>
        /// Clone this set.
        /// </summary>
        /// <returns>Copy of cloned set.</returns>
        override public object Clone()
        {
            TorqueSet set = (TorqueSet)base.Clone();

            if (_items != null)
            {
                set._items = new List<TorqueRef>();

                foreach (TorqueBase item in Itr<TorqueBase>())
                    set.AddItem(item);
            }

            return set;
        }

        #endregion


        #region Private, protected, internal methods

        private void _RemoveItem(int idx)
        {
            Assert.Fatal(idx >= 0 && idx < _items.Count, "TorqueSet._RemoveItem - Index out of range.");

            if (Ordered)
            {
                _items.RemoveAt(idx);
            }
            else
            {
                _items[idx] = _items[_items.Count - 1];
                _items.RemoveAt(_items.Count - 1);
            }
        }

        #endregion


        #region Private, protected, internal fields

        List<TorqueRef> _items = new List<TorqueRef>();

        #endregion

        #region IDisposable Members

        public override void Dispose()
        {
            _IsDisposed = true;
            if (_items != null)
                Reset();
            _items = null;
            base.Dispose();
        }

        #endregion
    }



    /// <summary>
    /// A TorqueFolder holds and owns a collection of TorqueObjects.  A TorqueObject can be a member
    /// of at most one TorqueFolder (and must be a member of exactly one once it is registered).  When
    /// a TorqueFolder is unregistered, all it's contained objects are also unregistered.
    /// </summary>
    public class TorqueFolder : TorqueObject, IDisposable
    {
        #region Public properties, operators, constants, and enums

        [Flags]
        public enum TorqueFolderFlags
        {
            Ordered = TorqueObjectFlags.LastObjectFlag << 1,
            LastFolderFlag = Ordered
        }



        /// <summary>
        /// If true then objects in folder are kept in order.
        /// </summary>
        public bool Ordered
        {
            get { return TestFlag((TorqueObjectFlags)TorqueFolderFlags.Ordered); }
            set { _SetFlag((TorqueObjectFlags)TorqueFolderFlags.Ordered, value); }
        }



        /// <summary>
        /// True if this is the root folder.  The root folder can neither be moved or removed.
        /// </summary>
        public bool IsRoot
        {
            // Root folder is the only folder which contains itself.
            get { return this == this.Folder; }
        }



        /// <summary>
        /// TorqueObjectDatabase which holds this folder.
        /// </summary>
        public new TorqueObjectDatabase Manager
        {
            get { return _objectManager; }
        }

        #endregion


        #region Public methods

        /// <summary>
        /// Called on TorqueFolder when unregistered from TorqueObjectDatabase.
        /// </summary>
        public override void OnUnregister()
        {
            // Disconnect our object list so that we don't modify the collection
            List<TorqueObject> objects = _objects;
            _objects = null;

            for (int index = 0; index < objects.Count; index++)
                if (objects[index].IsRegistered && objects[index].IsPersistent == false)
                    Manager.Unregister(objects[index]);

            // put object list back (but unload it)
            _objects = objects;
            _objects.Clear();

            // let baseclass know
            base.OnUnregister();
        }



        /// <summary>
        /// Find TorqueObject in folder by name.
        /// </summary>
        /// <param name="name">Name of the object to find.</param>
        /// <param name="recursive">If true, search recurses into child folders.</param>
        /// <returns>The found object or null if none found.</returns>
        public TorqueObject FindObject(String name, bool recursive)
        {
            for (int index = 0; index < _objects.Count; index++)
                if (_objects[index].Name == name)
                    return _objects[index];

            if (recursive)
            {
                for (int index = 0; index < _objects.Count; index++)
                {
                    if (_objects[index] != null && _objects[index] is TorqueFolder)
                    {
                        TorqueObject ret = ((TorqueFolder)_objects[index]).FindObject(name, recursive);

                        if (ret != null)
                            return ret;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Find the first child object in the folder of the specified type.
        /// </summary>
        /// <param name="recursive">If true, search recurses into child folders.</param>
        /// <typeparam name="T">The type of object to search for.</typeparam>
        /// <returns>The first object of the specified type.</returns>
        public T FindObject<T>(bool recursive) where T : TorqueObject
        {
            for (int index = 0; index < _objects.Count; index++)
                if (_objects[index] is T)
                    return _objects[index] as T;

            if (recursive)
            {
                for (int index = 0; index < _objects.Count; index++)
                //foreach (TorqueFolder folder in _objects)
                {
                    if (_objects[index] != null && _objects[index] is TorqueFolder)
                    {
                        T ret = ((TorqueFolder)_objects[index]).FindObject<T>(recursive);

                        if (ret != null)
                            return ret as T;
                    }
                }
            }

            return null;
        }



        /// <summary>
        /// Get a TorqueObject from the folder by index.
        /// </summary>
        /// <param name="idx">Index of object to find.</param>
        /// <returns>Found object.</returns>
        public TorqueObject GetObject(int idx)
        {
            Assert.Fatal(idx >= 0 && idx < _objects.Count, "Index out of range");
            return _objects[idx];
        }



        /// <summary>
        /// Number of object in the folder.
        /// </summary>
        /// <returns>Number of object in the folder.</returns>
        public int GetNumObjects()
        {
            return _objects.Count;
        }



        /// <summary>
        /// Enumerator for iterating through all TorqueObjects in the folder (not recursive).
        /// </summary>
        /// <returns>The enumerator.</returns>
        public IEnumerator<TorqueObject> GetEnumerator()
        {
            for (int index = 0; index < _objects.Count; index++)
                yield return _objects[index];
        }



        /// <summary>
        /// Enumerator for iterating through all objects of type T in folder and possibly
        /// child folders.
        /// </summary>
        /// <typeparam name="T">Type of objects to iterate over.</typeparam>
        /// <param name="recursive">If true, child folders are iterated over too.</param>
        /// <returns>The enumerator.</returns>
        public IEnumerable<T> Itr<T>(bool recursive) where T : TorqueObject
        {
            for (int index = 0; index < _objects.Count; index++)
            {
                T t = _objects[index] as T;

                if (t != null)
                    yield return t;

                if (recursive)
                {
                    TorqueFolder subfolder = _objects[index] as TorqueFolder;

                    if (subfolder != null)
                    {
                        foreach (T t2 in subfolder.Itr<T>(true))
                            yield return t2;
                    }
                }
            }
        }



        /// <summary>
        /// Make the passed object the first in the folder.
        /// </summary>
        /// <param name="obj">Object to move</param>
        public void BringObjectToFront(TorqueObject obj)
        {
            ReOrder(obj, _objects[0]);
        }



        /// <summary>
        /// Make the passed object the lst in the folder.
        /// </summary>
        /// <param name="obj">Object to move</param>
        public void PushObjectToBack(TorqueObject obj)
        {
            ReOrder(obj, null);
        }



        /// <summary>
        /// Swap the order of two objects in a folder.
        /// </summary>
        /// <param name="obj">First object to move.</param>
        /// <param name="target">Second object to move.</param>
        /// <returns>True if reoder was successful.</returns>
        public bool ReOrder(TorqueObject obj, TorqueObject target)
        {
            // don't reorder, same object but don't indicate error
            if (obj == null || obj == target)
                return true;

            int objIdx = _objects.IndexOf(obj);

            // obj must be in the list
            if (objIdx < 0)
                return false;

            // if there is no target, then put obj to back of list
            if (target == null)
            {
                // don't move if already last object
                if (objIdx != _objects.Count - 1)
                {
                    _objects.RemoveAt(objIdx);  // remove object from its current location
                    _objects.Add(obj);          // push it to the back of the list
                }
            }
            else
            {
                // target must be in the list
                if (!_objects.Contains(target))
                    return false;

                // remove the obj
                _objects.RemoveAt(objIdx);

                // now that obj is now erased, we have to find the target index
                int targetIdx = _objects.IndexOf(target);

                // insert obj in front of target
                _objects.Insert(targetIdx, obj);
            }

            return true;
        }



        /// <summary>
        /// Clone folder and all it's contained objects.
        /// </summary>
        /// <returns>The cloned folder</returns>
        override public object Clone()
        {
            TorqueFolder folder = (TorqueFolder)base.Clone();

            for (int index = 0; index < this._objects.Count; index++)
            {
                TorqueObject obj2 = (TorqueObject)this._objects[index].Clone();
                obj2.Folder = folder;
            }

            return folder;
        }

        #endregion


        #region Private, protected, internal methods

        internal virtual void _AddObject(TorqueObject obj)
        {
            if (obj == this)
                // safe-guard against adding ourself to ourself (this needs to be here when adding root folder)
                return;

            if (_objects == null)
                return;

            _objects.Add(obj);
            obj.OnAddToFolder();
        }



        internal virtual bool _RemoveObject(TorqueObject obj)
        {
            if (_objects == null)
            {
                // happens if we are removing objects from folder
                obj.OnRemoveFromFolder(); // make sure callback gets called
                return true;
            }

            for (int i = 0; i < _objects.Count; i++)
            {
                if (obj == _objects[i])
                    return _RemoveObject(i);
            }

            return false;
        }



        internal bool _RemoveObject(int idx)
        {
            Assert.Fatal(idx >= 0 && idx < _objects.Count, "Index out of range");

            if (idx < 0 || idx >= _objects.Count)
                return false;

            TorqueObject obj = _objects[idx];
            obj.OnRemoveFromFolder();

            if (Ordered)
            {
                _objects.RemoveAt(idx);
            }
            else
            {
                _objects[idx] = _objects[_objects.Count - 1];
                _objects.RemoveAt(_objects.Count - 1);
            }

            return true;
        }



        internal void _SetManager(TorqueObjectDatabase mgr)
        {
            Assert.Fatal(_objectManager == null, "Object manager already set on object");

            if (_objectManager == null)
                _objectManager = mgr;
        }

        #endregion


        #region Private, protected, internal fields

        List<TorqueObject> _objects = new List<TorqueObject>();
        TorqueObjectDatabase _objectManager = null;

        #endregion

        #region IDisposable Members

        public override void Dispose()
        {
            _IsDisposed = true;
            _objects.Clear();
            _objects = null;
            base.Dispose();
        }

        #endregion
    }
}
