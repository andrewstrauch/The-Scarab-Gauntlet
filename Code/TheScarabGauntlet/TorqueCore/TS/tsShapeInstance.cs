//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Microsoft.Xna.Framework;
using GarageGames.Torque.Core;
using GarageGames.Torque.MathUtil;
using GarageGames.Torque.SceneGraph;
using GarageGames.Torque.Util;



namespace GarageGames.Torque.TS
{
    /// <summary>
    /// Shape object wrapper that stores information specific to a shape instance.
    /// </summary>
    public struct ObjectInstance
    {
        #region Public properties, operators, constants, and enums

        /// <summary>
        /// The Object this is an instance of.
        /// </summary>
        public Object Object;



        /// <summary>
        /// The current frame of the object.
        /// </summary>
        public int Frame;



        /// <summary>
        /// The current material frame of the object.
        /// </summary>
        public int MaterialFrame;



        /// <summary>
        /// The current visibility of the object.
        /// </summary>
        public float Visibility;

        #endregion
    }



    /// <summary>
    /// Ifl material wrapper that stores information specific to a shape instance.
    /// </summary>
    public struct IflMaterialInstance
    {
        #region Public properties, operators, constants, and enums

        /// <summary>
        /// The material being wrapped.
        /// </summary>
        public IflMaterial IflMaterial;



        /// <summary>
        /// The frame this instance of the material is on.
        /// </summary>
        public int Frame;

        #endregion
    };



    /// <summary>
    /// Shape wrapper to allow shape data to exist once, and instance specific data to
    /// exist in every place it is needed. This stores information about the current state
    /// of the shape, like current animation frame, detail level, and trigger data.
    /// </summary>
    public partial class ShapeInstance
    {
        /// <summary>
        /// Helper structure for translating node transforms into Transform3D objects. These are
        /// only created for nodes that are actually referenced by a Transform3D.
        /// </summary>
        struct TSCallback
        {

            #region Constructors

            public TSCallback(int nodeIndex, Transform3D transform)
            {
                Transform = transform;
                NodeIndex = nodeIndex;
            }

            #endregion


            #region Public properties, operators, constants, and enums

            /// <summary>
            /// The Transform3D of the node at NodeIndex.
            /// </summary>
            public Transform3D Transform;



            /// <summary>
            /// The node index associated with this transform.
            /// </summary>
            public int NodeIndex;

            #endregion
        }


        #region Static methods, fields, constructors

        public static bool ForceHighestDetail = false;

        // Workspace for Node Transforms
        internal static Quaternion[] _nodeCurrentRotations;
        internal static Vector3[] _nodeCurrentTranslations;
        internal static float[] _nodeCurrentUniformScales;
        internal static Vector3[] _nodeCurrentAlignedScales;
        internal static ArbitraryScale[] _nodeCurrentArbitraryScales;

        // Keep track of which thread controls what on 
        // currently animating shape
        internal static Thread[] _workRotationThreads;
        internal static Thread[] _workTranslationThreads;
        internal static Thread[] _workScaleThreads;

        internal static bool _renderTranslucent = true;
        internal static bool _renderNonTranslucent = true;
        #endregion


        #region Constructors

        /// <summary>
        /// Creates a shape instance for a specified shape.
        /// </summary>
        /// <param name="inShape">The shape to wrap in this instance.</param>
        /// <param name="loadMaterials"></param>
        public ShapeInstance(Shape inShape, bool loadMaterials)
        {
            _shape = inShape;

            _currentDetailLevel = 0;

            // Set up subtree data
            int ss = _shape.SubShapeFirstNode.Length;
            _dirtyFlags = new DirtyFlags[ss];

            // Set up node data
            int numNodes = _shape.Nodes.Length;
            _nodeTransforms = new Matrix[numNodes];

            // add objects to trees
            int numObjects = _shape.Objects.Length;
            _meshObjects = new ObjectInstance[numObjects];
            for (int i = 0; i < numObjects; i++)
            {
                _meshObjects[i].Object = _shape.Objects[i];
                _meshObjects[i].Visibility = 1.0f;
            }

            // initialize bitvectors
            _transitionRotationNodes = new BitVector();
            _transitionRotationNodes.SetSize(numNodes);
            _transitionTranslationNodes = new BitVector();
            _transitionTranslationNodes.SetSize(numNodes);
            _transitionScaleNodes = new BitVector();
            _transitionScaleNodes.SetSize(numNodes);
            _disableBlendNodes = new BitVector();
            _disableBlendNodes.SetSize(numNodes);
            _handsOffNodes = new BitVector();
            _handsOffNodes.SetSize(numNodes);

            // make sure we have a thread list
            _threadList = new List<Thread>();
            _transitionThreads = new List<Thread>();

            // construct ifl material objects
            _iflMaterialInstances = new IflMaterialInstance[_shape.IflMaterials.Length];
            for (int i = 0; i < _shape.IflMaterials.Length; i++)
            {
                _iflMaterialInstances[i].IflMaterial = _shape.IflMaterials[i];
                _iflMaterialInstances[i].Frame = -1;
            }

            if (loadMaterials)
                _SetMaterialList(_shape.MaterialList);

            _AnimateSubtrees(true);
        }

