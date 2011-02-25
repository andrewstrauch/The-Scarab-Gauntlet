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



namespace GarageGames.Torque.TS
{
    /// <summary>
    /// A mesh type that stores clusters of primitives and sorts them before rendering.
    /// </summary>
    public class SortedMesh : Mesh
    {

        #region Constructors

        public SortedMesh() : base(MeshEnum.SortedMeshType) { }

        #endregion


        #region Public properties, operators, constants, and enums

        /// <summary>
        /// Stores information about a group of vertices to be used in sorting.
        /// </summary>
        public struct Cluster
        {
            /// <summary>
            /// The index of the primitive that starts this cluster.
            /// </summary>
            public int StartPrimitive;



            /// <summary>
            /// The index of the primitive that ends this cluster.
            /// </summary>
            public int EndPrimitive;



            /// <summary>
            /// The normal of the cluster.
            /// </summary>
            public Vector3 Normal;



            /// <summary>
            /// The distance along the normal of the cluster (defines a plane).
            /// </summary>
            public float K;



            /// <summary>
            /// Go to this cluster if in front of the plane.
            /// </summary>
            public int FrontCluster;



            /// <summary>
            /// Go to this cluster if in back of the plane.
            /// </summary>
            public int BackCluster;
        }

        #endregion


        #region Public methods

        public override int GetNumPolys()
        {
            int count = 0;
            int cIdx = _clusters.Length == 0 ? -1 : 0;
            while (cIdx >= 0)
            {
                Cluster cluster = _clusters[cIdx];
                for (int i = cluster.StartPrimitive; i < cluster.EndPrimitive; i++)
                {
                    if ((_primitives[i].MaterialIndex & DrawPrimitive.TypeMask) == DrawPrimitive.Triangles)
                        count += _primitives[i].NumElements / 3;
                    else
                        count += _primitives[i].NumElements - 2;
                }

                cIdx = cluster.FrontCluster; // always use frontCluster...we assume about the same no matter what
            }

            return count;
        }



        public override void Render(int frame, int matFrame, Material[] materialList, SceneRenderState srs)
        {
            Vector3 cameraCenter = srs.CameraPosition;

            DrawPrimitive.Set(_vb.Instance, _ib.Instance, _verts.Length, 0, GFXVertexFormat.VertexSize, GFXVertexFormat.GetVertexDeclaration(srs.Gfx.Device));

            int nextCluster = _startCluster;
            do
            {
                // Render the cluster...
                for (int i = _clusters[nextCluster].StartPrimitive; i < _clusters[nextCluster].EndPrimitive; i++)
                {
                    DrawPrimitive.SetMaterial(_primitives[i].MaterialIndex & DrawPrimitive.MaterialMask, materialList, ref _bounds, srs);
                    DrawPrimitive.Render(_primitives[i].MaterialIndex & DrawPrimitive.TypeMask, _primitives[i].Start, _primitives[i].NumElements);
                }

                // determine Next cluster...
                if (_clusters[nextCluster].FrontCluster != _clusters[nextCluster].BackCluster)
                {
                    float dot = _clusters[nextCluster].Normal.X * cameraCenter.X +
                            _clusters[nextCluster].Normal.Y * cameraCenter.Y +
                            _clusters[nextCluster].Normal.Z * cameraCenter.Z;

                    // this is opposite of TGE (i.e., -dot rather than dot).
                    nextCluster = (-dot > _clusters[nextCluster].K) ? _clusters[nextCluster].FrontCluster : _clusters[nextCluster].BackCluster;
                }
                else
                {
                    nextCluster = _clusters[nextCluster].FrontCluster;
                }
            } while (nextCluster >= 0);

            DrawPrimitive.Clear();
        }

        #endregion


        #region Private, protected, internal fields

        internal Cluster[] _clusters;
        internal int _startCluster;
        internal int _firstVert;
        internal int _numVerts;
        internal int _firstTVert;

        #endregion
    }
}
