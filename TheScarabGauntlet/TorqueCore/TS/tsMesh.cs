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
using GarageGames.Torque.SceneGraph;
using GarageGames.Torque.Util;



namespace GarageGames.Torque.TS
{
    /// <summary>
    /// A renderable object attached to a TS shape.
    /// </summary>
    public class Mesh : IDisposable
    {

        #region Constructors

        public Mesh() : this(MeshEnum.StandardMeshType) { }

        public Mesh(MeshEnum type)
        {
            _meshType = type;
            _parentMesh = -1;
        }

        #endregion


        #region Public properties, operators, constants, and enums

        /// <summary>
        /// Various flags about the mesh, like the type of mesh and features about the mesh.
        /// </summary>
        [Flags]
        public enum MeshEnum
        {
            // mesh types
            StandardMeshType = 0,
            SkinMeshType = 1,
            DecalMeshType = 2,
            SortedMeshType = 3,
            NullMeshType = 4,
            TypeMask = StandardMeshType | SkinMeshType | DecalMeshType | SortedMeshType | NullMeshType,

            // flags
            Billboard = 1 << 31, // not implemented
            HasDetailTexture = 1 << 30, // deprecated
            BillboardZAxis = 1 << 29, // not implemented
            UseEncodedNormals = 1 << 28, // deprecated
            FlagMask = Billboard | BillboardZAxis | HasDetailTexture | UseEncodedNormals
        };



        /// <summary>
        /// The bounding box of the mesh.
        /// </summary>
        public Box3F Bounds
        {
            get { return _bounds; }
        }



        /// <summary>
        /// The center point of the mesh.
        /// </summary>
        public Vector3 Center
        {
            get { return _center; }
        }



        /// <summary>
        /// The bounding radius of the mesh.
        /// </summary>
        public float Radius
        {
            get { return _radius; }
        }



        /// <summary>
        /// The type of mesh.
        /// </summary>
        public MeshEnum MeshType
        {
            get { return _meshType & MeshEnum.TypeMask; }
        }

        #endregion


        #region Public methods

        /// <summary>
        /// Renders the mesh at the specified frame with the specified material.
        /// </summary>
        /// <param name="frame">The frame to render at.</param>
        /// <param name="matFrame">The material frame to render at.</param>
        /// <param name="materialList">The list of materials to use.</param>
        /// <param name="srs">The scene render state.</param>
        public virtual void Render(int frame, int matFrame, Material[] materialList, SceneRenderState srs)
        {
            if (_vertsPerFrame <= 0)
                return;

            // do need to recreate the vb and ib?
            if (_vb.IsNull)
                CreateVBIB();

            // compute offset into vertex buffer.  Assume we either Animate _verts or _tverts but not both.
            int startOffset = Math.Max(frame, matFrame) * _vertsPerFrame + _vertOffset;

            bool _useLighting = true;

            if (_useLighting)
                DrawPrimitive.Set(_vb.Instance, _ib.Instance, _vertsPerFrame, startOffset, GFXVertexFormat.VertexSize, GFXVertexFormat.GetVertexDeclaration(srs.Gfx.Device));
            else
                DrawPrimitive.Set(_vb.Instance, _ib.Instance, _vertsPerFrame, startOffset, VertexPositionColorTexture.SizeInBytes, srs.Gfx.GetVertexDeclarationVPCT());

            for (uint i = 0; i < _primitives.Length; i++)
            {
                DrawPrimitive.SetMaterial(_primitives[i].MaterialIndex & DrawPrimitive.MaterialMask, materialList, ref _bounds, srs);
                DrawPrimitive.Render(_primitives[i].MaterialIndex & DrawPrimitive.TypeMask, _primitives[i].Start + _indexOffset, _primitives[i].NumElements);
            }

            DrawPrimitive.Clear();
        }



        /// <summary>
        /// Creates the vertex and index buffers for the mesh.
        /// </summary>
        virtual public void CreateVBIB()
        {
            if (_vb.IsNull)
            {
                bool _useLighting = true;
                if (_useLighting)
                {
                    int sizeInBytes = _verts.Length * GFXVertexFormat.VertexSize;
                    _vb = ResourceManager.Instance.CreateDynamicVertexBuffer(ResourceProfiles.ManualStaticVBProfile, sizeInBytes);
                    GFXVertexFormat.PCTTBN[] scratch = TorqueUtil.GetScratchArray<GFXVertexFormat.PCTTBN>(_verts.Length);
                    CopyVB(scratch, 0, Matrix.Identity);
                    _vb.Instance.SetData<GFXVertexFormat.PCTTBN>(scratch, 0, _verts.Length);
                }
                else
                {
                    int sizeInBytes = _verts.Length * VertexPositionColorTexture.SizeInBytes;
                    _vb = ResourceManager.Instance.CreateDynamicVertexBuffer(ResourceProfiles.ManualStaticVBProfile, sizeInBytes);
                    VertexPositionColorTexture[] scratch = TorqueUtil.GetScratchArray<VertexPositionColorTexture>(_verts.Length);
                    CopyVB(scratch, 0, Matrix.Identity);
                    _vb.Instance.SetData<VertexPositionColorTexture>(scratch, 0, _verts.Length);
                }
            }

            // create 16-bit index buffer
            if (_ib.IsNull)
            {
                int sizeInBytes = _indices.Length * sizeof(short);
                _ib = ResourceManager.Instance.CreateDynamicIndexBuffer(ResourceProfiles.ManualStaticIBProfile, sizeInBytes, IndexElementSize.SixteenBits);
                short[] scratch = TorqueUtil.GetScratchArray<short>(_indices.Length);
                CopyIB(scratch, 0);
                _ib.Instance.SetData<short>(scratch, 0, _indices.Length);
            }
            _vb.Instance.ContentLost += new EventHandler(_vbInstance_ContentLost);
            _ib.Instance.ContentLost += new EventHandler(_ibInstance_ContentLost);
        }

