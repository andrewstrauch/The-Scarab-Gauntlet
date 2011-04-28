using System;

using Microsoft.Xna.Framework;

using GarageGames.Torque.Core;
using GarageGames.Torque.Sim;
using GarageGames.Torque.T2D;
using GarageGames.Torque.SceneGraph;
using GarageGames.Torque.MathUtil;

using GarageGames.Torque.PlatformerFramework;

using Scripting;

namespace PlatformerStarter.Enemies.ActorComponents.Level1
{
    public class BairneAnimations
    {
        public T2DAnimationData KushLaunchAnim;
        public T2DAnimationData DeathAnim;
        public T2DAnimationData SwipeAnim;
        public T2DAnimationData VineLaunchAnim;
    }

    [TorqueXmlSchemaType]
    public class BairneActorComponent : EnemyActorComponent
    {
        #region Private Members

        private BairneAnimations animations = new BairneAnimations();
        private WeaponComponent kushWeaponComponent;
        private WeaponComponent vineWeaponComponent;
        private T2DSceneObject kushWeapon;
        private T2DSceneObject vineWeapon;
        private Timer coolDownTimer;
        private Timer idleTimer;
        private string weaponType;
        private float actionCoolDown;
        private float idleCoolDown;
        private int launchFrame;
        private bool launched;
        private bool onLeft;
        private bool reachedBoundary;

        #endregion

        #region Public Properties

        public BairneAnimations Animations
        {
            get { return animations; }
            set { animations = value; }
        }

        public float ActionCoolDown
        {
            get { return actionCoolDown; }
            set { actionCoolDown = value; }
        }

        public float IdleCoolDown
        {
            get { return idleCoolDown; }
            set { idleCoolDown = value; }
        }

        public T2DSceneObject KushlingWeapon
        {
            get { return kushWeapon; }
            set { kushWeapon = value; }
        }

        public T2DSceneObject VineWeapon
        {
            get { return vineWeapon; }
            set { vineWeapon = value; }
        }

        public T2DSceneObject SceneObject
        {
            get { return Owner as T2DSceneObject; }
        }

        [System.Xml.Serialization.XmlIgnore]
        public bool OnLeft
        {
            get { return onLeft; }
            set { onLeft = value; }
        }

        [System.Xml.Serialization.XmlIgnore]
        public bool ReachedBound
        {
            get { return reachedBoundary; }
            set { reachedBoundary = value; }
        }

        #endregion

        #region Public Routines

        public override void CopyTo(TorqueComponent obj)
        {
            base.CopyTo(obj);

            BairneActorComponent obj2 = obj as BairneActorComponent;
            obj2.Animations = Animations;
            obj2.ActionCoolDown = ActionCoolDown;
            obj2.IdleCoolDown = IdleCoolDown;
            obj2.KushlingWeapon = KushlingWeapon;
            obj2.VineWeapon = VineWeapon;
        }

        public override void Attack()
        {
        }

        [LuaFuncAttr("Swipe", "Forces Bairne to swipe.")]
        public void Swipe()
        {
            if (!Alive)
                return;

            if (!coolDownTimer.Running)
            {
                ActionAnim = Animations.SwipeAnim;
                FSM.Instance.SetState(_animationManager, "swipe");
                coolDownTimer.Start();
            }
        }

        [LuaFuncAttr("VineLaunch", "Forces Bairned to launch vines.")]
        public void VineLaunch(int launchFrame)
        {
            if (!Alive)
                return;

            if (!coolDownTimer.Running)
            {
                this.launchFrame = launchFrame;
                weaponType = "vine";
                ActionAnim = Animations.VineLaunchAnim;
                FSM.Instance.SetState(_animationManager, "vine_launch");
                coolDownTimer.Start();
            }
        }

