//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Reflection;
using GarageGames.Torque.Core;



namespace GarageGames.Torque.Util
{
    /// <summary>
    /// Miscellaneous static utility methods used by Torque X.
    /// </summary>
    public class TorqueUtil
    {
        #region Static methods, fields, constructors

        /// <summary>
        /// Return empty array of System.Type objects.  Required because Compact Framework does not currently support System.Type.EmptyTypes
        /// </summary>
        public static System.Type[] EmptyTypes
        {
            get { return _emptyTypes; }
        }

        /// <summary>
        /// Swap references to two objects.
        /// </summary>
        /// <typeparam name="T">Type of objects to swap.</typeparam>
        /// <param name="a">First object to swap.</param>
        /// <param name="b">Second object to swap.</param>
        public static void Swap<T>(ref T a, ref T b)
        {
            T tmp;
            tmp = a;
            a = b;
            b = tmp;
        }

        #region Quick and dirty random number generator interface

        /// <summary>
        /// Quick and dirty random number generator.  This generator is about twice as fast as built in XNA RNG.  The
        /// successive random numbers have a correlation of about 0.04, with a uniform distribution.
        /// Note: GetFastRandomInt only returns 24 bit positive numbers.  The .NET random number generator returns 
        /// 31 bit positive integers.
        /// </summary>
        /// <returns>Signed random integer</returns>
        static public int GetFastRandomInt()
        {
            _quickRandom = 1664525 * _quickRandom + 1013904223;
            return (int)(_quickRandom >> 8);
        }

        /// <summary>
        /// Quick and dirty random number generator.  This generator is about twice as fast as built in XNA RNG.  The
        /// successive random numbers have a correlation of about 0.04, with a uniform distribution.  Returns random integer 
        /// between 0 and max-1, inclusive.
        /// </summary>
        /// <returns>Random integer between 0 and max-1, inclusive.</returns>
        static public int GetFastRandomInt(int max)
        {
            return GetFastRandomInt(0, max);
        }

        /// <summary>
        /// Quick and dirty random number generator.  This generator is about twice as fast as built in XNA RNG.  The
        /// successive random numbers have a correlation of about 0.04, with a uniform distribution.  Returns random integer 
        /// between min and max-1, inclusive.
        /// </summary>
        /// <returns>Random integer between min and max-1, inclusive.</returns>
        static public int GetFastRandomInt(int min, int max)
        {
            return (GetFastRandomInt() % (max - min)) + min;
        }

        /// <summary>
        /// Quick and dirty random number generator.  This generator is about twice as fast as built in XNA RNG.  The
        /// successive random numbers have a correlation of about 0.04, with a uniform distribution.  Returns random float 
        /// between 0 and 1, inclusive.
        /// </summary>
        /// <returns>Random float between 0 and 1, inclusive.</returns>
        static public float GetFastRandomFloat()
        {
            return _quickRandomMult * (float)GetFastRandomInt();
        }

        /// <summary>
        /// Quick and dirty random number generator.  This generator is about twice as fast as built in XNA RNG.  The
        /// successive random numbers have a correlation of about 0.04, with a uniform distribution.  Returns random float 
        /// between 0 and max, inclusive.
        /// </summary>
        /// <returns>Random float between 0 and max, inclusive.</returns>
        static public float GetFastRandomFloat(float max)
        {
            return (_quickRandomMult * (float)GetFastRandomInt()) * max;
        }

        /// <summary>
        /// Quick and dirty random number generator.  This generator is about twice as fast as built in XNA RNG.  The
        /// successive random numbers have a correlation of about 0.04, with a uniform distribution.  Returns random float between 
        /// min and max, inclusive.
        /// </summary>
        /// <returns>Random float between min and max, inclusive.</returns>
        static public float GetFastRandomFloat(float min, float max)
        {
            return (_quickRandomMult * (float)GetFastRandomInt()) * (max - min) + min;
        }

        /// <summary>
        /// Sets seed for random number generator based on built-in .NET RNG.
        /// </summary>
        /// <param name="seed">Initial seed for random number generator.</param>
        static public void SetFastRandomSeed(uint seed)
        {
            _quickRandom = seed;
        }

        /// <summary>
        /// Gets seed for random number generator based on built-in .NET RNG.
        /// </summary>
        /// <param name="seed">Initial seed for random number generator.</param>
        static public uint GetFastRandomSeed()
        {
            return _quickRandom;
        }

        #endregion

        #region XNA random number generator interface

        /// <summary>
        /// Random number generator based on built-in .NET RNG.  Returns 31 bit positive integer.
        /// </summary>
        /// <returns>Random integer</returns>
        static public int GetRandomInt()
        {
            return _random.Next();
        }

        /// <summary>
        /// Random number generator based on built-in .NET RNG.  Returns random integer between
        /// 0 and max-1, inclusive.
        /// </summary>
        /// <returns>Random integer between 0 and max-1, inclusive.</returns>
        static public int GetRandomInt(int max)
        {
            return _random.Next(max);
        }

        /// <summary>
        /// Random number generator based on built-in .NET RNG.  Returns random integer between min 
        /// and max-1, inclusive.
        /// </summary>
        /// <returns>Random integer between min and max-1, inclusive.</returns>
        static public int GetRandomInt(int min, int max)
        {
            return _random.Next(min, max);
        }

