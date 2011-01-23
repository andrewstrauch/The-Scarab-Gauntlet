//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using GarageGames.Torque.Sim;
using GarageGames.Torque.XNA;
using GarageGames.Torque.Core;
using GarageGames.Torque.SceneGraph;
using GarageGames.Torque.MathUtil;
using GarageGames.Torque.Util;



namespace GarageGames.Torque.T2D
{
    /// <summary>
    /// Scene camera used by T2DSceneGraph.
    /// </summary>
    public class T2DSceneCamera : T2DSceneObject, ISceneCamera, ITickObject, IDisposable
    {

        #region Public properties, operators, constants, and enums

        /// <summary>
        /// If true the the SceneMin and SceneMax will be scaled to fit
        /// the aspect ratio of the current display device.  Screen height
        /// will be maintained but width will be modified.  
        /// </summary>
        public bool ResizeToDisplayAspectRatio
        {
            set
            {
                Assert.Fatal(!value || !_resizeDisplayFixedWidth, "resize mode already specified");
                _resizeToDisplayAspectRatio = value;
                if (value)
                    _UpdateSceneRegion();
            }
            get { return _resizeToDisplayAspectRatio; }
        }



        /// <summary>
        /// If true the the SceneMin and SceneMax will be scaled to fit
        /// the aspect ratio of the current display device.  Screen width
        /// will be maintained but height will be modified. 
        /// </summary>
        public bool ResizeToDisplayAspectRatioWithFixedWidth
        {
            set
            {
                Assert.Fatal(!value || !_resizeToDisplayAspectRatio, "resize mode already specified");
                _resizeDisplayFixedWidth = value;
                if (value)
                    _UpdateSceneRegion();
            }
            get { return _resizeDisplayFixedWidth; }
        }



        /// <summary>
        /// Returns true if the camera extent is widescreen, false otherwise.
        /// </summary>
        public bool IsWideScreen
        {
            get { return GFX.GFXDevice.IsWideScreen(CameraAspectRatio); }
        }



        /// <summary>
        /// Return the width to height aspect ratio of the camera's extent.
        /// </summary>
        public float CameraAspectRatio
        {
            get
            {
                if (_extent.Y == 0.0f)
                    return 1.0f;
                else
                    return (_extent.X / _extent.Y);
            }
        }



        public float FOV
        {
            get { return MathHelper.PiOver2; }
            set { }
        }



        public float FarDistance
        {
            get { return _farDistance; }
            set { _farDistance = value; }
        }



        [XmlIgnore]
        public override Matrix Transform
        {
            get { return _camToWorld; }
        }



        /// <summary>
        /// The world view index associated with this camera.
        /// </summary>
        public int WorldViewIndex
        {
            get { return _worldViewIndex; }
            set { _worldViewIndex = value; }
        }



        /// <summary>
        /// Minimum values for the current view area.
        /// </summary>
        [XmlIgnore]
        public Vector2 SceneMin
        {
            get { return _sceneMin; }
        }



        /// <summary>
        /// Maximum values for the current view area.
        /// </summary>
        [XmlIgnore]
        public Vector2 SceneMax
        {
            get { return _sceneMax; }
        }



        public override Vector2 Position
        {
            get
            {
                if (UseCameraWorldLimits)
                {
                    // clamp the returned position to CameraWorldLimits (if used)
                    return new Vector2(MathHelper.Clamp(base.Position.X, CameraWorldLimitMin.X, CameraWorldLimitMax.X),
                                       MathHelper.Clamp(base.Position.Y, CameraWorldLimitMin.Y, CameraWorldLimitMax.Y));
                }
                else
                {
                    return base.Position;
                }
            }
            set
            {
                base.Position = value;
                _isMoving = false;
                _UpdateSceneRegion();
            }
        }



        /// <summary>
        /// Center of screen in world units.
        /// </summary>
        public Vector2 CenterPosition
        {
            get { return Position; }
            set { Position = value; }
        }



        /// <summary>
        /// If true, camera cannot move outside CameraWorldLimitMin and 
        /// CameraWorldLimitMax.
        /// </summary>
        public bool UseCameraWorldLimits
        {
            get { return _useCameraWorldLimits; }
            set { _useCameraWorldLimits = value; }
        }



