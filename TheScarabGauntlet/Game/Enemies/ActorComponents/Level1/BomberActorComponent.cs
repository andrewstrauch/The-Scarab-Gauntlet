using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework;
using PlatformerStarter.Common.Util;
using GarageGames.Torque.Core;
using GarageGames.Torque.T2D;
using GarageGames.Torque.XNA;
using GarageGames.Torque.GameUtil;
using GarageGames.Torque.PlatformerFramework;

namespace PlatformerStarter.Enemies.ActorComponents
{
    [TorqueXmlSchemaType]
    public class BomberActorComponent : EnemyActorComponent
    {
        #region Private Members
        private bool exploded;
        private T2DAnimationData warningAnim;
        private T2DAnimationData explodeAnim;
        private SpawnedParticle effect;
        private Timer explosionTimer;
        #endregion

        #region Public Properties
        /// <summary>
        /// The animation to play to warn the player that the bomber is 
        /// going to explode.
        /// </summary>
        public T2DAnimationData WarningAnim
        {
            get { return warningAnim; }
            set { warningAnim = value; }
        }

        /// <summary>
        /// The animation to play when the bomber explodes.
        /// </summary>
        public T2DAnimationData ExplodeAnim
        {
            get { return explodeAnim; }
            set { explodeAnim = value; }
        }

        /// <summary>
        /// The particle effect to play when the bomber explodes.
        /// </summary>
        public SpawnedParticle ExplodeEffect
        {
            get { return effect; }
            set { effect = value; }
        }

        /// <summary>
        /// The amount of time to wait before making the bomber explode.
        /// </summary>
        public float ExplosionDelay
        {
            get { return explosionTimer.SecondsUntilExpire; }
            set { explosionTimer.SecondsUntilExpire = value; }
        }

        #endregion

        #region Public Routines

        public BomberActorComponent()
        {
            explosionTimer = new Timer();
        }

        /// <summary>
        /// Copies all necessary properties to any objects cloned from this template.
        /// </summary>
        /// <param name="obj"></param>
        public override void CopyTo(TorqueComponent obj)
        {
            base.CopyTo(obj);

            BomberActorComponent obj2 = obj as BomberActorComponent;

            obj2.WarningAnim = WarningAnim;
            obj2.ExplodeAnim = ExplodeAnim;
            obj2.ExplodeEffect = ExplodeEffect;
            obj2.ExplosionDelay = ExplosionDelay;
        }

        /// <summary>
        /// Stops the bomber from moving, plays the bomber's warning animation, and after
        /// a certain time, blows the bomber up.
        /// </summary>
        public override void Attack()
        {
            if (!Alive)
                return;

            // Stops the bomber from moving left or right.
            this.Actor.Physics.VelocityX = 0;

            // Upon first entering this routine, the timer needs to start and the bomber
            // needs to go into the "warning" animation state.
            if (!explosionTimer.Running && !explosionTimer.Expired)
            {
                explosionTimer.Start();
                ActionAnim = WarningAnim;
                FSM.Instance.SetState(_animationManager, "warning");
            }
            // When the timber expires, we let the animation manager know that it's time
            // to switch to the "explode" state.
            else if (explosionTimer.Expired)
            {
                readyToAttack = true;
                ActionAnim = ExplodeAnim;
            }

            // If we're ready to explode and haven't yet, then find the attached weapon
            // component and fire a particle left, right, up, and on the left and right 
            // 45 degree angles.
            if (readyToAttack && !exploded)
            {
                WeaponComponent weapon = SceneObject.Components.FindComponent<WeaponComponent>();
                if (SceneObject != null)
                {
                    weapon.FireAt(new Vector2(-1, 0));
                    weapon.FireAt(new Vector2(-1, -1));
                    weapon.FireAt(new Vector2(0, -1));
                    weapon.FireAt(new Vector2(1, -1));
                    weapon.FireAt(new Vector2(1, 0));
                }

                effect.Spawn(SceneObject.Position);
                exploded = true;
            }
        }

        /// <summary>
        /// Makes the bomber explode when called.
        /// </summary>
        public void Explode()
        {
            _die(_maxHealth, this.Actor);
        }

        #endregion

        #region Private Routines

        /// <summary>
        /// Initializes the bomber so that it knows it's not yet ready to explode.
        /// </summary>
        /// <param name="owner">The object that this component is attached to.</param>
        /// <returns>True if initialization is successful, false otherwise.</returns>
        protected override bool _OnRegister(TorqueObject owner)
        {
            if(!base._OnRegister(owner))
                return false;

            exploded = false;
            readyToAttack = false;

            return true;
        }

        /// <summary>
        /// Creates an instance of the animation manager for animation fun!!
        /// </summary>
        protected override void _createAnimationManager()
        {
            _animationManager = new BomberAnimationManager(this);
        }

        /// <summary>
        /// Intializes the manager with various sound effects the bomber needs.
        /// </summary>
        protected override void _initAnimationManager()
        {
            _soundBank = "bomber";
            _useAnimationManagerSoundEvents = true;
            _animationManager.SetSoundEvent(RunAnim, "walk");
            _animationManager.SetSoundEvent(WarningAnim, "warning");
            _animationManager.SetSoundEvent(ExplodeAnim, "explode");
        }

        #endregion

        #region Animation Manager
        public class BomberAnimationManager : ActorAnimationManager
        {
            private BomberActorComponent actorComponent;

            public BomberAnimationManager(BomberActorComponent actorComponent)
                : base(actorComponent)
            {
                this.actorComponent = actorComponent;
            }

            protected override void _registerAnimStates()
            {
                base._registerAnimStates();

                FSM.Instance.RegisterState<ExplodeState>(this, "explode");
                FSM.Instance.RegisterState<WarningState>(this, "warning");
            }

            public class WarningState : ActionState
            {
                public override string Execute(IFSMObject obj)
                {
                    BomberAnimationManager animMgr = obj as BomberAnimationManager;

                    if(animMgr.actorComponent == null)
                        return null;

                    if (animMgr.actorComponent.ReadyToAttack)
                        return "explode";

                    return null;
                }
            }

            public class ExplodeState : ActionState
            {
                public override string Execute(IFSMObject obj)
                {
                    BomberAnimationManager animMgr = obj as BomberAnimationManager;

                    if (animMgr.actorComponent == null)
                        return null;

                    if (!animMgr.actorComponent.Alive)
                        return "die";

                    if(!animMgr.actorComponent.AnimatedSprite.IsAnimationPlaying)
                       animMgr.actorComponent.Explode();

                    return null;
                }
            }
        }
        #endregion
    }
}
