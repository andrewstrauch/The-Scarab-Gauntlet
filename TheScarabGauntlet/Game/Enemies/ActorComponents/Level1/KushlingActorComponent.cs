using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework;

using GarageGames.Torque.Core;
using GarageGames.Torque.Sim;
using GarageGames.Torque.T2D;
using GarageGames.Torque.SceneGraph;
using GarageGames.Torque.MathUtil;

using GarageGames.Torque.PlatformerFramework;

namespace PlatformerStarter.Enemies.ActorComponents.Level1
{
    [TorqueXmlSchemaType]
    public class KushlingActorComponent : EnemyActorComponent
    {
        #region Private Members
        private int damage;
        #endregion

        #region Public Properties
        public int Damage
        {
            get { return damage; }
            set { damage = value; }
        }
        public static T2DOnCollisionDelegate KushlingCollision
        {
            get { return KushCollision; }
        }
        #endregion

        #region Public Routines
        /// <summary>
        /// Copies all exposed variables to the spawned instance of the scene object.
        /// </summary>
        /// <param name="obj">The object to be copied.</param>
        public override void CopyTo(TorqueComponent obj)
        {
            base.CopyTo(obj);

            KushlingActorComponent obj2 = obj as KushlingActorComponent;

            obj2.Damage = Damage;
        }


        #endregion

        #region Private Routines
        /// <summary>
        /// Initializes the component when the scene object is created/registered.
        /// </summary>
        /// <param name="owner">The scene object that is created.</param>
        /// <returns>False if the registration failed, true otherwise.</returns>
        protected override bool _OnRegister(TorqueObject owner)
        {
            if (!base._OnRegister(owner))
                return false;

            SceneObject.Collision.CollidesWith += PlatformerData.PlayerObjectType;
            SceneObject.Collision.OnCollision = KushlingCollision;

            return true;
        }

        /// <summary>
        /// Creates a new instance of the animation manager.
        /// </summary>
        protected override void _createAnimationManager()
        {
            _animationManager = new KushlingActorAnimationManager(this);
        }

        protected override void _initAnimationManager()
        {
            _soundBank = "kushling";
            _useAnimationManagerSoundEvents = true;
            _animationManager.SetSoundEvent(IdleAnim, "idle");
            _animationManager.SetSoundEvent(RunAnim, "attack");
            _animationManager.SetSoundEvent(DieAnim, "death");
        }

        /// <summary>
        /// T2DOnCollision delegate to handle damage between the kushling and the player
        /// </summary>
        /// <param name="myObject">The kushling</param>
        /// <param name="theirObject">The collided object.</param>
        /// <param name="info">Collision information</param>
        /// <param name="resolve">How the collision will be resolved</param>
        /// <param name="physicsMaterial">The type of material that would affect the physics</param>
        public static void KushCollision(T2DSceneObject myObject, T2DSceneObject theirObject,
            T2DCollisionInfo info, ref T2DResolveCollisionDelegate resolve, ref T2DCollisionMaterial physicsMaterial)
        {
            int damage = myObject.Components.FindComponent<KushlingActorComponent>().Damage;

            if (theirObject.TestObjectType(PlatformerData.ActorObjectType))
            {
                ActorComponent actor = theirObject.Components.FindComponent<ActorComponent>();

                // Deal damage to the enemy
                if (actor != null)
                    actor.TakeDamage(damage, myObject, true, true);
            }

            else if(theirObject.TestObjectType(PlatformerData.EnemyObjectType))
            {
                resolve = T2DPhysicsComponent.ClampCollision;
            }
        }

        #endregion

        #region Animation Manager
        public class KushlingActorAnimationManager : ActorAnimationManager
        {
            private KushlingActorComponent actorComponent;
            private AIChaseController controller;

            public KushlingActorAnimationManager(KushlingActorComponent actorComponent)
                : base(actorComponent)
            {
                this.actorComponent = actorComponent;
                this.controller = actorComponent.Controller as AIChaseController;
            }

            protected override void _registerAnimStates()
            {
                FSM.Instance.RegisterState<IdleState>(this, "idle");
                FSM.Instance.RegisterState<RunState>(this, "run");
                FSM.Instance.RegisterState<DieState>(this, "die");

                _currentState = FSM.Instance.GetState(this, "idle");
            }

            new public class IdleState : ActorAnimationManager.IdleState
            {
                public override void Enter(IFSMObject obj)
                {
                    base.Enter(obj);

                    KushlingActorAnimationManager actorAnimMgr = obj as KushlingActorAnimationManager;

                    if (actorAnimMgr.actorComponent == null)
                        return;

                    actorAnimMgr.actorComponent._useAnimationManagerSoundEvents = false;
                }

                public override string Execute(IFSMObject obj)
                {
                    KushlingActorAnimationManager actorAnimMgr = obj as KushlingActorAnimationManager;

                    if (actorAnimMgr.actorComponent == null)
                        return null;

                    if (!actorAnimMgr.actorComponent._alive)
                        return "die";

                    // Uncomment if we include a fall animation
                    /* if(actorAnimMgr.FallingFromGround)
                         if(actorAnimMgr.actorComponent.Actor.Physics.VelocityY > 0)
                             return "fall";
                     */
                    if (actorAnimMgr.actorComponent._moveLeft || actorAnimMgr.actorComponent._moveRight)
                        return "run";

                    return null;
                }
            }

            new public class RunState : ActorAnimationManager.RunState
            {
                public override void Enter(IFSMObject obj)
                {
                    base.Enter(obj);

                    KushlingActorAnimationManager actorAnimMgr = obj as KushlingActorAnimationManager;

                    if (actorAnimMgr.actorComponent == null)
                        return;

                    actorAnimMgr._transitioningTo = actorAnimMgr.actorComponent.RunAnim;

                    if (!actorAnimMgr._transitioning)
                        actorAnimMgr._playAnimation(actorAnimMgr._transitioningTo);

                    if (actorAnimMgr.actorComponent._scaleRunAnimBySpeed)
                        actorAnimMgr.actorComponent.AnimatedSprite.AnimationTimeScale = actorAnimMgr.actorComponent._runAnimSpeedScale / actorAnimMgr.actorComponent._maxMoveSpeed;

                    actorAnimMgr.actorComponent._useAnimationManagerSoundEvents = false;
                }

                public override string Execute(IFSMObject obj)
                {
                    KushlingActorAnimationManager actorAnimMgr = obj as KushlingActorAnimationManager;

                    if (actorAnimMgr.actorComponent == null)
                        return null;

                    if (!actorAnimMgr.actorComponent._alive)
                        return "die";

                    if ((!actorAnimMgr.actorComponent._scaleRunAnimBySpeed && !(actorAnimMgr.actorComponent._moveLeft || actorAnimMgr.actorComponent._moveRight))
                        || (actorAnimMgr.actorComponent._scaleRunAnimBySpeed && actorAnimMgr.actorComponent._runAnimSpeedScale <= (actorAnimMgr.actorComponent._minRunAnimSpeedScale * actorAnimMgr.actorComponent._maxMoveSpeed)))
                        return "idle";

                    return null;
                }
            }
        }
        #endregion
    }
}
