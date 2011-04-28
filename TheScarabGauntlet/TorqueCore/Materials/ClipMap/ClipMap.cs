//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GarageGames.Torque.Core;
using GarageGames.Torque.MathUtil;
using GarageGames.Torque.Materials;
using GarageGames.Torque.GFX;
using GarageGames.Torque.Util;
using GarageGames.Torque.Sim;



namespace GarageGames.Torque.Materials.ClipMap
{
    /// <summary>
    /// Image cache interface for clip maps.
    /// </summary>
    public interface IClipMapImageCache
    {

        #region Interface methods

        /// <summary>
        /// Initialize the image cache with the specified information.
        /// </summary>
        /// <param name="clipMapSize">The height and width of the clip stack textures.</param>
        /// <param name="clipMapDepth">The number of clip levels in the clip stack.</param>
        void Initialize(int clipMapSize, int clipMapDepth);



        /// <summary>
        /// Prepare to do rect updates on the specified stack entry.
        /// </summary>
        /// <param name="mipLevel">The stack level of the specified entry.</param>
        /// <param name="stackEntry">The stack entry on which the updates will occur.</param>
        void BeginRectUpdates(int mipLevel, ClipStackEntry stackEntry);



        /// <summary>
        /// Updates the specified region of the stack entry with information from the specified region of source data.
        /// </summary>
        /// <param name="mipLevel">The stack level of the specified entry.</param>
        /// <param name="stackEntry">The stack entry to update</param>
        /// <param name="srcRegion">The region of source data to grab.</param>
        /// <param name="dstRegion">The region of the stack entry's texture to write the source data to.</param>
        void DoRectUpdate(int mipLevel, ClipStackEntry stackEntry, RectangleI srcRegion, RectangleI dstRegion);



        /// <summary>
        /// Complete rect updates on the specified stack entry.
        /// </summary>
        /// <param name="mipLevel">The stack level of the specified entry.</param>
        /// <param name="stackEntry">The entry to finish updating.</param>
        void FinishRectUpdates(int mipLevel, ClipStackEntry stackEntry);



        /// <summary>
        /// This is NOT a clone method!! Returns a copy of this image cache instance. The purpose is to allow image caches that
        /// require unique data for different clip maps in the same clip map effect. Unique and Debug image caches will just
        /// return themselves. The purpose is to allow the Blender image cache (and any other similar image cache) to return
        /// a copy instead of itself. This is neccesary because of how render targets work on PC: when the blender image cache
        /// is initialized it creates render targets. When it's re-initialized it disposes the existing ones and creates a new
        /// set. This results in the old clip map updating the new clip map's clip stack (which is bad!). Use this only for the
        /// purposes of getting a copy of an image cache for the same clip map effect this instance is assigned to. For example,
        /// when adding a duplicate ClipMap to an effect for the purposes of Split-Screen.
        /// </summary>
        /// <returns>
        /// This is NOT a clone method!! Returns a copy of this image cache instance. The purpose is to allow image caches that
        /// require unique data for different clip maps in the same clip map effect. Unique and Debug image caches will just
        /// return themselves. The purpose is to allow the Blender image cache (and any other similar image cache) to return
        /// a copy instead of itself. This is neccesary because of how render targets work on PC: when the blender image cache
        /// is initialized it creates render targets. When it's re-initialized it disposes the existing ones and creates a new
        /// set. This results in the old clip map updating the new clip map's clip stack (which is bad!). Use this only for the
        /// purposes of getting a copy of an image cache for the same clip map effect this instance is assigned to. For example,
        /// when adding a duplicate ClipMap to an effect for the purposes of Split-Screen.
        /// </returns>
        IClipMapImageCache GetCopyOfInstance();

        #endregion
    }



    /// <summary>
    /// A single clip stack entry on a clip map's clip stack. Stores texture and scaling data relevant to a single clip stack level.
    /// </summary>
    public class ClipStackEntry : IDisposable
    {

        #region Constructors

        public ClipStackEntry(Texture2D texture, float scaleFactor)
        {
            _texture = texture;
            _scaleFactor = scaleFactor;
        }

        #endregion


        #region Public properties

