//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GarageGames.Torque.Core;
using GarageGames.Torque.GFX;
using GarageGames.Torque.Materials;
using GarageGames.Torque.MathUtil;
using GarageGames.Torque.SceneGraph;
using GarageGames.Torque.XNA;
using GarageGames.Torque.Util;
using GarageGames.Torque.RenderManager;



namespace GarageGames.Torque.GUI
{
    /// <summary>
    /// DrawUtil provides convenient 2D drawing capabilities used by the GUI.
    /// </summary>
    public class DrawUtil
    {
        #region Static methods, fields, constructors


        static DrawUtil()
        {
            _vertexSet4[0] = new GFXVertexFormat.PCTTBN();
            _vertexSet4[1] = new GFXVertexFormat.PCTTBN();
            _vertexSet4[2] = new GFXVertexFormat.PCTTBN();
            _vertexSet4[3] = new GFXVertexFormat.PCTTBN();

            _vertexSet10[0] = new GFXVertexFormat.PCTTBN();
            _vertexSet10[1] = new GFXVertexFormat.PCTTBN();
            _vertexSet10[2] = new GFXVertexFormat.PCTTBN();
            _vertexSet10[3] = new GFXVertexFormat.PCTTBN();
            _vertexSet10[4] = new GFXVertexFormat.PCTTBN();
            _vertexSet10[5] = new GFXVertexFormat.PCTTBN();
            _vertexSet10[6] = new GFXVertexFormat.PCTTBN();
            _vertexSet10[7] = new GFXVertexFormat.PCTTBN();
            _vertexSet10[8] = new GFXVertexFormat.PCTTBN();
            _vertexSet10[9] = new GFXVertexFormat.PCTTBN();
        }



        /// <summary>
        /// Sets up the draw util.
        /// </summary>
        public static void Setup()
        {
            // set up the default gui material here
            _defaultMaterial = new Materials.SimpleMaterial();
            _defaultMaterial.TextureFilename = null;
            _defaultMaterial.IsTranslucent = true;

            // set up the default render instance to be used when rendering rects
            _rectRenderInstance = new RenderInstance();
            _rectRenderInstance.ObjectTransform = Matrix.Identity;

            // set up the vertex buffer
            _CreateVertexBuffer();
        }



        /// <summary>
        /// Sets up an orthographical viewport and clipping region.
        /// </summary>
        public static RectangleF ClipRect
        {
            get { return _clipRect; }
            set
            {
                _clipRect = value;

                _texLeft = _clipRect.X;
                _texRight = _clipRect.X + _clipRect.Width;
                _texBottom = _clipRect.Y + _clipRect.Height;
                _texTop = _clipRect.Y;

                // setup projection matrix
                _clipMatrix = GFXDevice.Instance.SetOrtho(false, _texLeft, _texRight, _texBottom, _texTop, 0.0f, 1.0f);

                viewport.X = (int)_clipRect.X;
                viewport.Y = (int)_clipRect.Y;
                viewport.Width = (int)_clipRect.Width;
                viewport.Height = (int)_clipRect.Height;
                viewport.MinDepth = 0.0f;
                viewport.MaxDepth = 1.0f;

                GFXDevice.Instance.Device.Viewport = viewport;
            }
        }



        /// <summary>
        /// Modulates a texture's color value based on the given modulation. This can
        /// be used to 'tint' a texture to a specific color.
        /// </summary>
        public static Color BitmapModulation
        {
            get { return _bitmapModulation; }
            set { _bitmapModulation = value; }
        }



        /// <summary>
        /// Resets the modulated texture color value to normal.
        /// </summary>
        public static void ClearBitmapModulation()
        {
            _bitmapModulation = new Color(255, 255, 255, 255);
        }



