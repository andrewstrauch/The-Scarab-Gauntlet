//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using GarageGames.Torque.Core;
using GarageGames.Torque.MathUtil;



namespace GarageGames.Torque.TS
{
    /// <summary>
    /// The base class for all hierarchical 3D transforms used in Torque X.
    /// </summary>
    abstract public class Transform3D : IDisposable
    {
        /// <summary>
        /// An interface for any object that requires updates when it's transform is altered.
        /// </summary>
        public interface IDirtyListener
        {
            void OnTransformDirty();
        }


        #region Public properties, operators, constants, and enums

        /// <summary>
        /// The parent of this transform. Ex: If this transform is a mounted scene object, the transform
        /// of the object it's mounted to should be the parent if this transform.
        /// </summary>
        public Transform3D ParentTransform
        {
            get { return _parentTransform; }
            set
            {
                if (_parentTransform == value)
                    return;

                _flags |= TransformFlags.ParentDirty;
                _parentTransform = value;

                if (_dirtyListener != null)
                    _dirtyListener.OnTransformDirty();
            }
        }

        /// <summary>
        /// Has the transform been disposed yet.
        /// </summary>
        public bool IsDisposed
        {
            get { return _IsDisposed; }
        }

        /// <summary>
        /// The IDirtyListener that will recieve notifications when this transform becomes altered in any way.
        /// </summary>
        public IDirtyListener DirtyListener
        {
            get { return _dirtyListener; }
            set { _dirtyListener = value; }
        }



        /// <summary>
        /// Specifies whether or not this transform has a local scale applied to it.
        /// </summary>
        public bool HasLocalScale
        {
            get { return (_flags & TransformFlags.LocalHasScale) != TransformFlags.None; }
        }



        /// <summary>
        /// Specifies whether or not this transform has a local or object-level scale applied to it.
        /// </summary>
        public bool HasObjectScale
        {
            get
            {
                if (HasLocalScale)
                    return true;

                // check all parent transforms except last for scale
                Transform3D walk = _parentTransform;

                while (walk != null && walk._parentTransform != null)
                {
                    if (walk.HasLocalScale)
                        return true;

                    walk = walk._parentTransform;
                }

                return false;
            }
        }



        /// <summary>
        /// Specifies whether or not this transform or any of it's parents including the top level have
        /// local scales applied to them.
        /// </summary>
        public bool HasWorldScale
        {
            get
            {
                if (HasLocalScale)
                    return true;

                // check all parent transforms for scale
                Transform3D walk = _parentTransform;

                while (walk != null)
                {
                    if (walk.HasLocalScale)
                        return true;

                    walk = walk._parentTransform;
                }

                return false;
            }
        }



        /// <summary>
        /// The world matrix of this transform.
        /// </summary>
        public Matrix WorldMatrix
        {
            get
            {
                Matrix world;

                GetWorldMatrix(out world, true);

                return world;
            }

            set
            {
                if (_parentTransform != null)
                {
                    Matrix parentMatrix;
                    _parentTransform.GetWorldMatrix(out parentMatrix, true);
                    Matrix parentMatrixInv;
                    Matrix.Invert(ref parentMatrix, out parentMatrixInv);
                    Matrix local;
                    Matrix.Multiply(ref value, ref parentMatrixInv, out local);
                    SetLocalMatrix(ref local);
                }
                else
                    SetLocalMatrix(ref value);
            }
        }



        /// <summary>
        /// The object matrix of this transform.
        /// </summary>
        public Matrix ObjectMatrix
        {
            get
            {
                Matrix objMatrix;
                GetObjectMatrix(out objMatrix, true);
                return objMatrix;
            }

            set
            {
                if (_parentTransform != null)
                {
                    Matrix parentMatrix;
                    _parentTransform.GetObjectMatrix(out parentMatrix, true);
                    Matrix parentMatrixInv;
                    Matrix.Invert(ref parentMatrix, out parentMatrixInv);
                    Matrix local;
                    Matrix.Multiply(ref value, ref parentMatrixInv, out local);
                    SetLocalMatrix(ref local);
                }
                else
                    SetLocalMatrix(ref value);
            }
        }



        /// <summary>
        /// The local matrix of this transform.
        /// </summary>
        public Matrix LocalMatrix
        {
            get
            {
                Matrix loc;
                GetLocalMatrix(out loc, true);
                return loc;
            }
            set
            {
                SetLocalMatrix(ref value);
            }
        }



        /// <summary>
        /// The position of this transform.
        /// </summary>
        abstract public Vector3 Position { get; set; }



