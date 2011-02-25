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
using GarageGames.Torque.GFX;
using GarageGames.Torque.Materials;
using GarageGames.Torque.MathUtil;
using GarageGames.Torque.SceneGraph;
using GarageGames.Torque.Sim;
using GarageGames.Torque.RenderManager;
using GarageGames.Torque.Util;



namespace GarageGames.Torque.RenderManager
{
    public class RenderQuad : IDisposable
    {

        #region Constructors

        public RenderQuad()
        {
        }

        #endregion


        #region Public properties, operators, constants, and enums

        public RenderMaterial Material
        {
            set { _material = value; }
            get { return _material; }
        }



        public TextureAddressMode TexAddressU
        {
            set { _texU = value; }
            get { return _texU; }
        }



        public TextureAddressMode TexAddressV
        {
            set { _texV = value; }
            get { return _texV; }
        }



        public void SetupUVs(RectangleF uvrect, bool flipX, bool flipY)
        {
            float minX, minY, maxX, maxY;
            minX = uvrect.X;
            minY = uvrect.Y;
            maxX = minX + uvrect.Width;
            maxY = minY + uvrect.Height;

            if (flipX)
                TorqueUtil.Swap<float>(ref minX, ref maxX);
            if (flipY)
                TorqueUtil.Swap<float>(ref minY, ref maxY);

            if (minX != _uvMinX || minY != _uvMinY || maxX != _uvMaxX || maxY != _uvMaxY)
            {
                _uvMinX = minX;
                _uvMinY = minY;
                _uvMaxX = maxX;
                _uvMaxY = maxY;
                _refillVB = true;
            }
        }



        public void SetupUVs(float offsetX, float offsetY, float repeatX, float repeatY, bool flipX, bool flipY)
        {
            float minX, minY, maxX, maxY;
            minX = offsetX;
            minY = offsetY;
            maxX = minX + repeatX;
            maxY = minY + repeatY;

            if (flipX)
                TorqueUtil.Swap<float>(ref minX, ref maxX);
            if (flipY)
                TorqueUtil.Swap<float>(ref minY, ref maxY);

            if (minX != _uvMinX || minY != _uvMinY || maxX != _uvMaxX || maxY != _uvMaxY)
            {
                _uvMinX = minX;
                _uvMinY = minY;
                _uvMaxX = maxX;
                _uvMaxY = maxY;
                _refillVB = true;
            }
        }


        #endregion


        #region Public methods

        // populate the object.
        public void CreateAndFillVB()
        {
            if (_vb == null)
                _CreateVB();

            Assert.Fatal((_vb != null), "Invalid buffer.");

            if (_refillVB || !_vb.AreBufferContentsValid())
                _FillVB();
        }



        public void FillRenderInstance(RenderInstance ri, float opacity, SceneRenderState srs)
        {
            GraphicsDevice d3d = srs.Gfx.Device;
            Assert.Fatal(d3d != null, "doh");

            ri.Type = RenderInstance.RenderInstanceType.Mesh2D;

            ri.ObjectTransform = srs.World.Top;

            ri.PrimitiveType = PrimitiveType.TriangleFan;
            ri.VertexDeclaration = GFXVertexFormat.GetVertexDeclaration(srs.Gfx.Device);

            ri.VertexBuffer = _vb.Buffer as VertexBuffer;
            ri.BaseVertex = _vb.StartIndex;
            ri.VertexCount = _vb.Count;
            ri.VertexSize = _vb.ElementSize;

            ri.PrimitiveCount = 2;


            ri.Opacity = opacity;
            ri.UTextureAddressMode = _texU;
            ri.VTextureAddressMode = _texV;

            ri.Material = _material;
        }



        // render the object.
        public void Render(Matrix objToWorld, float opacity, SceneRenderState srs)
        {
            CreateAndFillVB();

            Assert.Fatal(_vb != null, "doh");
            Assert.Fatal(!_refillVB, "doh, vb needs to be refilled");

            srs.World.Push();
            srs.World.MultiplyMatrixLocal(objToWorld);

            RenderInstance ri = SceneRenderer.RenderManager.AllocateInstance();

            FillRenderInstance(ri, opacity, srs);

            SceneRenderer.RenderManager.AddInstance(ri);
            srs.World.Pop();
        }



        public virtual void Dispose()
        {
            if (_vb != null)
            {
                _vb.Dispose();
                _vb = null;
            }
        }

        #endregion


        #region Private, protected, internal methods


        private void _CreateVB()
        {
            if (_vb != null)
                return;

            _vb = new GFXDynamicVertexBufferPCTTBN(_numVerts);

            _refillVB = true;
        }



        private void _FillVB()
        {
            Assert.Fatal(_vb != null, "doh");

            // Calculate our uv points.
            _uvs[0] = new Vector2(_uvMinX, _uvMinY);
            _uvs[1] = new Vector2(_uvMaxX, _uvMinY);
            _uvs[2] = new Vector2(_uvMaxX, _uvMaxY);
            _uvs[3] = new Vector2(_uvMinX, _uvMaxY);

            // Fill in vertex array.

            int sizeInBytes = _numVerts * GFXVertexFormat.VertexSize;
            GFXVertexFormat.PCTTBN[] vertices = _vb.GetScratchArray(_numVerts);

            for (int i = 0; i < _numVerts; ++i)
            {
                vertices[i] = new GFXVertexFormat.PCTTBN(
                    _points[i],
                    _color,
                    _uvs[i],
                    _uvs[i],
                    _tangent,
                    _normal);
            }

            // Pass array to the real buffer.
            _vb.SetData(vertices, 0, _numVerts);

            _refillVB = false;
        }

        #endregion


        #region Private, protected, internal fields

        private IGFXBuffer<GFXVertexFormat.PCTTBN> _vb = null;
        private RenderMaterial _material = null;
        private TextureAddressMode _texU = TextureAddressMode.Clamp;
        private TextureAddressMode _texV = TextureAddressMode.Clamp;
        private float _uvMinX = 0.0f;
        private float _uvMinY = 0.0f;
        private float _uvMaxX = 1.0f;
        private float _uvMaxY = 1.0f;
        private bool _refillVB = false;
        private const int _numVerts = 4;
        private Vector2[] _uvs = { new Vector2(0.0f, 0.0f),
                                   new Vector2(1.0f, 0.0f),
                                   new Vector2(1.0f, 1.0f),
                                   new Vector2(0.0f, 1.0f) };

        private static readonly Vector3[] _points = { new Vector3(-1.0f, -1.0f, 0.0f),
                                                      new Vector3(1.0f, -1.0f, 0.0f),
                                                      new Vector3(1.0f, 1.0f, 0.0f),
                                                      new Vector3(-1.0f, 1.0f, 0.0f) };

        private static readonly Vector4 _tangent = new Vector4(1.0f, 0.0f, 0.0f, 0.0f);

        private static readonly Vector4 _normal = new Vector4(0.0f, 0.0f, 1.0f, 0.0f);

        private static readonly Color _color = Color.White;

        #endregion
    }
}
