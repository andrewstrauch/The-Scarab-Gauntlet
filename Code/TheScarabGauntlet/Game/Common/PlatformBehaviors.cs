using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework;

using GarageGames.Torque.Core;
using GarageGames.Torque.T2D;
using GarageGames.Torque.Sim;
using GarageGames.Torque.PlatformerFramework;

namespace PlatformerStarter
{
    #region Bouncing Platform
    /// <summary>
    /// A platform behavior that tells Dragons that land on the platform to bounce.
    /// </summary>
    /*[TorqueXmlSchemaType]
    class TrampolinePlatformBehavior : PlatformBehavior
    {
        //======================================================
        #region Public properties, operators, constants, and enums

        [TorqueXmlSchemaType(DefaultValue = "100")]
        public float BounceMagnitude
        {
            get { return _bounceMagnitude; }
            set { _bounceMagnitude = value; }
        }

        [TorqueXmlSchemaType(DefaultValue = "150")]
        public float JumpBounceMagnitude
        {
            get { return _jumpBounceMagnitude; }
            set { _jumpBounceMagnitude = value; }
        }

        #endregion

        //======================================================
        #region Public methods

        public override void ActorLanded(ActorComponent actor)
        {
            base.ActorLanded(actor);

            PlayerActorComponent dragonActor = actor as PlayerActorComponent;

            if (dragonActor != null)
                dragonActor.Bounce(this);

            T2DAnimatedSprite sprite = _platform as T2DAnimatedSprite;

            if (sprite != null)
                sprite.PlayAnimation();
        }

        public override void CopyTo(TorqueComponent obj)
        {
            base.CopyTo(obj);

            TrampolinePlatformBehavior obj2 = (TrampolinePlatformBehavior)obj;

            obj2.BounceMagnitude = BounceMagnitude;
            obj2.JumpBounceMagnitude = JumpBounceMagnitude;
        }

        #endregion

        //======================================================
        #region Private, protected, internal fields

        private float _bounceMagnitude = 200;
        private float _jumpBounceMagnitude = 250;

        #endregion
    }*/
    #endregion

    /// <summary>
    /// A platform behavior that makes the platform fall from under an Actor after a set timeout.
    /// </summary>
    [TorqueXmlSchemaType]
    class FallingPlatformBehavior : PlatformBehavior, ITickObject
    {
        //======================================================
        #region Public properties, operators, constants, and enums

        [TorqueXmlSchemaType(DefaultValue = "2")]
        public float Timeout
        {
            get { return _timeout; }
            set { _timeout = value; }
        }

        [TorqueXmlSchemaType(DefaultValue = "6")]
        public float Gravity
        {
            get { return _gravity; }
            set { _gravity = value; }
        }

        public T2DAnimationData TriggeredAnimation
        {
            get { return _triggeredAnimation; }
            set { _triggeredAnimation = value; }
        }

        [TorqueXmlSchemaType(DefaultValue = "5")]
        public float DeleteAfterFallTimeout
        {
            get { return _deleteAfterFallTimeout; }
            set { _deleteAfterFallTimeout = value; }
        }

        [TorqueXmlSchemaType(DefaultValue = "0")]
        public bool AutoRecover
        {
            get { return _autoRecover; }
            set { _autoRecover = value; }
        }

        #endregion

        //======================================================
        #region Public methods

        public override void ActorLanded(ActorComponent actor)
        {
            base.ActorLanded(actor);

            T2DAnimatedSprite animSprite = Owner as T2DAnimatedSprite;
            if (animSprite != null && !_fallTriggered)
            {
                animSprite.PlayAnimation(_triggeredAnimation);
                animSprite.AnimationTimeScale = _timeout;
            }

            _fallTriggered = true;
        }

