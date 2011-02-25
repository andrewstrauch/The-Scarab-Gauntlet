using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using GarageGames.Torque.PlatformerFramework;
using GarageGames.Torque.Core;

namespace PlatformerStarter.Enemies
{
    public interface IBehavior
    {
        BaseAIController Controller
        {
            get;
            set;
        }

        void Initialize(TorqueObject owner);
    }
}
