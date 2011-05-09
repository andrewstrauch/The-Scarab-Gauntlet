#region Using Directives
using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework;

using GarageGames.Torque.Core;
using GarageGames.Torque.Util;
using GarageGames.Torque.Sim;
using GarageGames.Torque.T2D;
using GarageGames.Torque.SceneGraph;
using GarageGames.Torque.MathUtil;
#endregion

namespace PlatformerStarter.Common.Triggers
{
    [TorqueXmlSchemaType]
    public class PuzzleElementComponent : TorqueComponent, ITickObject
    {
        #region Private Members
        private bool allSwitchesOn;
        private List<SwitchTrigger> switches;
        private List<IPuzzleBehavior> behaviors;
        #endregion

        #region Public Properties

        /// <summary>
        /// A list of names of switches to find upon registering this object.
        /// </summary>
        public List<string> Switches
        {
            get;
            set;
        }

        /// <summary>
        /// A list of behaviors that will execute when a switch is flipped.
        /// </summary>
        public List<IPuzzleBehavior> PuzzleBehaviors
        {
            get { return behaviors; }
            set { behaviors = value; }
        }

        /// <summary>
        /// The object that this component is attached to.
        /// </summary>
        public T2DSceneObject SceneObject
        {
            get { return Owner as T2DSceneObject; }
        }

        #endregion

        #region Public Routines

        /// <summary>
        /// Checks whether all the switches are on or not.  If they are,
        /// the puzzle object runs all specified behaviors.  If not,
        /// it just sits there and looks pretty.
        /// </summary>
        /// <param name="move">Some move crap that's not applicable to this component.</param>
        /// <param name="dt">The time passed since last update.</param>
        public virtual void ProcessTick(Move move, float dt)
        {
            if (!allSwitchesOn)
            {
                allSwitchesOn = true;
                foreach (SwitchTrigger switchObj in switches)
                    allSwitchesOn &= switchObj.IsOn;
            }

            if (allSwitchesOn)
            {
                foreach (IPuzzleBehavior behavior in behaviors)
                    if(behavior.Active)
                        behavior.Execute(SceneObject);
            }
        }

        public virtual void InterpolateTick(float k)
        {
        }

        /// <summary>
        /// Copy that shit over, yo!
        /// </summary>
        /// <param name="obj">A new broheim that's being created.</param>
        public override void CopyTo(TorqueComponent obj)
        {
            base.CopyTo(obj);

            PuzzleElementComponent obj2 = obj as PuzzleElementComponent;

            obj2.Switches = Switches;
            obj2.PuzzleBehaviors = PuzzleBehaviors;
        }

        #endregion

        #region Private Routines

        /// <summary>
        /// Initializes the component.  Pretty straight-forward...
        /// </summary>
        /// <param name="owner">The object that "owns" this component.</param>
        /// <returns>True if everything went as expect, false otherwise.
        /// (Gotta love those ambiguous comments :-D)</returns>
        protected override bool _OnRegister(TorqueObject owner)
        {
            if (!base._OnRegister(owner) || !(owner is T2DSceneObject))
                return false;

            ProcessList.Instance.AddTickCallback(owner, this);
            
            switches = new List<SwitchTrigger>();
            GetSwitches();
            allSwitchesOn = false;

            return true;
        }

        /// <summary>
        /// Clears up the memory so the computer can remember more.
        /// </summary>
        protected override void _OnUnregister()
        {
            switches.Clear();
            behaviors.Clear();

            base._OnUnregister();
        }

        /// <summary>
        /// Finds the scene object and gets its SwitchTriggerComponent based on the 
        /// object's name.
        /// </summary>
        private void GetSwitches()
        {
            T2DSceneObject switchObj = null;
            SwitchTrigger switchComp = null;

            foreach (string switchName in Switches)
            {
                switchObj = TorqueObjectDatabase.Instance.FindObject<T2DSceneObject>(switchName);
                if (switchObj != null)
                {
                    switchComp = switchObj.Components.FindComponent<SwitchTrigger>();
                    if (switchComp != null)
                        switches.Add(switchComp);
                }
            }

            Switches.Clear();
        }

        #endregion

    }
}
