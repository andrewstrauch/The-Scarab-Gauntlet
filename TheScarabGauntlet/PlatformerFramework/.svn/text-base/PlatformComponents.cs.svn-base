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
using GarageGames.Torque.Sim;
using GarageGames.Torque.T2D;
using GarageGames.Torque.XNA;

namespace GarageGames.Torque.PlatformerFramework
{
    /// <summary>
    /// Base platform functionality. Actor will always collide with this type of platform, regardless of the angle of collision.
    /// For one-way platforms use PlatformComponent.
    /// </summary>
    [TorqueXmlSchemaType]
    [TorqueXmlSchemaDependency(Type = typeof(T2DCollisionComponent))]
    public class SolidPlatformComponent : TorqueComponent
    {
        //======================================================
        #region Public properties, operators, constants, and enums

        /// <summary>
        /// The scene object owner of this platform component.
        /// </summary>
        public T2DSceneObject SceneObject
        {
            get { return _platform; }
        }

        /// <summary>
        /// Specifies whether or not the platform is currently the GroundObject for an Actor.
        /// </summary>
        public bool PlatformInUse
        {
            get { return _actorsCarrying.Count > 0; }
        }

        /// <summary>
        /// Specifies whether or not the platform will currently allow Actors to land on it.
        /// If used on solid platforms, the top surface of the platform will not be considered ground and the Actor will fall across it.
        /// Can cause problems if the top surface is flat!
        /// </summary>
        [XmlIgnore]
        [TorqueCloneIgnore]
        public bool PlatformEnabled
        {
            get { return _enabled; }
            set { _enabled = value; }
        }

        /// <summary>
        /// The lateral force across the surface of the platform that an Actor should be pushed when standing on this platform.
        /// This is used for conveyor effects.
        /// </summary>
        public float SurfaceForce
        {
            get { return _force; }
            set { _force = value; }
        }

        /// <summary>
        /// Specifies the friction on the platform. This is pulled directly off the physics material on the owner scene object if
        /// the platform is using either the NormalSurfacePhysMat or the SlipperySurfacePhysMat. Otherwise, this property will return
        /// 1, which is "normal" friction according to Actor physics.
        /// </summary>
        public float PlatformFriction
        {
            get { return MathHelper.Clamp(_platform.Collision.CollisionMaterial.Friction / 0.3f, 0, 1); }
        }

        /// <summary>
        /// A list of all actors currently using this platform as their ground object;
        /// </summary>
        public List<ActorComponent> ActorsCarrying
        {
            get { return _actorsCarrying; }
        }

        #endregion

        //======================================================
        #region Public methods

        /// <summary>
        /// This is called whenever an Actor begins using this platform as its GroundObject.
        /// </summary>
        /// <param name="actor">The Actor that landed on this platform.</param>
        public virtual void ActorLanded(ActorComponent actor)
        {
            foreach (TorqueComponent comp in _platform.Components)
                if (comp as PlatformBehavior != null)
                    (comp as PlatformBehavior).ActorLanded(actor);

            if (!_actorsCarrying.Contains(actor))
                _actorsCarrying.Add(actor);
        }

        /// <summary>
        /// This is called when an Actor stops using this platform as its GroundObject.
        /// </summary>
        /// <param name="actor">The actor that left this platform.</param>
        public virtual void ActorLeft(ActorComponent actor)
        {
            foreach (TorqueComponent comp in _platform.Components)
                if (comp as PlatformBehavior != null)
                    (comp as PlatformBehavior).ActorLeft(actor);

            if (_actorsCarrying.Contains(actor))
                _actorsCarrying.Remove(actor);
        }

        /// <summary>
        /// Call this if you wish to notify each Actor this platform is supporting that the it has changed position. This should be
        /// used only when directly setting the position of this platform.
        /// </summary>
        public virtual void PlatformWarped()
        {
            foreach (ActorComponent actor in _actorsCarrying)
                actor.WarpToCurrentPlatform();
        }

        public override void CopyTo(TorqueComponent obj)
        {
            base.CopyTo(obj);
            SolidPlatformComponent obj2 = obj as SolidPlatformComponent;

            obj2.SurfaceForce = SurfaceForce;
        }

        #endregion

        //======================================================
        #region Private, protected, internal methods