        [LuaFuncAttr("KushlingLaunch", "Forces Bairne to launch kushlings")]
        public void KushlingLaunch(int launchFrame)
        {
            if (!Alive)
                return;

            if (!coolDownTimer.Running)
            {
                this.launchFrame = launchFrame;
                weaponType = "kushling";
                ActionAnim = Animations.KushLaunchAnim;
                FSM.Instance.SetState(_animationManager, "kush_launch");
                coolDownTimer.Start();
            }
        }

        [LuaFuncAttr("Run", "Forces Bairne into a run state.")]
        public void Run()
        {
            if (!Alive)
                return;

            if (!coolDownTimer.Running && reachedBoundary)
            {
                ActionAnim = RunAnim;
                FSM.Instance.SetState(_animationManager, "run");
                reachedBoundary = false;
            }
            
            if(!reachedBoundary)
            {
                if (onLeft)
                    MoveRight();
                else
                    MoveLeft();
                coolDownTimer.Start();
            }
        }

        [LuaFuncAttr("Idle", "Forces Bairne into an idle state.")]
        public void Idle(float idleTime)
        {
            if (!Alive)
                return;

            if (!idleTimer.Running)
            {
                idleTimer.MillisecondsUntilExpire = idleTime;
                ActionAnim = IdleAnim;
                FSM.Instance.SetState(_animationManager, "idle");
                idleTimer.Start();
            }

        }

        [LuaFuncAttr("ReadyToAttack", "Checks whether Bairne is ready to attack or not.")]
        public bool ReadyToAttack()
        {
            return coolDownTimer.Expired;
        }

        [LuaFuncAttr("ReachedBoundary", "Checks whether Bairne has reached the boundary during his run attack.")]
        public bool ReachedBoundary()
        {
            return reachedBoundary;
        }

        public void Die()
        {
            ActionAnim = DieAnim;
            FSM.Instance.SetState(_animationManager, "die");
        }

        /// <summary>
        /// Checks whether Bairne can start shooting shit at Amanda.  P.S:  It's frame-based.
        /// </summary>
        /// <returns>True if it's shoosting time, false otherwise.</returns>
        public bool CanShoot()
        {
            if (!launched)
                return (AnimatedSprite.CurrentFrame == launchFrame);
            else
                return false;
        }

        #endregion

        #region Private Routines

        protected override bool _OnRegister(TorqueObject owner)
        {
            if (!base._OnRegister(owner))
                return false;

            coolDownTimer = new Timer();
            idleTimer = new Timer();
            coolDownTimer.MillisecondsUntilExpire = actionCoolDown;
            idleTimer.MillisecondsUntilExpire = 2000;
            launchFrame = 0;
            launched = false;
            onLeft = false;
            reachedBoundary = true;

            if (kushWeapon != null)
                kushWeaponComponent = kushWeapon.Components.FindComponent<WeaponComponent>();
            else
                ; //Log Error and shit

            if (vineWeapon != null)
                vineWeaponComponent = vineWeapon.Components.FindComponent<WeaponComponent>();
            else
                ; //Again, log some errorz

            ((LuaAIController)Controller).Init(this);

            return true;
        }

        protected override void _OnUnregister()
        {
            base._OnUnregister();
        }

        protected override void _preUpdate(float elapsed)
        {
            base._preUpdate(elapsed);

            if (coolDownTimer.Expired)
            {
                coolDownTimer.Reset();
                coolDownTimer.MillisecondsUntilExpire = actionCoolDown;
                
                if (launched)
                    launched = false;
            }
        }

        /// <summary>
        /// Creates a new instance of the animation manager.
        /// </summary>
        protected override void _createAnimationManager()
        {
            _animationManager = new BairneActorAnimationManager(this);
        }

        protected override void _initAnimationManager()
        {
            /*_soundBank = "bairne";
            _useAnimationManagerSoundEvents = true;
            _animationManager.SetSoundEvent(IdleAnim, "idle");
            _animationManager.SetSoundEvent(RunAnim, "attack");
            _animationManager.SetSoundEvent(DieAnim, "death");*/
        }

