using System;

using Microsoft.Xna.Framework;

using GarageGames.Torque.Core;
using GarageGames.Torque.Util;
using GarageGames.Torque.Sim;
using GarageGames.Torque.T2D;
using GarageGames.Torque.SceneGraph;
using GarageGames.Torque.MathUtil;

using GarageGames.Torque.PlatformerFramework;

namespace PlatformerStarter.Enemies.AIComponents.Components
{
    class LuaAIComponent : BaseAIComponent, IBehavior
    {
        protected LuaAIController controller;
        private string initScript;
        private string updateScript;

        #region Properties
        [System.Xml.Serialization.XmlIgnore]
        public BaseAIController Controller
        {
            get { return controller; }
            set { controller = (LuaAIController)value; }
        }

        public string InitScript
        {
            get { return initScript; }
            set { initScript = value; }
        }

        public string UpdateScript
        {
            get { return updateScript; }
            set { updateScript = value; }
        }
        #endregion

        protected override bool _OnRegister(TorqueObject owner)
        {
            if (!base._OnRegister(owner) || !(owner is T2DSceneObject))
                return false;

            controller = new LuaAIController();

            controller.AttackDist = AttackDist;
            controller.AlertDist = AlertDist;
            controller.Attacks = Attacks;
            controller.InitScript = initScript;
            controller.UpdateScript = updateScript;

            return true;
        }
    }
}
