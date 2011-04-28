//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GarageGames.Torque.Core;
using GarageGames.Torque.MathUtil;



namespace GarageGames.Torque.TS
{
    /// <summary>
    /// Nodes hold the transforms in the shape's tree. They are the bones of the skeleton.
    /// </summary>
    public struct Node
    {
        #region Public properties, operators, constants, and enums

        /// <summary>
        /// Index of this node's name in the name array on the shape.
        /// </summary>
        public int NameIndex;



        /// <summary>
        /// Index of this node's parent in the node array on the shape.
        /// </summary>
        public int ParentIndex;



        /// <summary>
        /// Index of the first object attached to this node in the object array on the shape.
        /// </summary>
        public int FirstObject;



        /// <summary>
        /// Index of the first child node attached to this node in the node array on the shape.
        /// </summary>
        public int FirstChild;



        /// <summary>
        /// Index of this node's sibling in the node array on the shape.
        /// </summary>
        public int NextSibling;

        #endregion
    }



    /// <summary>
    /// Objects hold renderable items and are rooted to a particular node.
    /// </summary>
    public struct Object
    {
        #region Public properties, operators, constants, and enums

        /// <summary>
        /// Index of this node's name in the name array on the shape.
        /// </summary>
        public int NameIndex;



        /// <summary>
        /// Number of meshes contained in this object.
        /// </summary>
        public int MeshCount;



        /// <summary>
        /// Index of the first mesh attached to this object in the mesh array on the shape.
        /// </summary>
        public int FirstMesh;



        /// <summary>
        /// Index of the node this object is attached to in the node array on the shape.
        /// </summary>
        public int NodeIndex;



        /// <summary>
        /// Index of the next object attached to the same parent node as this object in the object array on the shape.
        /// </summary>
        public int NextSibling;

        #endregion
    }



    /// <summary>
    /// Stores information about an image file list that can be used to automatically change the texture
    /// on a material over time.
    /// </summary>
    public struct IflMaterial
    {
        #region Public properties, operators, constants, and enums

        /// <summary>
        /// Index of the materials filename in the name array on the shape.
        /// </summary>
        public int NameIndex;



        /// <summary>
        /// Index of the material in the materials array on the shape. This is the current material that is being used.
        /// </summary>
        public int MaterialSlot;



        /// <summary>
        /// Index of the first material associated with this image file list in the materials array on the shape.
        /// </summary>
        public int FirstFrame;



        /// <summary>
        /// Index of the time the first frame will be displayed in the off time array on the shape.
        /// </summary>
        public int FirstFrameOffTimeIndex;



        /// <summary>
        /// The number of frames (different materials) associated with this image file list.
        /// </summary>
        public int FrameCount;

        #endregion
    }



    /// <summary>
    /// Animation data. Contains keyframes and what aspects of the shape those keyframes control.
    /// </summary>
    public class Sequence
    {
        #region Public properties, operators, constants, and enums

        /// <summary>
        /// Index of the sequence's name in the name array on the shape.
        /// </summary>
        public int NameIndex;



        /// <summary>
        /// Number of keyframes on this sequence.
        /// </summary>
        public int KeyframeCount;



        /// <summary>
        /// The amount of time (in seconds) that one cycle of the sequence takes.
        /// </summary>
        public float Duration;



        /// <summary>
        /// Index in the shape's rotations array of the first rotation of the first node on this sequence.
        /// </summary>
        public int BaseRotation;



        /// <summary>
        /// Index in the shape's translation array of the first translation of the first node on this sequence.
        /// </summary>
        public int BaseTranslation;



        /// <summary>
        /// Index in the shape's scale array of the first scale of the first node on this sequence.
        /// </summary>
        public int BaseScale;



        /// <summary>
        /// Index in the shape's object state array of the first object state for this sequence.
        /// </summary>
        public int BaseObjectState;



        /// <summary>
        /// Index in the shape's various ground transform arrays of the first ground frame for this sequence.
        /// </summary>
        public int FirstGroundFrame;



        /// <summary>
        /// Number of ground frames the sequence has.
        /// </summary>
        public int GroundFrameCount;



        /// <summary>
        /// Index in the shape's trigger array of the first trigger on the sequence.
        /// </summary>
        public int FirstTrigger;



        /// <summary>
        /// Number of triggers the sequence has.
        /// </summary>
        public int TriggerCount;



        /// <summary>
        /// The time at which Ifl materials start when this sequence is playing.
        /// </summary>
        public float ToolBegin;



