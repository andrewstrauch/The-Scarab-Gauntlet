//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Xml.Serialization;
using GarageGames.Torque.Core;
using GarageGames.Torque.Util;



namespace GarageGames.Torque.SceneGraph
{
    /// <summary>
    /// Class for spatially querying the scene graph container for scene objects.
    /// </summary>
    public class SceneContainerQueryData
    {

        #region Public properties, operators, constants, and enums

        /// <summary>
        /// Whether invisible objects should be included.  
        /// </summary>
        public bool FindInvisible
        {
            get { return _findInvisible; }
            set { _findInvisible = value; }
        }

        /// <summary>
        /// TorqueObjectTypes to find.
        /// </summary>
        public TorqueObjectType ObjectTypes
        {
            get { return _objectTypes; }
            set { _objectTypes = value; }
        }

        /// <summary>
        /// Exclude the specified object from the search results.
        /// </summary>
        /// 
        public ISceneContainerObject IgnoreObject
        {
            get { return _ignoreObject; }
            set { _ignoreObject = value; }
        }

        /// <summary>
        /// Exclude objects from this list.
        /// </summary>
        public ISceneContainerObject[] IgnoreObjects
        {
            get { return _ignoreObjects; }
            set { _ignoreObjects = value; }
        }

        /// <summary>
        /// If non-null, result objects will be added to this list.  
        /// </summary>
        public List<ISceneContainerObject> ResultList
        {
            get { return _resultList; }
            set { _resultList = value; }
        }

        #endregion


        #region Private, protected, internal fields

        internal bool _findInvisible = false;
        internal TorqueObjectType _objectTypes = TorqueObjectType.AllObjects;
        internal ISceneContainerObject _ignoreObject;
        internal ISceneContainerObject[] _ignoreObjects;
        internal List<ISceneContainerObject> _resultList;

        #endregion
    }



    /// <summary>
    /// Objects that are to be added to the container system must implement this interface.
    /// </summary>
    public interface ISceneContainerObject : ISceneObject
    {

        #region Public properties, operators, constants, and enums

        /// <summary>
        /// Spatial database information for the object.
        /// </summary>
        SceneContainerData SceneContainerData
        {
            get;
            set;
        }

        #endregion
    }



    /// <summary>
    /// This class holds data needed by the container system for each object added to it.
    /// Any class that wants to be added to the container system should have an 
    /// instance field of this type and implement the ISceneContainerObject interface.
    /// This is not a struct because we want it passed by reference through property accessors.
    /// </summary>
    public class SceneContainerData
    {

        #region Public properties, operators, constants, and enums

        /// <summary>
        /// Reference to the first SceneContainerBinReference that contains the object.  Next bin reference is accessed
        /// using the _objectLink field of this field.
        /// </summary>
        public SceneContainerBinReference _binReferenceChain;

        /// <summary>
        /// Minimum bin number covered by the object in X dimension.
        /// </summary>
        internal uint _minBinX;

        /// <summary>
        /// Maximum bin number covered by object in X dimension.
        /// </summary>
        internal uint _maxBinX;

        /// <summary>
        /// Minimum bin number covered by the object in Y dimension.
        /// </summary>
        internal uint _minBinY;

        /// <summary>
        /// Maximum bin number covered by object in Y dimension.
        /// </summary>
        internal uint _maxBinY;

        /// <summary>
        /// Sequency key from the last search that examined this object.  Objects can be contained in multiple bins, so 
        /// setting and checking this value allows us to only examine an object once per search.
        /// </summary>
        internal uint _sequenceKey;

        #endregion
    }



    /// <summary>
    /// This is the internal data structure used by the container storage.  It is not intended for use outside
    /// of the container system; the only reason it is public is that SceneContainerData, which must be public, 
    /// has a reference to it.
    /// </summary>
    public class SceneContainerBinReference : IDisposable
    {

        #region Constructors

        public SceneContainerBinReference()
        {
        }

        #endregion


        #region Public methods

        public void Reset()
        {
            _sceneObject = null;
            _nextBinReference = null;
            _previousBinReference = null;
            _objectLink = null;
        }

