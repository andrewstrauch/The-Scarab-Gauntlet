//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GarageGames.Torque.Core;
using GarageGames.Torque.Materials;
using GarageGames.Torque.SceneGraph;
using GarageGames.Torque.GFX;
using GarageGames.Torque.RenderManager;
using GarageGames.Torque.MathUtil;
using GarageGames.Torque.TS;
using GarageGames.Torque.Sim;
using GarageGames.Torque.Util;
using GarageGames.Torque.XNA;



namespace GarageGames.Torque.T2D
{
    public class T2DShape3D : T2DSceneObject, IAnimatedObject, IDisposable
    {
        #region Constructors

        /// <summary>
        /// Default constructor.
        /// </summary>
        public T2DShape3D()
        {
        }

        #endregion


        #region Public properties, operators, constants, and enums

        /// <summary>
        /// Apply scale to shape when rendering.
        /// </summary>
        public Vector3 ShapeScale
        {
            get { return _shapeScale; }
            set { _shapeScale = value; }
        }



        /// <summary>
        /// This matrix is applied to the shape before it is
        /// positioned, scaled, and rendered.  It is generally
        /// used to set the orientation of the shape.
        /// </summary>
        public Matrix ShapeMatrix
        {
            get { return _shapeMat; }
            set { _shapeMat = value; }
        }



        public Vector3 Rotation2
        {
            get { return Vector3.Zero; }
            set { ShapeMatrix = Matrix.CreateFromYawPitchRoll(value.X, value.Y, value.Z); }
        }



        /// <summary>
        /// This is normaly only used from serialization to 
        /// assign a shape file to this shape object.
        /// </summary>
        public String ShapeFile
        {
            get { return _shapeFile; }
            internal set { SetShape(value); }
        }



        /// <summary>
        /// Shape to render.  This can be set by loading a dts file 
        /// (see SetShape method).
        /// </summary>
        [XmlIgnore]
        public Shape Shape
        {
            get
            {
                if (_shapeInstance != null)
                    return _shapeInstance.GetShape();

                return null;
            }
            set
            {
                UnmountAllShapes();
                _shapeInstance = null;
                _threadInterfaces.Clear();
                _threadNames.Clear();
                _threadAutoAdvance = 0;

                if (value != null)
                    _shapeInstance = new ShapeInstance(value, true);

                // make sure pre-set detail level is properly clamped
                DetailLevel = _detailLevel;
            }
        }



        /// <summary>
        /// T2DShape3D to mount this shape to.  See MountShape 
        /// method.
        /// </summary>
        public T2DShape3D MountedToShape
        {
            get { return _mount; }
        }



        /// <summary>
        /// The level of detail to render.  The value is clamped to
        /// the range of detail levels within the shape.
        /// </summary>
        public int DetailLevel
        {
            set
            {
                if (_shapeInstance != null)
                {
                    int maxLevel = _shapeInstance.GetShape().DetailLevelCount - 1;
                    _detailLevel = value;
                    if (_detailLevel < -1)
                        _detailLevel = -1;
                    if (_detailLevel > maxLevel)
                        _detailLevel = maxLevel;
                }
                else
                    _detailLevel = value;
            }
            get { return _detailLevel; }
        }



        /// <summary>
        /// Returns the number of detail levels in the loaded shape
        /// or zero if no shape is loaded.
        /// </summary>
        public int DetailLevels
        {
            get
            {
                if (_shapeInstance != null)
                    return _shapeInstance.GetShape().DetailLevelCount;
                return 0;
            }
        }



        /// <summary>
        /// This is a fractional value that sets the percentage
        /// between the current detail level and the next.
        /// </summary>
        public float IntraDetailLevel
        {
            set { _intraDetailLevel = MathHelper.Clamp(value, 0, 1); }
            get { return _intraDetailLevel; }
        }



        public override string AutoName
        {
            get
            {
                string autoname = base.AutoName;
                if (_shapeFile != string.Empty)
                    autoname += "_" + _shapeFile;
                return autoname;
            }
        }