        /// <summary>
        /// Whether or not rotation is animated for each node in the sequence.
        /// </summary>
        public BitVector DoesRotationMatter;



        /// <summary>
        /// Whether or not translation is animated for each node in the sequence.
        /// </summary>
        public BitVector DoesTranslationMatter;



        /// <summary>
        /// Whether or not scale is animated for each node in the sequence.
        /// </summary>
        public BitVector DoesScaleMatter;



        /// <summary>
        /// Whether or not visibility is animated for each node in the sequence.
        /// </summary>
        public BitVector DoesVisibilityMatter;



        /// <summary>
        /// Whether or not a frame change matters for each node in the sequence.
        /// </summary>
        public BitVector DoesFrameMatter;



        /// <summary>
        /// Whether or not a material frame change matters for each node in the sequence.
        /// </summary>
        public BitVector DoesMaterialFrameMatter;



        /// <summary>
        /// Whether or not each node has an Ifl material associated with it.
        /// </summary>
        public BitVector DoesIflMatter;



        /// <summary>
        /// Determines the amount to weight this sequence's node information when blending with
        /// another sequence.
        /// </summary>
        public int Priority;



        /// <summary>
        /// Stores information about what this sequence animates.
        /// </summary>
        public SequenceFlags Flags;



        /// <summary>
        /// The different animation types that are not currently updated.
        /// </summary>
        public DirtyFlags DirtyFlags;

        #endregion


        #region Public methods

        /// <summary>
        /// Tests whether or not this sequence uses a particular type of animation.
        /// </summary>
        /// <param name="flags">The flags to test</param>
        /// <returns>True if the flag is set, false otherwise.</returns>
        public bool TestFlags(SequenceFlags flags)
        {
            return (Flags & flags) != 0;
        }



        /// <summary>
        /// Tests whether or any scaling is animated by this sequence.
        /// </summary>
        /// <returns>True if it is animated, false otherwise.</returns>
        public bool IsScaleAnimated()
        {
            return TestFlags(SequenceFlags.AnyScale);
        }



        /// <summary>
        /// Tests whether or not uniform scale is animated by this sequence.
        /// </summary>
        /// <returns>True if it is animated, false otherwise.</returns>
        public bool IsUniformScaleAnimated()
        {
            return TestFlags(SequenceFlags.UniformScale);
        }



        /// <summary>
        /// Tests whether or not aligned scale is animated by this sequence.
        /// </summary>
        /// <returns>True if it is animated, false otherwise.</returns>
        public bool IsAlignedScaleAnimated()
        {
            return TestFlags(SequenceFlags.AlignedScale);
        }



        /// <summary>
        /// Tests whether or not arbitrary scale is animated by this sequence.
        /// </summary>
        /// <returns>True if it is animated, false otherwise.</returns>
        public bool IsArbitraryScaleAnimated()
        {
            return TestFlags(SequenceFlags.ArbitraryScale);
        }



        /// <summary>
        /// Tests whether or not this sequences can blend with other sequences.
        /// </summary>
        /// <returns>True if it can blend, false otherwise.</returns>
        public bool IsBlend()
        {
            return TestFlags(SequenceFlags.Blend);
        }



        /// <summary>
        /// Tests whether or not this sequence repeats
        /// </summary>
        /// <returns>True if it repeats, false otherwise.</returns>
        public bool IsCyclic()
        {
            return TestFlags(SequenceFlags.Cyclic);
        }



        /// <summary>
        /// Tests whether or not paths should be generated between keyframes to determine when
        /// triggers should be fired.
        /// </summary>
        /// <returns>True if paths are generated, false otherwise.</returns>
        public bool MakePath()
        {
            return TestFlags(SequenceFlags.MakePath);
        }

        #endregion
    }



    /// <summary>
    /// Stores state information about an Object.
    /// </summary>
    public struct ObjectState
    {

        #region Public properties, operators, constants, and enums

        /// <summary>
        /// Current visibility of the object.
        /// </summary>
        public float Visibility;



        /// <summary>
        /// Current playback frame of the object.
        /// </summary>
        public int FrameIndex;



        /// <summary>
        /// Current playback frame of the material on the object.
        /// </summary>
        public int MaterialFrameIndex;

        #endregion
    }



    /// <summary>
    /// When time on a sequence advances past a certain point, a trigger takes effect and changes
    /// one of the state variables to on or off (State variables found in TSShapeInstance::_triggerStates).
    /// </summary>
    public struct Trigger
    {
        /// <summary>
        /// Stores information about the state of a trigger in the _triggerStates variable on TSShapeInstance.
        /// </summary>
        public enum TriggerStates
        {
            /// <summary>
            /// Determines whether or not the trigger is active.
            /// </summary>
            StateOn = 1 << 31,         // BIT(31),

