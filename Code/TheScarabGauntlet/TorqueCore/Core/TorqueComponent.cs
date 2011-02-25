//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using GarageGames.Torque.Util;
using System.ComponentModel;

namespace GarageGames.Torque.Core
{
    /// <summary>
    /// Holds a collection of TorqueComponents.  A component container can
    /// be held by a TorqueObject or a TorqueComponent, but there is always
    /// a TorqueObject owner at the top of the hierarchy.
    /// </summary>
    public class TorqueComponentContainer : IDisposable
    {
        #region Public methods

        /// <summary>
        /// Create an enumerator to iterate through all the interfaces exposed by 
        /// components which match the passed type and name.
        /// </summary>
        /// <param name="type">Type name of interface to search for. Can include wildcard characters * and ?.</param>
        /// <param name="name">Name of interface to search for. Can include wildcard characters * and ?.</param>
        /// <returns>Enumerator</returns>
        public IEnumerable<TorqueInterface> InterfaceItr(String type, String name)
        {
            // need to find the object which holds this component container
            // in order to get the interface records.  This might seem a little
            // backwards (could just call the lookup routine on the object)
            // but it provides more forward api flexibility to have this routine
            // on the component container (e.g., could allow searching sub-ranges
            // of components at a later time).  Also nice API-wise because it puts searching
            // for component interfaces onto component container rather than object, even
            // if in the end the actual data is in the object.
            if (_components == null || _components.Count == 0)
                yield break;

            TorqueObject obj = _components[0].Owner;

            if (obj == null)
                // not yet registered, so no interfaces
                yield break;

            PatternMatch nameMatch = new PatternMatch(name);
            PatternMatch typeMatch = new PatternMatch(type);

            for (SList<TorqueObject.InterfaceCache> list = obj._GetCachedInterfaces(); list != null; list = list.Next)
            {
                if (list.Val.Name != null && !nameMatch.TestMatch(list.Val.Name))
                    // name required and doesn't match
                    continue;
                if (list.Val.Type != null && !typeMatch.TestMatch(list.Val.Type))
                    // type required and doesn't match
                    continue;

                if (list.Val.Interface != null)
                {
                    yield return list.Val.Interface;
                }
                else if (list.Val.Component != null)
                {
                    Assert.Fatal(!_inIterator, "TorqueComponentContainer.InterfaceItr - Cannot use interface iterator inside _GetInterfaces.");
                    _inIterator = true;
                    _baseGetListCalled = false;
                    _queryList.Clear();

                    list.Val.Component._GetInterfaces(typeMatch, nameMatch, _queryList);

                    _inIterator = false;

                    Assert.Fatal(_baseGetListCalled, "TorqueComponentContainer.InterfaceItr - A component's _GetInterfaces method failed to call parent's method.");

                    for (int index = 0; index < _queryList.Count; index++)
                        yield return _queryList[index];

                    _queryList.Clear();
                }
            }
        }



        /// <summary>
        /// Create an enumerator to iterate through all the interfaces exposed by 
        /// components which match the passed typename, name and interface type.
        /// </summary>
        /// <typeparam name="T">TorqueInterface type to search for.</typeparam>
        /// <param name="type">Type name of interface to search for.  Can include wildcard characters * and ?.</param>
        /// <param name="name">Name of interface to search for.  Can include wildcard characters * and ?.</param>
        /// <returns>Enumerator</returns>
        public IEnumerable<TorqueInterface> InterfaceItr<T>(String type, String name) where T : TorqueInterface
        {
            foreach (TorqueInterface iface in InterfaceItr(type, name))
            {
                T tface = iface as T;

                if (tface != null)
                    yield return tface;
            }
        }



        /// <summary>
        /// Find a TorqueInterface exposed by one of the components in the container which
        /// matches the passed typename and name.
        /// </summary>
        /// <param name="type">Type name of interface to search for.  Can include wildcard characters * and ?.</param>
        /// <param name="name">Name of interface to search for.  Can include wildcard characters * and ?.</param>
        /// <returns>The found interface or null if none found.</returns>
        public TorqueInterface GetInterface(String type, String name)
        {
            foreach (TorqueInterface iface in InterfaceItr(type, name))
            {
                // return first one we get
                return iface;
            }

            return null;
        }



