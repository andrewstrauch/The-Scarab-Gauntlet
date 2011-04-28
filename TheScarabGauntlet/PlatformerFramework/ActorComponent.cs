//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using System.Xml.Serialization;

using Microsoft.Xna.Framework;

using GarageGames.Torque.Core;
using GarageGames.Torque.SceneGraph;
using GarageGames.Torque.Sim;
using GarageGames.Torque.T2D;
using GarageGames.Torque.XNA;
using GarageGames.Torque.MathUtil;

namespace GarageGames.Torque.PlatformerFramework
{
    /// <summary>
    /// The sole purpose of this component is to get rotated Actors to work. The idea is that instead using an ActorComponent on an animated sprite,
    /// as you normally would, you can attach the ActorComponent to a blank scene object, and then mount an animated sprite to that scene object. That
    /// way you can rotate the animated sprite without mussing up the collision image or physics of the actor. The ActorPuppetComponent exists to allow
    /// the ActorComponent on the scene object to manipulate the animations to be played on the animated sprite thats mounted to the scene object. So
    /// to sum up: an ActorPuppetComponent should only ever be used on a T2DAnimatedSprite that's mounted to a T2DSceneObject that has an ActorComponent 
    /// (or some component derived from ActorComponent), and only when you wish to rotate your actors.
    /// </summary>
    [TorqueXmlSchemaType]
    public class ActorPuppetComponent : TorqueComponent
    {
        //======================================================
        #region Public properties, operators, constants, and enums

        /// <summary>
        /// Specifies whether or not the sprite should appear to rotate to the ground surface the actor is standing on..
        /// </summary>
        [TorqueXmlSchemaType(DefaultValue = "1")]
        public bool RotateToGroundSurface
        {
            get { return _rotateToGroundSurface; }
            set { _rotateToGroundSurface = value; }
        }

        /// <summary>
        /// The rate at which the sprite should rotate the animated sprite to the angle of the ground surface it's standing on. If zero, 
        /// rotation will be instantaneous. If no rotation is desired, set RotateToGroundSurface to zero.
        /// </summary>
        [TorqueXmlSchemaType(DefaultValue = "685")]
        public float RateOfRotation
        {
            get { return _rateOfRotation; }
            set { _rateOfRotation = value; }
        }

        /// <summary>
        /// Use this to control the rotation of the puppet.
        /// </summary>
        [XmlIgnore]
        public float PuppetRotation
        {
            get { return _pivotObject != null ? _pivotObject.Rotation : 0; }
            set
            {
                // set the rotation of the pivot object
                if (_pivotObject != null)
                    _pivotObject.Rotation = value;
            }
        }

        /// <summary>
        /// An optional additional offset around which to rotate the animated sprite. This offset is in local coordinates of the scene object that the
        /// animated sprite is mounted to.
        /// </summary>
        public Vector2 ActorPivotOffset
        {
            get { return _actorPivotOffset; }
            set { _actorPivotOffset = value; }
        }

        /// <summary>
        /// The ActorComponent that is controlling this ActorPuppetComponent.
        /// </summary>
        public ActorComponent Master
        {
            get { return _master; }
        }

        #endregion

        //======================================================
        #region Public methods

        /// <summary>
        /// This warps the animated sprite into position to avoid any noticeable interpolation. It is currently used only when the scene object changes
        /// directions because when it flips the mount offset can potentially change quite a bit in a single frame and the position of the animated
        /// sprite should be updated instantly.
        /// </summary>
        public void RefreshSpritePosition()
        {
            // manually refresh exact position of sprite
            // (called when flip occurs to avoid interpolation to flipped mount offset)
            if (_pivotObject != null)
                _pivotObject.WarpToPosition(_pivotObject.Position, _pivotObject.Rotation);
        }

        public override void CopyTo(TorqueComponent obj)
        {
            base.CopyTo(obj);

            ActorPuppetComponent obj2 = obj as ActorPuppetComponent;

            obj2.ActorPivotOffset = ActorPivotOffset;
            obj2.PuppetRotation = PuppetRotation;
            obj2.RotateToGroundSurface = RotateToGroundSurface;
            obj2.RateOfRotation = RateOfRotation;
        }

        #endregion

        //======================================================
        #region Private, protected, internal methods

        protected override bool _OnRegister(TorqueObject owner)
        {
            if (!base._OnRegister(owner))
                return false;

            // record our scene object
            _sceneObject = owner as T2DAnimatedSprite;

            // make sure this component is being used properly
            // (this might be too strict, but better safe than sorry)
            if (_sceneObject == null || _sceneObject.MountedTo == null)
            {
                Assert.Fatal(false, "ActorPuppetComponent must be used on a T2DAnimatedSprite that's mounted to a T2DSceneObject that has an ActorComponent. \n\nThe owner of the ActorPuppetComponent will be deleted if this message is ignored.");
                owner.MarkForDelete = true;
                return false;
            }

            // find the actor component of the object we're mounted to
            if (_sceneObject.MountedTo.TestObjectType(PlatformerData.ActorObjectType))
                _master = _sceneObject.MountedTo.Components.FindComponent<ActorComponent>();

            // make sure there is a valid actor
            if (_master == null)
            {
                Assert.Fatal(false, "ActorPuppetComponent must be used on a T2DAnimatedSprite that's mounted to a T2DSceneObject that has an ActorComponent. \n\nThe owner of the ActorPuppetComponent will be deleted if this message is ignored.");
                owner.MarkForDelete = true;
                return false;
            }

            // create the dummy pivot object
            _pivotObject = new T2DSceneObject();
            _pivotObject.Size = _master.Actor.Size;
            _pivotObject.Position = _master.Actor.Position;
            _pivotObject.CreateWithLinkPoints = true;
            TorqueObjectDatabase.Instance.Register(_pivotObject);

            // get the current offset of the mounted puppet sprite
            _sceneObject.SnapToMount();
            Vector2 offset = (_sceneObject.Position - _master.Actor.Position) / (_master.Actor.Size / 2);

            // account for flipped objects
            // (when the mount position is updated it will reverse this if it was supposed to be flipped)
            if (_master.Actor.FlipX)
                offset.X = -offset.X;

            if (_master.Actor.FlipY)
                offset.Y = -offset.Y;

            // do the big switcheroo
            // (mount pivot object offset by actorPivotOffset to the center of the master actor 
            // scene object and offset by actorPivotOffset)
            _pivotObject.LinkPoints.AddLinkPoint("SpriteOffset", offset, 0);
            _master.Actor.LinkPoints.AddLinkPoint("Center", Vector2.Zero, 0);

            _pivotObject.Mount(_master.Actor, "Center", _actorPivotOffset, 0, true);
            _pivotObject.SnapToMount();

            _sceneObject.Dismount();
            _sceneObject.Mount(_pivotObject, "SpriteOffset", -_actorPivotOffset, 0, true);
            _sceneObject.SnapToMount();

            // make sure we have the correct mounting settings
            // (it's only actually neccesary that TrackMountRotation be false if the 
            // RotateToGroundSurface is enabled.
            // the other field values just make good sense)
            _sceneObject.TrackMountRotation = true;
            _sceneObject.IsOwnedByMount = true;
            _sceneObject.InheritMountFlip = true;
            _sceneObject.InheritMountVisibility = true;
            _sceneObject.UseMountForce = false;

            _pivotObject.TrackMountRotation = false;
            _pivotObject.IsOwnedByMount = true;
            _pivotObject.InheritMountFlip = true;
            _pivotObject.InheritMountVisibility = true;
            _pivotObject.UseMountForce = false;

            // set the master's AnimatedSprite property to this object
            // (note: it's possible that this overwrites another ActorPuppet on the master 
            // object. as to why you would ever need two animated sprite puppets on the 
            // same actor, i have no clue)
            _master.AnimatedSprite = _sceneObject;
            _master.ActorPuppet = this;

            // set the proper object type of this component's owner
            _sceneObject.SetObjectType(PlatformerData.ActorObjectType, true);

            return true;
        }

        #endregion

        //======================================================
        #region Private, protected, internal fields

        private T2DAnimatedSprite _sceneObject;
        private T2DSceneObject _pivotObject;
        private ActorComponent _master;
        private Vector2 _actorPivotOffset = Vector2.Zero;
        protected bool _rotateToGroundSurface = true;
        protected float _rateOfRotation = 685.0f;

        #endregion
    }

    /// <summary>
    /// The core class of the PlatformerFramework. An Actor is anything that wants to at some point interact with a platform.
    /// Normally, an ActorComponent is just added to an animated sprite. In the case that the Actor should rotate, you will need
    /// to implement an ActorPuppetComponent. To do that, the ActorComponent should be added to a blank scene object and set up 
    /// exactly the same and the ActorPuppetComponent should go on an animated sprite that is mounted to the ActorComponent. 
    /// </summary>
    [TorqueXmlSchemaType]
    [TorqueXmlSchemaDependency(Type = typeof(T2DPhysicsComponent))]
    [TorqueXmlSchemaDependency(Type = typeof(T2DCollisionComponent))]
    public partial class ActorComponent : MoveComponent, IFSMObject
    {
        //======================================================
        #region Constructors

        /// <summary>
        /// Constructor. Calls several methods that set up various different things for the ActorComponent. Namely, initializes the ground
        /// query data, creates a T2DPolyImage for the ground check to use, and calls the methods that register physics states and create
        /// the animation manager for this ActorComponent.
        /// </summary>
        public ActorComponent()
        {
            // init ground pick query data
            _initGroundQueryData();

            // create ground check poly image
            _groundPolyImage = new T2DPolyImage();

            // register physics states
            // (the states this registers define how the actor moves)
            _registerPhysicsStates();

            // create animation manager to handle this actor's animation states
            _createAnimationManager();
        }

        #endregion

        //======================================================
        #region Public properties, operators, constants, and enums

        /// <summary>
        /// The scene object that this ActorComponent is on.
        /// </summary>
        public T2DSceneObject Actor
        {
            get { return _actor; }
        }

        /// <summary>
        /// The animated sprite that this ActorComponent is controlling.
        /// </summary>
        [TorqueCloneIgnore]
        [XmlIgnore]
        public T2DAnimatedSprite AnimatedSprite
        {
            get { return _animatedSprite; }
            set { _animatedSprite = value; }
        }

        [TorqueCloneIgnore]
        [XmlIgnore]
        public ActorPuppetComponent ActorPuppet
        {
            get { return _puppet; }
            set { _puppet = value; }
        }


        /// <summary>
        /// The maximum velocity that this Actor will be allowed to travel at any time.
        /// </summary>
        [TorqueXmlSchemaType(DefaultValue = "200 200")]
        public Vector2 MaxVelocity
        {
            get { return _maxVelocity; }
            set { _maxVelocity = value; }
        }

        /// <summary>
        /// A vector that represents the current movement input that this Actor's movement will be based on. Both X and Y values
        /// range from 0 to 1, but this is not a normalized vector.
        /// </summary>
        public Vector2 InputVector
        {
            get
            {
                Vector2 dir = Vector2.Zero;

                if (_moveLeft)
                    dir.X = -1;
                else if (_moveRight)
                    dir.X = 1;

                if (_moveUp)
                    dir.Y = -1;
                else if (_moveDown)
                    dir.Y = 1;

                return dir;
            }
        }

        /// <summary>
        /// The rate at which this Actor accelerates due to gravity.
        /// </summary>
        [TorqueXmlSchemaType(DefaultValue = "3")]
        public float Gravity
        {
            get { return _gravity; }
            set { _gravity = value; }
        }

        /// <summary>
        /// The maximum speed this Actor can run across a surface.
        /// </summary>
        [TorqueXmlSchemaType(DefaultValue = "65")]
        public float MaxMoveSpeed
        {
            get { return _maxMoveSpeed; }
            set { _maxMoveSpeed = value; }
        }

        /// <summary>
        /// The rate at which this Actor accelerates across a surface with normal friction.
        /// </summary>
        [TorqueXmlSchemaType(DefaultValue = "3")]
        public float GroundAccel
        {
            get { return _groundAccel; }
            set { _groundAccel = value; }
        }

