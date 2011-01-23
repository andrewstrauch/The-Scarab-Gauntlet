//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using System.Xml;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GarageGames.Torque.Core;
using GarageGames.Torque.Materials;
using GarageGames.Torque.SceneGraph;
using GarageGames.Torque.T2D;
using GarageGames.Torque.RenderManager;
using GarageGames.Torque.GFX;
using GarageGames.Torque.Util;
using GarageGames.Torque.MathUtil;



namespace GarageGames.Torque.T2D
{
    public class T2DTileType : TorqueBase
    {
        #region Public properties, operators, constants, and enums

        /// <summary>
        /// Zero-based index of this type.
        /// </summary>
        public int Index
        {
            get { return _index; }
            set { _index = value; }
        }



        /// <summary>
        /// Reference to this tile type's RenderMaterial. If you are switching to or from a material with a TextureDivider 
        /// (for example, a celled image), you may need to call UpdateVertexBuffer on the T2DTileLayer for the material to 
        /// update its texture coordinates properly.
        /// </summary>
        public RenderMaterial Material
        {
            get { return _material; }
            set { _material = value; }
        }



        /// <summary>
        /// Specifies the region of the material to use, as defined by the material's TextureDivider. You will need to call
        /// UpdateVertexBuffer on the T2DTileLayer for any changes to this field to take effect in runtime.
        /// </summary>
        public int MaterialRegionIndex
        {
            get { return _materialRegionIndex; }
            set { _materialRegionIndex = value; }
        }



        /// <summary>
        /// Array of collision vertices in local space.
        /// </summary>
        public Vector2[] CollisionPolyBasis
        {
            get { return _collisionPolyBasis; }
            set { _collisionPolyBasis = value; }
        }



        /// <summary>
        /// Specifies whether collisions are enabled on this tile type.
        /// </summary>
        public bool CollisionsEnabled
        {
            get { return _collisionsEnabled; }
            set { _collisionsEnabled = value; }
        }



        /// <summary>
        /// This tile type's object type. NOTE: If ObjectType is modified directly on a T2DTileType (post OnRegister), be sure to 
        /// also update the T2DTileLayer ObjectType (T2DTileLayer's ObjectType should always be
        /// a union of all its T2DTileTypes' ObjectTypes.
        /// </summary>
        public TorqueObjectType ObjectType
        {
            get { return _objectType; }
            set { _objectType = value; }
        }

        #endregion


        #region Private, protected, internal fields

        private int _index;
        private Vector2[] _collisionPolyBasis;
        public RenderMaterial _material;
        private int _materialRegionIndex;
        private TorqueObjectType _objectType = TorqueObjectType.AllObjects;
        private bool _collisionsEnabled;
        internal Vector2[] _polyVertices;

        // work variables used during rendering
        internal List<int> _renderTiles = new List<int>();
        internal Resource<DynamicVertexBuffer> _collisionPolyVB;
        #endregion
    }



    public class T2DTileObject : T2DSceneObject, ICloneable, IDisposable
    {
        #region Public properties, operators, constants, and enums

        /// <summary>
        /// Position of the tile on the grid map of the tile layer.
        /// </summary>
        public Vector2 GridPosition
        {
            get { return _gridPosition; }
            set { _gridPosition = value; }
        }



        /// <summary>
        /// Name of tile type.  Can be used to identify tile in xml representation of tilemap.
        /// </summary>
        public string TileTypeName
        {
            get { return _tileTypeName; }
            set { _tileTypeName = value; }
        }



        /// <summary>
        /// Index of tile type.  Can be used to identify tile in xml representation of tilemap.
        /// </summary>
        public int TileTypeIndex
        {
            get { return _tileTypeIndex >> 2; }
            set
            {
                _tileTypeIndex &= _FlipXBit | _FlipYBit;
                _tileTypeIndex |= value << 2;
            }
        }



        /// <summary>
        /// Flip this tile along X axis?
        /// </summary>
        public override bool FlipX
        {
            get { return (_tileTypeIndex & _FlipXBit) != 0; }
            set
            {
                if (value)
                    _tileTypeIndex |= _FlipXBit;
                else
                    _tileTypeIndex &= ~_FlipXBit;
            }
        }



        /// <summary>
        /// Flip this tile along Y axis?
        /// </summary>
        public override bool FlipY
        {
            get { return (_tileTypeIndex & _FlipYBit) != 0; }
            set
            {
                if (value)
                    _tileTypeIndex |= _FlipYBit;
                else
                    _tileTypeIndex &= ~_FlipYBit;
            }
        }



        /// <summary>
        /// T2DTileType of this tile.
        /// </summary>
        [XmlIgnore]
        public T2DTileType TileType
        {
            get { return _tileType; }
            set { _tileType = value; }
        }

        #endregion


        #region Public methods

        /// <summary>
        /// Find world position of tile assuming it belongs to passed tile layer.
        /// </summary>
        /// <param name="tileLayer">Tile layer which owns this tile.</param>
        /// <returns>World position of tile.</returns>
        public Vector2 GetWorldPosition(T2DTileLayer tileLayer)
        {
            return GetWorldPosition(tileLayer.Position, tileLayer.Rotation, tileLayer.MapSize, tileLayer.TileSize);
        }



