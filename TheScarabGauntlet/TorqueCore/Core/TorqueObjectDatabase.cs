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
    /// <summary>
    /// A strongly typed way to assign types to TorqueObjects.  New TorqueObjectTypes can be registered
    /// on the TorqueObjectDatabase using the GetObjectType(String name) method.
    /// </summary>
    public struct TorqueObjectType
    {
        #region Static members

        // This function is for use by the XML deserializer to build an aggregate of object types.
        static TorqueObjectType Aggregate(List<object> objTypes)
        {
            TorqueObjectType ret = new TorqueObjectType();

            if (objTypes == null)
                return ret;

            // just one object?  then just set the type
            if (objTypes.Count == 1)
            {
                ret = (TorqueObjectType)objTypes[0];
            }
            else
            {
                for (int index = 0; index < objTypes.Count; index++)
                    ret = ret + (TorqueObjectType)objTypes[index];
            }

            return ret;
        }

        #endregion


        #region Constructors

        TorqueObjectType(ulong bits, String name)
        {
            _bits = bits;
            _typeName = name;
        }

        #endregion


        #region Public properties, operators, constants, and enums

        /// <summary>
        /// The name of the object type.  Only meaningful if we were generated directly from a TorqueObjectDatabase.
        /// </summary>
        public String Name
        {
            get { return _typeName; }
        }



        /// <summary>
        /// Test whether a TorqueObjectType has been initialized.
        /// </summary>
        public bool Valid
        {
            get { return _typeName != null && _typeName != String.Empty; }
        }



        /// <summary>
        /// Test whether we are a single object type or a union of several types.
        /// </summary>
        public bool IsMultiple
        {
            get { return _typeName == _multipleName; }
        }



        /// <summary>
        /// Present for Debug builds only, because we may change the internal representation 
        /// of TorqueObjectType at a later time, which would break this interface.
        /// </summary>
        public ulong Bits
        {
            get { return _bits; }
        }



        /// <summary>
        /// Object type representing a union of all possible object types.
        /// </summary>
        static public TorqueObjectType AllObjects
        {
            get { return _allObjects; }
        }



        /// <summary>
        /// Object type which no objects will match.
        /// </summary>
        static public TorqueObjectType NoObjects
        {
            get { return _noObjects; }
        }



        /// <summary>
        /// Take the union of two object types
        /// </summary>
        /// <param name="a">First object type in the union.</param>
        /// <param name="b">Second object type in the union.</param>
        /// <returns>Union of two object types.</returns>
        public static TorqueObjectType operator +(TorqueObjectType a, TorqueObjectType b)
        {
            TorqueObjectType newType = new TorqueObjectType();
            newType._bits = a._bits | b._bits;
            newType._typeName = _multipleName;

            return newType;
        }



        /// <summary>
        /// Take the difference between two object types.
        /// </summary>
        /// <param name="a">Starting object type.</param>
        /// <param name="b">Remove this object type(s).</param>
        /// <returns>First object type with types from second object type removed.</returns>
        public static TorqueObjectType operator -(TorqueObjectType a, TorqueObjectType b)
        {
            TorqueObjectType newType = new TorqueObjectType();
            newType._bits = a._bits & ~b._bits;
            newType._typeName = _multipleName;

            return newType;
        }



        /// <summary>
        /// Test to see if the two object type masks have any object types in common.
        /// </summary>
        /// <param name="a">First object type.</param>
        /// <param name="b">Second object type.</param>
        /// <returns>True if object type masks have common types.</returns>
        public static bool operator &(TorqueObjectType a, TorqueObjectType b)
        {
            return (a._bits & b._bits) != 0;
        }

        /// <summary>
        /// Convert the bits into a pretty description for the editor.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return TorqueObjectDatabase.Instance.GetObjectTypeDescription(this);
        }

        #endregion


        #region Private, protected, internal fields

        internal ulong _bits;
        internal String _typeName;
        static String _multipleName = "multiple";
        static TorqueObjectType _allObjects = new TorqueObjectType(0xFFFFFFFFFFFFFFFF, "all");
        static TorqueObjectType _noObjects = new TorqueObjectType(0, "none");

        #endregion
    }



    /// <summary>
    /// TorqueCookieType can be used to look up a cookie on TorqueObjects.  New cookie types can be
    /// registered with a TorqueObjectManager using the RegisterCookieType(TorqueCookieType cookieType)
    /// method.
    /// </summary>
    sealed public class TorqueCookieType
    {
        #region Constructors

        public TorqueCookieType(String name)
        {
            _cookieName = name;
        }

        #endregion


        #region Public properties, operators, constants, and enums

        /// <summary>
        /// Get the name of the TorqueCookieType.
        /// </summary>
        public String Name
        {
            get { return _cookieName; }
        }



        /// <summary>
        /// Whether the TorqueCookieType is valid. Will be invalid if the 
        /// type is not registered with the TorqueObjectDataBase.
        /// </summary>
        public bool Valid
        {
            get { return _cookieIndex != 0; }
        }

        #endregion


        #region Private, protected, internal fields

        internal int _cookieIndex;
        internal String _cookieName;

        #endregion
    }



    /// <summary>
    /// Database of a collection of TorqueObjects.  Every object must be registered with the object
    /// database before being used and unregistered after use.  Objects can be looked up by name
    /// or id.  Anything derived from TorqueBase can also be looked up by name.  When objects are
    /// unregistered from database, any TorqueSafePtr pointing to them is nulled out.
    /// </summary>
    public class TorqueObjectDatabase
    {
        #region Static methods, fields, constructors

        public static TorqueObjectDatabase Instance
        {
            get { return _db; }
        }

        #endregion


        #region Constructors

        public TorqueObjectDatabase()
        {
            _currentFolder = _rootFolder;
            Register(_rootFolder);

            if (_db == null)
                _db = this;

            _rootFolder.Name = "RootFolder";
        }

        #endregion


        #region Public properties, operators, constants, and enums

        /// <summary>
        /// The currently selected folder on the database.  New objects will be added to this folder.
        /// </summary>
        public TorqueFolder CurrentFolder
        {
            get { return _currentFolder; }
            set
            {
                Assert.Fatal(value != null, "TorqueObjectDatabase.CurrentFolder_set - Can't set current folder to null.");
                Assert.Fatal(value.IsRegistered && !value.IsUnregistered, "TorqueObjectDatabase.CurrentFolder_set - Folder not properly added, cannot be current folder.");
                Assert.Fatal(value.Manager == this, "TorqueObjectDatabase.CurrentFolder_set - Folder from different manager cannot be current folder.");

                if (value == null || !value.IsRegistered || value.IsUnregistered || value.Manager != this)
                    return;

                _currentFolder = value;
            }
        }



        /// <summary>
        /// The root folder of all objects in the database.  This folder can never be moved or removed.  All objects
        /// can be found by recursively iterating through the root folder and all it's subfolders.
        /// </summary>
        public TorqueFolder RootFolder
        {
            get { return _rootFolder; }
        }



        /// <summary>
        /// The TorqueDictionary associated with this database.  The TorqueDictionary holds values associated with
        /// objects using selected keys.  Direct access to the Dictionary is not normally needed because objects
        /// provide indirect access to it's methods via GetValue, SetValue, RemoveValue, and ItrValues methods.
        /// </summary>
        public TorqueDictionary Dictionary
        {
            get { return _dictionary; }
        }



        /// <summary>
        /// A TorqueObjectType which is shared by all objects.
        /// </summary>
        public TorqueObjectType GenericObjectType
        {
            get
            {
                if (!_aObjectType.Valid)
                    _aObjectType = GetObjectType("Object");

                return _aObjectType;
            }
        }



        /// <summary>
        /// If true, no new object types can be added to the database.  When the database is locked, 
        /// GetObjectType() on an unknown type will Assert in debug.
        /// </summary>
        public bool ObjectTypesLocked
        {
            get { return _objTypesLocked; }
            set { _objTypesLocked = value; }
        }

        #endregion


        #region Public methods

        /// <summary>
        /// Registers a TorqueObject with the database.  All TorqueObjects need to be registered before being used.  
        /// Objects cannot be registered more than once.  If the object is marked as a template object registration
        /// will fail.  If the object or any of it's components fails to properly registered, this method will return
        /// false and the entire object will not be registered.
        /// </summary>
        /// <param name="obj">The object to be registered.</param>
        /// <returns>True if register was successful</returns>
        public bool Register(TorqueObject obj)
        {
            Assert.Fatal(obj != null, "TorqueObjectDatabase.Register - Cannot register null.");

            if (obj == null || obj.ObjectId != 0 || obj.IsRegistered)
            {
                Assert.Fatal(false, "TorqueObjectDatabase.Register - Registering an object which is already registered.");
                return false;
            }

            if (obj.IsTemplate)
            {
                Assert.Fatal(false, "TorqueObjectDatabase.Register - Adding object marked as template to sim.");
                return false;
            }

            // assign object an id
            obj._SetId(_nextId++);
            _objectById.Add(obj.ObjectId, obj);

            // make sure all objects have at least one object type bit set so that
            // checks against "all" object type succeed
            obj.SetObjectType(GenericObjectType, true);

            // place in a folder
            if (obj.Folder == null /*|| !obj.Folder.IsRegistered*/) // rdbnote: discuss how this affects the GUI
                obj.Folder = _currentFolder;

            if (obj is TorqueFolder)
                (obj as TorqueFolder)._SetManager(this);

            if (!obj.OnRegister())
            {
                // add failed
                Unregister(obj);
                return false;
            }

            Assert.Fatal(obj.IsRegistered, "TorqueObjectDatabase.Register - Failed to call base.OnRegister.");

            // check for OnRegistered delegate. If it exists, call it.
            if (obj.OnRegistered != null)
                obj.OnRegistered(obj);

            return true;
        }


        /// <summary>
        /// Unregisters a TorqueObject.  The object must be registered or else this is an error.  Unregistering
        /// an object will perform any cleanup that is needed and will clear out TorqueSafePtr's that point to this
        /// object.
        /// </summary>
        /// <param name="obj">The object to unregister.</param>
        /// <returns>True if unregister was successful</returns>
        public bool Unregister(TorqueObject obj)
        {
            return Unregister(obj, true);
        }

        /// <summary>
        /// Unregisters a TorqueObject, but can optionally skip disposing or reinserting 
        /// the object into the object pools.
        /// </summary>
        /// <param name="obj">The object to unregister.</param>
        /// <param name="dispose">If true the object is disposed or added back into the object pools.</param>
        /// <returns>True if unregister was successful</returns>
        public bool Unregister(TorqueObject obj, bool dispose)
        {
            // make sure we are in object db
            Assert.Fatal(obj != null, "TorqueObjectDatabase.Unregister - Cannot unregister null.");

            if (obj == null || !_objectById.ContainsKey(obj.ObjectId) || obj.Folder == null)
            {
                Assert.Fatal(false, "TorqueObjectDatabase.Unregister - Unregistering an object which isn't properly registered.");
                return false;
            }

            if (obj is TorqueFolder && (obj as TorqueFolder).IsRoot)
                // can't remove root folder
                return false;

            // If deleting current folder, make root current (only folder we know is safe)
            if (CurrentFolder == obj)
                CurrentFolder = RootFolder;

            // take out of id and name db (verified that it was in id db above)
            _objectById.Remove(obj.ObjectId);

            // Notify object it's being removed, and then remove it
            obj.OnUnregister();

            // clear out reference so TorqueRef's are cleared
            obj.Ref.Ref = null;

            Assert.Fatal(obj.IsUnregistered, "TorqueObjectDatabase.Unregister - Failed to call base.OnUnregister.");

            // clear out dictionary entries
            obj.RemoveAllValues();

            // Skip out if we're not fully releasing the object.
            if (!dispose)
                return true;

            if (obj.TestFlag(TorqueObject.TorqueObjectFlags.PoolWithComponents) && obj.PoolData != null)
            {
                // detach component container and reset object and container separately
                // then reattach container.  We do this so objects and components get
                // reset but remain attached in the end.
                TorqueComponentContainer container = obj.Components;
                obj.Components = null;
                obj.Reset();
                Assert.Fatal(obj._ref == null, "TorqueObjectDatabase.Unregister - Reset method failed to call base.Reset.  See class " + obj.GetType() + ".");
                container._Reset(true);
                obj.Components = container;

                // add whole object to shared pool
                obj.PoolData.InsertAfter(obj);
            }
            else if (obj.TestFlag(TorqueObject.TorqueObjectFlags.Poolable))
            {
                for (int index = 0; index < obj.Components.GetNumComponents(); index++)
                {
                    Util.ObjectPooler.RecycleObject(obj.Components.GetComponentByIndex(index));
                    Assert.Fatal(obj.Components.GetComponentByIndex(index)._ref == null, "TorqueObjectDatabase.Unregister - Reset method failed to call base.Reset.  See class " + obj.Components.GetComponentByIndex(index).GetType() + ".");
                }

                Util.ObjectPooler.RecycleObject(obj);
                Assert.Fatal(obj._ref == null, "TorqueObjectDatabase.Unregister - Reset method failed to call base.Reset.  See class " + obj.GetType() + ".");
            }
            else if (obj is IDisposable)
            {
                // if not pooling, be dure to dispose of unmanaged resources
                (obj as IDisposable).Dispose();
            }

            return true;
        }



        /// <summary>
        /// Searches for a TorqueBase in the name database.  Note: this
        /// method can find items which are not TorqueObjects.  To find
        /// TorqueObjects only use the templated version.
        /// </summary>
        /// <param name="name">Name of the object to look up.</param>
        /// <returns>Found object or null if nothing found.</returns>
        public TorqueBase FindObject(string name)
        {
            return FindObject<TorqueBase>(name);
        }



        /// <summary>
        /// Searches for a TorqueObject by id.
        /// </summary>
        /// <param name="id">Id to search for.</param>
        /// <returns>Found object or null if nothing found.</returns>
        public TorqueObject FindObject(uint id)
        {
            TorqueObject obj = null;
            _objectById.TryGetValue(id, out obj);

            return obj;
        }



        /// <summary>
        /// Searches for an object of type T.
        /// </summary>
        /// <typeparam name="T">Type of object to search for.</typeparam>
        /// <returns>First found object or null if nothing found.</returns>
        public T FindObject<T>() where T : class
        {
            foreach (uint id in _objectById.Keys)
            {
                if (_objectById[id] as T != null)
                    return _objectById[id] as T;
            }

            foreach (string id in _objectByName.Keys)
            {
                if (_objectByName[id].Val as T != null)
                    return _objectByName[id].Val as T;
            }

            return null;
        }



        /// <summary>
        /// Searches for a named object of type T.
        /// </summary>
        /// <typeparam name="T">Type of object to search for.</typeparam>
        /// <param name="name">Name of object to search for.</param>
        /// <returns>Found object or null if nothing found.</returns>
        public T FindObject<T>(string name) where T : class
        {
            if (name == null || name == String.Empty)
                return null;

            SList<TorqueBase> startNode;

            if (_objectByName.TryGetValue(name, out startNode))
            {
                while (startNode != null)
                {
                    T obj = startNode.Val as T;

                    if (obj != null)
                        return obj;

                    startNode = startNode.Next;
                }
            }

            return null;
        }



        /// <summary>
        /// Searches for and returns a list of objects of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of object to search for.</typeparam>
        /// <returns>A list of objects of the specified type.</returns>
        public List<T> FindObjects<T>() where T : class
        {
            List<T> results = new List<T>();
            FindObjects<T>(ref results);

            return results;
        }



        /// <summary>
        /// Searches for and populates the results list with objects of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of object to search for.</typeparam>
        /// <param name="results">A list of objects of the specified type.</param>
        public void FindObjects<T>(ref List<T> results) where T : class
        {
            results.Clear();

            foreach (string id in _objectByName.Keys)
            {
                if (_objectByName[id].Val as T != null)
                    results.Add(_objectByName[id].Val as T);
            }

            foreach (uint id in _objectById.Keys)
            {
                if (_objectById[id] as T != null && !results.Contains(_objectById[id] as T))
                    results.Add(_objectById[id] as T);
            }
        }



        /// <summary>
        /// Searches for and returns a list of objects of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of object to search for.</typeparam>
        /// <returns>A list of objects of the specified type.</returns>
        public List<T> FindObjectsOfObjectType<T>(TorqueObjectType type) where T : class
        {
            List<T> results = new List<T>();
            FindObjectsOfObjectType<T>(ref results, ref type);

            return results;
        }



        /// <summary>
        /// Searches for and populates the results list with objects of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of object to search for.</typeparam>
        /// <param name="results">A list of objects of the specified type.</param>
        public void FindObjectsOfObjectType<T>(ref List<T> results, ref TorqueObjectType type) where T : class
        {
            results.Clear();

            foreach (uint id in _objectById.Keys)
            {
                if (_objectById[id] as T != null && !results.Contains(_objectById[id] as T))
                {
                    if (_objectById[id].TestObjectType(type) == true)
                        results.Add(_objectById[id] as T);
                }
            }
        }



        /// <summary>
        /// Searches for and returns a list of registered objects of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of object to search for.</typeparam>
        /// <returns>A list of registered objects of the specified type.</returns>
        public List<T> FindRegisteredObjects<T>() where T : class
        {
            List<T> results = new List<T>();
            FindRegisteredObjects<T>(ref results);

            return results;
        }



        /// <summary>
        /// Searches for and populates the results list with registered objects of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of object to search for.</typeparam>
        /// <param name="results">A list of registered objects of the specified type.</param>
        public void FindRegisteredObjects<T>(ref List<T> results) where T : class
        {
            results.Clear();

            foreach (uint id in _objectById.Keys)
            {
                object obj = _objectById[id];

                if (obj as T != null && obj as TorqueObject != null && (obj as TorqueObject).IsRegistered)
                    results.Add(_objectById[id] as T);
            }
        }



        /// <summary>
        /// Find an object of type T with the passed name and return a clone of it.
        /// </summary>
        /// <typeparam name="T">The type of the object to find.</typeparam>
        /// <param name="name">The name of the object to find.</param>
        /// <returns>A clone of the found object, or null if none found.</returns>
        public T CloneObject<T>(string name) where T : TorqueObject
        {
            T obj = FindObject<T>(name);

            if (obj != null)
                return obj.Clone() as T;

            return null;
        }



        /// <summary>
        /// Searches for an object of type T by id.
        /// </summary>
        /// <typeparam name="T">Type of object to search for.</typeparam>
        /// <param name="id">Id of object to search for.</param>
        /// <returns>Found object or null if nothing found.</returns>
        public T FindObject<T>(uint id) where T : TorqueObject
        {
            return FindObject(id) as T;
        }



        /// <summary>
        /// Finds an existing TorqueObjectType by name or registers a new version if one doesn't already exist.
        /// If the number of registered types exceeds the max (64) or if the object type database is locked
        /// an invalid object type will be returned (Valid property will be false).
        /// </summary>
        /// <param name="name">Name of object type to search for.</param>
        /// <returns>Named object type.</returns>
        public TorqueObjectType GetObjectType(String name)
        {
            TorqueObjectType objType = new TorqueObjectType();

            if (_objectTypes.TryGetValue(name, out objType))
                return objType;

            Assert.Fatal(!_objTypesLocked, "TorqueObjectDatabase.GetObjectType - Attempted to create new object type '" + name + "', but object database is locked.  Check type name or unlock database.");

            if (_objTypesLocked)
                // fail silently
                return objType;

            // didn't find it...add it
            Assert.Fatal(_objectTypes.Count < 63, "TorqueObjectDatabase.GetObjectType - Too many object types -- max 64 allowed.");

            if (_objectTypes.Count >= 64)
                // fail silently
                return objType;

            objType._bits = 1ul << _objectTypes.Count;
            objType._typeName = name;
            _objectTypes[name] = objType;

            return objType;
        }


        /// <summary>
        /// Convert set of bits in a TorqueObjectType into a pretty
        /// description string for use by the editor.
        /// </summary>
        public string GetObjectTypeDescription(TorqueObjectType type)
        {
            string desc = "";

            foreach (var pair in _objectTypes)
            {
                /// Skip over bit 1 as its the 'Object' type which
                /// is found on all TorqueObjectTypes.
                if (pair.Value.Bits <= 1)
                    continue;

                if ((pair.Value.Bits & type.Bits) != 0)
                    desc += pair.Value.Name + ", ";
            }

            return desc.TrimEnd(' ', ',');
        }

        /// <summary>
        /// Set an object type on a TorqueObject by name.
        /// </summary>
        /// <param name="obj">TorqueObject to set the type on.</param>
        /// <param name="objectTypeName">Name of TorqueObjectType</param>
        /// <param name="val">True to set type, false to clear type.</param>
        public void SetObjectType(TorqueObject obj, String objectTypeName, bool val)
        {
            Assert.Fatal(obj.Manager == null || obj.Manager == this, "Wrong manager");
            TorqueObjectType objectType = GetObjectType(objectTypeName);
            obj.SetObjectType(objectType, val);
        }

        /// <summary>
        /// Returns a string list of all the registered object type names
        /// except for the generic type of "Object".
        /// </summary>
        /// <returns></returns>
        public List<string> GetObjectTypeNames()
        {
            TorqueObjectType genericType = GenericObjectType;

            List<string> result = new List<string>();
            foreach (var pair in _objectTypes)
            {
                if (pair.Value.Bits != genericType.Bits)
                    result.Add(pair.Key);
            }
            return result;
        }

        /// <summary>
        /// Register a TorqueCookieType with the TorqueObjectDatabase.  TorqueCookieTypes can
        /// be used to look up TorqueCookies on TorqueObjects.  If a TorqueCookieType is kept
        /// private, then only those who have access to the cookie type can access the cookie.
        /// </summary>
        /// <param name="cookieType">TorqueCookieType to register.</param>
        public void RegisterCookieType(TorqueCookieType cookieType)
        {
            if (cookieType.Valid)
                // already registered...don't complain just exit
                return;

            cookieType._cookieIndex = _cookieTypes.Count + 1;
            _cookieTypes.Add(cookieType);
        }



        /// <summary>
        /// Get the number of cookie types currently registered.
        /// </summary>
        /// <returns>Number of cookie types.</returns>
        public int GetNumCookieTypes()
        {
            return _cookieTypes.Count;
        }



        /// <summary>
        /// Unregister all the objects which have been marked for deletion.  This method is 
        /// periodically called by the engine and does not need to be called by the user.
        /// </summary>
        public void DeleteMarkedObjects()
        {
            if (_readyForDelete.Count == 0)
                // early out
                return;

            // Note: Items might be added to this list as we go,
            // but that should be ok because they'll be added
            // to the back.  Items might also be removed, but that's
            // also ok because it will always be items later in the
            // list than 'i'.
            for (int i = 0; i < _readyForDelete.Count; i++)
                if (_readyForDelete[i].Object != null)
                    Unregister(_readyForDelete[i].Object);

            _readyForDelete.Clear();
        }

        #endregion


        #region Private, protected, internal methods

        internal void _AddMarkedObject(TorqueObject obj)
        {
            // might already be in list, but don't worry because we are using safe pointers
            TorqueSafePtr<TorqueObject> ptr = new TorqueSafePtr<TorqueObject>();
            ptr.Object = obj;
            _readyForDelete.Add(ptr);
        }



        internal void _RemoveMarkedObject(TorqueObject obj)
        {
            // might be in the list several times, so be sure to remove each instance
            for (int i = 0; i < _readyForDelete.Count; i++)
            {
                if (_readyForDelete[i].Object == obj)
                {
                    _readyForDelete[i] = _readyForDelete[_readyForDelete.Count - 1];
                    _readyForDelete.RemoveAt(_readyForDelete.Count - 1);
                }
            }
        }



        internal void OnObjectNameUpdated(TorqueBase obj, String name)
        {
            if (name == null)
            {
                Assert.Fatal(name != null, "Should not set object name to null");
                name = String.Empty;
            }

            if (obj.Name != String.Empty)
            {
                SList<TorqueBase> startNode;

                if (_objectByName.TryGetValue(obj.Name, out startNode))
                {
                    Assert.Fatal(startNode != null, "Null node should have been culled");
                    SList<TorqueBase>.Remove(ref startNode, obj);

                    if (startNode != null)
                        _objectByName[obj.Name] = startNode;
                    else
                        _objectByName.Remove(obj.Name);
                }
            }
            if (name != String.Empty)
            {
                SList<TorqueBase> startNode = null;
                _objectByName.TryGetValue(name, out startNode);
                SList<TorqueBase>.InsertFront(ref startNode, obj);
                _objectByName[name] = startNode;
            }
        }

        #endregion


        #region Private, protected, internal fields

        Dictionary<UInt32, TorqueObject> _objectById = new Dictionary<UInt32, TorqueObject>();
        Dictionary<string, SList<TorqueBase>> _objectByName = new Dictionary<string, SList<TorqueBase>>();
        TorqueDictionary _dictionary = new TorqueDictionary();
        Dictionary<string, TorqueObjectType> _objectTypes = new Dictionary<string, TorqueObjectType>();
        List<TorqueCookieType> _cookieTypes = new List<TorqueCookieType>();
        List<TorqueSafePtr<TorqueObject>> _readyForDelete = new List<TorqueSafePtr<TorqueObject>>();
        uint _nextId = 1;
        TorqueFolder _rootFolder = new TorqueFolder();
        TorqueFolder _currentFolder;
        TorqueObjectType _aObjectType;
        bool _objTypesLocked = false;

        static TorqueObjectDatabase _db = new TorqueObjectDatabase();

        #endregion
    }
}