        /// <summary>
        /// The rate at which this Actor slows to a stop on a surface with normal friction.
        /// </summary>
        [TorqueXmlSchemaType(DefaultValue = "5")]
        public float GroundDecel
        {
            get { return _groundDecel; }
            set { _groundDecel = value; }
        }

        /// <summary>
        /// The rate at which this Actor accelerates while in the air.
        /// </summary>
        [TorqueXmlSchemaType(DefaultValue = "2")]
        public float AirAccel
        {
            get { return _airAccel; }
            set { _airAccel = value; }
        }

        /// <summary>
        /// The rate at which this Actor slows to a stop while in the air.
        /// </summary>
        [TorqueXmlSchemaType(DefaultValue = "0.5")]
        public float AirDecel
        {
            get { return _airDecel; }
            set { _airDecel = value; }
        }

        /// <summary>
        /// The magnitude of the force applied to this Actor when it jumps.
        /// </summary>
        [TorqueXmlSchemaType(DefaultValue = "135")]
        public float JumpForce
        {
            get { return _jumpForce; }
            set { _jumpForce = value; }
        }

        /// <summary>
        /// The total time a jump event will be valid after the Jump is called.
        /// </summary>
        [TorqueXmlSchemaType(DefaultValue = "0.1")]
        public float JumpTimeThreshold
        {
            get { return _jumpTimeThreshold; }
            set { _jumpTimeThreshold = value; }
        }

        /// <summary>
        /// Specifies whether or not an Actor will be allowed to jump down through one-way platforms.
        /// </summary>
        [TorqueXmlSchemaType(DefaultValue = "1")]
        public bool AllowJumpDown
        {
            get { return _allowJumpDown; }
            set { _allowJumpDown = value; }
        }

        /// <summary>
        /// This specifies the time to wait after jumping down to begin allowing the Actor to land on one-way patforms.
        /// </summary>
        [TorqueXmlSchemaType(DefaultValue = "0.18")]
        public float JumpDownTimeout
        {
            get { return _jumpDownTimeout; }
            set { _jumpDownTimeout = value; }
        }

        /// <summary>
        /// The constant speed at which this Actor climbs up a ladder.
        /// </summary>
        [TorqueXmlSchemaType(DefaultValue = "25")]
        public float ClimbUpSpeed
        {
            get { return _climbUpSpeed; }
            set { _climbUpSpeed = value; }
        }

        /// <summary>
        /// The constant speed at which this Actor climbs down a ladder.
        /// </summary>
        [TorqueXmlSchemaType(DefaultValue = "45")]
        public float ClimbDownSpeed
        {
            get { return _climbDownSpeed; }
            set { _climbDownSpeed = value; }
        }

        /// <summary>
        /// The time to wait after releasing from a ladder before allowing the Actor to start climbing again.
        /// </summary>
        [TorqueXmlSchemaType(DefaultValue = "0.25")]
        public float ClimbTimeout
        {
            get { return _climbTimeout; }
            set { _climbTimeout = value; }
        }

        /// <summary>
        /// The coefficient to multiply jump force and max move speed by when jumping 
        /// </summary>
        [TorqueXmlSchemaType(DefaultValue = "0.5")]
        public float ClimbJumpCoefficient
        {
            get { return _climbJumpCoefficient; }
            set { _climbJumpCoefficient = value; }
        }

        /// <summary>
        /// The base height of the ground check. The actual area of the groun check is based largely on velocity, but if this value is too small
        /// the ground may never actually register any ground objects.
        /// </summary>
        [TorqueXmlSchemaType(DefaultValue = "0.3")]
        public float GroundCheckYThreshold
        {
            get { return _groundCheckYThreshold; }
            set { _groundCheckYThreshold = value; }
        }

        /// <summary>
        /// The distance that the Actor will keep the lowest point of its collision polygon off the ground.
        /// </summary>
        [TorqueXmlSchemaType(DefaultValue = "0.15")]
        public float GroundYBuffer
        {
            get { return _groundYBuffer; }
            set { _groundYBuffer = value; }
        }

        /// <summary>
        /// The width by which to check for ladders. The ladder check is not swept, so if this value is too small it will be possible to pass
        /// through ladders entirely and never grab them.
        /// </summary>
        [TorqueXmlSchemaType(DefaultValue = "1")]
        public float LadderAttachXThreshold
        {
            get { return _ladderAttachXThreshold; }
            set { _ladderAttachXThreshold = value; }
        }

        /// <summary>
        /// This read-only property specifies whether or not an Actor should currently be able to land on one-way platforms (based on JumpDownTimeout).
        /// </summary>
        public bool CanActivatePlatforms
        {
            get
            {
                return TorqueEngineComponent.Instance.TorqueTime - _lastJumpDownTime > JumpDownTimeout * 1000 || _lastJumpDownTime == 0;
            }
        }

        /// <summary>
        /// The maximum value of the Y component of the surface normal of a platform to allow the ActorComponent to consider it as a ground surface.
        /// </summary>
        [TorqueXmlSchemaType(DefaultValue = "-0.1")]
        public float MaxGroundNormalY
        {
            get { return _maxGroundNormalY; }
            set { _maxGroundNormalY = value; }
        }

        /// <summary>
        /// The path to the sound bank used by sound events on the animation manager.
        /// </summary>
        [XmlIgnore]
        public string SoundBank
        {
            get { return _soundBank; }
            set { _soundBank = value; }
        }

        /// <summary>
        /// Specifies whether or not this Actor should allow its animation manager to play sound cues when specific animations are played.
        /// </summary>
        [XmlIgnore]
        public bool UseAnimationManagerSoundEvents
        {
            get { return _useAnimationManagerSoundEvents; }
            set { _useAnimationManagerSoundEvents = value; }
        }

        /// <summary>
        /// Specifies whether or not this Actor should allow its animation manager to play sound cues for specific frames of specific animations.
        /// </summary>
        [XmlIgnore]
        public bool UseAnimationStepSoundList
        {
            get { return _useAnimationStepSoundList; }
            set { _useAnimationStepSoundList = value; }
        }


        // environment interaction properties
        /// <summary>
        /// Specifies whether or not this Actor is within the boundaries of a ladder's trigger. This is set by the LadderComponent.
        /// </summary>
        [TorqueCloneIgnore]
        [XmlIgnore]
        public bool InLadder
        {
            get { return _ladderObject != null; }
        }

        /// <summary>
        /// Specifies the scene object associated with the ladder that this Actor is currently interacting with. This is set by the LadderComponent.
        /// </summary>
        [TorqueCloneIgnore]
        [XmlIgnore]
        public T2DSceneObject LadderObject
        {
            get { return _ladderObject; }
            set { _ladderObject = value; }
        }

        /// <summary>
        /// Specifies whether or not this Actor is on a platform.
        /// </summary>
        public bool OnGround
        {
            get { return _onGround; }
        }


        // physics state properties for FSM interface
        /// <summary>
        /// The current physics state of this Actor as set by the FSM class.
        /// </summary>
        [TorqueCloneIgnore]
        [XmlIgnore]
        public FSMState CurrentState
        {
            get { return _currentState; }
            set { _currentState = value; }
        }

        /// <summary>
        /// The previous physics state of this Actor as set by the FSM class.
        /// </summary>
        [TorqueCloneIgnore]
        [XmlIgnore]
        public FSMState PreviousState
        {
            get { return _previousState; }
            set { _previousState = value; }
        }


        // gameplay properties
        /// <summary>
        /// This specifies the current health of the Actor.
        /// </summary>
        public float Health
        {
            get { return _health; }
        }

        /// <summary>
        /// This specifies the maximum amount of health this Actor can have.
        /// </summary>
        //[TorqueXmlSchemaType(DefaultValue = "100")]
        public float MaxHealth
        {
            get { return _maxHealth; }
            set { _maxHealth = value; }
        }

        /// <summary>
        /// This specifies the amount of armor this Actor has. This is literally the percent of damage taken that will be applied to the Actor.
        /// If 50 the Actor will take half-damage, if 75 the Actor will take 1/4 damage, etc.
        /// </summary>
        [TorqueXmlSchemaType(DefaultValue = "0")]
        public float Armor
        {
            get { return (1 - _armorModifier) * 100; }
            set { _armorModifier = MathHelper.Clamp(1 - (value / 100), 0, 1); }
        }

        /// <summary>
        /// Specifies whether or not the Actor is alive.
        /// </summary>
        public bool Alive
        {
            get { return _alive; }
        }

        /// <summary>
        /// Specifies the total number of lives this Actor has.
        /// </summary>
        [TorqueXmlSchemaType(DefaultValue = "10")]
        public int Lives
        {
            get { return _lives; }
            set { _lives = (int)MathHelper.Clamp(value, 0, _maxLives); }
        }

        /// <summary>
        /// Specifies the amount of time to wait after taking damage before the Actor should allow further damage to be dealt.
        /// </summary>
        [TorqueXmlSchemaType(DefaultValue = "0.75")]
        public float DamageTimeout
        {
            get { return _damageTimeout; }
            set { _damageTimeout = value; }
        }

        /// <summary>
        /// Specifies whether or not the Actor should be allowed to respawn if it has lives left.
        /// </summary>
        [TorqueXmlSchemaType(DefaultValue = "0")]
        public bool AllowRespawn
        {
            get { return _allowRespawn; }
            set { _allowRespawn = value; }
        }

        /// <summary>
        /// Specifies the position at which to respawn.
        /// </summary>
        [TorqueCloneIgnore]
        [XmlIgnore]
        public Vector2 RespawnPosition
        {
            get { return _respawnPosition; }
            set { _respawnPosition = value; }
        }


        // animation properties
        /// <summary>
        /// The animation manager that is manipulating animations for this Actor.
        /// </summary>
        public ActorAnimationManager AnimationManager
        {
            get { return _animationManager; }
        }

        /// <summary>
        /// The animation to be played by the animation manager when this Actor is in an idle state.
        /// </summary>
        public T2DAnimationData IdleAnim
        {
            get { return _idleAnim; }
            set { _idleAnim = value; }
        }

        /// <summary>
        /// The animation to be played by the animation manager when this Actor is jumping from an idle state.
        /// </summary>
        public T2DAnimationData JumpAnim
        {
            get { return _jumpAnim; }
            set { _jumpAnim = value; }
        }

        /// <summary>
        /// The animation to be played by the animation manager when this Actor is falling from a normal jump.
        /// </summary>
        public T2DAnimationData FallAnim
        {
            get { return _fallAnim; }
            set { _fallAnim = value; }
        }

        /// <summary>
        /// The animation to be played by the animation manager when this Actor is running.
        /// </summary>
        public T2DAnimationData RunAnim
        {
            get { return _runAnim; }
            set { _runAnim = value; }
        }

        /// <summary>
        /// Specifies whether or not to scale the speed of the run animation based on acceleration and deceleration across the ground.
        /// </summary>
        [TorqueXmlSchemaType(DefaultValue = "0")]
        public bool ScaleRunAnimBySpeed
        {
            get { return _scaleRunAnimBySpeed; }
            set { _scaleRunAnimBySpeed = value; }
        }

        /// <summary>
        /// The minimum speed the run animation will be played represented as a value from 0 to 1 to be multiplied to the base duration of the animation.
        /// For example, if MinRunAnimSpeed is set to 0.25, the slowest the animation will play if ScaleRunAnimBySpeed is set to true will be 1/4 the speed 
        /// of normal. 
        /// </summary>
        [TorqueXmlSchemaType(DefaultValue = "0.25")]
        public float MinRunAnimSpeed
        {
            get { return _minRunAnimSpeedScale; }
            set { _minRunAnimSpeedScale = value; }
        }

        /// <summary>
        /// The animation to be played by the animation manager when this Actor jumps when running.
        /// </summary>
        public T2DAnimationData RunJumpAnim
        {
            get { return _runJumpAnim; }
            set { _runJumpAnim = value; }
        }

        /// <summary>
        /// The animation to be played by the animation manager when this Actor is falling from a running jump.
        /// </summary>
        public T2DAnimationData RunFallAnim
        {
            get { return _runFallAnim; }
            set { _runFallAnim = value; }
        }