        /// <summary>
        /// Find world position of tile assuming it belongs to tile layer with the specified parameters.
        /// </summary>
        /// <param name="mapPos">Position of the tile layer in world coordinates.</param>
        /// <param name="mapRotation">Rotation of the tile layer in degrees.</param>
        /// <param name="mapSize">Dimensions of the tile layer.</param>
        /// <param name="tileSize">Size of each tile in the tile layer.</param>
        /// <returns>World position of tile.</returns>
        public Vector2 GetWorldPosition(Vector2 mapPos, float mapRotation, Vector2 mapSize, Vector2 tileSize)
        {
            Vector2 local = GetTileLocalPosition(mapSize, tileSize);
            if (mapRotation != 0.0f)
            {
                Rotation2D rotate = new Rotation2D(MathHelper.ToRadians(mapRotation));
                return rotate.Rotate(local) + mapPos;
            }
            else
                return local + mapPos;
        }



        /// <summary>
        /// Find position of tile in local tile layer space (center of tile map is origin).
        /// </summary>
        /// <param name="mapSize">Dimensions of the tile layer.</param>
        /// <param name="tileSize">Size of each tile in the tile layer.</param>
        /// <returns>Position of tile.</returns>
        public Vector2 GetTileLocalPosition(Vector2 mapSize, Vector2 tileSize)
        {
            return new Vector2(
                (_gridPosition.X * tileSize.X) + (tileSize.X / 2) - ((mapSize.X * tileSize.X) / 2),
                (_gridPosition.Y * tileSize.Y) + (tileSize.Y / 2) - ((mapSize.Y * tileSize.Y) / 2));
        }



        /// <summary>
        /// Find position of tile in local tile layer space (center of tile map is origin).
        /// </summary>
        /// <param name="tileLayer">Tile layer which owns this tile.</param>
        /// <returns>Position of tile.</returns>
        public Vector2 GetTileLocalPosition(T2DTileLayer tileLayer)
        {
            return GetTileLocalPosition(tileLayer.MapSize, tileLayer.TileSize);
        }



        /// <summary>
        /// Clones this object.
        /// </summary>
        /// <returns>Newly cloned object.</returns>
        public override object Clone()
        {
            return MemberwiseClone();
        }

        #endregion


        #region Private, protected, internal fields

        public Vector2 _gridPosition;
        T2DTileType _tileType;
        string _tileTypeName;
        private int _tileTypeIndex;

        // hide flipx and flipy on tileTypeIndex using these masks
        const int _FlipXBit = 1;
        const int _FlipYBit = 2;

        #endregion

        #region IDisposable Members

        public override void Dispose()
        {
            _IsDisposed = true;
            base.Dispose();
        }

        #endregion
    }



    public class T2DTileLayer : T2DSceneObject, IDisposable
    {
        internal class TileLayerCollisionImage : T2DCollisionImage
        {

            #region Constructors

            public TileLayerCollisionImage()
            {
                _polyImage.CollisionPolyBasis = new Vector2[4];
                _polyImage.UseCollisionBasisRaw = true;

            }

            #endregion


            #region Public properties, operators, constants, and enums

            public override int Priority
            {
                // we always want to have priority
                get { return 100; }
            }

            #endregion


            #region Public methods

            public override void MarkCollisionDirty()
            {
                // do nothing
            }



            public override void TestMove(ref float dt, Vector2 ourVelocity, T2DCollisionImage theirImage, List<T2DCollisionInfo> list)
            {
                // entire tile layer is moving, find all potential tile collisions
                _TestMove(ref dt, ourVelocity, theirImage, list, false);
            }



            public override void TestMoveAgainst(ref float dt, Vector2 theirVelocity, T2DCollisionImage theirImage, List<T2DCollisionInfo> list)
            {
                // find all potential tile collisions
                _TestMove(ref dt, theirVelocity, theirImage, list, true);
            }



            public override object Clone()
            {
                return new TileLayerCollisionImage();
            }

            #endregion


            #region Private, protected, internal methods