        /// <summary>
        /// The texture object this stack level is currently using. Usually not changed (except in the 
        /// case of the ClipMapBlenderImageCache, which uses render targets to blend data during rect updates).
        /// </summary>
        public Texture2D Texture
        {
            get { return _texture; }
            set { _texture = value; }
        }



        /// <summary>
        /// The number of times this stack entry wraps within the full texture.
        /// </summary>
        public float ScaleFactor
        {
            get { return _scaleFactor; }
        }



        /// <summary>
        /// The center that this stack entry is focussing on. This is set on a per-entry basis to allow
        /// clip map levels to update independently.
        /// </summary>
        public Vector2 ClipCenter
        {
            get { return _clipCenter; }
            set { _clipCenter = value; }
        }

        /// <summary>
        /// The current offset within the this stack entry's source texture.
        /// </summary>
        public Point ToroidalOffset
        {
            get { return _toroidalOffset; }
            set { _toroidalOffset = value; }
        }

        #endregion


        #region Private, protected, and internal fields

        Texture2D _texture;
        float _scaleFactor = 1.0f;
        Vector2 _clipCenter = new Vector2(0.5f, 0.5f);
        Point _toroidalOffset = new Point(0, 0);

        #endregion

        #region IDisposable Members

        public virtual void Dispose()
        {
            _texture = null;
        }

        #endregion
    }



    /// <summary>
    /// Core clip map class. Manages the clip stack and tells the image cache when and 
    /// where clip stack entries need new texture data.
    /// </summary>
    public class ClipMap : TorqueObject, IAnimatedObject, IDisposable
    {

        #region Constructors

        public ClipMap()
        {
            // use _clipMapSize^2 * 2 max texel per upload default;
            _maxTexelUploadPerRecenter = _clipMapSize * _clipMapSize * 2;

            ProcessList.Instance.AddAnimationCallback(this);
        }

        #endregion


        #region Public properties

        /// <summary>
        /// The full width of the virtual texture that data is pulled from in texels.
        /// </summary>
        public int TextureSize
        {
            get { return _textureSize; }
            set { _textureSize = value; }
        }



        /// <summary>
        /// The actual width of the clip stack in texels.
        /// </summary>
        public int ClipMapSize
        {
            get { return _clipMapSize; }
            set { _clipMapSize = value; }
        }



        /// <summary>
        /// The number of levels in the clip stack, as set by InitClipStack().
        /// </summary>
        public int ClipStackDepth
        {
            get { return _clipStackDepth; }
        }



        /// <summary>
        /// The maximum number of texels to update during a recenter call. Note that if MinUpdateDelta is passed, at least one level 
        /// will always be fully udpdated during a recenter, even if MaxTexelUploadPerRecenter is set to zero.
        /// </summary>
        public int MaxTexelUploadPerRecenter
        {
            get { return _maxTexelUploadPerRecenter; }
            set { _maxTexelUploadPerRecenter = value; }
        }



        /// <summary>
        /// The difference in source texture texel position to allow a Recenter to reach before an update is performed. This is 
        /// level-dependent because the textures at higher levels are stretched more.
        /// </summary>
        public int MinUpdateDelta
        {
            get { return _minUpdateDelta; }
            set { _minUpdateDelta = value; }
        }



        /// <summary>
        /// The image cache from which to pull source data.
        /// </summary>
        public IClipMapImageCache ImageCache
        {
            get { return _imageCache; }
            set
            {
                _imageCache = value;
                _imageCache.Initialize(_clipMapSize, _clipStackDepth);
            }
        }



        /// <summary>
        /// The list of clip stack entries that this clip map is currently using.
        /// </summary>
        public List<ClipStackEntry> ClipLevels
        {
            get { return _clipLevels; }
        }



        /// <summary>
        /// Set the center of the clip map and marks it for recentering when it's updated.
        /// </summary>
        public Vector2 Center
        {
            get { return _center; }
            set
            {
                if (_center == value)
                    return;

                _center = value;
                _needsRecenter = true;
            }
        }

        #endregion


        #region Public methods

