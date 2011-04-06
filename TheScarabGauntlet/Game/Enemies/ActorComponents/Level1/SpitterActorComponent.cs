using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework;

using GarageGames.Torque.Core;
using GarageGames.Torque.Sim;
using GarageGames.Torque.T2D;
using GarageGames.Torque.SceneGraph;
using GarageGames.Torque.MathUtil;
using GarageGames.Torque.GameUtil;
using GarageGames.Torque.XNA;

using GarageGames.Torque.PlatformerFramework;

namespace PlatformerStarter.Enemies.ActorComponents
{      
    [TorqueXmlSchemaType]
    public class SpitterActorComponent : EnemyActorComponent
    {
        #region Private Members
        private T2DAnimationData attackLeftAnim;
        private T2DAnimationData attackRightAnim;
        private T2DAnimationData alertLeftAnim;
        private T2DAnimationData alertRightAnim;
        private T2DAnimationData hideLeftAnim;
        private T2DAnimationData hideRightAnim;
        private T2DAnimationData leftIdleAnim;
        private T2DAnimationData rightIdleAnim;
        private T2DSceneObject weakSpotTemplate;
        private T2DSceneObject weakSpotObject;
        private WeaponComponent weaponComponent;
        private Timer attackTimer;
        private T2DSceneObject weaponTemplate;
        private string weaponLinkPoint;
        private float coolDown;
        private bool justShot;
        #endregion

        #region Public Properties
        public T2DAnimationData AttackLeftAnim
        {
            get { return attackLeftAnim; }
            set { attackLeftAnim = value; }
        }
        public T2DAnimationData AttackRightAnim
        {
            get { return attackRightAnim; }
            set { attackRightAnim = value; }
        }
        public T2DAnimationData AlertLeftAnim
        {
            get { return alertLeftAnim; }
            set { alertLeftAnim = value; }
        }
        public T2DAnimationData AlertRightAnim
        {
            get { return alertRightAnim; }
            set { alertRightAnim = value; }
        }
        public T2DAnimationData HideLeftAnim
        {
            get { return hideLeftAnim; }
            set { hideLeftAnim = value; }
        }
        public T2DAnimationData HideRightAnim
        {
            get { return hideRightAnim; }
            set { hideRightAnim = value; }
        }
        public T2DAnimationData LeftIdleAnim
        {
            get { return leftIdleAnim; }
            set { leftIdleAnim = value; }
        }
        public T2DAnimationData RightIdleAnim
        {
            get { return rightIdleAnim; }
            set { rightIdleAnim = value; }
        }
        public T2DSceneObject WeakSpotTemplate
        {
            get { return weakSpotTemplate; }
            set { weakSpotTemplate = value; }
        }
        public T2DSceneObject WeaponTemplate
        {
            get { return weaponTemplate; }
            set { weaponTemplate = value; }
        }
        public string WeaponLinkPoint
        {
            get { return weaponLinkPoint; }
            set { weaponLinkPoint = value; }
        }
        [TorqueXmlSchemaType(DefaultValue = "500")]
        public float CoolDown
        {
            get { return coolDown; }
            set { coolDown = value; }
        }
        #endregion

        #region Public Routines
        public override void CopyTo(TorqueComponent obj)
        {
            base.CopyTo(obj);

            SpitterActorComponent obj2 = obj as SpitterActorComponent;

            obj2.AlertLeftAnim = AlertLeftAnim;
            obj2.AlertRightAnim = AlertRightAnim;
            obj2.AttackLeftAnim = AttackLeftAnim;
            obj2.AttackRightAnim = AttackRightAnim;
            obj2.HideLeftAnim = HideLeftAnim;
            obj2.HideRightAnim = HideRightAnim;
            obj2.LeftIdleAnim = LeftIdleAnim;
            obj2.RightIdleAnim = RightIdleAnim;
            obj2.CoolDown = CoolDown;
            obj2.WeakSpotTemplate = WeakSpotTemplate;
            obj2.WeaponTemplate = WeaponTemplate;
            obj2.WeaponLinkPoint = WeaponLinkPoint;
        }

        /// <summary>
        /// Attacks the player.
        /// </summary>
        public override void Attack()
        {
            if (!Alive)
                return;

            // set the current action animation to punch
            if (((AIRangedAttackController)actorBehavior[0].Controller).OnLeft)
                ActionAnim = AttackLeftAnim;
            else
                ActionAnim = AttackRightAnim;

            // switch to the "action" state
            FSM.Instance.SetState(_animationManager, "attack");

            readyToAttack = false;
        }