        /// <summary>
        /// Draws an untextured filled rectangle from the upper left, <paramref name="a"/>,
        /// of the rectangle to the lower right, <paramref name="b"/> in the specified
        /// color, <paramref name="color"/>.
        /// </summary>
        public static void RectFill(Vector2 a, Vector2 b, Color color)
        {
            //
            // Convert Box   a----------x
            //               |          |
            //               x----------b
            // Into Quad
            //               v0---------v1
            //               | a       x |
            //			     |           |
            //               | x       b |
            //               v2---------v3
            //

            // setup render state
            GUICanvas.Instance.RenderState.World.LoadIdentity();
            GUICanvas.Instance.RenderState.View = Matrix.Identity;
            GUICanvas.Instance.RenderState.Projection = _clipMatrix;

            // setup the material
            _defaultMaterial.SetupEffect(GUICanvas.Instance.RenderState, null);


            Vector2 nw = new Vector2(-0.5f, -0.5f);
            Vector2 ne = new Vector2(+0.5f, -0.5f);

            _vertexSet4[0].Position = new Vector3(a.X + nw.X, a.Y + nw.Y, 0.0f);
            _vertexSet4[0].Color = color;
            _vertexSet4[1].Position = new Vector3(b.X + ne.X, a.Y + ne.Y, 0.0f);
            _vertexSet4[1].Color = color;
            _vertexSet4[2].Position = new Vector3(a.X - ne.X, b.Y - ne.Y, 0.0f);
            _vertexSet4[2].Color = color;
            _vertexSet4[3].Position = new Vector3(b.X - nw.X, b.Y - nw.Y, 0.0f);
            _vertexSet4[3].Color = color;

            // set up the material
            _defaultMaterial.SetupObject(_rectRenderInstance, GUICanvas.Instance.RenderState);

            GUICanvas.Instance.RenderState.Gfx.Device.VertexDeclaration = GFXVertexFormat.GetVertexDeclaration(GUICanvas.Instance.RenderState.Gfx.Device);

            // draw the vertices
            while (_defaultMaterial.SetupPass())
            {
                GUICanvas.Instance.RenderState.Gfx.Device.DrawUserPrimitives<GFXVertexFormat.PCTTBN>(PrimitiveType.TriangleStrip, _vertexSet4, 0, 2);
            }

            // clean up the material
            _defaultMaterial.CleanupEffect();
        }



        /// <summary>
        /// Draws an untextured filled rectangle from <paramref name="rect"/> in the specified
        /// color, <paramref name="color"/>.
        /// </summary>
        public static void RectFill(RectangleF rect, Color color)
        {
            lowerRight = new Vector2(rect.Width + rect.X - 1, rect.Height + rect.Y - 1);
            DrawUtil.RectFill(rect.Point, lowerRight, color);
        }



        /// <summary>
        /// Draws an untextured wireframe rectangle from the upper left, <paramref name="a"/>,
        /// of the rectangle to the lower right, <paramref name="b"/> in the specified
        /// color, <paramref name="color"/>.
        /// </summary>
        public static void Rect(Vector2 a, Vector2 b, Color color)
        {
            //
            // Convert Box   a----------x
            //               |          |
            //               x----------b
            //
            // Into tri-Strip Outline
            //               v2-----------v0
            //               | a         x |
            //               |  v1-----v7  |
            //               |   |     |   |
            //               |  v3-----v5  |
            //               | x         b |
            //               v4-----------v6
            //


            // setup render state
            GUICanvas.Instance.RenderState.World.LoadIdentity();
            GUICanvas.Instance.RenderState.View = Matrix.Identity;
            GUICanvas.Instance.RenderState.Projection = _clipMatrix;

            // setup the material
            _defaultMaterial.SetupEffect(GUICanvas.Instance.RenderState, null);


            Vector2 nw = new Vector2(-0.5f, -0.5f);
            Vector2 ne = new Vector2(+0.5f, -0.5f);

            _vertexSet10[0].Position = new Vector3(b.X + ne.X, a.Y + ne.Y, 0.0f);
            _vertexSet10[0].Color = color;

            _vertexSet10[1].Position = new Vector3(a.X - nw.X, a.Y - nw.Y, 0.0f);
            _vertexSet10[1].Color = color;

            _vertexSet10[2].Position = new Vector3(a.X + nw.X, a.Y + nw.Y, 0.0f);
            _vertexSet10[2].Color = color;

            _vertexSet10[3].Position = new Vector3(a.X + ne.X, b.Y + ne.Y, 0.0f);
            _vertexSet10[3].Color = color;

            _vertexSet10[4].Position = new Vector3(a.X - ne.X, b.Y - ne.Y, 0.0f);
            _vertexSet10[4].Color = color;

            _vertexSet10[5].Position = new Vector3(b.X + nw.X, b.Y + nw.Y, 0.0f);
            _vertexSet10[5].Color = color;

            _vertexSet10[6].Position = new Vector3(b.X - nw.X, b.Y - nw.Y, 0.0f);
            _vertexSet10[6].Color = color;

            _vertexSet10[7].Position = new Vector3(b.X - ne.X, a.Y - ne.Y, 0.0f);
            _vertexSet10[7].Color = color;

            _vertexSet10[8].Position = new Vector3(b.X + ne.X, a.Y + ne.Y, 0.0f); // same as v0
            _vertexSet10[8].Color = color;

            _vertexSet10[9].Position = new Vector3(a.X - nw.X, a.Y - nw.Y, 0.0f); // same as v1
            _vertexSet10[9].Color = color;

            GUICanvas.Instance.RenderState.Gfx.Device.VertexDeclaration = GFXVertexFormat.GetVertexDeclaration(GUICanvas.Instance.RenderState.Gfx.Device);

            // set up the material
            _defaultMaterial.SetupObject(_rectRenderInstance, GUICanvas.Instance.RenderState);

            // draw the vertices
            while (_defaultMaterial.SetupPass())
                GUICanvas.Instance.RenderState.Gfx.Device.DrawUserPrimitives<GFXVertexFormat.PCTTBN>(PrimitiveType.TriangleStrip, _vertexSet10, 0, 8);

            // cleanup the material
            _defaultMaterial.CleanupEffect();
        }



