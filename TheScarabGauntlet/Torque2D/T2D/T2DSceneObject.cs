//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using GarageGames.Torque.Core;
using GarageGames.Torque.MathUtil;
using GarageGames.Torque.Materials;
using GarageGames.Torque.SceneGraph;
using GarageGames.Torque.Sim;
using GarageGames.Torque.Util;



namespace GarageGames.Torque.T2D
{
    /// <summary>
    /// Object which provides basic functionality for 2D game objects.  T2DSceneObjects
    /// can be added to a T2DSceneGraph, can be mounted to other T2DSceneObjects, and can
    /// be modified by adding a number of built in components.  In particular, to add
    /// collision checking add a T2DCollisionComponent, to add physics add a T2DPhysicsComponent,
    /// and to add the ability to hold link points add a T2DLinkPointComponent.
    /// </summary>
    public class T2DSceneObject : TorqueObject, ISceneObject2D, ITickObject, IDisposable
    {
        struct MountEnumerator : IEnumerator<T2DSceneObject>
        {
            #region Constructors

            public MountEnumerator(T2DSceneObject sceneObject) { _sceneObject = sceneObject; _walk = null; }

            #endregion


            #region Public properties, operators, constants, and enums

            public T2DSceneObject Current
            {
                get { return _walk.Val; }
            }

            #endregion


            #region Public methods

            public bool MoveNext()
            {
                if (_walk == null)
                    _walk = _sceneObject._mountedObjects;
                else
                    _walk = _walk.Next;
                return _walk != null;
            }



            public void Reset()
            {
                _walk = null;
            }



            public void Dispose()
            {
            }



            public MountEnumerator GetEnumerator()
            {
                return this;
            }

            #endregion


            #region Private, protected, internal methods

            object System.Collections.IEnumerator.Current
            {
                get { return Current; }
            }

            #endregion


            #region Private, protected, internal fields

            SList<T2DSceneObject> _walk;
            T2DSceneObject _sceneObject;

            #endregion
        }


        #region Constructors

        public T2DSceneObject()
        {
            Visible = true;
        }

        #endregion


        #region Public properties, operators, constants, and enums

        /// <summary>
        /// The scenegraph associated with this scene object.
        /// </summary>
        public BaseSceneGraph SceneGraph
        {
            get { return _sceneGraph; }
            set
            {
                if (_sceneGraph == value)
                    return;

                if (value != null && !(value is T2DSceneGraph))
                    return;

                if (_sceneGraph != null)
                    _sceneGraph.RemoveObject(this);

                _sceneGraph = value as T2DSceneGraph;

                if (_sceneGraph != null && IsRegistered)
                    _sceneGraph.AddObject(this);
            }
        }



        /// <summary>
        /// Layer to draw scene object on.  Lower numbered layers are drawn on top.  
        /// </summary>
        public int Layer
        {
            get { return _layer; }
            set { _layer = value; }
        }



        /// <summary>
        /// Object is drawn at this z coordinate when rendering.  This is a function
        /// of Layer and is used for rendering purposes only.
        /// </summary>
        [TorqueCloneIgnore]
        public float LayerDepth
        {
            get { return _layerDepth; }
            set { _layerDepth = value; }
        }



        /// <summary>
        /// LayerMask is used for collision checking.  The layer mask is determined
        /// by setting the bit corresponding with the layer (layers outside the range
        /// of 0..31 are treated as Layer % 32).
        /// </summary>
        public uint LayerMask
        {
            get { return (uint)(1 << _layer); }
        }



        /// <summary>
        /// Order of object within a layer.  This is a read-only property used when rendering
        /// to make sure objects are rendered in the same order from frame to frame.
        /// </summary>
        public int LayerOrder
        {
            get { return (int)ObjectId; }
        }



        /// <summary>
        /// Size in world units of scene object.
        /// </summary>
        public virtual Vector2 Size
        {
            get { return _size; }
            set
            {
                if (_size.Equals(value))
                    return;

                _size = value;
                IsSpatialDirty = true;
                UpdateSpatialData();

                if (Collision != null)
                    Collision.MarkCollisionDirty();
            }
        }


        [XmlIgnore]
        [TorqueCloneIgnore]
        public virtual Matrix Transform
        {
            get
            {
                Matrix mat = Matrix.CreateRotationZ(MathHelper.ToRadians(Rotation));
                Vector3 pos = new Vector3(Position.X, Position.Y, 0.0f);
                mat.Translation = pos;
                return mat;
            }
        }



        /// <summary>
        /// Position of scene object.  Setting this property after the object has been registered
        /// will cause it to interpolate between current position and new position.  In order to make
        /// an object move to a new position without interpolation use WarpToPosition method.
        /// </summary>
        public virtual Vector2 Position
        {
            get { return _position; }
            set
            {
                _position = value;
                _postTick.pos = _position;
                IsSpatialDirty = true;
                UpdateSpatialData();
            }
        }



        /// <summary>
        /// Rotation of scene object.  Setting this property after the object has been registered
        /// will cause it to interpolate between current rotation and new rotation.  In order to
        /// make an object move to a new rotation without interpolation use WarpToPosition method.
        /// </summary>
        public virtual float Rotation
        {
            get { return _rotation; }
            set
            {
                _rotation = ((value % 360.0f) + 360.0f) % 360.0f;
                _postTick.rot = _rotation;
                IsSpatialDirty = true;
                UpdateSpatialData();
            }
        }



        /// <summary>
        /// The reference point for sorting a scene objects within a layer.
        /// </summary>
        public virtual Vector2 SortPoint
        {
            get { return _sortPoint; }
            set { _sortPoint = value; }
        }



        /// <summary>
        /// Physics component for scene object, or null if it doesn't have one.
        /// </summary>
        public T2DPhysicsComponent Physics
        {
            get
            {
                if (!IsRegistered && _physics == null)
                {
                    if (HasComponents)
                        _physics = Components.FindComponent<T2DPhysicsComponent>();
                    if (_physics == null && CreateWithPhysics)
                    {
                        _physics = new T2DPhysicsComponent();
                        Components.AddComponent(_physics);
                    }
                }
                return _physics;
            }
        }