        /// <summary>
        /// The rotation of this transform.
        /// </summary>
        abstract public Quaternion Rotation { get; set; }



        /// <summary>
        /// The scale of this transform.
        /// </summary>
        abstract public Vector3 Scale { get; set; }

        #endregion


        #region Public methods

        /// <summary>
        /// Return sthe world matrix of this transform.
        /// </summary>
        /// <param name="world">The output matrix that will be set to the world matrix of this transform.</param>
        /// <param name="includeLocalScale">Specifies whether or not to factor in local scale in the transform.</param>
        abstract public void GetWorldMatrix(out Matrix world, bool includeLocalScale);



        /// <summary>
        /// Returs the object matrix of this transform.
        /// </summary>
        /// <param name="obj">The output matrix that will be set to the object matrix of this transform.</param>
        /// <param name="includeLocalScale">Specifies whether or not to factor in local scale in the transform.</param>
        abstract public void GetObjectMatrix(out Matrix obj, bool includeLocalScale);



        /// <summary>
        /// Returs the object matrix of this transform.
        /// </summary>
        /// <param name="local">The output matrix that will be set to the local matrix of this transform.</param>
        /// <param name="includeLocalScale">Specifies whether or not to factor in local scale in the transform.</param>
        abstract public void GetLocalMatrix(out Matrix local, bool includeLocalScale);



        /// <summary>
        /// Sets the local matrix of this transform.
        /// </summary>
        /// <param name="local">The new local matrix of this transform.</param>
        abstract public void SetLocalMatrix(ref Matrix local);



        /// <summary>
        /// Returns true if this transform is a child of the specified parent transform.
        /// </summary>
        /// <param name="parent">The parent transform for which to check inheritance.</param>
        /// <param name="recursive">Specifies whether or not the check should be recursive to parents of parents, etc.</param>
        /// <returns>True if the transform specified was found to be a parent of this transform.</returns>
        public bool IsChildOf(Transform3D parent, bool recursive)
        {
            if (_parentTransform != null)
            {
                if (_parentTransform == parent)
                    return true;
                else if (recursive)
                    return _parentTransform.IsChildOf(parent, true);
                else
                    return false;
            }
            else
            {
                return false;
            }
        }

        #endregion


        #region Private, protected, internal fields

        protected enum TransformFlags
        {
            None = 0,
            LocalHasScale = 1 << 0,
            LocalPositionDirty = 1 << 1,
            LocalRotationDirty = 1 << 2,
            LocalScaleDirty = 1 << 3,
            LocalDirty = LocalPositionDirty | LocalRotationDirty,
            ParentDirty = 1 << 4,
            LastFlag = 1 << 4
        }

        protected Transform3D _parentTransform;
        protected IDirtyListener _dirtyListener;
        protected TransformFlags _flags;
        protected bool _IsDisposed = false;

        #endregion

        #region IDisposable Members

        public virtual void Dispose()
        {
            _IsDisposed = true;
            _dirtyListener = null;
            _parentTransform = null;
        }

        #endregion
    }



    /// <summary>
    /// A generic type of transform that's used for most 3D objects in the engine.
    /// </summary>
    public class Transform3DInPlace : Transform3D
    {

        #region Public properties, operators, constants, and enums

        public override Vector3 Position
        {
            get
            {
                return _position;
            }
            set
            {
                _position = value;
                _flags |= TransformFlags.LocalPositionDirty;
                if (_dirtyListener != null)
                    _dirtyListener.OnTransformDirty();
            }
        }



        public override Quaternion Rotation
        {
            get
            {
                return _rotation;
            }
            set
            {
                _rotation = value;

                _flags |= TransformFlags.LocalRotationDirty;
                if (_dirtyListener != null)
                    _dirtyListener.OnTransformDirty();
            }
        }



        public override Vector3 Scale
        {
            get
            {
                return _scale;
            }
            set
            {
                _scale = value;
                _flags |= TransformFlags.LocalScaleDirty;
                if (_dirtyListener != null)
                    _dirtyListener.OnTransformDirty();
            }
        }

        #endregion


        #region Public methods

        public override void GetWorldMatrix(out Matrix worldMat, bool includeLocalScale)
        {
            if (_parentTransform == null)
                GetLocalMatrix(out worldMat, includeLocalScale);
            else
            {
                Matrix localMat, parentMat;
                GetLocalMatrix(out localMat, includeLocalScale);
                _parentTransform.GetWorldMatrix(out parentMat, true);
                Matrix.Multiply(ref localMat, ref parentMat, out worldMat);
            }
        }