            /// <summary>
            /// Determines whether or not the trigger's state should be reversed if the sequence is reversed.
            /// </summary>
            InvertOnReverse = 1 << 30, // BIT(30),

            /// <summary>
            /// Mask for trigger states.
            /// </summary>
            StateMask = (1 << 30) - 1    // BIT(30) - 1
        }


        #region Public properties, operators, constants, and enums

        /// <summary>
        /// The current state of the trigger. The next time the trigger is fired, this will be reversed.
        /// </summary>
        public int State;



        /// <summary>
        /// The position in the sequence that this trigger is fired.
        /// </summary>
        public float Pos;

        #endregion
    }



    /// <summary>
    /// Details are used for level of detail selection when rendering an object. The projected size
    /// of a shape determines which object detail number and which sub shape number to use. The size field
    /// determines the minimum pixel size to use with this detail level (ts chooses the detail with the smallest
    /// size that isn't smaller than the projected shape size).
    /// </summary>
    public struct Detail
    {

        #region Public properties, operators, constants, and enums

        /// <summary>
        /// Index in the name array on the shape of this detail levels name.
        /// </summary>
        public int NameIndex;



        /// <summary>
        /// The sub shape number to use with this detail level.
        /// </summary>
        public int SubShapeNumber;



        /// <summary>
        /// The object associated with this detail level.
        /// </summary>
        public int ObjectDetailNumber;



        /// <summary>
        /// The projected size of a shape at which this detail level will be used.
        /// </summary>
        public float PixelSize;



        /// <summary>
        /// The average amount of error when this detail level is used versus when the full detail is used.
        /// </summary>
        public float AverageError;



        /// <summary>
        /// The maximum amount of error when this detail level is used versus when the full detail is used.
        /// </summary>
        public float MaxError;



        /// <summary>
        /// The number of polygons in this detail level.
        /// </summary>
        public int PolyCount;

        #endregion
    };



    /// <summary>
    /// DTS shape implementation. This class stores all of the information about a shape including bone
    /// information, materials, animation sequences, and detail levels.
    /// Features not copied over from C++ code:
    /// - Decals (deprecated)
    /// - collision meshes
    /// - alpha in/out (handled by material system)
    /// - billboard details
    /// - skip detail on load code
    /// - don't use encoded normals (never used them anyway)
    /// - mesh merge code (probably won't ever carry over -- if anything replace with something better)
    /// - mesh computeBounds (bounds stored on mesh, computed bounds old code)
    /// - sorted meshes don't support frame based animation anymore (first frame only is used)
    /// - Masking animation of _pos/rot on particular nodes (hands off and callback nodes still allowed)
    /// </summary>
    public class Shape
    {

        #region Static methods, fields, constructors

        /// <summary>
        /// Temporary storage for the node transforms of the shape that is currently being
        /// operated on.
        /// </summary>
        public static Matrix[] Transforms;



        /// <summary>
        /// Path on disk of the file that this shape came from. This is used to find any
        /// name mapped materials for the shape that is currently being operated on.
        /// </summary>
        public static String CurrentFilePath;

        /// <summary>
        /// Version of the file that the currently loading shape was read from. This is only
        /// valid when the shape is being read.
        /// </summary>
        internal static int ReadVersion = -1;



        /// <summary>
        /// The version of the DTS format that is supported.
        /// </summary>
        public const int WriteVersion = 26;

        #endregion


        #region Public properties, operators, constants, and enums

        /// <summary>
        /// Number of detail levels that this shape has.
        /// </summary>
        public int DetailLevelCount
        {
            get { return Details.Length; }
        }



        /// <summary>
        /// Array of all the nodes (including bones) in the shape.
        /// </summary>
        public Node[] Nodes;



        /// <summary>
        /// Array of all the Objects in the shape.
        /// </summary>
        public Object[] Objects;



        /// <summary>
        /// Array of all the Ifl materials used by the shape.
        /// </summary>
        public IflMaterial[] IflMaterials;



        /// <summary>
        /// Array of all the object states for the shape.
        /// </summary>
        public ObjectState[] ObjectStates;





        /// <summary>
        /// Array of indices of the first node for each sub shape.
        /// </summary>
        public int[] SubShapeFirstNode;

        /// <summary>
        /// Array of indices of the first object for each sub shape.
        /// </summary>
        public int[] SubShapeFirstObject;