        #endregion


        #region Public properties, operators, constants, and enums

        /// <summary>
        /// Gets the array of current node transforms for the shape.
        /// </summary>
        public Matrix[] NodeTransforms
        {
            get { return _nodeTransforms; }
        }

        #endregion


        #region Public methods

        /// <summary>
        /// Gets the shape associated with this instance.
        /// </summary>
        /// <returns>The shape.</returns>
        public Shape GetShape()
        {
            return _shape;
        }



        /// <summary>
        /// Advances time on all threads in the shape.
        /// </summary>
        /// <param name="delta"></param>
        public void AdvanceTime(float delta)
        {
            for (int i = 0; i < _threadList.Count; i++)
                _threadList[i].AdvanceTime(delta);
        }



        /// <summary>
        /// Renders the shape.
        /// </summary>
        /// <param name="dl">The detail level to render at.</param>
        /// <param name="intraDL">Unused</param>
        /// <param name="objectScale">Unused</param>
        /// <param name="srs">The current render state of the scene.</param>
        /// <param name="hidden">Bitmask of the mesh indices that should not be rendered.</param>
        public void Render(int dl, float intraDL, Vector3 objectScale, SceneRenderState srs, int hidden)
        {
            // if dl==-1, nothing to do
            if (dl == -1)
                return;

            Assert.Fatal(dl >= 0 && dl < _shape.DetailLevelCount, "ShapeInstance.render - Invalid detail level.");

            ss = _shape.Details[dl].SubShapeNumber;
            od = _shape.Details[dl].ObjectDetailNumber;

            if (ss < 0)
                return;

            // Set up static data
            _SetStatics(dl, intraDL, objectScale);
            _shape.CreateVBIB(srs.Gfx.Device);

            // Set up animating ifl materials
            for (int i = 0; i < _iflMaterialInstances.Length; i++)
                _materialList[_iflMaterialInstances[i].IflMaterial.MaterialSlot] = _materialList[_iflMaterialInstances[i].IflMaterial.FirstFrame + _iflMaterialInstances[i].Frame];

            // run through the meshes
            start = !_renderNonTranslucent ? _shape.SubShapeFirstTranslucentObject[ss] : _shape.SubShapeFirstObject[ss];
            end = !_renderTranslucent ? _shape.SubShapeFirstTranslucentObject[ss] : _shape.SubShapeFirstObject[ss] + _shape.SubShapeObjectCount[ss];

            for (int i = start; i < end; i++)
            {
                if ((1 << i & hidden) > 0)
                    continue;

                _RenderMesh(ref _meshObjects[i], od, _materialList, srs);
            }

            _ClearStatics();
        }



        /// <summary>
        /// Select a detail level given the specified scene render state.
        /// </summary>
        /// <param name="srs">The scene render state to base detail level selection on.</param>
        /// <returns>The detail level to use.</returns>
        public int SelectCurrentDetail(SceneRenderState srs)
        {
            if (_shape.DetailLevelCount == 1)
            {
                _currentDetailLevel = 0;
                return 0;
            }

            Matrix world = srs.World.Top;
            Vector3 p;
            Vector3.Transform(ref _shape.Center, ref world, out p);
            p -= srs.CameraPosition;
            float dist = p.LengthSquared(); ;

            x = world.Left;
            y = world.Forward;
            z = world.Up;
            float scale = x.LengthSquared();
            scale += y.LengthSquared();
            scale += z.LengthSquared();
            scale *= 0.3334f;

            dist /= scale;
            dist = (float)Math.Sqrt(dist);

            float pixelRadius = srs.Gfx.ProjectRadius(dist, _shape.Radius);
            return SelectCurrentDetail(pixelRadius);
        }