        public override void GetObjectMatrix(out Matrix objectMat, bool includeLocalScale)
        {
            if (_parentTransform == null)
                objectMat = Matrix.Identity;
            else if (_parentTransform.ParentTransform == null)
                GetLocalMatrix(out objectMat, includeLocalScale);
            else
            {
                Matrix localMat, parentMat;
                GetLocalMatrix(out localMat, includeLocalScale);
                _parentTransform.GetObjectMatrix(out parentMat, true);
                Matrix.Multiply(ref localMat, ref parentMat, out objectMat);
            }
        }



        public override void GetLocalMatrix(out Matrix localMat, bool includeLocalScale)
        {
            Matrix.CreateFromQuaternion(ref _rotation, out localMat);

            localMat.Translation = _position;
            if (includeLocalScale)
                MatrixUtil.ApplyPreScale(ref _scale, ref localMat);
        }



        public override void SetLocalMatrix(ref Matrix local)
        {
            _position = local.Translation;
            Quaternion.CreateFromRotationMatrix(ref local, out _rotation);

            MatrixUtil.GetPreScale(ref local, out _scale);
            _flags |= TransformFlags.LocalScaleDirty | TransformFlags.LocalRotationDirty | TransformFlags.LocalPositionDirty;
            if (_dirtyListener != null)
                _dirtyListener.OnTransformDirty();
        }

        #endregion


        #region Private, protected, internal fields

        Vector3 _position;
        Quaternion _rotation = Quaternion.Identity;
        Vector3 _scale = Vector3.One;

        #endregion
    }



    /// <summary>
    /// A specific type of transform used for TS objects (DTS meshes).
    /// </summary>
    public class TSTransform3D : Transform3D
    {

        #region Constructors

        public TSTransform3D(TS.ShapeInstance si, int nodeIndex)
        {
            _shapeInstance = si;
            _nodeIndex = nodeIndex;
            Assert.Fatal(_nodeIndex >= 0 && _nodeIndex < _shapeInstance.NodeTransforms.Length, "TSTransform3D Constructor - TSTransform3D nodeIndex out of range.");
        }

        #endregion


        #region Public properties, operators, constants, and enums

        public override Vector3 Position
        {
            get
            {
                if (!_HandleLocal)
                {
                    _shapeInstance.Animate();

                    // if node has no parent, easy enough to just grab the matrix of the node
                    int parentIdx = _shapeInstance.GetShape().Nodes[_nodeIndex].ParentIndex;
                    if (parentIdx < 0)
                        return _shapeInstance.NodeTransforms[_nodeIndex].Translation;

                    // has parent, local is transform from this node to parent so get local matrix the hard way
                    Matrix mat = Matrix.Multiply(_shapeInstance.NodeTransforms[_nodeIndex], Matrix.Invert(_shapeInstance.NodeTransforms[parentIdx]));
                    return mat.Translation;
                }
                else
                {
                    return _position;
                }
            }
            set
            {
                _HandleLocal = true;
                _position = value;
                _flags |= TransformFlags.LocalPositionDirty;
                if (_dirtyListener != null)
                    _dirtyListener.OnTransformDirty();
            }
        }



        public override Quaternion Rotation
        {
            get
            {
                if (!_HandleLocal)
                {
                    _shapeInstance.Animate();

                    // if node has no parent, easy enough to just grab the matrix of the node
                    int parentIdx = _shapeInstance.GetShape().Nodes[_nodeIndex].ParentIndex;
                    if (parentIdx < 0)
                    {
                        Quaternion.CreateFromRotationMatrix(ref _shapeInstance.NodeTransforms[_nodeIndex], out _rotation);
                        return _rotation;
                    }

                    // has parent, local is transform from this node to parent so get local matrix the hard way
                    Matrix mat = Matrix.Multiply(_shapeInstance.NodeTransforms[_nodeIndex], Matrix.Invert(_shapeInstance.NodeTransforms[parentIdx]));
                    Quaternion.CreateFromRotationMatrix(ref mat, out _rotation);
                    return _rotation;
                }
                else
                {
                    return _rotation;
                }
            }
            set
            {
                _HandleLocal = true;
                _rotation = value;
                _flags |= TransformFlags.LocalRotationDirty;
                if (_dirtyListener != null)
                    _dirtyListener.OnTransformDirty();
            }
        }