        /// <summary>
        /// The animation to be played by the animation manager when this Actor is sliding to quickly change directions.
        /// </summary>
        public T2DAnimationData SlideAnim
        {
            get { return _slideAnim; }
            set { _slideAnim = value; }
        }

        /// <summary>
        /// The animation to be played by the animation manager when this Actor is idle on a ladder.
        /// </summary>
        public T2DAnimationData ClimbIdleAnim
        {
            get { return _climbIdleAnim; }
            set { _climbIdleAnim = value; }
        }

        /// <summary>
        /// The animation to be played by the animation manager when this Actor is climbing up a ladder.
        /// </summary>
        public T2DAnimationData ClimbUpAnim
        {
            get { return _climbUpAnim; }
            set { _climbUpAnim = value; }
        }

        /// <summary>
        /// The animation to be played by the animation manager when this Actor is climbing down a ladder.
        /// </summary>
        public T2DAnimationData ClimbDownAnim
        {
            get { return _climbDownAnim; }
            set { _climbDownAnim = value; }
        }

        /// <summary>
        /// The animation to be played by the animation manager when this Actor is jumping from a ladder.
        /// </summary>
        public T2DAnimationData ClimbJumpAnim
        {
            get { return _climbJumpAnim; }
            set { _climbJumpAnim = value; }
        }

        /// <summary>
        /// The animation to be played by the animation manager when the "action" state is entered.
        /// </summary>
        public T2DAnimationData ActionAnim
        {
            get { return _actionAnim; }
            set { _actionAnim = value; }
        }

        /// <summary>
        /// The animation to be played by the animation manager when this Actor takes damage.
        /// </summary>
        public T2DAnimationData DamageAnim
        {
            get { return _damageAnim; }
            set
            {
                if (value != null)
                    Assert.Fatal(!value.AnimationCycle, "DamageAnim should not cycle. Specify an animation that doesn't cycle.");

                _damageAnim = value;
            }
        }

        /// <summary>
        /// The animation to be played by the animation manager when this Actor dies.
        /// </summary>
        public T2DAnimationData DieAnim
        {
            get { return _dieAnim; }
            set
            {
                if (value != null)
                    Assert.Fatal(!value.AnimationCycle, "DieAnim should not cycle. Specify an animation that doesn't cycle.");

                _dieAnim = value;
            }
        }


        // properties to read anim transition data from XML
        /// <summary>
        /// A list of XMLTransitionData objects that specify which animation states should have transitional animations.
        /// </summary>
        public List<XMLTransitionData> AnimationTransitions
        {
            get { return _animationTransitions; }
            set { _animationTransitions = value; }
        }

        #endregion

        //======================================================
        #region Public methods

        // movement manipulation methods
        // (to be called most likely by a MoveController, but potentially from other sources
        // for example, a specific type of trigger might make an AI character jump or wateverz)
        /// <summary>
        /// Tells the Actor to move left. Generally used for ground and air movement.
        /// </summary>
        public virtual void MoveLeft()
        {
            _moveLeft = true;
            _moveRight = false;
        }

        /// <summary>
        /// Tells the Actor to move right. Generally used for ground and air movement.
        /// </summary>
        public virtual void MoveRight()
        {
            _moveRight = true;
            _moveLeft = false;
        }

        /// <summary>
        /// Tells the Actor to move neither left nor right. Generally used for ground and air movement.
        /// </summary>
        public virtual void HorizontalStop()
        {
            _moveLeft = false;
            _moveRight = false;
        }

        /// <summary>
        /// Tells the Actor to move up. Generally used for moving up and down ladders, but potentially also for 
        /// swimming, flying etc.
        /// </summary>
        public virtual void MoveUp()
        {
            _moveUp = true;
            _moveDown = false;
        }

        /// <summary>
        /// Tells the Actor to move down. Generally used for moving up and down ladders, but potentially also for 
        /// swimming, flying etc.
        /// </summary>
        public virtual void MoveDown()
        {
            _moveDown = true;
            _moveUp = false;
        }

        /// <summary>
        /// Tells the Actor to move neither up nor down. Generally used for moving up and down ladders, but potentially also for 
        /// swimming, flying etc.
        /// </summary>
        public virtual void VerticalStop()
        {
            _moveUp = false;
            _moveDown = false;
        }

        /// <summary>
        /// Tells the Actor to attempt to jump. The Actor's physics state will ultimately decide what to do with this request.
        /// </summary>
        public virtual void Jump()
        {
            _jump = true;
            _lastJumpEventTime = TorqueEngineComponent.Instance.TorqueTime;
        }

        /// <summary>
        /// Tells the Actor to jump down. This generally applies to when an Actor is standing on a one-way platform.
        /// </summary>
        public virtual void JumpDown()
        {
            _jumpDown = true;
            _lastJumpDownEventTime = TorqueEngineComponent.Instance.TorqueTime;
        }

        // health manipulation methods
        /// <summary>
        /// This kills the Actor outright by removing all it's remaining health.
        /// </summary>
        /// <param name="sourceObject">The scene object associated with the source of damage.</param>
        public virtual void Kill(T2DSceneObject sourceObject)
        {
            // tired of living...
            // (take damage equal to our current health and ignore armor.
            // yes, elegant - I know. ;| 
            // this was done to make sure all the death and damage stuff happens in the same way
            TakeDamage(Health + 1.0f, sourceObject, true, true);
        }

        /// <summary>
        /// Deals the specified amount of damage to an Actor. This method assumes that the Actor's Armor and the damage timeout 
        /// will be taken into account.
        /// </summary>
        /// <param name="damage">The amount of damage to be dealt.</param>
        /// <param name="sourceObject">The scene object associated with the source of damage.</param>
        /// <returns>True if some amount of damage was dealt to the Actor.</returns>
        public virtual bool TakeDamage(float damage, T2DSceneObject sourceObject)
        {
            // overload TakeDamage with ignoreArmor = false by default
            // (ignoreArmor = false means use 'armor' damage modifier - default Armor is 1, which means no change in damage.
            // if you're using armor and you want to deal damage directly, call TakeDamage(amount, true)
            return TakeDamage(damage, sourceObject, false);
        }

        /// <summary>
        /// Deals the specified amount of damage to an Actor. This method assums that the Actor's damage timeout will be taken into
        /// account.
        /// </summary>
        /// <param name="damage">The amount of damage to be dealt.</param>
        /// <param name="sourceObject">The scene object associated with the source of damage.</param>
        /// <param name="ignoreArmor">If set to true, the Actor will not mitigate damage due to Armor.</param>
        /// <returns>True if some amount of damage was dealt to the Actor.</returns>
        public virtual bool TakeDamage(float damage, T2DSceneObject sourceObject, bool ignoreArmor)
        {
            // overload TakeDamage with ignoreDamageTimeout = false
            // (ignoreDamageTimeout specifies whether or not the damage should be applied regardless 
            // of the timeout specified for the actor)
            return TakeDamage(damage, sourceObject, ignoreArmor, false);
        }

        /// <summary>
        /// Deals the specified amount of damage to an Actor.
        /// </summary>
        /// <param name="damage">The amount of damage to be dealt.</param>
        /// <param name="sourceObject">The scene object associated with the source of damage.</param>
        /// <param name="ignoreArmor">If set to true, the Actor will not mitigate damage due to Armor.</param>
        /// <param name="ignoreDamageTimeout">If set to true, the Actor will process the damage regardless of whether or not the
        /// time specified by its damage timeout property has passed.</param>
        /// <returns>True if some amount of damage was dealt to the Actor.</returns>
        public virtual bool TakeDamage(float damage, T2DSceneObject sourceObject, bool ignoreArmor, bool ignoreDamageTimeout)
        {
            // beating a dead horse?
            if (!_alive)
                return false;

            // get the current time
            float currTime = TorqueEngineComponent.Instance.TorqueTime;

            // check for damage timeout
            if (!ignoreDamageTimeout && currTime - _lastDamageTime < _damageTimeout * 1000)
                return false;

            // record this as the last damage time
            _lastDamageTime = currTime;

            // if we aren't 'ignoring armor', scale damage by the armor modifier
            if (!ignoreArmor)
                damage *= _armorModifier;

            // store starting health
            float startHealth = _health;

            // decrement health and clamp it between zero and max health
            _health -= damage;
            _health = MathHelper.Clamp(_health, 0, _maxHealth);

            // if we're out of health, let's go ahead and die
            // otherwise call _tookDamage to allow the Actor to react
            if (_health == 0)
                _die(startHealth - _health, sourceObject);
            else
                _tookDamage(startHealth - _health, sourceObject);

            // finally, kick us off a ladder, if we're on one
            _Climbing = false;

            return true;
        }

        /// <summary>
        /// Recovers the specified amount of damage.
        /// </summary>
        /// <param name="damage">The amount of health to recover.</param>
        /// <param name="sourceObject">The scene object associated with the healing action.</param>
        /// <returns>True if the healing was successful.</returns>
        public virtual bool HealDamage(float damage, T2DSceneObject sourceObject)
        {
            // make sure the actor is alive
            if (!_alive)
                return false;

            // add the specified health
            _health += damage;
            _health = MathHelper.Clamp(_health, 0, _maxHealth);

            // return true for successful healing
            return true;
        }

        /// <summary>
        /// Explicitly sets the position of the actor to the offset that it thinks it is from the platform that it thinks it's on.
        /// Use this when directly setting the Position of a platform if you want the actor to stay on it. Taking direct control of
        /// a platform can otherwise cause the actor to fall off. This had to be added to allow for PlatformMoveComponents to snap 
        /// to their path nodes, but it could also be used to basically warp platforms around with actors still riding them.
        /// This *must* be called before the actor has a chance to process physics or it may fall - i.e. this should be called 
        /// right after you set the position of the platform. The preferred method is to call PlatformWarped on the Platform itself, 
        /// which will call WarpToCurrentPlatform on all supported Actors, but either way will work.
        /// </summary>
        public virtual void WarpToCurrentPlatform()
        {
            // make sure there's a ground object and a platform offset
            if (_onGround && _previouslyOnGround && _groundObject != null && _platformOffset != Vector2.Zero)
            {
                // move the player to it's current platform offset from it's current ground object
                _actor.Position = _platformOffset + _groundObject.Position;
            }
        }

        public virtual void OnCollision(T2DSceneObject ourObject, T2DSceneObject theirObject, T2DCollisionInfo info, ref T2DResolveCollisionDelegate resolve, ref T2DCollisionMaterial physicsMaterial)
        {
            // call our custom collision resolve
            // (this is almost exactly the same as clamp, but takes *both* objects' velocity into account)
            if (theirObject.TestObjectType(PlatformerData.PlatformObjectType))
                resolve = ResolveActorCollision;
            else
                resolve = null;

            // if we hit a ladder, enable it
            if (theirObject.TestObjectType(PlatformerData.LadderObjectType))
            {
                // find the ladder component
                LadderComponent ladder = theirObject.Components.FindComponent<LadderComponent>();

                // enable it
                if (ladder != null)
                    ladder.Enabled = true;
            }
        }

        public virtual bool TestEarlyOut(T2DSceneObject ourObject, T2DSceneObject theirObject)
        {
            // make sure it's a one-way platform that we're dealing with
            if (theirObject.TestObjectType(PlatformerData.PlatformObjectType))
            {
                // check if the platform is in our active platforms list
                if (_activePlatforms.Contains(theirObject))
                    // collide
                    return false;
                else
                    // don't collide
                    return true;
            }

            // default: collide
            return false;
        }

