//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System.Collections.Generic;
using GarageGames.Torque.Core;
using System;

namespace GarageGames.Torque.Util
{
    /// <summary>
    /// Pools a list of objects so new instances aren't constantly reallocated. This class should be used in place
    /// of a standard list in cases where the list is generated and cleared often.
    /// </summary>
    /// <typeparam name="T">The type of object to pool.</typeparam>
    public class SimpleObjectPool<T> : IDisposable where T : new()
    {
        #region Public properties, operators, constants, and enums

        /// <summary>
        /// Resets the list (basically clears the list, by setting the index to 0).
        /// </summary>
        public void Reset()
        {
            _poolIndex = 0;
        }

        /// <summary>
        /// Returns a new instance of the specified type, allocating it if necessary.
        /// </summary>
        /// <returns></returns>
        public T CreateObject()
        {
            if (_poolIndex >= _pool.Count)
                _pool.Add(new T());

            Assert.Fatal(_poolIndex < _pool.Count, "SimpleObjectPool.CreateObject - Pool somehow got undersized!");
            return _pool[_poolIndex++];
        }

        #endregion


        #region Private, protected, internal fields

        List<T> _pool = new List<T>();
        int _poolIndex;

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            _pool.Clear();
            _pool = null;
        }

        #endregion
    }
}