        void _ibInstance_ContentLost(object sender, EventArgs e)
        {
            _ib.Instance.ContentLost -= new EventHandler(_ibInstance_ContentLost);
            _ib.Invalidate();
        }

        void _vbInstance_ContentLost(object sender, EventArgs e)
        {
            _vb.Instance.ContentLost -= new EventHandler(_vbInstance_ContentLost);
            _vb.Invalidate();
        }


        /// <summary>
        /// Copy the vertex data into an array.
        /// </summary>
        /// <param name="vb">The vertex array receiving the vertex data.</param>
        /// <param name="vbStart">The start index.</param>
        /// <param name="mat">The transform matrix to rotate vertex data with.</param>
        public void CopyVB(GFXVertexFormat.PCTTBN[] vb, int vbStart, Matrix mat)
        {
            _ComputeTangents();

            for (int i = 0, idx = vbStart; i < _verts.Length; ++i)
            {
                Vector3 tang = new Vector3(_tangents[i].X, _tangents[i].Y, _tangents[i].Z);
                Vector3 binormal = Vector3.Cross(_norms[i], tang) * _tangents[i].W;
                tang = Vector3.TransformNormal(tang, mat);
                binormal = Vector3.TransformNormal(binormal, mat);
                Vector3 vert = Vector3.Transform(_verts[i], mat);
                Vector3 norm = Vector3.TransformNormal(_norms[i], mat);

                vb[idx++] = new GFXVertexFormat.PCTTBN(
                    new Vector3(vert.X, vert.Y, vert.Z),
                    (_colors != null) ? _colors[i] : Color.White,
                    _tverts[i],
                    (_tverts2 != null) ? _tverts2[i] : _tverts[i],
                    new Vector4(tang.X, tang.Y, tang.Z, _tangents[i].W),
                    new Vector4(norm.X, norm.Y, norm.Z, 0.0f));
            }
        }



        /// <summary>
        /// Copy the vertex data into an array.
        /// </summary>
        /// <param name="vb">The vertex array receiving the vertex data.</param>
        /// <param name="vbStart">The start index.</param>
        /// <param name="mat">The transform matrix to rotate vertex data with.</param>
        public void CopyVB(VertexPositionColorTexture[] vb, int vbStart, Matrix mat)
        {
            for (int i = 0, idx = vbStart; i < _verts.Length; ++i)
            {
                Vector3 vert = Vector3.Transform(_verts[i], mat);
                vb[idx++] = new VertexPositionColorTexture(vert, Color.White, _tverts[i]);
            }
        }



        /// <summary>
        /// Copy the index data into an array.
        /// </summary>
        /// <param name="ib">The array receiving the index data.</param>
        /// <param name="ibStart">The start index.</param>
        public void CopyIB(short[] ib, int ibStart)
        {
            System.Array.Copy(_indices, 0, ib, ibStart, _indices.Length);
        }



        /// <summary>
        /// Counts the number of vertices and indices in the mesh.
        /// </summary>
        /// <param name="numVerts">Receives the number of vertices.</param>
        /// <param name="numIndices">Receives the number of indices.</param>
        public virtual void TakeInventory(ref int numVerts, ref int numIndices)
        {
            if (_parentMesh >= 0)
                // parent holds our verts and indices
                return;

            numVerts += _verts.Length;
            numIndices += _indices.Length;
        }



        /// <summary>
        /// Gets the number of polygons in the mesh.
        /// </summary>
        /// <returns>The number of polygons.</returns>
        public virtual int GetNumPolys()
        {
            int count = 0;
            for (int i = 0; i < _primitives.Length; i++)
            {
                if ((_primitives[i].MaterialIndex & DrawPrimitive.TypeMask) == DrawPrimitive.Triangles)
                    count += _primitives[i].NumElements / 3;
                else
                    count += _primitives[i].NumElements - 2;
            }

            return count;
        }



        /// <summary>
        /// Sets flags on the mesh.
        /// </summary>
        /// <param name="flag">The flags to set.</param>
        public void SetFlags(MeshEnum flag)
        {
            _meshType |= flag;
        }



