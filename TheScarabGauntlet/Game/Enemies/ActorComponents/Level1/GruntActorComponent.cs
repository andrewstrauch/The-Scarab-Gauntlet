using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

using GarageGames.Torque.Core;
using GarageGames.Torque.T2D;
using GarageGames.Torque.XNA;
using GarageGames.Torque.GUI;
using GarageGames.Torque.GameUtil;
using GarageGames.Torque.PlatformerFramework;

namespace PlatformerStarter.Enemies.ActorComponents
{
    [TorqueXmlSchemaType]
    public class GruntActorComponent : EnemyActorComponent
    {
        #region Private Members
        private T2DAnimationData attackAnim;
        private T2DSceneObject attackMeleeTemplate;
        private EnemyAttackCollisionComponent enemyAttackComponent;
        private Timer meleeTimer;
        private float coolDown;
        private float attackSpeed;
        #endregion

        #region Public Properties
        public T2DAnimationData AttackAnim
        {
            get { return attackAnim; }
            set { attackAnim = value; }
        }
        [TorqueXmlSchemaType(DefaultValue = "500")]
        public float CoolDown
        {
            get { return coolDown; }
            set { coolDown = value; }
        }
        public T2DSceneObject AttackObjectTemplate
        {
            get { return attackMeleeTemplate; }
            set { attackMeleeTemplate = value; }
        }
        [TorqueXmlSchemaType(DefaultValue = "25")]
        public float AttackSpeed
        {
            get { return attackSpeed; }
            set { attackSpeed = value; }
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

            GruntActorComponent obj2 = obj as GruntActorComponent;

            obj2.AttackAnim = AttackAnim;
            obj2.CoolDown = CoolDown;
            obj2.AttackObjectTemplate = AttackObjectTemplate;
            obj2.AttackSpeed = AttackSpeed;
        }

        /// <summary>
        /// Runs the logic for the grunt to attack.
        /// </summary>
        public override void Attack()
        {
            if (!Alive)
                return;

            ActionAnim = attackAnim;
            
            // switch to the "action" state
            FSM.Instance.SetState(_animationManager, "attack");

            ApplyMovement();

            ApplyAttack();

        }

        /// <summary>
        /// Mounts or dismounts the attack object based on the current frame of the animation.
        /// </summary>
        public void CheckAttackFrame()
        {
            if (enemyAttackComponent != null)
            {
                if (AnimatedSprite.CurrentFrame == enemyAttackComponent.MountFrame)
                    enemyAttackComponent.MountAttack();
                else if (AnimatedSprite.CurrentFrame == enemyAttackComponent.DismountFrame)
                    enemyAttackComponent.DismountAttack();
            }
        }

        #endregion

        #region Private Routines
        /// <summary>
        /// Applies the necessary movement while in the attack state.
        /// </summary>
        private void ApplyMovement()
        {
            // if the grunt is hopping, we need to make sure he's still moving left/right
            // while in the air
            Vector2 direction = _getDistanceToPlayer();

            if (direction.X >= 0)
                MoveLeft();
            else
                MoveRight();

            // makes the grunt do a little hop
            Jump();

            if (!OnGround)
                this.HorizontalStop();
        }

        /// <summary>
        /// Applies the necessary attack operations while in the attack state.
        /// </summary>
        private void ApplyAttack()
        {
            T2DSceneObject attackObject = attackMeleeTemplate.Clone() as T2DSceneObject;
            Vector2 attackVelocity = new Vector2(attackSpeed, 0);

            if (attackObject != null)
            {
                attackObject.Position = SceneObject.Position;

                // Flip the damage poly if the enemy is facing the other way (aka left)
                attackObject.FlipX = AnimatedSprite.FlipX;

                if (AnimatedSprite.FlipX)
                    attackVelocity *= -1;

                attackObject.Physics.Velocity = attackVelocity;

                TorqueObjectDatabase.Instance.Register(attackObject);
                enemyAttackComponent = attackObject.Components.FindComponent<EnemyAttackCollisionComponent>();
            }

            readyToAttack = false;
        }

        /// <summary>
        /// Executes needed game logic before the main logic for this component.
        /// </summary>
        /// <param name="elapsed">The time elapsed from the last clock pulse.</param>
        protected override void _preUpdate(float elapsed)
        {
            base._preUpdate(elapsed);

            if (meleeTimer.Expired)
            {
                readyToAttack = true;
                meleeTimer.Reset();
            }
        }

        /// <summary>
        /// Initializes the component when the scene object is created/registered.
        /// </summary>
        /// <param name="owner">The scene object that is created.</param>
        /// <returns>False if the registration failed, true otherwise.</returns>
        protected override bool _OnRegister(TorqueObject owner)
        {
            if (!base._OnRegister(owner))
                return false;

            meleeTimer = new Timer("gruntMeleeTimer");
            meleeTimer.MillisecondsUntilExpire = coolDown;

            //animations = new Animations();

            return true;
        }

