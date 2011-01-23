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
using GarageGames.Torque.SceneGraph;
using GarageGames.Torque.Util;
using GarageGames.Torque.MathUtil;



namespace GarageGames.Torque.TS
{
    /// <summary>
    /// A mesh type that is used for skinning.
    /// </summary>
    public class SkinMesh : Mesh, IDisposable
    {
        #region Static methods, fields, constructors

        // work variables
        static Vector3[] _workSkinVerts;
        static Vector3[] _workSkinNormals;
        static Vector4[] _workSkinTangents;
        static Matrix[] _workBoneTransforms;

        #endregion


        #region Constructors

        public SkinMesh() : base(MeshEnum.SkinMeshType) { }

        #endregion


        #region Public methods

        public override void CreateVBIB()
        {
            _verts = _initialVerts;
            _norms = _initialNormals;

            // Create our vb here so we can use our dynamic profile.
            if (_vb.IsNull)
            {
                int sizeInBytes = _verts.Length * GFXVertexFormat.VertexSize;
                _vb = ResourceManager.Instance.CreateDynamicVertexBuffer(SkinMeshVertexBufferProfile, sizeInBytes);
                GFXVertexFormat.PCTTBN[] scratch = TorqueUtil.GetScratchArray<GFXVertexFormat.PCTTBN>(_verts.Length);
                CopyVB(scratch, 0, Matrix.Identity);
                _vb.Instance.SetData<GFXVertexFormat.PCTTBN>(scratch, 0, _verts.Length);
            }

            base.CreateVBIB();
            _initialTangents = _tangents;
        }



        public override void Render(int frame, int matFrame, Material[] materialList, SceneRenderState srs)
        {
            // update _verts and normals...
            UpdateSkin();

            if (_vb.IsNull)
                base.CreateVBIB();

            if (!_vb.IsNull)
            {
                int sizeInBytes = _initialVerts.Length * GFXVertexFormat.VertexSize;
                GFXVertexFormat.PCTTBN[] v = TorqueUtil.GetScratchArray<GFXVertexFormat.PCTTBN>(_initialVerts.Length);

                for (int i = 0; i < _initialVerts.Length; ++i)
                {
                    Vector3 tang = new Vector3(_tangents[i].X, _tangents[i].Y, _tangents[i].Z);
                    Vector3 binormal = Vector3.Cross(_norms[i], tang) * _tangents[i].W;
                    v[i] = new GFXVertexFormat.PCTTBN(
                        new Vector3(_verts[i].X, _verts[i].Y, _verts[i].Z),
                        Color.White,
                        _tverts[i],
                        _tverts[i],
                        _tangents[i],
                        new Vector4(_norms[i].X, _norms[i].Y, _norms[i].Z, 0.0f));
                }

                GFXDevice.Instance.Device.Textures[0] = null;

                _vb.Instance.SetData<GFXVertexFormat.PCTTBN>(v, 0, _initialVerts.Length, SetDataOptions.NoOverwrite);
            }

            base.Render(frame, matFrame, materialList, srs);

            RestoreSkin();
        }

        #endregion


        #region Private, protected, internal methods

