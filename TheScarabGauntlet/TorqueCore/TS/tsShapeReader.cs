//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Microsoft.Xna.Framework;
using GarageGames.Torque.Core;
using GarageGames.Torque.MathUtil;
using GarageGames.Torque.Util;
using U8 = System.Byte;
using S8 = System.SByte;



namespace GarageGames.Torque.TS
{
    /// <summary>
    /// Helper class for reading a shape from a dts file or a sequence from a dsq file.
    /// </summary>
    public class ShapeReader
    {

        #region Static methods, fields, constructors

        static void _readAndExtendArray(BinaryReader bin, ref Vector3[] array, int addNum)
        {
            int startSize = array.Length;
            TorqueUtil.ResizeArray(ref array, startSize + addNum);
            for (int i = startSize; i < startSize + addNum; i++)
            {
                array[i].X = bin.ReadSingle();
                array[i].Y = bin.ReadSingle();
                array[i].Z = bin.ReadSingle();
            }
        }



        static void _readAndExtendArray(BinaryReader bin, ref Quat16[] array, int addNum)
        {
            int startSize = array.Length;
            TorqueUtil.ResizeArray(ref array, startSize + addNum);
            for (int i = startSize; i < startSize + addNum; i++)
            {
                array[i].X = bin.ReadInt16();
                array[i].Y = bin.ReadInt16();
                array[i].Z = bin.ReadInt16();
                array[i].W = bin.ReadInt16();
            }
        }



        static void _readAndExtendArray(BinaryReader bin, ref float[] array, int addNum)
        {
            int startSize = array.Length;
            TorqueUtil.ResizeArray(ref array, startSize + addNum);
            for (int i = startSize; i < startSize + addNum; i++)
                array[i] = bin.ReadSingle();
        }

        #endregion


        #region Constructors

        public ShapeReader() { }



        /// <summary>
        /// Initializes the shape reader for reading sequences (dsq files).
        /// </summary>
        /// <param name="shape">The shape to import the sequence into.</param>
        public ShapeReader(Shape shape)
        {
            _shape = shape;
        }

        #endregion


        #region Public methods

        /// <summary>
        /// Reads a shape from a stream.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <returns>The new shape.</returns>
        public Shape ReadShape(Stream stream)
        {
            _shape = new Shape();

            BinaryReader bin = new BinaryReader(stream);
            int readVersion = bin.ReadInt32();
            int exporterVersion = readVersion >> 16;
            readVersion &= 0xFF;
            if (readVersion > Shape.WriteVersion || readVersion < 24)
            {
                Assert.Fatal(false, "TSShapeReader.ReadShape - Old shape formats are not supported.");
                return null;
            }

            Shape.ReadVersion = readVersion;

            int sizeMemBuffer, startushort, startU8;
            sizeMemBuffer = 4 * bin.ReadInt32();
            startushort = 4 * bin.ReadInt32();
            startU8 = 4 * bin.ReadInt32();

            byte[] buffer = bin.ReadBytes(sizeMemBuffer);

            ShapeStream shapeStream = new ShapeStream(buffer, 0, startushort, startU8, startushort, startU8 - startushort, sizeMemBuffer - startU8);

            // Read sequences
            int numSequences = bin.ReadInt32();
            _shape.Sequences = new Sequence[numSequences];
            for (int i = 0; i < numSequences; i++)
            {
                _shape.Sequences[i] = new Sequence();
                _ReadSequence(bin, ref _shape.Sequences[i], true);
            }

            // Read material list
            _ReadMaterialList(bin);

            _AssembleShape(shapeStream);

            _shape.Initialize();

            return _shape;
        }



        /// <summary>
        /// Reads a sequence from a stream.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <returns>The shape.</returns>
        public Shape ImportSequence(Stream stream)
        {
            return ImportSequence(stream, null);
        }