        /// <summary>
        /// Random number generator based on built-in .NET RNG.  Returns random float between
        /// 0 and 1, inclusive.
        /// </summary>
        /// <returns>Random float between 0 and 1, inclusive.</returns>
        static public float GetRandomFloat()
        {
            return (float)_random.NextDouble();
        }

        /// <summary>
        /// Random number generator based on built-in .NET RNG.  Returns random float between
        /// 0 and max, inclusive.
        /// </summary>
        /// <returns>Random float between 0 and max, inclusive.</returns>
        static public float GetRandomFloat(float max)
        {
            return ((float)_random.NextDouble()) * max;
        }

        /// <summary>
        /// Random number generator based on built-in .NET RNG.  Returns random float between 
        /// min and max, inclusive.
        /// </summary>
        /// <returns>Random float between min and max, inclusive.</returns>
        static public float GetRandomFloat(float min, float max)
        {
            return (((float)_random.NextDouble()) * (max - min)) + min;
        }

        /// <summary>
        /// Sets seed for random number generator based on built-in .NET RNG.
        /// </summary>
        /// <param name="seed">Initial seed for random number generator.</param>
        static public void SetRandomSeed(int seed)
        {
            _random = new Random(seed);
        }

        #endregion

        /// <summary>
        /// Return an array of the specified type and size.  There is only one scratch array per type: I.e, calling this function
        /// twice in a row with the same type and size will return the same array.  This function is useful for creating temporary buffers 
        /// before handing them off to a vertex or index buffer.
        /// </summary>
        /// <typeparam name="T">Type of scratch array.</typeparam>
        /// <param name="requiredSize">Size of scratch array required.</param>
        /// <returns>Scratch array.</returns>
        public static T[] GetScratchArray<T>(int requiredSize)
        {
            object array;
            Type key = typeof(T);

            if (!_scratchArrays.TryGetValue(key, out array))
            {
                // create and add it
                array = new T[requiredSize];
                _scratchArrays[key] = array;
            }
            else
            {
                // found it, make sure it has sufficient size
                T[] tArray = (T[])array;
                GrowArray<T>(ref tArray, requiredSize);

                if (tArray != array)
                {
                    // new array due to growth.  update our dictionary 
                    _scratchArrays[key] = tArray;
                    // and our array pointer
                    array = tArray;
                }
            }

            return (T[])array;
        }

        /// <summary>
        /// Make sure passed array is large enough.  Intended for temporary work arrays which grow 
        /// to be large enough to accomodate their biggest consumer.  Contents of old array not preserved.
        /// </summary>
        /// <typeparam name="T">Type of array.</typeparam>
        /// <param name="array">Array to grow.</param>
        /// <param name="size">Minimum size required.</param>
        public static void GrowArray<T>(ref T[] array, Int32 size)
        {
            if (array == null || array.Length < size)
                array = new T[size];
        }

        /// <summary>
        /// Resize incoming array to be same size as previous array, copying incoming array into
        /// returned array if size not the same.  See GrowArray.
        /// </summary>
        /// <typeparam name="T">Type of array.</typeparam>
        /// <param name="array">Incoming array (possibly returns new array).</param>
        /// <param name="size">Required size.</param>
        public static void ResizeArray<T>(ref T[] array, Int32 size)
        {
            if (array == null)
            {
                array = new T[size];
            }
            else if (array.Length != size)
            {
                T[] newArray = new T[size];
                for (Int32 i = 0; i < Math.Min(array.Length, newArray.Length); i++)
                    newArray[i] = array[i];
                array = newArray;
            }
        }

        /// <summary>
        /// Gets an array of the values in an Enum object of type T.  Required because the Compact Framework does not have the Enum.GetValues() function.  This
        /// Creates garbage memory so it should not be called frequently.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T[] GetEnumValues<T>()
        {
            System.Type type = typeof(T);
            if (!type.IsEnum)
                throw new Exception("Type must be an Enum");

            List<T> tlist = new List<T>();

            FieldInfo[] fields = type.GetFields();
            foreach (FieldInfo f in fields)
            {
                if (!f.IsSpecialName && f.IsLiteral && f.IsStatic)
                    tlist.Add((T)f.GetValue(null));
            }

            return tlist.ToArray();
        }

        /// <summary>
        /// Chop the file extension off of a filename.
        /// </summary>
        /// <param name="filepath">Filename to truncate.</param>
        /// <returns>Input filename with extension removed.</returns>
        public static string ChopFileExtension(string filepath)
        {
            if (filepath == null || filepath == string.Empty)
                return filepath;

            if (Path.HasExtension(filepath))
                filepath = filepath.Substring(0, filepath.LastIndexOf("."));

            return filepath;
        }

        /// <summary>
        /// Returns log base 2 of the specified value. This was needed because Math.Log(x, base) isn't supported by the compact framework
        /// and thus wouldn't work on the XBox.
        /// </summary>
        /// <param name="value">The whose logarightm is to be found</param>
        /// <returns>The logarithm of the specified number in base 2.</returns>
        public static double GetLog2(double value)
        {
            return Math.Log(value) / _ln2;
        }

        static TorqueUtil()
        {
            _quickRandom = (uint)_random.Next();
        }

        static uint _quickRandom;
        static float _quickRandomMult = 1.0f / (float)(1 << 24);

        static private Random _random = new Random();
        static private Dictionary<Type, object> _scratchArrays = new Dictionary<Type, object>();
        static System.Type[] _emptyTypes = new System.Type[0];

        static float _ln2 = (float)Math.Log(2); // natural log of 2 cached for the static GetLog2 method

        #endregion
    }
}