        /// <summary>
        /// Sets the image cache for the clip map and initializes the clip stack.
        /// </summary>
        /// <param name="imageCache">The image cache to use.</param>
        /// <param name="textureSize">The total size of the virtual texture this clip map will serve data from.</param>
        /// <param name="clipMapSize">The width of the clip stack in texels. This will be the height and width of each clip level.</param>
        public void Initialize(IClipMapImageCache imageCache, int textureSize, int clipMapSize)
        {
            Assert.Fatal(TextureSize >= ClipMapSize, "ClipMap.Initialize - The clipMapSize parameter must bes smaller than the textureSize parameter. Can't make a clip map bigger than the virtual texture it represents!");
            Assert.Fatal(imageCache != null, "ClipMap.Initialize - Error: Image cache is null.");

            // ORDER HERE IS IMPORTANT:
            // -clip stack must be initialized after clip map size and texture scale are set
            // -image cache must be set after clip stack has been initialized
            // -clip stack can then be filled with texture data from the image cache
            TextureSize = textureSize;
            ClipMapSize = clipMapSize;
            _InitClipStack();
            ImageCache = imageCache;
            FillWithTextureData();
            _needsRecenter = true;
        }



        /// <summary>
        /// Updates any neccesary clip levels to be centered on the specified position. If you're using split-screen
        /// or any complex post-processing effects use the Center property rather than directly calling this method.
        /// That will allow the clip map to recenter during UpdateAnimation rather than immediately update. Otherwise
        /// you risk corrupting your render targets on platforms such as XBox that don't support multiple render
        /// targets.
        /// </summary>
        /// <param name="position">The new clip center of the clip map.</param>
        public void Recenter(Vector2 position)
        {
#if DEBUG
            Profiler.Instance.StartBlock("ClipMap.Recenter");
#endif

            _center = position;
            _needsRecenter = false;

            // update our budget texel upload budget
            _maxTexelUploadPerRecenter = _clipMapSize * _clipMapSize;

            Assert.Fatal((int)TorqueUtil.GetLog2(_clipMapSize) % 1f == 0f, "ClipMap.Recenter - Requires a power of 2 clipmap size.");

            // do toroidal updates on each entry of the clipstack
            // calculate the new texel at most detailed level.
            Vector2 texelCenterF = position * (float)_clipMapSize * _clipLevels[0].ScaleFactor;
            Point texelCenter = new Point((int)Math.Floor(texelCenterF.X), (int)Math.Floor(texelCenterF.Y));

            // Note how many we were at so we can cut off at the right time.
            int lastTexelsUpdated = _texelsUpdated;

            // create a list to store desired data in
            List<RectangleI> desiredData;

            // For each texture...
            for (int i = _clipStackDepth - 2; i >= 0; i--)
            {
                ClipStackEntry stackEntry = _clipLevels[i];

                // calculate new center point for this texture
                texelCenterF = position * (float)_clipMapSize * stackEntry.ScaleFactor;

                int texelMin = _clipMapSize / 2;
                int texelMax = (int)((float)(_clipMapSize) * stackEntry.ScaleFactor) - texelMin;

                // get the top left corner
                Point texelTopLeft = new Point((int)MathHelper.Clamp((int)(Math.Floor(texelCenterF.X)), texelMin, texelMax) - texelMin,
                                                (int)MathHelper.Clamp((int)(Math.Floor(texelCenterF.Y)), texelMin, texelMax) - texelMin);

                // prevent very small updates - the RT changes are costly
                Point delta = new Point(stackEntry.ToroidalOffset.X - texelTopLeft.X, stackEntry.ToroidalOffset.Y - texelTopLeft.Y);
                if (Math.Abs(delta.X) <= _minUpdateDelta && Math.Abs(delta.Y) <= _minUpdateDelta)
                    continue;

                // this + current toroid offset tells us what regions have to be blasted
                RectangleI oldData = new RectangleI(stackEntry.ToroidalOffset, new Point(_clipMapSize, _clipMapSize));
                RectangleI newData = new RectangleI(texelTopLeft, new Point(_clipMapSize, _clipMapSize));

                // get the bounds of the new texture data we want to copy
                desiredData = _CalculateModuloDeltaBounds(oldData, newData);
                //Console.WriteLine("Level {0}: {1} rects", i, desiredData.Count);
                Assert.Fatal(desiredData.Count < 8, "ClipMap.Recenter - Got too many rects back from CalculateModuloDeltaBounds.");

                // update the clip stack entry's clip center and toroidal offset
                stackEntry.ClipCenter = position;
                stackEntry.ToroidalOffset = texelTopLeft;

                // update any regions we found
                if (desiredData.Count > 0)
                {
                    //jk 9-14 temp, causes crash when enter debug mode //jk
                    for (int t = 0, c = GFXDevice.Instance.Device.GraphicsDeviceCapabilities.MaxSimultaneousTextures; t < c; t++)
                        GFXDevice.Instance.Device.Textures[t] = null;
                    //jk

                    _imageCache.BeginRectUpdates(i, stackEntry);

                    // update regions
                    for (int j = 0; j < desiredData.Count; j++)
                    {
                        Assert.Fatal(desiredData[j].IsValid, "ClipMap.Recenter - got an invalid rect.");

                        // Note the rect, so we can then wrap and let the image cache do its thing.
                        RectangleI srcRegion = desiredData[j];
                        desiredData[j] = new RectangleI(new Point(srcRegion.Point.X % _clipMapSize, srcRegion.Point.Y % _clipMapSize), desiredData[j].Extent);

                        Assert.Fatal(newData.Contains(srcRegion), "ClipMap.Recenter - got update buffer outside of expected new data bounds.");

                        _totalUpdates++;
                        _texelsUpdated += srcRegion.Extent.X * srcRegion.Extent.Y;

                        _imageCache.DoRectUpdate(i, stackEntry, srcRegion, desiredData[j]);
                    }

                    _imageCache.FinishRectUpdates(i, stackEntry);
                }

                // check if we've overrun our budget.
                if ((_texelsUpdated - lastTexelsUpdated) > _maxTexelUploadPerRecenter)
                {
                    _needsRecenter = true;
                    break;
                }
            }

#if DEBUG
            Profiler.Instance.EndBlock("ClipMap.Recenter");
#endif
        }