        protected void Shoot()
        {
            launched = true;
            Vector2 direction = _getDistanceToPlayer();
            direction /= direction;

            if (vineWeaponComponent != null && kushWeaponComponent != null)
            {
                switch (weaponType)
                {
                    case "vine":
                        vineWeaponComponent.FireAt(direction);
                        break;
                    case "kushling":
                        kushWeaponComponent.ShotAngle *= direction.X;
                        kushWeaponComponent.Fire();
                        break;
                    default:
                        ; // Log an error
                        break;
                }
            }
            else
                ; // Log error and shit
        }

        #endregion


        #region Animation Manager
        public class BairneActorAnimationManager : ActorAnimationManager
        {
            private BairneActorComponent actorComponent;
            private LuaAIController controller;

            public BairneActorAnimationManager(BairneActorComponent actorComponent)
                : base(actorComponent)
            {
                this.actorComponent = actorComponent;
                this.controller = actorComponent.Controller as LuaAIController;
            }

            protected override void _registerAnimStates()
            {
                FSM.Instance.RegisterState<IdleState>(this, "idle");
                FSM.Instance.RegisterState<RunState>(this, "run");
                FSM.Instance.RegisterState<ActionState>(this, "swipe");
                FSM.Instance.RegisterState<AttackState>(this, "kush_launch");
                FSM.Instance.RegisterState<AttackState>(this, "vine_launch");
                FSM.Instance.RegisterState<DieState>(this, "die");

                _currentState = FSM.Instance.GetState(this, "idle");
            }

            new public class IdleState : ActorAnimationManager.IdleState
            {
                public override void Enter(IFSMObject obj)
                {
                    base.Enter(obj);

                    BairneActorAnimationManager actorAnimMgr = obj as BairneActorAnimationManager;

                    if (actorAnimMgr.actorComponent == null)
                        return;
                }

                public override string Execute(IFSMObject obj)
                {
                    BairneActorAnimationManager actorAnimMgr = obj as BairneActorAnimationManager;

                    if (actorAnimMgr.actorComponent == null)
                        return null;

                    if (!actorAnimMgr.actorComponent._alive)
                        return "die";

                    return null;
                }
            }

            new public class RunState : ActorAnimationManager.RunState
            {
                public override void Enter(IFSMObject obj)
                {
                    base.Enter(obj);

                    BairneActorAnimationManager actorAnimMgr = obj as BairneActorAnimationManager;

                    if (actorAnimMgr.actorComponent == null)
                        return;

                    actorAnimMgr._transitioningTo = actorAnimMgr.actorComponent.RunAnim;

                    if (!actorAnimMgr._transitioning)
                        actorAnimMgr._playAnimation(actorAnimMgr._transitioningTo);

                    if (actorAnimMgr.actorComponent._scaleRunAnimBySpeed)
                        actorAnimMgr.actorComponent.AnimatedSprite.AnimationTimeScale = actorAnimMgr.actorComponent._runAnimSpeedScale / actorAnimMgr.actorComponent._maxMoveSpeed;

                    //actorAnimMgr.currentSound = SoundManager.Instance.PlaySound("grunt", "walk");
                }

                public override string Execute(IFSMObject obj)
                {
                    BairneActorAnimationManager actorAnimMgr = obj as BairneActorAnimationManager;

                    if (actorAnimMgr.actorComponent == null)
                        return null;

                    if (!actorAnimMgr.actorComponent._alive)
                        return "die";

                    return null;
                }

                public override void Exit(IFSMObject obj)
                {
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

                    BairneActorAnimationManager actorAnimMgr = obj as BairneActorAnimationManager;

                    if (actorAnimMgr.actorComponent == null)
                        return null;

                    if (!actorAnimMgr.actorComponent.Alive)
                        return "die";

                    if(actorAnimMgr.actorComponent.CanShoot())
                        actorAnimMgr.actorComponent.Shoot();

                    return null;
                }

                public override void Exit(IFSMObject obj)
                {
                }
            }
        }

        #endregion

    }
}