        void UpdateSkin()
        {
            int i;

            // let skin _verts and _norms grow as we add more _verts
            TorqueUtil.GrowArray<Vector3>(ref _workSkinVerts, _initialVerts.Length);
            TorqueUtil.GrowArray<Vector3>(ref _workSkinNormals, _initialNormals.Length);

            if (_initialTangents != null)
                TorqueUtil.GrowArray<Vector4>(ref _workSkinTangents, _initialTangents.Length);

            TorqueUtil.GrowArray<Matrix>(ref _workBoneTransforms, _nodeIndex.Length);

            _verts = _workSkinVerts;
            _norms = _workSkinNormals;
            _tangents = _workSkinTangents;

            // Set up bone transforms
            for (i = 0; i < _nodeIndex.Length; i++)
            {
                int node = _nodeIndex[i];
                _workBoneTransforms[i] = Matrix.Multiply(_initialTransforms[i], Shape.Transforms[node]);
            }

            // multiply _verts and normals by boneTransforms
            int prevIndex = -1;
            Vector3 v0, n0;
            Vector4 t0 = new Vector4();

            for (i = 0; i < _vertexIndex.Length; i++)
            {
                int vIndex = _vertexIndex[i];
                v0 = MatrixUtil.MatMulP(ref _initialVerts[vIndex], ref _workBoneTransforms[_boneIndex[i]]);
                n0 = MatrixUtil.MatMulP(ref _initialNormals[vIndex], ref _workBoneTransforms[_boneIndex[i]]);
                if (_initialTangents != null)
                    t0 = MatrixUtil.MatMulP(ref _initialTangents[vIndex], ref _workBoneTransforms[_boneIndex[i]]);
                v0 *= _weight[i];
                n0 *= _weight[i];
                if (_initialTangents != null)
                    t0 *= _weight[i];
                if (vIndex != prevIndex)
                {
                    _verts[vIndex].X = 0.0f;
                    _verts[vIndex].Y = 0.0f;
                    _verts[vIndex].Z = 0.0f;
                    _norms[vIndex].X = 0.0f;
                    _norms[vIndex].Y = 0.0f;
                    _norms[vIndex].Z = 0.0f;
                    if (_initialTangents != null)
                    {
                        _tangents[vIndex].X = 0.0f;
                        _tangents[vIndex].Y = 0.0f;
                        _tangents[vIndex].Z = 0.0f;
                        _tangents[vIndex].W = 0.0f;
                    }
                }
                _verts[vIndex] += v0;
                _norms[vIndex] += n0;
                if (_initialTangents != null)
                    _tangents[vIndex] += t0;
                prevIndex = vIndex;
            }

            // normalize normals...
            for (i = 0; i < _initialVerts.Length; i++)
            {
                float len = _norms[i].X * _norms[i].X + _norms[i].Y * _norms[i].Y + _norms[i].Z * _norms[i].Z;
                if (len > 0.01f)
                {
                    float invSqrt = 1.0f / (float)System.Math.Sqrt(len);
                    _norms[i].X *= invSqrt;
                    _norms[i].Y *= invSqrt;
                    _norms[i].Z *= invSqrt;
                }
                if (_initialTangents != null)
                {
                    len = _tangents[i].X * _tangents[i].X + _tangents[i].Y * _tangents[i].Y + _tangents[i].Z * _tangents[i].Z;
                    if (len > 0.01f)
                    {
                        float invSqrt = 1.0f / (float)System.Math.Sqrt(len);
                        _tangents[i].X *= invSqrt;
                        _tangents[i].Y *= invSqrt;
                        _tangents[i].Z *= invSqrt;
                    }
                }
            }
        }

        void RestoreSkin()
        {
            _verts = null;
            _norms = null;
            _tangents = null;
        }

        #endregion


        #region Private, protected, internal fields

        internal static D3DVertexBufferProfile SkinMeshVertexBufferProfile = new D3DVertexBufferProfile(BufferUsage.WriteOnly);

        internal Vector3[] _initialVerts;
        internal Vector3[] _initialNormals;
        internal Vector4[] _initialTangents;

        internal int[] _nodeIndex;
        internal Matrix[] _initialTransforms;

        internal int[] _vertexIndex;
        internal uint[] _boneIndex;
        internal float[] _weight;

        #endregion

        #region IDisposable Members

        public override void Dispose()
        {
            if (!_vb.IsNull)
            {
                _vb.Instance.Dispose();
                _vb.Invalidate();
            }
            if (!_ib.IsNull)
            {
                _ib.Instance.Dispose();
                _ib.Invalidate();
            }
            base.Dispose();
        }

        #endregion
    }
}