        /// <summary>
        /// Array of the number of nodes in each sub shape.
        /// </summary>
        public int[] SubShapeNodeCount;



        /// <summary>
        /// Array of the number of objects in each sub shape.
        /// </summary>
        public int[] SubShapeObjectCount;



        /// <summary>
        /// Array of the index of the first translucent object in each sub shape.
        /// </summary>
        public int[] SubShapeFirstTranslucentObject;



        /// <summary>
        /// Array of all the detail levels for this shape.
        /// </summary>
        public Detail[] Details;



        /// <summary>
        /// The rotations of each node by default (i.e if no sequence is playing on the shape).
        /// </summary>
        public Quat16[] DefaultRotations;



        /// <summary>
        /// The translation to use for each node by default (i.e if no sequence is playing on the shape).
        /// </summary>
        public Vector3[] DefaultTranslations;



        /// <summary>
        /// Array of all the meshes in the shape.
        /// </summary>
        public Mesh[] Meshes;



        /// <summary>
        /// Array of all the sequences loaded for the shape.
        /// </summary>
        public Sequence[] Sequences;



        /// <summary>
        /// Array of all the possible rotations for each node in each sequence.
        /// </summary>
        public Quat16[] NodeRotations;



        /// <summary>
        /// Array of all the possible rotations for each node in each sequence.
        /// </summary>
        public Vector3[] NodeTranslations;



        /// <summary>
        /// Array of all the possible rotations for each node in each sequence.
        /// </summary>
        public float[] NodeUniformScales;



        /// <summary>
        /// Array of all the possible rotations for each node in each sequence.
        /// </summary>
        public Vector3[] NodeAlignedScales;



        /// <summary>
        /// Array of all the possible rotations for each node in each sequence.
        /// </summary>
        public Quat16[] NodeArbitraryScaleRotations;



        /// <summary>
        /// Array of all the possible rotations for each node in each sequence.
        /// </summary>
        public Vector3[] NodeArbitraryScaleFactors;



        /// <summary>
        /// Array of ground rotations. Not currently used.
        /// </summary>
        public Quat16[] GroundRotations;



        /// <summary>
        /// Array of ground translations. Not currently used.
        /// </summary>
        public Vector3[] GroundTranslations;



        /// <summary>
        /// Array of triggers for each sequence in the shape.
        /// </summary>
        public Trigger[] Triggers;



        /// <summary>
        /// Array of time spent per frame for each ifl material.
        /// </summary>
        public float[] IflFrameOffTimes;



        /// <summary>
        /// Array of names for everything associated with the shape - nodes, sequences, detail levels, etc.
        /// </summary>
        public String[] Names;



        /// <summary>
        /// Array of all materials used by this shape.
        /// </summary>
        public Material[] MaterialList;



        /// <summary>
        /// The radius of the bounding sphere of the shape.
        /// </summary>
        public float Radius;



        /// <summary>
        /// The radius of the bounding tube of the shape.
        /// </summary>
        public float TubeRadius;



        /// <summary>
        /// The center point of the shape.
        /// </summary>
        public Vector3 Center;



        /// <summary>
        /// The bounding box of the shape.
        /// </summary>
        public Box3F Bounds;



        /// <summary>
        /// The smallest size at which this shape is visible.
        /// </summary>
        public float SmallestVisibleSize;



        /// <summary>
        /// The smallest detail level at which this shape is visible.
        /// </summary>
        public int SmallestVisibleDL;



        /// <summary>
        /// Flags about the sequence that is currently playing on the shape.
        /// </summary>
        public SequenceFlags Flags;



        /// <summary>
        /// The path of the file this shape was loaded from.
        /// </summary>
        public String FilePath;



        /// <summary>
        /// Whether this shapes buffers have been initialized yet.
        /// </summary>
        public bool IsVBIBInitialized = false;

        #endregion


        #region Public methods