        /// <summary>
        /// Exposes the weak spot for collision.
        /// </summary>
        public void ExposeWeakSpot()
        {
            weakSpotObject.CollisionsEnabled = true;
        }

        /// <summary>
        /// Hides the weak spot for collision.
        /// </summary>
        public void HideWeakSpot()
        {
            weakSpotObject.CollisionsEnabled = false;
        }

        /// <summary>
        /// Fires the spitter's weapon (spit).
        /// </summary>
        public void FireWeapon()
        {
            if (!justShot)
            {
                weaponComponent.FireAt(actorBehavior[0].Controller.GetDirectionToPlayer());
                //weaponObject.Components.FindComponent<WeaponComponent>().Fire();//new Vector2(-1, 0));
                justShot = true;
            }
        }
        #endregion

        #region Private Routines
        protected override void _preUpdate(float elapsed)
        {
            base._preUpdate(elapsed);

            if (attackTimer.Expired)
            {
                justShot = false;
                readyToAttack = true;
                attackTimer.Reset();
            }
        }

        protected override bool _OnRegister(TorqueObject owner)
        {
            if (!base._OnRegister(owner))
                return false;

            attackTimer = new Timer("spitterAttackTimer");
            attackTimer.MillisecondsUntilExpire = coolDown;

            if (weakSpotTemplate != null)
            {
                weakSpotObject = weakSpotTemplate.Clone() as T2DSceneObject;
                foreach (T2DCollisionImage image in weakSpotObject.Collision.Images)
                    SceneObject.Collision.InstallImage(image);
            }

            T2DSceneObject weapon = weaponTemplate.Clone() as T2DSceneObject;
            TorqueObjectDatabase.Instance.Register(weapon);

            if (weapon != null)
            {
                weapon.Mount(SceneObject, weaponLinkPoint, true);
                weaponComponent = weapon.Components.FindComponent<WeaponComponent>();
            }
            
            justShot = false;

            return true;
        }

        protected override void _createAnimationManager()
        {
            _animationManager = new SpitterActorAnimationManager(this);
        }

        protected override void _initAnimationManager()
        {
            _useAnimationManagerSoundEvents = true;

            _soundBank = "spitter";
            _useAnimationManagerSoundEvents = true;
            _animationManager.SetSoundEvent(HideLeftAnim, "hide");
            _animationManager.SetSoundEvent(HideRightAnim, "hide");
            //_animationManager.SetSoundEvent(AlertLeftAnim, "alert");
            //_animationManager.SetSoundEvent(AlertRightAnim, "alert");
            _animationManager.SetSoundEvent(AttackLeftAnim, "attack");
            _animationManager.SetSoundEvent(AttackRightAnim, "attack");
            _animationManager.SetSoundEvent(DieAnim, "death");
        }
        #endregion

        #region Animation Manager
        public class SpitterActorAnimationManager : ActorAnimationManager
        {
            private SpitterActorComponent actorComponent;

            public SpitterActorAnimationManager(SpitterActorComponent actorComponent)
                : base(actorComponent)
            {
                this.actorComponent = actorComponent;
            }

            protected override void _registerAnimStates()
            {
                FSM.Instance.RegisterState<IdleState>(this, "idle");
                FSM.Instance.RegisterState<AttackState>(this, "attack");
                FSM.Instance.RegisterState<AlertState>(this, "alert");
                FSM.Instance.RegisterState<HideState>(this, "hide");
                FSM.Instance.RegisterState<DieState>(this, "die");

                _currentState = FSM.Instance.GetState(this, "idle");
            }

            new public class IdleState : ActorAnimationManager.IdleState
            {
                public override void Enter(IFSMObject obj)
                {
                    //base.Enter(obj);

                    SpitterActorAnimationManager actorAnimMgr = obj as SpitterActorAnimationManager;

                    if (actorAnimMgr.actorComponent == null)
                        return;

                    AIRangedAttackController controller = (AIRangedAttackController)actorAnimMgr.actorComponent.Controller;

                    if (controller.OnLeft)
                        actorAnimMgr._transitioningTo = actorAnimMgr.actorComponent.LeftIdleAnim;
                    else
                        actorAnimMgr._transitioningTo = actorAnimMgr.actorComponent.RightIdleAnim;

                    if (!actorAnimMgr._transitioning)
                        actorAnimMgr._playAnimation(actorAnimMgr._transitioningTo);
    
                }

                public override string Execute(IFSMObject obj)
                {
                    //base.Execute(obj);

                    SpitterActorAnimationManager actorAnimMgr = obj as SpitterActorAnimationManager;

                    if (actorAnimMgr.actorComponent == null)
                        return null;

                    if (!actorAnimMgr.actorComponent._alive)
                        return "die";

                    AIRangedAttackController controller = (AIRangedAttackController)actorAnimMgr.actorComponent.Controller;

                    if (controller == null)
                        return null;

                    if (controller.InAlertRange)
                    {
                        SoundManager.Instance.PlaySound("spitter", "alert");
                        return "alert";
                    }

                    return null;
                }
            }

