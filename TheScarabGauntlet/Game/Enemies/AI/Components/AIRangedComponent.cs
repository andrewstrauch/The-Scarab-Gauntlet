using System;

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
    public class AIRangedComponent : BaseAIComponent, IBehavior
    {
        protected AIRangedAttackController controller;

        #region Properties
        [System.Xml.Serialization.XmlIgnore]
        public BaseAIController Controller
        {
            get { return controller; }
            set { controller = (AIRangedAttackController)value; }
        }
        #endregion

        protected override bool _OnRegister(TorqueObject owner)
        {
            if (!base._OnRegister(owner) || !(owner is T2DSceneObject))
                return false;
            
            controller = new AIRangedAttackController();

            controller.AttackDist = AttackDist;
            controller.AlertDist = AlertDist;
            controller.Attacks = Attacks;

            return true;
        }
    }
}