        /// <summary>
        /// Initializes the shape by computing various static information based on the various
        /// components (sequences, detail levels, etc) that have been loaded.
        /// </summary>
        public void Initialize()
        {
            int numSubShapes = SubShapeFirstNode.Length;
            Assert.Fatal(numSubShapes == SubShapeFirstObject.Length, "TSShape.Initialize - Invalid sub shape count.");

            int i, j;

            // Set up parent/child relationships on nodes and objects
            for (i = 0; i < Nodes.Length; i++)
                Nodes[i].FirstObject = Nodes[i].FirstChild = Nodes[i].NextSibling = -1;

            for (i = 0; i < Nodes.Length; i++)
            {
                int parentIndex = Nodes[i].ParentIndex;
                if (parentIndex >= 0)
                {
                    if (Nodes[parentIndex].FirstChild < 0)
                        Nodes[parentIndex].FirstChild = i;
                    else
                    {
                        int child = Nodes[parentIndex].FirstChild;
                        while (Nodes[child].NextSibling >= 0)
                            child = Nodes[child].NextSibling;
                        Nodes[child].NextSibling = i;
                    }
                }
            }

            for (i = 0; i < Objects.Length; i++)
            {
                Objects[i].NextSibling = -1;

                int nodeIndex = Objects[i].NodeIndex;
                if (nodeIndex >= 0)
                {
                    if (Nodes[nodeIndex].FirstObject < 0)
                        Nodes[nodeIndex].FirstObject = i;
                    else
                    {
                        int objectIndex = Nodes[nodeIndex].FirstObject;
                        while (Objects[objectIndex].NextSibling >= 0)
                            objectIndex = Objects[objectIndex].NextSibling;
                        Objects[objectIndex].NextSibling = i;
                    }
                }
            }

            Flags = 0;
            for (i = 0; i < Sequences.Length; i++)
            {
                if (!Sequences[i].IsScaleAnimated())
                    continue;

                SequenceFlags curVal = Flags & SequenceFlags.AnyScale;
                SequenceFlags newVal = Sequences[i].Flags & SequenceFlags.AnyScale;
                Flags &= ~(SequenceFlags.AnyScale);
                Flags |= (SequenceFlags)Math.Max((uint)curVal, (uint)newVal); // take the larger value (can only convert upwards)
            }

            for (i = 0; i < Details.Length; i++)
            {
                int count = 0;
                int ss = Details[i].SubShapeNumber;
                int od = Details[i].ObjectDetailNumber;
                if (ss < 0)
                {
                    // billboard detail...
                    // Note: not implemented, but still need to skip this if shape has one
                    count += 2;
                    continue;
                }
                int start = SubShapeFirstObject[ss];
                int end = start + SubShapeObjectCount[ss];
                for (j = start; j < end; j++)
                {
                    if (od < Objects[j].MeshCount)
                    {
                        Mesh mesh = Meshes[Objects[j].FirstMesh + od];
                        count += mesh != null ? mesh.GetNumPolys() : 0;
                    }
                }
                Details[i].PolyCount = count;
            }

            InitializeMaterialList();
        }



        /// <summary>
        /// Creates the vertex and index buffers for each mesh in the shape. It is safe to call
        /// this multiple times, the buffers will not be recreated each time.
        /// </summary>
        /// <param name="d3d"></param>
        public void CreateVBIB(GraphicsDevice d3d)
        {
            if (IsVBIBInitialized)
                return;

            foreach (Mesh mesh in Meshes)
            {
                if (mesh != null)
                    mesh.CreateVBIB();
            }

            IsVBIBInitialized = true;
        }



        /// <summary>
        /// Initializes the translucency settings for each object and sub shape based on
        /// the materials assigned to each mesh.
        /// </summary>
        public void InitializeMaterialList()
        {
            int i, j, k;
            int numSubShapes = SubShapeFirstObject.Length;

            // for each subshape, find the first translucent object
            // also, while we're at it, Set mHasTranslucency
            for (int ss = 0; ss < numSubShapes; ss++)
            {
                int start = SubShapeFirstObject[ss];
                int end = SubShapeObjectCount[ss];
                SubShapeFirstTranslucentObject[ss] = end;
                for (i = start; i < end; i++)
                {
                    // check to see if this object has translucency
                    for (j = 0; j < Objects[i].MeshCount; j++)
                    {
                        Mesh mesh = Meshes[Objects[i].FirstMesh + j];
                        if (mesh == null)
                            continue;
                        for (k = 0; k < mesh._primitives.Length; k++)
                        {
                            if ((mesh._primitives[k].MaterialIndex & DrawPrimitive.NoMaterial) != 0)
                                continue;
                            MaterialFlags flags = MaterialList[mesh._primitives[k].MaterialIndex & DrawPrimitive.MaterialMask].Flags;
                            if ((flags & MaterialFlags.Translucent) != 0)
                            {
                                flags |= (MaterialFlags)SequenceFlags.HasTranslucency; // cafTODO: converting from _sequence to material _flags? fix that.
                                SubShapeFirstTranslucentObject[ss] = i;
                                break;
                            }
                        }
                        if (k != mesh._primitives.Length)
                            break;
                    }
                    if (j != Objects[i].MeshCount)
                        break;
                }
                if (i != end)
                    break;
            }
        }