        public override Vector3 Scale
        {
            get
            {
                if (!_HandleLocal)
                {
                    _shapeInstance.Animate();

                    // if node has no parent, easy enough to just grab the matrix of the node
                    int parentIdx = _shapeInstance.GetShape().Nodes[_nodeIndex].ParentIndex;
                    if (parentIdx < 0)
                    {
                        MatrixUtil.GetPreScale(ref _shapeInstance.NodeTransforms[_nodeIndex], out _scale);
                        return _scale;
                    }

                    // has parent, local is transform from this node to parent so get local matrix the hard way
                    Matrix mat = Matrix.Multiply(_shapeInstance.NodeTransforms[_nodeIndex], Matrix.Invert(_shapeInstance.NodeTransforms[parentIdx]));
                    MatrixUtil.GetPreScale(ref mat, out _scale);
                    return _scale;
                }
                else
                {
                    return _scale;
                }
            }
            set
            {
                _HandleLocal = true;
                _scale = value;
                _flags |= TransformFlags.LocalScaleDirty;
                if (_dirtyListener != null)
                    _dirtyListener.OnTransformDirty();
            }
        }

        #endregion


        #region Public methods

        public override void GetWorldMatrix(out Matrix worldMat, bool includeLocalScale)
        {
            if (_parentTransform == null)
            {
                _shapeInstance.Animate();
                worldMat = _shapeInstance.NodeTransforms[_nodeIndex];
            }
            else
            {
                _shapeInstance.Animate();

                Matrix parentMat;
                _parentTransform.GetWorldMatrix(out parentMat, true);
                Matrix.Multiply(ref _shapeInstance.NodeTransforms[_nodeIndex], ref parentMat, out worldMat);
            }
        }



        public override void GetObjectMatrix(out Matrix objectMat, bool includeLocalScale)
        {
            if (_parentTransform == null)
                objectMat = Matrix.Identity;
            else if (_parentTransform.ParentTransform == null)
            {
                _shapeInstance.Animate();
                objectMat = _shapeInstance.NodeTransforms[_nodeIndex];
            }
            else
            {
                _shapeInstance.Animate();

                Matrix parentMat;
                _parentTransform.GetObjectMatrix(out parentMat, true);
                Matrix.Multiply(ref _shapeInstance.NodeTransforms[_nodeIndex], ref parentMat, out objectMat);
            }
        }



        public override void GetLocalMatrix(out Matrix localMat, bool includeLocalScale)
        {
            if (_HandleLocal)
            {
                Matrix.CreateFromQuaternion(ref _rotation, out localMat);
                localMat.Translation = _position;
                if (includeLocalScale)
                    MatrixUtil.ApplyPreScale(ref _scale, ref localMat);
            }
            else
            {
                _shapeInstance.Animate();
                localMat = _shapeInstance.NodeTransforms[_nodeIndex];
                if (!includeLocalScale && (_flags & TransformFlags.LocalHasScale) != TransformFlags.None)
                {
                    Vector3 scale;
                    MatrixUtil.GetPreScale(ref localMat, out scale);
                    scale.X = 1.0f / scale.X;
                    scale.Y = 1.0f / scale.Y;
                    scale.Z = 1.0f / scale.Z;
                    MatrixUtil.ApplyPreScale(ref scale, ref localMat);
                }
            }

        }



        public override void SetLocalMatrix(ref Matrix local)
        {
            _HandleLocal = true;
            _position = local.Translation;
            Quaternion.CreateFromRotationMatrix(ref local, out _rotation);
            MatrixUtil.GetPreScale(ref local, out _scale);
            _flags |= TransformFlags.LocalScaleDirty | TransformFlags.LocalRotationDirty | TransformFlags.LocalPositionDirty;
            if (_dirtyListener != null)
                _dirtyListener.OnTransformDirty();
        }

        #endregion


        #region Private, protected, internal fields

        [Flags]
        enum TSTransformFlags
        {
            HandleLocal = TransformFlags.LastFlag << 1,
            LastFlag = TransformFlags.LastFlag << 1
        }



        protected bool _HandleLocal
        {
            get { return (_flags & (TransformFlags)TSTransformFlags.HandleLocal) != TransformFlags.None; }
            set
            {
                if (value == _HandleLocal)
                    return;

                if (value)
                {
                    _position = Position;
                    _rotation = Rotation;
                    _scale = Scale;
                    _shapeInstance.SetHandsOff(_nodeIndex, this);
                }
                else
                    _shapeInstance.ClearHandsOff(_nodeIndex);
                _flags ^= (TransformFlags)TSTransformFlags.HandleLocal;
            }
        }



        TS.ShapeInstance _shapeInstance;
        int _nodeIndex;

        Vector3 _position;
        Quaternion _rotation;
        Vector3 _scale;

        #endregion
    }
}