        [Conditional("TRACE")]
        public void AssertIfNotNull()
        {
            Assert.Fatal(_sceneObject == null, "doh");
            Assert.Fatal(_nextBinReference == null, "doh");
            Assert.Fatal(_previousBinReference == null, "doh");
            Assert.Fatal(_objectLink == null, "doh");
        }

        #endregion


        #region Private, protected, internal fields

        /// <summary>
        /// The scene object contained in this bin reference.
        /// </summary>
        internal ISceneContainerObject _sceneObject;

        /// <summary>
        /// Pointer to the next bin reference in this chain.
        /// </summary>
        internal SceneContainerBinReference _nextBinReference;

        /// <summary>
        /// Pointer to the previous bin reference in this change.
        /// </summary>
        internal SceneContainerBinReference _previousBinReference;

        /// <summary>
        /// Used to link references that are not necessarily in the same bin.  For instance, the free bin list uses
        /// this to link together all the free references.  Also, when objects are added to bins, this is used to link
        /// all the bins containing the object together (see _binReferenceChain in SceneContainerData).  The distinction
        /// between this and the next/previous fields is that those fields point to other references in the _same_ bin,
        /// whereas this can point to references in any bin.
        /// </summary>
        internal SceneContainerBinReference _objectLink;

        #endregion

        #region IDisposable Members

        public virtual void Dispose()
        {
            Reset();
        }

        #endregion
    }



    /// <summary>
    /// Spatial database for tracking objects in the scene.
    /// </summary>
    abstract public class SceneContainer : TorqueBase, IDisposable
    {

        #region Static methods, fields, constructors

        /// <summary>
        /// Size of scene container bins if the size isn't specified. Default 20.
        /// </summary>
        public static readonly float DefaultBinSize = 20.0f;

        /// <summary>
        /// Number of scene container bins if the count isn't specified. Default 256
        /// </summary>
        public static readonly uint DefaultBinCount = 256;

        /// <summary>
        /// Whether or not to pre allocate bin references. Default false.
        /// </summary>
        public static readonly bool PreAllocSceneBinArrayReferences = false;
        public static readonly bool UseFreeList = true;
        public static readonly uint FreeListBlockSize = 256;

        #endregion


        #region Public properties, operators, constants, and enums

        /// <summary>
        /// Scene container keeps objects in a number of bins for faster lookup.
        /// This property determines how many bins a single object can be in before
        /// it's added to an "overflow" bin.  If this number is too large then
        /// an object might end up in an inordinate number of bins, thus making it
        /// slow to update.  If this number is too small then most objects might end
        /// up in the overflow bin, thus making everything slow to update.
        /// </summary>
        public int MaxBinsPerObject
        {
            get { return _maxBinsPerObject; }
            set { _maxBinsPerObject = value; }
        }

        /// <summary>
        /// Scene graph for which this is a scene container.
        /// </summary>
        [XmlIgnore]
        public BaseSceneGraph SceneGraph
        {
            get { return _sg; }
            set { _sg = value; }
        }

        /// <summary>
        /// Size in world space units of each bin (other than the overflow bin).  Note that
        /// the units for the size are determined by the scene graph and will vary for, say,
        /// 2D and 3D.
        /// </summary>
        public float BinSize
        {
            get { return _binSize; }
            set { _binSize = value; }
        }

        /// <summary>
        /// Number of bins in both the x and y direction.  Thus, the total number of bins used by the
        /// container will be BinCount squared.
        /// </summary>
        public uint BinCount
        {
            get { return _binCount; }
            set { _binCount = value; }
        }

        #endregion


        #region Constructors

        /// <summary>
        /// Create a scene container at the default bin size and count.
        /// </summary>
        public SceneContainer()
        {
            _sg = null;
            _binSize = DefaultBinSize;
            _binCount = DefaultBinCount;

            Init();
        }

        /// <summary>
        /// Create a scene container at the default bin size and count.
        /// </summary>
        /// <param name="sg">The scenegraph to construct the container system for.</param>
        public SceneContainer(BaseSceneGraph sg) : this(sg, DefaultBinSize, DefaultBinCount) { }

