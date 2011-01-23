//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Microsoft.Xna.Framework;



namespace GarageGames.Torque.TS
{
    /// <summary>
    /// Flags that store information about what a sequence is animating. Position
    /// and rotation are always animated.
    /// </summary>
    [Flags]
    public enum SequenceFlags
    {
        /// <summary>
        /// No animation other than position and rotation
        /// </summary>
        None = 0,
        /// <summary>
        /// Scale is animated uniformly in x, y, and z
        /// </summary>
        UniformScale = 1 << 0,
        /// <summary>
        /// Axis aligned scale is animated 
        /// </summary>
        AlignedScale = 1 << 1,
        /// <summary>
        /// Scale around an arbitrary axis is animated
        /// </summary>
        ArbitraryScale = 1 << 2,
        /// <summary>
        /// The sequence can blend with another sequence
        /// </summary>
        Blend = 1 << 3,
        /// <summary>
        /// The sequence can be repeated
        /// </summary>
        Cyclic = 1 << 4,
        /// <summary>
        /// Tells the sequence to store information between updates so triggers can
        /// be activated at the correct time.
        /// </summary>
        MakePath = 1 << 5,
        /// <summary>
        /// The texture on the sequence is animated
        /// </summary>
        IflInit = 1 << 6,
        /// <summary>
        /// The animation has translucency at some point
        /// </summary>
        HasTranslucency = 1 << 7,
        /// <summary>
        /// Some type of scale is animated
        /// </summary>
        AnyScale = UniformScale | AlignedScale | ArbitraryScale
    }



    /// <summary>
    /// These flags determine what parts of an shape have been animated, and thus need
    /// to have their state updated.
    /// </summary>
    [Flags]
    public enum DirtyFlags
    {
        /// <summary>
        /// Nothing has changed
        /// </summary>
        NoneDirty = 0,
        /// <summary>
        /// Node spatial data has changed
        /// </summary>
        TransformDirty = 1 << 0,
        /// <summary>
        /// Visibility level has changed
        /// </summary>
        VisDirty = 1 << 1,
        /// <summary>
        /// Current animation frame has changed
        /// </summary>
        FrameDirty = 1 << 2,
        /// <summary>
        /// Current animation frame for the material has changed
        /// </summary>
        MatFrameDirty = 1 << 3,
        /// <summary>
        /// Decals are deprecated so this means nothing
        /// </summary>
        DecalDirty = 1 << 4,
        /// <summary>
        /// The texture has changed
        /// </summary>
        IflDirty = 1 << 5,
        /// <summary>
        /// The thread has been updated
        /// </summary>
        ThreadDirty = 1 << 6,
        /// <summary>
        /// Everything is dirty
        /// </summary>
        AllDirtyMask = TransformDirty | VisDirty | FrameDirty | MatFrameDirty | DecalDirty | IflDirty | ThreadDirty
    }



    /// <summary>
    /// Helper structure that is used as a string of bits of arbitrary length. Specific bits in
    /// the string can be set, tested, and cleared.
    /// </summary>
    public struct BitVector
    {

        #region Public methods

        /// <summary>
        /// Sets the bit at the specified index to 0.
        /// </summary>
        /// <param name="index">The index of the bit to clear.</param>
        public void Clear(int index)
        {
            _bits[index >> 5] &= (uint)~(1 << (index & 31));
        }



        /// <summary>
        /// Sets the bit at the specified index to 1.
        /// </summary>
        /// <param name="index">The index of the bit to set.</param>
        public void Set(int index)
        {
            _bits[index >> 5] |= (uint)(1 << (index & 31));
        }



        /// <summary>
        /// Tests if the bit at the specified index is set.
        /// </summary>
        /// <param name="index">The index of the bit to test.</param>
        /// <returns>True if the specified bit is set, false otherwise.</returns>
        public bool Test(int index)
        {
            return (index < _numBits && (_bits[index >> 5] & (uint)(1 << (index & 31))) != 0);
        }



        /// <summary>
        /// Sets every bit to 0.
        /// </summary>
        public void ClearAll()
        {
            SetAllTo(0);
        }



        /// <summary>
        /// Sets every bit to 1.
        /// </summary>
        public void SetAll()
        {
            SetAllTo(0xFFFFFFFF);
        }



        /// <summary>
        /// Sets the number of bits in the string.
        /// </summary>
        /// <param name="size">The number of bits in the string.</param>
        public void SetSize(int size)
        {
            _bits = new uint[(size + 31) >> 5];
            _numBits = size;
        }



        /// <summary>
        /// Sets a numeric value for each of the integers that make up the string of bits.
        /// </summary>
        /// <param name="val"></param>
        public void SetAllTo(uint val)
        {
            int maxIdx = (_numBits + 31) >> 5;
            for (int i = 0; i < maxIdx; i++)
                _bits[i] = val;
        }