        /// <summary>
        /// Collision component for scene object, or null if it doesn't have one.
        /// </summary>
        public T2DCollisionComponent Collision
        {
            get
            {
                if (!IsRegistered && _collision == null)
                {
                    if (HasComponents)
                        _collision = Components.FindComponent<T2DCollisionComponent>();
                    if (_collision == null && CreateWithCollision)
                    {
                        _collision = new T2DCollisionComponent();
                        Components.AddComponent(_collision);
                    }
                }
                return _collision;
            }
        }



        /// <summary>
        /// World limit component for scene object, or null if it doesn't have one.
        /// </summary>
        public T2DWorldLimitComponent WorldLimit
        {
            get
            {
                if (!IsRegistered && _worldLimit == null)
                {
                    if (HasComponents)
                        _worldLimit = Components.FindComponent<T2DWorldLimitComponent>();
                    if (_worldLimit == null && CreateWithWorldLimit)
                    {
                        _worldLimit = new T2DWorldLimitComponent();
                        Components.AddComponent(_worldLimit);
                    }
                }
                return _worldLimit;
            }
        }



        /// <summary>
        /// Link point component for scene object, or null if it doesn't have one.  Note:
        /// link point component is not cached on object, so this property causes a
        /// component lookup.
        /// </summary>
        public T2DLinkPointComponent LinkPoints
        {
            get
            {
                T2DLinkPointComponent link = null;
                if (HasComponents)
                    link = Components.FindComponent<T2DLinkPointComponent>();
                if (link != null)
                    return link;
                if (!IsRegistered && CreateWithLinkPoints)
                {
                    link = new T2DLinkPointComponent();
                    Components.AddComponent(link);
                }
                return link;
            }
        }



        // private interface for xml deserialization of mounts.  
        // this object will be mounted to everything in the mount list.  
        // this list contains things that should be mounted on this object.
        [TorqueXmlDeserializeInclude] // needed because this property is nonpublic
        [XmlElement(ElementName = "Mounts")] // remap the element name 
        internal List<XmlMount> XmlMounts
        {
            get { return _xmlMounts; }
            set { _xmlMounts = value; }
        }



        /// <summary>
        /// True if scene object is mounted to another scene object.
        /// </summary>
        public bool IsMounted
        {
            get { return _mountedTo != null && TestFlag((TorqueObjectFlags)T2DFlags.MountedFlag); }
        }

        /// <summary>
        /// Returns the name of the object we are mounted to
        /// or an empty string if not mounted.
        /// </summary>
        public string MountedName
        {
            get { return IsMounted ? _mountedTo.Val.Name : ""; }
        }


        /// <summary>
        /// Returns the object we are mounted to or null.
        /// </summary>
        public T2DSceneObject MountedToObj
        {
            get { return IsMounted ? _mountedTo.Val.MountedTo : null; }
        }


        /// <summary>
        /// Returns the number of objects mounted to us.
        /// </summary>
        public int TotalMountedObjects
        {
            get { return _TotalMountedObjects; }
        }


        /// <summary>
        /// The list of mounted objects.
        /// </summary>         
        public SList<T2DSceneObject> MountedObjects
        {
            get { return _mountedObjects; }
        }


        /// <summary>
        /// True if scene object is owned by object it is mounted to.  If an object is owned
        /// by another object then it will be unregistered when that object is unregistered.
        /// </summary>
        [XmlIgnore]
        [TorqueCloneIgnore]
        public bool IsOwnedByMount
        {
            get { return TestFlag((TorqueObjectFlags)T2DFlags.OwnedByMountFlag); }
            set { _SetFlag((TorqueObjectFlags)T2DFlags.OwnedByMountFlag, value); }
        }



        /// <summary>
        /// True if scene object tracks rotation of object mounted to.  If the object was mounted
        /// with a link point the link rotation will be added in.  If the mount has a rotation offset
        /// then it will be added too.
        /// </summary>
        public bool TrackMountRotation
        {
            get { return TestFlag((TorqueObjectFlags)T2DFlags.TrackMountRotationFlag); }
            set { _SetFlag((TorqueObjectFlags)T2DFlags.TrackMountRotationFlag, value); }
        }



        /// <summary>
        /// True if object's FlipX and FlipY properties should match the 
        /// object mounted to.
        /// </summary>
        public bool InheritMountFlip
        {
            get { return TestFlag((TorqueObjectFlags)T2DFlags.InheritMountFlipFlag); }
            set
            {
                if (value && MountedTo != null)
                {
                    FlipX = MountedTo.FlipX;
                    FlipY = MountedTo.FlipY;
                }
                _SetFlag((TorqueObjectFlags)T2DFlags.InheritMountFlipFlag, value);
            }
        }



        /// <summary>
        /// True if object's Visible and VisibilityLevel properties should match the 
        /// object mounted to.
        /// </summary>
        public bool InheritMountVisibility
        {
            get { return TestFlag((TorqueObjectFlags)T2DFlags.InheritMountVisibilityFlag); }
            set
            {
                if (value && MountedTo != null)
                {
                    Visible = MountedTo.Visible;
                    VisibilityLevel = MountedTo.VisibilityLevel;
                }
                _SetFlag((TorqueObjectFlags)T2DFlags.InheritMountVisibilityFlag, value);
            }
        }



        /// <summary>
        /// Value of mount force, if UseMountForce is true.  Mount force is used to move
        /// an object towards mount point rather than snapping directly onto the mount point
        /// each update.  The units for mount force are arbitrary, with larger values resulting
        /// in tighter tracking.  A value of 1 means it will take about 1 second to snap into
        /// place.  A value of 2 means it will take about 1/2 seconds, etc.
        /// </summary>
        public float MountForce
        {
            get
            {
                if (_mountedTo != null)
                    return _mountedTo.Val.MountForce;
                else
                    return 0.0f;
            }
            set
            {
                if (_mountedTo == null && value != 0.0f)
                    // handle odd-ball case where we're setting mount force but no mount yet
                    SList<MountTo>.InsertFront(ref _mountedTo, new MountTo());

                if (_mountedTo != null)
                    _mountedTo.Val.MountForce = value;
            }
        }