        /// <summary>
        /// Updates any neccesary clip levels to be centered on the specified position.
        /// </summary>
        /// <param name="position">The new clip center of the clip map.</param>
        /// <param name="forceFullUpdate">Specifies whether or not to force a recenter of all levels during the update.</param>
        public void Recenter(Vector2 position, bool forceFullUpdate)
        {
            int maxTexUpload = _maxTexelUploadPerRecenter;
            int minUpdateDelta = _minUpdateDelta;

            if (forceFullUpdate)
            {
                _maxTexelUploadPerRecenter = _clipMapSize * _clipMapSize * _clipStackDepth;
                _minUpdateDelta = 0;
            }

            Recenter(position);

            _maxTexelUploadPerRecenter = maxTexUpload;
            _minUpdateDelta = minUpdateDelta;
        }



        /// <summary>
        /// Fills each clip stack entry with texture data. This should be called once the clip stack and image cache have been initialized.
        /// </summary>
        /// <returns>True if the operation completed successfully.</returns>
        public bool FillWithTextureData()
        {
            // get the interest center
            Vector2 texelCenterF;

            List<RectangleI> desiredData = new List<RectangleI>();

            // first generate our desired rects for each level
            for (int i = 0; i < _clipStackDepth; i++)
            {
                // get this stack entry
                ClipStackEntry stackEntry = _clipLevels[i];

                // calculate new center point for this texture.
                texelCenterF = stackEntry.ClipCenter * (float)_clipMapSize * stackEntry.ScaleFactor;

                int texelMin = _clipMapSize / 2;
                int texelMax = (int)((float)_clipMapSize * stackEntry.ScaleFactor) - texelMin;

                Point texelTopLeft = new Point((int)MathHelper.Clamp((int)(Math.Floor(texelCenterF.Y)), texelMin, texelMax) - texelMin,
                                                (int)MathHelper.Clamp((int)(Math.Floor(texelCenterF.X)), texelMin, texelMax) - texelMin);

                desiredData.Add(new RectangleI(texelTopLeft, new Point(_clipMapSize, _clipMapSize)));
            }

            // upload all the textures...
            for (int i = 0; i < _clipStackDepth; i++)
            {
                ClipStackEntry stackEntry = _clipLevels[i];

                _updateRectList.Clear();
                _ClipAgainstGrid(_clipMapSize, desiredData[i], ref _updateRectList);

                Assert.Fatal(_updateRectList.Count < 8, "ClipMap.FillWithTextureData - Got too many rects back!");

                if (_updateRectList.Count > 0)
                {
                    _imageCache.BeginRectUpdates(i, stackEntry);

                    for (int j = 0; j < _updateRectList.Count; j++)
                    {
                        RectangleI srcRegion = _updateRectList[j];
                        _updateRectList[j] = new RectangleI(new Point(srcRegion.Point.X % _clipMapSize, srcRegion.Point.Y % _clipMapSize), _updateRectList[j].Extent);

                        _imageCache.DoRectUpdate(i, stackEntry, srcRegion, _updateRectList[j]);
                    }

                    _imageCache.FinishRectUpdates(i, stackEntry);
                }

                stackEntry.ToroidalOffset = desiredData[i].Point;
            }

            // success!
            return true;
        }



