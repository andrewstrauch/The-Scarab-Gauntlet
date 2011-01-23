//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Reflection;

using GarageGames.Torque.Core;

namespace GarageGames.Torque.Util
{
    /// <summary>
    /// Utility class for creating object pools for objects of any type.  An object pool is a collection
    /// of objects which have already been used once and are ready for recycled use.  Using an object pooler
    /// can reduce memory churn in some cases.  Overuse of object poolers can cause more memory fragmentation
    /// than otherwise expected.
    /// </summary>
    public class ObjectPooler
    {
        /// <summary>
        /// Objects which inherit this interface have Reset called when they are added to object pools.
        /// Typically the result of calling Reset is to put the object in the state it is in after initial
        /// construction, but the exact behavior is left up to the class implementing IResetable.
        /// </summary>
        public interface IResetable
        {
            /// <summary>
            /// Called when object is placed into an object pool by the ObjectPooler.
            /// </summary>
            void Reset();
        }

        /// <summary>
        /// A node in the pool.
        /// </summary>
        class PoolNode
        {
            public object obj;
            public PoolNode next;
        }

        //======================================================
        #region Static methods, fields, constructors

        /// <summary>
        /// Construct another object of the same type as passed object.
        /// </summary>
        /// <param name="obj">Object whose type we duplicate.</param>
        /// <returns>New object.</returns>
        public static object Construct(object obj)
        {
            return Construct(obj.GetType());
        }

        /// <summary>
        /// Construct an object of the passed type.
        /// </summary>
        /// <param name="type">Type of object to create.</param>
        /// <returns>New object.</returns>
        public static object Construct(Type type)
        {
            // cache constructor info
            ConstructorInfo info;
            if (!_defaultConstructors.TryGetValue(type, out info))
            {
                info = type.GetConstructor(TorqueUtil.EmptyTypes);
                _defaultConstructors[type] = info;
            }

            Assert.Fatal(info != null, "ObjectPooler.Construct - No default constructor defined for type!");
            return info.Invoke(null);
        }

        /// <summary>
        /// Recover object of given type from object pool or create it
        /// if pool is empty.
        /// </summary>
        /// <typeparam name="T">Type of object to get.</typeparam>
        /// <returns>Recovered or new object.</returns>
        public static T CreateObject<T>() where T : new()
        {
            T t = (T)FindPooledObject(typeof(T));
            if (t == null)
                t = new T();

            return t;
        }

        /// <summary>
        /// Find object of passed type in object pool.  Returns null if none found.
        /// Use CreateObject(Type type) if you want an object returned no matter what.
        /// </summary>
        /// <param name="type">Type of object to find.</param>
        /// <returns>Found object or null if none found.</returns>
        public static object FindPooledObject(Type type)
        {
            PoolNode node;
            if (_pooler.TryGetValue(type, out node))
            {
                Assert.Fatal(node != null, "ObjectPooler.FindPooledObject - Unable to find pool for specified type.");

                if (node.next == null)
                    _pooler.Remove(type);

                else
                    _pooler[type] = node.next;

                node.next = _rootNode;
                _rootNode = node;

                return node.obj;
            }

            return null;
        }

        /// <summary>
        /// Find object of passed type in object pool or create
        /// a new one if none found.
        /// </summary>
        /// <param name="type">Type of object to find or construct.</param>
        /// <returns>Found or created object.</returns>
        public static object CreateObject(Type type)
        {
            object obj = FindPooledObject(type);
            return obj ?? Construct(type);
        }

        /// <summary>
        /// Place an object into the object pool.
        /// </summary>
        /// <param name="obj">Object to recycle.</param>
        public static void RecycleObject(object obj)
        {
            // get a new PoolNode and set it up
            PoolNode newNode;
            if (_rootNode == null)
            {
                newNode = new PoolNode();
            }

            else
            {
                newNode = _rootNode;
                _rootNode = _rootNode.next;
                newNode.next = null;
            }

            newNode.obj = obj;

            // add to lifo stack for this type
            PoolNode node;
            Type type = obj.GetType();
            if (_pooler.TryGetValue(type, out node))
                newNode.next = node;
            _pooler[type] = newNode;

            // reset object
            IResetable reset = obj as IResetable;
            if (reset != null)
                reset.Reset();
        }

        static PoolNode _rootNode;
        static Dictionary<Type, PoolNode> _pooler = new Dictionary<Type, PoolNode>();
        static Dictionary<Type, ConstructorInfo> _defaultConstructors = new Dictionary<Type, ConstructorInfo>();

        #endregion
    }
}