        protected override bool _OnRegister(TorqueObject owner)
        {
            if (!base._OnRegister(owner))
                return false;

            // store the owner as a member var
            _platform = owner as T2DSceneObject;

            // assert existence of collision and physics components
            Assert.Fatal(_platform.Components.FindComponent<T2DCollisionComponent>() != null, "Platforms must have Collision Components");

            // force the neccesary component configuration
            _platform.CollisionsEnabled = true;
            _platform.Collision.SolveOverlap = true;
            _platform.Collision.CollidesWith = PlatformerData.PlatformTriggerObjectType;
            _platform.SetObjectType(PlatformerData.PlatformObjectType, true);

            if (_platform.Physics != null)
                _platform.Physics.InverseMass = 0;

            if (SceneObject.Collision.CollisionMaterial == null)
                SceneObject.Collision.CollisionMaterial = PlatformerData.NormalSurfacePhysMat;

            return true;
        }

        #endregion

        //======================================================
        #region Private, protected, internal fields

        // owner
        protected T2DSceneObject _platform;

        // physical properties
        protected bool _enabled = true;
        protected float _force;
        protected List<ActorComponent> _actorsCarrying = new List<ActorComponent>();

        #endregion
    }

    /// <summary>
    /// The standard one-way platform. This is derived from SolidPlatformComponent and should be used when a one-way platform is needed.
    /// If a solid object is needed, use SolidPlatformComponent. To tweak the highest slope an Actor can stand on, adjust MaxGroundNormalY
    /// on ActorComponent.
    /// </summary>
    [TorqueXmlSchemaType]
    public class OneWayPlatformComponent : SolidPlatformComponent
    {
        //======================================================
        #region Public properties, operators, constants, and enums

        /// <summary>
        /// Specifies whether or not the platform is currently active and allowing collisions. This is normally set automatically
        /// as actors pass over platforms.
        /// </summary>
        [XmlIgnore]
        [TorqueCloneIgnore]
        public bool PlatformActive
        {
            get { return _active; }
            set { _active = value; }
        }

        /// <summary>
        /// Specifies whether or not an Actor will be able to climb down through this platform. Actors can always climb up through
        /// platforms of this type.
        /// </summary>
        public bool CanClimbThrough
        {
            get { return _canClimbThrough; }
            set { _canClimbThrough = value; }
        }

        #endregion

        //======================================================
        #region Public methods

        /// <summary>
        /// Collision system calls this to optionally early-out of collisions with an object based on the EarlyOutObjectType on the 
        /// T2DCollisionComponent, which we set to ActorObjectType in _OnRegister. By default, platforms do not collide with anything
        /// actively, they are only collided against, so unless that is changed this code will only be called on if CollidesWith is 
        /// modified externally after this component is initialized.
        /// </summary>
        /// <param name="ourObject">The scene object that owns this component.</param>
        /// <param name="theirObject">The scene object that owns the ActorComponent.</param>
        /// <returns>True if a collision should occur. False otherwise.</returns>
        public bool TestEarlyOut(T2DSceneObject ourObject, T2DSceneObject theirObject)
        {
            // check if the object is an Actor
            if (theirObject.TestObjectType(PlatformerData.ActorObjectType))
            {
                // if this platform is in use, don't collide
                if (_active)
                    return false;
                else
                    return true;
            }

            // default to no collision
            return false;
        }

        public override void CopyTo(TorqueComponent obj)
        {
            base.CopyTo(obj);

            OneWayPlatformComponent obj2 = obj as OneWayPlatformComponent;

            obj2.CanClimbThrough = CanClimbThrough;
        }

        #endregion

        //======================================================
        #region Private, protected, internal methods

        protected override bool _OnRegister(TorqueObject owner)
        {
            if (!base._OnRegister(owner))
                return false;

            // set the object type properly so Actors can decide when to collide
            _platform.SetObjectType(PlatformerData.OneWayPlatformObjectType, true);

            _platform.Collision.EarlyOutObjectType = PlatformerData.ActorObjectType;

            // set the early out delegate for the owner
            _platform.Collision.TestEarlyOut = TestEarlyOut;

            return true;
        }

        #endregion

        //======================================================
        #region Private, protected, internal fields

        /// <summary>
        /// Specifies whether or not the platform is currently active and allowing collisions.
        /// </summary>
        protected bool _active = false;