        /// <summary>
        /// Reads a sequence from a stream and assigns it a name.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <param name="seqName">The name to assign.</param>
        /// <returns>The shape.</returns>
        public Shape ImportSequence(Stream stream, string seqName)
        {
            BinaryReader bin = new BinaryReader(stream);
            int readVersion = bin.ReadInt32();
            int exporterVersion = readVersion >> 16;
            readVersion &= 0xFF;
            if (readVersion > Shape.WriteVersion || readVersion < 22)
                return null;

            Shape.ReadVersion = readVersion;
            Assert.Fatal(Shape.ReadVersion >= 22, "TSShapeReader.ImportSequence - Old shape formats are not supported.");

            int[] nodeMap;   // node index of each node from imported sequences
            List<int> checkForDups = new List<int>();

            // Read node names
            // -- this is how we will map imported _sequence nodes to our nodes
            int sz = bin.ReadInt32();
            nodeMap = new int[sz];
            for (int i = 0; i < sz; i++)
            {
                int startSize = _shape.Names.Length;
                int nameIndex = _ReadName(bin, true);
                int count = 0;
                if (nameIndex >= 0)
                {
                    while (checkForDups.Count < nameIndex + 1)
                        checkForDups.Add(0);

                    count = checkForDups[nameIndex]++;
                }

                if (count != 0)
                {
                    // not first time this Name came up...look for later instance of the node
                    nodeMap[i] = -1;
                    for (int j = 0; j < _shape.Nodes.Length; j++)
                    {
                        if (_shape.Nodes[j].NameIndex == nameIndex && count-- == 0)
                        {
                            nodeMap[i] = j;
                            if (j == _shape.Nodes.Length)
                                return null;

                            break;
                        }
                    }
                }
                else
                {
                    nodeMap[i] = _shape.FindNode(nameIndex);
                }

                if (nodeMap[i] < 0)
                {
                    // error -- node found in _sequence but not shape
                    if (_shape.Names.Length != startSize)
                        Assert.Fatal(_shape.Names.Length == startSize, "Shape.ImportSequence - Invalid node in sequence.");

                    return null;
                }
            }

            // Read the following size, but won't do anything with it...legacy:  was going to support
            // import of sequences that Animate objects...we don't...
            sz = bin.ReadInt32();

            // before reading keyframes, take note of a couple numbers
            int oldShapeNumObjects = bin.ReadInt32();

            // adjust all the new keyframes
            int adjNodeRots = _shape.NodeRotations.Length;
            int adjNodeTrans = _shape.NodeTranslations.Length;
            int adjNodeScales1 = _shape.NodeUniformScales.Length;
            int adjNodeScales2 = _shape.NodeAlignedScales.Length;
            int adjNodeScales3 = _shape.NodeArbitraryScaleFactors.Length;
            int adjObjectStates = _shape.ObjectStates.Length - oldShapeNumObjects;
            int adjGroundStates = _shape.GroundTranslations.Length; // groundTrans==groundRot

            // add these node states to our own
            int addNum = bin.ReadInt32();
            _readAndExtendArray(bin, ref _shape.NodeRotations, addNum);
            addNum = bin.ReadInt32();
            _readAndExtendArray(bin, ref _shape.NodeTranslations, addNum);
            addNum = bin.ReadInt32();
            _readAndExtendArray(bin, ref _shape.NodeUniformScales, addNum);
            addNum = bin.ReadInt32();
            _readAndExtendArray(bin, ref _shape.NodeAlignedScales, addNum);
            addNum = bin.ReadInt32();
            _readAndExtendArray(bin, ref _shape.NodeArbitraryScaleRotations, addNum);
            _readAndExtendArray(bin, ref _shape.NodeArbitraryScaleFactors, addNum);
            addNum = bin.ReadInt32();
            _readAndExtendArray(bin, ref _shape.GroundTranslations, addNum);
            _readAndExtendArray(bin, ref _shape.GroundRotations, addNum);

            // add these object states to our own -- shouldn't be any...assume it
            bin.ReadInt32();

            // Read sequences
            sz = bin.ReadInt32();
            int startSeqNum = _shape.Sequences.Length;
            TorqueUtil.ResizeArray<Sequence>(ref _shape.Sequences, startSeqNum + sz);

            for (int i = startSeqNum; i < startSeqNum + sz; i++)
            {
                _shape.Sequences[i] = new Sequence();
                Sequence seq = _shape.Sequences[i];

                // Read Name
                seq.NameIndex = _ReadName(bin, true);

                // Read the rest of the _sequence
                _ReadSequence(bin, ref seq, false);
                seq.BaseRotation += adjNodeRots;
                seq.BaseTranslation += adjNodeTrans;
                if (seq.IsUniformScaleAnimated())
                    seq.BaseScale += adjNodeScales1;
                else if (seq.IsAlignedScaleAnimated())
                    seq.BaseScale += adjNodeScales2;
                else if (seq.IsArbitraryScaleAnimated())
                    seq.BaseScale += adjNodeScales3;

                // not quite so easy...
                // now we have to remap nodes from shape the _sequence came from to this shape
                // that's where nodeMap comes in handy...
                // ditto for the objects.

                // first the nodes
                BitVector newMembership1 = new BitVector();
                BitVector newMembership2 = new BitVector();
                BitVector newMembership3 = new BitVector();
                newMembership1.SetSize(_shape.Nodes.Length);
                newMembership2.SetSize(_shape.Nodes.Length);
                newMembership3.SetSize(_shape.Nodes.Length);
                for (int j = 0; j < nodeMap.Length; j++)
                {
                    if (seq.DoesTranslationMatter.Test(j))
                        newMembership1.Set(nodeMap[j]);

                    if (seq.DoesRotationMatter.Test(j))
                        newMembership2.Set(nodeMap[j]);

                    if (seq.DoesScaleMatter.Test(j))
                        newMembership3.Set(nodeMap[j]);
                }

                seq.DoesTranslationMatter = newMembership1;
                seq.DoesRotationMatter = newMembership2;
                seq.DoesScaleMatter = newMembership3;

                // adjust trigger numbers...we'll Read triggers after sequences...
                seq.FirstTrigger += _shape.Triggers.Length;

                // finally, adjust ground transform's nodes states
                seq.FirstGroundFrame += adjGroundStates;
            }

            // Do we need to rename the last loaded sequence?
            if (seqName != null && _shape.Sequences.Length != 0)
            {
                Sequence seq = _shape.Sequences[_shape.Sequences.Length - 1];

                int nameIndex = _shape.FindName(seqName);
                if (nameIndex < 0)
                {
                    nameIndex = _shape.Names.Length;
                    TorqueUtil.ResizeArray(ref _shape.Names, _shape.Names.Length + 1);
                    _shape.Names[_shape.Names.Length - 1] = seqName;
                }

                seq.NameIndex = nameIndex;
            }

            // add the new triggers
            sz = bin.ReadInt32();
            int startNum = _shape.Triggers.Length;
            TorqueUtil.ResizeArray<Trigger>(ref _shape.Triggers, sz + startNum);
            for (int i = startNum; i < startNum + sz; i++)
            {
                _shape.Triggers[i].State = bin.ReadInt32();
                _shape.Triggers[i].Pos = bin.ReadSingle();
            }

            _shape.Initialize();

            return _shape;
        }