        /// <summary>
        /// If UseCameraWorldLimits is true, then camera cannot move outside 
        /// CameraWorldLimitMin and CameraWorldLimitMax.
        /// </summary>
        public Vector2 CameraWorldLimitMin
        {
            get { return _cameraWorldLimitMin; }
            set { _cameraWorldLimitMin = value; }
        }



        /// <summary>
        /// If UseCameraWorldLimits is true, then camera cannot move outside 
        /// CameraWorldLimitMin and CameraWorldLimitMax.
        /// </summary>
        public Vector2 CameraWorldLimitMax
        {
            get { return _cameraWorldLimitMax; }
            set { _cameraWorldLimitMax = value; }
        }



        /// <summary>
        /// Amount of 2D world to view.  Reduce the extent to zoom in and
        /// expand the extent to zoom out.  Extent and CenterPosition are 
        /// used to determine SceneMin and SceneMax.
        /// </summary>
        public Vector2 Extent
        {
            set
            {
                _extent = value;
                _UpdateSceneRegion();
            }

            get { return _extent; }
        }



        /// <summary>
        /// The position that the camera should end up at if AnimatePosition is called.
        /// </summary>
        public Vector2 AnimatePositionTarget
        {
            get { return _animatePositionTarget; }
            set { _animatePositionTarget = value; }
        }



        /// <summary>
        /// The time in milliseconds that the camera will take to get to it's target position when AnimatePosition is called.
        /// </summary>
        public float AnimatePositionTime
        {
            get { return _animatePositionTime; }
            set { _animatePositionTime = value; }
        }



        /// <summary>
        /// The interpolation mode to use when animating this camera's position.
        /// </summary>
        public InterpolationMode AnimatePositionMode
        {
            get { return _animatePositionMode; }
            set { _animatePositionMode = value; }
        }



        /// <summary>
        /// Specifies whether or not the camera is currently animating its position.
        /// </summary>
        public bool IsMoving
        {
            get { return _isMoving; }
        }



        public override float Rotation
        {
            get { return base.Rotation; }
            set
            {
                base.Rotation = value;
                _isRotating = false;
            }
        }



        /// <summary>
        /// The rotation that the camera should end up at if AnimateRotation is called.
        /// </summary>
        public float AnimateRotationTarget
        {
            get { return _animateRotationTarget; }
            set { _animateRotationTarget = value; }
        }



        /// <summary>
        /// The time in milliseconds that the camera will take to get to it's target rotation when AnimateRotation is called.
        /// </summary>
        public float AnimateRotationTime
        {
            get { return _animateRotationTime; }
            set { _animateRotationTime = value; }
        }



        /// <summary>
        /// The interpolation mode to use when animating this camera's rotation.
        /// </summary>
        public InterpolationMode AnimateRotationMode
        {
            get { return _animateRotationMode; }
            set { _animateRotationMode = value; }
        }



        /// <summary>
        /// Specifies whether or not the camera is currently animating its rotation.
        /// </summary>
        public bool IsRotating
        {
            get { return _isRotating; }
        }



        /// <summary>
        /// The current zoom level of the camera.
        /// </summary>
        public float Zoom
        {
            get { return _zoom; }
            set
            {
                _zoom = value;
                _isZooming = false;
            }
        }



        /// <summary>
        /// The zoom that the camera should end up at if AnimateZoom is called.
        /// </summary>
        public float AnimateZoomTarget
        {
            get { return _animateZoomTarget; }
            set { _animateZoomTarget = value; }
        }



        /// <summary>
        /// The time in milliseconds that the camera will take to get to it's target Zoom when AnimateZoom is called.
        /// </summary>
        public float AnimateZoomTime
        {
            get { return _animateZoomTime; }
            set { _animateZoomTime = value; }
        }



        /// <summary>
        /// The interpolation mode to use when animating this camera's zoom.
        /// </summary>
        public InterpolationMode AnimateZoomMode
        {
            get { return _animateZoomMode; }
            set { _animateZoomMode = value; }
        }



        /// <summary>
        /// Specifies whether or not the camera is currently animating its zoom value.
        /// </summary>
        public bool IsZooming
        {
            get { return _isZooming; }
        }

        #endregion


        #region Static methods, fields, constructors

        public T2DSceneCamera()
        {
        }

        #endregion


        #region Public methods