        /// <summary>
        /// Create a scene container with specified bin size and count.
        /// </summary>
        /// <param name="sg">The scenegraph to construct the container system for.</param>
        /// <param name="binSize">The size of the bins.</param>
        /// <param name="binCount">The number of bins.</param>
        public SceneContainer(BaseSceneGraph sg, float binSize, uint binCount)
        {
            _sg = sg;
            _binSize = binSize;
            _binCount = binCount;

            Init();
        }

        #endregion


        #region Public methods

        /// <summary>
        /// Checks to make sure the given object is in the right bins and updates the
        /// object if not.
        /// </summary>
        /// <param name="obj">Object to check.</param>
        public void CheckSceneObjectBins(ISceneContainerObject obj)
        {
            // Check everything is fine!
            Assert.Fatal(obj != null, "Invalid Object");

            // Get the object's SceneContainerData
            SceneContainerData scd = obj.SceneContainerData;

            // Find which bins we cover.
            uint minBinX, minBinY, maxBinX, maxBinY;
            _GetBins(obj, out minBinX, out minBinY, out maxBinX, out maxBinY);

            _CheckSceneObjectBins(obj, scd, minBinX, minBinY, maxBinX, maxBinY);
        }

        /// <summary>
        /// Remove Scene Object from Container.
        /// </summary>
        /// <param name="obj">Object to remove.</param>
        public void RemoveSceneObject(ISceneContainerObject obj)
        {
            // Check everything is fine!
            Assert.Fatal(obj != null, "SceneContainer::removeSceneObject() - Invalid Object");

            // Get bin data
            SceneContainerData scd = obj.SceneContainerData;

            // Fetch Bin Chain Reference.
            SceneContainerBinReference refChain = scd._binReferenceChain;

            // Cut Chain from Object.
            scd._binReferenceChain = null;

            // Free Chain Scene Bin References.
            while (refChain != null)
            {
                // Fetch Free Reference.
                SceneContainerBinReference freeRef = refChain;

                // Move to next link.
                refChain = refChain._objectLink;

                // Remove Reference from Chain.
                if (freeRef._nextBinReference != null)
                    freeRef._nextBinReference._previousBinReference = freeRef._previousBinReference;
                freeRef._previousBinReference._nextBinReference = freeRef._nextBinReference;

                // Free Reference.
                _FreeSceneBinReference(freeRef);
            }
        }

        /// <summary>
        /// Perform a spatial query on the container.  Fields on the query will determine
        /// which objects are found.
        /// </summary>
        /// <param name="queryData">Query to perform.</param>
        public void FindObjects(SceneContainerQueryData queryData)
        {
            // Find which bins are covered.
            uint minX, minY, maxX, maxY;
            _GetBins(queryData, out minX, out minY, out maxX, out maxY);

            // Change the Current Sequence Key.
            _currentSequenceKey++;

            // *******************************************************************************
            // Fetch Standard Container Bins.
            // *******************************************************************************
            SceneContainerBinReference binChain = null;

            // Step through Bin ranges.
            for (uint y = minY; y <= maxY; y++)
            {
                // Calculate Bin Position.
                uint offsetY = (y % _binCount) * _binCount;

                for (uint x = minX; x <= maxX; x++)
                {
                    // Calculate Bin Position.
                    uint arrayIndex = offsetY + (x % _binCount);

                    // Fetch Bin Chain.
                    if (_sceneBinArray[arrayIndex] == null)
                        continue; // Bin is empty, continue

                    binChain = _sceneBinArray[arrayIndex]._nextBinReference;
                    _FindInBin(binChain, queryData);
                }
            }


            // *******************************************************************************
            // Fetch Overflow Bin Chain.
            // *******************************************************************************
            _FindInBin(_sceneOverflowBin._nextBinReference, queryData);

#if TORQUE_SCENECONTAINER_DEBUG
            int count2 = _FindObjectsInefficiently(iQueryData);
            Assert.Fatal(count2 == objectsFound, "doh!");
#endif
        }

