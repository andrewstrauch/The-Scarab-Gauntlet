//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GarageGames.Torque.Core;
using GarageGames.Torque.GFX;
using GarageGames.Torque.SceneGraph;
using GarageGames.Torque.Materials;
using GarageGames.Torque.RenderManager;
using GarageGames.Torque.MathUtil;



namespace GarageGames.Torque.TS
{
    /// <summary>
    /// Flags for storing properties about a material.
    /// </summary>
    [Flags]
    public enum MaterialFlags
    {
        /// <summary>
        /// Whether or not the texture wraps horizontally.
        /// </summary>
        S_Wrap = 1 << 0, //BIT(0),

        /// <summary>
        /// Whether or not the texture wraps vertically.
        /// </summary>
        T_Wrap = 1 << 1, // BIT(1),

        /// <summary>
        /// Whether or not the material contains any translucency.
        /// </summary>
        Translucent = 1 << 2, // BIT(2),

        /// <summary>
        /// Not used.
        /// </summary>
        IflMaterial = 1 << 27, // BIT(27),
    };



    /// <summary>
    /// Wraps a render material with some information required by the TS system.
    /// </summary>
    public struct Material
    {

        #region Public properties, operators, constants, and enums

        /// <summary>
        /// The name of the material.
        /// </summary>
        public string Name;



        /// <summary>
        /// Flags about what the material contains.
        /// </summary>
        public MaterialFlags Flags;

        #endregion


        #region Private, protected, internal fields

        /// <summary>
        /// The render material associated with this material.
        /// </summary>
        internal RenderMaterial _renderMaterial;

        #endregion
    }



    /// <summary>
    /// Helper class for setting up render instances. This is mostly useful for rendering meshes that contain
    /// the same vertex information, but different material information. Usage:
    /// 
    /// Call DrawPrimitive.Set(), passing it all the vertex information, to set up the render instance.
    /// Then, call DrawPrimitive.SetMaterial(), followed by DrawPrimitve.Render as many times as necessary.
    /// Finally, call DrawPrimitive.Cleanup().
    /// 
    /// This is also used to store primitive information for ts shapes.
    /// </summary>
    public struct DrawPrimitive
    {

        #region Static methods, fields, constructors

        /// <summary>
        /// Sets up the draw primitive for a specified scene render state and vertex information.
        /// </summary>
        /// <param name="srs">The scene render state.</param>
        /// <param name="_vb">The vertex buffer to draw with.</param>
        /// <param name="_ib">The index buffer to draw with.</param>
        /// <param name="numVerts">The number of vertices in the vertex buffer.</param>
        /// <param name="startOffset">The vertex index to start drawing at.</param>
        /// <param name="vertSize">The size</param>
        /// <param name="decl">The vertex declaration.</param>
        public static void Set(VertexBuffer vb, IndexBuffer ib, int numVerts, int startOffset, int vertSize, VertexDeclaration decl)
        {
            Assert.Fatal(vb != null, "DrawPrimitive.Set - Invalid vertex buffer!");
            Assert.Fatal(ib != null, "DrawPrimitive.Set - Invalid index buffer!");

            Assert.Fatal(_curRenderInst == null, "DrawPrimitive.Set - Current render instance is not null, a cleanup call was missed.");

            _curRenderInst = SceneRenderer.RenderManager.AllocateInstance();

            _curRenderInst.VertexSize = vertSize;
            _curRenderInst.VertexDeclaration = decl;

            _curRenderInst.VertexBuffer = vb;
            _curRenderInst.IndexBuffer = ib;

            _curRenderInst.VertexCount = numVerts;
            _curRenderInst.BaseVertex = startOffset;
        }



        /// <summary>
        /// Clears the draw primitive so it can be used with a new set of vertex data.
        /// </summary>
        public static void Clear()
        {
            _matIndex = 0xFFFFFFFF;
            SceneRenderer.RenderManager.FreeInstance(_curRenderInst);
            _curRenderInst = null;
        }



        /// <summary>
        /// Sets the material information for the draw primitive. This will do nothing if the same material
        /// index was used with the previous call.
        /// </summary>
        /// <param name="matIndex">The index of the material to use.</param>
        /// <param name="matList">The material list to look up the material in.</param>
        /// <param name="bounds">The bounding box of the mesh being rendered.</param>
        /// <param name="srs">The scene render state.</param>
        public static void SetMaterial(uint matIndex, Material[] matList, ref Box3F bounds, SceneRenderState srs)
        {
            if (matIndex == _matIndex)
                return;

            _matIndex = matIndex;

            SetMaterial(ref matList[matIndex], ref bounds, srs);
        }



