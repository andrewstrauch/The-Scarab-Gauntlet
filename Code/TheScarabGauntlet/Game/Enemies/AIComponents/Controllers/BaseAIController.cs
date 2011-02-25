using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using GarageGames.Torque.T2D;
using GarageGames.Torque.Core;
using GarageGames.Torque;
using GarageGames.Torque.PlatformerFramework;

namespace PlatformerStarter.Enemies
{
    public class BaseAIController : ActorAIController
    {
        protected IEnemyActor actor;
        protected bool onLeft = true;
        protected bool attacks;
        protected bool onSameLevel;
        protected float attackDist;
        protected float alertDist;

        #region Properties
        public bool OnLeft
        {
            get { return onLeft; }
        }
        public float AttackDist
        {
            set { attackDist = value; }
        }
        public float AlertDist
        {
            set { alertDist = value; }
        }
        public bool InAlertRange
        {
            get
            {
                float distance = Math.Abs(_getDistanceToPlayer().X); 
                return (distance < alertDist); }
        }
        public bool InAttackRange
        {
            get { return (Math.Abs(_getDistanceToPlayer().X) < attackDist); }
        }
        public bool Attacks
        {
            get { return attacks; }
            set { attacks = value; }
        }
        #endregion

        /// <summary>
        /// Gets a directional vector from the enemy to the player.  Note that this
        /// is a normalized vector.
        /// </summary>
        /// <returns>A normalized directional vector to the player</returns>
        public Vector2 GetDirectionToPlayer()
        {
            Vector2 direction = _getDistanceToPlayer();
            direction.Normalize();
            return direction;
        }

        public Vector2 _getDistanceToPlayer()
        {
            //T2DSceneCamera camera = TorqueObjectDatabase.Instance.FindObject<T2DSceneCamera>("Camera");
            T2DSceneObject player = TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>("Amanda");

            // get the distance of this enemy to the player
            return (player.Position - actor.Actor.Position);
        }

        protected void CheckPlayerPosition(Vector2 distance)
        {
            if (distance.X <= 0)
                onLeft = true;
            else
                onLeft = false;

            if (distance.Y - 20 > 0)
                onSameLevel = false;
            else
                onSameLevel = true;
        }

        protected override void _update()
        {
            base._update();

            CheckPlayerPosition(_getDistanceToPlayer());
        }

        public override void ActorSpawned(ActorComponent actor)
        {
            base.ActorSpawned(actor);

            this.actor = actor as IEnemyActor;

            // make sure all the actors are in the idle state when they spawn
            CurrentState = FSM.Instance.GetState(this, "idle");
        }
        
        public override void ActorDied(ActorComponent actor, float damage, T2DSceneObject sourceObject)
        {
        }
    }
}
