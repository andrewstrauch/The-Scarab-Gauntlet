//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GarageGames.Torque.Core;
using GarageGames.Torque.MathUtil;
using GarageGames.Torque.GFX;
using GarageGames.Torque.Materials;
using GarageGames.Torque.SceneGraph;
using GarageGames.Torque.Util;



namespace GarageGames.Torque.RenderManager
{
    /// <summary>
    /// A render instance stores all the information necessary to render an object. It is
    /// completely detached from the object itself. This allows the scene renderer and
    /// specific render managers to perform various rendering optimizations like batching
    /// and setting common render states.
    /// </summary>
    public class RenderInstance : ObjectPooler.IResetable
    {

        #region Public properties, operators, constants, and enums

        /// <summary>
        /// Enumeration for different types of render instances. The type determines which
        /// render manager will handle rendering of the instance.
        /// </summary>
        public enum RenderInstanceType
        {
            Sky = 0,
            Mesh2D,
            Terrain,
            Mesh3D,
            Avatar,
            Shadow,
            Translucent2D,
            Translucent3D,
            Billboard,
            Refraction,
            UndefinedType
        }



        /// <summary>
        /// The vertex buffer to render with.
        /// </summary>
        public VertexBuffer VertexBuffer;



        /// <summary>
        /// The index buffer to render with.
        /// </summary>
        public IndexBuffer IndexBuffer;



        /// <summary>
        /// The type of vertex data in the vertex buffer.
        /// </summary>
        public VertexDeclaration VertexDeclaration;



        /// <summary>
        /// The size in bytes of a single vertex in the vertex buffer.
        /// </summary>
        public int VertexSize;



        /// <summary>
        /// The type of primitive to use when rendering this instance.
        /// </summary>
        public PrimitiveType PrimitiveType = PrimitiveType.TriangleStrip;



        /// <summary>
        /// The vertex to start at in the vertex buffer.
        /// </summary>
        public int BaseVertex;



        /// <summary>
        /// The number of vertices to use from the vertex buffer.
        /// </summary>
        public int VertexCount;



        /// <summary>
        /// The index to start at in the index buffer.
        /// </summary>
        public int StartIndex;



        /// <summary>
        /// The number of primitives to render.
        /// </summary>
        public int PrimitiveCount;



        /// <summary>
        /// The material to use when rendering this instance.
        /// </summary>
        public RenderMaterial Material;



        /// <summary>
        /// Optional additional data attached to the material that some render managers may make use of.
        /// </summary>
        public MaterialInstanceData MaterialInstanceData;



        /// <summary>
        /// The visibility level of the object.
        /// </summary>
        public float Opacity = 1.0f;



        /// <summary>
        /// The horizontal texture addressing mode.
        /// </summary>
        public TextureAddressMode UTextureAddressMode = TextureAddressMode.Clamp;



        /// <summary>
        /// The vertical texture addressing mode.
        /// </summary>
        public TextureAddressMode VTextureAddressMode = TextureAddressMode.Clamp;



        /// <summary>
        /// The absolute transform of the object.
        /// </summary>
        public Matrix ObjectTransform = Matrix.Identity;



        /// <summary>
        /// The bounding box in world space of the object.
        /// </summary>
        public Box3F WorldBox;



        /// <summary>
        /// The type of render instance. This determines which render manager will render it.
        /// </summary>
        public RenderInstanceType Type = RenderInstanceType.UndefinedType;



        /// <summary>
        /// The sort point for the render instance. Some render managers (Translucent) uses this
        /// to order the render instances before rendering.
        /// </summary>
        public Vector3 SortPoint = Vector3.Zero;



        /// <summary>
        /// By default the sort point is set to the translation of the object. If this is set, it
        /// will not be set, which allows the sort point to be set when the instance is actually
        /// created.
        /// </summary>
        public bool IsSortPointSet = false;



        internal int MaterialSortKey;
        internal int GeometrySortKey;
        internal bool IsReset = true;

        #endregion


        #region Public methods

        /// <summary>
        /// Copies a render instance's fields into this render instance.
        /// </summary>
        /// <param name="src">The render instance to copy.</param>
        public void Copy(RenderInstance src)
        {
            VertexBuffer = src.VertexBuffer;
            IndexBuffer = src.IndexBuffer;
            VertexDeclaration = src.VertexDeclaration;
            Material = src.Material;
            ObjectTransform = src.ObjectTransform;
            WorldBox = src.WorldBox;
            UTextureAddressMode = src.UTextureAddressMode;
            VTextureAddressMode = src.VTextureAddressMode;
            VertexSize = src.VertexSize;
            PrimitiveType = src.PrimitiveType;
            BaseVertex = src.BaseVertex;
            VertexCount = src.VertexCount;
            StartIndex = src.StartIndex;
            PrimitiveCount = src.PrimitiveCount;
            Type = src.Type;
            SortPoint = src.SortPoint;
            IsSortPointSet = src.IsSortPointSet;
            Opacity = src.Opacity;
        }



        /// <summary>
        /// Resets the render instance's fields to defaults.
        /// </summary>
        public virtual void Reset()
        {
            VertexBuffer = null;
            IndexBuffer = null;
            VertexDeclaration = null;
            Material = null;
            MaterialInstanceData = null;
            Opacity = 1.0f;
            UTextureAddressMode = TextureAddressMode.Clamp;
            VTextureAddressMode = TextureAddressMode.Clamp;
            ObjectTransform = Matrix.Identity;
            SortPoint = Vector3.Zero;
            IsSortPointSet = false;
            VertexSize = 0;
            PrimitiveType = PrimitiveType.TriangleStrip;
            BaseVertex = 0;
            VertexCount = 0;
            StartIndex = 0;
            PrimitiveCount = 0;
            Type = RenderInstanceType.UndefinedType;
            MaterialSortKey = 0;
            GeometrySortKey = 0;
            IsReset = true;
        }

        #endregion
    }
}