        /// <summary>
        /// Specifies whether or not an Actor will be able to climb down through this platform. Actors can always climb up through
        /// platforms of this type.
        /// </summary>
        protected bool _canClimbThrough = false;

        #endregion
    }

    /// <summary>
    /// Base PlatformBehavior class. Derive from this class to add contained functionality to a platform. See the FallingPlatformBehavior 
    /// in the PlatformerDemo for an example of how to use this.
    /// </summary>
    public abstract class PlatformBehavior : TorqueComponent
    {
        //======================================================
        #region Public properties, operators, constants, and enums

        /// <summary>
        /// The platform component on the owner scene object that this behavior will interface with.
        /// </summary>
        [XmlIgnore]
        [TorqueCloneIgnore]
        public SolidPlatformComponent RootComponent
        {
            get { return _platformComponent; }
            set { _platformComponent = value; }
        }

        #endregion

        //======================================================
        #region Public methods

        /// <summary>
        /// This is called whenever an Actor begins using this platform as its GroundObject.
        /// </summary>
        /// <param name="actor">The Actor that landed on this platform.</param>
        public virtual void ActorLanded(ActorComponent actor) { }

        /// <summary>
        /// This is called whenever an Actor stops using this platform as its GroundObject.
        /// </summary>
        /// <param name="actor">The Actor that left on this platform.</param>
        public virtual void ActorLeft(ActorComponent actor) { }

        #endregion

        //======================================================
        #region Private, protected, internal methods

        protected override bool _OnRegister(TorqueObject owner)
        {
            if (!base._OnRegister(owner))
                return false;

            // keep track of the owning scene object
            _platform = owner as T2DSceneObject;

            // grab a reference to the owner's platform component
            _platformComponent = owner.Components.FindComponent<SolidPlatformComponent>();

            Assert.Fatal(_platform != null && _platformComponent != null, "PlatformBehavior - Invalid owner or missing PlatformComponent on owner.");

            return true;
        }

        #endregion

        //======================================================
        #region Private, protected, internal fields

        // owner and platform component
        protected T2DSceneObject _platform;
        protected SolidPlatformComponent _platformComponent;

        #endregion
    }

    /// <summary>
    /// A basic move component for moving platforms. Allows you to specify a path for the platform to travel.
    /// </summary>
    [TorqueXmlSchemaType]
    public class PlatformMoveComponent : MoveComponent
    {
        /// <summary>
        /// A single node on a path used by a PlatformMoveComponent.
        /// </summary>
        public class PlatformPathNode
        {
            //======================================================
            #region Public properties, operators, constants, and enums

            /// <summary>
            /// The position of this node.
            /// </summary>
            public Vector2 Position
            {
                get { return _position; }
                set { _position = value; }
            }

            /// <summary>
            /// Specifies whether or not the position of this node should be interpreted as relative to the previous node, 
            /// or absolute in world coordinates.
            /// </summary>
            [TorqueXmlSchemaType(DefaultValue = "1")]
            public bool RelativeToPrevious
            {
                get { return _relativeToPrevious; }
                set { _relativeToPrevious = value; }
            }

            /// <summary>
            /// Specifies the time in secods it takes this platform to get from its previous path node to this one.
            /// </summary>
            [TorqueXmlSchemaType(DefaultValue = "5")]
            public float TimeToDestination
            {
                get { return _timeToDestination; }
                set { _timeToDestination = value; }
            }

            #endregion

            //======================================================
            #region Private, protected, internal fields

            private Vector2 _position;
            private bool _relativeToPrevious;
            private float _timeToDestination;

            #endregion
        }

        //======================================================
        #region Public properties, operators, constants, and enums

        /// <summary>
        /// The path along which this platform will travel.
        /// </summary>
        [TorqueCloneIgnore]
        public List<PlatformPathNode> Path
        {
            get { return _path; }
            set { _path = value; }
        }

        /// <summary>
        /// Specifies whether or not this platform should start when it is registered. If set to false, Start must be called
        /// at some point to make the platform move.
        /// </summary>
        [TorqueXmlSchemaType(DefaultValue = "1")]
        public bool RunOnInit
        {
            get { return _runOnInit; }
            set { _runOnInit = value; }
        }