        /// <summary>
        /// Find a TorqueInterface exposed by one of the components in the container which
        /// matches the passed typename, name and TorqueInterface type T.
        /// </summary>
        /// <typeparam name="T">TorqueInterface type to search for.</typeparam>
        /// <param name="type">Type name of interface to search for.  Can include wildcard characters * and ?.</param>
        /// <param name="name">Name of interface to search for.  Can include wildcard characters * and ?.</param>
        /// <returns>The found interface or null if none found.</returns>
        public T GetInterface<T>(String type, String name) where T : TorqueInterface
        {
            TorqueInterface iface = GetInterface(type, name);

            if (iface != null)
            {
                Assert.Fatal(iface is T, "TorqueComponentContainer.GetInterface - Wrong interface type.");
                return iface as T;
            }

            return null;
        }



        /// <summary>
        /// Returns a list of interfaces matching the passed typename, name, and TorqueInterface type T.
        /// </summary>
        /// <typeparam name="T">TorqueInterface type to search for.</typeparam>
        /// <param name="type">Type name of interface to search for.  Can include wildcard characters * and ?.</param>
        /// <param name="name">Name of interface to search for.  Can include wildcard characters * and ?.</param>
        /// <param name="list">List of TorqueInterfaces matching the criteria.</param>
        public void GetInterfaceList<T>(String type, String name, List<T> list) where T : TorqueInterface
        {
            foreach (TorqueInterface iface in InterfaceItr(type, name))
            {
                T tface = iface as T;

                if (tface != null)
                    list.Add(tface);
            }
        }



        /// <summary>
        /// Returns an enumerator to recursively iterate through all components in component container.
        /// </summary>
        /// <returns>The enumerator.</returns>
        public IEnumerator<TorqueComponent> GetEnumerator()
        {
            if (_components != null)
            {
                for (int index = 0; index < _components.Count; index++)
                {
                    yield return _components[index];

                    if (_components[index].HasComponents)
                    {
                        for (int compIndex = 0; compIndex < _components[index].Components._components.Count; compIndex++)
                            yield return _components[index].Components._components[compIndex];
                    }
                }
            }
        }



        /// <summary>
        /// Returns an enumerator to recursively iterate through all components of type T
        /// in component container.
        /// </summary>
        /// <returns>The enumerator.</returns>
        public IEnumerable<T> Itr<T>() where T : TorqueComponent
        {
            for (int index = 0; index < this._components.Count; index++)
            {
                T t = this._components[index] as T;

                if (t != null)
                    yield return t;
            }
        }



        /// <summary>
        /// Recursively find a component of type T in component container.
        /// </summary>
        /// <typeparam name="T">Component type to search for.</typeparam>
        /// <returns>Found component or null if none found.</returns>
        public T FindComponent<T>() where T : TorqueComponent
        {
            T t = null;

            if (_components == null)
                return null;

            for (int index = 0; index < this._components.Count; index++)
            {
                t = this._components[index] as T;

                if (t != null)
                    return t;
            }

            return null;
        }


        /// <summary>
        /// Recursively find a component by type in component container.
        /// </summary>
        /// <param name="type">The type to search for.</param>
        /// <returns></returns>
        public TorqueComponent FindComponent(Type type)
        {
            if (_components == null)
                return null;

            for (int index = 0; index < _components.Count; index++)
            {
                if (_components[index].GetType() == type)
                    return _components[index];
            }

            return null;
        }


        /// <summary>
        /// Recursively find all components of type T in component container.
        /// </summary>
        /// <typeparam name="T">Component type to search for.</typeparam>
        /// <returns>Found component or null if none found.</returns>
        public List<T> FindComponents<T>() where T : TorqueComponent
        {
            List<T> list = new List<T>();
            T t = null;

            for (int index = 0; index < this._components.Count; index++)
            {
                t = this._components[index] as T;

                if (t != null)
                    list.Add(t);
            }

            return list;
        }



        /// <summary>
        /// Recursively find a component of type T in component container.
        /// </summary>
        /// <typeparam name="T">Component type to search for.</typeparam>
        /// <returns>Found component or null if none found.</returns>
        public T FindComponentAs<T>() where T : class
        {
            if (_components == null)
                return null;

            for (int index = 0; index < this._components.Count; index++)
            {
                if (this._components[index] is T)
                    return this._components[index] as T;
            }

            return null;
        }



