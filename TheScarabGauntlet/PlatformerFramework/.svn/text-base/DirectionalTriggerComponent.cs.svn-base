//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

using GarageGames.Torque.Core;
using GarageGames.Torque.T2D;

namespace GarageGames.Torque.PlatformerFramework
{
    /// <summary>
    /// This is a directional trigger that was created to avoid the performance issues that are inherent in 
    /// a game that needs a bajillion triggers everywhere. If normal T2DTriggerComponent triggers are 'active'
    /// triggers, think of this as a 'passive' trigger. Note that for these to work on moving objects, the 
    /// object that is checking for triggers must have ProcessCollisionsAtRest set to true. Typically you'll
    /// want to use this with only a T2DCollisionComponent and set its object type to something that your
    /// scene objects will hit.
    /// </summary>
    [TorqueXmlSchemaDependency(Type = typeof(T2DCollisionComponent))]
    public class DirectionalTriggerComponent : TorqueComponent
    {
        //======================================================
        #region Public properties, operators, constants, and enums

        /// <summary>
        /// The T2DSceneObject owner of this component.
        /// </summary>
        [XmlIgnore]
        [TorqueCloneIgnore]
        public T2DSceneObject SceneObject
        {
            get { return _sceneObject; }
            set { _sceneObject = value; }
        }

        #endregion

        //======================================================
        #region Public methods

        public void OnCollision(T2DSceneObject ourObject, T2DSceneObject theirObject, T2DCollisionInfo info, ref T2DResolveCollisionDelegate resolve, ref T2DCollisionMaterial physicsMaterial)
        {
            // call our local trigger OnEnter method
            _onEnter(ourObject, theirObject, info);
        }

        #endregion

        //======================================================
        #region Private, protected, internal methods

        /// <summary>
        /// The _onEnter callback that's called whenever an obejct collides with this trigger.
        /// </summary>
        /// <param name="ourObject">The owner of this trigger component.</param>
        /// <param name="theirObject">The object that collided with this trigger.</param>
        /// <param name="info">The collision info generated for the collision.</param>
        protected virtual void _onEnter(T2DSceneObject ourObject, T2DSceneObject theirObject, T2DCollisionInfo info) { }

        protected override bool _OnRegister(TorqueObject owner)
        {
            if (!base._OnRegister(owner))
                return false;

            _sceneObject = owner as T2DSceneObject;

            if (_sceneObject.Collision == null)
                Assert.Fatal(false, "TriggerComponent requires a T2DCollisionComponent. Please add these components to your trigger.\n\nTrigger position: " + _sceneObject.Position.ToString());

            // make sure the collision component has at least one image
            if (_sceneObject.Collision.Images.Count == 0)
                _sceneObject.Collision.InstallImage(new T2DPolyImage());

            // make sure the collision and physics settigns are correct
            _sceneObject.Collision.OnCollision = OnCollision;
            _sceneObject.Collision.CollidesWith = TorqueObjectType.NoObjects;
            _sceneObject.Collision.SolveOverlap = false;
            _sceneObject.CollisionsEnabled = true;

            return true;
        }

        #endregion

        //======================================================
        #region Private, protected, internal fields

        private T2DSceneObject _sceneObject;

        #endregion
    }
}