        /// <summary>
        /// Draws an untextured wireframe rectangle from <paramref name="rect"/> in the specified
        /// color, <paramref name="color"/>.
        /// </summary>
        public static void Rect(RectangleF rect, Color color)
        {
            Vector2 lowerRight = new Vector2(rect.Width + rect.X - 1, rect.Height + rect.Y - 1);
            DrawUtil.Rect(rect.Point, lowerRight, color);
        }



        /// <summary>
        /// Draws a stretched sub-region of a texture.
        /// </summary>
        /// <param name="material">The material used when rendering. The material must implement ITextureMaterial.</param>
        /// <param name="dstRect">Rectangle where the texture object will be drawn.</param>
        /// <param name="srcRect">Sub-region of the texture that will be applied over the <paramref name="dstRect"/>.</param>
        /// <param name="flipMode">Any flipping to be done of the source texture.</param>
        public static void BitmapStretchSR(RenderMaterial material, RectangleF dstRect, RectangleF srcRect, BitmapFlip flipMode)
        {
            // setup render state
            GUICanvas.Instance.RenderState.World.LoadIdentity();
            GUICanvas.Instance.RenderState.View = Matrix.Identity;
            GUICanvas.Instance.RenderState.Projection = _clipMatrix;

            // setup the material
            material.SetupEffect(GUICanvas.Instance.RenderState, null);

            _texLeft = srcRect.X / ((Texture2D)((ITextureMaterial)material).Texture.Instance).Width;
            _texRight = (srcRect.X + srcRect.Width) / ((Texture2D)((ITextureMaterial)material).Texture.Instance).Width;
            _texTop = srcRect.Y / ((Texture2D)((ITextureMaterial)material).Texture.Instance).Height;
            _texBottom = (srcRect.Y + srcRect.Height) / ((Texture2D)((ITextureMaterial)material).Texture.Instance).Height;

            _screenLeft = dstRect.X;
            _screenRight = dstRect.X + dstRect.Width;
            _screenTop = dstRect.Y;
            _screenBottom = dstRect.Y + dstRect.Height;

            // flip x
            if ((flipMode & BitmapFlip.FlipX) != 0)
            {
                float temp = _texLeft;
                _texLeft = _texRight;
                _texRight = temp;
            }

            // flip y
            if ((flipMode & BitmapFlip.FlipY) != 0)
            {
                float temp = _texTop;
                _texTop = _texBottom;
                _texBottom = temp;
            }

            color = _bitmapModulation;

            _vertexSet4[0].Position = new Vector3(_screenLeft - 0.5f, _screenTop - 0.5f, 0.0f);
            _vertexSet4[0].TextureCoordinate = new Vector2(_texLeft, _texTop);
            _vertexSet4[0].Color = color;

            _vertexSet4[1].Position = new Vector3(_screenRight - 0.5f, _screenTop - 0.5f, 0.0f);
            _vertexSet4[1].TextureCoordinate = new Vector2(_texRight, _texTop);
            _vertexSet4[1].Color = color;

            _vertexSet4[2].Position = new Vector3(_screenLeft - 0.5f, _screenBottom - 0.5f, 0.0f);
            _vertexSet4[2].TextureCoordinate = new Vector2(_texLeft, _texBottom);
            _vertexSet4[2].Color = color;

            _vertexSet4[3].Position = new Vector3(_screenRight - 0.5f, _screenBottom - 0.5f, 0.0f);
            _vertexSet4[3].TextureCoordinate = new Vector2(_texRight, _texBottom);
            _vertexSet4[3].Color = color;

            // adltodo: hacks
            _workingRenderInstance = SceneRenderer.RenderManager.AllocateInstance();
            _workingRenderInstance.ObjectTransform = Matrix.Identity;

            GUICanvas.Instance.RenderState.Gfx.Device.VertexDeclaration = GFXVertexFormat.GetVertexDeclaration(GUICanvas.Instance.RenderState.Gfx.Device);

            // draw the vertices
            while (material.SetupPass())
            {
                material.SetupObject(_workingRenderInstance, GUICanvas.Instance.RenderState);

                GUICanvas.Instance.RenderState.Gfx.Device.DrawUserPrimitives<GFXVertexFormat.PCTTBN>(PrimitiveType.TriangleStrip, _vertexSet4, 0, 2);
            }

            // cleanup the material
            material.CleanupEffect();
            SceneRenderer.RenderManager.FreeInstance(_workingRenderInstance);
        }