        /// <summary>
        /// Finds the index in the name array of a particular name.
        /// </summary>
        /// <param name="lookup">The name to lookup.</param>
        /// <returns>The index in the name array.</returns>
        public int FindName(String lookup)
        {
            for (int i = 0; i < Names.Length; i++)
            {
                if (String.Equals(Names[i], lookup, StringComparison.OrdinalIgnoreCase))
                    return i;
            }

            return -1;
        }



        /// <summary>
        /// Returns the name from the name array at the specified index.
        /// </summary>
        /// <param name="nameIdx">The index of the name to lookup.</param>
        /// <returns>The name.</returns>
        public String GetName(int nameIdx)
        {
            return Names[nameIdx];
        }



        /// <summary>
        /// Looks up a node based on its name index.
        /// </summary>
        /// <param name="nameIndex">The index of the node in the name array.</param>
        /// <returns>The index of the node in the node array.</returns>
        public int FindNode(int nameIndex)
        {
            for (int i = 0; i < Nodes.Length; i++)
            {
                if (Nodes[i].NameIndex == nameIndex)
                    return i;
            }

            return -1;
        }



        /// <summary>
        /// Looks up an object based on its name index.
        /// </summary>
        /// <param name="nameIndex">The index of the object in the name array.</param>
        /// <returns>The index of the object in the object array.</returns>
        public int FindObject(int nameIndex)
        {
            for (int i = 0; i < Objects.Length; i++)
            {
                if (Objects[i].NameIndex == nameIndex)
                    return i;
            }

            return -1;
        }



        /// <summary>
        /// Looks up an Ifl material based on its name index.
        /// </summary>
        /// <param name="nameIndex">The index of the Ifl material in the name array.</param>
        /// <returns>The index of the Ifl material in the Ifl material array.</returns>
        public int FindIflMaterial(int nameIndex)
        {
            for (int i = 0; i < IflMaterials.Length; i++)
            {
                if (IflMaterials[i].NameIndex == nameIndex)
                    return i;
            }

            return -1;
        }



        /// <summary>
        /// Looks up a detail level based on its name index.
        /// </summary>
        /// <param name="nameIndex">The index of the detail level in the name array.</param>
        /// <returns>The index of the detail level in the details array.</returns>
        public int FindDetail(int nameIndex)
        {
            for (int i = 0; i < Details.Length; i++)
            {
                if (Details[i].NameIndex == nameIndex)
                    return i;
            }

            return -1;
        }



        /// <summary>
        /// Looks up a sequence based on its name index.
        /// </summary>
        /// <param name="nameIndex">The index of the sequence in the name array.</param>
        /// <returns>The index of the sequence in the sequence array.</returns>
        public int FindSequence(int nameIndex)
        {
            for (int i = 0; i < Sequences.Length; i++)
            {
                if (Sequences[i].NameIndex == nameIndex)
                    return i;
            }

            return -1;
        }



        /// <summary>
        /// Returns the name of the sequence at the specified index.
        /// </summary>
        /// <param name="index">The index of the sequence in the sequence array.</param>
        /// <returns>The name of the sequence.</returns>
        public string GetSequenceName(int index)
        {
            return GetName(Sequences[index].NameIndex);
        }

        /// <summary>
        /// Looks up a node by name.
        /// </summary>
        /// <param name="name">The name of the node.</param>
        /// <returns>The index of the node in the nodes array.</returns>
        public int FindNode(String name)
        {
            return FindNode(FindName(name));
        }



        /// <summary>
        /// Looks up an object by name.
        /// </summary>
        /// <param name="name">The name of the object.</param>
        /// <returns>The index of the object in the objects array.</returns>
        public int FindObject(String name)
        {
            return FindObject(FindName(name));
        }

        /// <summary>
        /// Looks up an Ifl material by name.
        /// </summary>
        /// <param name="name">The name of the Ifl material.</param>
        /// <returns>The index of the Ifl material in the Ifl material array.</returns>
        public int FindIflMaterial(String name)
        {
            return FindIflMaterial(FindName(name));
        }



        /// <summary>
        /// Looks up a detail level by name.
        /// </summary>
        /// <param name="name">The name of the detail level.</param>
        /// <returns>The index of the detail level in the details array.</returns>
        public int FindDetail(String name)
        {
            return FindDetail(FindName(name));
        }



        /// <summary>
        /// Looks up a sequence by name.
        /// </summary>
        /// <param name="name">The name of the sequence.</param>
        /// <returns>The index of the sequence in the sequence array.</returns>
        public int FindSequence(String name)
        {
            return FindSequence(FindName(name));
        }