        #endregion


        #region Private, protected, internal methods

        int _ReadName(BinaryReader bin, bool addName)
        {
            int sz = bin.ReadInt32();
            int nameIndex = -1;
            if (sz != 0)
            {
                String name = new String(bin.ReadChars(sz));
                nameIndex = _shape.FindName(name);
                if (nameIndex < 0 && addName)
                {
                    nameIndex = _shape.Names.Length;
                    TorqueUtil.ResizeArray(ref _shape.Names, _shape.Names.Length + 1);
                    _shape.Names[_shape.Names.Length - 1] = name;
                }
            }

            return nameIndex;
        }



        void _ReadSequence(BinaryReader bin, ref Sequence seq, bool readNameIndex)
        {
            if (readNameIndex)
                seq.NameIndex = bin.ReadInt32();

            seq.Flags = (SequenceFlags)bin.ReadUInt32();
            seq.KeyframeCount = bin.ReadInt32();
            seq.Duration = bin.ReadSingle();
            seq.Priority = bin.ReadInt32();
            seq.FirstGroundFrame = bin.ReadInt32();
            seq.GroundFrameCount = bin.ReadInt32();
            seq.BaseRotation = bin.ReadInt32();
            seq.BaseTranslation = bin.ReadInt32();
            seq.BaseScale = bin.ReadInt32();
            seq.BaseObjectState = bin.ReadInt32();
            int baseDecalState = bin.ReadInt32(); // deprecated
            seq.FirstTrigger = bin.ReadInt32();
            seq.TriggerCount = bin.ReadInt32();
            seq.ToolBegin = bin.ReadSingle();
            seq.DoesRotationMatter.Read(bin);
            seq.DoesTranslationMatter.Read(bin);
            seq.DoesScaleMatter.Read(bin);
            BitVector decalMatters = new BitVector(); // deprecated
            decalMatters.Read(bin);
            seq.DoesIflMatter.Read(bin);
            seq.DoesVisibilityMatter.Read(bin);
            seq.DoesFrameMatter.Read(bin);
            seq.DoesMaterialFrameMatter.Read(bin);

            // figure out dirty _flags
            seq.DirtyFlags = 0;

            if (seq.DoesRotationMatter.TestAll() || seq.DoesTranslationMatter.TestAll() || seq.DoesScaleMatter.TestAll())
                seq.DirtyFlags |= DirtyFlags.TransformDirty;

            if (seq.DoesVisibilityMatter.TestAll())
                seq.DirtyFlags |= DirtyFlags.VisDirty;

            if (seq.DoesFrameMatter.TestAll())
                seq.DirtyFlags |= DirtyFlags.FrameDirty;

            if (seq.DoesMaterialFrameMatter.TestAll())
                seq.DirtyFlags |= DirtyFlags.MatFrameDirty;

            if (seq.DoesIflMatter.TestAll())
                seq.DirtyFlags |= DirtyFlags.IflDirty;
        }



        bool _ReadMaterialList(BinaryReader bin)
        {
            // parent Read...

            U8 version = bin.ReadByte();
            Assert.Fatal(version == 1, "TSShapeReader._ReadMaterialList - Material list embedded in ts shape is wrong version!");
            if (version != 1)
                return false;

            // how many materials?
            uint count = bin.ReadUInt32();

            // pre-size the vectors for efficiency
            _shape.MaterialList = new Material[count];

            // Read in the materials
            for (uint i = 0; i < count; i++)
            {
                // Load the bitmap Name
                U8 len = bin.ReadByte();
                char[] chars = bin.ReadChars(len);

                // Material paths are a legacy of Tribes tools,
                // strip them off...
                int start = len;
                while (start != 0)
                {
                    --start;
                    if (chars[start] == '/' || chars[start] == '\\')
                    {
                        ++start;
                        break;
                    }
                }

                _shape.MaterialList[i].Name = new String(chars, start, len - start);
            }

            for (uint i = 0; i < count; i++)
                _shape.MaterialList[i].Flags = (MaterialFlags)bin.ReadUInt32();
            for (uint i = 0; i < count; i++)
                bin.ReadInt32(); // reflectance map
            for (uint i = 0; i < count; i++)
                bin.ReadInt32(); // bump map
            for (uint i = 0; i < count; i++)
                bin.ReadInt32(); // detail map
            for (uint i = 0; i < count; i++)
                bin.ReadSingle(); // detail map scale
            for (uint i = 0; i < count; i++)
                bin.ReadSingle(); // reflection amount

            // get rid of Name of any ifl material names
            for (uint i = 0; i < count; i++)
            {
                if ((_shape.MaterialList[i].Flags & MaterialFlags.IflMaterial) != 0)
                    _shape.MaterialList[i].Name = null;
            }

            return true;
        }



