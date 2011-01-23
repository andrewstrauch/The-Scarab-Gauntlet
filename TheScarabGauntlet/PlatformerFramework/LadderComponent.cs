//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework;

using GarageGames.Torque.Core;
using GarageGames.Torque.T2D;

namespace GarageGames.Torque.PlatformerFramework
{
    /// <summary>
    /// A component to be added to a scene object to give it ladder functionality. A ladder is anything an actor can climb up or down.
    /// </summary>
    [TorqueXmlSchemaType]
    [TorqueXmlSchemaField(Name = "Enabled", ExportField = false)]
    [TorqueXmlSchemaField(Name = "CollidesWith", ExportField = false)]
    [TorqueXmlSchemaField(Name = "OnEnter", ExportField = false)]
    [TorqueXmlSchemaField(Name = "OnStay", ExportField = false)]
    [TorqueXmlSchemaField(Name = "OnLeave", ExportField = false)]
    public class LadderComponent : T2DTriggerComponent
    {
        //======================================================
        #region Constructors

        /// <summary>
        /// Constructor. This sets up a T2DTriggerComponent for this component to use.
        /// </summary>
        public LadderComponent()
        {
            OnEnter = OnEnterLadder;
            OnStay = OnStayLadder;
            OnLeave = OnLeaveLadder;
            CollidesWith = PlatformerData.ActorObjectType;
            InstallImage(new T2DPolyImage());
        }

        #endregion

        //======================================================
        #region Public methods

        public void OnEnterLadder(T2DSceneObject ourObject, T2DSceneObject theirObject)
        {
            // call OnStay
            OnStay(ourObject, theirObject);
        }

        public void OnStayLadder(T2DSceneObject ourObject, T2DSceneObject theirObject)
        {
            // make sure the object is an actor type
            if (!theirObject.TestObjectType(PlatformerData.ActorObjectType))
                return;

            // grab the actor component
            ActorComponent actor = theirObject.Components.FindComponent<ActorComponent>();

            // notify the actor that they are in a ladder and give them a reference
            if (actor != null && !actor.InLadder)
                actor.LadderObject = _ladder;
        }

        public void OnLeaveLadder(T2DSceneObject ourObject, T2DSceneObject theirObject)
        {
            // notify the actor they are no longer in the ladder
            ActorComponent actor = theirObject.Components.FindComponent<ActorComponent>();

            if (actor != null)
                actor.LadderObject = null;

            // disable the trigger when all actors leave
            if (_objectList.Count == 0 && _ladder.Collision != null)
                Enabled = false;
        }

        public virtual bool TestEarlyOut(T2DSceneObject ourObject, T2DSceneObject theirObject)
        {
            // enable trigger when Actor's found
            if (theirObject.TestObjectType(PlatformerData.ActorObjectType))
                Enabled = true;

            // collide normally
            return false;
        }

        #endregion

        //======================================================
        #region Private, protected, internal methods

        protected override bool _OnRegister(TorqueObject owner)
        {
            if (!base._OnRegister(owner))
                return false;

            // record the scene object
            _ladder = owner as T2DSceneObject;

            // set the appropriate object type bits
            _ladder.SetObjectType(PlatformerData.ActorTriggerObjectType, true);
            _ladder.SetObjectType(PlatformerData.LadderObjectType, true);

            // make sure collisions are enabled on the ladder
            if (_ladder.Collision != null)
            {
                _ladder.CollisionsEnabled = true;
                _ladder.Collision.CollidesWith = TorqueObjectType.NoObjects;

                if (_ladder.Collision.EarlyOutObjectType.Equals(TorqueObjectType.AllObjects))
                    _ladder.Collision.EarlyOutObjectType = PlatformerData.ActorObjectType;
                else
                    _ladder.Collision.EarlyOutObjectType += PlatformerData.ActorObjectType;

                _ladder.Collision.TestEarlyOut = TestEarlyOut;

                _ladder.Collision.SolveOverlap = false;
                _ladder.Collision.ResolveCollision = null;

                // don't trust tile layer collision polygons!
                if (_ladder is T2DTileLayer)
                    _ladder.Collision.InstallImage(new T2DPolyImage());

                // if we have a collision component, default enabled to false so we don't chug resources
                Enabled = false;
            }

            return true;
        }

        #endregion

        //======================================================
        #region Private, protected, internal fields

        private T2DSceneObject _ladder;

        #endregion
    }
}