        /// <summary>
        /// Get the number of active bins being referenced.  Not efficient.
        /// </summary>
        /// <returns>Number of references.</returns>
        public int GetNumActiveReferences()
        {
            int total = 0;
            foreach (SceneContainerBinReference binRef in _sceneBinArray)
            {
                SceneContainerBinReference walk = binRef;
                while (walk != null)
                {
                    if (walk._sceneObject != null)
                        total++;
                    walk = walk._nextBinReference;
                }
            }

            // check overflow bin
            SceneContainerBinReference walk2 = this._sceneOverflowBin;
            while (walk2 != null)
            {
                if (walk2._sceneObject != null)
                    total++;
                walk2 = walk2._nextBinReference;
            }

            return total;
        }

        /// <summary>
        /// Get the number of active objects in the system.  Not efficient.
        /// </summary>
        /// <returns>Number of active objects.</returns>
        public int GetNumObjects()
        {
            Dictionary<ISceneContainerObject, bool> objs = new Dictionary<ISceneContainerObject, bool>();

            foreach (SceneContainerBinReference binRef in _sceneBinArray)
            {
                SceneContainerBinReference walk = binRef;
                while (walk != null)
                {
                    if (walk._sceneObject != null && !objs.ContainsKey(walk._sceneObject))
                        objs[walk._sceneObject] = true;
                    walk = walk._nextBinReference;
                }
            }

            // check overflow bin
            SceneContainerBinReference walk2 = this._sceneOverflowBin;
            while (walk2 != null)
            {
                if (walk2._sceneObject != null && !objs.ContainsKey(walk2._sceneObject))
                    objs[walk2._sceneObject] = true;
                walk2 = walk2._nextBinReference;
            }

            return objs.Keys.Count;
        }

        #endregion


        #region Private, protected, internal methods

        public void Init()
        {
            if (_sceneBinArray != null)
                // already initialized
                return;

            Assert.Fatal(_binSize > 0, "SceneContainer requires a positive BinSize");
            Assert.Fatal(_binCount > 0, "SceneContainer requires a positive BinCount");

            _invBinSize = 1.0f / _binSize;
            // Calculate Total Bin Size.
            _totalBinSize = _binSize * _binCount;
            _CreateBins();
        }

        protected void _CheckSceneObjectBins(ISceneContainerObject obj, SceneContainerData scd, uint minBinX, uint minBinY, uint maxBinX, uint maxBinY)
        {
            // Check everything is fine!
            Assert.Fatal(obj != null, "Invalid Object");

            // Is the Scene Object in the container?
            if (scd._binReferenceChain == null)
            {
                // No, so add to the Scene Container.
                _AddSceneObject(obj);
                // Finish here.
                return;
            }

            // Check to see if the object has moved outside its allocated bins...

            // Should the bin allocation change?
            if (minBinX != scd._minBinX ||
                    minBinY != scd._minBinY ||
                    maxBinX != scd._maxBinX ||
                    maxBinY != scd._maxBinY)
            {
                // Yes, so remove scene object.
                RemoveSceneObject(obj);
                // Add the scene object back.
                _AddSceneObjectDirect(obj, minBinX, minBinY, maxBinX, maxBinY);
            }
        }