        /// <summary>
        /// Add a component to the component container.  It is illegal to add components after the 
        /// object has been registered.
        /// </summary>
        /// <param name="component">Component to add.</param>
        /// <returns>True if add succeeded, false otherwise.</returns>
        public bool AddComponent(TorqueComponent component)
        {
            if (_components == null)
                _components = new List<TorqueComponent>();

            if (_components.Count > 0)
                Assert.Fatal(_components[0].Owner == null, "TorqueComponentContainer.AddComponent - Cannot add component after object registered.");

            _components.Add(component);

            return true;
        }


        /// <summary>
        /// Removes a component from the component container.  Note that it is
        /// illegal to remove a component until the object has been unregistered.
        /// </summary>
        /// <param name="component">The component to remove</param>
        public void RemoveComponent(TorqueComponent component)
        {
            if (_components == null || _components.Count == 0)
                return;

            Assert.Fatal(_components[0].Owner == null, "TorqueComponentContainer.RemoveComponent - Cannot remove components until the object is unregistered.");
            _components.Remove(component);
        }


        /// <summary>
        /// Calls OnLoaded on each of the components in the container.
        /// </summary>
        public void OnLoaded()
        {
            if (_components == null)
                return;

            for (int index = 0; index < _components.Count; index++)
            {
                _components[index].OnLoaded();

                if (_components[index].HasComponents)
                    _components[index].OnLoaded();
            }
        }



        /// <summary>
        /// Return the number of components in the container (top level only).
        /// </summary>
        /// <returns>Number of components.</returns>
        public int GetNumComponents()
        {
            return _components != null ? _components.Count : 0;
        }



        /// <summary>
        /// Returns the indexed component.
        /// </summary>
        /// <param name="idx">Index of component to return</param>
        /// <returns>Component indexed or null if index out of range.</returns>
        public TorqueComponent GetComponentByIndex(int idx)
        {
            Assert.Fatal(_components != null && idx >= 0 && idx < _components.Count, "TorqueComponentContainer.GetComponentByIndex - Index out of range.");
            return _components != null ? _components[idx] : null;
        }



        /// <summary>
        /// Copy one component container to another.  Component containers must contain the same
        /// number and type of components and in the same order.  Used by TorqueObject.Clone().
        /// </summary>
        /// <param name="obj">Component container to copy over.</param>
        public void CopyTo(TorqueComponentContainer obj)
        {
            if (_components != null && obj._components != null)
            {
                // must make sure all the components match
                if (_components.Count != obj._components.Count)
                {
                    Assert.Fatal(false, "TorqueComponentContainer.CopyTo - Error while cloning components: wrong number of components!");
                    obj._components = null;
                    return;
                }
                for (int i = 0; i < obj._components.Count; i++)
                {
                    if (_components[i].GetType() != obj._components[i].GetType())
                    {
                        Assert.Fatal(false, "TorqueComponentContainer.CopyTo - Error while cloning components: wrong component type!");
                        obj._components = null;
                        return;
                    }
                    _components[i].CopyTo(obj._components[i]);
                    if (_components[i].HasComponents)
                        _components[i].Components.CopyTo(obj._components[i].Components);
                }
                return;
            }
            else if (_components == null && obj._components == null)
            {
                return;
            }

            Assert.Fatal(false, "TorqueComponentContainer.CopyTo - Error while cloning components: empty component list!");
            obj._components = null;
        }



        /// <summary>
        /// Clone a component container and all it's components.  This does not implement ICloneable
        /// interface because it returns an object of type TorqueComponentContainer rather than 'object'.
        /// </summary>
        /// <returns>The cloned component container.</returns>
        public TorqueComponentContainer Clone()
        {
            TorqueComponentContainer container = null;
            if (_components != null && _components.Count != 0)
            {
                container = Util.ObjectPooler.CreateObject<TorqueComponentContainer>();

                for (int index = 0; index < _components.Count; index++)
                    container.AddComponent((TorqueComponent)_components[index].Clone());
            }
            return container;
        }



        /// <summary>
        /// Uses reflection to test that a copy operation copied all public properties not
        /// marked with TorqueCloneIgnore attributes and that any property marked with 
        /// TorqueCloneDeep are properly cloned deeply.  This method is only called in debug
        /// and does nothing on the compact framework (XBOX 360).
        /// </summary>
        /// <param name="components">Component container to verify as duplicate of this one.</param>
        [Conditional("TRACE")]
        internal void TestCopy(TorqueComponentContainer components)
        {
            int usCount = _components == null ? 0 : _components.Count;
            int themCount = components._components == null ? 0 : components._components.Count;
            if (usCount != themCount)
            {
                Assert.Fatal(false, "TorqueComponentContainer.TestCopy - Wrong number of components in cloned object");
                return;
            }

            for (int i = 0; i < usCount; i++)
            {
                Util.TestObjectCopy.Test(_components[i], components._components[i]);
                if (_components[i].HasComponents)
                    _components[i].Components.TestCopy(components._components[i].Components);
            }
        }