        public virtual void ResolveActorCollision(T2DSceneObject ourObject, T2DSceneObject theirObject, ref T2DCollisionInfo info, T2DCollisionMaterial physicsMaterial, bool handleBoth)
        {
            // this is a custom collision response mode for actors
            // make sure this method is being used on an Actor
            if (!ourObject.TestObjectType(PlatformerData.ActorObjectType))
                return;

            // make sure it's the right actor
            ActorComponent actor = ourObject.Components.FindComponent<ActorComponent>();
            if (actor == null)
                return;

            // check for a physics component
            if (_actor.Physics == null)
                return;

            // get their object's velocity
            Vector2 theirVel = Vector2.Zero;

            if (theirObject.Physics != null)
                theirVel = theirObject.Physics.Velocity;

            // check if it's a ground surface
            bool groundSurface = info.Normal.Y <= _maxGroundNormalY;

            // get the actor's velocity modified velocity by the destination object's velocity
            Vector2 vel = _actor.Physics.Velocity - theirVel;

            //get the dot product
            float dot = Vector2.Dot(vel, info.Normal);

            // if we're currently on the ground, do magical stuff
            if (_onGround && !groundSurface)
            {
                // don't let the clamp push us off the ground or into the ground!
                // accomplish this by clamping the collision normal against the ground normal
                float groundDot = Vector2.Dot(info.Normal, _groundSurfaceNormal);

                // remove any portion of the ground surface normal from the collision normal
                info.Normal -= groundDot * _groundSurfaceNormal;

                // cancel move speed (this isn't a ground surface that we hit)
                _moveSpeed.X = 0;
            }

            // return if we're moving away from the surface
            if (dot >= 0.0f)
                return;

            // if object was not a ground surface...
            if (!groundSurface)
            {
                // notify the controller that we hit a wall
                if (Controller as ActorController != null)
                    (Controller as ActorController).ActorHitWall(this, info, dot);

                // forfeit all inherited velocity
                // (we ran into something, just get rid of it!)
                _inheritedVelocity = Vector2.Zero;
            }
            else
            {
                // if we actually overlapped with a platform, correct by a small amount (1/10 of a unit)
                // (the purpose of _groundYBuffer is to avoid constant collisions from scraping across the ground.
                // if we are colliding with the ground regularly, _groundYBuffer is too low. this error correction
                // is intended to solve potential problems when prolonged penetration occurs.)
                _platformError += info.Normal * 0.1f;
            }

            // clamp our actial velocity anyway
            _actor.Physics.Velocity -= dot * info.Normal;

            // if we're not on the ground, just dump our velocity into our move speed
            // and let the actor's physics clamp it
            if (!_onGround)
                _moveSpeed.X = _actor.Physics.Velocity.X;
        }

        public override void CopyTo(TorqueComponent obj)
        {
            base.CopyTo(obj);
            ActorComponent obj2 = obj as ActorComponent;

            obj2.Gravity = Gravity;
            obj2.MaxVelocity = MaxVelocity;
            obj2.MaxMoveSpeed = MaxMoveSpeed;
            obj2.GroundAccel = GroundAccel;
            obj2.GroundDecel = GroundDecel;
            obj2.AirAccel = AirAccel;
            obj2.AirDecel = AirDecel;
            obj2.JumpForce = JumpForce;
            obj2.AllowJumpDown = AllowJumpDown;
            obj2.JumpDownTimeout = JumpDownTimeout;
            obj2.ClimbUpSpeed = ClimbUpSpeed;
            obj2.ClimbDownSpeed = ClimbDownSpeed;
            obj2.ClimbTimeout = ClimbTimeout;
            obj2.ClimbJumpCoefficient = ClimbJumpCoefficient;
            obj2.LadderAttachXThreshold = LadderAttachXThreshold;
            obj2.GroundCheckYThreshold = GroundCheckYThreshold;
            obj2.GroundYBuffer = GroundYBuffer;

            obj2.MaxHealth = MaxHealth;
            obj2.Armor = Armor;
            obj2.Lives = Lives;
            obj2.AllowRespawn = AllowRespawn;
            obj2.DamageTimeout = DamageTimeout;

            obj2.SoundBank = SoundBank;
            obj2.UseAnimationManagerSoundEvents = UseAnimationManagerSoundEvents;
            obj2.UseAnimationStepSoundList = UseAnimationStepSoundList;

            obj2.AnimationTransitions = AnimationTransitions;
            obj2.AnimationManager.Transitions = AnimationManager.Transitions.Clone() as Hashtable;
            obj2.AnimationManager.SoundEvents = AnimationManager.SoundEvents.Clone() as Hashtable;
            obj2.AnimationManager.StepSoundAnimations = AnimationManager.StepSoundAnimations.Clone() as Hashtable;
            obj2.IdleAnim = IdleAnim;
            obj2.JumpAnim = JumpAnim;
            obj2.FallAnim = FallAnim;
            obj2.RunAnim = RunAnim;
            obj2.RunJumpAnim = RunJumpAnim;
            obj2.RunFallAnim = RunFallAnim;
            obj2.SlideAnim = SlideAnim;
            obj2.ClimbIdleAnim = ClimbIdleAnim;
            obj2.ClimbUpAnim = ClimbUpAnim;
            obj2.ClimbDownAnim = ClimbDownAnim;
            obj2.ClimbJumpAnim = ClimbJumpAnim;
            obj2.ActionAnim = ActionAnim;
            obj2.DamageAnim = DamageAnim;
            obj2.DieAnim = DieAnim;
        }

        #endregion

        //======================================================
        #region Private, protected, internal methods

        /// <summary>
        /// Called when damage is applied to this Actor. Use this callback to respond to damage.
        /// </summary>
        /// <param name="damage">The amount of damage that was dealt to this Actor.</param>
        /// <param name="sourceObject">The scene object associated with the source of the damage. Normally the owner of a component that 
        /// called for the damage.</param>
        protected virtual void _tookDamage(float damage, T2DSceneObject sourceObject)
        {
            FSM.Instance.SetState(_animationManager, "damage");
            if (Controller != null)
                (Controller as ActorController).ActorDamaged(this, damage, sourceObject);
        }

        /// <summary>
        /// Called when the Actor dies.
        /// </summary>
        /// <param name="damage">The amount of damage that was dealt to this Actor that caused its death. Note that this can potentially be
        /// relatively huge if the Kill method was used to destroy the Actor.</param>
        /// <param name="sourceObject">The scene object associated with the source of the damage. Normally the owner of a component that 
        /// called for the damage.</param>
        protected virtual void _die(float damage, T2DSceneObject sourceObject)
        {
            // tag our alive flag
            _alive = false;
            _actor.CollisionsEnabled = false;
            _actor.SetObjectType(PlatformerData.ActorObjectType, false);

            // decrement lives
            _lives--;
            _lives = (int)MathHelper.Clamp(_lives, 0, _maxLives);

            // notify the controller that this actor died
            if (Controller != null)
                (Controller as ActorController).ActorDied(this, damage, sourceObject);
        }

        /// <summary>
        /// Respawns this Actor at its RespawnPosition.
        /// </summary>
        protected virtual void _respawn()
        {
            // makes sure we are allowed to respawn
            if (!_allowRespawn)
                return;

            // bring this player back to life and spawn it at its spawn position
            _actor.Physics.Velocity = Vector2.Zero;
            _moveSpeed = Vector2.Zero;
            _inheritedVelocity = Vector2.Zero;
            _actor.WarpToPosition(RespawnPosition, _actor.Rotation);
            _alive = true;
            _actor.CollisionsEnabled = true;
            _actor.SetObjectType(PlatformerData.ActorObjectType, true);
            _health = _maxHealth;

            // notify the controller that this actor respawned
            if (Controller != null)
                (Controller as ActorController).ActorSpawned(this);
        }

        /// <summary>
        /// Called when this Actor is possessed by a MoveController. This is used to make sure that only an ActorController can possess 
        /// an ActorComponent.
        /// </summary>
        /// <param name="controller">The MoveController that possessed this Actor.</param>
        protected override void _possessed(MoveController controller)
        {
            base._possessed(controller);

            // make sure that the controller possessing us is an 
            // actor controller. if not, dump it!
            if (controller as ActorController == null)
                Unpossess();
        }

        /// <summary>
        /// This method is called if PooledWithComponents is enabled on the owner object to give you a chance to reinitialize the Actor.
        /// </summary>
        protected virtual void _resetActor()
        {
            // reset all the default values
            _actor.Physics.Velocity = Vector2.Zero;
            _moveSpeed = Vector2.Zero;
            _inheritedVelocity = Vector2.Zero;
            _actor.WarpToPosition(RespawnPosition, _actor.Rotation);
            _alive = true;
            _actor.CollisionsEnabled = true;
            _actor.SetObjectType(PlatformerData.ActorObjectType, true);
            _health = _maxHealth;
        }

        protected override bool _OnRegister(TorqueObject owner)
        {
            if (!base._OnRegister(owner))
                return false;

            // store the owner as a static sprite
            _actor = owner as T2DSceneObject;

            // if there is no animated sprite specified use the owner
            if (_animatedSprite == null)
                _animatedSprite = _actor as T2DAnimatedSprite;

            // set the object type of the scene object
            _actor.SetObjectType(PlatformerData.ActorObjectType, true);

            // make sure we have both a collision and physics component..
            Assert.Fatal(_actor.Collision != null && _actor.Physics != null, "Actor must have a collision component and a physics component.");

            // make sure we have collision enabled
            _actor.CollisionsEnabled = true;
            _actor.Collision.SolveOverlap = true;

            // preserve existing collision types, if any
            if (_actor.Collision.CollidesWith.Equals(TorqueObjectType.AllObjects))
            {
                _actor.Collision.CollidesWith = PlatformerData.PlatformObjectType
                                                + PlatformerData.ActorTriggerObjectType
                                                + PlatformerData.CollectibleObjectType
                                                + PlatformerData.DamageTriggerObjecType;
            }
            else
            {
                _actor.Collision.CollidesWith += PlatformerData.PlatformObjectType
                                                + PlatformerData.ActorTriggerObjectType
                                                + PlatformerData.CollectibleObjectType
                                                + PlatformerData.DamageTriggerObjecType;
            }

            // preserve existing early-out object types
            _actor.Collision.EarlyOutObjectType += PlatformerData.OneWayPlatformObjectType;

            // set the collision delegates
            _actor.Collision.OnCollision = OnCollision;
            _actor.Collision.TestEarlyOut = TestEarlyOut;

            // make sure ProcessCollisionsAtRest is enabled
            // (this ensures that our passive DirectionalTriggerComponents work)
            _actor.Physics.ProcessCollisionsAtRest = true;

            // set default respawn position to the current position
            _respawnPosition = _actor.Position;

            // if PoolWithComponents is true on the scene object, reset the Actor
            if (_actor.PoolWithComponents)
                _resetActor();

            // notify the controller that this actor spawned
            if (Controller != null)
                (Controller as ActorController).ActorSpawned(this);

            // process any extra data we might have collected from XML
            _processXMLDataLists();

            // initialize animation manager
            // (this will handle all our animations and transitions)
            _initAnimationManager();

            _health = _maxHealth;

            // return true
            return true;
        }

        /// <summary>
        /// This is called by _OnRegister and is used to process the animation transition data that was assigned during deserializaion 
        /// and insert it onto our animation manager's transitions list.
        /// </summary>
        protected virtual void _processXMLDataLists()
        {
            if (_animationManager != null && _animationTransitions != null)
                foreach (XMLTransitionData transition in _animationTransitions)
                    _animationManager.SetTransition(transition.FromState, transition.ToState, transition.Animation);
        }

        /// <summary>
        /// This is used to register various physics states for this Actor. Override this method to insert or replace a physics state.
        /// </summary>
        protected virtual void _registerPhysicsStates()
        {
            // register states  
            // this function exists to allow you to override and 
            // register additional/different physics states
            FSM.Instance.RegisterState<OnGroundState>(this, "onGround");
            FSM.Instance.RegisterState<InAirState>(this, "inAir");
            FSM.Instance.RegisterState<OnLadderState>(this, "onLadder");
            FSM.Instance.RegisterState<DeadState>(this, "dead");

            // set the initial state
            _currentState = FSM.Instance.GetState(this, "onGround");
        }

        /// <summary>
        /// This is used to create an instance of our animation manager class. Override this to use a derived animation manager
        /// with specific animation states that you specify.
        /// </summary>
        protected virtual void _createAnimationManager()
        {
            // create and assign animation manager
            // the sole purpose of this function is to alow you to 
            // override this call and assign an animation manager
            // that has additional/different animation states
            _animationManager = new ActorAnimationManager(this);
        }