            protected void _TestMove(ref float dt, Vector2 velocity, T2DCollisionImage theirImage, List<T2DCollisionInfo> list, bool theyMove)
            {
                Assert.Fatal(SceneObject is T2DTileLayer, "Not a tile layer");
                T2DTileLayer tileLayer = (T2DTileLayer)SceneObject;
                _polyImage._sceneObject = tileLayer;

                T2DSceneObject theirObj = theirImage.SceneObject;
                Vector2 theirPos = theirObj.Position;
                Vector2 theirSize = theirObj.Size;

                Vector2 searchMin = theirPos - 0.5f * theirSize;
                Vector2 searchMax = searchMin + theirSize;
                Vector2 searchDelta = theyMove ? velocity * dt : -velocity * dt;

                // adjust search area by velocity
                if (searchDelta.X > 0.0f)
                    searchMax.X += searchDelta.X;
                else
                    searchMin.X += searchDelta.X;
                if (searchDelta.Y > 0.0f)
                    searchMax.Y += searchDelta.Y;
                else
                    searchMin.Y += searchDelta.Y;

                ReadOnlyArray<T2DTileObject> matchedTiles = tileLayer.GetCollisionMatches(searchMin, searchMax);

                Vector2[] savePoly = _polyImage.CollisionPolyBasis;

                for (int i = 0; i < matchedTiles.Count; i++)
                {
                    T2DTileObject tile = matchedTiles[i];

                    if (!tile.TileType.CollisionsEnabled ||
                        !(tile.TileType.ObjectType & theirImage.SceneObject.Collision.CollidesWith))
                        continue;

                    // Build collision poly for this tile
                    Vector2 scenePos = tileLayer.Position;
                    Vector2 tileSize = tileLayer.TileSize;
                    Vector2 tilePos = tile.GetTileLocalPosition(tileLayer.MapSize, tileSize);
                    Vector2 flip = new Vector2(tile.FlipX ? -1.0f : 1.0f, tile.FlipY ? -1.0f : 1.0f);
                    tileSize *= flip;
                    if (tile.TileType.CollisionPolyBasis == null)
                    {
                        // no custom poly so just use the tile's bounds
                        _polyImage.CollisionPolyBasis = savePoly; // restore to original poly image poly
                        _polyImage.CollisionPolyBasis[0] = tilePos - 0.5f * tileSize;
                        _polyImage.CollisionPolyBasis[1] = _polyImage.CollisionPolyBasis[2] = _polyImage.CollisionPolyBasis[3] = _polyImage.CollisionPolyBasis[0];
                        _polyImage.CollisionPolyBasis[1].X += tileSize.X;
                        _polyImage.CollisionPolyBasis[2] += tileSize;
                        _polyImage.CollisionPolyBasis[3].Y += tileSize.Y;
                    }
                    else
                    {
                        // see if we need to create a new PolyVertices array for this TileType
                        if (tile.TileType._polyVertices == null || tile.TileType._polyVertices.Length != tile.TileType.CollisionPolyBasis.Length)
                        {
                            tile.TileType._polyVertices = new Vector2[tile.TileType.CollisionPolyBasis.Length];
                        }
                        for (int j = 0; j < tile.TileType.CollisionPolyBasis.Length; j++)
                            tile.TileType._polyVertices[j] = 0.5f * tile.TileType.CollisionPolyBasis[j] * tileSize + tilePos;

                        // assign our TileType's PolyVertices to the TileLayer's _polyImage array
                        _polyImage.CollisionPolyBasis = tile.TileType._polyVertices;
                    }

                    if (_polyImage.Priority == theirImage.Priority)
                    {
                        // same priority, prefer "TestMove" version -- this ensures
                        // we only use TestMove version of poly collision image,
                        // saving us from implementing move against version.
                        if (theyMove)
                            theirImage.TestMove(ref dt, velocity, _polyImage, list);
                        else
                            _polyImage.TestMoveAgainst(ref dt, velocity, theirImage, list);
                    }
                    else if (_polyImage.Priority < theirImage.Priority)
                    {
                        // gotta use our image
                        if (theyMove)
                            _polyImage.TestMoveAgainst(ref dt, velocity, theirImage, list);
                        else
                            _polyImage.TestMove(ref dt, velocity, theirImage, list);
                    }
                    else
                    {
                        // gotta use their image
                        if (theyMove)
                            theirImage.TestMove(ref dt, velocity, _polyImage, list);
                        else
                            theirImage.TestMoveAgainst(ref dt, velocity, _polyImage, list);
                    }
                }
                _polyImage.CollisionPolyBasis = savePoly;
            }

            #endregion


            #region Private, protected, internal fields

            T2DPolyImage _polyImage = new T2DPolyImage();

            #endregion
        }


        #region Constructors

        public T2DTileLayer()
        {
            Components.AddComponent(new T2DCollisionComponent());
            Collision.InstallImage(new TileLayerCollisionImage());
        }

        #endregion


        #region Public properties, operators, constants, and enums

        /// <summary>
        /// Size of the TileLayer in tile units.
        /// </summary>
        public Vector2 MapSize
        {
            get { return _mapSize; }
            set { _mapSize = value; }
        }



        /// <summary>
        /// Size of individual tiles in world units.
        /// </summary>
        public Vector2 TileSize
        {
            get { return _tileSize; }
            set { _tileSize = value; }
        }



        /// <summary>
        /// Reference to T2DTileType to serve as the default tile type if no type is specified for a given position within the TileLayer.
        /// </summary>
        public T2DTileType DefaultTileType
        {
            get { return _defaultTileType; }
            set { _defaultTileType = value; }
        }



        /// <summary>
        /// String used to hold deserialized tile definition array. Post-deserialization, this string is not used.
        /// </summary>
        public string TileDefinitions
        {
            get { return _tileDefinitions; }
            set { _tileDefinitions = value; }
        }



        /// <summary>
        /// Specifies whether or not to render debug bounds for each tile type within the layer.
        /// </summary>
        public bool RenderCollisionBounds
        {
            get { return _renderBounds; }
            set { _renderBounds = value; }
        }



        /// <summary>
        /// List containing the layer's tile types.
        /// </summary>
        public List<T2DTileType> TileTypes
        {
            get { return _tileTypes; }
            set { _tileTypes = value; }
        }

        #endregion


        #region Public methods

        public override bool OnRegister()
        {
            if (!base.OnRegister())
                return false;

            OnLoaded();

            return true;
        }