        /// <summary>
        /// Tests whether or not any of the bits are set.
        /// </summary>
        /// <returns>True if a bit somewhere in the string is set, false otherwise.</returns>
        public bool TestAll()
        {
            // Test 32 bits at a time up to the last dword
            int maxIdx = _numBits >> 5;
            for (int i = 0; i < maxIdx; i++)
                if (_bits[i] != 0)
                    return true;

            if (maxIdx * 32 != _numBits)
            {
                int upto = _numBits - maxIdx * 32;
                upto = ((1 << upto) - 1);
                return (_bits[maxIdx] & (uint)upto) != 0;
            }
            return false;
        }



        /// <summary>
        /// Merge this BitVector with another one. This is basically an or operation.
        /// </summary>
        /// <param name="other">The BitVector to merge with.</param>
        public void Overlap(BitVector other)
        {
            int maxIdx = Math.Min((_numBits + 31) >> 5, (other._numBits + 31) >> 5);
            for (int i = 0; i < maxIdx; i++)
                _bits[i] |= other._bits[i];
        }



        /// <summary>
        /// Remove the bits from a BitVector from this one.
        /// </summary>
        /// <param name="other">The BitVector with bits set that are to be removed from this.</param>
        public void TakeAway(BitVector other)
        {
            int maxIdx = Math.Min((_numBits + 31) >> 5, (other._numBits + 31) >> 5);
            for (int i = 0; i < maxIdx; i++)
                _bits[i] &= ~other._bits[i];
        }



        /// <summary>
        /// Gets the next bit after the specified bit that is set.
        /// </summary>
        /// <param name="i">The index of the bit to start searching at.</param>
        public void Next(ref int i)
        {
            i++;
            if (i >= _numBits)
                return;

            // keep incrementing i until we hit a bit
            // skip a whole dword at a time if we can

            // starting bit...
            int idx = i >> 5;
            uint bit = 1U << (int)(i & 31);

            // mask less significant bits than initial i
            uint dword = _bits[idx] & ~(bit - 1);

            // _loop till we find a dword with a Set bit
            while (dword == 0)
            {
                i = (~31 & (int)(i + 32));// (i + 32) & 31;
                if (i >= _numBits)
                    return;
                dword = _bits[++idx];
                bit = 1;
            }

            // dword is not zero...so we are guaranteed to hit something
            while ((bit & dword) == 0)
            {
                bit <<= 1;
                i++;
            }
        }



        /// <summary>
        /// Copy the value of another BitVector into this one.
        /// </summary>
        /// <param name="from"></param>
        public void Copy(ref BitVector from)
        {
            SetSize(from._numBits);
            for (int i = 0; i < from._bits.Length; i++)
                _bits[i] = from._bits[i];
        }



        /// <summary>
        /// Gets the index of the first bit that is set.
        /// </summary>
        /// <returns>The index of the first bit that is set.</returns>
        public int Start()
        {
            int ret = -1;
            Next(ref ret);
            return ret;
        }



        /// <summary>
        /// Gets the number of bits in the string.
        /// </summary>
        /// <returns>The number of bits in the string.</returns>
        public int End()
        {
            return _numBits;
        }



        /// <summary>
        /// Reads in a BitVector from a stream.
        /// </summary>
        /// <param name="bin">The BinaryReader to read from.</param>
        public void Read(BinaryReader bin)
        {
            ClearAll();

            int numInts = bin.ReadInt32();
            int sz = bin.ReadInt32();
            SetSize(sz * 32);
            for (int i = 0; i < sz; i++)
                _bits[i] = bin.ReadUInt32();
        }

        #endregion


        #region Private, protected, internal fields

        uint[] _bits;
        int _numBits;

        #endregion
    }



    /// <summary>
    /// Sets up an arbitrary scale transform for animating arbitrary scales.
    /// </summary>
    public struct ArbitraryScale
    {
        /// <summary>
        /// The rotation quaternion defining the axis that scaling happens on.
        /// </summary>
        public Quaternion Rotate;



        /// <summary>
        /// The amount to scale.
        /// </summary>
        public Vector3 Scale;



        /// <summary>
        /// Sets the 
        /// </summary>
        public void SetIdentity()
        {
            Rotate.X = 0;
            Rotate.Y = 0;
            Rotate.Z = 0;
            Rotate.W = 1;
            Scale.X = 1.0f;
            Scale.Y = 1.0f;
            Scale.Z = 1.0f;
        }
    }



    /// <summary>
    /// Helper class for doing various animation related transforms.
    /// </summary>
    public class Transform
    {

        #region Static methods, fields, constructors

