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
using GarageGames.Torque.Materials;
using GarageGames.Torque.SceneGraph;
using GarageGames.Torque.GFX;
using GarageGames.Torque.RenderManager;
using GarageGames.Torque.Util;



namespace GarageGames.Torque.T2D
{
    public class T2DPolygon : T2DSceneObject, IDisposable
    {
        #region Constructors

        public T2DPolygon()
        {
            // by default, we'll have physics and collision
            CreateWithCollision = true;
            CreateWithPhysics = true;

            GarageGames.Torque.Materials.SimpleMaterial seffect = new GarageGames.Torque.Materials.SimpleMaterial();
            _material = seffect;
        }

        #endregion


        #region Public Properties

        /// <summary>
        /// The list of vertices that will define the polygon in clock-wise order.
        /// </summary>
        public Vector2[] Vertices
        {
            get { return _vertices; }
            set { _vertices = value; }
        }



        /// <summary>
        /// Specify whether the polygon should be collidable. 
        /// This is different from just enabling collisions as the vertices that make up the polygon are passed to the collision image.        
        /// Only works correctly if the polygon is convex.
        /// </summary>
        public bool Collidable
        {
            get { return _collidable; }
            set
            {
                _collidable = value;
                _polyDirty = true;
            }
        }



        /// <summary>
        /// Specifies the color of the polygon.
        /// </summary>
        public Vector4 Color
        {
            get { return _color; }
            set { _color = value; }
        }



        /// <summary>
        /// Define the polygon by a primitive based on the vertex count
        /// For example, 3 would specify an equilateral triangle and 4 would 
        /// specify a square.
        /// </summary>
        public int Primitive
        {
            set
            {
                int vertexCount = value;
                if (vertexCount < 3)
                    vertexCount = 3;
                _vertices = new Vector2[vertexCount];
                if (vertexCount == 4)
                {
                    // define a quad poly basis list
                    _vertices[0] = new Vector2(-1.0f, -1.0f);
                    _vertices[1] = new Vector2(1.0f, -1.0f);
                    _vertices[2] = new Vector2(1.0f, 1.0f);
                    _vertices[3] = new Vector2(-1.0f, 1.0f);
                }
                else
                {
                    float angle = (float)Math.PI / vertexCount;
                    float angleStep = (float)(2.0f * Math.PI) / vertexCount;

                    for (int n = 0; n < vertexCount; n++)
                    {
                        angle += angleStep;
                        _vertices[n] = new Vector2((float)Math.Cos((double)angle), (float)Math.Sin((double)angle));
                    }
                }
                _polyDirty = true;
            }
            internal get { return _vertices.Length; }  // for XML deserialization purposes
        }

        #endregion


        #region Public Methods

        public override void Render(SceneRenderState srs)
        {
            GraphicsDevice d3d = srs.Gfx.Device;
            Assert.Fatal(d3d != null, "doh");

            if (_vertices == null)
                return;

            if (_vb.IsNull)
                _CreateVB();

            if (_refillVB)
                _FillVB();

            if (_polyDirty)
                _UpdateCollisionPoly();

            Vector3 ScaleVector = new Vector3(0.5f * Size.X, 0.5f * Size.Y, 1);

            // scale, translate, rotate
            Matrix ScaleMatrix = Matrix.CreateScale(ScaleVector);
            Matrix TranslationMatrix = Matrix.CreateTranslation(new Vector3(Position.X, Position.Y, LayerDepth));
            Matrix RotationMatrix = Matrix.CreateRotationZ(MathHelper.ToRadians(Rotation));
            Matrix objToWorld = ScaleMatrix * RotationMatrix * TranslationMatrix;

            int numVerts = _vertices.Length + 1;

            srs.World.Push();
            srs.World.MultiplyMatrixLocal(objToWorld);

            RenderInstance ri = SceneRenderer.RenderManager.AllocateInstance();
            ri.Type = RenderInstance.RenderInstanceType.Mesh2D;
            ri.ObjectTransform = srs.World.Top;
            ri.VertexBuffer = _vb.Instance;
            ri.PrimitiveType = PrimitiveType.LineStrip;
            ri.VertexSize = GFXVertexFormat.VertexSize;
            ri.VertexDeclaration = GFXVertexFormat.GetVertexDeclaration(d3d);
            ri.VertexCount = numVerts;
            ri.BaseVertex = 0;
            ri.PrimitiveCount = numVerts - 1;

            ri.Opacity = VisibilityLevel;
            ri.UTextureAddressMode = TextureAddressMode.Clamp;
            ri.VTextureAddressMode = TextureAddressMode.Clamp;

            ri.Material = _material;
            SceneRenderer.RenderManager.AddInstance(ri);

            srs.World.Pop();

            base.Render(srs);
        }



        public override void CopyTo(TorqueObject obj)
        {
            base.CopyTo(obj);

            T2DPolygon obj2 = (T2DPolygon)obj;
            obj2.Vertices = Vertices;
        }

        #endregion


        #region Private, protected, internal methods

        /// <summary>
        /// Sets the default collision vertices to be the same as the vertices of the polygon if
        /// the polygon is specified as being Collidable.
        /// </summary>
        private void _UpdateCollisionPoly()
        {
            if (_vertices == null || _collidable == false)
                return;
            (this.Collision.Images[0] as T2DPolyImage).CollisionPolyBasis = _vertices;
            _polyDirty = false;
        }



        private void _CreateVB()
        {
            if (!_vb.IsNull)
                return;

            Assert.Fatal(_vb.IsNull, "doh");

            int maxVerts = _vertices.Length + 1;
            int sizeInBytes = maxVerts * GFXVertexFormat.VertexSize;
            _vb = ResourceManager.Instance.CreateDynamicVertexBuffer(ResourceProfiles.ManualStaticVBProfile, sizeInBytes);

            _refillVB = true;
        }



        private void _FillVB()
        {
            Assert.Fatal(!_vb.IsNull, "doh");
            Assert.Fatal(_vertices.Length > 0, "doh");

            int numVerts = _vertices.Length + 1;

            // fill in vertex array 
            int sizeInBytes = numVerts * GFXVertexFormat.VertexSize;
            GFXVertexFormat.PCTTBN[] vertices = TorqueUtil.GetScratchArray<GFXVertexFormat.PCTTBN>(numVerts);
            for (int i = 0; i < numVerts - 1; ++i)
            {
                vertices[i] = new GFXVertexFormat.PCTTBN();
                vertices[i].Position = new Vector3(_vertices[i].X, _vertices[i].Y, 0.0f);
                vertices[i].Color = new Color(_color);
            }
            vertices[numVerts - 1] = vertices[0];

            _vb.Instance.SetData<GFXVertexFormat.PCTTBN>(vertices, 0, numVerts);

            _refillVB = false;
        }

        #endregion


        #region Private, protected, internal fields

        private Vector2[] _vertices;
        private bool _collidable;
        private bool _polyDirty;
        private Vector4 _color = new Vector4(0f, 0f, 0f, 1f);
        private Resource<DynamicVertexBuffer> _vb;
        private bool _refillVB;
        private RenderMaterial _material;

        #endregion

        #region IDisposable Members

        public override void Dispose()
        {
            _IsDisposed = true;
            if (!_vb.IsNull)
            {
                _vb.Instance.Dispose();
                _vb.Invalidate();
            }
            base.Dispose();
        }

        #endregion
    }
}