        void _AssembleShape(ShapeStream shapeStream)
        {
            int i;

            // get counts...
            int numNodes = shapeStream.ReadS32();
            int numObjects = shapeStream.ReadS32();
            int numDecals = shapeStream.ReadS32();
            int numSubShapes = shapeStream.ReadS32();
            int numIflMaterials = shapeStream.ReadS32();
            int numNodeRots = shapeStream.ReadS32();
            int numNodeTrans = shapeStream.ReadS32();
            int numNodeUniformScales = shapeStream.ReadS32();
            int numNodeAlignedScales = shapeStream.ReadS32();
            int numNodeArbitraryScales = shapeStream.ReadS32();
            int numGroundFrames = shapeStream.ReadS32();
            int numObjectStates = shapeStream.ReadS32();
            int numDecalStates = shapeStream.ReadS32();
            int numTriggers = shapeStream.ReadS32();
            int numDetails = shapeStream.ReadS32();
            int numMeshes = shapeStream.ReadS32();
            int numNames = shapeStream.ReadS32();
            _shape.SmallestVisibleSize = shapeStream.ReadF32();
            _shape.SmallestVisibleDL = shapeStream.ReadS32();

            shapeStream.CheckGuard();

            // get bounds...
            _shape.Radius = shapeStream.ReadF32();
            _shape.TubeRadius = shapeStream.ReadF32();
            shapeStream.ReadPoint3F(ref _shape.Center);
            shapeStream.ReadPoint3F(ref _shape.Bounds.Min);
            shapeStream.ReadPoint3F(ref _shape.Bounds.Max);

            shapeStream.CheckGuard();

            // allocate some storage
            _shape.SubShapeFirstTranslucentObject = new int[numSubShapes];

            // Read in nodes
            _shape.Nodes = new Node[numNodes];
            for (i = 0; i < numNodes; i++)
            {
                _shape.Nodes[i].NameIndex = shapeStream.ReadS32();
                _shape.Nodes[i].ParentIndex = shapeStream.ReadS32();
                _shape.Nodes[i].FirstObject = shapeStream.ReadS32();
                _shape.Nodes[i].FirstChild = shapeStream.ReadS32();
                _shape.Nodes[i].NextSibling = shapeStream.ReadS32();
            }

            shapeStream.CheckGuard();

            // Read in objects
            _shape.Objects = new Object[numObjects];
            for (i = 0; i < numObjects; i++)
            {
                _shape.Objects[i].NameIndex = shapeStream.ReadS32();
                _shape.Objects[i].MeshCount = shapeStream.ReadS32();
                _shape.Objects[i].FirstMesh = shapeStream.ReadS32();
                _shape.Objects[i].NodeIndex = shapeStream.ReadS32();
                _shape.Objects[i].NextSibling = shapeStream.ReadS32();
                shapeStream.ReadS32(); // decals are deprecated
            }

            shapeStream.CheckGuard();

            // should be no decals, but Read them in and ignore if they are there
            shapeStream.ReadS32s(numDecals * 5);

            shapeStream.CheckGuard();

            // ifl materials
            _shape.IflMaterials = new IflMaterial[numIflMaterials];
            for (i = 0; i < numIflMaterials; i++)
            {
                _shape.IflMaterials[i].NameIndex = shapeStream.ReadS32();
                _shape.IflMaterials[i].MaterialSlot = shapeStream.ReadS32();
                _shape.IflMaterials[i].FirstFrame = shapeStream.ReadS32();
                _shape.IflMaterials[i].FirstFrameOffTimeIndex = shapeStream.ReadS32();
                _shape.IflMaterials[i].FrameCount = shapeStream.ReadS32();
            }

            shapeStream.CheckGuard();

            // subshape first lists...
            _shape.SubShapeFirstNode = shapeStream.ReadS32s(numSubShapes);
            _shape.SubShapeFirstObject = shapeStream.ReadS32s(numSubShapes);
            shapeStream.ReadS32s(numSubShapes); // no decal support

            shapeStream.CheckGuard();

            // subshape num lists...
            _shape.SubShapeNodeCount = shapeStream.ReadS32s(numSubShapes);
            _shape.SubShapeObjectCount = shapeStream.ReadS32s(numSubShapes);
            shapeStream.ReadS32s(numSubShapes); // no decal support

            shapeStream.CheckGuard();

            // get default rotations and translations
            _shape.DefaultRotations = shapeStream.ReadQuat16s(numNodes);
            _shape.DefaultTranslations = shapeStream.ReadPoint3Fs(numNodes);

            // get _sequence rotation and translations
            _shape.NodeTranslations = shapeStream.ReadPoint3Fs(numNodeTrans);
            _shape.NodeRotations = shapeStream.ReadQuat16s(numNodeRots);

            shapeStream.CheckGuard();

            // more node _sequence data...Scale
            _shape.NodeUniformScales = shapeStream.ReadF32s(numNodeUniformScales);
            _shape.NodeAlignedScales = shapeStream.ReadPoint3Fs(numNodeAlignedScales);
            _shape.NodeArbitraryScaleFactors = shapeStream.ReadPoint3Fs(numNodeArbitraryScales);
            _shape.NodeArbitraryScaleRotations = shapeStream.ReadQuat16s(numNodeArbitraryScales);

            shapeStream.CheckGuard();

            // ground _sequence data
            _shape.GroundTranslations = shapeStream.ReadPoint3Fs(numGroundFrames);
            _shape.GroundRotations = shapeStream.ReadQuat16s(numGroundFrames);

            shapeStream.CheckGuard();

            // object states
            _shape.ObjectStates = new ObjectState[numObjectStates];
            for (i = 0; i < numObjectStates; i++)
            {
                _shape.ObjectStates[i].Visibility = shapeStream.ReadF32();
                _shape.ObjectStates[i].FrameIndex = shapeStream.ReadS32();
                _shape.ObjectStates[i].MaterialFrameIndex = shapeStream.ReadS32();

            }

            shapeStream.CheckGuard();

            shapeStream.ReadS32s(numDecalStates); // no decal support

            shapeStream.CheckGuard();

            // frame triggers
            _shape.Triggers = new Trigger[numTriggers];
            for (i = 0; i < numTriggers; i++)
            {
                _shape.Triggers[i].State = shapeStream.ReadS32();
                _shape.Triggers[i].Pos = shapeStream.ReadF32();
            }

            shapeStream.CheckGuard();

            // details
            _shape.Details = new Detail[numDetails];
            for (i = 0; i < numDetails; i++)
            {
                _shape.Details[i].NameIndex = shapeStream.ReadS32();
                _shape.Details[i].SubShapeNumber = shapeStream.ReadS32();
                _shape.Details[i].ObjectDetailNumber = shapeStream.ReadS32();
                _shape.Details[i].PixelSize = shapeStream.ReadF32();
                _shape.Details[i].AverageError = shapeStream.ReadF32();
                _shape.Details[i].MaxError = shapeStream.ReadF32();
                _shape.Details[i].PolyCount = shapeStream.ReadS32();

                if (Shape.ReadVersion >= 26)
                {
                    shapeStream.ReadU32s(6);
                }
            }

            shapeStream.CheckGuard();

            // Read in meshes...this is much simpler than in C++ code because
            // we never skip a detail level on load
            _shape.Meshes = new Mesh[numMeshes];
            for (i = 0; i < numMeshes; i++)
            {
                Mesh.MeshEnum meshType = (Mesh.MeshEnum)shapeStream.ReadS32();
                _shape.Meshes[i] = _AssembleMesh(shapeStream, meshType);
            }

            shapeStream.CheckGuard();

            _shape.Names = new String[numNames];
            for (i = 0; i < numNames; i++)
            {
                _shape.Names[i] = shapeStream.ReadCString();
            }

            shapeStream.CheckGuard();
        }