        public override void OnLoaded()
        {
            base.OnLoaded();

            if (_tilesArray != null)
                return;

            // pre-allocate our array of T2DTileObject references
            _tilesArray = new object[(int)_mapSize.X * (int)_mapSize.Y];

            // filling our lookup array with the tiles we've gotten so far. the DefaultTile stuff below 
            //  will depend on finding non-defined tiles, thus the extra loop here.
            foreach (T2DTileObject tile in _xmlTiles)
            {
                int index = (int)tile.GridPosition.X + (int)(tile.GridPosition.Y * _mapSize.X);
                _tilesArray[index] = tile;
            }

            // xml loaded, no more need for this
            _xmlTiles = null;

            if (_tileDefinitions.Length > 0)
            {
                string[] arrMatches = _tileDefinitions.Split(' ');
                for (int i = 0; i < arrMatches.Length; i++)
                {
                    string[] arrTileInfo = arrMatches[i].Split(',');
                    Assert.Fatal(arrTileInfo.Length == 3 || arrTileInfo.Length == 4, "Improperly defined tile definition array.");

                    T2DTileObject newTile = new T2DTileObject();

                    newTile.GridPosition = new Vector2(Convert.ToInt16(arrTileInfo[0]), Convert.ToInt16(arrTileInfo[1]));
                    newTile.TileTypeIndex = Convert.ToInt16(arrTileInfo[2]);

                    if (arrTileInfo.Length == 4)
                    {
                        if (String.Compare(arrTileInfo[3], "x", true) == 0)
                            newTile.FlipX = true;
                        else if (String.Compare(arrTileInfo[3], "y", true) == 0)
                            newTile.FlipY = true;
                        else if (String.Compare(arrTileInfo[3], "xy", true) == 0)
                        {
                            newTile.FlipX = true;
                            newTile.FlipY = true;
                        }
                        else
                            Assert.Fatal(false, "Illegal flip value");
                    }

                    int index = (int)newTile.GridPosition.X + (int)(newTile.GridPosition.Y * _mapSize.X);
                    _tilesArray[index] = newTile;
                }
            }

            // read these in, no more need of them
            _tileDefinitions = null;

            if (_defaultTileType != null)
            {
                for (int j = 0; j < _mapSize.Y; j++)
                {
                    for (int i = 0; i < _mapSize.X; i++)
                    {
                        if (GetTileByGridCoords(i, j) == null)
                        {
                            T2DTileObject newTile = new T2DTileObject();

                            newTile.GridPosition = new Vector2(i, j);
                            newTile.TileTypeName = _defaultTileType.Name;

                            int index = (int)newTile.GridPosition.X + (int)(newTile.GridPosition.Y * _mapSize.X);
                            _tilesArray[index] = newTile;
                        }
                    }
                }
            }

            foreach (T2DTileObject tile in _tilesArray)
            {
                if (tile == null)
                    continue;
                tile.TileType = tile.TileTypeName == null ? _FindTileType(tile.TileTypeIndex) : _FindTileType(tile.TileTypeName);
                Assert.Fatal(tile.TileType != null, "Could not match up Tile with TileType");
            }

            foreach (T2DTileType type in _tileTypes)
            {
                ObjectType += type.ObjectType;
            }

            // automatically calculate Size based on dimensions and individual tile size
            Size = new Vector2(_mapSize.X * _tileSize.X, _mapSize.Y * _tileSize.Y);
            UpdateSpatialData();
        }



        public override void CopyTo(TorqueObject obj)
        {
            base.CopyTo(obj);

            T2DTileLayer obj2 = (T2DTileLayer)obj;
            obj2.MapSize = MapSize;
            obj2.TileSize = TileSize;
            obj2.DefaultTileType = DefaultTileType; // shallow copy tile types
            obj2.TileDefinitions = TileDefinitions;
            obj2._xmlTiles = _xmlTiles;
            obj2.RenderCollisionBounds = RenderCollisionBounds;
            obj2.TileTypes = TileTypes;

            if (_tilesArray != null)
            {
                obj2._tilesArray = new object[_tilesArray.Length];
                for (int i = 0; i < _tilesArray.Length; i++)
                {
                    T2DTileObject tile = _tilesArray[i] as T2DTileObject;
                    obj2._tilesArray[i] = tile != null ? tile.Clone() : null;
                }
            }
        }