        public override void CopyTo(TorqueObject obj)
        {
            base.CopyTo(obj);

            T2DSceneCamera dest = (T2DSceneCamera)obj;
            dest.ResizeToDisplayAspectRatio = this.ResizeToDisplayAspectRatio;
            dest.ResizeToDisplayAspectRatioWithFixedWidth = this.ResizeToDisplayAspectRatioWithFixedWidth;
            dest._camToWorld = this._camToWorld;
            dest._sceneMin = this._sceneMin;
            dest._sceneMax = this._sceneMax;
            dest.CenterPosition = this.CenterPosition;
            dest.Extent = this.Extent;
            dest.FarDistance = FarDistance;
        }



        public override void ProcessTick(Move move, float elapsed)
        {
#if DEBUG
            Profiler.Instance.StartBlock("T2DSceneCamera.ProcessTick");
#endif

            // WARNING: changing the call order of the methods here could cause
            // camera interpolation bugs, camera jitter, or mounting failure - be 
            // aware of any changes here!

            // what does this mean?
            // 1) base.StartTick should be explicitly called before any positions are 
            // set so the previous tick can be logged and any changes in position 
            // can be correctly recorded as occuring during "this tick"
            // 2) changes in position (camera animation and camera shake and any other
            // custom camera movements) happen next.
            // 3) base.ProcessTick must happen last. this is for two reasons. first, if
            // the camera is mounted it has to be updated after the camera has it's say
            // about where it wants to go. second, the tick is officially ended at the end
            // of base.ProcessTick, so if we change the position or try to start the tick
            // after that, the order will be screwed and interpolation won't work correctly.
            // 4) the actual view region of the camera is updated based on any changes to
            // position of rotation that might have just happened.

            // phew... 
            // ...enough chatter, here it is:

            // 1) explicitly call StatTick here...
            // this is normally done in moving objects by the physics component, 
            // but cameras don't require one. we need to call this to make sure that
            // position and rotation are interpolated and updated properly.
            base.StartTick();

            // 2) perform any position or rotation changes...
            // interpolate zoom and position
            _Animate();

            // upate camera shake
            _UpdateShake();

            // 3)call base.ProcessTick...
            // call T2DSceneObject's ProcessTick to update our position
            // based on our mountee's position (if we are mounted to another object)
            base.ProcessTick(move, elapsed);

            // 4) update the scene region...
            // recalculate our extents and transforms
            _UpdateSceneRegion();

            // see? 
            // piece of cake! >_<;

#if DEBUG
            Profiler.Instance.EndBlock("T2DSceneCamera.ProcessTick");
#endif
        }



        public override void InterpolateTick(float k)
        {
#if DEBUG
            Profiler.Instance.StartBlock("T2DSceneCamera.InterpolateTick");
#endif

            base.InterpolateTick(k);

            // interpolate zoom and position
            _Animate();

            // recalculate our extents and transforms
            _UpdateSceneRegion();

#if DEBUG
            Profiler.Instance.EndBlock("T2DSceneCamera.InterpolateTick");
#endif
        }



        public override bool OnRegister()
        {
            if (!base.OnRegister())
                return false;

            // register ourselves for a tick callback
            ProcessList.Instance.AddTickCallback(this);

            return true;
        }

        public override void OnUnregister()
        {
            ProcessList.Instance.RemoveObject(this);
            base.OnUnregister();
        }

        /// <summary>
        /// Apply the configured camera resize options (ResizeToDisplayAspectRatio or ResizeToDisplayAspectRatioWithFixedWidth) to the 
        /// specified input vector.  The camera will perform this automatically on its extent if one of the above parameters is true.  You can
        /// also call this function to resize objects.  For instance you may have a border image which defines the edge of the viewable area,
        /// so this function could be used to resize that image.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public Vector2 ApplyResize(Vector2 input)
        {
            if (_resizeToDisplayAspectRatio || _resizeDisplayFixedWidth)
            {
                Assert.Fatal(GFX.GFXDevice.Instance != null, "Attempted to perform a camera resize operation when the GFXDevice does not exist");

                int height = GFX.GFXDevice.Instance.CurrentVideoMode.VirtualHeight;
                int width = GFX.GFXDevice.Instance.CurrentVideoMode.VirtualWidth;

                float displayRatio = (float)height / width;
                float inputRatio = (float)input.Y / input.X;
                if (inputRatio != displayRatio)
                {
                    if (_resizeDisplayFixedWidth)
                        input.Y = input.X * displayRatio;
                    else
                        input.X = input.Y * (1.0f / displayRatio);
                }
            }
            return input;
        }