        Mesh _AssembleMesh(ShapeStream shapeStream, Mesh.MeshEnum meshType)
        {
            if (meshType == Mesh.MeshEnum.StandardMeshType)
                return _AssembleStandardMesh(shapeStream, null);
            else if (meshType == Mesh.MeshEnum.SkinMeshType)
                return _AssembleSkinMesh(shapeStream);
            else if (meshType == Mesh.MeshEnum.SortedMeshType)
                return _AssembleSortedMesh(shapeStream);

            return null;
        }



        Mesh _AssembleStandardMesh(ShapeStream shapeStream, Mesh mesh)
        {
            try
            {
                if (mesh == null)
                    mesh = new Mesh();

                shapeStream.CheckGuard();

                mesh._numFrames = shapeStream.ReadS32();
                mesh._numMatFrames = shapeStream.ReadS32();
                mesh._parentMesh = shapeStream.ReadS32();
                shapeStream.ReadPoint3F(ref mesh._bounds.Min);
                shapeStream.ReadPoint3F(ref mesh._bounds.Max);
                shapeStream.ReadPoint3F(ref mesh._center);
                mesh._radius = shapeStream.ReadF32();

                int numVerts = shapeStream.ReadS32();
                mesh._verts = mesh._parentMesh < 0 ? shapeStream.ReadPoint3Fs(numVerts) : _shape.Meshes[mesh._parentMesh]._verts;

                int numTVerts = shapeStream.ReadS32();
                mesh._tverts = mesh._parentMesh < 0 ? shapeStream.ReadPoint2Fs(numTVerts) : _shape.Meshes[mesh._parentMesh]._tverts;

                // Version 26 added vertex color and texcoord2.
                if (Shape.ReadVersion > 25)
                {
                    int numTVerts2 = shapeStream.ReadS32();
                    if (numTVerts2 > 0)
                    {
                        if (mesh._parentMesh < 0)
                            mesh._tverts2 = shapeStream.ReadPoint2Fs(numTVerts2);
                        else
                            mesh._tverts2 = _shape.Meshes[mesh._parentMesh]._tverts2;
                    }

                    int numVColors = shapeStream.ReadS32();
                    if (numVColors > 0)
                    {
                        if (mesh._parentMesh < 0)
                            mesh._colors = shapeStream.ReadColors(numVColors);
                        else
                            mesh._colors = _shape.Meshes[mesh._parentMesh]._colors;
                    }
                }

                mesh._norms = mesh._parentMesh < 0 ? shapeStream.ReadPoint3Fs(numVerts) : _shape.Meshes[mesh._parentMesh]._norms;
                if (mesh._parentMesh < 0)
                    shapeStream.ReadU8s(numVerts); // Read past encoded normals

                int szPrim;
                ushort[] prim16;
                int[] prim32;
                int szInd;
                short[] ind16;

                // Version 26 has 32-bit indices.
                // We don't, so read them into our 16-bit buffers.
                if (Shape.ReadVersion > 25)
                {
                    szPrim = shapeStream.ReadS32();
                    int[] primIn = shapeStream.ReadS32s(szPrim * 3);

                    szInd = shapeStream.ReadS32();
                    int[] indIn = shapeStream.ReadS32s(szInd);

                    // 
                    prim16 = new ushort[szPrim * 2];
                    prim32 = new int[szPrim];

                    int j = 0;
                    for (int i = 0; i < szPrim; i += 3)
                    {
                        prim16[j] = (ushort)primIn[i];
                        prim16[j + 1] = (ushort)primIn[i + 1];
                        prim32[j] = primIn[i + 2];

                        j++;
                    }

                    // Copy down the array of indices from 32-bit to 16.
                    ind16 = new short[szInd];
                    for (int i = 0; i < szInd; i++)
                        ind16[i] = (short)indIn[i];
                }
                else
                {
                    // Copy the _primitives and _indices...how we do this depends on what
                    // form we want them in when copied...first just get the data
                    szPrim = shapeStream.ReadS32();
                    prim16 = shapeStream.ReadU16s(szPrim * 2); // start, numElements
                    prim32 = shapeStream.ReadS32s(szPrim);     // matIndex

                    szInd = shapeStream.ReadS32();
                    ind16 = shapeStream.ReadS16s(szInd);
                }

                // count then Copy...
                int cpyPrim, cpyInd;
                if (_useTriangles)
                    _ConvertToTris(prim16, prim32, ind16, szPrim, out cpyPrim, out cpyInd, null, null);
                else if (_useOneStrip)
                    _ConvertToSingleStrip(prim16, prim32, ind16, szPrim, out cpyPrim, out cpyInd, null, null);
                else
                    _LeaveAsMultipleStrips(prim16, prim32, ind16, szPrim, out cpyPrim, out cpyInd, null, null);
                mesh._primitives = new DrawPrimitive[cpyPrim];
                mesh._indices = new short[cpyInd];

                // _primitives and _indices counted and allocated above, now Copy them in
                int chkPrim;
                int chkInd;
                if (_useTriangles)
                    _ConvertToTris(prim16, prim32, ind16, szPrim, out chkPrim, out chkInd, mesh._primitives, mesh._indices);
                else if (_useOneStrip)
                    _ConvertToSingleStrip(prim16, prim32, ind16, szPrim, out chkPrim, out chkInd, mesh._primitives, mesh._indices);
                else
                    _LeaveAsMultipleStrips(prim16, prim32, ind16, szPrim, out chkPrim, out chkInd, mesh._primitives, mesh._indices);

                Assert.Fatal(chkPrim == cpyPrim && chkInd == cpyInd, "TSShapeReader._AssembleStandardMesh - DrawPrimitive conversion error.");

                // Read in merge _indices...deprecated
                int numMerge = shapeStream.ReadS32();
                shapeStream.ReadS16s(numMerge);

                mesh._vertsPerFrame = shapeStream.ReadS32();
                mesh.SetFlags((Mesh.MeshEnum)shapeStream.ReadU32());

                shapeStream.CheckGuard();
            }
            catch
            { }

            return mesh;
        }