        public override void Render(SceneRenderState srs)
        {
            Assert.Fatal(_renderedTileTypes.Count == 0, "previous render tiles not cleared");

#if DEBUG
            Profiler.Instance.StartBlock("T2DTileLayer.Render");
#endif

            if (_vb.IsNull)
                _CreateAndFillVB();

            T2DSceneCamera cam = _sceneGraph.Camera as T2DSceneCamera;
            Vector2 camExtent = (cam.SceneMax - cam.SceneMin); // we want world space extents
            Vector2 camCenter = cam.CenterPosition;
            Rectangle r = _GetNearbyTiles(camCenter.X, camCenter.Y, camExtent.X, camExtent.Y, 0.1f);

            // Get rotation and tile radius for q&d check to see if tile
            // is on screen (for rotated case).
            float tileRadius = 0.51f * _tileSize.Length();
            Rotation2D mapRotation = new Rotation2D(MathHelper.ToRadians(Rotation));

            Matrix mapToWorld = Matrix.CreateRotationZ(MathHelper.ToRadians(Rotation)) * Matrix.CreateTranslation(Position.X, Position.Y, 0.0f);

            // track how many tiles we'll actually render
            int tileCount = 0;

            // collect all the tiles we're actually going to render and put them in the right batch
            bool doCheckRotation = _rotation != 0.0f;
            for (int j = r.Top; j <= r.Height; j++)
            {
                for (int i = r.Left; i <= r.Width; i++)
                {
                    T2DTileObject tile = GetTileByGridCoords(i, j);
                    if (tile == null)
                        continue;

                    // check for off-screen tiles (rotated case, but doesn't cull tiles off screen due to camera rotation)
                    if (doCheckRotation)
                    {
                        Vector2 tilePos = tile.GetTileLocalPosition(_mapSize, _tileSize);
                        Vector2 tileWorldPos = mapRotation.Rotate(tilePos) + Position;
                        if (Math.Abs(tileWorldPos.X - camCenter.X) - tileRadius > 0.5f * camExtent.X || Math.Abs(tileWorldPos.Y - camCenter.Y) - tileRadius > 0.5f * camExtent.Y)
                            continue;
                    }

                    // at this point we're committed to rendering this tile
                    // for now simply add to the list of tiles for that 
                    // tile type so we can batch them up later
                    tileCount++;
                    int tileIdx = i + j * (int)_mapSize.X;
                    if (tile.TileType._renderTiles.Count == 0)
                        _renderedTileTypes.Add(tile.TileType);
                    tile.TileType._renderTiles.Add(tileIdx);
                }
            }

            // Assuming we found some tiles, render all the batches.
            // Note: can't set 0 length index buffer without crashing, so
            // need to skip out of that case.
            if (tileCount != 0)
            {
                // We now know how many tiles we're going to render.  Is our index buffer big enough?
                if (_indexBuffer.IsNull)
                {
                    // index buffer cleared out...start over with size
                    _renderIndexCount = 0;
                }

                if (tileCount * 6 > _renderIndexCount)
                {
                    if (!_indexBuffer.IsNull)
                    {
                        _indexBuffer.Instance.Dispose();
                        _indexBuffer.Invalidate();
                    }
                    _renderIndexCount = tileCount * 8; // go a little higher than current max
                    _indexBuffer = ResourceManager.Instance.CreateDynamicIndexBuffer(ResourceProfiles.ManualDynamicIBProfile, _renderIndexCount * sizeof(UInt16), IndexElementSize.SixteenBits);
                }
                UInt16[] indexScratch = TorqueUtil.GetScratchArray<UInt16>(tileCount * 6);

                // render tile types in batches
                int idx = 0;
                srs.World.Push();
                srs.World.MultiplyMatrixLocal(mapToWorld);
                Matrix worldTransform = srs.World.Top;
                VertexDeclaration vd = GFXVertexFormat.GetVertexDeclaration(srs.Gfx.Device);
                for (int i = 0; i < _renderedTileTypes.Count; i++)
                {
                    T2DTileType tileType = _renderedTileTypes[i];

                    RenderInstance ri = SceneRenderer.RenderManager.AllocateInstance();

                    ri.Type = RenderInstance.RenderInstanceType.Mesh2D;

                    ri.ObjectTransform = worldTransform;

                    ri.PrimitiveType = PrimitiveType.TriangleList;
                    ri.VertexDeclaration = vd;

                    ri.VertexBuffer = _vb.Instance;
                    ri.VertexSize = GFXVertexFormat.VertexSize;
                    ri.BaseVertex = 0;
                    ri.VertexCount = 4 * (int)_mapSize.X * (int)_mapSize.Y;
                    ri.IndexBuffer = _indexBuffer.Instance;
                    ri.StartIndex = idx;
                    ri.PrimitiveCount = tileType._renderTiles.Count * 2;

                    ri.Opacity = VisibilityLevel;
                    ri.UTextureAddressMode = TextureAddressMode.Clamp;
                    ri.VTextureAddressMode = TextureAddressMode.Clamp;

                    ri.Material = tileType.Material;

                    // fill up index buffer
                    for (int j = 0; j < tileType._renderTiles.Count; j++)
                    {
                        int tileIdx = tileType._renderTiles[j];
                        Assert.Fatal(tileIdx * 4 + 3 < UInt16.MaxValue,
                           "T2DTileLayer.Render - Got out of range tile index!");
                        indexScratch[idx++] = (UInt16)(4 * tileIdx + 0);
                        indexScratch[idx++] = (UInt16)(4 * tileIdx + 1);
                        indexScratch[idx++] = (UInt16)(4 * tileIdx + 2);
                        indexScratch[idx++] = (UInt16)(4 * tileIdx + 0);
                        indexScratch[idx++] = (UInt16)(4 * tileIdx + 2);
                        indexScratch[idx++] = (UInt16)(4 * tileIdx + 3);
                    }
                    tileType._renderTiles.Clear();

                    SceneRenderer.RenderManager.AddInstance(ri);
                }
                srs.World.Pop();

                // rendered all the batches
                _renderedTileTypes.Clear();

                // set index data
                GFXDevice.Instance.Device.Indices = null;
#if XBOX
                _indexBuffer.Instance.SetData<UInt16>(indexScratch, 0, idx); // XBox cannot discard... performance issue?
#else
                _indexBuffer.Instance.SetData<UInt16>(indexScratch, 0, idx, SetDataOptions.Discard);
#endif
            }

            // Renders custom collision polys for tiles based on tile types
            if (RenderCollisionBounds)
            {
                Matrix scaleMat = Matrix.CreateScale(_tileSize.X * 0.5f, _tileSize.Y * 0.5f, 1.0f);
                for (int j = r.Top; j <= r.Height; j++)
                {
                    for (int i = r.Left; i <= r.Width; i++)
                    {
                        T2DTileObject tile = GetTileByGridCoords(i, j);
                        if (tile == null || tile.TileType.CollisionPolyBasis == null)
                            continue;

                        // this is debug code so don't worry about all the math
                        Vector2 tilePos = tile.GetTileLocalPosition(_mapSize, _tileSize);
                        Matrix transMat = Matrix.CreateTranslation(tilePos.X, tilePos.Y, 0.0f);
                        Matrix tileToWorld = scaleMat * transMat * mapToWorld;

                        _RenderTileBounds(srs, ref tileToWorld, tile);
                    }
                }
            }

#if DEBUG
            Profiler.Instance.EndBlock("T2DTileLayer.Render");
#endif
        }