        /// <summary>
        /// Begin animating the camera's position using the data specified in the 
        /// camera's AnimatePositionTarget, AnimatePositionTime, and AnimatePositionMode properties.
        /// </summary>
        public void AnimatePosition()
        {
            _startPosition = Position;
            _positionInterpStartTime = TorqueEngineComponent.Instance.TorqueTime;
            _isMoving = true;
        }



        /// <summary>
        /// Begin animating the camera's position using the specified values.
        /// </summary>
        /// <param name="targetPosition">The desired position to end up at.</param>
        public void AnimatePosition(Vector2 targetPosition)
        {
            // apply the specified value
            _animatePositionTarget = targetPosition;

            // begin animating position
            AnimatePosition();
        }



        /// <summary>
        /// Begin animating the camera's position using the specified values.
        /// </summary>
        /// <param name="targetPosition">The desired position to end up at.</param>
        /// <param name="animationTime">The amount of time in milliseconds to allow the camera to get to the target position.</param>
        public void AnimatePosition(Vector2 targetPosition, float animationTime)
        {
            // apply the specified values
            _animatePositionTarget = targetPosition;
            _animatePositionTime = animationTime;

            // begin animating position
            AnimatePosition();
        }



        /// <summary>
        /// Begin animating the camera's position using the specified values.
        /// </summary>
        /// <param name="targetPosition">The desired position to end up at.</param>
        /// <param name="animationTime">The amount of time in milliseconds to allow the camera to get to the target position.</param>
        /// <param name="mode">The InterpolateionMode to use when animating the camera's position.</param>
        public void AnimatePosition(Vector2 targetPosition, float animationTime, InterpolationMode mode)
        {
            // apply the specified values
            _animatePositionTarget = targetPosition;
            _animatePositionTime = animationTime;
            _animatePositionMode = mode;

            // begin animating position
            AnimatePosition();
        }



        /// <summary>
        /// Immediately stop animating the camera's position and snap to the AnimatePositionTarget position.
        /// </summary>
        public void CompletePositionAnimation()
        {
            Position = _animatePositionTarget;
            _isMoving = false;
        }



        /// <summary>
        /// Begin animating the camera's rotation using the data specified in the 
        /// camera's AnimateRotationTarget, AnimateRotationTime, and AnimateRotationMode properties.
        /// </summary>
        public void AnimateRotation()
        {
            _startRotation = Rotation;
            _rotationInterpStartTime = TorqueEngineComponent.Instance.TorqueTime;
            _isRotating = true;
        }



        /// <summary>
        /// Begin animating the camera's rotation using the specified values.
        /// </summary>
        /// <param name="targetRotation">The desired rotation to end up at.</param>
        public void AnimateRotation(float targetRotation)
        {
            // apply the specified value
            _animateRotationTarget = targetRotation;

            // begin animating rotation
            AnimateRotation();
        }



        /// <summary>
        /// Begin animating the camera's rotation using the specified values.
        /// </summary>
        /// <param name="targetRotation">The desired rotation to end up at.</param>
        /// <param name="animationTime">The amount of time in milliseconds to allow the camera to get to the target rotation.</param>
        public void AnimateRotation(float targetRotation, float animationTime)
        {
            // apply the specified values
            _animateRotationTarget = targetRotation;
            _animateRotationTime = animationTime;

            // begin animating rotation
            AnimateRotation();
        }

        /// <summary>
        /// Begin animating the camera's rotation using the specified values.
        /// </summary>
        /// <param name="targetRotation">The desired rotation to end up at.</param>
        /// <param name="animationTime">The amount of time in milliseconds to allow the camera to get to the target rotation.</param>
        /// <param name="mode">The InterpolateionMode to use when animating the camera's rotation.</param>
        public void AnimateRotation(float targetRotation, float animationTime, InterpolationMode mode)
        {
            // apply the specified values
            _animateRotationTarget = targetRotation;
            _animateRotationTime = animationTime;
            _animateRotationMode = mode;

            // begin animating rotation
            AnimateRotation();
        }



        /// <summary>
        /// Immediately stop animating the camera's rotation and snap to the AnimateRotationTarget rotation.
        /// </summary>
        public void CompleteRotationAnimation()
        {
            Rotation = _animateRotationTarget;
            _isRotating = false;
        }

