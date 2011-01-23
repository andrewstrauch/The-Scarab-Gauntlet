using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using GarageGames.Torque.Platform;
using GarageGames.Torque.T2D;

namespace GarageGames.Torque.PlatformerFramework
{
    public static class TorqueTools
    {
        public static Vector2 ConvertLinkPointToWorld(T2DSceneObject gameObject, string linkPointName)
        {
            Vector2 linkPosition;
            float rotation;

            if (gameObject.LinkPoints.HasLinkPoint(linkPointName))
            {
                gameObject.LinkPoints.GetLinkPoint(linkPointName, out linkPosition, out rotation);

                float x = (gameObject.WorldClipRectangle.Width / 2) * linkPosition.X;
                float y = (gameObject.WorldClipRectangle.Height / 2) * linkPosition.Y;

                return new Vector2(x, y);
            }

            return Vector2.Zero;
        }
    }
}