        /// <summary>
        /// This is used to assign transitions and sound events, if neccesary. Override this to apply various transitions or 
        /// sound events to this Actor's animation manager.
        /// </summary>
        protected virtual void _initAnimationManager()
        {
            // assign any neccesary transitions, sound events, or 
            // step sound frames by overriding this method.
        }

        /// <summary>
        /// This is the standard MoveComponent _preUpdate callback. This is where _updatePhysics and _updateAnimation are called.
        /// </summary>
        /// <param name="elapsed">Elapsed time since last _preUpdate.</param>
        protected override void _preUpdate(float elapsed)
        {
            // make sure we have a damn scene object!
            if (_actor == null)
                return;

            // update physics
            _updatePhysics(elapsed);

            // update animations
            _updateAnimation(elapsed);
        }

        /// <summary>
        /// This is the standard MoveComponent _postUpdate callback. This is where _updateOnGround is called.
        /// </summary>
        /// <param name="elapsed">Elapsed time since last _postUpdate.</param>
        protected override void _postUpdate(float elapsed)
        {
            // make sure we have a damn scene object.. again!
            if (_actor == null)
                return;

            // update our ground state
            _updateOnGround(elapsed);
        }

        /// <summary>
        /// This is the core physics update method. All state-specific physics are called from here.
        /// </summary>
        /// <param name="elapsed">Elapsed time passed from _preUpdate.</param>
        protected virtual void _updatePhysics(float elapsed)
        {
            // assert animated sprite found
            Assert.Fatal(!(_actor as T2DAnimatedSprite == null && _animatedSprite == null), "An ActorComponent must be added to an animated sprite, or an animated sprite must be specified. \n\nActor cannot update without an animated sprite.");

            // figure out if this actor should be climbing
            _updateClimbing();

            // execute the state code to decide which physics state we are in
            FSM.Instance.Execute(this);

            // scale elapsed to so movement variables can make sense to humans
            // (seriously, that's the only reason.. o_O)
            float timeScale = elapsed * 100;

            // let the current physics state update our physics
            if (_currentState as ActorState != null)
                (_currentState as ActorState).UpdatePhysics(this, timeScale);

            // clamp the actor's velocity to maxVelocity
            // (so we don't break stuff)
            _actor.Physics.VelocityX = MathHelper.Clamp(_actor.Physics.VelocityX, -_maxVelocity.X, _maxVelocity.X);
            _actor.Physics.VelocityY = MathHelper.Clamp(_actor.Physics.VelocityY, -_maxVelocity.Y, _maxVelocity.Y);

            // record what the total X velocity was at the end of this update
            // (this is used in some of the physics states to determine how much X velocity has changed since the previous state)
            _previousTotalVelocityX = _actor.Physics.VelocityX;
        }

        /// <summary>
        /// This is the core animation update method. The animation manager gets executed here.
        /// </summary>
        /// <param name="elapsed">Elapsed time passed from _preUpdate.</param>
        protected virtual void _updateAnimation(float elapsed)
        {
            // set proper time scale for accel/decel
            float timeScale = elapsed * 100;

            // throttle run animation speed field, if set
            if (_scaleRunAnimBySpeed)
            {
                if (_moveLeft || _moveRight)
                    _runAnimSpeedScale += (_onGround ? _groundAccel : _airAccel) * timeScale;
                else
                    _runAnimSpeedScale -= (_onGround ? _groundDecel : _airDecel) * timeScale;

                _runAnimSpeedScale = MathHelper.Clamp(_runAnimSpeedScale, _minRunAnimSpeedScale * _maxMoveSpeed, _maxMoveSpeed);
            }

            // let the animation manager do its magic
            FSM.Instance.Execute(_animationManager);

            // flip the image only when:
            // -we're not climbing
            // -we're not dead X_X
            // -we're actually moving left
            // -we're not sliding
            // (note: no default action)
            bool oldFlipX = _actor.FlipX;

            if (!(_slideAnim != null && _animationManager.CurrentState.StateName == "slide"))
            {
                    if (_moveRight || _Climbing)
                        _actor.FlipX = false;
                    else if (_moveLeft)
                        _actor.FlipX = true;
            }
            else
            {
                _actor.FlipX = _moveSpeed.X < 0.0f;
            }

            // check to see if we are controlling an actor puppet
            // (we never rotate the actual actor - rotation only gets updated for actor puppets!)
            if (_puppet != null)
            {
                // perform rotation
                if (_puppet.RotateToGroundSurface)
                {
                    float targetRotation = 0;

                    if (_onGround)
                        targetRotation = MathHelper.ToDegrees((float)Math.Atan2(_groundSurfaceNormal.X, -_groundSurfaceNormal.Y));

                    if (_puppet.RateOfRotation == 0)
                    {
                        _puppet.PuppetRotation = targetRotation;
                    }
                    else
                    {
                        float difference = Math.Abs(targetRotation - _puppet.PuppetRotation) % 360;
                        float movementModifier = 2 - (Math.Abs(_moveSpeed.X) / _maxMoveSpeed);
                        difference = (difference > 180 ? difference - 360 : difference) * _puppet.RateOfRotation * movementModifier * elapsed;
                        _puppet.PuppetRotation += ((targetRotation < _puppet.PuppetRotation ? -difference : difference)) * elapsed;
                    }
                }
                else
                {
                    _puppet.PuppetRotation = 0;
                }

                // if a flip occurred, refresh the sprite position immediately to avoid interpolation
                if (_actor.FlipX != oldFlipX)
                    _puppet.RefreshSpritePosition();
            }
        }

        /// <summary>
        /// Perform required checks to decide whether or not this Actor should start or stop climbing.
        /// </summary>
        protected virtual void _updateClimbing()
        {
            // grab the current ground component cast as a one-way platform component
            OneWayPlatformComponent platComp = _groundObjectComponent as OneWayPlatformComponent;

            // if...
            // -(we are in a valid ladder and not already climbing
            // -and we are within the required threshold of the center of the ladder
            // -(and we are not trying to climb up from above the ladder
            //   -(and we are not trying to climb down when standing on a ground object that we are not allowed to climb down through))))
            if (InLadder && !_Climbing
                && Math.Abs(_ladderObject.Position.X - _actor.Position.X) < _ladderAttachXThreshold
                && ((_moveUp && _actor.Position.Y + _actorMaxY > _ladderObject.Position.Y - (_ladderObject.Size.Y / 2))
                    || (_moveDown && (!_onGround || (platComp != null && platComp.CanClimbThrough)))))
            {
                // set climbing to true
                _Climbing = true;

                // clear the active platform list
                // (so we can climb down through platforms.
                // the list will repopulate itself correctly if we shouldn't be allowed to.)
                _activePlatforms.Clear();

                // snap the actor to the ladder
                if (_Climbing)
                    _actor.Position = new Vector2(_ladderObject.Position.X, _actor.Position.Y);

                // modify the process list so we get physics updates after the ladder
                ProcessList.Instance.SetProcessOrder(_ladderObject, _actor);
            }
            else if (_Climbing && !InLadder)
            {
                // if we think we're climbing and we arent.. 
                // set climbing to false
                _Climbing = false;
            }
        }