        /// <summary>
        /// Gets the rotation of a sequence at a specified keyframe.
        /// </summary>
        /// <param name="seq">The sequence.</param>
        /// <param name="keyframeNum">The keyframe number</param>
        /// <param name="rotNum">The node index.</param>
        /// <param name="rot">Receives the rotation.</param>
        public void GetRotation(Sequence seq, int keyframeNum, int rotNum, out Quaternion rot)
        {
            NodeRotations[seq.BaseRotation + rotNum * seq.KeyframeCount + keyframeNum].Get(out rot);
        }



        /// <summary>
        /// Gets the translation of a sequence at a specified keyframe.
        /// </summary>
        /// <param name="seq">The sequence.</param>
        /// <param name="keyframeNum">The keyframe number</param>
        /// <param name="tranNum">The node index.</param>
        /// <returns>The translation.</returns>
        public Vector3 GetTranslation(Sequence seq, int keyframeNum, int tranNum)
        {
            return NodeTranslations[seq.BaseTranslation + tranNum * seq.KeyframeCount + keyframeNum];
        }



        /// <summary>
        /// Gets the uniform scale of a sequence at a specified keyframe.
        /// </summary>
        /// <param name="seq">The sequence.</param>
        /// <param name="keyframeNum">The keyframe number</param>
        /// <param name="scaleNum">The node index.</param>
        /// <returns>The scale.</returns>
        public float GetUniformScale(Sequence seq, int keyframeNum, int scaleNum)
        {
            return NodeUniformScales[seq.BaseScale + scaleNum * seq.KeyframeCount + keyframeNum];
        }



        /// <summary>
        /// Gets the aligned scale of a sequence at a specified keyframe.
        /// </summary>
        /// <param name="seq">The sequence.</param>
        /// <param name="keyframeNum">The keyframe number</param>
        /// <param name="scaleNum">The node index.</param>
        /// <returns>The scale.</returns>
        public Vector3 GetAlignedScale(Sequence seq, int keyframeNum, int scaleNum)
        {
            return NodeAlignedScales[seq.BaseScale + scaleNum * seq.KeyframeCount + keyframeNum];
        }



        /// <summary>
        /// Gets the arbitrary scale of a sequence at a specified keyframe.
        /// </summary>
        /// <param name="seq">The sequence.</param>
        /// <param name="keyframeNum">The keyframe number</param>
        /// <param name="scaleNum">The node index.</param>
        /// <param name="scale">Receives the scale.</param>
        public void GetArbitraryScale(Sequence seq, int keyframeNum, int scaleNum, out ArbitraryScale scale)
        {
            NodeArbitraryScaleRotations[seq.BaseScale + scaleNum * seq.KeyframeCount + keyframeNum].Get(out scale.Rotate);
            scale.Scale = NodeArbitraryScaleFactors[seq.BaseScale + scaleNum * seq.KeyframeCount + keyframeNum];
        }



        /// <summary>
        /// Gets the object state of a sequence at the specified keyframe.
        /// </summary>
        /// <param name="seq">The sequence.</param>
        /// <param name="keyframeNum">The keyframe number.</param>
        /// <param name="objectNum">The object index.</param>
        /// <returns>The object state.</returns>
        public ObjectState GetObjectState(Sequence seq, int keyframeNum, int objectNum)
        {
            return ObjectStates[seq.BaseObjectState + objectNum * seq.KeyframeCount + keyframeNum];
        }



