using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework;

using GarageGames.Torque.Core;
using GarageGames.Torque.T2D;
using GarageGames.Torque.XNA;
using GarageGames.Torque.PlatformerFramework;

namespace PlatformerStarter.Enemies.ActorComponents
{
    [TorqueXmlSchemaType]
    public class BomberActorComponent : EnemyActorComponent
    {
        protected T2DAnimationData attackAnim;
        protected bool attacked;
        private bool exploded;

        #region Properties
        public T2DAnimationData AttackAnim
        {
            get { return attackAnim; }

            set { attackAnim = value; }
        }
        #endregion

        public override void CopyTo(TorqueComponent obj)
        {
            base.CopyTo(obj);

            BomberActorComponent obj2 = obj as BomberActorComponent;

            obj2.AttackAnim = AttackAnim;
        }

        public override void Attack()
        {
            if (!Alive)
                return;

            if (!attacked)
            {
                // switch to the "die" state
                ActionAnim = attackAnim;
                FSM.Instance.SetState(_animationManager, "action");

                // Fire off projectiles
                attacked = true;
            }

            this.Actor.Physics.VelocityX = 0;

            if (AnimatedSprite.CurrentFrame == (AnimatedSprite.FinalFrame-7))
            {
                WeaponComponent weapon = SceneObject.Components.FindComponent<WeaponComponent>();
                if (SceneObject != null && !exploded)
                {
                    weapon.FireAt(new Vector2(-1, 0));
                    weapon.FireAt(new Vector2(-1, -1));
                    weapon.FireAt(new Vector2(0, -1));
                    weapon.FireAt(new Vector2(1, -1));
                    weapon.FireAt(new Vector2(1, 0));
                }
                exploded = true;
            }

            if(AnimatedSprite.CurrentFrame >= AnimatedSprite.FinalFrame)
                _die(_maxHealth, this.Actor);
        }

        protected override bool _OnRegister(TorqueObject owner)
        {
            if(!base._OnRegister(owner))
                return false;

            attacked = false;
            exploded = false;

            return true;
        }

        protected override void _initAnimationManager()
        {
            _soundBank = "bomber";
            _useAnimationManagerSoundEvents = true;
            //_animationManager.SetSoundEvent(RunAnim, "walk");
            _animationManager.SetSoundEvent(AttackAnim, "explode");

            _useAnimationStepSoundList = true;
            _animationManager.AddStepSoundFrame(RunAnim, 1, "walk");
        }
    }
}