        public virtual void ProcessTick(Move move, float elapsed)
        {
            if (!_fallTriggered)
                return;

            _totalElapsed += elapsed;

            if (_totalElapsed >= _timeout)
                _platform.Physics.VelocityY += _gravity * elapsed * 100;

            if (!(Owner.IsRegistered && _totalElapsed >= _timeout + _deleteAfterFallTimeout))
                return;

            _platformComponent.PlatformEnabled = false;

            if (!Owner.TestObjectType(PlatformerData.SpawnedObjectType))
            {
                TorqueObjectDatabase.Instance.Unregister(Owner);
                return;
            }

            SpawnedObjectComponent spawnedObject = Owner.Components.FindComponent<SpawnedObjectComponent>();

            if (_autoRecover && spawnedObject != null)
                spawnedObject.SpawnPoint.ResetSpawnPoint();
            else
                TorqueObjectDatabase.Instance.Unregister(Owner);
        }

        public virtual void InterpolateTick(float k) { }

        public override void CopyTo(TorqueComponent obj)
        {
            base.CopyTo(obj);
            FallingPlatformBehavior obj2 = obj as FallingPlatformBehavior;

            obj2.Timeout = Timeout;
            obj2.Gravity = Gravity;
            obj2.TriggeredAnimation = TriggeredAnimation;
            obj2.DeleteAfterFallTimeout = DeleteAfterFallTimeout;
            obj2.AutoRecover = AutoRecover;
        }

        #endregion

        //======================================================
        #region Private, protected, internal methods

        protected override bool _OnRegister(TorqueObject owner)
        {
            // call base
            if (!base._OnRegister(owner))
                return false;

            // register with the process list so we get a tick
            ProcessList.Instance.AddTickCallback(owner, this);

            return true;
        }

        #endregion

        //======================================================
        #region Private, protected, internal fields

        private bool _fallTriggered = false;
        private float _timeout = 2;
        private float _totalElapsed = 0;
        private float _gravity = 6;
        private T2DAnimationData _triggeredAnimation;
        private float _deleteAfterFallTimeout = 5;
        private bool _autoRecover = false;

        #endregion
    }

    /// <summary>
    /// A PlatformBehavior that tells a PlatformMoveComponent to begin moving when an actor lands on it.
    /// </summary>
    [TorqueXmlSchemaType]
    [TorqueXmlSchemaDependency(Type = typeof(PlatformMoveComponent))]
    class StartMoveOnLandPlatformBehavior : PlatformBehavior
    {
        public override void ActorLanded(ActorComponent actor)
        {
            base.ActorLanded(actor);

            if (!actor.Actor.TestObjectType(PlatformerData.PlayerObjectType))
                return;

            PlatformMoveComponent mover = _platform.Components.FindComponent<PlatformMoveComponent>();

            if (mover == null)
                return;

            if (!mover.IsRunning)
                mover.Start();
        }
    }

    #region Substance Platforms
    /// <summary>
    /// PlatformBehavior to notify a Dragon when to change it's step sound to the grass sound.
    /// </summary>
    /*[TorqueXmlSchemaType]
    class GrassMaterialPlatformBehavior : PlatformBehavior
    {
        //======================================================
        #region Public methods

        public override void ActorLanded(ActorComponent actor)
        {
            base.ActorLanded(actor);

            PlayerActorComponent dragon = actor as PlayerActorComponent;

            if (dragon != null)
                dragon.OnGrassMaterial();
        }

        #endregion
    }

    /// <summary>
    /// PlatformBehavior to notify a Dragon when to change it's step sound to the dirt sound.
    /// </summary>
    [TorqueXmlSchemaType]
    class DirtMaterialPlatformBehavior : PlatformBehavior
    {
        //======================================================
        #region Public methods

        public override void ActorLanded(ActorComponent actor)
        {
            base.ActorLanded(actor);

            PlayerActorComponent dragon = actor as PlayerActorComponent;

            if (dragon != null)
                dragon.OnDirtMaterial();
        }

        #endregion
    }

    /// <summary>
    /// PlatformBehavior to notify a Dragon when to change it's step sound to the wood sound.
    /// </summary>
    [TorqueXmlSchemaType]
    class WoodMaterialPlatformBehavior : PlatformBehavior
    {
        //======================================================
        #region Public methods

        public override void ActorLanded(ActorComponent actor)
        {
            base.ActorLanded(actor);

            PlayerActorComponent dragon = actor as PlayerActorComponent;

            if (dragon != null)
                dragon.OnWoodMaterial();
        }

        #endregion
    }*/
    #endregion
}