        /// <summary>
        /// Assign this delagate to do your own 
        /// animation work like switching sequences and 
        /// controlling threads.
        /// </summary>
        /// <param name="dt">The animation time delta.</param>
        public delegate void OnAnimateShapeDelegate(float dt);



        /// <summary>
        /// </summary>
        public OnAnimateShapeDelegate OnAnimateShape
        {
            set { _OnAnimateShape = value; }
            get { return _OnAnimateShape; }
        }



        /// <summary>
        /// When a trigger event occurs in one of the shapes
        /// trigger channels, this callback is fired.
        /// </summary>
        /// <param name="channel">The channel the trigger occured in.</param>
        public delegate void OnAnimationTriggerDelegate(int channel);



        /// <summary>
        /// </summary>
        public OnAnimationTriggerDelegate OnAnimationTrigger
        {
            set { _OnAnimationTrigger = value; }
            get { return _OnAnimationTrigger; }
        }

        #endregion


        #region Public methods

        /// <summary>
        /// Sets the DTS shape for this object to render.
        /// </summary>
        /// <param name="shapeFile">The path to the DTS file.</param>
        /// <returns>True on successful load or false when failed or same DTS is already loaded.</returns>
        public bool SetShape(String shapeFile)
        {
            if (_shapeFile == shapeFile)
                return false;

            _shapeFile = shapeFile;

            // TODO: Hook in the shape manager here!

            // create the shape
            FileStream fs = null;
            try
            {
                fs = new FileStream(_shapeFile, FileMode.Open);
            }
            catch
            {
                return false;
            }

            ShapeReader reader = new ShapeReader();
            Shape shape = reader.ReadShape(fs);
            fs.Close();
            FileInfo fi = new FileInfo(_shapeFile);
            shape.FilePath = fi.DirectoryName;

            // remove executable directory so that path is relative to it
            shape.FilePath = shape.FilePath.Replace(TorqueEngineComponent.Instance.ExecutableDirectory + @"\", string.Empty);

            Shape = shape;

            return true;
        }



        /// <summary>
        /// Loads a DSQ animation sequence file.
        /// </summary>
        /// <param name="dsqFilePath">The path to the DSQ file.</param>
        /// <param name="sequenceName">An optional name for the sequence or null to use the default.</param>
        public bool LoadSequence(String dsqFilePath, String sequenceName)
        {
            // Ignore it if we have no shape or sequence!
            if (_shapeInstance == null || dsqFilePath == null)
                return false;

            // import the sequence -- cafTODO: error checking...exception handling?
            Shape shape = _shapeInstance.GetShape();

            FileStream fs = null;
            try
            {
                fs = new FileStream(dsqFilePath, FileMode.Open);
            }
            catch
            {
                return false;
            }

            ShapeReader reader = new GarageGames.Torque.TS.ShapeReader(shape);
            shape = reader.ImportSequence(fs, sequenceName);
            fs.Close();
            return true;
        }



        /// <summary>
        /// Adds a new animation thread to the shape and optionally sets 
        /// a sequence to play on it.
        /// </summary>
        /// <param name="threadName">The case sensitive name of the animation thread.</param>
        /// <param name="startSequence">The case sensitive sequence name or null if no sequence should be played.</param>
        /// <param name="autoAdvance">If true the thread will be automatically advanced during the animation update.</param>
        /// <returns>True if the sequence was found and the thread was added.</returns>
        public bool AddThread(String threadName, String startSequence, bool autoAdvance)
        {
            // Ignore it if we have no shape or thread name!
            if (_shapeInstance == null || threadName == null)
                return false;

            // Find the sequence first.
            int sequenceIndex = -1;
            if (startSequence != null)
            {
                sequenceIndex = _shapeInstance.GetShape().FindSequence(startSequence);
                if (sequenceIndex < 0)
                    return false;
            }

            // Create a thread if we don't already have one.
            Thread tsThread = null;
            int ti = _threadNames.IndexOf(threadName);
            if (ti != -1)
                tsThread = _threadInterfaces[ti].Value;
            else
            {
                _threadNames.Add(threadName);
                tsThread = _shapeInstance.AddThread();
                _threadInterfaces.Add(new ValueInPlaceInterface<GarageGames.Torque.TS.Thread>(tsThread));
            }

            // Set the sequence.
            if (sequenceIndex >= 0)
                _shapeInstance.SetSequence(tsThread, sequenceIndex, 0.0f);

            // Toggle the automatic advance bit.
            int threadBit = 1 << (_threadNames.Count - 1);
            if (autoAdvance)
                _threadAutoAdvance |= threadBit;
            else
                _threadAutoAdvance &= ~threadBit;

            return true;
        }



        /// <summary>
        /// Plays a new sequence on the named thread transitioning from the currently
        /// playing sequence by interpolating between frames.
        /// </summary>
        /// <param name="threadName">The case sensitive name of the animation thread.</param>
        /// <param name="sequenceName">The case sensitive sequence name to transition to.</param>
        /// <param name="pos">The 0 to 1 position to start playback within the sequence.</param>
        /// <param name="duration">The positive, non-zero, duration in seconds to take for the transition.</param>
        /// <param name="continuePlay">If true the sequence will continue to play after the transition is complete.</param>
        /// <returns>Returns true if the transition was started.</returns>
        public bool TransitionToSequence(String threadName, String sequenceName, float pos, float duration, bool continuePlay)
        {
            Assert.Fatal(duration > 0.0f, "T2DShape3D::TransitionToSequence: must pass a positive non-zero duration");

            // Ignore it if we have no shape or threads!
            if (_shapeInstance == null || threadName == null || sequenceName == null)
                return false;

            // Find the thread.
            int ti = _threadNames.IndexOf(threadName);
            if (ti == -1)
                return false;
            Thread tsThread = _threadInterfaces[ti].Value;

            // Find the new sequence.
            int sequenceIndex = _shapeInstance.GetShape().FindSequence(sequenceName);
            if (sequenceIndex == -1)
                return false;

            _shapeInstance.TransitionToSequence(tsThread, sequenceIndex, pos, duration, continuePlay);
            return true;
        }



        /// <summary>
        /// Immediately starts a sequence on the named thread. 
        /// </summary>
        /// <param name="threadName">The case sensitive name of the animation thread.</param>
        /// <param name="sequenceName">The case sensitive sequence name to set.</param>
        /// <param name="pos">The 0 to 1 position to start playback within the sequence.</param>
        /// <returns>Returns true if sequence was set.</returns>
        public bool SetSequence(String threadName, String sequenceName, float pos)
        {
            // Ignore it if we have no shape or threads!
            if (_shapeInstance == null || threadName == null || sequenceName == null)
                return false;

            // Find the thread.
            int ti = _threadNames.IndexOf(threadName);
            if (ti == -1)
                return false;
            Thread tsThread = _threadInterfaces[ti].Value;

            // Find the new sequence.
            int sequenceIndex = _shapeInstance.GetShape().FindSequence(sequenceName);
            if (sequenceIndex == -1)
                return false;

            _shapeInstance.SetSequence(tsThread, sequenceIndex, pos);
            return true;
        }



        /// <summary>
        /// Returns true if a transition is in progress on the named thread.
        /// </summary>
        /// <param name="threadName">The case sensitive name of the animation thread.</param>
        /// <returns></returns>
        public bool IsInTransition(String threadName)
        {
            if (_shapeInstance == null ||
                threadName == null)
                return false;

            int ti = _threadNames.IndexOf(threadName);
            if (ti == -1)
                return false;

            Thread tsThread = _threadInterfaces[ti].Value;
            if (tsThread == null)
                return false;

            return tsThread.IsInTransition;
        }



        /// <summary>
        /// Returns true if the sequence is playing on the named thread.
        /// </summary>
        /// <param name="threadName">The case sensitive name of the animation thread.</param>
        /// <param name="sequence">The case sensitive sequence name to set.</param>
        /// <returns></returns>
        public bool IsSequencePlaying(String threadName, String sequence)
        {
            if (_shapeInstance == null ||
                threadName == null ||
                sequence == null)
                return false;

            int ti = _threadNames.IndexOf(threadName);
            if (ti == -1)
                return false;

            Thread tsThread = _threadInterfaces[ti].Value;
            int si = _shapeInstance.GetShape().FindSequence(sequence);
            return _shapeInstance.GetSequence(tsThread) == si;
        }



        /// <summary>
        /// Returns the name of the sequence playing on a thread.
        /// </summary>
        /// <param name="threadName">The case sensitive name of the animation thread.</param>
        /// <returns></returns>
        public string GetSequenceName(String threadName)
        {
            if (_shapeInstance == null ||
                threadName == null)
                return string.Empty;

            int ti = _threadNames.IndexOf(threadName);
            if (ti == -1)
                return string.Empty;

            Thread tsThread = _threadInterfaces[ti].Value;
            int si = _shapeInstance.GetSequence(tsThread);
            if (si == -1)
                return string.Empty;

            return _shapeInstance.GetShape().GetSequenceName(si);
        }



        /// <summary>
        /// Gets the position of the current sequence on a thread.
        /// </summary>
        /// <param name="threadName">The case sensitive name of the animation thread.</param>
        /// <returns>A value between 0 and 1 for the current sequence position.  Zero 
        /// is returned if the shape or thread do not exist.</returns>
        public float GetThreadPosition(String threadName)
        {
            if (_shapeInstance == null ||
                threadName == null)
                return 0;

            int ti = _threadNames.IndexOf(threadName);
            if (ti == -1)
                return 0;

            Thread tsThread = _threadInterfaces[ti].Value;
            if (tsThread == null)
                return 0;

            return tsThread.Position;
        }



        /// <summary>
        /// Scales the speed of animation playback for a thread. 
        /// </summary>
        /// <param name="threadName">The case sensitive name of the animation thread.</param>
        /// <param name="scale">A positive scale factor.</param>
        /// <returns>Returns true if the thread exists and the scale was set.</returns>
        public bool SetThreadTimeScale(String threadName, float scale)
        {
            if (_shapeInstance == null ||
                threadName == null)
                return false;

            int ti = _threadNames.IndexOf(threadName);
            if (ti == -1)
                return false;

            Thread tsThread = _threadInterfaces[ti].Value;
            if (tsThread == null)
                return false;

            tsThread.TimeScale = scale;
            return true;
        }



        /// <summary>
        /// Returns the current time scale for a thread or 0 if the shape or thread has not been created.
        /// </summary>
        /// <param name="threadName">The case sensitive name of the animation thread.</param>
        /// <returns></returns>
        public float GetThreadTimeScale(String threadName)
        {
            if (_shapeInstance == null ||
                threadName == null)
                return 0;

            int ti = _threadNames.IndexOf(threadName);
            if (ti == -1)
                return 0;

            Thread tsThread = _threadInterfaces[ti].Value;
            return tsThread.TimeScale;
        }



        public void UpdateAnimation(float dt)
        {
            if (_shapeInstance == null)
                return;

#if DEBUG
            Profiler.Instance.StartBlock("T2DShape3D.UpdateAnimation");
#endif

            // Let the derived class get a chance to do any
            // custom animation tasks.
            if (OnAnimateShape != null)
                OnAnimateShape(dt);

            // Advance any threads that need it.
            for (int i = 0; i < _threadInterfaces.Count; i++)
            {
                Thread thread = _threadInterfaces[i].Value;
                if (thread != null && (_threadAutoAdvance & (1 << i)) != 0)
                    thread.AdvanceTime(dt);
            }

            // Fire off the trigger events.
            for (int i = 1; i <= 32; i++)
            {
                // TODO: Should be a delegate!
                if (_shapeInstance.GetTriggerState(i, true))
                    OnAnimationTrigger(i);
            }

#if DEBUG
            Profiler.Instance.EndBlock("T2DShape3D.UpdateAnimation");
#endif
        }



        /// <summary>
        /// Returns true if this shape is mounted on to another shape.
        /// </summary>
        /// <returns></returns>
        public bool IsShapeMounted()
        {
            return _mount != null;
        }



        /// <summary>
        /// Mounts another shape object on to this shape at a node.  The mountee
        /// will be unregistered to remove it from the scene graph.
        /// </summary>
        /// <param name="mountee">The shape to mount.</param>
        /// <param name="node">The case sensitive node name at which to mount the shape.</param>
        /// <returns>Returns true if the shape was mounted.</returns>
        public bool MountShape(T2DShape3D mountee, String node)
        {
            // Check some basic issues... we should be initialized.  We should get
            // a mountee and a node.  Make sure the mountee isn't already mounted to
            // something... unmount first!
            if (_shapeInstance == null || mountee == null || node == null ||
                mountee._mount != null)
                return false;

            // Find the node to mount to!
            Shape shape = _shapeInstance.GetShape();
            int index = shape.FindNode(node);
            if (index == -1)
                return false;

            // Setup the mountee... notice we unregister the object
            // here because we no longer want it to appear in the scene
            // graph... we take over rendering ourselves.
            mountee._mount = this;
            mountee._mountIndex = index;
            if (mountee.IsRegistered)
                TorqueObjectDatabase.Instance.Unregister(mountee);

            // Add it to our list of mounted shapes.
            _mounted.Add(mountee);

            return true;
        }



        /// <summary>
        /// Unmounts a shape from this shape.  The unmounted shape is in an unregistered state.
        /// </summary>
        /// <param name="mountee">The shape which was previously mounted to this shape.</param>
        /// <returns>True if the shape was found and unmounted.</returns>
        public bool UnmountShape(T2DShape3D mountee)
        {
            if (mountee._mount != this)
                return false;

            mountee._mount = null;
            mountee._mountIndex = -1;
            _mounted.Remove(mountee);

            return true;
        }



        /// <summary>
        /// Unmounts all shapes mounted to this one.
        /// </summary>
        public void UnmountAllShapes()
        {
            foreach (T2DShape3D mountee in _mounted)
            {
                mountee._mount = null;
                mountee._mountIndex = -1;
            }
            _mounted.Clear();
        }



        public Matrix GetShapeProjectionMatrix(T2DSceneCamera camera)
        {
            if (_shapeInstance == null)
                return Matrix.Identity;

            // Get the shape radius.
            float shapeRadius = _shapeInstance.GetShape().Radius;

            // What is the maximum scale in any axis.
            float maxAxisScale = Math.Max(Math.Max(_shapeScale.X, _shapeScale.Y), _shapeScale.Z);

            // Calculate a good zrange to use for the projection
            //
            // TODO: Need to properly calculate radius including mounted shapes.
            float zRange = shapeRadius * maxAxisScale * 4;

            // Create the projection for 3d.  We use the radius to ensure that
            // our near and far planes have enough range and do not clip on rotation.
            Matrix projection = GFXDevice.Instance.SetOrtho(false,
                -0.5f * camera.Extent.X, 0.5f * camera.Extent.X,
                -0.5f * camera.Extent.Y, 0.5f * camera.Extent.Y,
                -zRange, zRange);

            return projection;
        }



        public override void Render(SceneRenderState srs)
        {
            base.Render(srs);

            if (_shapeInstance == null)
                return;

#if DEBUG
            Profiler.Instance.StartBlock("T2DShape3D.Render");
#endif

            // First flip to account for coordinate system differences between 2D and 3D.
            Matrix world = Matrix.CreateScale(new Vector3(_shapeScale.X, -_shapeScale.Y, _shapeScale.Z));

            // Apply the shape transform.
            world = Matrix.Multiply(world, _shapeMat);

            // Now transform the object to the 2d sceen position.
            Matrix tans = Matrix.CreateTranslation(Position.X, Position.Y, 0);
            world = Matrix.Multiply(world, tans);

            // Set the world matrix for rendering.
            srs.World.Push();
            srs.World.LoadMatrix(world);

            // We override the bin here to force the 3D object to be
            // processed by the T2DRenderManager.
            RenderInstance.RenderInstanceType lastBin = SceneRenderer.RenderManager.BinOverride;
            SceneRenderer.RenderManager.BinOverride = RenderInstance.RenderInstanceType.Mesh2D;

            // Animate and render the shape... notice we override the bin.
            _shapeInstance.Animate();
            _shapeInstance.Render(_detailLevel, _intraDetailLevel, _shapeScale, srs, 0);

            // TODO: Split out the outer portion of this render routine from
            // the inner portion... this would allow us to nest mounted shapes.

            // Render all the objects mounted to us.
            foreach (T2DShape3D mountee in _mounted)
            {
                // Get the mounted transform.
                Matrix mat = _shapeInstance.NodeTransforms[mountee._mountIndex];

                // Apply the mount transform to the world matrix stack.
                srs.World.Push();
                srs.World.MultiplyMatrixLocal(mat);

                // Animate and render the mounted shape.
                mountee._shapeInstance.Animate();
                mountee._shapeInstance.Render(mountee._detailLevel, mountee._intraDetailLevel, mountee._shapeScale, srs, 0);

                srs.World.Pop();
            }

            // Restore the old bin override.
            SceneRenderer.RenderManager.BinOverride = lastBin;

            // Cleanup.
            srs.World.Pop();

#if DEBUG
            Profiler.Instance.EndBlock("T2DShape3D.Render");
#endif
        }



        public override bool OnRegister()
        {
            if (!base.OnRegister())
                return false;

            // Make sure we get a chance to advance animation.
            ProcessList.Instance.AddAnimationCallback(this, this);

            return true;
        }

        public override void OnUnregister()
        {
            base.OnUnregister();
            ProcessList.Instance.RemoveObject(this);
        }

        public override void CopyTo(TorqueObject obj)
        {
            base.CopyTo(obj);

            T2DShape3D obj2 = (T2DShape3D)obj;
            obj2.Shape = Shape;
            obj2.OnAnimateShape = OnAnimateShape;
            obj2.OnAnimationTrigger = OnAnimationTrigger;

            obj2.ShapeScale = ShapeScale;
            obj2.ShapeMatrix = ShapeMatrix;
            obj2.ShapeFile = ShapeFile;
            obj2.Shape = Shape;
            obj2.DetailLevel = DetailLevel;
            obj2.IntraDetailLevel = IntraDetailLevel;
        }



        public override void Dispose()
        {
            _IsDisposed = true;
            base.Dispose();
        }

        #endregion


        #region Private, protected, internal fields

        //protected float _camDist = 10;
        //protected float _camFov = (float)Math.PI / 2.0f;

        protected Matrix _shapeMat = Matrix.Identity;
        protected Vector3 _shapeScale = new Vector3(1, 1, 1);

        protected int _detailLevel = 0;
        protected float _intraDetailLevel = 0;

        protected String _shapeFile;
        protected ShapeInstance _shapeInstance;
        protected List<ValueInPlaceInterface<GarageGames.Torque.TS.Thread>> _threadInterfaces = new List<ValueInPlaceInterface<Thread>>();
        protected List<String> _threadNames = new List<string>();
        protected int _threadAutoAdvance; // Assumes fewer than 32 threads!

        protected T2DShape3D _mount = null;
        protected int _mountIndex;
        protected List<T2DShape3D> _mounted = new List<T2DShape3D>();

        protected OnAnimateShapeDelegate _OnAnimateShape;
        protected OnAnimationTriggerDelegate _OnAnimationTrigger;

        #endregion
    }
}
