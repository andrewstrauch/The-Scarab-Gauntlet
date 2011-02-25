//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GarageGames.Torque.Core;
using GarageGames.Torque.Util;
using GarageGames.Torque.Materials;
using GarageGames.Torque.SceneGraph;
using GarageGames.Torque.GFX;
using GarageGames.Torque.RenderManager;
using GarageGames.Torque.MathUtil;



namespace GarageGames.Torque.T2D
{
    public class T2DStaticSprite : T2DSceneObject, IDisposable
    {
        #region Constructors

        public T2DStaticSprite()
        {
            // by default, we'll have physics and collision
            CreateWithCollision = true;
            CreateWithPhysics = true;
        }

        #endregion


        #region Public Properties

        /// <summary>
        /// Specifies the Sprite's Texture
        /// </summary>
        public RenderMaterial Material
        {
            // forward access to quad object
            get { return _quad != null ? _quad.Material : null; }
            set
            {
                if (_quad != null)
                    _quad.Material = value;
            }
        }



        /// <summary>
        /// Specifies the region of the material to use, as defined by the material's TextureDivider.
        /// </summary>
        public int MaterialRegionIndex
        {
            get { return _materialRegionIndex; }
            set { _materialRegionIndex = value; }
        }

        public override string AutoName
        {
            get
            {
                string autoname = base.AutoName;
                // add material name
                if (Material != null)
                    autoname += "_" + Material.AutoName;

                return autoname;
            }
        }

        #endregion


        #region Public Methods

        public override bool OnRegister()
        {
            if (!base.OnRegister())
                return false;

            return true;
        }



        public override void Render(SceneRenderState srs)
        {
#if DEBUG
            Profiler.Instance.StartBlock("T2DStaticSprite.Render");
#endif

            Vector2 halfSize = Vector2.Multiply(_size, 0.5f);

            Vector3 ScaleVector = new Vector3(halfSize.X, halfSize.Y, 1);

            Matrix objToWorld = Matrix.Identity;

            // scale
            Matrix ScaleMatrix = Matrix.CreateScale(ScaleVector);

            // translate            
            Matrix TranslationMatrix = Matrix.CreateTranslation(new Vector3(Position.X, Position.Y, LayerDepth));

            // rotate            
            Matrix RotationMatrix = Matrix.CreateRotationZ(MathHelper.ToRadians(Rotation));

            _quad.SetupUVs(Material.GetRegionCoords(_materialRegionIndex), FlipX, FlipY);

            objToWorld = ScaleMatrix * RotationMatrix * TranslationMatrix;

            // forward to quad
            _quad.Render(objToWorld, VisibilityLevel, srs);

            base.Render(srs);

#if DEBUG
            Profiler.Instance.EndBlock("T2DStaticSprite.Render");
#endif
        }

        public override object Clone()
        {
            if (_IsDisposed)
                return null;

            return base.Clone();
        }

        public override void CopyTo(TorqueObject obj)
        {
            base.CopyTo(obj);
            T2DStaticSprite obj2 = (T2DStaticSprite)obj;
            obj2.Material = this.Material;
            obj2.MaterialRegionIndex = this.MaterialRegionIndex;
        }



        public override void Dispose()
        {
            _IsDisposed = true;
            if (_quad != null)
                _quad.Dispose();
            _quad = null;
            base.Dispose();
        }


        #endregion


        #region Private, protected, internal fields

        RenderQuad _quad = new RenderQuad();
        protected int _materialRegionIndex;

        #endregion
    }
}