        /// <summary>
        /// True if uses mount force (MountForce property must be non-zero for this to have
        /// an effect).
        /// </summary>
        public bool UseMountForce
        {
            get { return TestFlag((TorqueObjectFlags)T2DFlags.UseMountForceFlag); }
            set { _SetFlag((TorqueObjectFlags)T2DFlags.UseMountForceFlag, value); }
        }



        /// <summary>
        /// Object which current object is mounted to, or null if not mounted.
        /// </summary>
        public T2DSceneObject MountedTo
        {
            get { return IsMounted ? _mountedTo.Val.MountedTo : null; }
        }



        /// <summary>
        /// True if collisions are enabled on the object.  Even if collisions are enabled, object also needs
        /// to have a collision component and the collision component needs to have a collision image.
        /// </summary>
        public bool CollisionsEnabled
        {
            get { return TestFlag((TorqueObjectFlags)T2DFlags.CollisionsEnabledFlag); }
            set { _SetFlag((TorqueObjectFlags)T2DFlags.CollisionsEnabledFlag, value); }
        }



        /// <summary>
        /// True if visible.  Collisions are also disabled if not visible.
        /// </summary>
        public bool Visible
        {
            get { return (InheritMountVisibility && IsMounted) ? MountedTo.Visible : TestFlag((TorqueObjectFlags)T2DFlags.VisibleFlag); }
            set { _SetFlag((TorqueObjectFlags)T2DFlags.VisibleFlag, value); IsSpatialDirty = true; }
        }



        /// <summary>
        /// Sets alpha blend value to control the degree of visibility.  If Visible property is false then object is not visible no
        /// matter what this value is.
        /// </summary>
        public float VisibilityLevel
        {
            get { return (InheritMountVisibility && IsMounted) ? MountedTo.VisibilityLevel : _visibilityLevel.Value; }
            set { _visibilityLevel.Value = value; }
        }



        /// <summary>
        /// True if scene object has registered it's tick callbacks with the process list.  Set to true when
        /// SetTicking is called.  SetTicking needs only be called once on an object and the object will
        /// receive tick callbacks.  Normally T2DSceneObject handles whether or not to call SetTicking and
        /// user does not need to.
        /// </summary>
        public bool TickStarted
        {
            get { return TestFlag((TorqueObjectFlags)T2DFlags.TickStartedFlag); }
        }



        /// <summary>
        /// True if StartTicking has been called this tick.  It is not generally needed to check this property.
        /// If a component requires the object to have already started it's tick (e.g., if it changes the position
        /// of the object) then call StartTicking whether or not tick is already in progress. 
        /// </summary>
        public bool TickInProgress
        {
            get { return TestFlag((TorqueObjectFlags)T2DFlags.TickInProgressFlag); }
        }



        /// <summary>
        /// True if object has been added to the scene graph and scene container.
        /// </summary>
        public bool InSceneContainer
        {
            get
            {
                return SceneContainerData._binReferenceChain != null;
            }
        }



        [XmlIgnore]
        public bool PickingAllowed
        {
            get { return false; }
        }



        /// <summary>
        /// Draw object with x-axis flipped.
        /// </summary>
        public virtual bool FlipX
        {
            get { return (InheritMountFlip && IsMounted) ? MountedTo.FlipX : TestFlag((TorqueObjectFlags)T2DFlags.FlipXFlag); }
            set
            {
                _SetFlag((TorqueObjectFlags)T2DFlags.FlipXFlag, value);

                if (Collision != null)
                    Collision.MarkCollisionDirty();
            }
        }



        /// <summary>
        /// Draw object with y-axis flipped.
        /// </summary>
        public virtual bool FlipY
        {
            get { return (InheritMountFlip && IsMounted) ? MountedTo.FlipY : TestFlag((TorqueObjectFlags)T2DFlags.FlipYFlag); }
            set
            {
                _SetFlag((TorqueObjectFlags)T2DFlags.FlipYFlag, value);

                if (Collision != null)
                    Collision.MarkCollisionDirty();
            }
        }



        /// <summary>
        /// World-space rectangle bounding our object.
        /// </summary>
        public RectangleF WorldClipRectangle
        {
            get
            {
                return _worldClipRectangle;
            }
        }



        /// <summary>
        /// World-space rectangle bounding our object.
        /// </summary>
        public RectangleF WorldCollisionClipRectangle
        {
            // currently, this is always the same as world clip rectangle
            get { return WorldClipRectangle; }
        }



        [TorqueCloneIgnore]
        [XmlIgnore]
        public SceneContainerData SceneContainerData
        {
            get { return _sceneContainerData; }
            set { _sceneContainerData = value; }
        }



        [TorqueCloneIgnore]
        [XmlIgnore]
        public bool IsSpatialDirty
        {
            get { return TestFlag((TorqueObjectFlags)T2DFlags.SpatialDirtyFlag); }
            private set { _SetFlag((TorqueObjectFlags)T2DFlags.SpatialDirtyFlag, value); }
        }



        /// <summary>
        /// True if object should be created with physics component.  This property is
        /// true by default.
        /// </summary>
        public bool CreateWithPhysics
        {
            get { return TestFlag((TorqueObjectFlags)T2DFlags.CreateWithPhysicsFlag); }
            set
            {
                if (IsRegistered || value == TestFlag((TorqueObjectFlags)T2DFlags.CreateWithPhysicsFlag))
                    return;
                _SetFlag((TorqueObjectFlags)T2DFlags.CreateWithPhysicsFlag, value);

                // changing component profile, so need to reset shared pool
                PoolData = null;
            }
        }



        /// <summary>
        /// True if object should be created with collision component.  This property is
        /// true by default.
        /// </summary>
        public bool CreateWithCollision
        {
            get { return TestFlag((TorqueObjectFlags)T2DFlags.CreateWithCollisionFlag); }
            set
            {
                if (IsRegistered || value == TestFlag((TorqueObjectFlags)T2DFlags.CreateWithCollisionFlag))
                    return;
                _SetFlag((TorqueObjectFlags)T2DFlags.CreateWithCollisionFlag, value);

                // changing component profile, so need to reset shared pool
                PoolData = null;
            }
        }