        Mesh _AssembleSkinMesh(ShapeStream shapeStream)
        {
            SkinMesh mesh = new SkinMesh();
            _AssembleStandardMesh(shapeStream, mesh);

            int numVerts = shapeStream.ReadS32();
            if (mesh._parentMesh < 0)
            {
                mesh._initialVerts = shapeStream.ReadPoint3Fs(numVerts);
                mesh._initialNormals = shapeStream.ReadPoint3Fs(numVerts);

                // eat up the encoded normals
                if (mesh._parentMesh < 0)
                    shapeStream.ReadU8s(numVerts);

                int sz = shapeStream.ReadS32();
                mesh._initialTransforms = new Matrix[sz];
                for (int i = 0; i < sz; i++)
                {
                    float[] mat = shapeStream.ReadF32s(16);
                    mesh._initialTransforms[i].M11 = mat[0];
                    mesh._initialTransforms[i].M21 = mat[1];
                    mesh._initialTransforms[i].M31 = mat[2];
                    mesh._initialTransforms[i].M41 = mat[3];
                    mesh._initialTransforms[i].M12 = mat[4];
                    mesh._initialTransforms[i].M22 = mat[5];
                    mesh._initialTransforms[i].M32 = mat[6];
                    mesh._initialTransforms[i].M42 = mat[7];
                    mesh._initialTransforms[i].M13 = mat[8];
                    mesh._initialTransforms[i].M23 = mat[9];
                    mesh._initialTransforms[i].M33 = mat[10];
                    mesh._initialTransforms[i].M43 = mat[11];
                    mesh._initialTransforms[i].M14 = mat[12];
                    mesh._initialTransforms[i].M24 = mat[13];
                    mesh._initialTransforms[i].M34 = mat[14];
                    mesh._initialTransforms[i].M44 = mat[15];
                }

                sz = shapeStream.ReadS32();
                mesh._vertexIndex = shapeStream.ReadS32s(sz);
                mesh._boneIndex = shapeStream.ReadU32s(sz);
                mesh._weight = shapeStream.ReadF32s(sz);

                sz = shapeStream.ReadS32();
                mesh._nodeIndex = shapeStream.ReadS32s(sz);
            }
            else
            {
                SkinMesh otherSkin = _shape.Meshes[mesh._parentMesh] as SkinMesh;
                if (otherSkin == null)
                {
                    Assert.Fatal(false, "TSShapeReader._AssembleSkinMesh - Parent of skin mesh not a skin mesh.");
                    return null;
                }

                // consult our parent for our vectors
                mesh._initialVerts = otherSkin._initialVerts;
                mesh._initialNormals = otherSkin._initialNormals;

                // eat up the encoded normals
                if (mesh._parentMesh < 0)
                    shapeStream.ReadU8s(numVerts);

                int sz = shapeStream.ReadS32();
                mesh._initialTransforms = otherSkin._initialTransforms;

                sz = shapeStream.ReadS32();
                mesh._vertexIndex = otherSkin._vertexIndex;
                mesh._boneIndex = otherSkin._boneIndex;
                mesh._weight = otherSkin._weight;

                sz = shapeStream.ReadS32();
                mesh._nodeIndex = otherSkin._nodeIndex;
            }

            shapeStream.CheckGuard();

            return mesh;
        }



