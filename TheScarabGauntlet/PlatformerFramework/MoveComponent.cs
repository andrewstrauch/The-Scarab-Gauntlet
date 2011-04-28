//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework;

using GarageGames.Torque.Core;
using GarageGames.Torque.SceneGraph;
using GarageGames.Torque.Sim;
using GarageGames.Torque.T2D;

namespace GarageGames.Torque.PlatformerFramework
{
    /// <summary>
    /// A base component for moving objects. Contains functionality to handshake with a MoveController. 
    /// Also establishes pre- and post- physics update callbacks. See ActorComponent for an example of
    /// intended use.
    /// </summary>
    public abstract class MoveComponent : TorqueComponent, IT2DForceGenerator
    {
        //======================================================
        #region Public properties, operators, constants, and enums

        /// <summary>
        /// The scene object that owns this MoveComponent.
        /// </summary>
        public T2DSceneObject SceneObject
        {
            set { _sceneObject = value; }
            get { return _sceneObject; }
        }

        /// <summary>
        /// The MoveController that is currently possessing this MoveComponent.
        /// </summary>
        public MoveController Controller
        {
            get { return _controller; }
        }

        /// <summary>
        /// Returns true if this MoveComponent is currently possessed by a MoveController
        /// </summary>
        public bool IsPossessed
        {
            get { return _controller != null; }
        }

        #endregion

        //======================================================
        #region Public methods

        /// <summary>
        /// Attempt to possess this MoveComponent with a MoveController.
        /// </summary>
        /// <param name="controller">The MoveController requesting possession of this MoveComponent.</param>
        public void Possess(MoveController controller)
        {
            _previousController = _controller;

            if (controller != null)
            {
                _controller = controller;

                if (!_controller.IsPossessing(this))
                    _controller.PossessMover(this);

                _possessed(controller);
            }
        }

        /// <summary>
        /// Unpossess this MoveComponent.
        /// </summary>
        public void Unpossess()
        {
            _previousController = _controller;

            if (_controller != null)
                _unpossessed(_controller);

            if (_controller.IsPossessing(this))
                _controller.UnpossessMover(this);

            _controller = null;
        }

        /// <summary>
        /// Revert possesion of this MoveComponent to it's previous MoveController. Presumably useful for 
        /// swapping vehicle control or temporarily possessing movers for cutscenes and whatnot.
        /// </summary>
        public void RevertPossession()
        {
            MoveController controller = _previousController;

            if (_controller != null && _controller.IsPossessing(this))
                _controller.UnpossessMover(this);

            _controller = controller;

            if (_controller != null && !_controller.IsPossessing(this))
                _controller.PossessMover(this);
        }

        /// <summary>
        /// IT2DForceGenerator interface method. Called by the Physics component before the current move is processed.
        /// </summary>
        /// <param name="move">Move is generally ignored by MoveComponents because their control is intended to come from 
        /// a MoveController, rather than user input.</param>
        /// <param name="elapsed">Elapsed time since last PreUpdateForces call.</param>
        public virtual void PreUpdateForces(Move move, float elapsed)
        {
            _preUpdate(elapsed);
        }

        /// <summary>
        /// IT2DForceGenerator interface method. Called by the Physics component after the current move is processed.
        /// </summary>
        /// <param name="move">Move is generally ignored by MoveComponents because their control is intended to come from 
        /// a MoveController, rather than user input.</param>
        /// <param name="elapsed">Elapsed time since last PostUpdateForces call.</param>
        public virtual void PostUpdateForces(Move move, float elapsed)
        {
            _postUpdate(elapsed);
        }

        #endregion

        //======================================================
        #region Private, protected, internal methods

        /// <summary>
        /// Callback when this MoveComponent is possessed by a MoveController. Override this to check for appropriate controllers. 
        /// Use unpossess in the case of an undesired controller.
        /// </summary>
        /// <param name="controller">The MoveController that just possessed this MoveComponent.</param>
        protected virtual void _possessed(MoveController controller) { }

        /// <summary>
        /// Callback when this MoveComponent is unpossessed by a MoveController.
        /// </summary>
        /// <param name="controller">The MoveController that just unpossessed this MoveComponent.</param>
        protected virtual void _unpossessed(MoveController controller) { }

        /// <summary>
        /// Callback for derived classes to perform physics updates before each move is processed.
        /// </summary>
        /// <param name="elapsed">Elapsed time in seconds since last _preUpdate callback.</param>
        protected virtual void _preUpdate(float elapsed) { }

        /// <summary>
        /// Callback for derived classes to perform physics updates after each move is processed.
        /// </summary>
        /// <param name="elapsed">Elapsed time in seconds since last _postUpdate callback. This will often be less
        /// than a standard tick when collisions occurred during the move.</param>
        protected virtual void _postUpdate(float elapsed) { }

        protected override bool _OnRegister(TorqueObject owner)
        {
            if (!base._OnRegister(owner))
                return false;

            // loose check for extra move components
            MoveComponent mover = owner.Components.FindComponent<MoveComponent>();
            Assert.Fatal(mover != null, "This MoveComponent does not belong to its owner.");
            Assert.Fatal(mover == this, "Extra MoveComponent detected on object.");

            // store the scene object on this mover
            _sceneObject = owner as T2DSceneObject;
            Assert.Fatal(_sceneObject != null, "MoveComponent must be used on a scene object.");

            return true;
        }

        protected override void _RegisterInterfaces(TorqueObject owner)
        {
            // register the force generator interface to allow us to get
            // pre- and post- update physics callbacks
            Owner.RegisterCachedInterface("force", String.Empty, this, _forceInterface);
        }

        #endregion

        //======================================================
        #region Private, protected, internal fields

        // scene object (owner)
        protected T2DSceneObject _sceneObject;

        // controller
        private MoveController _controller;
        private MoveController _previousController;

        // force interface
        TorqueInterfaceWrap<IT2DForceGenerator> _forceInterface = new TorqueInterfaceWrap<IT2DForceGenerator>();

        #endregion
    }
}
