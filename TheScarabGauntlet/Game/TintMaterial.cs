using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GarageGames.Torque.Materials;

namespace PlatformerStarter
{
    class TintMaterial : SimpleMaterial
    {
        private Vector4 tint = Color.White.ToVector4();
        //private Texture2D texture;

        public Color Tint
        {
            set
            {
                tint = value.ToVector4();
            }
        }
       // public Texture2D Texture
       // {
        //    set
        //    {
        //        texture = value;
        //    }
       // }



        public TintMaterial()
        {
            EffectFilename = "data/effects/CustomEffect";
        }

        protected override string _SetupEffect(GarageGames.Torque.SceneGraph.SceneRenderState srs, MaterialInstanceData materialData)
        {
            base._SetupEffect(srs, materialData);
            Effect.Instance.Parameters["Tint"].SetValue(tint);
            return "TintTechnique";
        }
    }
}