        /// <summary>
        /// Gets whether or not a flag is set on the mesh.
        /// </summary>
        /// <param name="flag">The flags to test.</param>
        /// <returns>True if the one of the flags is set.</returns>
        public MeshEnum GetFlags(MeshEnum flag)
        {
            return _meshType & flag;
        }

        #endregion


        #region Private, protected, internal methods

        void _FindTangent(short index1, short index2, short index3, Vector3[] tan0, Vector3[] tan1)
        {
            Vector3 v1 = _verts[index1];
            Vector3 v2 = _verts[index2];
            Vector3 v3 = _verts[index3];

            Vector2 w1 = _tverts[index1];
            Vector2 w2 = _tverts[index2];
            Vector2 w3 = _tverts[index3];

            float x1 = v2.X - v1.X;
            float x2 = v3.X - v1.X;
            float y1 = v2.Y - v1.Y;
            float y2 = v3.Y - v1.Y;
            float z1 = v2.Z - v1.Z;
            float z2 = v3.Z - v1.Z;

            float s1 = w2.X - w1.X;
            float s2 = w3.X - w1.X;
            float t1 = w2.Y - w1.Y;
            float t2 = w3.Y - w1.Y;

            float denom = (s1 * t2 - s2 * t1);

            // handle degenerate triangles from strips
            if (Math.Abs(denom) < 0.0001)
                return;

            float r = 1.0f / denom;

            Vector3 sdir = new Vector3((t2 * x1 - t1 * x2) * r, (t2 * y1 - t1 * y2) * r, (t2 * z1 - t1 * z2) * r);
            Vector3 tdir = new Vector3((s1 * x2 - s2 * x1) * r, (s1 * y2 - s2 * y1) * r, (s1 * z2 - s2 * z1) * r);

            tan0[index1] += sdir;
            tan1[index1] += tdir;

            tan0[index2] += sdir;
            tan1[index2] += tdir;

            tan0[index3] += sdir;
            tan1[index3] += tdir;
        }



        void _ComputeTangents()
        {
            if (_tangents == null || _tangents.Length != _verts.Length)
            {
                _tangents = new Vector4[_verts.Length];

                Vector3[] tan0 = new Vector3[_verts.Length];
                Vector3[] tan1 = new Vector3[_verts.Length];

                for (int i = 0; i < _primitives.Length; i++)
                {
                    DrawPrimitive draw = _primitives[i];
                    short start = (short)draw.Start;
                    short p1Index = 0;
                    short p2Index = 0;
                    if ((draw.MaterialIndex & DrawPrimitive.TypeMask) == DrawPrimitive.Triangles)
                    {
                        // triangle list
                        for (uint j = 0; j < draw.NumElements; j += 3)
                        {
                            _FindTangent(_indices[start + j], _indices[start + j + 1], _indices[start + j + 2], tan0, tan1);
                        }
                    }
                    else
                    {
                        // triangle strip
                        p1Index = _indices[start + 0];
                        p2Index = _indices[start + 1];
                        for (uint j = 2; j < draw.NumElements; j++)
                        {
                            _FindTangent(p1Index, p2Index, _indices[start + j], tan0, tan1);
                            p1Index = p2Index;
                            p2Index = _indices[start + j];
                        }
                    }
                }

                // fill out final info from accumulated basis data
                for (uint i = 0; i < _verts.Length; i++)
                {
                    Vector3 n = _norms[i];
                    Vector3 t = tan0[i];
                    Vector3 b = tan1[i];

                    t -= Vector3.Dot(n, t) * n;
                    t.Normalize();

                    Vector3 cp = Vector3.Cross(n, t);
                    float w = Vector3.Dot(cp, b) < 0.0f ? -1.0f : 1.0f;
                    _tangents[i] = new Vector4(t.X, t.Y, t.Z, w);
                }
            }
        }

        #endregion


        #region Private, protected, internal fields

        protected MeshEnum _meshType;

        public DrawPrimitive[] _primitives;
        public Vector3[] _verts;
        public Vector3[] _norms;
        public Vector4[] _tangents;
        public Vector2[] _tverts;
        public Vector2[] _tverts2;
        public Color[] _colors;
        public short[] _indices;

        public int _parentMesh;
        public int _numFrames;
        public int _numMatFrames;
        public int _vertsPerFrame;

        public Resource<DynamicVertexBuffer> _vb;
        public Resource<DynamicIndexBuffer> _ib;

        public Box3F _bounds;
        public Vector3 _center;
        public float _radius;

        public int _vertOffset;
        public int _indexOffset;

        #endregion

        #region IDisposable Members

        public virtual void Dispose()
        {
            if (!_vb.IsNull)
            {
                _vb.Instance.ContentLost -= new EventHandler(_vbInstance_ContentLost);
                _vb.Instance.Dispose();
                _vb.Invalidate();
            }
            if (!_ib.IsNull)
            {
                _ib.Instance.ContentLost -= new EventHandler(_ibInstance_ContentLost);
                _ib.Instance.Dispose();
                _ib.Invalidate();
            }
        }

        #endregion
    }
}