        /// <summary>
        /// Specifies whether the platform should repeat along the path when it reaches the end.
        /// </summary>
        [TorqueXmlSchemaType(DefaultValue = "1")]
        public bool Loop
        {
            get { return _loop; }
            set { _loop = value; }
        }

        /// <summary>
        /// Specifies whether or not this platform is currently running, or whatever.
        /// </summary>
        public bool IsRunning
        {
            get { return _running; }
        }

        #endregion

        //======================================================
        #region Public methods

        /// <summary>
        /// Start the platform moving from its start position to the first node on its path.
        /// </summary>
        public void Start()
        {
            if (_path.Count == 0)
                return;

            _sceneObject.Position = _startPosition;
            _running = true;
            _currentPathNode = 0;
            _destination = _startPosition;
            _travelToCurrentNode();

            if (!_sceneObject.TestObjectType(PlatformerData.PlatformObjectType))
                return;

            SolidPlatformComponent platComp = _sceneObject.Components.FindComponent<SolidPlatformComponent>();

            if (platComp == null)
                return;

            if (platComp.PlatformInUse)
                platComp.PlatformWarped();
        }

        public override void CopyTo(TorqueComponent obj)
        {
            base.CopyTo(obj);

            PlatformMoveComponent obj2 = obj as PlatformMoveComponent;

            obj2.Path = new List<PlatformPathNode>(Path);
            obj2.Loop = Loop;
            obj2.RunOnInit = RunOnInit;
        }

        #endregion

        //======================================================
        #region Private, protected, internal methods

        protected override void _preUpdate(float elapsed)
        {
            if (!_running)
                return;

            if (Vector2.Distance(_destination, _sceneObject.Position) < _sceneObject.Physics.Velocity.Length() * elapsed)
                _arrived = true;
        }

        protected override void _postUpdate(float elapsed)
        {
            if (_arrived)
                _reachedNode();
        }

        /// <summary>
        /// Sets the velocity of the platform towards the current target node on the path.
        /// </summary>
        protected void _travelToCurrentNode()
        {
            _arrived = false;
            _source = _destination;
            _destination = _currentNodePosition();
            _sceneObject.Physics.Velocity = (_destination - _source) / _path[_currentPathNode].TimeToDestination;
        }

        /// <summary>
        /// Returns the current target node position.
        /// </summary>
        /// <returns>The current target node position in world coordinates.</returns>
        protected Vector2 _currentNodePosition()
        {
            if (_currentPathNode >= _path.Count)
                return Vector2.Zero;

            return _path[_currentPathNode].RelativeToPrevious ? _source + _path[_currentPathNode].Position : _path[_currentPathNode].Position;
        }

        /// <summary>
        /// Swaps out current and previous nodes and loops the path, if specified.
        /// </summary>
        protected virtual void _reachedNode()
        {
            _sceneObject.Physics.Velocity = Vector2.Zero;
            _sceneObject.Position = _destination;

            if (_currentPathNode < _path.Count - 1)
            {
                _currentPathNode++;
                _travelToCurrentNode();
            }
            else if (_loop && _currentPathNode >= _path.Count - 1)
            {
                _currentPathNode = 0;
                _travelToCurrentNode();
            }
            else
            {
                _running = false;
            }

            if (!_sceneObject.TestObjectType(PlatformerData.PlatformObjectType))
                return;

            SolidPlatformComponent platComp = _sceneObject.Components.FindComponent<SolidPlatformComponent>();

            if (platComp == null)
                return;

            if (platComp.PlatformInUse)
                platComp.PlatformWarped();
        }

        protected override bool _OnRegister(TorqueObject owner)
        {
            if (!base._OnRegister(owner))
                return false;

            // record the starting position
            _startPosition = _sceneObject.Position;
            _sceneObject.Physics.Velocity = Vector2.Zero;

            // start up the platform if it's ready
            if (_runOnInit)
                Start();

            return true;
        }

        #endregion

        //======================================================
        #region Private, protected, internal fields

        private List<PlatformPathNode> _path = new List<PlatformPathNode>();
        private Vector2 _source = Vector2.Zero;
        private Vector2 _destination = Vector2.Zero;
        private Vector2 _startPosition = Vector2.Zero;
        private int _currentPathNode;
        private bool _running;
        private bool _arrived;

        private bool _runOnInit = true;
        private bool _loop = true;

        #endregion
    }
}