        /// <summary>
        /// Begin animating the camera's zoom using the data specified in the 
        /// camera's AnimateZoomTarget, AnimateZoomTime, and AnimateZoomMode properties.
        /// </summary>
        public void AnimateZoom()
        {
            _startZoom = _zoom;
            _zoomInterpStartTime = TorqueEngineComponent.Instance.TorqueTime;
            _isZooming = true;
        }



        /// <summary>
        /// Begin animating the camera's zoom using the specified values.
        /// </summary>
        /// <param name="targetZoom">The desired zoom to end up at.</param>
        public void AnimateZoom(float targetZoom)
        {
            // apply the specified value
            _animateZoomTarget = targetZoom;

            // begin animating zoom
            AnimateZoom();
        }



        /// <summary>
        /// Begin animating the camera's zoom using the specified values.
        /// </summary>
        /// <param name="targetZoom">The desired zoom to end up at.</param>
        /// <param name="animationTime">The amount of time in milliseconds to allow the camera to get to the target zoom value.</param>
        public void AnimateZoom(float targetZoom, float animationTime)
        {
            // apply the specified values
            _animateZoomTarget = targetZoom;
            _animateZoomTime = animationTime;

            // begin animating zoom
            AnimateZoom();
        }



        /// <summary>
        /// Begin animating the camera's zoom using the specified values.
        /// </summary>
        /// <param name="targetZoom">The desired zoom to end up at.</param>
        /// <param name="animationTime">The amount of time in milliseconds to allow the camera to get to the target zoom value.</param>
        /// <param name="mode">The InterpolateionMode to use when animating the camera's zoom.</param>
        public void AnimateZoom(float targetZoom, float animationTime, InterpolationMode mode)
        {
            // apply the specified values
            _animateZoomTarget = targetZoom;
            _animateZoomTime = animationTime;
            _animateZoomMode = mode;

            // begin animating zoom
            AnimateZoom();
        }



        /// <summary>
        /// Immediately stop animating the camera's zoom and snap to the AnimateZoomTarget zoom value.
        /// </summary>
        public void CompleteZoomAnimation()
        {
            _zoom = _animateZoomTarget;
            _isZooming = false;
        }



        /// <summary>
        /// Immediately stop animating the camera's position, rotation, and zoom and snap any of them that were animating 
        /// to their respective target values.
        /// </summary>
        public void CompleteAllAnimation()
        {
            if (_isMoving)
                CompletePositionAnimation();

            if (_isRotating)
                CompleteRotationAnimation();

            if (_isZooming)
                CompleteZoomAnimation();
        }



        public void StartShake(float magnitude, float duration)
        {
            if (_shakeActive)
                _shakeMagnitude += magnitude;
            else
            {
                _shakeMagnitude = magnitude;
                _shakeStartPosition = CenterPosition;
            }

            _shakeEnd = TorqueEngineComponent.Instance.TorqueTime + duration;
            _shakeActive = true;
        }

        #endregion


        #region Private, protected, internal methods

        protected void _Animate()
        {
            if (_isMoving)
                _InterpolatePosition();

            if (_isRotating)
                _InterpolateRotation();

            if (_isZooming)
                _InterpolateZoom();
        }



        protected void _InterpolatePosition()
        {
            float progress = (TorqueEngineComponent.Instance.TorqueTime - _positionInterpStartTime) / _animatePositionTime;

            float positionX = InterpolationHelper.Interpolate(_startPosition.X, _animatePositionTarget.X, progress, _animatePositionMode);
            float positionY = InterpolationHelper.Interpolate(_startPosition.Y, _animatePositionTarget.Y, progress, _animatePositionMode);

            base.Position = new Vector2(positionX, positionY);

            if (Position == _animatePositionTarget)
                CompletePositionAnimation();
        }



        protected void _InterpolateRotation()
        {
            float progress = (TorqueEngineComponent.Instance.TorqueTime - _rotationInterpStartTime) / _animateRotationTime;

            base.Rotation = InterpolationHelper.Interpolate(_startRotation, _animateRotationTarget, progress, _animateRotationMode);

            if (Rotation == _animateRotationTarget)
                CompleteRotationAnimation();
        }