        /// <summary>
        /// Interpolate between one vector and another.
        /// </summary>
        /// <param name="p1">The start vector</param>
        /// <param name="p2">The end vector</param>
        /// <param name="t">Value between 0.0 and 1.0 to interpolate at</param>
        /// <param name="p12">The interpolated vector</param>
        public static void Interpolate(Vector3 p1, Vector3 p2, float t, out Vector3 p12)
        {
            p12 = new Vector3(p1.X + t * (p2.X - p1.X), p1.Y + t * (p2.Y - p1.Y), p1.Z + t * (p2.Z - p1.Z));
        }



        /// <summary>
        /// Interpolates between two floating point values.
        /// </summary>
        /// <param name="p1">The start value</param>
        /// <param name="p2">The end value</param>
        /// <param name="t">Value between 0.0 and 1.0 to interpolate at</param>
        /// <returns>The interpolated value</returns>
        public static float Interpolate(float p1, float p2, float t)
        {
            return p1 + t * (p2 - p1);
        }



        /// <summary>
        /// Interpolate between one quaternion and another.
        /// </summary>
        /// <param name="q1">The start quaternion</param>
        /// <param name="q2">The end quaternion</param>
        /// <param name="t">Value between 0.0 and 1.0 to interpolate at</param>
        /// <param name="q12">The interpolated quaternion</param>
        public static void Interpolate(Quaternion q1, Quaternion q2, float t, out Quaternion q12)
        {
            q12 = Quaternion.Slerp(q1, q2, t);
        }



        /// <summary>
        /// Interpolate between one arbitrary scale and another.
        /// </summary>
        /// <param name="s1">The start scale</param>
        /// <param name="s2">The end scale</param>
        /// <param name="t">Value between 0.0 and 1.0 to interpolate at</param>
        /// <param name="s12">The interpolated scale</param>
        public static void Interpolate(ref ArbitraryScale s1, ref ArbitraryScale s2, float t, out ArbitraryScale s12)
        {
            Transform.Interpolate(s1.Rotate, s2.Rotate, t, out s12.Rotate);
            Transform.Interpolate(s1.Scale, s2.Scale, t, out s12.Scale);
        }



        /// <summary>
        /// Sets a matrix with the specified rotation.
        /// </summary>
        /// <param name="q">The quaternion to set</param>
        /// <param name="mat">The resulting matrix</param>
        public static void SetMatrix(Quaternion q, out Matrix mat)
        {
            q.W *= -1.0f;
            mat = Matrix.CreateFromQuaternion(q);
        }



        /// <summary>
        /// Sets a matrix with the specified rotation and position.
        /// </summary>
        /// <param name="q">The quaternion to set</param>
        /// <param name="p">The translation to set</param>
        /// <param name="mat">The resulting matrix</param>
        public static void SetMatrix(Quaternion q, Vector3 p, out Matrix mat)
        {
            q.W *= -1.0f;
            mat = Matrix.CreateFromQuaternion(q);
            mat.M41 = p.X;
            mat.M42 = p.Y;
            mat.M43 = p.Z;
        }



        /// <summary>
        /// Applies a scale to a matrix.
        /// </summary>
        /// <param name="scale">The uniform scale value</param>
        /// <param name="mat">The matrix to scale</param>
        public static void ApplyScale(float scale, ref Matrix mat)
        {
            mat.M11 *= scale;
            mat.M21 *= scale;
            mat.M31 *= scale;
            mat.M12 *= scale;
            mat.M22 *= scale;
            mat.M32 *= scale;
            mat.M13 *= scale;
            mat.M23 *= scale;
            mat.M33 *= scale;
        }



        /// <summary>
        /// Applies a scale to a matrix.
        /// </summary>
        /// <param name="scale">The scale value</param>
        /// <param name="mat">The matrix to scale</param>
        public static void ApplyScale(Vector3 scale, ref Matrix mat)
        {
            mat.M11 *= scale.X;
            mat.M21 *= scale.X;
            mat.M31 *= scale.X;
            mat.M12 *= scale.Y;
            mat.M22 *= scale.Y;
            mat.M32 *= scale.Y;
            mat.M13 *= scale.Z;
            mat.M23 *= scale.Z;
            mat.M33 *= scale.Z;
        }



        /// <summary>
        /// Applies a scale to a matrix.
        /// </summary>
        /// <param name="scale">The scale value</param>
        /// <param name="mat">The matrix to scale</param>
        public static void ApplyScale(ArbitraryScale scale, ref Matrix mat)
        {
            Matrix mat2;
            Transform.SetMatrix(scale.Rotate, out mat2);
            Matrix mat3 = Matrix.Invert(mat2);
            Transform.ApplyScale(scale.Scale, ref mat3);
            mat2 = Matrix.Multiply(mat3, mat2);
            mat = Matrix.Multiply(mat2, mat);
        }

        #endregion
    }
}