        /// <summary>
        /// Draws an unstretched bitmap.
        /// </summary>
        /// <param name="material">The material used when rendering. The material must implement ITextureMaterial.</param>
        /// <param name="position">Where to draw the texture in 2d coordinates.</param>
        /// <param name="flipMode">Any flipping to be done of the source texture.</param>
        public static void Bitmap(RenderMaterial material, Vector2 position, BitmapFlip flipMode)
        {
            Assert.Fatal(material != null, "No material specified for DrawUtil::Bitmap");

            if (((ITextureMaterial)material).Texture.IsNull)
            {
                Texture2D texture = ((Texture2D)ResourceManager.Instance.LoadTexture((material as ITextureMaterial).TextureFilename).Instance);
                subRegion = new RectangleF(0.0f, 0.0f, texture.Width, texture.Height);
                stretch = new RectangleF(position.X, position.Y, ((Texture2D)((ITextureMaterial)material).Texture.Instance).Width, ((Texture2D)((ITextureMaterial)material).Texture.Instance).Height);
            }
            else
            {
                subRegion = new RectangleF(0.0f, 0.0f, ((Texture2D)((ITextureMaterial)material).Texture.Instance).Width, ((Texture2D)((ITextureMaterial)material).Texture.Instance).Height);
                stretch = new RectangleF(position.X, position.Y, ((Texture2D)((ITextureMaterial)material).Texture.Instance).Width, ((Texture2D)((ITextureMaterial)material).Texture.Instance).Height);
            }

            DrawUtil.BitmapStretchSR(material, stretch, subRegion, flipMode);
        }



        /// <summary>
        /// Draws a stretched bitmap.
        /// </summary>
        /// <param name="material">The material used when rendering. The material must implement ITextureMaterial.</param>
        /// <param name="dstRect">Rectangle where the texture object will be drawn.</param>
        /// <param name="flipMode">Any flipping to be done of the source texture.</param>
        public static void BitmapStretch(RenderMaterial material, RectangleF dstRect, BitmapFlip flipMode)
        {
            Assert.Fatal(material != null, "No material specified for DrawUtil::BitmapStretch");

            if (((ITextureMaterial)material).Texture.IsNull)
            {
                Texture2D texture = ((Texture2D)ResourceManager.Instance.LoadTexture((material as ITextureMaterial).TextureFilename).Instance);
                subRegion = new RectangleF(0.0f, 0.0f, texture.Width, texture.Height);
            }
            else
            {
                subRegion = new RectangleF(0.0f, 0.0f, ((Texture2D)((ITextureMaterial)material).Texture.Instance).Width, ((Texture2D)((ITextureMaterial)material).Texture.Instance).Height);
            }

            DrawUtil.BitmapStretchSR(material, dstRect, subRegion, flipMode);
        }