        protected void _InterpolateZoom()
        {
            float progress = (TorqueEngineComponent.Instance.TorqueTime - _zoomInterpStartTime) / _animateZoomTime;

            _zoom = InterpolationHelper.Interpolate(_startZoom, _animateZoomTarget, progress, _animatePositionMode);

            if (_zoom == _animateZoomTarget)
                CompleteZoomAnimation();
        }



        protected void _UpdateSceneRegion()
        {
            // partially based on c++ t2dSceneWindow::setCurrentCameraPosition...doesn't do zoom window or mounting

            // resize base on aspect ratio if specified
            _extent = ApplyResize(_extent);

            // set scene range.  
            // if we implement zooming this will need to change, see t2dSceneWindow::calculateCameraView.
            Vector2 centerPos = CenterPosition;

            if (Rotation == 0.0f)
            {
                _sceneMin.X = centerPos.X - _extent.X / (2 * _zoom);
                _sceneMin.Y = centerPos.Y - _extent.Y / (2 * _zoom);
                _sceneMax.X = centerPos.X + _extent.X / (2 * _zoom);
                _sceneMax.Y = centerPos.Y + _extent.Y / (2 * _zoom);
                _camToWorld = Matrix.Identity * Matrix.CreateScale(1 / _zoom);
            }
            else
            {
                Rotation2D rot = new Rotation2D(MathHelper.ToRadians(Rotation));
                Vector2 x = rot.X;
                Vector2 y = rot.Y;
                float xext = 0.5f * _extent.X * Math.Abs(x.X) + 0.5f * _extent.Y * Math.Abs(y.X);
                float yext = 0.5f * _extent.X * Math.Abs(x.Y) + 0.5f * _extent.Y * Math.Abs(y.Y);
                _sceneMin = _sceneMax = centerPos;
                _sceneMin.X -= xext * (1 / _zoom);
                _sceneMax.X += xext * (1 / _zoom);
                _sceneMin.Y -= yext * (1 / _zoom);
                _sceneMax.Y += yext * (1 / _zoom);
                _camToWorld = Matrix.CreateRotationZ(MathHelper.ToRadians(Rotation)) * Matrix.CreateScale(1 / _zoom);
            }

            _camToWorld.Translation = new Vector3(centerPos, 20.0f);
        }



        protected void _UpdateShake()
        {
            if (!_shakeActive)
                return;

            if (TorqueEngineComponent.Instance.TorqueTime > _shakeEnd)
            {
                _shakeActive = false;
                CenterPosition = _shakeStartPosition;
                return;
            }

            Vector2 shake = new Vector2(_random.Next((int)(_shakeMagnitude * 100.0f)), _random.Next((int)(_shakeMagnitude * 100.0f))) / 100.0f;
            CenterPosition = _shakeStartPosition + shake;

            _shakeMagnitude /= 1.1f;
        }

        #endregion


        #region Private, protected, internal fields

        Matrix _camToWorld = Matrix.Identity;
        int _worldViewIndex;
        Vector2 _sceneMin;
        Vector2 _sceneMax;
        Vector2 _extent;
        bool _resizeToDisplayAspectRatio;
        bool _resizeDisplayFixedWidth;
        bool _useCameraWorldLimits;
        Vector2 _cameraWorldLimitMin;
        Vector2 _cameraWorldLimitMax;
        float _farDistance = 100.0f;

        bool _isMoving;
        Vector2 _startPosition;
        Vector2 _animatePositionTarget;
        float _animatePositionTime = 1000.0f;
        float _positionInterpStartTime;
        InterpolationMode _animatePositionMode = InterpolationMode.EaseInOut;

        bool _isRotating;
        float _startRotation;
        float _animateRotationTarget;
        float _animateRotationTime = 1000.0f;
        float _rotationInterpStartTime;
        InterpolationMode _animateRotationMode = InterpolationMode.EaseInOut;

        bool _isZooming;
        float _startZoom;
        float _zoom = 1.0f;
        float _animateZoomTarget = 1.0f;
        float _animateZoomTime = 1000.0f;
        float _zoomInterpStartTime;
        InterpolationMode _animateZoomMode = InterpolationMode.EaseInOut;

        Random _random = new Random();
        float _shakeMagnitude;
        float _shakeEnd;
        bool _shakeActive;
        Vector2 _shakeStartPosition;

        #endregion

        #region IDisposable Members

        public override void Dispose()
        {
            _IsDisposed = true;
            base.Dispose();
        }

        #endregion
    }
}