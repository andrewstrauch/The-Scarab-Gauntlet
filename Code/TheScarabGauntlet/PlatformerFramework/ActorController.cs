//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;

using GarageGames.Torque.T2D;

namespace GarageGames.Torque.PlatformerFramework
{
    /// <summary>
    /// Base MoveController class for all Actors. Contains control and notification functionality. All Actors possessed by this controller
    /// should be controlled by calling the ActorController's move functions (such as _moveLeft and _moveRight). You can also choose to override
    /// the notification callbacks (such as ActorCollectedItem or ActorDamaged) if you wish to have the ActorController respond in some way to any 
    /// of the feedback from its possessed Actors.
    /// </summary>
    public class ActorController : MoveController
    {
        //======================================================
        #region Public methods

        /// <summary>
        /// Called from an actor possessed by this controller each time it spawns.
        /// </summary>
        /// <param name="actor">The possessed ActorComponent that spawned.</param>
        public virtual void ActorSpawned(ActorComponent actor) { }

        /// <summary>
        /// Called from an actor possessed by this controller each time it picks up a collectible. Note that
        /// in many cases the collectible can be deleted before this code is reached, though by default the 
        /// collectible is only made invisible by this point, and then deleted after this code has had a
        /// chance to run.
        /// </summary>
        /// <param name="actor">The possessed ActorComponent that got a collectible.</param>
        /// <param name="collectible">The CollectibleComponent for the item that was picked up.</param>
        public virtual void ActorCollectedItem(ActorComponent actor, CollectibleComponent collectible) { }

        /// <summary>
        /// Called from an actor possessed by this controller each time it lands on a valid platform.
        /// A valid platform is a scene object with either a PlatformComponent or a SolidPlatformComponent.
        /// </summary>
        /// <param name="actor">The possessed ActorComponent that landed on a platform.</param>
        /// <param name="platform">The scene object that the Actor landed on.</param>
        public virtual void ActorLanded(ActorComponent actor, T2DSceneObject platform) { }

        /// <summary>
        /// Called from an actor possessed by this controller each time it collides with a wall or ceiling.
        /// (as defined on a per-actor basis by the ActorComponent's _maxSurfaceNormalY field).
        /// </summary>
        /// <param name="actor">The possessed ActorComponent that hit a wall.</param>
        /// <param name="info">The collision info for the collision that lead to this callback.</param>
        /// <param name="dot">Dot product of the velocity and the normal.</param>
        public virtual void ActorHitWall(ActorComponent actor, T2DCollisionInfo info, float dot) { }

        /// <summary>
        /// Called from an actor possessed by this controller each time it receives damage.
        /// </summary>
        /// <param name="actor">The possessed ActorComponent that took damage.</param>
        /// <param name="damage">The amount of damage that was taken by the actor.</param>
        /// <param name="sourceObject">The scene object related to the damage transaction. In most cases, the owner 
        /// of the component that dealt the damage).</param>
        public virtual void ActorDamaged(ActorComponent actor, float damage, T2DSceneObject sourceObject) { }

        /// <summary>
        /// Called from an actor possessed by this controller each time it dies.
        /// </summary>
        /// <param name="actor">The possessed ActorComponent that died.</param>
        /// <param name="damage">The amount of damage that was taken the instant of death. This should always be reliable
        /// because the default Kill method on the ActorComponent deals damage via the same methods as anything else.</param>
        /// <param name="sourceObject">The scene object related to the damage transaction. In most cases, the owner 
        /// of the component that dealt the damage).</param>
        public virtual void ActorDied(ActorComponent actor, float damage, T2DSceneObject sourceObject) { }

        #endregion

        //======================================================
        #region Private, protected, internal methods

        /// <summary>
        /// Calls MoveLeft method on all ActorComponents possessed by this controller.
        /// </summary>
        protected virtual void _moveLeft()
        {
            foreach (ActorComponent actor in Movers)
                actor.MoveLeft();
        }

        /// <summary>
        /// Calls MoveRight method on all ActorComponents possessed by this controller
        /// </summary>
        protected virtual void _moveRight()
        {
            foreach (ActorComponent actor in Movers)
                actor.MoveRight();
        }

        /// <summary>
        /// Calls HorizontalStop method on all ActorComponents possessed by this controller.
        /// </summary>
        protected virtual void _horizontalStop()
        {
            foreach (ActorComponent actor in Movers)
                actor.HorizontalStop();
        }

        /// <summary>
        /// Calls MoveUp method on all ActorComponents possessed by this controller.
        /// </summary>
        protected virtual void _moveUp()
        {
            foreach (ActorComponent actor in Movers)
                actor.MoveUp();
        }

        /// <summary>
        /// Calls MoveDown method on all ActorComponents possessed by this controller.
        /// </summary>
        protected virtual void _moveDown()
        {
            foreach (ActorComponent actor in Movers)
                actor.MoveDown();
        }

        /// <summary>
        /// Calls VerticalStop method on all ActorComponents possessed by this controller.
        /// </summary>
        protected virtual void _verticalStop()
        {
            foreach (ActorComponent actor in Movers)
                actor.VerticalStop();
        }

        /// <summary>
        /// Calls Jump method on all ActorComponents possessed by this controller.
        /// </summary>
        protected virtual void _jump()
        {
            foreach (ActorComponent actor in Movers)
                actor.Jump();
        }

        /// <summary>
        /// Calls JumpDown method on all ActorComponents possessed by this controller.
        /// </summary>
        protected virtual void _jumpDown()
        {
            foreach (ActorComponent actor in Movers)
                actor.JumpDown();
        }
        #endregion
    }
}