        /// <summary>
        /// True if object should be created with world limit component.  This property is
        /// false by default.
        /// </summary>
        public bool CreateWithWorldLimit
        {
            get { return TestFlag((TorqueObjectFlags)T2DFlags.CreateWithWorldLimitFlag); }
            set
            {
                if (IsRegistered || value == TestFlag((TorqueObjectFlags)T2DFlags.CreateWithWorldLimitFlag))
                    return;
                _SetFlag((TorqueObjectFlags)T2DFlags.CreateWithWorldLimitFlag, value);

                // changing component profile, so need to reset shared pool
                PoolData = null;
            }
        }



        /// <summary>
        /// True if object should be created with link points component.  This property is
        /// false by default.
        /// </summary>
        public bool CreateWithLinkPoints
        {
            get { return TestFlag((TorqueObjectFlags)T2DFlags.CreateWithLinkPointsFlag); }
            set
            {
                if (IsRegistered || value == TestFlag((TorqueObjectFlags)T2DFlags.CreateWithLinkPointsFlag))
                    return;
                _SetFlag((TorqueObjectFlags)T2DFlags.CreateWithLinkPointsFlag, value);

                // changing component profile, so need to reset shared pool
                PoolData = null;
            }
        }

        #endregion


        #region Public methods

        public override bool OnRegister()
        {
            // if they specified that certain components should exist ahead of time then add them now
            _AddStockComponents();

            // look these up first because after IsRegistered flag is set we rely on cached version
            if (HasComponents)
            {
                _physics = Components.FindComponent<T2DPhysicsComponent>();
                _collision = Components.FindComponent<T2DCollisionComponent>();
                _worldLimit = Components.FindComponent<T2DWorldLimitComponent>();
            }

            // try to make sure we have a scenegraph before we call base and register our components
            if (_sceneGraph == null)
                _sceneGraph = TorqueObjectDatabase.Instance.FindObject<T2DSceneGraph>();

            if (!base.OnRegister())
                return false;

            // if we are mounted on a template object, it is an error (cannot register an object that is mounted to a template)
            bool badMount = MountedTo != null && MountedTo.IsTemplate;
            Assert.Fatal(!badMount, "Cannot register an object that is mounted to a template");
            if (badMount)
                return false;

            // make sure interpolation is all reset and mounts are in the right location
            WarpToPosition(_position, _rotation);

            IsSpatialDirty = true;

            // Register mounted objects...we'll be lax and assume that an already registered
            // object is not an error and skip the mount.  This lets people mount things in
            // initComponent so long as they register them first.
            foreach (T2DSceneObject mounted in _MountItr())
                if (!mounted.IsRegistered)
                    Manager.Register(mounted);

            if (IsMounted && Physics == null)
                // updating mount is our responsibility, we'll need to tick
                SetTicking();

            if (HasComponents)
                // hackneyed scheme for registering interface from base object -- use first component we can find
                RegisterCachedInterface("float", "alpha", Components.GetComponentByIndex(0), _visibilityLevel);

            // try to add us to our scenegraph
            if (_sceneGraph != null)
                _sceneGraph.AddObject(this);

            return true;
        }



        public override void OnUnregister()
        {
            base.OnUnregister();

            // unregister owned objects, clear list of mounted objects
            // Note: unregister/dismount will remove item from list so
            // need to write it this way.
            while (_mountedObjects != null)
            {
                if (_mountedObjects.Val.IsOwnedByMount)
                    TorqueObjectDatabase.Instance.Unregister(_mountedObjects.Val);
                else
                    _mountedObjects.Val.Dismount();
            }

            // be sure to clear out mount from parent.
            Dismount();
            SList<MountTo>.ClearList(ref _mountedTo);

            // remove us from our scenegraph
            // try to remove us from our scenegraph
            if (_sceneGraph != null)
                _sceneGraph.RemoveObject(this);
        }



        public virtual void Render(SceneRenderState srs)
        {
            if (Collision != null && Collision.RenderCollisionBounds)
                Collision.RenderBounds(srs);
        }



        /// <summary>
        /// Registers object with process list to receive tick callbacks if
        /// not already registered.  It is generally not necessary to call this
        /// method because the object will call this method when needed.
        /// </summary>
        public void SetTicking()
        {
            if (!TickStarted)
            {
                ProcessList.Instance.AddTickCallback(this, this, 1.0f);

                // Make sure flags set
                _SetFlag((TorqueObjectFlags)T2DFlags.TickStartedFlag, true);
            }
        }



        /// <summary>
        /// Begin a tick if not already begun.  This method insures that
        /// any changes in position or rotation are recorded as movements
        /// during this tick and not the last tick.  This method should
        /// be called by any component which changes position during a
        /// tick.
        /// </summary>
        public void StartTick()
        {
            if (TickInProgress)
                return;
            if (!TickStarted)
                SetTicking();

            // save tick info
            _preTick = _postTick;

            // Make sure flag set
            _SetFlag((TorqueObjectFlags)T2DFlags.TickInProgressFlag, true);
        }



        public virtual void ProcessTick(Move move, float dt)
        {
            if (IsMounted)
            {
                // mount behaviour is our responsibility
                StartTick();
                if (UseMountForce)
                {
                    Vector2 pos;
                    float rot;
                    GetMountPosition(out pos, out rot);
                    Vector2 dir = pos - Position;
                    if (MountForce * dt < 1.0f)
                        dir *= MountForce * dt;
                    SetPosition(Position + dir, false);
                    SetRotation(rot, false);
                }
                else
                {
                    SnapToMount();
                }
            }

            // finish tick
            _postTick.pos = _position;
            _postTick.rot = _rotation;
            UpdateSpatialData();
            _SetFlag((TorqueObjectFlags)T2DFlags.TickInProgressFlag, false);
        }