        /// <summary>
        /// Update whether or not the Actor is on some sort of "ground" (specifically, any type of platform).
        /// </summary>
        /// <param name="elapsed">Elapsed time passed from _postUpdate.</param>
        /// <returns>True if the Actor is standing on a ground surface. This value can also be accessed via the public OnGround property.</returns>
        private bool _updateOnGround(float elapsed)
        {
            // make sure we're alive
            if (!_alive)
                return false;

            // run pickGround
            _pickGround(elapsed);

            // init vars
            float groundMinY = 0;
            float groundLowestDot = 0;
            T2DSceneObject newGroundObject = null;
            SolidPlatformComponent newPlatformComponent = null;
            Vector2 platformVel = Vector2.Zero;
            float velMod = 0;

            // clear the active platforms list
            _activePlatforms.Clear();

            // check pick results
            foreach (ISceneObject sobj in _containerQueryResults)
            {
                // skip if the object is not a platform object type
                if (!(sobj as T2DSceneObject).TestObjectType(PlatformerData.PlatformObjectType))
                    continue;

                // grab the platform component of this object
                SolidPlatformComponent platComp = (sobj as T2DSceneObject).Components.FindComponent<SolidPlatformComponent>();

                // skip if it doesn't have a platform component
                // or it's platform component is marked as disabled
                if (platComp == null || !platComp.PlatformEnabled)
                    continue;

                // do extra checks for normal one-way platforms
                if (platComp is OneWayPlatformComponent)
                {
                    // skip this platform if we're jumping down
                    if (!CanActivatePlatforms)
                        continue;

                    // skip this platform if the actor is climbing -and- it's possible to climb through this platform
                    if (_Climbing && (platComp as OneWayPlatformComponent).CanClimbThrough)
                        continue;
                }

                // do a poly check against this ground object
                _testGroundPolyMove(sobj as T2DSceneObject);

                // if there were no collisions with this object, skip it
                if (_groundCollisionList.Count == 0)
                    continue;

                // chose a ground object and activate all platforms
                for (int i = 0; i < _groundCollisionList.Count; i++)
                {
                    // skip vertical walls.. duh >_<
                    if (_groundCollisionList[i].Normal.Y > _maxGroundNormalY)
                        continue;

                    // get the ground object's velocity
                    if ((sobj as T2DSceneObject).Physics != null)
                        platformVel = (sobj as T2DSceneObject).Physics.Velocity;
                    else
                        platformVel = Vector2.Zero;

                    // get the dot product of the actor's velocity relative to the platform's velocity
                    Vector2 ourVel = _actor.Physics.Velocity - platformVel;

                    // normalize the velocity only if it's not zero 
                    //(this avoids "divide by zero" errors)
                    if (ourVel != Vector2.Zero)
                        ourVel.Normalize();

                    float dot = Vector2.Dot(ourVel, _groundCollisionList[i].Normal);

                    // skip platforms we are moving away from 
                    // (allows us to move through one-ways! because we ignore them here they 
                    // are omitted from the active platforms list, which is checked in TestEarlyOut)
                    if (dot > Epsilon.Value)
                        continue;

                    // if we are on a slope, make sure to find an exact ground contact position
                    if (Math.Abs(_groundCollisionList[i].Normal.X) > Epsilon.Value)
                    {
                        // get the offset based on direction
                        float directionOffset = 0;

                        if (_groundCollisionList[i].Normal.X > 0)
                        {
                            if (_actor.FlipX)
                                directionOffset = _ActorMaxX;
                            else
                                directionOffset = -_ActorMinX;
                        }
                        else
                        {
                            if (_actor.FlipX)
                                directionOffset = _ActorMinX;
                            else
                                directionOffset = -_ActorMaxX;
                        }

                        // get the X difference
                        float xDiff = _groundCollisionList[i].Position.X - (_actor.Position.X - directionOffset);

                        // get an appropriate y difference along the surface
                        float yDiff = xDiff * (_groundCollisionList[i].Normal.X / _groundCollisionList[i].Normal.Y);

                        // modify the ground contact position
                        T2DCollisionInfo newInfo = _groundCollisionList[i];
                        newInfo.Position = newInfo.Position + new Vector2(xDiff, yDiff);
                        _groundCollisionList[i] = newInfo;
                    }

                    // choose a velocity modifier to use when checking one-way platforms
                    // (if on the ground, use X velocity; if in the air, use Y velocity)
                    if (_onGround)
                        velMod = Math.Abs(_actor.Physics.VelocityX) * elapsed;
                    else
                        velMod = Math.Abs(_actor.Physics.VelocityY) * elapsed;

                    velMod += _groundCheckYThreshold;

                    // make sure we are above the potential ground object's collision point
                    // (some leeway is given based on penetration and ground check threshold)
                    if (platComp is OneWayPlatformComponent
                        && _actor.Position.Y + _actorMaxY - _groundCheckYThreshold - velMod > _groundCollisionList[i].Position.Y)
                        continue;

                    // add the one-way platform component to the list of currently active platforms 
                    // (if the platform reached this point it's a platform that we might collide with
                    // .. in other words, it is either a solid platform or a one-way platform and
                    // we are moving either parallel to the surface or towards the surface in some way)
                    if (platComp is OneWayPlatformComponent && CanActivatePlatforms)
                    {
                        (platComp as OneWayPlatformComponent).PlatformActive = true;
                        _activePlatforms.Add(platComp.SceneObject);
                    }

                    // modify the dot product to force the following platform selection to favor angled 
                    // platforms a litle more than they would otherwise
                    // (this is done specifically to avoid a terrible and obnoxious case where you are
                    // falling down towards the intersection of a flat one-way plat and a sloped one-way plat
                    // and the actor chooses the flat one, causing you to run straight through the slope.
                    // this happens because at the time that you hit the ground your velocity is more
                    // directly towards the flat platform than the sloped one. it's extremely annoying.
                    // this bug still prevails sometimes, despite my best efforts. the only other solution
                    // I can think of would be to support multiple ground objects - WNF for initial release)
                    float fancyDot = dot - (1 + (_groundCollisionList[i].Normal.Y / 1.1f));

                    // if our velocity is more directly into this platform than the last
                    // or it's the first platform we got to, grab its info
                    // (note that if dot is equal, this check will always prefer the 
                    // current ground object over a new one to avoid needless assignments 
                    // later on in this method)
                    if (newGroundObject == null
                        || (_groundCollisionList[i].Position.Y < groundMinY)
                        || (_onGround && (fancyDot < groundLowestDot
                                        || ((!_moveLeft && !_moveRight) || fancyDot == groundLowestDot) && sobj == _groundObject)))
                    {
                        // grab highest point so far if this is the first ground object
                        if (newGroundObject == null)
                            groundMinY = _groundCollisionList[i].Position.Y;

                        // store values for this ground object
                        newGroundObject = sobj as T2DSceneObject;
                        newPlatformComponent = platComp;
                        groundLowestDot = fancyDot;
                        _groundSurfaceNormal = _groundCollisionList[i].Normal;
                        _groundContactPosition = _groundCollisionList[i].Position;
                        _groundPenetration = _groundCollisionList[i].Penetration;
                    }

                    // record the highest ground contact position found
                    if (_groundCollisionList[i].Position.Y < groundMinY)
                        groundMinY = _groundCollisionList[i].Position.Y;
                }
            }

            // deactivate all unused platforms
            for (int i = 0; i < _previouslyActivePlatforms.Count; i++)
            {
                if (!_activePlatforms.Contains(_previouslyActivePlatforms[i]))
                {
                    OneWayPlatformComponent platComp = _previouslyActivePlatforms[i].Components.FindComponent<OneWayPlatformComponent>();
                    platComp.PlatformActive = false;
                    _previouslyActivePlatforms.Remove(_previouslyActivePlatforms[i]);
                    i--;
                }
            }

            // update previously active platforms list
            for (int i = 0; i < _activePlatforms.Count; i++)
            {
                if (!_previouslyActivePlatforms.Contains(_activePlatforms[i]))
                    _previouslyActivePlatforms.Add(_activePlatforms[i]);
            }

            // if no valid ground objects found...
            if (newGroundObject == null)
            {
                // notify current ground component that we are leaving (if newly airborne)
                if (_onGround && _groundObjectComponent != null)
                    _groundObjectComponent.ActorLeft(this);

                // set onGround flag to false
                _previouslyOnGround = _onGround;
                _onGround = false;

                // return false
                return false;
            }

            // set the onGround flag to true
            _onGround = true;

            // if this is a new ground object -and- we were on the ground before
            // (we ran from one platform to another without leaving the ground)
            if (newGroundObject != _previousGroundObject && _previouslyOnGround)
            {
                // notify the previous ground component we're leaving
                // (this would not get called otherwise)
                if (_groundObjectComponent != null)
                    _groundObjectComponent.ActorLeft(this);
            }

            // if this is a new ground component -or- we just landed
            // (either running from one to the other, or just landing on a new one)
            // (in other words: every platform we use and only when we first get it)
            if (newGroundObject != _previousGroundObject || !_previouslyOnGround)
            {
                // update previous ground object for next check
                _previousGroundObject = _groundObject;

                // record the ground object that we found
                _groundObject = newGroundObject;

                // find the new ground component
                _groundObjectComponent = newPlatformComponent;

                // set the ground force and friction based on the ground component
                // (note: PlatformFriction property pulls friction value from owner's T2DPhysicsMaterial)
                if (_groundObjectComponent != null)
                {
                    _groundSurfaceForce = _groundObjectComponent.SurfaceForce;
                    _groundFriction = _groundObjectComponent.PlatformFriction;
                }
                else
                {
                    // no ground object component.. 
                    // just init them to normal values
                    _groundSurfaceForce = 0;
                    _groundFriction = 1;
                }

                // notify the new ground component we arrived
                if (_groundObjectComponent != null)
                    _groundObjectComponent.ActorLanded(this);

                // notify the controller of this actor that it landed on something
                if (Controller != null)
                    (Controller as ActorController).ActorLanded(this, _groundObject);

                // instantly remove any inherited velocity directly toward or away from the surface of the ground object
                float dot = Vector2.Dot(_inheritedVelocity - platformVel, _groundSurfaceNormal);
                _inheritedVelocity = _inheritedVelocity - dot * _groundSurfaceNormal;

                // make sure Climbing is set to false
                if (_Climbing)
                    _Climbing = false;

                // zero out platform error
                // (this can accumulate over a series of collisions with the same platform.
                // must be cleared so each platform can be corrected properly, if neccesary.
                // this value will remain zero for the vast majority of all actor movement.)
                if (_platformError != Vector2.Zero)
                    _platformError = Vector2.Zero;

                // modify the process list so we get physics updates after the ground object we're standing on
                ProcessList.Instance.SetProcessOrder(_actor, _groundObject);
            }

            // if we wound up with a Y value beneath the highest found
            // modify the one we have to be match the highest
            // (sounds crazy, but happens a lot when running between sloped platforms)
            if (_groundContactPosition.Y > groundMinY)
            {
                // get the y difference
                float yDiff = groundMinY - _groundContactPosition.Y;

                // init xDiff to zero
                float xDiff = 0.0f;

                // get an appropriate x difference along the surface
                if (_groundSurfaceNormal.X != 0.0f)
                    xDiff = yDiff * (_groundSurfaceNormal.Y / _groundSurfaceNormal.X);

                // flip the x difference based on the direction we are going
                if (_groundSurfaceNormal.X < 0)
                    xDiff *= -1;

                // modify the ground contact position
                _groundContactPosition += new Vector2(xDiff, yDiff);
            }

            // get the previous position for move clamping
            Vector2 prevPos = _previousPosition;

            // factor in platform movement in the Y direction
            if (_groundObject.Physics != null && Math.Abs(_groundObject.Physics.VelocityY) > Epsilon.Value)
            {
                // get the vel diff this tick
                Vector2 velDiff = _groundObject.Physics.Velocity * elapsed;

                // modify contact position and previous position by vel diff
                _groundContactPosition += velDiff;
                prevPos += velDiff;
            }

            // get the new position on the ground
            Vector2 newPosition = new Vector2(_actor.Position.X, _groundContactPosition.Y - _ActorMaxY) + _platformError;

            // if the platform is slanted, get a more exact offset
            if (_previouslyOnGround && newPosition.X != prevPos.X
                && _groundSurfaceNormal == _previousSurfaceNormal && Math.Abs(_groundSurfaceNormal.X) > Epsilon.Value)
            {
                // get the desired move vector
                Vector2 desiredMoveVector = newPosition - prevPos;

                // get the dot product of the move vector and the ground normal
                float moveDot = Vector2.Dot(desiredMoveVector, _groundSurfaceNormal);

                // get the move vector corrected by the surface normal
                Vector2 correctedMoveVector = desiredMoveVector - (moveDot * _groundSurfaceNormal);

                // modify newPosition's Y component by the difference
                newPosition = new Vector2(newPosition.X, newPosition.Y + (correctedMoveVector.Y - desiredMoveVector.Y));
            }

            // snap to the ground
            _actor.Position = newPosition;

            // record several states at the end of this update
            _previouslyOnGround = _onGround;
            _previousPosition = newPosition;
            _previousSurfaceNormal = _groundSurfaceNormal;

            // record the last known onGround time 
            // (which is.. right now!)
            _lastOnGroundTime = TorqueEngineComponent.Instance.TorqueTime;

            // record the current offset from the platform
            // (this is just useful to have sometimes)
            _platformOffset = _actor.Position - _groundObject.Position;

            // return true
            return true;
        }

        /// <summary>
        /// Initializes the container query data used to evaluate whether the Actor is on the ground or not. Called by the constructor.
        /// </summary>
        private void _initGroundQueryData()
        {
            // init query data for ground pick
            _queryData.ObjectTypes = PlatformerData.PlatformObjectType;
            _queryData.LayerMask = 0xFFFFFFFF;
            _queryData.IgnoreObject = null;
            _queryData.IgnoreObjects = null;
            _queryData.FindInvisible = true;
            _queryData.IgnorePhysics = false;
            _queryData.ResultList = _containerQueryResults;
        }

        /// <summary>
        /// Performs the container query used to gather a list of objects beneath the Actor for the purposes of ground checking.
        /// Called by _updateOnGround.
        /// </summary>
        /// <param name="elapsed">Elapsed time passed from _updateOnGround.</param>
        private void _pickGround(float elapsed)
        {
            // pick rect position vars
            float left, top, width, height;

            // get the position and extent of the rect
            left = _actor.Position.X + _ActorMinX;
            top = _ActorMaxY + _actor.Position.Y;
            width = _ActorMaxX - _ActorMinX;
            height = _groundCheckYThreshold;

            // get scaled velocity for swept check
            float vx = _actor.Physics.VelocityX * elapsed;
            float vy = _actor.Physics.VelocityY * elapsed;

            // expand the extents
            if (vx >= 0.0f)
            {
                // grow box to the right
                width += vx;
            }
            else
            {
                // grow box to the left
                left += vx;
                width -= vx;
            }

            // grow box down based
            if (vy >= 0.0f)
            {
                height += vy;
            }
            else
            {
                height -= vy;
            }

            // create a rect for the container query to use
            _queryData.Rectangle = new RectangleF(left, top, width, height);

            // clear results from previous container query
            _containerQueryResults.Clear();

            // do the query
            SceneObject.SceneGraph.Container.FindObjects(_queryData);
        }

        /// <summary>
        /// Performs a specialized collision check against the specified object using our ground check collision polyon.
        /// </summary>
        /// <param name="obj">The object to check collision against.</param>
        /// <returns>A list of collisions that occured with the object.</returns>
        private void _testGroundPolyMove(T2DSceneObject obj)
        {
            // clear the Actor's ground collision list for the new check
            _groundCollisionList.Clear();

            // grab their collision component
            T2DCollisionComponent theirCollider = obj.Collision;

            // if it doesnt have a collision component we don't care about it
            if (theirCollider == null)
                return;

            // always check based on a full tick
            float elapsed = 0.03f;

            // vars for moving poly - poly check
            float collisionTime = 0.0f;
            Vector2 collisionNormal = Vector2.Zero;
            Vector2 collisionPoint = Vector2.Zero;
            Vector2 collisionPenetration = Vector2.Zero;

            // use a position shifted up by the height of the ground check poly
            // (the poly itself is based below the actor, so this actually puts the bottom of it at the bottom of the actor's poly)
            Vector2 ourPos = new Vector2(_actor.Position.X, _actor.Position.Y - (_groundCheckYThreshold * 4));

            // use a velocity that will allow the poly to pass from it's current position 
            // to a position exactly below where the actor's feet will be
            // (divide by threshold elapsed here to get the full distance in one tick)
            Vector2 ourVel = new Vector2(_actor.Physics.VelocityX, Math.Abs(_actor.Physics.VelocityY) + ((_groundCheckYThreshold * 4) / elapsed));

            // iterate the object's images
            for (int i = 0; i < obj.Collision.Images.Count; i++)
            {
                // grab this collision image
                T2DPolyImage theirImage = obj.Collision.Images[i] as T2DPolyImage;

                // if this isn't a poly image, skip it
                // (when new image types arise, add an if structure here)
                if (theirImage == null)
                    continue;

                // call the intersect function to do the math for us
                bool collisionTest = Collision2D.IntersectMovingPolyPoly(
                    elapsed, _GroundPolyImage.CollisionPoly, theirImage.CollisionPoly,
                    ourPos, theirImage.SceneObject.Position,
                    _GroundPolyImage.SceneObject.Rotation, theirImage.SceneObject.Rotation,
                    ourVel, Vector2.Zero,
                    ref collisionPoint, ref collisionNormal, ref collisionPenetration, ref collisionTime);

                // if the collision test came back positive...
                if (collisionTest)
                {
                    // skip platforms if the collision surface isn't facing 'upwards' at all
                    if (collisionNormal.Y >= 0)
                        continue;

                    // create a new collision info structure and add it to the list so we can return it
                    T2DCollisionInfo newInfo = new T2DCollisionInfo();
                    newInfo.Normal = collisionNormal;
                    newInfo.Penetration = collisionPenetration;
                    newInfo.Position = collisionPoint + (collisionPenetration / 2);
                    _groundCollisionList.Add(newInfo);
                }
            }
        }