        Mesh _AssembleSortedMesh(ShapeStream shapeStream)
        {
            SortedMesh mesh = new SortedMesh();

            bool save1 = _useTriangles;
            bool save2 = _useOneStrip;
            _useTriangles = false;
            _useOneStrip = false;

            _AssembleStandardMesh(shapeStream, mesh);

            _useTriangles = save1;
            _useOneStrip = save2;

            int numClusters = shapeStream.ReadS32();
            mesh._clusters = new SortedMesh.Cluster[numClusters];
            SortedMesh.Cluster[] clusters = mesh._clusters;
            for (int i = 0; i < numClusters; i++)
            {
                clusters[i].StartPrimitive = shapeStream.ReadS32();
                clusters[i].EndPrimitive = shapeStream.ReadS32();
                shapeStream.ReadPoint3F(ref clusters[i].Normal);
                clusters[i].K = shapeStream.ReadF32();
                clusters[i].FrontCluster = shapeStream.ReadS32();
                clusters[i].BackCluster = shapeStream.ReadS32();
            }

            // Code before reads in arrays but only uses the first element.
            // This is because we no longer support frame based sorted meshes, but
            // we need to Read old files.

            int sz = shapeStream.ReadS32();
            int[] ints = shapeStream.ReadS32s(sz);
            mesh._startCluster = sz != 0 ? ints[0] : -1;

            sz = shapeStream.ReadS32();
            ints = shapeStream.ReadS32s(sz);
            mesh._firstVert = sz != 0 ? ints[0] : -1;

            sz = shapeStream.ReadS32();
            ints = shapeStream.ReadS32s(sz);
            mesh._numVerts = sz != 0 ? ints[0] : -1;

            sz = shapeStream.ReadS32();
            ints = shapeStream.ReadS32s(sz);
            mesh._firstTVert = sz != 0 ? ints[0] : -1;

            // throw away alwaysWriteDepth bool
            shapeStream.ReadS32();

            shapeStream.CheckGuard();

            return mesh;
        }



        // utility routines for converting draw primitive format from list of strips to tris
        void _ConvertToTris(ushort[] primitiveDataIn, int[] primitiveMatIn, short[] indicesIn, int numPrimIn,
                out int numPrimOut, out int numIndicesOut,
                DrawPrimitive[] primitivesOut, short[] indicesOut)
        {
            numPrimOut = -1;
            numIndicesOut = 0;

            int prevMaterial = -99999;
            for (int i = 0; i < numPrimIn; i++)
            {
                uint newMat = (uint)primitiveMatIn[i];
                newMat &= ~DrawPrimitive.TypeMask;
                if (newMat != prevMaterial)
                {
                    numPrimOut++;
                    if (primitivesOut != null)
                    {
                        primitivesOut[numPrimOut].Start = (ushort)numIndicesOut;
                        primitivesOut[numPrimOut].NumElements = 0;
                        primitivesOut[numPrimOut].MaterialIndex = newMat | DrawPrimitive.Triangles;
                    }
                    prevMaterial = (int)newMat;
                }
                ushort start = primitiveDataIn[i * 2];
                ushort numElements = primitiveDataIn[i * 2 + 1];

                // gonna depend on what kind of primitive it is...
                if ((primitiveMatIn[i] & DrawPrimitive.TypeMask) == DrawPrimitive.Triangles)
                {
                    for (int j = 0; j < numElements; j += 3)
                    {
                        if (indicesOut != null)
                        {
                            indicesOut[numIndicesOut + 0] = indicesIn[start + j + 0];
                            indicesOut[numIndicesOut + 1] = indicesIn[start + j + 1];
                            indicesOut[numIndicesOut + 2] = indicesIn[start + j + 2];
                        }
                        if (primitivesOut != null)
                            primitivesOut[numPrimOut].NumElements += 3;
                        numIndicesOut += 3;
                    }
                }
                else
                {
                    short[] idx = { indicesIn[start + 0], 0, indicesIn[start + 1] };
                    uint nextIdx = 1;
                    for (int j = 2; j < numElements; j++)
                    {
                        idx[nextIdx] = idx[2];
                        nextIdx = nextIdx ^ 1;
                        idx[2] = indicesIn[start + j];
                        if (idx[0] == idx[1] || idx[1] == idx[2] || idx[2] == idx[0])
                            continue;
                        if (indicesOut != null)
                        {
                            indicesOut[numIndicesOut + 0] = idx[0];
                            indicesOut[numIndicesOut + 1] = idx[1];
                            indicesOut[numIndicesOut + 2] = idx[2];
                        }
                        if (primitivesOut != null)
                            primitivesOut[numPrimOut].NumElements += 3;
                        numIndicesOut += 3;
                    }
                }
            }
            numPrimOut++;
        }