        #endregion


        #region Private, protected, internal methods

        internal void _RegisterInterfaces(TorqueObject owner)
        {
            if (_components == null)
                return;

            for (int index = 0; index < _components.Count; index++)
            {
                _components[index]._owner = owner;
                _components[index]._RegisterInterfaces(owner);

                if (_components[index].HasComponents)
                    _components[index].Components._RegisterInterfaces(owner);
            }
        }



        internal bool _RegisterComponents(TorqueObject owner)
        {
            if (_components == null)
                return true;


            for (int index = 0; index < _components.Count; index++)
            {
                if (!_components[index]._OnRegister(owner))
                    return false;

                Assert.Fatal(_components[index].Owner == owner, "TorqueComponentContainer._RegisterComponents - _OnRegister failed to call base._OnRegister, see " + _components[index].GetType() + ".");

                if (_components[index].HasComponents && !_components[index].Components._RegisterComponents(owner))
                    return false;
            }
            return true;
        }



        internal void _PostRegisterComponents()
        {
            if (_components == null)
                return;


            for (int index = 0; index < _components.Count; index++)
            {
                _components[index]._PostRegister();

                if (_components[index].HasComponents)
                    _components[index].Components._PostRegisterComponents();
            }
        }



        internal bool _UnregisterComponents()
        {
            if (_components == null)
                return true;

            // Reset each child component.
            for (int index = 0; index < _components.Count; index++)
            {
                _components[index]._OnUnregister();

                Assert.Fatal(_components[index].Owner == null, "TorqueComponentContainer._UnregisterComponents - _OnUnregister failed to call base._OnUnregister, see " + _components[index].GetType() + ".");

                if (_components[index].HasComponents)
                    _components[index].Components._UnregisterComponents();
            }

            return true;
        }



        internal void _Reset(bool preserveComponents)
        {
            if (_components != null)
            {

                for (int index = 0; index < _components.Count; index++)
                {
                    TorqueComponentContainer container = null;

                    if (_components[index].HasComponents && preserveComponents)
                    {
                        // detach container and reattach since we preserve components
                        container = _components[index].Components;
                        _components[index].Components = null;
                        _components[index].Components._Reset(preserveComponents);
                        container._Reset(true);
                    }

                    _components[index].Reset();
                    _components[index].Components = container;
                }
            }
        }

        #endregion


        #region Private, protected, internal fields

        protected List<TorqueComponent> _components;
        static List<TorqueInterface> _queryList = new List<TorqueInterface>();
        internal static bool _inIterator;
        internal static bool _baseGetListCalled;

        #endregion

        #region IDisposable Members

        public virtual void Dispose()
        {
            // At this point all the components should 
            // have been unregistered.
            this._components.Clear();
            this._components = null;
        }

        #endregion
    }



    /// <summary>
    /// TorqueComponents can be added to TorqueObjects or other TorqueComponents
    /// to provide pieces of re-usable functionality. Component are added to objects
    /// before being added to the object database. They are initialized and unitialized
    /// at the same time as the TorqueObject which contains them via the _OnRegister
    /// and _OnUnregister methods. Components can expose interfaces to all other components
    /// inside the _RegisterInterfaces method.
    /// <remarks>
    /// The main reason for adding one component to another is to package them together for copying 
    /// between multple objects.  When a TorqueComponent is added to another TorqueComponent it 
    /// behaves very much like it was simply added to the owning TorqueObject. When searching for an
    /// interface held by a component inside a component container, the search always begins at the owning
    /// TorqueObject, even if the component container is from a component not at the top level of the
    /// hierarchy. However, if using the FindComponent method on a component container, it will search
    /// in that component container and all child containers only, so if using FindComponent to look up
    /// components, one can use the component hierarchy to scope the search.
    /// </remarks>
    /// </summary>
    public class TorqueComponent : TorqueBase, ICloneable, IDisposable
    {
        #region Public properties, operators, constants, and enums

        /// <summary>
        /// True if container contains components.
        /// </summary>
        [BrowsableAttribute(false)]
        public bool HasComponents
        {
            get { return (_components != null) && (_components.GetNumComponents() != 0); }
        }