        /// <summary>
        /// Selects the clip map levels to render given the specified near and far distance.
        /// </summary>
        /// <param name="near">The nearest on the desired mesh.</param>
        /// <param name="far">The farthest point on the desired mesh.</param>
        /// <param name="texBounds">The bounds of the texture coordinates used by the desired mesh.</param>
        /// <param name="startLevel">The lowest clip level needed by the desired mesh.</param>
        /// <param name="endLevel">The highest clip level needed by the mesh.</param>
        public void CalculateClipMapLevels(float near, float far, RectangleF texBounds, out int startLevel, out int endLevel)
        {
            // We also have to deal w/ the available data. So let's figure out if our
            // desired TCs are in the loaded textureset.

            // Adjust the specified TC range into a texel range and inset the rect by 1.
            // (this is pretty high-traffic code, so try to minimise function call overhead
            // by using vectors rather than rectangles and doing rect math inline)
            Vector2 texCoordPoint = new Vector2(((float)texBounds.Point.X * (float)_textureSize) + 1,
                                                ((float)texBounds.Point.Y * (float)_textureSize) + 1);

            Vector2 texCoordExtent = new Vector2(((float)texBounds.Extent.X * (float)_textureSize) - 2,
                                                ((float)texBounds.Extent.Y * (float)_textureSize) - 2);

            // Put some safe defaults in for starters.
            endLevel = _clipStackDepth - 1;
            startLevel = Math.Max(endLevel - 3, 0);

            // Now iterate over every clipstack entry and find the smallest that contains
            // the relevant TCs.
            int minLevelOverlap = _clipStackDepth + 1;
            int maxLevelOverlap = -1;

            // create tmp vars for storing shit locally
            // (minimize function call overhead... yay, C#! hur hur hur)
            Point toroidalOffset;
            Vector2 clipLevelPoint, clipLevelExtent;

            // iterate over the clip stack
            for (int i = _clipStackDepth - 1; i >= 0; i--)
            {
                // get the torroidal offset at this level
                toroidalOffset = _clipLevels[i].ToroidalOffset;

                clipLevelPoint = new Vector2(
                   (float)(1 << i) * toroidalOffset.X,
                   (float)(1 << i) * toroidalOffset.Y);

                clipLevelExtent = new Vector2(
                    (float)((1 << i) * _clipMapSize),
                    (float)((1 << i) * _clipMapSize));

                // Is our tex region fully contained?
                if ((clipLevelPoint.X <= texCoordPoint.X && clipLevelPoint.Y <= texCoordPoint.Y)
                    && (texCoordPoint.X + texCoordExtent.X <= clipLevelPoint.X + clipLevelExtent.X)
                    && (texCoordPoint.Y + texCoordExtent.Y <= clipLevelPoint.Y + clipLevelExtent.Y))
                {
                    // If we're fully contained, then this is our new max.
                    maxLevelOverlap = i;
                    minLevelOverlap = i;
                    continue;
                }

                // Or else maybe we've got overlap?
                if (!(clipLevelPoint.X + clipLevelExtent.X < texCoordPoint.X)
                    || (clipLevelPoint.Y + clipLevelExtent.Y < texCoordPoint.Y)
                    || (texCoordPoint.X + texCoordExtent.X < clipLevelPoint.X)
                    || (texCoordPoint.Y + texCoordExtent.Y < clipLevelPoint.Y))
                {
                    // Make sure that texels at this level are going to be visible (at
                    // closest point).
                    if (near > 100f)
                    {
                        float texelSize = (far - near) / ((texCoordExtent.X + texCoordExtent.Y) / 2f);
                        float size = GFXDevice.Instance.ProjectRadius(near, texelSize);

                        // If it's less than 1px tall, we can skip this level...
                        if (size < 0.9f)
                            continue;
                    }

                    // If we're overlapping then this is our new min...
                    minLevelOverlap = Math.Min(minLevelOverlap, i);
                    continue;
                }
            }

            // Given our level range, do a best fit. We ALWAYS have to have
            // enough for the minimum detail, so we fit that constraint then
            // do our best to give additional detail on top of that.
            endLevel = (int)MathHelper.Clamp(maxLevelOverlap, 0, _clipStackDepth - 1);
            startLevel = (int)MathHelper.Clamp(minLevelOverlap, 0, endLevel);
        }