        #endregion

        //======================================================
        #region Private, protected, internal fields

        // owner
        protected T2DSceneObject _actor;
        protected T2DAnimatedSprite _animatedSprite;
        protected ActorPuppetComponent _puppet;

        // movement/physics values
        protected float _gravity = 3.0f;
        protected Vector2 _maxVelocity = new Vector2(200.0f, 200.0f);
        protected float _maxMoveSpeed = 65.0f;
        protected float _groundAccel = 3.0f;
        protected float _groundDecel = 5.0f;
        protected float _airAccel = 2.0f;
        protected float _airDecel = 0.5f;
        protected float _jumpForce = 135.0f;
        protected float _climbUpSpeed = 25.0f;
        protected float _climbDownSpeed = 45.0f;
        protected float _climbTimeout = 0.25f;
        protected float _climbJumpCoefficient = 0.5f;
        protected float _groundCheckYThreshold = 0.3f;
        protected float _ladderAttachXThreshold = 1.0f;
        protected float _jumpTimeThreshold = 0.1f;
        protected float _jumpDownTimeThreshold = 0.1f;
        protected float _jumpDownTimeout = 0.18f;
        protected bool _allowJumpDown = false;
        protected bool _scaleRunAnimBySpeed = false;
        protected float _minRunAnimSpeedScale = 0.25f;
        protected float _maxGroundNormalY = -0.1f;
        protected float _groundYBuffer = 0.15f;

        // movement flags
        protected bool _moveLeft;
        protected bool _moveRight;
        protected bool _moveUp;
        protected bool _moveDown;
        protected bool _jump;
        protected bool _jumpDown;

        // physics state fields
        protected Vector2 _moveSpeed = Vector2.Zero;
        protected bool _onGround;
        protected Vector2 _previousPosition = Vector2.Zero;
        protected bool _previouslyOnGround;
        protected T2DPolyImage _groundPolyImage;
        protected T2DSceneObject _groundObject;
        protected T2DSceneObject _previousGroundObject;
        protected SolidPlatformComponent _groundObjectComponent;
        protected List<T2DSceneObject> _activePlatforms = new List<T2DSceneObject>();
        protected List<T2DSceneObject> _previouslyActivePlatforms = new List<T2DSceneObject>();
        protected float _previousTotalVelocityX;
        protected float _groundSurfaceForce;
        protected Vector2 _groundForceVector = Vector2.Zero;
        protected float _groundFriction = 1;
        protected Vector2 _platformOffset = Vector2.Zero;
        protected Vector2 _platformError = Vector2.Zero;
        protected Vector2 _groundSurfaceNormal = Vector2.Zero;
        protected Vector2 _previousSurfaceNormal = Vector2.Zero;
        protected Vector2 _groundContactPosition = Vector2.Zero;
        protected Vector2 _groundPenetration = Vector2.Zero;
        protected Vector2 _groundVelocity = Vector2.Zero;
        protected Vector2 _moveVector = Vector2.Zero;
        protected Vector2 _inheritedVelocity = Vector2.Zero;
        protected float _lastOnGroundTime;
        protected float _lastJumpEventTime;
        protected float _lastJumpDownEventTime;
        protected float _lastJumpDownTime;
        protected T2DSceneObject _ladderObject;
        protected bool _climbing;
        protected float _ladderDetatchTime;
        protected float _moveAccel;
        protected float _moveDecel;

        // health and damage
        protected float _health;
        protected float _maxHealth;
        protected float _armorModifier = 1.0f;
        protected float _damageTimeout = 1f;
        protected bool _alive = true;
        protected bool _allowRespawn = false;
        protected int _lives = 10;
        protected int _maxLives = 99;
        protected Vector2 _respawnPosition;
        protected float _lastDamageTime;

        // collision fields
        protected TorqueObjectType _collidesWith;
        protected float _actorMaxY;
        protected float _actorMaxX;
        protected float _actorMinX;

        // sound bank
        protected string _soundBank;
        protected bool _useAnimationManagerSoundEvents;
        protected bool _useAnimationStepSoundList;

        // animation manager and animations
        protected float _runAnimSpeedScale;
        protected ActorAnimationManager _animationManager;
        protected T2DAnimationData _idleAnim;
        protected T2DAnimationData _jumpAnim;
        protected T2DAnimationData _fallAnim;
        protected T2DAnimationData _runAnim;
        protected T2DAnimationData _runJumpAnim;
        protected T2DAnimationData _runFallAnim;
        protected T2DAnimationData _slideAnim;
        protected T2DAnimationData _climbIdleAnim;
        protected T2DAnimationData _climbUpAnim;
        protected T2DAnimationData _climbDownAnim;
        protected T2DAnimationData _climbJumpAnim;
        protected T2DAnimationData _actionAnim;
        protected T2DAnimationData _damageAnim;
        protected T2DAnimationData _dieAnim;

        // query fields for ground checking
        protected T2DSceneContainerQueryData _queryData = new T2DSceneContainerQueryData();
        protected List<ISceneContainerObject> _containerQueryResults = new List<ISceneContainerObject>();
        protected List<T2DCollisionInfo> _groundCollisionList = new List<T2DCollisionInfo>();

        // IFSMObject interface state fields
        protected FSMState _currentState;
        protected FSMState _previousState;

        // XML helper lists
        protected List<XMLTransitionData> _animationTransitions;

        /// <summary>
        /// The collision image used for ground checking.
        /// </summary>
        protected T2DPolyImage _GroundPolyImage
        {
            get
            {
                if (_groundPolyImage.SceneObject == null && _actor != null)
                {
                    _groundPolyImage = new T2DPolyImage(_actor);

                    float bottom = _ActorMaxY / (_actor.Size.Y / 2);
                    float left = _ActorMinX / (_actor.Size.X / 2);
                    float right = _ActorMaxX / (_actor.Size.X / 2);

                    Vector2[] groundPoly = { new Vector2(left, bottom), new Vector2(right, bottom),
                                            new Vector2(right, bottom + (_groundCheckYThreshold / (_actor.Size.Y / 2))), 
                                            new Vector2(left, bottom + (_groundCheckYThreshold / (_actor.Size.Y / 2))) };

                    _groundPolyImage.CollisionPolyBasis = groundPoly;
                }

                return _groundPolyImage;
            }
        }

        /// <summary>
        /// The lowest point on the actor's collision poly.
        /// </summary>
        protected float _ActorMaxY
        {
            get
            {
                if (_actorMaxY == 0 && _actor.Collision != null && _actor.Collision.Images.Count != 0 && (_actor.Collision.Images[0] is T2DPolyImage) && (_actor.Collision.Images[0] as T2DPolyImage).CollisionPoly.Length != 0)
                {
                    Vector2[] collisionPoly = (_actor.Collision.Images[0] as T2DPolyImage).CollisionPoly;
                    _actorMaxY = collisionPoly[0].Y;

                    foreach (Vector2 vector in collisionPoly)
                        if (vector.Y > _actorMaxY)
                            _actorMaxY = vector.Y;

                    _actorMaxY += _groundYBuffer;
                }

                return _actorMaxY;
            }
        }

        /// <summary>
        /// The rightmost point on the actor's collision poly.
        /// </summary>
        protected float _ActorMaxX
        {
            get
            {
                if (_actorMaxX == 0 && _actor.Collision != null && _actor.Collision.Images.Count != 0 && (_actor.Collision.Images[0] is T2DPolyImage) && (_actor.Collision.Images[0] as T2DPolyImage).CollisionPoly.Length != 0)
                {
                    Vector2[] collisionPoly = (_actor.Collision.Images[0] as T2DPolyImage).CollisionPoly;
                    _actorMaxX = collisionPoly[0].X;

                    foreach (Vector2 vector in collisionPoly)
                        if (vector.X > _actorMaxX)
                            _actorMaxX = vector.X;
                }

                return _actorMaxX;
            }
        }

        /// <summary>
        /// The leftmost point on the actor's collision poly.
        /// </summary>
        protected float _ActorMinX
        {
            get
            {
                if (_actorMinX == 0 && _actor.Collision != null && _actor.Collision.Images.Count != 0 && (_actor.Collision.Images[0] is T2DPolyImage) && (_actor.Collision.Images[0] as T2DPolyImage).CollisionPoly.Length != 0)
                {
                    Vector2[] collisionPoly = (_actor.Collision.Images[0] as T2DPolyImage).CollisionPoly;
                    _actorMinX = collisionPoly[0].X;

                    foreach (Vector2 vector in collisionPoly)
                        if (vector.X < _actorMinX)
                            _actorMinX = vector.X;
                }

                return _actorMinX;
            }
        }

        /// <summary>
        /// True if the Actor is currently climbing.
        /// </summary>
        protected bool _Climbing
        {
            get { return _climbing; }
            set
            {
                if (value)
                {
                    float elapsedSinceClimb = TorqueEngineComponent.Instance.TorqueTime - _ladderDetatchTime;

                    if (elapsedSinceClimb >= _climbTimeout * 1000)
                        _climbing = true;
                }
                else
                {
                    if (_climbing)
                        _ladderDetatchTime = TorqueEngineComponent.Instance.TorqueTime;

                    _climbing = false;
                }
            }
        }

        /// <summary>
        /// The scene object of the platform this Actor is currently standing on.
        /// </summary>
        protected T2DSceneObject _GroundObject
        {
            get { return _groundObject; }
        }

        /// <summary>
        /// True if the Actor should jump, if possible.
        /// </summary>
        protected bool _Jumping
        {
            get
            {
                return (_jump && TorqueEngineComponent.Instance.TorqueTime - _lastJumpEventTime <= _jumpTimeThreshold * 1000);
            }
        }

        /// <summary>
        /// True if the Actor should jump down, if possible.
        /// </summary>
        protected bool _JumpingDown
        {
            get
            {
                return (_allowJumpDown && _jumpDown && TorqueEngineComponent.Instance.TorqueTime - _lastJumpDownEventTime <= _jumpTimeThreshold * 1000);
            }
        }

        #endregion

        //======================================================
        #region Actor physics states

        /// <summary>
        /// The base class for all actor physics states. Establishes the UpdatePhysics method for specific states to override.
        /// </summary>
        protected abstract class ActorState : FSMState
        {
            abstract public void UpdatePhysics(ActorComponent actor, float elapsed);
        }

