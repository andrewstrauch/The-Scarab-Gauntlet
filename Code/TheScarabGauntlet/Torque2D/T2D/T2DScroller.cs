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
using GarageGames.Torque.Materials;
using GarageGames.Torque.RenderManager;
using GarageGames.Torque.SceneGraph;
using GarageGames.Torque.Sim;
using GarageGames.Torque.Core;
using GarageGames.Torque.Util;



namespace GarageGames.Torque.T2D
{
    public class T2DScroller : T2DSceneObject, IAnimatedObject, IDisposable
    {
        #region Constructors

        public T2DScroller()
        {
            // disable collisions on scrollers by default
            CollisionsEnabled = false;
        }

        #endregion


        #region Public properties, operators, constants, and enums

        /// <summary>
        /// Material of the scroller
        /// </summary>
        public RenderMaterial Material
        {
            set { _q.Material = value; }
            get { return _q.Material; }
        }



        /// <summary>
        /// Changes the rate the scroller moves horizontally.
        /// </summary>
        public float ScrollRateX
        {
            set { _scrollX = value; }
            get { return _scrollX; }
        }



        /// <summary>
        /// Changes the rate the scroller moves vertically.
        /// </summary>
        public float ScrollRateY
        {
            set { _scrollY = value; }
            get { return _scrollY; }
        }



        /// <summary>
        /// Specifies the X offset of the scroller texture 
        /// </summary>
        public float TextureOffsetX
        {
            set { _offsetX = value; }
            get { return _offsetX; }
        }



        /// <summary>
        /// Specifies the Y offset of the scroller texture 
        /// </summary>
        public float TextureOffsetY
        {
            set { _offsetY = value; }
            get { return _offsetY; }
        }



        /// <summary>
        /// Specifies how many times the texture repeats horizontally accross the scroll area.
        /// </summary>
        public float TextureRepeatX
        {
            set { _repeatX = value; }
            get { return _repeatX; }
        }



        /// <summary>
        /// Specifies how many times the texture repeats vertically accross the scroll area.
        /// </summary>
        public float TextureRepeatY
        {
            set { _repeatY = value; }
            get { return _repeatY; }
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


        #region Public methods

        public override bool OnRegister()
        {
            if (!base.OnRegister())
                return false;

            ProcessList.Instance.AddAnimationCallback(this);
            return true;
        }

        public override void OnUnregister()
        {
            ProcessList.Instance.RemoveObject(this);
            base.OnUnregister();
        }

        public override void CopyTo(TorqueObject obj)
        {
            base.CopyTo(obj);
            T2DScroller obj2 = (T2DScroller)obj;
            obj2.Material = Material;
            obj2.ScrollRateX = ScrollRateX;
            obj2.ScrollRateY = ScrollRateY;
            obj2.TextureOffsetX = TextureOffsetX;
            obj2.TextureOffsetY = TextureOffsetY;
            obj2.TextureRepeatX = TextureRepeatX;
            obj2.TextureRepeatY = TextureRepeatY;
        }



        public override void Render(SceneRenderState srs)
        {
#if DEBUG
            Profiler.Instance.StartBlock("T2DScroller.Render");
#endif

            Matrix ScaleMatrix = Matrix.CreateScale(0.5f * Size.X, 0.5f * Size.Y, 1.0f);
            Matrix TranslationMatrix = Matrix.CreateTranslation(Position.X, Position.Y, LayerDepth);
            Matrix RotationMatrix = Matrix.CreateRotationZ(MathHelper.ToRadians(Rotation));
            Matrix ObjToWorld = ScaleMatrix * RotationMatrix * TranslationMatrix;

            if (_repeatX == 1.0f && _scrollX == 0.0f && _offsetX == 0.0f)
                _q.TexAddressU = TextureAddressMode.Clamp;
            else
                _q.TexAddressU = TextureAddressMode.Wrap;

            if (_repeatY == 1.0f && _scrollY == 0.0f && _offsetY == 0.0f)
                _q.TexAddressV = TextureAddressMode.Clamp;
            else
                _q.TexAddressV = TextureAddressMode.Wrap;

            // forward to quad
            _q.SetupUVs(_offsetX, _offsetY, _repeatX, _repeatY, FlipX, FlipY);
            _q.Render(ObjToWorld, VisibilityLevel, srs);

#if DEBUG
            Profiler.Instance.EndBlock("T2DScroller.Render");
#endif
        }



        public virtual void UpdateAnimation(float dt)
        {
#if DEBUG
            Profiler.Instance.StartBlock("T2DScroller.UpdateAnimation");
#endif

            // Calculate texel shift per world-unit.
            float scrollTexelX = _repeatX / Size.X;
            float scrollTexelY = _repeatY / Size.Y;

            float scrollOffsetX = scrollTexelX * _scrollX * dt;
            float scrollOffsetY = scrollTexelY * _scrollY * dt;

            // Calculate new offset.
            _offsetX = (_offsetX + scrollOffsetX) % 1.0f;
            _offsetY = (_offsetY + scrollOffsetY) % 1.0f;

#if DEBUG
            Profiler.Instance.EndBlock("T2DScroller.UpdateAnimation");
#endif
        }



        public override void Dispose()
        {
            _IsDisposed = true;
            _q.Dispose();
            base.Dispose();
        }

        #endregion


        #region Private, protected, internal fields

        private float _scrollX = 0.0f;
        private float _scrollY = 0.0f;
        private float _offsetX = 0.0f;
        private float _offsetY = 0.0f;
        private float _repeatX = 1.0f;
        private float _repeatY = 1.0f;

        private RenderQuad _q = new RenderQuad();

        #endregion
    }
}
