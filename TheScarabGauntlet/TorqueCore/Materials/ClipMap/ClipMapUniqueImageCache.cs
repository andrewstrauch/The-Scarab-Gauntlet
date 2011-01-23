//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using MSXNA = Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.PackedVector;
using Microsoft.Xna.Framework.Content;

#if !XBOX
using System.Drawing;
#endif

using GarageGames.Torque.Core;
using GarageGames.Torque.GFX;
using GarageGames.Torque.MathUtil;
using System.IO;



namespace GarageGames.Torque.Materials.ClipMap
{
    public class ClipMapUniqueImageCache : IClipMapImageCache, IDisposable
    {

        #region Public properties

        /// <summary>
        /// The filename of the source texture file.
        /// </summary>
        public string SourceTextureFilename
        {
            get { return _sourceTextureFilename; }
            set { _sourceTextureFilename = value; }
        }

        #endregion


        #region Public methods

        public void Initialize(int clipMapSize, int clipMapDepth)
        {
#if !XBOX
            // clear any existing texture data
            _textureData.Clear();

            // load the source image file
            Bitmap image = null;
            if (File.Exists(_sourceTextureFilename))
                image = (Bitmap)Bitmap.FromFile(_sourceTextureFilename);

            int width;
            int height;
            int dataSize;
            uint[] data;
            int thisIndex = 0;

            if (image != null)
            {
                // create an array for mip level 0
                width = image.Width;
                height = image.Height;
                dataSize = width * height;
                data = new uint[dataSize];

                // get mip level 0
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        thisIndex = (y * width) + x;
                        data[thisIndex] = (uint)image.GetPixel(x, y).ToArgb();
                    }
                }
            }
            else
            {
                // create an array for mip level 0
                width = 256;
                height = 256;
                dataSize = width * height;
                data = new uint[dataSize];
            }

            // add mip level 0 to the texture data list
            _textureData.Add(data);

            // generate the rest of the mip levels
            int sourceLevel = 0;
            int topLIndex, bottomLIndex;
            uint topL, topR, bottomL, bottomR;

            do
            {
                width /= 2;
                height /= 2;
                dataSize = width * height;
                data = new uint[dataSize];

                // generate color data by averaging colors in 4 pixel area
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        thisIndex = (y * width) + x;
                        topLIndex = (y * width * 4) + (x * 2);
                        bottomLIndex = topLIndex + (width * 2);

                        topL = _textureData[sourceLevel][topLIndex];
                        topR = _textureData[sourceLevel][topLIndex + 1];
                        bottomL = _textureData[sourceLevel][bottomLIndex];
                        bottomR = _textureData[sourceLevel][bottomLIndex + 1];

                        data[thisIndex] = _GetLinearMipColor(topL, topR, bottomL, bottomR);
                    }
                }

                _textureData.Add(data);
                sourceLevel++;
            }
            while (width > 1 && height >= 1);
#else
            // load the texture
            Resource<Texture> sourceTexResource = ResourceManager.Instance.LoadTexture(_sourceTextureFilename);
            Texture2D sourceTex = sourceTexResource.Instance as Texture2D;

            // extract texture data from each mip level
            int texLevelWidth = sourceTex.Width;

            for (int i = 0; i < clipMapDepth; i++)
            {
                int dataSize = (texLevelWidth * texLevelWidth);
                uint[] data = new uint[dataSize];

                Rectangle rect = new Rectangle(0, 0, texLevelWidth, texLevelWidth);
                sourceTex.GetData<uint>(i, rect, data, 0, dataSize);

                _textureData.Add(data);

                texLevelWidth /= 2;
            }

            // dispose the texture
            sourceTex.Dispose();
