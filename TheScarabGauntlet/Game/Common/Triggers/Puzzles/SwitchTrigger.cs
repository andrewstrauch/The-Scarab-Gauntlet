#region Using Directives
using System;
using System.Collections.Generic;

using PlatformerStarter.Common.Traps;

using Microsoft.Xna.Framework;

using GarageGames.Torque.T2D;
using GarageGames.Torque.Core;
using GarageGames.Torque.PlatformerFramework;
#endregion

namespace PlatformerStarter.Common.Triggers
{
    [TorqueXmlSchemaType]
    public class SwitchTrigger : DirectionalTriggerComponent
    {
        #region Private Members

        private bool switchedOn;
        private bool flipped;
        private List<ISwitchBehavior> behaviors;
        
        #endregion

        #region Public Properties

        /// <summary>
        /// The status of the switch (on/off)
        /// </summary>
        public bool IsOn
        {
            get { return switchedOn; }
            set { switchedOn = value; }
        }

        /// <summary>
        /// A list of behaviors that occur when the switch is flipped.
        /// </summary>
        public List<ISwitchBehavior> SwitchBehaviors
        {
            get { return behaviors; }
            set { behaviors = value; }
        }

        /// <summary>
        /// A flag indicating whether the switch has been activated or not.
        /// </summary>
        [System.Xml.Serialization.XmlIgnore]
        public bool Flipped
        {
            get { return flipped; }
            set { flipped = value; }
        }

        #endregion

        #region Public Routines

        /// <summary>
        /// Yummm... Copypasta....
        /// </summary>
        /// <param name="obj">Copypasta *drool*</param>
        public override void CopyTo(TorqueComponent obj)
        {
            base.CopyTo(obj);

            SwitchTrigger obj2 = obj as SwitchTrigger;
            obj2.IsOn = IsOn;
            obj2.SwitchBehaviors = SwitchBehaviors;
        }

        #endregion

        #region Private Routines

        /// <summary>
        /// Initializes the component.
        /// </summary>
        /// <param name="owner">The object this component is attached to.</param>
        /// <returns>Usually true (too tired to care).</returns>
        protected override bool _OnRegister(TorqueObject owner)
        {
            if (!base._OnRegister(owner) || !(owner is T2DSceneObject))
                return false;

            SceneObject.SetObjectType(PlatformerData.ActorTriggerObjectType, true);

            flipped = false;

            return true;
        }

        /// <summary>
        /// Frees up memory so we don't have a slow game.
        /// </summary>
        protected override void _OnUnregister()
        {
            base._OnUnregister();

            behaviors.Clear();
        }

        /// <summary>
        /// The routine run when the player collides with the object.
        /// </summary>
        /// <param name="ourObject">The trigger object.</param>
        /// <param name="theirObject">The player that collided with the trigger.</param>
        /// <param name="info">Some sort of info...</param>
        protected override void _onEnter(T2DSceneObject ourObject, T2DSceneObject theirObject, T2DCollisionInfo info)
        {
            if (!flipped)
            {
                switchedOn = !switchedOn;
                
                if (behaviors != null)
                    foreach (ISwitchBehavior behavior in behaviors)
                        behavior.Execute(ourObject, switchedOn);

                flipped = true;
            }
        }

        #endregion
    }
}
