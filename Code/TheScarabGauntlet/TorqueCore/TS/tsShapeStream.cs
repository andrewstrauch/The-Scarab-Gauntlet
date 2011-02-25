//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GarageGames.Torque.MathUtil;
using U8 = System.Byte;
using S8 = System.SByte;



namespace GarageGames.Torque.TS
{
    /// <summary>
    /// Helper class for reading dts files.
    /// </summary>
    class ShapeStream
    {
        #region Constructors

        public ShapeStream(byte[] buffer, int offset32, int offset16, int offset8, int count32, int count16, int count8)
        {
            MemoryStream memStream32 = new MemoryStream(buffer, offset32, count32);
            _dwords = new BinaryReader(memStream32, new System.Text.ASCIIEncoding());

            MemoryStream memStream16 = new MemoryStream(buffer, offset16, count16);
            _words = new BinaryReader(memStream16, new System.Text.ASCIIEncoding());

            MemoryStream memStream8 = new MemoryStream(buffer, offset8, count8);
            _bytes = new BinaryReader(memStream8, new System.Text.ASCIIEncoding());
        }

        #endregion


        #region Public methods

        /// <summary>
        /// Read a string.
        /// </summary>
        /// <returns>The string.</returns>
        public String ReadCString()
        {
            StringBuilder sb = new StringBuilder(32);
            char ch;
            while ((ch = _bytes.ReadChar()) != '\0')
                sb.Append(ch);

            return sb.ToString();
        }



        /// <summary>
        /// Read a float.
        /// </summary>
        /// <returns>The float</returns>
        public float ReadF32()
        {
            return _dwords.ReadSingle();
        }



        /// <summary>
        /// Read a series of floats.
        /// </summary>
        /// <param name="count">The number of floats to read.</param>
        /// <returns>The float array.</returns>
        public float[] ReadF32s(int count)
        {
            float[] ret = new float[count];
            for (uint i = 0; i < count; i++)
                ret[i] = _dwords.ReadSingle();

            return ret;
        }



        /// <summary>
        /// Read an unsigned int.
        /// </summary>
        /// <returns>The int.</returns>
        public uint ReadU32()
        {
            return _dwords.ReadUInt32();
        }



        /// <summary>
        /// Read a series of unsigned integers.
        /// </summary>
        /// <param name="count">The number of integers to read.</param>
        /// <returns>The integer array.</returns>
        public uint[] ReadU32s(int count)
        {
            uint[] ret = new uint[count];
            for (uint i = 0; i < count; i++)
                ret[i] = _dwords.ReadUInt32();
            return ret;
        }



        /// <summary>
        /// Read an integer.
        /// </summary>
        /// <returns>The integer.</returns>
        public int ReadS32()
        {
            return _dwords.ReadInt32();
        }



        /// <summary>
        /// Read a series of integers.
        /// </summary>
        /// <param name="count">The number of integers to read.</param>
        /// <returns>The integer array.</returns>
        public int[] ReadS32s(int count)
        {
            int[] ret = new int[count];
            for (uint i = 0; i < count; i++)
                ret[i] = _dwords.ReadInt32();
            return ret;
        }



        /// <summary>
        /// Read an unsigned short.
        /// </summary>
        /// <returns>The short.</returns>
        public ushort ReadU16()
        {
            return _words.ReadUInt16();
        }



        /// <summary>
        /// Read a series of unsigned shorts.
        /// </summary>
        /// <param name="count">The number of shorts to read.</param>
        /// <returns>The short array.</returns>
        public ushort[] ReadU16s(int count)
        {
            ushort[] ret = new ushort[count];
            for (uint i = 0; i < count; i++)
                ret[i] = _words.ReadUInt16();
            return ret;
        }



        /// <summary>
        /// Read a short.
        /// </summary>
        /// <returns>The short.</returns>
        public short ReadS16()
        {
            return _words.ReadInt16();
        }



        /// <summary>
        /// Read a series of shorts.
        /// </summary>
        /// <param name="count">The number of shorts to read.</param>
        /// <returns>The short array.</returns>
        public short[] ReadS16s(int count)
        {
            short[] ret = new short[count];
            for (uint i = 0; i < count; i++)
                ret[i] = _words.ReadInt16();
            return ret;
        }



