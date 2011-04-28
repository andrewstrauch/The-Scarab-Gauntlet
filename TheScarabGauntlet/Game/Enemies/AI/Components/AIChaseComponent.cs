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

using GarageGames.Torque.PlatformerFramework;

namespace PlatformerStarter.Enemies.AIComponents
{
    [TorqueXmlSchemaType]
    public class AIChaseComponent : BaseAIComponent, IBehavior
    {
        protected AIChaseController controller;
        protected bool bounces;

        #region Properties
        [System.Xml.Serialization.XmlIgnore]
        public BaseAIController Controller
        {
            get { return controller; }
            set { controller = (AIChaseController)value; }
        }
        public bool Bounces
        {
            get { return bounces; }
            set 
            { 
                bounces = value;
                if(bounces)
                    attacks = false;
            }
        }
        #endregion

        public override void CopyTo(TorqueComponent obj)
        {
            base.CopyTo(obj);

            AIChaseComponent obj2 = obj as AIChaseComponent;

            obj2.Bounces = Bounces;
        }

        protected override bool _OnRegister(TorqueObject owner)
        {
            if (!base._OnRegister(owner))
                return false;

            controller = new AIChaseController();

            controller.AlertDist = AlertDist;
            controller.AttackDist = AttackDist;
            controller.Attacks = Attacks;
            controller.Bounces = Bounces;

            return true;
        }
    }
}