        public void UpdateAnimation(float dt)
        {
            // recenter operations are done in UpdateAnimation if the Center property is used.
            // this is done to avoid render target related issues when using split-screen
            // on XBox and other platforms that don't support multiple render targets.
            if (_needsRecenter)
                Recenter(_center);

            /* //jk
            int counter = 0;
            foreach (ClipStackEntry entry in ClipLevels)
            {

                entry.Texture.Save("TEST_" + counter + ".JPG", ImageFileFormat.Jpg);
                counter++;
            }
            */

        }

        #endregion


        #region Private, protected, internal methods

        /// <summary>
        /// Initializes the clip stack based on the clip map size and texture size that are currently set.
        /// </summary>
        protected void _InitClipStack()
        {
            // clear all clip levels
            _clipLevels.Clear();

            // initialize stack depth
            _clipStackDepth = ((int)TorqueUtil.GetLog2((double)_textureSize) - (int)TorqueUtil.GetLog2(_clipMapSize) + 1);

            // first do our base textures - they are not mipped
            for (int i = 0; i < _clipStackDepth; i++)
            {
                Texture2D texture = new Texture2D(GFXDevice.Instance.Device, _clipMapSize, _clipMapSize, 1, TextureUsage.None, SurfaceFormat.Color);
                _clipLevels.Add(new ClipStackEntry(texture, 1 << (_clipStackDepth - (1 + i))));
            }
        }



        /// <summary>
        /// Clips a given rectangle against a source texture and returns a list of rects that the rect will wrap to.
        /// </summary>
        /// <param name="gridSpacing">The grid over which to wrap the rectangle. Normally _clipMapSize.</param>
        /// <param name="rect">The rectangle to wrap around the grid.</param>
        /// <returns>The resultant list of rectangles from wrapping the texture to the grid.</returns>
        protected void _ClipAgainstGrid(int gridSpacing, RectangleI rect, ref List<RectangleI> rectList)
        {
            // check against X grids...
            int startX = rect.Point.X;
            int endX = rect.Point.X + rect.Extent.X;

            int gridMask = ~(gridSpacing - 1);
            int startGridX = startX & gridMask;
            int endGridX = endX & gridMask;

            _clipRectBuffer.Clear();

            // check X...
            if (startGridX != endGridX && endX - endGridX > 0)
            {
                // we have a clip: split against the grid multiple and store
                _clipRectBuffer.Add(new RectangleI(startX, rect.Point.Y, endGridX - startX, rect.Extent.Y));
                _clipRectBuffer.Add(new RectangleI(endGridX, rect.Point.Y, endX - endGridX, rect.Extent.Y));
            }
            else
            {
                // copy the original rect
                _clipRectBuffer.Add(rect);
            }

            // now, check Y for the one or two rects we have from above.
            for (int i = 0; i < _clipRectBuffer.Count; i++)
            {
                // Figure our extent and grid information.
                int startY = _clipRectBuffer[i].Point.Y;
                int endY = _clipRectBuffer[i].Point.Y + rect.Extent.Y;
                int startGridY = startY & gridMask;
                int endGridY = endY & gridMask;

                if (startGridY != endGridY && endY - endGridY > 0)
                {
                    // we have a clip: split against the grid multiple and store
                    rectList.Add(new RectangleI(_clipRectBuffer[i].Point.X, endGridY, _clipRectBuffer[i].Extent.X, endY - endGridY));
                    rectList.Add(new RectangleI(_clipRectBuffer[i].Point.X, startY, _clipRectBuffer[i].Extent.X, endGridY - startY));
                }
                else
                {
                    // copy the current rect
                    rectList.Add(_clipRectBuffer[i]);
                }
            }
        }