        /// <summary>
        /// Draws an unstretched sub-region of a texture.
        /// </summary>
        /// <param name="material">The material used when rendering. The material must implement ITextureMaterial.</param>
        /// <param name="position">Where to draw the texture in 2d coordinates.</param>
        /// <param name="srcRect">Sub-region of the texture to be drawn.</param>
        /// <param name="flipMode">Any flipping to be done of the source texture.</param>
        public static void BitmapSR(RenderMaterial material, Vector2 position, RectangleF srcRect, BitmapFlip flipMode)
        {
            Assert.Fatal(material != null, "No texture specified for DrawUtil::BitmapSR");

            RectangleF stretch = new RectangleF(position.X, position.Y, srcRect.Width, srcRect.Height);
            DrawUtil.BitmapStretchSR(material, stretch, srcRect, flipMode);
        }



        /// <summary>
        /// Deaws text at a location in the 2d gui coordinates.
        /// </summary>
        /// <param name="font">The font to draw with, usually specified by a GUIStyle.</param>
        /// <param name="offset">Point where to start drawing the text.</param>
        /// <param name="size">Margins used to calculate the text alignment.</param>
        /// <param name="alignment">Justification of the text relative to the offset.</param>
        /// <param name="color">The color to draw the text as.</param>
        /// <param name="text">The string to be drawn.</param>
        public static void JustifiedText(Resource<SpriteFont> font, Vector2 offset, Vector2 size, TextAlignment alignment, Color color, string text)
        {
            if (font.IsNull)
                return;

            float textWidth = font.Instance.MeasureString(text).X;
            Vector2 start;

            switch (alignment)
            {
                case TextAlignment.JustifyRight:
                    start = new Vector2(size.X - textWidth, 0.0f);
                    break;
                case TextAlignment.JustifyCenter:
                    start = new Vector2((size.X - textWidth) / 2.0f, 0.0f);
                    break;
                default:
                    start = new Vector2(0.0f, 0.0f);
                    break;
            }

            // if the text is longer then the box size it will get clipped,
            // so force left justify
            if (textWidth > size.X)
                start = new Vector2(0.0f, 0.0f);

            // center vertical
            if (font.Instance.LineSpacing > size.Y)
                start.Y = 0.0f - ((font.Instance.LineSpacing - size.Y) / 2.0f);
            else
                start.Y = (size.Y - font.Instance.LineSpacing) / 2.0f;

            FontRenderer.Instance.DrawString(font, start + offset, color, text);
        }



        private static void _CreateVertexBuffer()
        {
            if (_vertexBuffer.IsNull)
                _vertexBuffer = ResourceManager.Instance.CreateVertexBuffer(ResourceProfiles.AutomaticStaticVBProfile, vertexSizeInBytes);

            Assert.Fatal(!_vertexBuffer.IsNull, "DrawUtil::_CreateVertexBuffer: failed to create a vertex buffer!");
        }


        private static void _CreateDynamicVertexBuffer()
        {
            if (_dynamicvertexBuffer.IsNull)
                _dynamicvertexBuffer = ResourceManager.Instance.CreateDynamicVertexBuffer(ResourceProfiles.AutomaticStaticVBProfile, vertexSizeInBytes);

            Assert.Fatal(!_vertexBuffer.IsNull, "DrawUtil::_CreateVertexBuffer: failed to create a vertex buffer!");
        }


        static int vertexSizeInBytes = 1024 * GFXVertexFormat.VertexSize;

        static RenderInstance _rectRenderInstance;
        static RectangleF _clipRect;
        static Matrix _clipMatrix;
        static Color _bitmapModulation = new Color(255, 255, 255, 255);
        static Materials.SimpleMaterial _defaultMaterial;
        static Resource<VertexBuffer> _vertexBuffer;
        static Resource<DynamicVertexBuffer> _dynamicvertexBuffer;

        static float _texLeft;
        static float _texRight;
        static float _texTop;
        static float _texBottom;

        static float _screenLeft;
        static float _screenRight;
        static float _screenTop;
        static float _screenBottom;

        static Vector2 lowerRight;

        static RectangleF subRegion = new RectangleF();
        static RectangleF stretch = new RectangleF();

        static Color color = _bitmapModulation;

        static GFXVertexFormat.PCTTBN[] _vertexSet4 = TorqueUtil.GetScratchArray<GFXVertexFormat.PCTTBN>(4);
        static GFXVertexFormat.PCTTBN[] _vertexSet10 = TorqueUtil.GetScratchArray<GFXVertexFormat.PCTTBN>(10);
        static RenderInstance _workingRenderInstance = SceneRenderer.RenderManager.AllocateInstance();

        static Viewport viewport = new Viewport();

        #endregion
    }
}