        public virtual void InterpolateTick(float k)
        {
            // adjust rotation so that interpolation is between near values
            float rotAdj = 0;
            if (Math.Abs(_preTick.rot - _postTick.rot) > 180)
            {
                if (_preTick.rot > _postTick.rot)
                    rotAdj = -360.0f;
                else
                    rotAdj = 360.0f;
            }

            // now interpolate
            _rotation = ((1.0f - k) * (_preTick.rot + rotAdj) + k * _postTick.rot) % 360.0f;
            _position = (1.0f - k) * _preTick.pos + k * _postTick.pos;
            IsSpatialDirty = true;
            UpdateSpatialData();
        }



        /// <summary>
        /// Updates spatial data held by scene graph if position or rotation has
        /// changed since last time the method was called.
        /// </summary>
        public void UpdateSpatialData()
        {
            if (!IsSpatialDirty)
                return;

            float hx = 0.5f * _size.X;
            float hy = 0.5f * _size.Y;

            Rotation2D rotate = new Rotation2D(MathHelper.ToRadians(_rotation));
            Vector2 x = hx * rotate.X;
            Vector2 y = hy * rotate.Y;

            // do quick rotated box calculation (move to math util?)
            float absxx = Math.Abs(x.X);
            float absyx = Math.Abs(y.X);
            float minx = -absxx - absyx + _position.X;
            float maxx = absxx + absyx + _position.X;
            float absxy = Math.Abs(x.Y);
            float absyy = Math.Abs(y.Y);
            float miny = -absxy - absyy + _position.Y;
            float maxy = absxy + absyy + _position.Y;

            _worldClipRectangle = new RectangleF(minx, miny, maxx - minx, maxy - miny);

            IsSpatialDirty = false;

            if (InSceneContainer)
                _sceneGraph.UpdateObjectT2D(this);
        }



        /// <summary>
        /// Changes position of object optionally updating spatial data.  Use this method 
        /// with updateSpatialdata=false rather than Position property if you have several
        /// updates to make to the object in order to save unneeded calls to UpdateSpatialData.
        /// However, UpdateSpatialData must be called in the end or else the scene container
        /// may have old data.
        /// </summary>
        /// <param name="pos">New position</param>
        /// <param name="updateSpatialdata">True if spatial data should be updated.</param>
        public void SetPosition(Vector2 pos, bool updateSpatialdata)
        {
            _position = pos;
            IsSpatialDirty = true;
            if (updateSpatialdata)
                UpdateSpatialData();
        }



        /// <summary>
        /// Changes rotation of object optionally updating spatial data.  Use this method 
        /// with updateSpatialdata=false rather than Rotation property if you have several
        /// updates to make to the object in order to save unneeded calls to UpdateSpatialData.
        /// However, UpdateSpatialData must be called in the end or else the scene container
        /// may have old data.
        /// </summary>
        /// <param name="rot">New rotation</param>
        /// <param name="updateSpatialdata">True if spatial data should be updated.</param>
        public void SetRotation(float rot, bool updateSpatialdata)
        {
            _rotation = rot;
            IsSpatialDirty = true;
            if (updateSpatialdata)
                UpdateSpatialData();
        }



        /// <summary>
        /// Move scene object and all mounted objects to new position and rotation without
        /// interpolation.  This method should be used if an object is being moved large distances
        /// (e.g., when re-spawning player).  WarpToPosition is called during OnRegister so 
        /// if the Position and Rotation properties are used before the object is registered
        /// there will be no interpolation and mounted objects will be properly moved.
        /// Note: mounted objects normally udpate their position during their own tick, but 
        /// calling WarpToPosition will move them immediately (necessary for certain cases).
        /// </summary>
        /// <param name="pos">New position.</param>
        /// <param name="rot">New rotation.</param>
        public void WarpToPosition(Vector2 pos, float rot)
        {
            // get delta's for force mounted objects
            Vector2 deltaPos = pos - _position;
            float deltaRot = rot - _rotation;

            // update our pos/rot and interpolation
            _position = pos;
            _postTick.pos = _position;
            _rotation = rot;
            _rotation = ((_rotation % 360.0f) + 360.0f) % 360.0f;
            _postTick.rot = _rotation;
            _preTick = _postTick;

            foreach (T2DSceneObject mount in _MountItr())
            {
                if (mount.UseMountForce)
                {
                    mount.SetPosition(mount.Position + deltaPos, false);
                    mount.SetRotation(mount.Rotation + deltaRot, false);
                }
                else
                    mount.SnapToMount();

                // be sure to warp sub-mounts
                mount.WarpToPosition(mount.Position, mount.Rotation);
            }
            IsSpatialDirty = true;
            UpdateSpatialData();
        }



        /// <summary>
        /// Mount this object onto another object.  If a link point name is passed the
        /// object will be mounted to the named link point if it exists.  If the link
        /// point doesn't exist it will be mounted to the object as if no name was provided,
        /// except that it can still be found via GetMountedObject.
        /// </summary>
        /// <param name="mountee">Object to mount onto.</param>
        /// <param name="linkPointName">Name of link point.</param>
        /// <param name="offset">Offset from linkpoint in link space (or object space if no linkpoint).</param>
        /// <param name="rotationOffset">Offset rotation from linkpoint.</param>
        /// <param name="ownedByMount">True if this object should be unregistered when mountee is unregistered.</param>
        public void Mount(T2DSceneObject mountee, string linkPointName, Vector2 offset, float rotationOffset, bool ownedByMount)
        {
            Dismount();

            if (mountee == null)
                return;

            Assert.Fatal(IsRegistered == mountee.IsRegistered, "Mounter and mountee must either both be registered or neither registered.");
            if (IsRegistered != mountee.IsRegistered)
                return;

            if (Physics == null && IsRegistered)
                // updating mount is our responsibility, we'll need to tick
                SetTicking();

            MountTo mountedTo = new MountTo();
            mountedTo.MountedTo = mountee;
            mountedTo.Offset = offset;
            mountedTo.RotationOffset = rotationOffset;
            mountedTo.Name = linkPointName == null ? String.Empty : linkPointName;

            if (mountedTo.Name != String.Empty)
            {
                T2DLinkPointComponent linkPoints = mountee.LinkPoints;
                if (linkPoints != null)
                    linkPoints.GetLinkPoint(linkPointName, out mountedTo.Position, out mountedTo.Rotation);
            }

            if (_mountedTo == null)
                SList<MountTo>.InsertFront(ref _mountedTo, mountedTo);
            _mountedTo.Val = mountedTo;
            _SetFlag((TorqueObjectFlags)T2DFlags.MountedFlag, true);
            IsOwnedByMount = ownedByMount;
            TrackMountRotation = true;
            mountee._AddMountedObject(this);
            mountee._TotalMountedObjects++;
            ProcessList.Instance.SetProcessOrder(mountee, this);
        }