        /// <summary>
        /// Calculate the list of rects that need to be updated when moving a clip level from one source area to another.
        /// </summary>
        /// <param name="oldData">The area the clip level is moving from.</param>
        /// <param name="newData">The area the clip level is moving to.</param>
        /// <returns>A list of rectangles that need to be updated.</returns>
        protected List<RectangleI> _CalculateModuloDeltaBounds(RectangleI oldData, RectangleI newData)
        {
            // sanity checking
            Assert.Fatal(oldData.Point.X >= 0 && oldData.Point.Y >= 0 && oldData.IsValid, "ClipMap.CalculateModuloDeltaBounds - Negative oldData origin or bad rect.");
            Assert.Fatal(newData.Point.X >= 0 && newData.Point.Y >= 0 && newData.IsValid, "ClipMap.CalculateModuloDeltaBounds - Negative newData origin or bad rect.");
            Assert.Fatal(newData.Extent == oldData.Extent, "ClipMap.CalculateModuloDeltaBounts - Mismatching extents, can only work with matching extents.");

            _updateRectList.Clear();

            // easiest case - if they're the same then do nothing
            if (oldData.Point == newData.Point)
                return _updateRectList;

            // easy case - if there's no overlap then it's all new
            if (!oldData.Overlaps(newData))
            {
                // clip out to return buffer, and we're done
                _ClipAgainstGrid(_clipMapSize, newData, ref _updateRectList);
                return _updateRectList;
            }

            // calculate some useful values for both X and Y. delta is used a lot
            // in determining bounds, and the boundary values are important for
            // determining where to start copying new data in.
            int xDelta = newData.Point.X - oldData.Point.X;
            int yDelta = newData.Point.Y - oldData.Point.Y;

            int xBoundary = (oldData.Point.X + oldData.Extent.X) % _clipMapSize;
            int yBoundary = (oldData.Point.Y + oldData.Extent.Y) % _clipMapSize;

            Assert.Fatal(xBoundary % _clipMapSize == oldData.Point.X % _clipMapSize, "ClipMap.CalculateModuleDeltaBounds - We assume that left and right of the dataset are identical (ie, it's periodical on size of clipmap!) (x)");
            Assert.Fatal(yBoundary % _clipMapSize == oldData.Point.Y % _clipMapSize, "ClipMap.CalculateModuleDeltaBounds - We assume that left and right of the dataset are identical (ie, it's periodical on size of clipmap!) (y)");

            // now, let's build up our rects. we have one rect if we are moving
            // on the X or Y axis, two if both. we dealt with the no-move case
            // previously.
            if (xDelta == 0)
            {
                // moving on Y! so generate and store clipped results.
                RectangleI yRect = new RectangleI();

                if (yDelta < 0)
                {
                    // we need to generate the box from right of old to right of new
                    yRect.Point = newData.Point;
                    yRect.Extent = new Point(_clipMapSize, -yDelta);
                }
                else
                {
                    // we need to generate the box from left of old to left of new
                    yRect.Point = new Point(newData.Point.X, (oldData.Point.Y + oldData.Extent.Y));
                    yRect.Extent = new Point(_clipMapSize, yDelta);
                }

                // clip out to return buffer, and we're done
                _ClipAgainstGrid(_clipMapSize, yRect, ref _updateRectList);

                return _updateRectList;
            }
            else if (yDelta == 0)
            {
                // moving on X! So generate and store clipped results.
                RectangleI xRect = new RectangleI();

                if (xDelta < 0)
                {
                    // We need to generate the box from right of old to right of new.
                    xRect.Point = newData.Point;
                    xRect.Extent = new Point(-xDelta, _clipMapSize);
                }
                else
                {
                    // we need to generate the box from left of old to left of new.
                    xRect.Point = new Point((oldData.Point.X + oldData.Extent.X), newData.Point.Y);
                    xRect.Extent = new Point(xDelta, _clipMapSize);
                }

                // clip out to return buffer, and we're done
                _ClipAgainstGrid(_clipMapSize, xRect, ref _updateRectList);

                return _updateRectList;
            }
            else
            {
                // Both! We have an L shape. So let's do the bulk of it in one rect,
                // and the remainder in the other. We'll choose X as the dominant axis.
                //
                // a-----b---------c   going from e to a.
                // |     |         |
                // |     |         |
                // d-----e---------f   So the dominant rect is abgh and the passive
                // |     |         |   rect is bcef. Obviously depending on delta we
                // |     |         |   have to switch things around a bit.
                // |     |         |          y+ ^
                // |     |         |             |  
                // g-----h---------i   x+->      |

                RectangleI xRect = new RectangleI();
                RectangleI yRect = new RectangleI();

                if (xDelta < 0)
                {
                    // case in the diagram
                    xRect.Point = newData.Point;
                    xRect.Extent = new Point(-xDelta, _clipMapSize);

                    // set up what of yRect we know too
                    yRect.Point = new Point(xRect.Point.X + xRect.Extent.X, 0);
                    yRect.Extent = new Point(_clipMapSize + xDelta, 0);
                }
                else
                {
                    // opposite of case in diagram
                    xRect.Point = new Point(oldData.Point.X + oldData.Extent.X, newData.Point.Y);
                    xRect.Extent = new Point(xDelta, _clipMapSize);

                    // set up what of yRect we know too
                    yRect.Point = new Point((xRect.Point.X + xRect.Extent.X) - _clipMapSize, 0);
                    yRect.Extent = new Point(_clipMapSize - xRect.Extent.X, 0);
                }

                if (yDelta < 0)
                {
                    // case in the diagram.
                    yRect.Point = new Point(yRect.Point.X, newData.Point.Y);
                    yRect.Extent = new Point(yRect.Extent.X, -yDelta);
                }
                else
                {
                    // case in the diagram.
                    yRect.Point = new Point(yRect.Point.X, oldData.Point.Y + oldData.Extent.Y);
                    yRect.Extent = new Point(yRect.Extent.X, yDelta);
                }

                // make sure we don't overlap.
                Assert.Fatal(!yRect.Overlaps(xRect), "ClipMap.CalculateModuloDeltaBounds() - overlap in result rects - should not happen!");

                // now run them through the clipper and we're done
                _ClipAgainstGrid(_clipMapSize, xRect, ref _updateRectList);
                _ClipAgainstGrid(_clipMapSize, yRect, ref _updateRectList);

                return _updateRectList;
            }
        }