        public ReadOnlyArray<T2DTileObject> GetCollisionMatches(T2DSceneObject obj)
        {
            return GetCollisionMatches(obj.Position, obj.Position + obj.Size);
        }



        public ReadOnlyArray<T2DTileObject> GetCollisionMatches(Vector2 min, Vector2 max, float padding)
        {
            _matchedTiles.Clear();

            Rectangle r = _GetNearbyTiles(0.5f * (min.X + max.X), 0.5f * (min.Y + max.Y), max.X - min.X, max.Y - min.Y, padding);
            for (int j = r.Top; j <= r.Height; j++)
            {
                for (int i = r.Left; i <= r.Width; i++)
                {
                    T2DTileObject tile = GetTileByGridCoords(i, j);
                    if (tile != null)
                        _matchedTiles.Add(tile);
                }
            }
            return new ReadOnlyArray<T2DTileObject>(_matchedTiles);
        }



        /// <summary>
        /// Get a list of all possible collision matches given a min and max position within the tile layer.
        /// </summary>
        /// <param name="min">Minimum position.</param>
        /// <param name="max">Maximum position.</param>
        /// <returns>ReadOnlyArray containing the found matches.</returns>
        public ReadOnlyArray<T2DTileObject> GetCollisionMatches(Vector2 min, Vector2 max)
        {
            return GetCollisionMatches(min, max, 0.1f);
        }



        public override void Dispose()
        {
            _IsDisposed = true;
            if (!_vb.IsNull)
            {
                _vb.Instance.Dispose();
                _vb.Invalidate();
            }

            for (int i = 0; i < _tilesArray.Length; i++)
                _tilesArray[i] = null;

            base.Dispose();
        }



        /// <summary>
        /// Find a tile by world coordinates.
        /// </summary>
        /// <param name="worldPos">Position, in world coordinates.</param>
        /// <returns>The found T2DTileObject, or null if no match was found.</returns>
        public T2DTileObject PickTile(Vector2 worldPos)
        {
            float x, y;

            x = ((worldPos.X - _position.X) / _tileSize.X) + (_mapSize.X * 0.5f);
            y = ((worldPos.Y - _position.Y) / _tileSize.Y) + (_mapSize.Y * 0.5f);

            x = MathHelper.Clamp(x, 0.0f, _mapSize.X - 1);
            y = MathHelper.Clamp(y, 0.0f, _mapSize.Y - 1);

            return GetTileByGridCoords((int)x, (int)y);
        }



        /// <summary>
        /// Find a tile by grid (x,y) coordinates.
        /// </summary>
        /// <param name="x">Position, in grid (x,y) coordinates.</param>
        /// <param name="y">Position, in grid (x,y) coordinates.</param>
        /// <returns>The found T2DTileObject, or null if no match was found.</returns>
        public T2DTileObject GetTileByGridCoords(int x, int y)
        {
            int rowSize = (int)_mapSize.X;

            int index = x + (y * rowSize);
            if (index < _tilesArray.Length)
            {
                return (T2DTileObject)_tilesArray[index];
            }

            return null;
        }



        /// <summary>
        /// Sets the tile at the specified grid coordinates.
        /// </summary>
        /// <param name="x">Position, in grid (x,y) coordinates.</param>
        /// <param name="y">Position, in grid (x,y) coordinates.</param>
        /// <param name="tile">The tile object to insert into the tile layer at the specified grid coordinates.</param>
        /// <returns>True if the tile was successfully replaced.</returns>
        public bool SetTileByGridCoords(int x, int y, T2DTileObject tile)
        {
            // tiles with null tile types or tile types with no material are not allowed
            if (tile != null && (tile.TileType == null || tile.TileType.Material == null))
                return false;

            // get the index of the tile in the array
            int index = x + (y * (int)_mapSize.X);

            // allocate our array of T2DTileObject references if it's not already created
            if (_tilesArray == null)
                _tilesArray = new object[(int)_mapSize.X * (int)_mapSize.Y];

            // early out if the index is out of range
            if (index >= _tilesArray.Length)
                return false;

            // get the existing tile object
            T2DTileObject existing = _tilesArray[index] as T2DTileObject;

            // early out if the tile is already the same
            if (tile == existing)
                return true;

            // if the tile isn't null, do some stuff here
            if (tile != null)
            {
                // force the appropriate grid position
                tile.GridPosition = new Vector2(x, y);

                // check if the new tile's tile type is not in our tile types list already
                if (!_tileTypes.Contains(tile.TileType))
                    _tileTypes.Add(tile.TileType);
            }

            // replace the tile with the one specified
            _tilesArray[index] = tile;

            // dispose of the current vertex buffer and set the vertex buffer null so next render pass 
            // it will be recreated with the new tile included
            _vb.Invalidate();

            // success: return true
            return true;
        }

        #endregion


        #region Private, protected, internal methods

        private T2DTileType _FindTileType(string tileTypeName)
        {
            return TorqueObjectDatabase.Instance.FindObject<T2DTileType>(tileTypeName);
        }