        protected void _FindInBin(SceneContainerBinReference bin, SceneContainerQueryData queryData)
        {
            object ignoreObject = queryData._ignoreObject;
            object[] ignoredObjects = queryData._ignoreObjects;
            ulong typebits = queryData._objectTypes._bits;

            // Step through Chain.
            while (bin != null)
            {
                // Fetch Scene Object Reference.
                ISceneContainerObject sceneObjectRef = bin._sceneObject;

                // Note: the following tries to get the object type directly from a TorqueObject rather than
                // going through the ISceneContainerObject interface.  This ends up being a LOT (a LOT) faster
                // for T2DSceneObjects even though it adds a branch.  It won't help when the object isn't a
                // TorqueObject as is the case of our T3D code, but we can consider other measures if that
                // becomes a problem (like making bin ref's point to SceneContainerData instead of scene object
                // and adding a pointer to iscene object in SceneContainerData as well as object type, but current
                // scheme is simpler and more effective for T2D).
                TorqueObject tobj = sceneObjectRef as TorqueObject;
                ulong ourType = tobj == null ? sceneObjectRef.ObjectType._bits : tobj._objectType;

                if ((ourType & typebits) == 0)
                {
                    // Move to next bin reference.
                    bin = bin._nextBinReference;
                    continue;
                }

                // get the scene container data for the object
                SceneContainerData scd = sceneObjectRef.SceneContainerData;

                // Have we dealt with this object already?
                if (scd._sequenceKey == _currentSequenceKey)
                {
                    // Move to next bin reference.
                    bin = bin._nextBinReference;
                    continue;
                }

                // Set the container sequence key to indicate that we've dealt with this object.
                scd._sequenceKey = _currentSequenceKey;

                // Check for ignored objects
                bool ignored = false;
                if (ignoreObject == sceneObjectRef)
                {
                    ignored = true;
                }
                else if (ignoredObjects != null)
                {
                    foreach (object o in ignoredObjects)
                        if (o == sceneObjectRef)
                        {
                            ignored = true;
                            break;
                        }
                }

                // Is the Object Not being ignored?
                if (!ignored)
                    _FoundObject(sceneObjectRef, queryData);

                // Move to next bin reference.
                bin = bin._nextBinReference;
            }
        }

        protected void _FindObjectsInefficientHelper(SceneContainerQueryData query, SceneContainerBinReference bin, ref int objectsFound)
        {
            SceneContainerBinReference walk = bin;
            while (walk != null)
            {
                if (walk._sceneObject != null)
                {
                    // Have we dealt with this object already?
                    if (walk._sceneObject.SceneContainerData._sequenceKey == _currentSequenceKey)
                    {
                        // Move to next bin reference.
                        walk = walk._nextBinReference;
                        continue;
                    }
                    walk._sceneObject.SceneContainerData._sequenceKey = _currentSequenceKey;

                    if (_IntersectsWith(walk._sceneObject, query))
                        objectsFound++;
                }
                walk = walk._nextBinReference;
            }
        }

        /// <summary>
        /// Inefficient, but always finds all the appropriate objects.  Good for verification of FindObjects.
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        protected internal int _FindObjectsInefficiently(SceneContainerQueryData query)
        {
            int objectsFound = 0;
            _currentSequenceKey++;

            foreach (SceneContainerBinReference binRef in _sceneBinArray)
                if (binRef != null)
                    _FindObjectsInefficientHelper(query, binRef, ref objectsFound);
            _FindObjectsInefficientHelper(query, _sceneOverflowBin, ref objectsFound);

            return objectsFound;
        }

        void _GetObjectsHelper(SceneContainerBinReference bin, List<ISceneObject> list)
        {
            for (SceneContainerBinReference walk = bin; walk != null; walk = walk._nextBinReference)
            {
                if (walk._sceneObject != null && walk._sceneObject.SceneContainerData._sequenceKey != _currentSequenceKey)
                {
                    list.Add(walk._sceneObject);
                    walk._sceneObject.SceneContainerData._sequenceKey = _currentSequenceKey;
                }

            }
        }

        protected internal void _GetObjects(List<ISceneObject> list)
        {
            _currentSequenceKey++;

            foreach (SceneContainerBinReference binRef in _sceneBinArray)
                if (binRef != null)
                    _GetObjectsHelper(binRef, list);
            _GetObjectsHelper(_sceneOverflowBin, list);
        }

        protected void _CreateBins()
        {
            // Create Scene Bin Array.
            _sceneBinArray = new SceneContainerBinReference[_binCount * _binCount];
            // if configured to do so, create individual array objects.
            // this can end up creating a lot of objects.
            if (PreAllocSceneBinArrayReferences)
                for (int i = 0; i < _sceneBinArray.Length; ++i)
                    _sceneBinArray[i] = new SceneContainerBinReference();

            // Initialise Scene Overflow Bin.
            _sceneOverflowBin = new SceneContainerBinReference();

            // Reset Free Scene Bin References.
            _freeSceneBinReferences = null;
            _CreateFreeListBlock();
        }