        /// <summary>
        /// Sets the material information for the draw primitive.
        /// </summary>
        /// <param name="material">The material to use.</param>
        /// <param name="bounds">The bounding box of the mesh being rendered.</param>
        /// <param name="srs">The scene render state.</param>
        public static void SetMaterial(ref Material material, ref Box3F bounds, SceneRenderState srs)
        {
            // perform name mapping to replace materials with custom materials
            if (material.Name != null && material._renderMaterial == null)
            {
                String baseName = Shape.CurrentFilePath + @"\" + material.Name;
                material._renderMaterial = MaterialManager.Lookup(baseName);
                if (material._renderMaterial == null)
                {
                    // if no material mapped, then use a default material         
                    if (baseName != null)
                    {
                        LightingMaterial effect = new LightingMaterial();
                        effect.LightingMode = LightingMode.PerPixel;
                        effect.TextureFilename = baseName;
                        effect.IsTranslucent = (material.Flags & MaterialFlags.Translucent) != 0;

                        material._renderMaterial = MaterialManager.Add(baseName, effect);
                    }

                    // if we didn't find it this time, don't look it up again
                    if (material._renderMaterial == null)
                        material.Name = null;
                }
            }

            // material info
            _curRenderInst.Material = material._renderMaterial;

            bool translucent = (material.Flags & MaterialFlags.Translucent) != 0;
            _curRenderInst.Type = translucent ? RenderInstance.RenderInstanceType.Translucent2D : RenderInstance.RenderInstanceType.Mesh3D;

            bool sWrap = (material.Flags & MaterialFlags.S_Wrap) != 0;
            _curRenderInst.UTextureAddressMode = sWrap ? TextureAddressMode.Wrap : TextureAddressMode.Clamp;

            bool tWrap = (material.Flags & MaterialFlags.T_Wrap) != 0;
            _curRenderInst.VTextureAddressMode = tWrap ? TextureAddressMode.Wrap : TextureAddressMode.Clamp;

            // transform info
            Matrix transform = srs.World.Top;
            _curRenderInst.WorldBox = Box3F.Transform(ref bounds, ref transform);
            _curRenderInst.ObjectTransform = transform;
        }



        /// <summary>
        /// Finalizes the render instance and submits it to the render manager.
        /// </summary>
        /// <param name="drawType">The primitive type to render as.</param>
        /// <param name="startIndex">The index in the index buffer to start at.</param>
        /// <param name="numElements">The number of elements to draw.</param>
        public static void Render(uint drawType, int startIndex, int numElements)
        {
            Assert.Fatal(drawType == DrawPrimitive.Strip || drawType == DrawPrimitive.Triangles, "DrawPrimitive.Render - DrawType must be Strip or Triangles.");

            PrimitiveType prim;

            if (drawType == DrawPrimitive.Strip)
            {
                prim = PrimitiveType.TriangleStrip;
                numElements -= 2; // convert strip length to triangle count
            }
            else
            {
                prim = PrimitiveType.TriangleList;
                numElements /= 3; // convert from vertex count to triangle count
            }

            _curRenderInst.PrimitiveType = prim;
            _curRenderInst.PrimitiveCount = numElements;
            _curRenderInst.StartIndex = startIndex;

            SceneRenderer.RenderManager.AddInstance(_curRenderInst);

            // Now, copy this render instance to support multiple renderings off the same shape
            RenderInstance ri = SceneRenderer.RenderManager.AllocateInstance();
            ri.Copy(_curRenderInst);
            _curRenderInst = ri;
        }



        /// <summary>
        /// Triangle list bit.
        /// </summary>
        public const uint Triangles = 0 << 30;



        /// <summary>
        /// Triangle strip bit.
        /// </summary>
        public const uint Strip = 1 << 30;



        /// <summary>
        /// Indexed. This is always the case.
        /// </summary>
        public const uint Indexed = 1 << 29;



        /// <summary>
        /// No material set on the primitive.
        /// </summary>
        public const uint NoMaterial = 1 << 28;



        /// <summary>
        /// Mask for reading the primitive type information.
        /// </summary>
        public const uint TypeMask = Strip | 0x80000000 | Triangles; // 0x8000000 was "fan" but is deprecated



        /// <summary>
        /// Mask for stripping out the primitive information.
        /// </summary>
        public const uint MaterialMask = ~(TypeMask | Indexed | NoMaterial);



        static uint _matIndex = 0xFFFFFFFF;
        static RenderInstance _curRenderInst;

        #endregion


        #region Private, protected, internal fields

        /// <summary>
        /// The index in the index buffer to start rendering at.
        /// </summary>
        public ushort Start;



        /// <summary>
        /// The number of elements to render for this primitive.
        /// </summary>
        public ushort NumElements;



        /// <summary>
        /// The index of the material that this primitive uses.
        /// </summary>
        public uint MaterialIndex;

        #endregion
    }
}
