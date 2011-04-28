//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using GarageGames.Torque.Core;



namespace GarageGames.Torque.Util
{
    /// <summary>
    /// CompoundKey can be used as a key in a dictionary.  It combines
    /// two other types, overriding GetHashCode to combine the hash of
    /// each type.  It also appropriately overrides the equals and not
    /// equals operators.
    /// </summary>
    /// <typeparam name="S">The type of the first sub key.</typeparam>
    /// <typeparam name="T">The type of the second sub key.</typeparam>
    public struct CompoundKey<S, T>
    {
        #region Static methods, fields, constructors

        /// <summary>
        /// Return true if sub keys of each key are equal.
        /// </summary>
        /// <param name="x">First key to compare.</param>
        /// <param name="y">Second key to compare.</param>
        public static bool operator ==(CompoundKey<S, T> x, CompoundKey<S, T> y)
        {
            return x._a.Equals(y._a) && x._b.Equals(y._b);
        }

        /// <summary>
        /// Return false if sub-keys of each key are equal.
        /// </summary>
        /// <param name="x">First key to compare.</param>
        /// <param name="y">Second key to compare.</param>
        public static bool operator !=(CompoundKey<S, T> x, CompoundKey<S, T> y)
        {
            return !(x == y);
        }

        #endregion


        #region Constructors

        /// <summary>
        /// Creates a compound key with sub keys a and b.
        /// </summary>
        /// <param name="a">The first key.</param>
        /// <param name="b">The second key.</param>
        public CompoundKey(S a, T b)
        {
            _a = a;
            _b = b;
        }

        #endregion


        #region Public methods

        /// <summary>
        /// Return a hash code which is a combination of the hash of each
        /// of the sub keys.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return _a.GetHashCode() ^ _b.GetHashCode();
        }

        /// <summary>
        /// Return true if both objects are CompoundKeys and both
        /// sub-objects of each key are equal.
        /// </summary>
        /// <param name="x">First key to compare.</param>
        /// <param name="y">Second key to compare.</param>
        public override bool Equals(object obj)
        {
            return obj is CompoundKey<S, T> && this == (CompoundKey<S, T>)obj;
        }

        #endregion


        #region Private, protected, internal fields

        S _a;
        T _b;

        #endregion
    }
}