        protected void _CreateFreeListBlock()
        {
            if (!UseFreeList)
                return;

            for (int i = 0; i < FreeListBlockSize; ++i)
                _FreeSceneBinReference(new SceneContainerBinReference());
        }

        protected SceneContainerBinReference _AllocateSceneBinReference()
        {
            if (UseFreeList && _freeSceneBinReferences == null)
                _CreateFreeListBlock();

            if (UseFreeList && _freeSceneBinReferences != null)
            {
                SceneContainerBinReference freeRef = _freeSceneBinReferences;
                // update free list head pointer
                _freeSceneBinReferences = _freeSceneBinReferences._objectLink;
                // unlink the freeRef
                freeRef._objectLink = null;

                // all fields should be null on free ref at this point
                freeRef.AssertIfNotNull();
                return freeRef;
            }
            else
                return new SceneContainerBinReference();
        }

        protected void _FreeSceneBinReference(SceneContainerBinReference freeRef)
        {
            Assert.Fatal(freeRef != null, "doh");
            if (UseFreeList)
            {
                freeRef.Reset();
                freeRef._objectLink = _freeSceneBinReferences;
                _freeSceneBinReferences = freeRef;
            }
            //else, go garbage collector!
        }

        /// Get Bin Range (1D).
        protected void _GetBinRange(float min, float max, out uint minBin, out uint maxBin)
        {
            // Check Range.
            Assert.Fatal(max >= min, "SceneContainer::getBinRange() - Bad Range!");

            // Is the range bigger than our total bin size?
            if (max - min > _totalBinSize)
            {
                minBin = 0;
                maxBin = _binCount - 1;

                // Finish here.
                return;
            }
            else
            {
                minBin = (uint)(min * _invBinSize);
                maxBin = (uint)(max * _invBinSize);
                if (min < 0.0f)
                    // deal with inconvenient rounding of negative numbers up and positive down
                    minBin--;
                if (max < 0.0f)
                    // deal with inconvenient rounding of negative numbers up and positive down
                    maxBin--;
                uint delta = maxBin - minBin;
                minBin = minBin % _binCount; // Note: always positive since minBin is uint
                maxBin = minBin + delta;

                // Sanity!
                Assert.Fatal(maxBin >= minBin, "SceneContainer::getBinRange() - MinBin should always be less than MaxBin!");
            }
        }

        /// Get Bin Range
        abstract protected void _GetBins(ISceneContainerObject obj, out uint minBinX, out uint minBinY, out uint maxBinX, out uint maxBinY);
        abstract protected void _GetBins(SceneContainerQueryData query, out uint minBinX, out uint minBinY, out uint maxBinX, out uint maxBinY);

        /// Add Scene Object to Container.
        protected void _AddSceneObject(ISceneContainerObject obj)
        {
            // Check everything is fine!
            Assert.Fatal(obj != null, "SceneContainer::addSceneObject() - Invalid Object");
            SceneContainerData scd = obj.SceneContainerData;
            Assert.Fatal(scd._binReferenceChain == null, "SceneContainer::addSceneObject() - Object is already within a Container System!");

            // Find which bins we cover.
            uint minX, minY, maxX, maxY;
            _GetBins(obj, out minX, out minY, out maxX, out maxY);

            // Add the Scene Object Directly.
            _AddSceneObjectDirect(obj, minX, minY, maxX, maxY);
        }