        /// <summary>
        /// Mount this object onto another object.  If a link point name is passed the
        /// object will be mounted to the named link point if it exists.  If the link
        /// point doesn't exist it will be mounted to the object as if no name was provided,
        /// except that it can still be found via GetMountedObject.
        /// </summary>
        /// <param name="mountee">Object to mount onto.</param>
        /// <param name="linkPointName">Name of link point.</param>
        /// <param name="ownedByMount">True if this object should be unregistered when mountee is unregistered.</param>
        public void Mount(T2DSceneObject mountee, string linkPointName, bool ownedByMount)
        {
            Mount(mountee, linkPointName, Vector2.Zero, 0.0f, ownedByMount);
        }


        /// <summary>
        /// Adjusts the mount offset of this object.
        /// </summary>
        /// <param name="offset">Offset from linkpoint in link space.</param>
        /// <param name="rotationOffset">Offset rotation from linkpoint.</param>
        public void MountOffset(Vector2 offset, float rotationOffset)
        {
            if (_mountedTo == null)
                return;

            _mountedTo.Val.Offset = offset;
            _mountedTo.Val.RotationOffset = rotationOffset;
        }


        /// <summary>
        /// Dismount this object from whatever object it is mounted to.  If not mounted then this method will do nothing.
        /// </summary>
        public void Dismount()
        {
            if (!IsMounted)
                return;

            IsOwnedByMount = false;
            if (_mountedTo.Val.MountedTo != null)
            {
                _mountedTo.Val.MountedTo._ClearMountedObject(this);
                _mountedTo.Val.MountedTo._TotalMountedObjects--;
                _mountedTo.Val.MountedTo = null;
            }
            _SetFlag((TorqueObjectFlags)T2DFlags.MountedFlag, false);
            ProcessList.Instance.ClearProcessOrder(this);
        }



        /// <summary>
        /// Finds the object mounted to this object using the given link point name.
        /// </summary>
        /// <param name="name">Name of link point used to mount searched for object.  Wildcards are allowed in name.</param>
        /// <returns>Found object or null if none found.</returns>
        public T2DSceneObject GetMountedObject(String name)
        {
            PatternMatch match = new PatternMatch(name);

            foreach (T2DSceneObject sceneObject in _MountItr())
                if (match.TestMatch(sceneObject._mountedTo.Val.Name))
                    return sceneObject;
            return null;
        }



        /// <summary>
        /// List of objects mounted to this object which match name.  Wildcards can be used in name,
        /// so "*" will return all mounted objects.
        /// </summary>
        /// <param name="name">Name of link point used to mount searched for objects.  Wildcards are allowed in name.</param>
        /// <param name="list">List to add mounted objects to.</param>
        public void GetMountedObjects(String name, List<T2DSceneObject> list)
        {
            PatternMatch match = new PatternMatch(name == String.Empty ? PatternMatch.MatchAll : name);

            foreach (T2DSceneObject sceneObject in _MountItr())
                if (match.TestMatch(sceneObject._mountedTo.Val.Name))
                    list.Add(sceneObject);
        }



        /// <summary>
        /// Return the top level mountee for a given object or null if not mounted.
        /// </summary>
        public T2DSceneObject GetRootMountee()
        {
            if (!IsMounted)
                // special case this so you get null if not mounted to anything
                return null;

            // keep walking till we reach the head of this chain
            T2DSceneObject mountee = this;
            while (mountee.IsMounted)
                mountee = mountee.MountedTo;
            return mountee;
        }



        /// <summary>
        /// Find offset from a link point in world coordinates.  Link point is specified with link point
        /// position and rotation.  Position is in normalized object coordinates (i.e., -1,-1 is lower left,
        /// +1,+1 is upper right).  Rotation is specified in degrees.
        /// </summary>
        /// <param name="linkPos">Position of link point in normalized object coordinates.</param>
        /// <param name="linkRot">Rotation of link point in degrees.</param>
        /// <param name="offset">Offset from link point.  This value will be rotated by link point rotation.</param>
        /// <returns>Position offset from given link point in world space coordinates.</returns>
        public Vector2 GetWorldLinkPosition(Vector2 linkPos, float linkRot, Vector2 offset)
        {
            // rotate mount offset into object space
            if (Math.Abs(linkRot) > Epsilon.Value && offset.LengthSquared() > Epsilon.Value)
            {
                Rotation2D offsetRotate = new Rotation2D(MathHelper.ToRadians(linkRot));
                offset = offsetRotate.Rotate(offset);
            }

            // transform link point from mount space to object space
            Vector2 halfSize = 0.5f * Size;
            if (FlipX)
                halfSize.X *= -1.0f;
            if (FlipY)
                halfSize.Y *= -1.0f;
            Vector2 pos = halfSize * (linkPos + offset);

            // transform from object space to world space
            float rot = Rotation;
            if (Math.Abs(rot) > Epsilon.Value)
            {
                Rotation2D rotate = new Rotation2D(MathHelper.ToRadians(rot));
                pos = rotate.Rotate(pos);
            }
            pos += Position;
            return pos;
        }