        /// <summary>
        /// Writes out information about a shape to a stream.
        /// </summary>
        /// <param name="stream">The stream that will receive the shape information.</param>
        public void Dump(TextWriter stream)
        {
            stream.WriteLine();
            stream.WriteLine("Shape Hierarchy:");
            stream.WriteLine();
            stream.WriteLine("   Details:");

            for (int i = 0; i < Details.Length; i++)
            {
                String name = Names[Details[i].NameIndex];
                stream.WriteLine("      {0}, Subtree {1}, objectDetail {2}, size {3}",
                        name, Details[i].SubShapeNumber, Details[i].ObjectDetailNumber, Details[i].PixelSize);
            }

            stream.WriteLine();
            stream.WriteLine("   Subtrees:");

            for (int i = 0; i < SubShapeFirstNode.Length; i++)
            {
                int a = SubShapeFirstNode[i];
                int b = a + SubShapeNodeCount[i];
                stream.WriteLine("      Subtree {0}", i);

                // compute detail sizes for each subshape
                List<float> detailSizes = new List<float>();
                for (int j = 0; j < Details.Length; j++)
                    if (Details[j].SubShapeNumber == i)
                        detailSizes.Add(Details[j].PixelSize);

                for (int j = a; j < b; j++)
                {
                    if (Nodes[j].ParentIndex < 0)
                        _DumpNode(stream, 3, j, detailSizes);
                }
            }

            bool foundSkin = false;
            for (int i = 0; i < Objects.Length; i++)
            {
                if (Objects[i].NodeIndex < 0) // must be a skin
                {
                    if (!foundSkin)
                    {
                        stream.WriteLine();
                        stream.WriteLine("   Skins:");
                        foundSkin = true;
                    }
                    String skinName = "none";
                    int nameIndex = Objects[i].NameIndex;
                    if (nameIndex >= 0)
                        skinName = GetName(nameIndex);
                    stream.Write("      Skin {0} with following details: ", skinName);
                    for (int num = 0; num < Objects[i].MeshCount; num++)
                    {
                        if (Meshes[num] != null)
                            stream.Write("   {0}", Details[num].PixelSize);
                    }
                    stream.WriteLine();
                }
            }
            if (foundSkin)
                stream.WriteLine();

            stream.WriteLine();
            stream.WriteLine("   Sequences:");
            for (int i = 0; i < Sequences.Length; i++)
            {
                String name = "(none)";
                if (Sequences[i].NameIndex != -1)
                    name = GetName(Sequences[i].NameIndex);
                stream.WriteLine("      {0}: {1}", i, name);
            }

            if (MaterialList != null)
            {
                Material[] mats = MaterialList;
                stream.WriteLine();
                stream.WriteLine("   Material list:");
                for (int i = 0; i < mats.Length; i++)
                {
                    MaterialFlags flags = mats[i].Flags;
                    String name = mats[i].Name;
                    stream.Write("   material #{0}: \"{1}\"{2}.", i, name, (flags & (MaterialFlags.S_Wrap | MaterialFlags.T_Wrap)) != 0 ? string.Empty : " not tiled");
                    if ((flags & MaterialFlags.IflMaterial) != 0)
                        stream.Write("  Place holder for ifl.");

                    if ((flags & MaterialFlags.Translucent) != 0)
                        stream.Write("  Translucent.");

                    stream.WriteLine();
                }
            }
        }

        #endregion


        #region Private, protected, internal methods

        /// <summary>
        /// Dumps details about a node to a stream.
        /// </summary>
        /// <param name="stream">The stream to dump to.</param>
        /// <param name="level">The detail level to dump.</param>
        /// <param name="nodeIndex">The node to dump.</param>
        /// <param name="detailSizes">The sizes of each detail level.</param>
        void _DumpNode(TextWriter stream, int level, int nodeIndex, List<float> detailSizes)
        {
            if (nodeIndex < 0)
                return;

            String nodeName = Nodes[nodeIndex].NameIndex < 0 ? "<node>" : GetName(Nodes[nodeIndex].NameIndex);

            for (int i = 0; i < level * 3; i++)
                stream.Write(' ');
            stream.Write(nodeName);

            // find all the objects that hang off this node...
            List<Object> objectList = new List<Object>();
            for (int i = 0; i < Objects.Length; i++)
                if (Objects[i].NodeIndex == nodeIndex)
                    objectList.Add(Objects[i]);

            if (objectList.Count == 0)
                stream.WriteLine();

            int spaceCount = -1;
            foreach (Object o in objectList)
            {
                String name = o.NameIndex != -1 ? GetName(o.NameIndex) : "<object>";

                // more spaces if this is the second object on this node
                if (spaceCount > 0)
                {
                    for (int i = 0; i < spaceCount; i++)
                        stream.Write(' ');
                }

                // Dump object Name
                stream.Write(" --> Object {0} with following details: ", name);

                // Dump object detail levels
                for (int i = 0; i < o.MeshCount; i++)
                {
                    int a = o.FirstMesh;
                    if (Meshes[a + i] != null)
                    {
                        stream.Write(' ');
                        stream.Write(detailSizes[i]);
                    }
                }

                stream.WriteLine();

                // how many spaces should we prepend if we have another object on this node
                if (spaceCount < 0)
                    spaceCount = level * 3 + nodeName.Length;
            }

            // search for children
            for (int i = nodeIndex + 1; i < Nodes.Length; i++)
            {
                if (Nodes[i].ParentIndex == nodeIndex)
                    // this is our child
                    _DumpNode(stream, level + 1, i, detailSizes);
            }
        }

        #endregion
    }
}