        /// <summary>
        /// A basic OnGroundState Actor physics state. Controls how an Actor's physics are modified while standing on a platform.
        /// </summary>
        protected class OnGroundState : ActorState
        {
            public override void UpdatePhysics(ActorComponent actor, float elapsed)
            {
                // update ground force based on the ground surface normal
                actor._groundForceVector = Vector2.Multiply(new Vector2(-actor._groundSurfaceNormal.Y,
                                                                        actor._groundSurfaceNormal.X), actor._groundSurfaceForce);

                // set desired ground velocity
                if (actor._GroundObject == null || actor._GroundObject.Physics == null)
                    actor._groundVelocity = actor._groundForceVector;
                else
                    actor._groundVelocity = actor._groundForceVector + actor._GroundObject.Physics.Velocity;

                // modify movement and increment inherited velocity
                actor._moveAccel = actor._groundAccel * elapsed * (float)Math.Pow(actor._groundFriction, 1.5);
                actor._moveDecel = actor._groundDecel * elapsed * (float)Math.Pow(actor._groundFriction, 1.5);

                // inherit ground velocity
                if (actor._inheritedVelocity != actor._groundVelocity)
                {
                    float incrementX = (actor._groundVelocity.X - actor._inheritedVelocity.X) * (float)Math.Pow(actor._groundFriction, 2);
                    float incrementY = (actor._groundVelocity.Y - actor._inheritedVelocity.Y) * (float)Math.Pow(actor._groundFriction, 2);

                    actor._inheritedVelocity.X += incrementX;
                    actor._inheritedVelocity.Y += incrementY;

                    if (Math.Abs(actor._groundVelocity.X - actor._inheritedVelocity.X) < actor._groundAccel * actor._groundFriction)
                        actor._inheritedVelocity.X = actor._groundVelocity.X;

                    if (Math.Abs(actor._groundVelocity.Y - actor._inheritedVelocity.Y) < actor._groundAccel * actor._groundFriction)
                        actor._inheritedVelocity.Y = actor._groundVelocity.Y;
                }

                // move in the direction specified, or slow to a stop based on damping
                if (actor._moveLeft)
                {
                    actor._moveSpeed.X -= actor._moveAccel;
                }
                else if (actor._moveRight)
                {
                    actor._moveSpeed.X += actor._moveAccel;
                }
                else
                {
                    if (Math.Abs(actor._moveSpeed.X) > actor._moveDecel)
                    {
                        int dirMod;

                        if (actor._moveSpeed.X < 0)
                            dirMod = -1;
                        else
                            dirMod = 1;

                        actor._moveSpeed.X -= actor._moveDecel * dirMod;
                    }
                    else
                    {
                        actor._moveSpeed.X = 0;
                    }
                }

                // clamp move speed to maxMoveSpeed
                actor._moveSpeed.X = MathHelper.Clamp(actor._moveSpeed.X, -actor._maxMoveSpeed, actor._maxMoveSpeed);

                // get the modified movement vector
                if (actor._groundObject != null && !actor._groundSurfaceNormal.Equals(new Vector2(0, -1)))
                {
                    // angled ground object: find walk vector
                    actor._moveVector = Vector2.Multiply(new Vector2(-actor._groundSurfaceNormal.Y,
                                                                     actor._groundSurfaceNormal.X), actor._moveSpeed.X);
                }
                else
                {
                    // no ground object or flat ground object: zero "Y walk"
                    actor._moveVector = new Vector2(actor._moveSpeed.X, 0);
                }

                // apply move vector to actor velocity
                actor._actor.Physics.Velocity = actor._moveVector;

                // apply ground force
                actor._actor.Physics.Velocity += actor._inheritedVelocity;

                // do jumping
                if (actor._JumpingDown && actor._groundObjectComponent is OneWayPlatformComponent)
                {
                    // clear the active platforms list so we stop colliding with these platforms
                    actor._activePlatforms.Clear();

                    // set the animation state
                    FSM.Instance.SetState(actor._animationManager, "fall");

                    // manually override the 'on ground' state
                    actor._onGround = false;


                    // use the jump-down event and any regular jump event
                    // (this is to specifically allow for pushy controllers to just send both jump
                    // events when a jump-down condition is met on the controller side and
                    // not have to worry about what kind of platform the actor is on.
                    // See PlayerController in the PlatformerDemo)
                    actor._jumpDown = false;
                    actor._jump = false;

                    // record this as the last time we jumped down
                    actor._lastJumpDownTime = TorqueEngineComponent.Instance.TorqueTime;
                }
                else if (actor._Jumping)
                {
                    // jump up
                    actor._actor.Physics.VelocityY = actor._groundVelocity.Y - actor._jumpForce;

                    // set the appropriate animation state for jumping
                    if (Math.Abs(actor._moveSpeed.X) < 0.01f)
                        FSM.Instance.SetState(actor._animationManager, "jump");
                    else
                        FSM.Instance.SetState(actor._animationManager, "runJump");

                    // override the 'on ground' state manually
                    // (this allows the animation manager, among other things
                    // to react to jumping properly)
                    actor._onGround = false;

                    // set actor's jump flag to false
                    // (we used the jump 'event' - let's turn it off so it's not used again)
                    // (without this, it's possible to launch yourself up crazy distances 
                    // by jumping off the ground through a ladder.. or 'something')
                    // (we also want to cancel the jump-down flag in case a lazy
                    // controller sent both events)
                    actor._jump = false;
                    actor._jumpDown = false;
                }
            }

            public override string Execute(IFSMObject obj)
            {
                ActorComponent obj2 = obj as ActorComponent;

                if (!obj2._alive)
                    return "dead";

                if (obj2._Climbing)
                    return "onLadder";
                else if (!obj2.OnGround)
                    return "inAir";

                return null;
            }

            public override void Exit(IFSMObject obj)
            {
                // make sure we have a valid actor component
                ActorComponent actor = obj as ActorComponent;

                if (actor == null)
                    return;

                // zero out move vector
                actor._moveVector = Vector2.Zero;
            }
        }

        /// <summary>
        /// A basic InAirState Actor physics state. Controls how an Actor's physics are modified while in the air.
        /// </summary>
        protected class InAirState : ActorState
        {
            public override void UpdatePhysics(ActorComponent actor, float elapsed)
            {
                // air-based control
                actor._moveAccel = actor._airAccel * elapsed;
                actor._moveDecel = actor._airDecel * elapsed;

                // move in the direction specified, or slow to a stop based on damping
                if (actor._moveLeft)
                {
                    actor._moveSpeed.X -= actor._moveAccel;
                }
                else if (actor._moveRight)
                {
                    actor._moveSpeed.X += actor._moveAccel;
                }
                else
                {
                    if (Math.Abs(actor._moveSpeed.X) > actor._moveDecel)
                    {
                        int dirMod;

                        if (actor._moveSpeed.X < 0)
                            dirMod = -1;
                        else
                            dirMod = 1;

                        // only dampen moveSpeed if the resulting speed is not faster
                        if (Math.Abs((actor._moveSpeed.X + actor._inheritedVelocity.X) - (actor._moveDecel * dirMod)) <= Math.Abs(actor._moveSpeed.X + actor._inheritedVelocity.X))
                            actor._moveSpeed.X -= actor._moveDecel * dirMod;
                    }
                    else
                    {
                        actor._moveSpeed.X = 0;
                    }
                }

                // clamp move speed to maxMoveSpeed
                if (actor._inheritedVelocity.X > 0)
                    actor._moveSpeed.X = MathHelper.Clamp(actor._moveSpeed.X, -actor._maxMoveSpeed - actor._inheritedVelocity.X, actor._maxMoveSpeed);
                else
                    actor._moveSpeed.X = MathHelper.Clamp(actor._moveSpeed.X, -actor._maxMoveSpeed, actor._maxMoveSpeed - actor._inheritedVelocity.X);

                // apply x velocity to actor
                actor._actor.Physics.VelocityX = actor._moveSpeed.X;

                // apply X ground force
                actor._actor.Physics.VelocityX += actor._inheritedVelocity.X;

                // apply gravity
                actor._actor.Physics.VelocityY += actor.Gravity * elapsed;
            }

            public override void Enter(IFSMObject obj)
            {
                base.Enter(obj);

                ActorComponent actor = obj as ActorComponent;

                if (actor == null)
                    return;
            }

            public override string Execute(IFSMObject obj)
            {
                ActorComponent obj2 = obj as ActorComponent;

                if (!obj2._alive)
                    return "dead";

                if (obj2._Climbing)
                    return "onLadder";
                else if (obj2.OnGround)
                    return "onGround";

                return null;
            }
        }

        /// <summary>
        /// A basic OnLadderState Actor physics state. Controls how an Actor's physics are modified while on a ladder.
        /// </summary>
        protected class OnLadderState : ActorState
        {
            public override void UpdatePhysics(ActorComponent actor, float elapsed)
            {
                // get desired inherited velocity
                if (actor._ladderObject == null || actor._ladderObject.Physics == null)
                    actor._groundVelocity = Vector2.Zero;
                else
                    actor._groundVelocity = actor._ladderObject.Physics.Velocity;

                // inherit groundVelocity directly
                if (actor._inheritedVelocity.X != actor._groundVelocity.X)
                    actor._inheritedVelocity.X = actor._groundVelocity.X;

                // interpret vertical movement flags
                if (actor._moveUp)
                    actor._moveSpeed.Y = -actor._climbUpSpeed;
                else if (actor._moveDown)
                    actor._moveSpeed.Y = actor._climbDownSpeed;
                else
                    actor._moveSpeed.Y = 0;

                // jump only when holding right or left
                if (actor._Jumping || actor._jumpDown)
                {
                    if (actor._moveLeft || actor._moveRight)
                    {
                        actor._actor.Physics.VelocityY -= actor._jumpForce * actor._climbJumpCoefficient;

                        if (actor._moveLeft)
                        {
                            actor._moveSpeed.X = -actor._maxMoveSpeed * actor._climbJumpCoefficient;
                            actor._actor.Position = new Vector2(actor._actor.Position.X - actor._ladderAttachXThreshold, actor._actor.Position.Y);

                            // set the appropriate animation state for jumping off a ladder
                            FSM.Instance.SetState(actor._animationManager, "climbJump");
                        }
                        else if (actor._moveRight)
                        {
                            actor._moveSpeed.X = actor._maxMoveSpeed * actor._climbJumpCoefficient;
                            actor._actor.Position = new Vector2(actor._actor.Position.X + actor._ladderAttachXThreshold, actor._actor.Position.Y);

                            // set the appropriate animation state for jumping off a ladder
                            FSM.Instance.SetState(actor._animationManager, "climbJump");
                        }
                    }

                    // apply X inherited force from ladder
                    actor._actor.Physics.VelocityX = actor._inheritedVelocity.X + actor._moveSpeed.X;
                    actor._Climbing = false;

                    // set actor's jump flag to false
                    // (we used the jump 'event' - let's turn it off so it's not used again)
                    // (without this, it's possible to launch yourself up crazy distances 
                    // by jumping off the ground through a ladder.. or something)
                    actor._jump = false;
                }
                else
                {
                    // apply ladder's vel to the player
                    actor._actor.Physics.VelocityX = actor._groundVelocity.X;
                    actor._actor.Physics.VelocityY = actor._groundVelocity.Y + actor._moveSpeed.Y;
                }
            }

            public override void Enter(IFSMObject obj)
            {
                ActorComponent actor = obj as ActorComponent;

                // make sure there's an actor... >_<
                if (actor == null)
                    return;

                // reset moveSpeed.Y
                actor._moveSpeed.X = 0;
            }

            public override string Execute(IFSMObject obj)
            {
                ActorComponent obj2 = obj as ActorComponent;

                if (!obj2._alive)
                    return "dead";

                if (!obj2._Climbing)
                {
                    if (obj2.OnGround)
                        return "onGround";
                    else
                        return "inAir";
                }

                return null;

            }

            public override void Exit(IFSMObject obj)
            {
                ActorComponent actor = obj as ActorComponent;

                // reset moveSpeed.Y
                actor._moveSpeed.Y = 0;
            }
        }

        /// <summary>
        /// A basic DeadState Actor physics state. Controls how an Actor's physics are modified while dead.
        /// </summary>
        protected class DeadState : ActorState
        {
            public override void UpdatePhysics(ActorComponent actor, float elapsed)
            {
                actor._actor.Physics.Velocity = Vector2.Zero;
            }

            public override string Execute(IFSMObject obj)
            {
                ActorComponent obj2 = obj as ActorComponent;

                if (obj2.Alive)
                    return "onGround";

                return null;
            }
        }

        #endregion
    }
}