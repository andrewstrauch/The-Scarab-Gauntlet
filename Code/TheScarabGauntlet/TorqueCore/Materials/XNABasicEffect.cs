//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using GarageGames.Torque.Core;
using GarageGames.Torque.GFX;
using GarageGames.Torque.SceneGraph;
using GarageGames.Torque.RenderManager;



namespace GarageGames.Torque.Materials
{
    /// <summary>
    /// Wraps an XNA BasicEffect for use with Torque X's rendering system. This is mostly used as a default
    /// material for XNA meshes.
    /// </summary>
    public class XNABasicEffect : RenderMaterial
    {

        #region Constructors

        public XNABasicEffect(Effect effect)
        {
            _effect = ResourceManager.Instance.CreateResource<Effect>(effect);
        }

        #endregion


        #region Private, protected, internal methods

        protected override void _SetupGlobalParameters(SceneRenderState srs, MaterialInstanceData materialData)
        {
            base._SetupGlobalParameters(srs, materialData);

            BasicEffect basicEffect = _effect.Instance as BasicEffect;
            if (basicEffect != null)
            {
                basicEffect.EnableDefaultLighting();
                basicEffect.View = srs.View;
                basicEffect.Projection = srs.Projection;
            }
            else
            {
                EffectParameter viewParam = _effect.Instance.Parameters["View"];
                viewParam.SetValue(srs.View);

                EffectParameter projectionParam = _effect.Instance.Parameters["Projection"];
                projectionParam.SetValue(srs.Projection);
            }
        }



        protected override void _SetupObjectParameters(RenderInstance renderInstance, SceneRenderState srs)
        {
            base._SetupObjectParameters(renderInstance, srs);

            BasicEffect basicEffect = _effect.Instance as BasicEffect;
            if (basicEffect != null)
                basicEffect.World = renderInstance.ObjectTransform;

            else
            {
                EffectParameter worldParam = _effect.Instance.Parameters["World"];
                worldParam.SetValue(srs.World.Top);
            }
        }

        #endregion
    }
}