        /// <summary>
        /// Select a detail level given the specified size on screen.
        /// </summary>
        /// <param name="size">The pixel size of the object on the screen.</param>
        /// <returns>The detail level to use.</returns>
        public int SelectCurrentDetail(float size)
        {
            if (ForceHighestDetail)
            {
                _currentDetailLevel = 0;
                return _currentDetailLevel;
            }

            // check to see if not visible first...
            if (size <= _shape.SmallestVisibleSize)
            {
                // don't render...
                _currentDetailLevel = -1;
                return -1;
            }

            // same detail level as last time?
            // only search for detail level if the current one isn't the right one already
            if (_currentDetailLevel < 0 ||
                 (_currentDetailLevel == 0 && size <= _shape.Details[0].PixelSize) ||
                 (_currentDetailLevel > 0 && (size <= _shape.Details[_currentDetailLevel].PixelSize || size > _shape.Details[_currentDetailLevel - 1].PixelSize)))
            {
                // scan shape for highest detail size smaller than us...
                // shapes details are sorted from largest to smallest...
                // a detail of size <= 0 means it isn't a renderable detail level (utility detail)
                for (int i = 0; i < _shape.Details.Length; i++)
                {
                    if (size > _shape.Details[i].PixelSize)
                    {
                        _currentDetailLevel = i;
                        break;
                    }

                    if (i + 1 >= _shape.Details.Length || _shape.Details[i + 1].PixelSize < 0)
                    {
                        // We've run out of details and haven't found anything?
                        // Let's just grab this one.
                        _currentDetailLevel = i;
                        break;
                    }
                }
            }

            return _currentDetailLevel;
        }

        #endregion


        #region Private, protected, internal methods

        void _SetMaterialList(Material[] mlist)
        {
            _materialList = mlist;
        }



        void _RenderMesh(ref ObjectInstance obj, int objectDetail, Material[] materialList, SceneRenderState srs)
        {
            if (obj.Visibility > 0.01f)
            {
                mesh = _GetMesh(ref obj.Object, objectDetail);

                if (mesh != null)
                {
                    srs.World.Push();

                    if (obj.Object.NodeIndex >= 0)
                        srs.World.MultiplyMatrixLocal(_nodeTransforms[obj.Object.NodeIndex]);

                    mesh.Render(obj.Frame, obj.MaterialFrame, materialList, srs);

                    srs.World.Pop();
                }
            }
        }



        Mesh _GetMesh(ref Object obj, int num)
        {
            return num < obj.MeshCount ? _shape.Meshes[obj.FirstMesh + num] : null;
        }



        void _SetStatics(int dl, float intraDL, Vector3 objectScale)
        {
            Shape.Transforms = _nodeTransforms;
            Shape.CurrentFilePath = _shape.FilePath;
        }



        void _ClearStatics()
        {
            Shape.Transforms = null;
            Shape.CurrentFilePath = null;
        }

        #endregion


        #region Private, protected, internal fields

        internal Vector3 x, y, z;
        internal int ss, od, start, end;
        internal Mesh mesh = null;

        internal ObjectInstance[] _meshObjects;
        internal IflMaterialInstance[] _iflMaterialInstances;

        /// storage space for node transforms
        private Matrix[] _nodeTransforms;

        // Reference Transform Vectors. Unused until first transition
        internal Quat16[] _nodeReferenceRotations;
        internal Vector3[] _nodeReferenceTranslations;
        internal float[] _nodeReferenceUniformScales;
        internal Vector3[] _nodeReferenceScaleFactors;
        internal Quat16[] _nodeReferenceArbitraryScaleRots;

        // Resource data:
        private Shape _shape;

        // by default, grab material list from shape
        public Material[] _materialList;

        private bool _scaleCurrentlyAnimated;
        private List<Thread> _threadList;
        private List<Thread> _transitionThreads;
        internal DirtyFlags[] _dirtyFlags;
        List<TSCallback> _callbackNodes;

        // Transition nodes -- keep track of nodes involved in a transition.
        private BitVector _transitionRotationNodes;
        private BitVector _transitionTranslationNodes;
        private BitVector _transitionScaleNodes;

        // keep track of nodes with animation restrictions put on them
        private BitVector _disableBlendNodes;

        // Turn off animation on these threads (control via external code)
        private BitVector _handsOffNodes;

        // detail level - lower numbers mean bigger on screen
        private int _currentDetailLevel;

        // Triggers
        private int _triggerStates;

        #endregion
    }
}