        /// <param name="linkPosInterface">TorqueInterface to link position in normalized object coordinates.</param>
        /// <param name="linkRotInterface">TorqueInterface to link rotation in degrees.</param>
        /// <param name="offset">Offset from link point.  This value will be rotated by link point rotation.</param>
        /// <returns>Position offset from given link point in world space coordinates.</returns>
        public Vector2 GetWorldLinkPosition(ValueInterface<Vector2> linkPosInterface, ValueInterface<float> linkRotInterface, Vector2 offset)
        {
            Vector2 linkPos = linkPosInterface != null ? linkPosInterface.Value : Vector2.Zero;
            float linkRot = linkRotInterface != null ? linkRotInterface.Value : 0.0f;
            return GetWorldLinkPosition(linkPos, linkRot, offset);
        }



        /// <summary>
        /// Find world-space rotation of a link point, offset by rotOffset.
        /// </summary>
        /// <param name="linkRot">Rotation of link point (in object space).</param>
        /// <param name="rotOffset">Rotation offset from link point.</param>
        /// <returns>World-space rotation.</returns>
        public float GetWorldLinkRotation(float linkRot, float rotOffset)
        {
            float rot = (Rotation + linkRot + rotOffset) % 360.0f;
            if (rot < 0.0f)
                rot += 360.0f;
            return rot;
        }



        /// <param name="linkRotInterface">TorqueInterface to link rotation in degrees.</param>
        /// <param name="rotOffset">Rotation offset from link point.</param>
        /// <returns>World-space rotation.</returns>
        public float GetWorldLinkRotation(ValueInterface<float> linkRotInterface, float rotOffset)
        {
            float linkRot = linkRotInterface != null ? linkRotInterface.Value : 0.0f;
            return GetWorldLinkRotation(linkRot, rotOffset);
        }



        /// <summary>
        /// Return the position that the object is mounted to.  In the case of force mount
        /// the object moves toward this position.  Otherwise the object snaps to the position.
        /// </summary>
        /// <param name="position">Position of mount.</param>
        /// <param name="rotation">Rotation of mount.</param>
        public void GetMountPosition(out Vector2 position, out float rotation)
        {
            if (!IsMounted)
            {
                Assert.Fatal(false, "Getting mount position of an unmounted object");
                position = Position;
                rotation = Rotation;
                return;
            }

            T2DSceneObject mountedTo = _mountedTo.Val.MountedTo;
            position = mountedTo.GetWorldLinkPosition(_mountedTo.Val.Position, _mountedTo.Val.Rotation, _mountedTo.Val.Offset);
            rotation = TrackMountRotation ? mountedTo.GetWorldLinkRotation(_mountedTo.Val.Rotation, _mountedTo.Val.RotationOffset) : Rotation;
        }



        /// <summary>
        /// Move object to the mount location if it is mounted to another object.
        /// </summary>
        public void SnapToMount()
        {
            if (!IsMounted)
                return;

            Vector2 pos;
            float rot;
            GetMountPosition(out pos, out rot);
            SetPosition(pos, false);
            SetRotation(rot, true);
        }



        public override object Clone()
        {
            if (_IsDisposed)
                return null;

            // before we copy components, add any auto-added components
            _AddStockComponents();

            T2DSceneObject clonedObj = base.Clone() as T2DSceneObject;
            clonedObj._SetFlag((TorqueObjectFlags)T2DFlags.TickStartedFlag, false);

            return clonedObj;
        }



        public override void CopyTo(TorqueObject obj)
        {
            base.CopyTo(obj);

            T2DSceneObject obj2 = (T2DSceneObject)obj;
            obj2.CreateWithPhysics = CreateWithPhysics;
            obj2.CreateWithCollision = CreateWithCollision;
            obj2.CreateWithWorldLimit = CreateWithWorldLimit;
            obj2.CreateWithLinkPoints = CreateWithLinkPoints;
            obj2.Layer = Layer;
            obj2.Visible = Visible;
            obj2.VisibilityLevel = VisibilityLevel;
            obj2.Size = Size;
            obj2.Position = Position;
            obj2.Rotation = Rotation;
            obj2.SortPoint = SortPoint;
            obj2.FlipX = FlipX;
            obj2.FlipY = FlipY;
            obj2.CollisionsEnabled = CollisionsEnabled;
            obj2._collision = null;
            obj2._physics = null;
            obj2._worldLimit = null;
            foreach (T2DSceneObject ourMountedObject in _MountItr())
            {
                T2DSceneObject theirMountedObject = (T2DSceneObject)ourMountedObject.Clone();

                // our owned object is mounted to us, so mount their clone on our copy
                MountTo ourMount = ourMountedObject._mountedTo.Val;
                theirMountedObject.Mount(obj2, ourMount.Name, ourMount.Offset, ourMount.RotationOffset, ourMountedObject.IsOwnedByMount);
                theirMountedObject.TrackMountRotation = ourMountedObject.TrackMountRotation;
                theirMountedObject.MountForce = ourMountedObject.MountForce;
                theirMountedObject.UseMountForce = ourMountedObject.UseMountForce;
                theirMountedObject.InheritMountFlip = ourMountedObject.InheritMountFlip;
                theirMountedObject.InheritMountVisibility = ourMountedObject.InheritMountVisibility;
            }

            obj2.TrackMountRotation = TrackMountRotation;
            obj2.MountForce = MountForce;
            obj2.UseMountForce = UseMountForce;
            obj2.InheritMountFlip = InheritMountFlip;
            obj2.InheritMountVisibility = InheritMountVisibility;
            obj2.SceneGraph = SceneGraph;
        }



        public override void Reset()
        {
            base.Reset();
            _collision = null;
            _physics = null;
            _worldLimit = null;
            SList<T2DSceneObject>.ClearList(ref _mountedObjects);
            SList<MountTo>.ClearList(ref _mountedTo);
        }



        public override void OnLoaded()
        {
            base.OnLoaded();

            // need to do mount processing on load, because if we do it at deserialization time, the link
            // points may not exist yet.
            _ProcessXMLMounts();
        }

        #endregion


        #region Private, protected, internal methods