            public class AlertState : AnimationState
            {
                public override void Enter(IFSMObject obj)
                {
                    base.Enter(obj);

                    SpitterActorAnimationManager actorAnimMgr = obj as SpitterActorAnimationManager;

                    if (actorAnimMgr.actorComponent == null)
                        return;

                    AIRangedAttackController controller = (AIRangedAttackController)actorAnimMgr.actorComponent.Controller;

                    if (!controller.OnLeft)
                        actorAnimMgr._transitioningTo = actorAnimMgr.actorComponent.AlertRightAnim;
                    else
                        actorAnimMgr._transitioningTo = actorAnimMgr.actorComponent.AlertLeftAnim;

                    if (actorAnimMgr.PreviousState.StateName == "attack" ||
                        actorAnimMgr.PreviousState.StateName == "alert")
                        actorAnimMgr._playAnimation(actorAnimMgr._transitioningTo,
                            (uint)actorAnimMgr._transitioningTo.AnimationFramesList.Count - 1);
                    else
                        if (!actorAnimMgr._transitioning)
                            actorAnimMgr._playAnimation(actorAnimMgr._transitioningTo);

                    actorAnimMgr.actorComponent.ExposeWeakSpot();
                }

                public override string Execute(IFSMObject obj)
                {
                    SpitterActorAnimationManager actorAnimMgr = obj as SpitterActorAnimationManager;

                    if (actorAnimMgr.actorComponent == null)
                        return null;

                    if (!actorAnimMgr.actorComponent._alive)
                        return "die";

                    if (!actorAnimMgr.actorComponent.AnimatedSprite.IsAnimationPlaying)
                    {
                        AIRangedAttackController controller = (AIRangedAttackController)actorAnimMgr.actorComponent.actorBehavior[0].Controller;

                        //Game.Instance.Window.Title = controller._getDistanceToPlayer().X.ToString();
                        
                        if (controller == null)
                            return null;

                        if (!controller.InAlertRange)
                            return "hide";
                    }

                    return null;
                }
            }

            public class HideState : AnimationState
            {
                public override void Enter(IFSMObject obj)
                {
                    base.Enter(obj);

                    SpitterActorAnimationManager actorAnimMgr = obj as SpitterActorAnimationManager;

                    if (actorAnimMgr.actorComponent == null)
                        return;

                    AIRangedAttackController controller = (AIRangedAttackController)actorAnimMgr.actorComponent.actorBehavior[0].Controller;

                    if (!controller.OnLeft)
                        actorAnimMgr._transitioningTo = actorAnimMgr.actorComponent.HideRightAnim;
                    else
                        actorAnimMgr._transitioningTo = actorAnimMgr.actorComponent.HideLeftAnim;

                    if (!actorAnimMgr._transitioning)
                        actorAnimMgr._playAnimation(actorAnimMgr._transitioningTo);

                    actorAnimMgr.actorComponent.HideWeakSpot();
                }

                public override string Execute(IFSMObject obj)
                {
                    SpitterActorAnimationManager actorAnimMgr = obj as SpitterActorAnimationManager;

                    if (actorAnimMgr.actorComponent == null)
                        return null;

                    if (!actorAnimMgr.actorComponent.AnimatedSprite.IsAnimationPlaying)
                        return "idle";

                    return null;
                }
            }

            public class AttackState : ActionState
            {
                public override string Execute(IFSMObject obj)
                {
                    SpitterActorAnimationManager actorAnimMgr = obj as SpitterActorAnimationManager;

                    if (actorAnimMgr.actorComponent == null)
                        return null;

                    if (actorAnimMgr.actorComponent.AnimatedSprite.CurrentFrame == 7)
                        actorAnimMgr.actorComponent.FireWeapon();

                    if (!actorAnimMgr.actorComponent._alive)
                        return "die";

                    if(!actorAnimMgr.actorComponent.AnimatedSprite.IsAnimationPlaying)
                        return "alert";

                    return null;
                }

                public override void Exit(IFSMObject obj)
                {
                    base.Exit(obj);

                    SpitterActorAnimationManager actorAnimMgr = obj as SpitterActorAnimationManager;

                    if (actorAnimMgr.actorComponent == null)
                        return;

                    actorAnimMgr.actorComponent.attackTimer.Start();
                }
            }
        }
        #endregion
    }
}
