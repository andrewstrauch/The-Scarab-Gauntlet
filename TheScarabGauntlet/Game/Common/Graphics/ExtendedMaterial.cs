using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using GarageGames.Torque.Materials;
using GarageGames.Torque.SceneGraph;
using GarageGames.Torque.RenderManager;

namespace PlatformerStarter.Common.Graphics
{
    public class ExtendedMaterial : SimpleMaterial
    {
        #region Constructors
        
        public ExtendedMaterial()
        {
            EffectFilename = "data/effects/Effect1";
        }
        
        #endregion

        #region Private, protected, internal methods
        // Sets up this effect and selects a technique to render with.
        protected override string _SetupEffect(SceneRenderState srs, MaterialInstanceData materialData)
        {
            // For now, we'll accept whatever SimpleMaterial would use.
            base._SetupEffect(srs, materialData);

            return "Technique1";
        }

        // Performs per-object parameter setup.
        protected override void _SetupObjectParameters(RenderInstance renderInstance, SceneRenderState srs)
        {
            base._SetupObjectParameters(renderInstance, srs);

            // We'll access some instance data here soon.
        }

        // Loads the parameters from our effect into this material instance.
        protected override void _LoadParameters()
        {
            base._LoadParameters();

            // Here is where we'll find the parameters in our .fx file so we can set them from C# code.
        }

        // Clears references to parameters from this material.
        protected override void _ClearParameters()
        {
            // Any parameters you found in _LoadParameters are no longer valid when this is called, so you should
            // set them to null.

            base._ClearParameters();
        }
        #endregion

        //======================================================
        #region Private, protected, internal fields
        // We'll add our parameters here shortly.
        #endregion
    }
}