#endif
        }



        public void BeginRectUpdates(int mipLevel, ClipStackEntry stackEntry) { }



        public void DoRectUpdate(int mipLevel, ClipStackEntry stackEntry, RectangleI srcRegion, RectangleI dstRegion)
        {
            // get an array of texture data
            int elementCount = dstRegion.Width * dstRegion.Height;
            uint[] dstData = new uint[elementCount];
            uint[] srcData = _textureData[mipLevel];

            // make local copies of the rectangle components for quick access
            int srcExtentX = srcRegion.Extent.X;
            int srcExtentY = srcRegion.Extent.Y;
            int srcIndex = srcRegion.Point.X + (srcRegion.Point.Y * (int)stackEntry.ScaleFactor * stackEntry.Texture.Width);

            for (int y = 0; y < srcExtentY; y++)
            {
                Array.Copy(srcData, srcIndex, dstData, y * srcExtentX, srcExtentX);
                srcIndex += (int)stackEntry.ScaleFactor * stackEntry.Texture.Width;
            }

            // send the new data to the stack entry texture
            MSXNA.Rectangle dstTextureRect = new MSXNA.Rectangle(dstRegion.X, dstRegion.Y, dstRegion.Width, dstRegion.Height);

            // replace the specified rect of texture data on the texture
#if XBOX
               stackEntry.Texture.SetData<uint>(0, dstTextureRect, dstData, 0, elementCount, SetDataOptions.None);
#else
            stackEntry.Texture.SetData<uint>(0, dstTextureRect, dstData, 0, elementCount, SetDataOptions.Discard);
#endif
        }



        public void FinishRectUpdates(int mipLevel, ClipStackEntry stackEntry) { }



        public IClipMapImageCache GetCopyOfInstance()
        {
            return this;
        }

        #endregion


        #region Private, protected, internal methods

        /// <summary>
        /// Returns the average color of four color values in ARGB uint format (essentailly, SurfaceFormat.Color). This is used
        /// in the Initialize method to generate a linear mip map of the source texture.
        /// </summary>
        /// <param name="color1O">The first of the four colors to average.</param>
        /// <param name="color2O">The second of the four colors to average.</param>
        /// <param name="color3O">The third of the four colors to average.</param>
        /// <param name="color4O">The fourth of the four colors to average.</param>
        /// <returns>Returns a color with R, G, B, and A components equal the averages of the passed colors' R, G, B, and A components.</returns>
        protected uint _GetLinearMipColor(uint color1, uint color2, uint color3, uint color4)
        {
            // declare variables to store the ARGB color components of each color
            uint color1A, color1R, color1G, color1B,
                color2A, color2R, color2G, color2B,
                color3A, color3R, color3G, color3B,
                color4A, color4R, color4G, color4B;

            // get the ARGB components of the four colors:
            // color 1
            color1B = color1 & 0xFF;
            color1 >>= 8;
            color1G = color1 & 0xFF;
            color1 >>= 8;
            color1R = color1 & 0xFF;
            color1 >>= 8;
            color1A = color1 & 0xFF;

            // color 2
            color2B = color2 & 0xFF;
            color2 >>= 8;
            color2G = color2 & 0xFF;
            color2 >>= 8;
            color2R = color2 & 0xFF;
            color2 >>= 8;
            color2A = color2 & 0xFF;

            // color 2
            color3B = color3 & 0xFF;
            color3 >>= 8;
            color3G = color3 & 0xFF;
            color3 >>= 8;
            color3R = color3 & 0xFF;
            color3 >>= 8;
            color3A = color3 & 0xFF;

            // color 3
            color4B = color4 & 0xFF;
            color4 >>= 8;
            color4G = color4 & 0xFF;
            color4 >>= 8;
            color4R = color4 & 0xFF;
            color4 >>= 8;
            color4A = color4 & 0xFF;

            // initialize the return color
            uint returnColor = 0;

            // average the four color components:
            // alpha
            returnColor += ((color1A + color2A + color3A + color4A) / 4) << 24;

            // red
            returnColor += ((color1R + color2R + color3R + color4R) / 4) << 16;

            //green
            returnColor += ((color1G + color2G + color3G + color4G) / 4) << 8;

            // blue
            returnColor += (color1B + color2B + color3B + color4B) / 4;

            // return the resulting color
            return returnColor;
        }

        #endregion


        #region Private, protected, internal fields

        List<uint[]> _textureData = new List<uint[]>();
        string _sourceTextureFilename;

        #endregion

        #region IDisposable Members

        public virtual void Dispose()
        {
            _textureData.Clear();
            _textureData = null;
            _sourceTextureFilename = null;
        }

        #endregion
    }
}
