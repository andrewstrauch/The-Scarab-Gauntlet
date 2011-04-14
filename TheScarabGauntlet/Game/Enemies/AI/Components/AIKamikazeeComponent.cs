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
    public class AIKamikazeeComponent : BaseAIComponent, IBehavior
    {
        protected AIKamikazeeController controller;

        #region Properties
        [System.Xml.Serialization.XmlIgnore]
        public BaseAIController Controller
        {
            get { return controller; }
            set { controller = (AIKamikazeeController)value; }
        }
        #endregion

        protected override bool _OnRegister(TorqueObject owner)
        {
            if (!base._OnRegister(owner) || !(owner is T2DSceneObject))
                return false;

            controller = new AIKamikazeeController();

            controller.AttackDist = minAttackDist;
            controller.AlertDist = maxAlertDist;
            controller.Attacks = attacks;

            return true;
        }
    }
}
