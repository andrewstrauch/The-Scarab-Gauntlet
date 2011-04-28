using System;
using Microsoft.Xna.Framework;

namespace PlatformerStarter.Common.Collectibles
{
    public interface IMovement
    {
        Vector2 StartingPosition
        {   set;   }
        Vector2 Position
        {   get;   }

        void Update(float dt);
        void Initialize();


    }
}