        /// Add Scene Object to Container Directly (via Bin Bounds).
        protected void _AddSceneObjectDirect(ISceneContainerObject obj, uint minX, uint minY, uint maxX, uint maxY)
        {
            // Check everything is fine!
            Assert.Fatal(obj != null, "SceneContainer::addSceneObject() - Invalid Object");
            SceneContainerData scd = obj.SceneContainerData;
            Assert.Fatal(scd._binReferenceChain == null, "SceneContainer::addSceneObject() - Object is already within a Container System!");

            // Update Scene Object.
            scd._minBinX = minX;
            scd._minBinY = minY;
            scd._maxBinX = maxX;
            scd._maxBinY = maxY;

            // Very Large Objects got into the Scene Overflow bin.  Overflow bin?
            if ((maxX - minX + 1) * (maxY - minY + 1) < MaxBinsPerObject)
            {
                // Not in overflow bin
                for (uint y = minY; y <= maxY; y++)
                {
                    // Calculate Bin Position.
                    uint offsetY = (y % _binCount) * _binCount;

                    for (uint x = minX; x <= maxX; x++)
                    {
                        // Calculate Bin Position.
                        uint arrayIndex = offsetY + (x % _binCount);

                        // Allocate Scene Bin Reference.
                        SceneContainerBinReference freeRef = _AllocateSceneBinReference();

                        // Insert into Scene Bin Array.
                        if (_sceneBinArray[arrayIndex] == null)
                            _sceneBinArray[arrayIndex] = _AllocateSceneBinReference();

                        freeRef._sceneObject = obj;
                        freeRef._nextBinReference = _sceneBinArray[arrayIndex]._nextBinReference;
                        freeRef._previousBinReference = _sceneBinArray[arrayIndex];
                        freeRef._objectLink = null;

                        // Finalise the link.
                        if (_sceneBinArray[arrayIndex]._nextBinReference != null)
                            _sceneBinArray[arrayIndex]._nextBinReference._previousBinReference = freeRef;
                        _sceneBinArray[arrayIndex]._nextBinReference = freeRef;

                        // Insert Current Object Reference.
                        // JMQ: note, the following method of insertion results in a
                        // list ordering that is reversed from T2D.  Don't think it matters.
                        if (scd._binReferenceChain == null)
                        {
                            scd._binReferenceChain = freeRef;
                        }
                        else
                        {
                            freeRef._objectLink = scd._binReferenceChain;
                            scd._binReferenceChain = freeRef;
                        }
                    }
                }
            }
            else
            {
                // Yes, so allocate Scene Bin Reference.
                SceneContainerBinReference freeRef = _AllocateSceneBinReference();

                // Insert into Scene Overflow Bin.
                freeRef._sceneObject = obj;
                freeRef._nextBinReference = _sceneOverflowBin._nextBinReference;
                freeRef._previousBinReference = _sceneOverflowBin;
                freeRef._objectLink = null;

                // Finalise the link.
                if (_sceneOverflowBin._nextBinReference != null)
                    _sceneOverflowBin._nextBinReference._previousBinReference = freeRef;
                _sceneOverflowBin._nextBinReference = freeRef;

                // Set Current Object Reference.
                scd._binReferenceChain = freeRef;
            }

        }

        /// Found Object Tests
        abstract protected bool _FoundObject(ISceneContainerObject obj, SceneContainerQueryData query);
        abstract protected bool _IntersectsWith(ISceneContainerObject obj, SceneContainerQueryData query);

        #endregion


        #region Private, protected, internal fields

        BaseSceneGraph _sg;
        float _binSize;
        float _invBinSize;
        uint _binCount;
        int _maxBinsPerObject = 50;
        float _totalBinSize;
        uint _currentSequenceKey;

        SceneContainerBinReference[] _sceneBinArray;
        SceneContainerBinReference _sceneOverflowBin;
        SceneContainerBinReference _freeSceneBinReferences;

        #endregion

        #region IDisposable Members

        public override void Dispose()
        {
            _IsDisposed = true;
            for (uint i = 0; i < _sceneBinArray.Length; i++)
            {
                if (_sceneBinArray[i] != null)
                    _sceneBinArray[i].Reset(); // Just to make sure to not create a circular reference of disposed objects
                _sceneBinArray[i] = null;
            }

            _ResetRefs();
            _sceneOverflowBin.Reset(); // Just to make sure to not create a circular reference of disposed objects
            _sceneOverflowBin = null;
            _sceneBinArray = null;
            _sg.Reset();
            _sg = null;
            base.Dispose();
        }

        #endregion
    }
}