        protected void _ProcessXMLMounts()
        {
            if (_xmlMounts != null)
            {
                // mount the mounts to us
                foreach (XmlMount m in _xmlMounts)
                {
                    Assert.Fatal(m.Object != null, "mount object loaded from xml has no mounted object reference");
                    if (m.Object == null)
                        continue;

                    // cannot mount an added object to a template
                    bool badMount = IsTemplate && m.Object.IsRegistered;
                    Assert.Fatal(!badMount, "Cannot mount a registered object to a template");
                    if (badMount)
                        continue;

                    m.Object._ProcessXMLMounts();

                    T2DSceneObject obj = m.Object;
                    if (m.Object.IsTemplate && !(m.Object is T2DSpawnObject)) // if the object is a template, mount a clone
                        obj = m.Object.Clone() as T2DSceneObject;
                    else // mounting a non template object, assert if it is already mounted
                        Assert.Fatal(obj.MountedTo == null, "Non-template object is already mounted. Cannot re-mount.");

                    // do mount and set mount specific properties
                    obj.Mount(this, m.LinkPoint, m.Offset, m.RotationOffset, m.OwnedByMount);
                    if (m.SnapToMount)
                        obj.SnapToMount();
                    obj.TrackMountRotation = m.TrackMountRotation;
                    obj.InheritMountFlip = m.InheritMountFlip;
                    obj.InheritMountVisibility = m.InheritMountVisibility;
                    obj.MountForce = m.MountForce;
                    if (m.MountForce > 0.0f)
                        obj.UseMountForce = true;
                }
                _xmlMounts.Clear();
                _xmlMounts = null;
            }
        }



        void _ClearMountedObject(T2DSceneObject obj)
        {
            SList<T2DSceneObject>.Remove(ref _mountedObjects, obj);
        }



        void _AddMountedObject(T2DSceneObject obj)
        {
            // don't own twice
            _ClearMountedObject(obj);
            SList<T2DSceneObject>.InsertFront(ref _mountedObjects, obj);
        }



        MountEnumerator _MountItr()
        {
            return new MountEnumerator(this);
        }



        void _AddStockComponents()
        {
            if (IsRegistered)
                return;

            if (CreateWithPhysics && Physics == null)
                Components.AddComponent(new T2DPhysicsComponent());
            if (CreateWithCollision)
            {
                if (Collision == null)
                    Components.AddComponent(new T2DCollisionComponent());
                if (Collision.Images.Count == 0)
                    Collision.InstallImage(new T2DPolyImage());
            }
            if (CreateWithWorldLimit && WorldLimit == null)
                Components.AddComponent(new T2DWorldLimitComponent());
            if (CreateWithLinkPoints && LinkPoints == null)
                Components.AddComponent(new T2DLinkPointComponent());
        }

        #endregion


        #region Private, protected, internal fields

        [Flags]
        enum T2DFlags
        {
            FirstFlag = TorqueObject.TorqueObjectFlags.LastObjectFlag << 1,
            VisibleFlag = FirstFlag << 0,
            CollisionsEnabledFlag = FirstFlag << 1,
            SpatialDirtyFlag = FirstFlag << 2,
            FlipXFlag = FirstFlag << 3,
            FlipYFlag = FirstFlag << 4,
            MountedFlag = FirstFlag << 5,
            OwnedByMountFlag = FirstFlag << 6,
            TrackMountRotationFlag = FirstFlag << 7,
            UseMountForceFlag = FirstFlag << 8,
            InheritMountFlipFlag = FirstFlag << 9,
            InheritMountVisibilityFlag = FirstFlag << 10,
            TickStartedFlag = FirstFlag << 11,
            TickInProgressFlag = FirstFlag << 12,
            CreateWithPhysicsFlag = FirstFlag << 13,
            CreateWithCollisionFlag = FirstFlag << 14,
            CreateWithWorldLimitFlag = FirstFlag << 15,
            CreateWithLinkPointsFlag = FirstFlag << 16,
            LastFlag = FirstFlag << 16
        }



        protected struct MountTo
        {
            public T2DSceneObject MountedTo;
            public Vector2 Offset;
            public float RotationOffset;
            public String Name;
            public ValueInterface<Vector2> Position;
            public ValueInterface<float> Rotation;
            public float MountForce;
        }



        protected struct TickHelper
        {
            public Vector2 pos;
            public float rot;
        }

        [TorqueXmlSchemaType(Name = "Mount")]
        internal class XmlMount
        {
            public T2DSceneObject Object = null;
            public string LinkPoint = string.Empty;
            public Vector2 Offset = Vector2.Zero;
            public float RotationOffset = 0.0f;
            public bool OwnedByMount = false;
            public bool SnapToMount = false;
            public bool TrackMountRotation = false;
            public bool InheritMountFlip = true;
            public bool InheritMountVisibility = true;
            public float MountForce = 0.0f;
        }

        protected TickHelper _preTick;
        protected TickHelper _postTick;

        protected Vector2 _position;
        protected float _rotation;
        protected ValueInPlaceInterface<float> _visibilityLevel = new ValueInPlaceInterface<float>(1.0f);

        protected Vector2 _sortPoint;

        protected T2DSceneGraph _sceneGraph;
        internal int _layer;
        protected float _layerDepth;
        protected Vector2 _size;

        protected RectangleF _worldClipRectangle;
        protected SceneContainerData _sceneContainerData = new SceneContainerData();

        SList<T2DSceneObject> _mountedObjects;
        SList<MountTo> _mountedTo; // not really a list, using SList for gc purposes
        int _TotalMountedObjects = 0;

        T2DPhysicsComponent _physics;
        T2DCollisionComponent _collision;
        T2DWorldLimitComponent _worldLimit;

        internal List<XmlMount> _xmlMounts;

        #endregion

        #region IDisposable Members

        public override void Dispose()
        {
            _IsDisposed = true;
            if (this.Components != null)
            {
                foreach (TorqueComponent tc in this.Components)
                {
                    tc.Dispose();
                }
            }
            if (this.LinkPoints != null)
                this.LinkPoints.Dispose();
            this.OnRegistered = null;
            this._ResetRefs();
            base.Dispose();
        }

        #endregion
    }
}