        #endregion


        #region Private, protected, and internal fields

        private int _totalUpdates;
        private int _texelsUpdated;
        private int _minUpdateDelta = 2;
        private int _textureSize = 4096;
        private int _clipMapSize = 512;
        private int _clipStackDepth;
        private int _maxTexelUploadPerRecenter;
        private IClipMapImageCache _imageCache;
        private List<ClipStackEntry> _clipLevels = new List<ClipStackEntry>();
        private Vector2 _center = Vector2.Zero;
        private bool _needsRecenter;

        // DO NOT ACCESS THESE LISTS DIRECTLTY!!!
        // these were added to reduce memory churn. the clip map is optimized
        // to only use certain lists when neccesary. _CalculateModuloDeltaBounds
        // will return either a reference to _clipRectList or a reference to 
        // _updateRectList depending on the the subdivisions of the rects for
        // that specific update. as a result, you can never assume the contents 
        // of either list will be consistant.
        static List<RectangleI> _clipRectBuffer = new List<RectangleI>(8);
        static List<RectangleI> _updateRectList = new List<RectangleI>(8);

        #endregion

        #region IDisposable Members

        public override void Dispose()
        {
            _IsDisposed = true;
            _imageCache = null;
            _clipLevels.Clear();
            _clipLevels = null;
            base.Dispose();
        }

        #endregion
    }
}
