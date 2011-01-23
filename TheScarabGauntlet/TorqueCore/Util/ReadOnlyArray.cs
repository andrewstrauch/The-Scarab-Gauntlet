//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System.Collections.Generic;
using GarageGames.Torque.Core;



namespace GarageGames.Torque.Util
{
    /// <summary>
    /// Utility object which wraps a List or Array and allows only
    /// read-only access.  Note: although the List/Array cannot be
    /// modified, ReadOnlyArray does not prevent the elements of
    /// the List/Array from being modified if they are ref types.
    /// </summary>
    /// <typeparam name="T">Type of List/Array elements.</typeparam>
    public struct ReadOnlyArray<T>
    {
        #region Constructors

        /// <summary>
        /// Wrap an Array in a ReadOnlyArray.
        /// </summary>
        /// <param name="array">Array to wrap.</param>
        public ReadOnlyArray(T[] array)
        {
            _array = array;
            _list = null;
        }

        /// <summary>
        /// Wrap a List in a ReadOnlyArray.
        /// </summary>
        /// <param name="list">List to wrap.</param>
        public ReadOnlyArray(List<T> list)
        {
            _list = list;
            _array = null;
        }

        #endregion


        #region Public properties, operators, constants, and enums

        /// <summary>
        /// Number of elements in wrapped List/Array.
        /// </summary>
        public int Count
        {
            get
            {
                if (_array != null)
                    return _array.Length;

                if (_list != null)
                    return _list.Count;

                return 0;
            }
        }

        /// <summary>
        /// Read-only access to List/Array elements.
        /// </summary>
        /// <param name="idx">Index of element to access.</param>
        /// <returns>Indexed element in List/Array.</returns>
        public T this[int idx]
        {
            get
            {
                if (_array != null)
                    return _array[idx];

                if (_list != null)
                    return _list[idx];

                Assert.Fatal(false, "ReadOnlyArray[] - No list or array to index!");
                return default(T);
            }
        }

        #endregion


        #region Public methods

        /// <summary>
        /// Returns enumerator for iterating through elements of read only array.
        /// </summary>
        /// <returns>Enumerator.</returns>
        public IEnumerator<T> GetEnumerator()
        {
            if (_array != null)
            {
                foreach (T t in _array)
                    yield return t;
            }

            else if (_list != null)
            {
                foreach (T t in _list)
                    yield return t;
            }
        }

        #endregion


        #region Private, protected, internal fields

        T[] _array;
        List<T> _list;

        #endregion
    }
}