        private T2DTileType _FindTileType(int tileTypeIndex)
        {
            foreach (T2DTileType tileType in _tileTypes)
            {
                if (tileType.Index == tileTypeIndex)
                    return tileType;
            }

            return null;
        }



        private Rectangle _GetNearbyTiles(float x, float y, float width, float height, float pad)
        {
            // use offset from tile map center.
            x -= _position.X;
            y -= _position.Y;

            if (Rotation != 0.0f)
            {
                // need to transform world-space box to tile map aligned box which includes
                // original box.
                Rotation2D rotation = new Rotation2D(MathHelper.ToRadians(Rotation));

                // figure tile space position
                Vector2 newPos = new Vector2(x, y);
                newPos = rotation.Unrotate(newPos);
                x = newPos.X;
                y = newPos.Y;

                // compute tile space width/height, expanded to include all 
                // of original box in tile aligned box
                Vector2 xvec = rotation.X;
                Vector2 yvec = rotation.Y;
                float newWidth = width * Math.Abs(xvec.X) + height * Math.Abs(xvec.Y);
                float newHeight = width * Math.Abs(yvec.X) + height * Math.Abs(yvec.Y);
                width = newWidth;
                height = newHeight;
            }

            float minX, minY, maxX, maxY;

            minX = ((x - (width * 0.5f)) / _tileSize.X) + (_mapSize.X * 0.5f);
            maxX = ((x + (width * 0.5f)) / _tileSize.X) + (_mapSize.X * 0.5f);
            minY = ((y - (height * 0.5f)) / _tileSize.Y) + (_mapSize.Y * 0.5f);
            maxY = ((y + (height * 0.5f)) / _tileSize.Y) + (_mapSize.Y * 0.5f);

            minX -= pad;
            minY -= pad;
            maxX += pad;
            maxY += pad;

            // make sure that returned rectangle only contains valid tile boundaries
            minX = MathHelper.Clamp(minX, 0.0f, _mapSize.X - 1);
            maxX = MathHelper.Clamp(maxX + 1, 0.0f, _mapSize.X - 1);
            minY = MathHelper.Clamp(minY, 0.0f, _mapSize.Y - 1);
            maxY = MathHelper.Clamp(maxY + 1, 0.0f, _mapSize.Y - 1);

            return new Rectangle((int)minX, (int)minY, (int)(maxX), (int)(maxY));
        }



        void _CreateTangentSpace(out Vector4 normal, out Vector4 tangent, out Vector4 binormal)
        {
            Vector3 point0 = new Vector3(-1.0f, -1.0f, 0.0f);
            Vector3 point1 = new Vector3(1.0f, -1.0f, 0.0f);
            Vector3 point2 = new Vector3(1.0f, 1.0f, 0.0f);
            Vector3 point3 = new Vector3(-1.0f, 1.0f, 0.0f);

            Vector3 v3n = Vector3.Cross(point1 - point0, point2 - point0);
            normal = new Vector4(v3n.X, v3n.Y, v3n.Z, 0.0f);
            if (normal.LengthSquared() > 0.0001f)
                normal.Normalize();

            Vector3 v3tangent = point1 - point0;
            tangent = new Vector4(v3tangent.X, v3tangent.Y, v3tangent.Z, 0.0f);
            if (tangent.LengthSquared() > 0.0001f)
                tangent.Normalize();

            Vector3 v3binormal = point3 - point0;
            binormal = new Vector4(v3binormal.X, v3binormal.Y, v3binormal.Z, 0.0f);
            if (binormal.LengthSquared() > 0.0001f)
                binormal.Normalize();
        }



        void _CreateAndFillVB()
        {
            Assert.Fatal(_vb.IsNull, "About to leak a vertex buffer");

            // create the new buffer of the correct size
            int numVerts = 4 * (int)_mapSize.X * (int)_mapSize.Y;
            int sizeInBytes = numVerts * GFXVertexFormat.VertexSize;
            _vb = ResourceManager.Instance.CreateDynamicVertexBuffer(ResourceProfiles.ManualStaticVBProfile, sizeInBytes);

            // create some values which will be constant over entire vertex space
            Color color = Color.White;
            Vector4 normal, tangent, binormal;
            _CreateTangentSpace(out normal, out tangent, out binormal);

            Vector2 t0 = new Vector2(0, 0);
            Vector2 t1 = new Vector2(1, 0);
            Vector2 t2 = new Vector2(1, 1);
            Vector2 t3 = new Vector2(0, 1);

            // fill in vertex array 
            GFXVertexFormat.PCTTBN[] vertices = TorqueUtil.GetScratchArray<GFXVertexFormat.PCTTBN>(numVerts);
            for (int x = 0; x < (int)_mapSize.X; x++)
            {
                for (int y = 0; y < (int)_mapSize.Y; y++)
                {
                    int tileIdx = x + y * (int)_mapSize.X;

                    T2DTileObject tile = GetTileByGridCoords(x, y);
                    if (tile != null)
                    {
                        // get the region coordinates for this tile
                        tile.TileType.Material.GetRegionCoords(tile.TileType.MaterialRegionIndex, out t0, out t1, out t2, out t3);

                        // flip the coordinstes if neccesary
                        if (tile.FlipX != FlipX)
                        {
                            TorqueUtil.Swap<Vector2>(ref t0, ref t1);
                            TorqueUtil.Swap<Vector2>(ref t2, ref t3);
                        }
                        if (tile.FlipY != FlipY)
                        {
                            TorqueUtil.Swap<Vector2>(ref t0, ref t3);
                            TorqueUtil.Swap<Vector2>(ref t1, ref t2);
                        }
                    }

                    // get tile extents
                    float minx = _tileSize.X * (float)x - 0.5f * _mapSize.X * _tileSize.X;
                    float maxx = _tileSize.X * (float)(x + 1) - 0.5f * _mapSize.X * _tileSize.X;
                    float miny = _tileSize.Y * (float)y - 0.5f * _mapSize.Y * _tileSize.Y;
                    float maxy = _tileSize.Y * (float)(y + 1) - 0.5f * _mapSize.Y * _tileSize.Y;

                    vertices[4 * tileIdx + 0] = new GFXVertexFormat.PCTTBN(
                        new Vector3(minx, miny, 0.0f),
                        color,
                        t0,
                        t0,
                        tangent,
                        normal);

                    vertices[4 * tileIdx + 1] = new GFXVertexFormat.PCTTBN(
                        new Vector3(maxx, miny, 0.0f),
                        color,
                        t1,
                        t1,
                        tangent,
                        normal);

                    vertices[4 * tileIdx + 2] = new GFXVertexFormat.PCTTBN(
                        new Vector3(maxx, maxy, 0.0f),
                        color,
                        t2,
                        t2,
                        tangent,
                        normal);

                    vertices[4 * tileIdx + 3] = new GFXVertexFormat.PCTTBN(
                        new Vector3(minx, maxy, 0.0f),
                        color,
                        t3,
                        t3,
                        tangent,
                        normal);
                }
            }

            _vb.Instance.SetData<GFXVertexFormat.PCTTBN>(vertices, 0, numVerts);
        }