        /// <summary>
        /// Read an unsigned byte.
        /// </summary>
        /// <returns>The byte.</returns>
        public U8 ReadU8()
        {
            return _bytes.ReadByte();
        }



        /// <summary>
        /// Read a series of unsigned bytes.
        /// </summary>
        /// <param name="count">The number of bytes to read.</param>
        /// <returns>The byte array.</returns>
        public U8[] ReadU8s(int count)
        {
            return _bytes.ReadBytes((int)count);
        }



        /// <summary>
        /// Read a byte.
        /// </summary>
        /// <returns>The byte.</returns>
        public S8 ReadS8()
        {
            return _bytes.ReadSByte();
        }



        /// <summary>
        /// Read two floats as a vector.
        /// </summary>
        /// <param name="pnt">The vector to receive the floats.</param>
        public void ReadPoint2F(ref Vector2 pnt)
        {
            pnt.X = ReadF32();
            pnt.Y = ReadF32();
        }

        /// <summary>
        /// Read a series of floats as 2 dimensional vectors.
        /// </summary>
        /// <param name="count">The number of vectors to read.</param>
        /// <returns>The vector array.</returns>
        public Vector2[] ReadPoint2Fs(int count)
        {
            Vector2[] ret = new Vector2[count];
            for (int i = 0; i < ret.Length; i++)
                ReadPoint2F(ref ret[i]);

            return ret;
        }



        /// <summary>
        /// Read three floats as a vector.
        /// </summary>
        /// <param name="pnt">The vector to receive the floats.</param>
        public void ReadPoint3F(ref Vector3 pnt)
        {
            pnt.X = ReadF32();
            pnt.Y = ReadF32();
            pnt.Z = ReadF32();
        }



        /// <summary>
        /// Read a series of floats as 3 dimensional vectors.
        /// </summary>
        /// <param name="count">The number of vectors to read.</param>
        /// <returns>The vector array.</returns>
        public Vector3[] ReadPoint3Fs(int count)
        {
            Vector3[] ret = new Vector3[count];
            for (int i = 0; i < ret.Length; i++)
                ReadPoint3F(ref ret[i]);
            return ret;
        }



        /// <summary>
        /// Read four shorts as a quaternion.
        /// </summary>
        /// <param name="q">The quaternion to receive the data.</param>
        public void ReadQuat16(ref Quat16 q)
        {
            q.X = ReadS16();
            q.Y = ReadS16();
            q.Z = ReadS16();
            q.W = ReadS16();
        }



        /// <summary>
        /// Read a series of quaternions.
        /// </summary>
        /// <param name="count">The number of quaternions to read.</param>
        /// <returns>The quaternion array.</returns>
        public Quat16[] ReadQuat16s(int count)
        {
            Quat16[] ret = new Quat16[count];
            for (int i = 0; i < ret.Length; i++)
                ReadQuat16(ref ret[i]);
            return ret;
        }

        public void ReadColor(ref Color c)
        {
            uint temp = ReadU32();

            c.R = (U8)(temp);
            c.G = (U8)(temp >> 8);
            c.B = (U8)(temp >> 16);
            c.A = (U8)(temp >> 24);
        }

        public Color[] ReadColors(int count)
        {
            Color[] ret = new Color[count];
            for (int i = 0; i < ret.Length; i++)
                ReadColor(ref ret[i]);
            return ret;
        }



        /// <summary>
        /// For debugging file errors.
        /// </summary>
        public void CheckGuard()
        {
            _saveGuard32 = ReadU32();
            _saveGuard16 = ReadU16();
            _saveGuard8 = ReadU8();

            Debug.Assert(_saveGuard32 == _memGuard32, "TSShapeStream.CheckGuard - Error in file");
            Debug.Assert(_saveGuard16 == _memGuard16, "TSShapeStream.CheckGuard - Error in file");
            Debug.Assert(_saveGuard8 == _memGuard8, "TSShapeStream.CheckGuard - Error in file");

            _memGuard32++;
            _memGuard16++;
            _memGuard8++;
        }

        #endregion


        #region Private, protected, internal fields

        uint _saveGuard32;
        ushort _saveGuard16;
        U8 _saveGuard8;
        uint _memGuard32;
        ushort _memGuard16;
        U8 _memGuard8;
        BinaryReader _bytes;
        BinaryReader _words;
        BinaryReader _dwords;

        #endregion


    }
}
