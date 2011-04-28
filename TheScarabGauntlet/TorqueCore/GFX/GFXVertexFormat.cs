//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;



namespace GarageGames.Torque.GFX
{
    /// <summary>
    /// Contains Vertex format used by the Torque engine and its built-in materials.  
    /// </summary>
    public class GFXVertexFormat
    {
        /// <summary>
        /// Struct which defines the vertex format.  Name is abbreviations for each of its member types 
        /// (a convention borrowed from TSE).
        /// </summary>
        public struct PCTTBN
        {
            public Vector3 _position;
            public Color _color;
            public Vector2 _texture1;
            public Vector2 _texture2;
            public Vector4 _tangent;
            public Vector4 _normal;



            public PCTTBN(Vector3 position, Color color, Vector2 texture1, Vector2 texture2, Vector4 tangent, Vector4 normal)
            {
                _position = position;
                _color = color;
                _texture1 = texture1;
                _texture2 = texture2;
                _tangent = tangent;
                _normal = normal;
            }



            public Vector3 Position
            {
                get { return _position; }
                set { _position = value; }
            }



            public Color Color
            {
                get { return _color; }
                set { _color = value; }
            }



            public Vector2 TextureCoordinate
            {
                get { return _texture1; }
                set { _texture1 = value; }
            }



            public Vector2 TextureCoordinate2
            {
                get { return _texture2; }
                set { _texture2 = value; }
            }



            public Vector4 Tangent
            {
                get { return _tangent; }
                set { _tangent = value; }
            }



            public Vector4 Normal
            {
                get { return _normal; }
                set { _normal = value; }
            }
        }



        /// <summary>
        /// Returns the size of the PCTTBN vertex structure.
        /// </summary>
        public static int VertexSize
        {
            get
            {
                // Should be 64!
                if (_vertexSize == 0)
                    _vertexSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(GFXVertexFormat.PCTTBN));
                return _vertexSize;
            }
        }



        /// <summary>
        /// Get the vertex declaration for the PCTTBN format.
        /// </summary>
        /// <param name="d3d"></param>
        /// <returns></returns>
        public static VertexDeclaration GetVertexDeclaration(GraphicsDevice d3d)
        {
            if (d3d != _d3d || _vd == null || _vd.IsDisposed)
            {
                _vd = new VertexDeclaration(d3d, GFXVertexFormat.PCTTBNDeclaration);
                _d3d = d3d;
            }
            return _vd;
        }



        /// <summary>
        /// Vertex element array for the PCTTBN format.
        /// </summary>
        static readonly VertexElement[] PCTTBNDeclaration =
            {
                new VertexElement(0,  0, VertexElementFormat.Vector3, VertexElementMethod.Default, VertexElementUsage.Position, 0),
                new VertexElement(0, 12, VertexElementFormat.Color, VertexElementMethod.Default, VertexElementUsage.Color, 0),
                new VertexElement(0, 16, VertexElementFormat.Vector2, VertexElementMethod.Default, VertexElementUsage.TextureCoordinate, 0),
                new VertexElement(0, 24, VertexElementFormat.Vector2, VertexElementMethod.Default, VertexElementUsage.TextureCoordinate, 1),
                new VertexElement(0, 32, VertexElementFormat.Vector4, VertexElementMethod.Default, VertexElementUsage.Tangent, 0),
                new VertexElement(0, 48, VertexElementFormat.Vector4, VertexElementMethod.Default, VertexElementUsage.Normal, 0),
            };



        static GraphicsDevice _d3d;
        static VertexDeclaration _vd;
        static int _vertexSize;
    }



    /// <summary>
    /// Contains Vertex format used by the Torque engine for terrain chunks.  
    /// </summary>
    public class TerrainVertexFormat
    {
        /// <summary>
        /// Struct which defines the vertex format.  Name is abbreviations for each of its member types 
        /// (a convention borrowed from TSE).
        /// </summary>
        public struct PCTN
        {
            public Vector3 _position;
            public Color _color;
            public Vector2 _texture;
            public Vector4 _normal;



            public PCTN(Vector3 position, Color color, Vector2 texture, Vector4 normal)
            {
                _position = position;
                _color = color;
                _texture = texture;
                _normal = normal;
            }



            public Vector3 Position
            {
                get { return _position; }
                set { _position = value; }
            }



            public Color Color
            {
                get { return _color; }
                set { _color = value; }
            }



            public Vector2 TextureCoordinate
            {
                get { return _texture; }
                set { _texture = value; }
            }



            public Vector4 Normal
            {
                get { return _normal; }
                set { _normal = value; }
            }
        }



        /// <summary>
        /// Returns the size of the PCTN vertex structure.
        /// </summary>
        public static int VertexSize
        {
            get
            {
                // Should be 64!
                if (_vertexSize == 0)
                    _vertexSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(TerrainVertexFormat.PCTN));
                return _vertexSize;
            }
        }



        /// <summary>
        /// Get the vertex declaration for the PCTN format.
        /// </summary>
        /// <param name="d3d"></param>
        /// <returns></returns>
        public static VertexDeclaration GetVertexDeclaration(GraphicsDevice d3d)
        {
            if (d3d != _d3d || _vd == null || _vd.IsDisposed)
            {
                _vd = new VertexDeclaration(d3d, TerrainVertexFormat.PCTNDeclaration);
                _d3d = d3d;
            }
            return _vd;
        }



        /// <summary>
        /// Vertex element array for the PCTN format.
        /// </summary>
        static readonly VertexElement[] PCTNDeclaration =
            {
                new VertexElement(0,  0, VertexElementFormat.Vector3, VertexElementMethod.Default, VertexElementUsage.Position, 0),
                new VertexElement(0, 12, VertexElementFormat.Color, VertexElementMethod.Default, VertexElementUsage.Color, 0),
                new VertexElement(0, 16, VertexElementFormat.Vector2, VertexElementMethod.Default, VertexElementUsage.TextureCoordinate, 0),
                new VertexElement(0, 24, VertexElementFormat.Vector4, VertexElementMethod.Default, VertexElementUsage.Normal, 0),
            };



        static GraphicsDevice _d3d;
        static VertexDeclaration _vd;
        static int _vertexSize;
    }
}