        void _RenderTileBounds(SceneRenderState srs, ref Matrix objToWorld, T2DTileObject tile)
        {
            int numVerts = tile.TileType.CollisionPolyBasis.Length + 1;
            if (tile.TileType._collisionPolyVB.IsNull)
            {
                int sizeInBytes = numVerts * GFXVertexFormat.VertexSize;
                tile.TileType._collisionPolyVB = ResourceManager.Instance.CreateDynamicVertexBuffer(ResourceProfiles.ManualStaticVBProfile, sizeInBytes);

                // fill in vertex array 
                GFXVertexFormat.PCTTBN[] pVertices = TorqueUtil.GetScratchArray<GFXVertexFormat.PCTTBN>(numVerts);
                for (int k = 0; k < numVerts - 1; ++k)
                {
                    pVertices[k] = new GFXVertexFormat.PCTTBN();
                    pVertices[k].Position = new Vector3(tile.TileType.CollisionPolyBasis[k].X, tile.TileType.CollisionPolyBasis[k].Y, 0.0f);
                    pVertices[k].Color = Color.Green;
                }
                pVertices[numVerts - 1] = pVertices[0];

                tile.TileType._collisionPolyVB.Instance.SetData<GFXVertexFormat.PCTTBN>(pVertices, 0, numVerts);
            }

            srs.World.Push();
            srs.World.MultiplyMatrixLocal(objToWorld);
            float flipx = tile.FlipX ? -1.0f : 1.0f;
            float flipy = tile.FlipY ? -1.0f : 1.0f;
            srs.World.MultiplyMatrixLocal(Matrix.CreateScale(new Vector3(flipx, flipy, 1.0f)));


            if (_effect == null)
            {
                _effect = new GarageGames.Torque.Materials.SimpleMaterial();
            }

            RenderInstance ri = SceneRenderer.RenderManager.AllocateInstance();
            ri.Type = RenderInstance.RenderInstanceType.Mesh2D;
            ri.ObjectTransform = srs.World.Top;
            ri.VertexBuffer = tile.TileType._collisionPolyVB.Instance;
            ri.PrimitiveType = PrimitiveType.LineStrip;
            ri.VertexSize = GFXVertexFormat.VertexSize;
            ri.VertexDeclaration = GFXVertexFormat.GetVertexDeclaration(srs.Gfx.Device);
            ri.VertexCount = numVerts;
            ri.BaseVertex = 0;
            ri.PrimitiveCount = numVerts - 1;

            ri.UTextureAddressMode = TextureAddressMode.Clamp;
            ri.VTextureAddressMode = TextureAddressMode.Clamp;

            ri.Material = _effect;
            SceneRenderer.RenderManager.AddInstance(ri);

            srs.World.Pop();
        }

        #endregion


        #region Private, protected, internal fields

        private Vector2 _mapSize;
        private Vector2 _tileSize;

        // used for fast index-based tile object lookup
        private object[] _tilesArray;

        private List<T2DTileObject> _matchedTiles = new List<T2DTileObject>();

        private T2DTileType _defaultTileType;
        private string _tileDefinitions;

        Resource<DynamicVertexBuffer> _vb;

        private List<T2DTileType> _tileTypes = new List<T2DTileType>();

        [XmlArrayItem(
            ElementName = "Tile",
            Type = typeof(T2DTileObject))]
        private List<T2DTileObject> _xmlTiles = new List<T2DTileObject>();

        // for bounds rendering
        private bool _renderBounds;

        // work variable for rendering
        private List<T2DTileType> _renderedTileTypes = new List<T2DTileType>();
        private int _renderIndexCount;
        private Resource<DynamicIndexBuffer> _indexBuffer;

        private GarageGames.Torque.Materials.SimpleMaterial _effect;
        #endregion
    }
}