        /// <summary>
        /// Container for TorqueComponents held by this component.  Generally want to test HasComponents property before
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
            internal set // this is ONLY for use by the XML deserializer.  changing components on the fly will cause headaches for you.  don't do it.
            {
                _components = value;
            }
        }



        /// <summary>
        /// TorqueObject which holds this component (and all sibling components).
        /// </summary>
        [BrowsableAttribute(false)]
        public TorqueObject Owner
        {
            get { return _owner; }
        }

        #endregion


        #region Public methods

        /// <summary>
        /// Implements IResetable interface.  This method is called before an object
        /// is stored in an object pooler.  Data that needs to be reset on recycled
        /// objects should be reset here.  See Pool and PoolWithComponents properties
        /// on TorqueObject for more information.
        /// </summary>
        public override void Reset()
        {
            base.Reset();

            if (HasComponents)
                Components._Reset(false);

            _owner = null;
        }



        /// <summary>
        /// Called by Clone. Objects need to implement this method to copy all public properties not marked
        /// with TorqueCloneIgnore attribute.
        /// </summary>
        /// <param name="obj">The object to be copied over.</param>
        virtual public void CopyTo(TorqueComponent obj) { }



        /// <summary>
        /// Clone component using object pooler and CopyTo.
        /// </summary>
        /// <returns>The new cloned object.</returns>
        public object Clone()
        {
            TorqueComponent copy = (TorqueComponent)Util.ObjectPooler.CreateObject(GetType());
            CopyTo(copy);
            copy._owner = null;

            if (_components != null)
                copy._components = _components.Clone();

            return copy;
        }

        #endregion


        #region Private, protected, internal methods

        /// <summary>
        /// This method is called when searching for interfaces that are not cached directly on the TorqueObject.  
        /// Override this method in order to expose interfaces without creating them when initializing the component.
        /// For _GetInterfaces to be called, a RegesterCachedInterfaces call must be made on the owner which
        /// matches the _GetInterfaces typeMatch and nameMatch.
        /// </summary>
        /// <param name="typeMatch">Type name to match.</param>
        /// <param name="nameMatch">Name to match.</param>
        /// <param name="list">List of found interfaces.</param>
        virtual protected internal void _GetInterfaces(PatternMatch typeMatch, PatternMatch nameMatch, List<TorqueInterface> list)
        {
            TorqueComponentContainer._baseGetListCalled = true;
        }



        /// <summary>
        /// This method is called before _OnRegister and should be used to register all the interfaces your object
        /// exposes.  Call owner.RegisterCachedInterfaces for each interface your component exposes.  Also call 
        /// owner.RegisterCachedInterfaces for each _GetInterface callback your component needs.
        /// </summary>
        /// <param name="owner">The owner of this component.</param>
        virtual protected internal void _RegisterInterfaces(TorqueObject owner) { }



        /// <summary>
        /// This method is called when the owning object is registered.  Any initialization needed by the component
        /// should be done here.  It is important to call base._OnRegister and respect it's return value like so:
        /// 
        /// bool _OnRegister(TorqueObject owner)
        /// {
        ///     if (!base._OnRegister(owner))
        ///         return false;
        /// 
        ///     // ...
        /// 
        ///     return true;
        /// }
        /// 
        /// </summary>
        /// <param name="owner">The owner of this component.</param>
        /// <returns>True if initialization succesful.</returns>
        virtual protected internal bool _OnRegister(TorqueObject owner)
        {
            _owner = owner;

            return true;
        }



        /// <summary>
        /// This method is called when the owning object is unregistered.  Any cleanup needed by your component should
        /// be done here.
        /// </summary>
        virtual protected internal void _OnUnregister()
        {
            _owner = null;
        }



        /// <summary>
        /// This method is called after all components have been registered.
        /// </summary>
        virtual protected internal void _PostRegister() { }

        #endregion


        #region Private, protected, internal fields

        TorqueComponentContainer _components;
        internal TorqueObject _owner;

        #endregion

        #region IDisposable Members

        public override void Dispose()
        {
            _IsDisposed = true;
            if (this.HasComponents)
            {
                foreach (TorqueComponent c in this.Components)
                {
                    c._owner = null;
                    c.Dispose();
                }
            }
            this._ResetRefs();
            _owner = null;
            base.Dispose();
        }

        #endregion
    }
}