        /// <summary>
        /// Creates a new instance of the animation manager.
        /// </summary>
        protected override void _createAnimationManager()
        {
            _animationManager = new GruntActorAnimationManager(this);
        }

        protected override void _initAnimationManager()
        {
            _soundBank = "grunt";
            _useAnimationManagerSoundEvents = true;
            //_animationManager.SetSoundEvent(RunAnim, "walk");
            _animationManager.SetSoundEvent(AttackAnim, "attack");
            _animationManager.SetSoundEvent(DieAnim, "death");
        }

        #endregion

        #region Animation Manager
        public class GruntActorAnimationManager : ActorAnimationManager
        {
            private GruntActorComponent actorComponent;
            private AIChaseController controller;
            private Cue currentSound;

            public GruntActorAnimationManager(GruntActorComponent actorComponent)
                : base(actorComponent)
            {
                this.actorComponent = actorComponent;
                this.controller = actorComponent.Controller as AIChaseController;
            }

            protected override void _registerAnimStates()
            {
                FSM.Instance.RegisterState<IdleState>(this, "idle");
                FSM.Instance.RegisterState<RunState>(this, "run");
                FSM.Instance.RegisterState<AttackState>(this, "attack");
                FSM.Instance.RegisterState<DieState>(this, "die");

                _currentState = FSM.Instance.GetState(this, "idle");
            }

            new public class IdleState : ActorAnimationManager.IdleState
            {
                public override void Enter(IFSMObject obj)
                {
                    base.Enter(obj);

                    GruntActorAnimationManager actorAnimMgr = obj as GruntActorAnimationManager;

                    if (actorAnimMgr.actorComponent == null)
                        return;
                }

                public override string Execute(IFSMObject obj)
                {
                    GruntActorAnimationManager actorAnimMgr = obj as GruntActorAnimationManager;

                    if (actorAnimMgr.actorComponent == null)
                        return null;

                    if (!actorAnimMgr.actorComponent._alive)
                        return "die";
                    
                   // Uncomment if we include a fall animation for the grunt
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

                    GruntActorAnimationManager actorAnimMgr = obj as GruntActorAnimationManager;

                    if (actorAnimMgr.actorComponent == null)
                        return;

                    actorAnimMgr._transitioningTo = actorAnimMgr.actorComponent.RunAnim;

                    if (!actorAnimMgr._transitioning)
                        actorAnimMgr._playAnimation(actorAnimMgr._transitioningTo);

                    if (actorAnimMgr.actorComponent._scaleRunAnimBySpeed)
                        actorAnimMgr.actorComponent.AnimatedSprite.AnimationTimeScale = actorAnimMgr.actorComponent._runAnimSpeedScale / actorAnimMgr.actorComponent._maxMoveSpeed;

                    actorAnimMgr.currentSound = SoundManager.Instance.PlaySound("grunt", "walk");
                }

                public override string Execute(IFSMObject obj)
                {
                    GruntActorAnimationManager actorAnimMgr = obj as GruntActorAnimationManager;

                    if (actorAnimMgr.actorComponent == null)
                        return null;
                    
                    if (!actorAnimMgr.actorComponent._alive)
                        return "die";

                    if ((!actorAnimMgr.actorComponent._scaleRunAnimBySpeed && !(actorAnimMgr.actorComponent._moveLeft || actorAnimMgr.actorComponent._moveRight))
                        || (actorAnimMgr.actorComponent._scaleRunAnimBySpeed && actorAnimMgr.actorComponent._runAnimSpeedScale <= (actorAnimMgr.actorComponent._minRunAnimSpeedScale * actorAnimMgr.actorComponent._maxMoveSpeed)))
                        return "idle";

                    return null;
                }

                public override void Exit(IFSMObject obj)
                {
                    base.Exit(obj);

                    GruntActorAnimationManager actorAnimMgr = obj as GruntActorAnimationManager;
                    
                    if(actorAnimMgr.currentSound.IsPlaying)
                        actorAnimMgr.currentSound.Stop(AudioStopOptions.Immediate);
                }
            }

            public class AttackState : ActionState
            {
                public override void Enter(IFSMObject obj)
                {
                    base.Enter(obj);
                }

                public override string Execute(IFSMObject obj)
                {
                    base.Execute(obj);

                    GruntActorAnimationManager actorAnimMgr = obj as GruntActorAnimationManager;

                    if (actorAnimMgr.actorComponent == null)
                        return null;

                    if (!actorAnimMgr.actorComponent.Alive)
                        return "die";

                    actorAnimMgr.actorComponent.CheckAttackFrame();

                    if (!actorAnimMgr.actorComponent.AnimatedSprite.IsAnimationPlaying)
                        return "idle";

                    return null;
                }

                public override void Exit(IFSMObject obj)
                {
                    base.Exit(obj);

                    GruntActorAnimationManager actorAnimMgr = obj as GruntActorAnimationManager;

                    if (actorAnimMgr.actorComponent == null)
                        return;

                    actorAnimMgr.actorComponent.meleeTimer.Start();
                }
            }
        }
        #endregion
    }
}