        // utility routines for converting draw primitive format from list of strips to one big uber strip
        void _ConvertToSingleStrip(ushort[] primitiveDataIn, int[] primitiveMatIn, short[] indicesIn, int numPrimIn,
                out int numPrimOut, out int numIndicesOut,
                DrawPrimitive[] primitivesOut, short[] indicesOut)
        {
            numPrimOut = -1;
            numIndicesOut = 0;

            int prevMaterial = -99999;
            int curDrawOut = 0;
            for (int i = 0; i < numPrimIn; i++)
            {
                int newMat = primitiveMatIn[i];
                if (newMat != prevMaterial)
                {
                    numPrimOut++;
                    if (primitivesOut != null)
                    {
                        primitivesOut[numPrimOut].Start = (ushort)numIndicesOut;
                        primitivesOut[numPrimOut].NumElements = 0;
                        primitivesOut[numPrimOut].MaterialIndex = (uint)newMat;
                    }
                    curDrawOut = 0;
                    prevMaterial = newMat;
                }
                ushort start = primitiveDataIn[i * 2];
                ushort numElements = primitiveDataIn[i * 2 + 1];

                // gonna depend on what kind of primitive it is...
                // from above we know it's the same kind as the one we're building...
                if ((primitiveMatIn[i] & DrawPrimitive.TypeMask) == DrawPrimitive.Triangles)
                {
                    // triangles primitive...add to it
                    for (int j = 0; j < numElements; j += 3)
                    {
                        if (indicesOut != null)
                        {
                            indicesOut[numIndicesOut + 0] = indicesIn[start + j + 0];
                            indicesOut[numIndicesOut + 1] = indicesIn[start + j + 1];
                            indicesOut[numIndicesOut + 2] = indicesIn[start + j + 2];
                        }
                        if (primitivesOut != null)
                            primitivesOut[numPrimOut].NumElements += 3;
                        numIndicesOut += 3;
                    }
                }
                else
                {
                    // strip primitive...add to it
                    if (indicesOut != null)
                    {
                        if ((curDrawOut & 1) != 0)
                        {
                            indicesOut[numIndicesOut + 0] = indicesOut[numIndicesOut - 1];
                            indicesOut[numIndicesOut + 1] = indicesOut[numIndicesOut - 1];
                            indicesOut[numIndicesOut + 2] = indicesIn[start];
                            for (int j = 0; j < numElements; j++)
                                indicesOut[numIndicesOut + 3 + j] = indicesIn[start + j];
                        }
                        else if (curDrawOut != 0)
                        {
                            indicesOut[numIndicesOut + 0] = indicesOut[numIndicesOut - 1];
                            indicesOut[numIndicesOut + 1] = indicesIn[start];
                            for (int j = 0; j < numElements; j++)
                                indicesOut[numIndicesOut + 2 + j] = indicesIn[start + j];
                        }
                        else
                        {
                            for (int j = 0; j < numElements; j++)
                                indicesOut[numIndicesOut + j] = indicesIn[start + j];
                        }
                    }
                    ushort added = numElements;
                    added += (ushort)((curDrawOut != 0) ? (((curDrawOut & 1) != 0) ? 3 : 2) : 0);
                    if (primitivesOut != null)
                        primitivesOut[numPrimOut].NumElements += added;
                    numIndicesOut += added;
                    curDrawOut += added;
                }
            }
            numPrimOut++;
        }



        // utility routines for converting draw primitive format from list of strips...list of strips
        void _LeaveAsMultipleStrips(ushort[] primitiveDataIn, int[] primitiveMatIn, short[] indicesIn, int numPrimIn,
                out int numPrimOut, out int numIndicesOut,
                DrawPrimitive[] primitivesOut, short[] indicesOut)
        {
            numPrimOut = -1;
            numIndicesOut = 0;

            int prevMaterial = -99999;
            for (int i = 0; i < numPrimIn; i++)
            {
                int newMat = primitiveMatIn[i];

                ushort start = primitiveDataIn[i * 2];
                ushort numElements = primitiveDataIn[i * 2 + 1];

                numPrimOut++;
                if (primitivesOut != null)
                {
                    primitivesOut[numPrimOut].Start = (ushort)numIndicesOut;
                    primitivesOut[numPrimOut].NumElements = 0;
                    primitivesOut[numPrimOut].MaterialIndex = (uint)newMat;
                }
                prevMaterial = newMat;

                // gonna depend on what kind of primitive it is...
                // from above we know it's the same kind as the one we're building...
                if ((primitiveMatIn[i] & DrawPrimitive.TypeMask) == DrawPrimitive.Triangles)
                {
                    // triangles primitive...add to it
                    for (int j = 0; j < numElements; j += 3)
                    {
                        if (indicesOut != null)
                        {
                            indicesOut[numIndicesOut + 0] = indicesIn[start + j + 0];
                            indicesOut[numIndicesOut + 1] = indicesIn[start + j + 1];
                            indicesOut[numIndicesOut + 2] = indicesIn[start + j + 2];
                        }
                        if (primitivesOut != null)
                            primitivesOut[numPrimOut].NumElements += 3;
                        numIndicesOut += 3;
                    }
                }
                else
                {
                    // strip primitive...add to it
                    if (indicesOut != null)
                    {
                        for (int j = 0; j < numElements; j++)
                            indicesOut[numIndicesOut + j] = indicesIn[start + j];
                    }
                    if (primitivesOut != null)
                        primitivesOut[numPrimOut].NumElements = numElements;
                    numIndicesOut += numElements;
                }
            }
            numPrimOut++;
        }

        #endregion


        #region Private, protected, internal fields

        Shape _shape;
        bool _useTriangles;
        bool _useOneStrip = true;

        #endregion
    }
}
