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
using GarageGames.Torque.GFX;
using GarageGames.Torque.SceneGraph;
using GarageGames.Torque.MathUtil;
using GarageGames.Torque.RenderManager;
using GarageGames.Torque.XNA;
using GarageGames.Torque.Util;



namespace GarageGames.Torque.Materials
{
    /// <summary>
    /// Camera object used for rendering dynamic cubemap faces.
    /// </summary>
    public class CubemapCamera : ISceneCamera, IDisposable
    {

        #region Public properties, operators, constants, and enums

        public BaseSceneGraph SceneGraph
        {
            get { return null; }
        }



        public Matrix Transform
        {
            get { return _transform; }
            set { _transform = value; }
        }



        public int WorldViewIndex
        {
            get { return _worldViewIndex; }
            set { _worldViewIndex = value; }
        }



        public float FOV
        {
            get { return 0.0f; }
            set { }
        }



        public float FarDistance
        {
            get { return _farDistance; }
            set { _farDistance = value; }
        }

        #endregion


        #region Private, protected, internal fields

        private int _worldViewIndex;
        float _farDistance = 100.0f;
        public Matrix _transform;

        #endregion

        #region IDisposable Members

        public virtual void Dispose()
        {
            //It's here just as a GC helper
        }

        #endregion
    }



    public class Cubemap : IDisposable
    {

        #region Public properties, operators, constants, and enums

        /// <summary>
        /// The filenames of the cubemap faces. This is only necessary if the cubemap is static.
        /// </summary>
        public string[] CubeFaceFilenames
        {
            get { return _cubeFaceFilenames; }
            set { _cubeFaceFilenames = value; }
        }



        /// <summary>
        /// The priority to update the cubemap at.
        /// </summary>
        public int DynamicUpdatePriority
        {
            get { return _dynamicUpdatePriority; }
            set { _dynamicUpdatePriority = value; }
        }



        /// <summary>
        /// The TextureCube that holds the texture data for each of the cubemap faces.
        /// </summary>
        public TextureCube Texture
        {
            get { return _cubeTexture.Instance; }
        }



        /// <summary>
        /// Whether or not the cubemap has been initialized.
        /// </summary>
        public bool IsInitialized
        {
            get { return !_cubeTexture.IsNull; }
        }



        /// <summary>
        /// The position of the cubemap in the world.
        /// </summary>
        public Vector3 Position
        {
            get { return _position; }
            set { _position = value; }
        }

        #endregion


        #region Public methods

        /// <summary>
        /// Initializes the cubemap.
        /// </summary>
        /// <param name="dynamic">True to make a dynamic cubemap, false to make a static cubemap.</param>
        /// <param name="size">The pixel size of the cubemap faces.</param>
        /// <param name="camera">The camera to use to render the cubemap faces.</param>
        public void Create(bool dynamic, int size, CubemapCamera camera)
        {
            if (dynamic)
                _InitDynamic(size, camera);
            else
            {
                for (int i = 0; i < 6; i++)
                {
                    string loadpath = TorqueUtil.ChopFileExtension(_cubeFaceFilenames[i].Trim());
                    _cubeFaces[i] = ResourceManager.Instance.LoadTexture(loadpath).Instance;
                }

                _InitStatic(ref _cubeFaces);
            }
        }



        /// <summary>
        /// Cleans up the cubemap.
        /// </summary>
        public void Destroy()
        {
            _cubeTexture.Invalidate();
            _renderTarget.Invalidate();
            _sceneRenderState = null;
            _camera = null;
        }

        #endregion


        #region Private, protected, internal methods

        protected void _InitStatic(ref Texture[] faces)
        {
            if (!_cubeTexture.IsNull)
                return;

            int width = ((Texture2D)faces[0]).Width;
            _cubeTexture = ResourceManager.Instance.CreateTextureCube(ResourceProfiles.ManualTextureCubeProfile, width, 1, SurfaceFormat.Bgr32);
            _FillCubeTextures(ref faces);
        }



        protected void _InitDynamic(int size, CubemapCamera camera)
        {
            if (!_cubeTexture.IsNull)
                return;

            _camera = camera;
            _sceneRenderState = new SceneRenderState();
            _renderTarget = ResourceManager.Instance.CreateRenderTargetCube(size);

            // resolve faces of cube texture for the xbox
            for (int i = 0; i < 6; ++i)
            {
                GFXDevice.Instance.Device.SetRenderTarget(0, _renderTarget.Instance, (CubeMapFace)i);
            }

            // reset render target 
            TorqueEngineComponent.Instance.ReapplyMainRenderTarget();

            _cubeTexture = ResourceManager.Instance.CreateTextureCube(_renderTarget);
        }



        public void UpdateDynamic(SceneRenderState srs)
        {
            Vector3 upVec = new Vector3(), lookAt = new Vector3(), cross = new Vector3();

            // save the old viewport
            Viewport oldViewport = GFXDevice.Instance.Device.Viewport;

            for (int i = 0; i < 6; i++)
            {
                _camera.Transform = Matrix.Identity;

                switch (i)
                {
                    case (int)CubeMapFace.NegativeX:
                        upVec.X = 0.0F;
                        upVec.Y = 1.0F;
                        upVec.Z = 0.0F;

                        lookAt.X = -1.0F;
                        lookAt.Y = 0.0F;
                        lookAt.Z = 0.0F;
                        break;

                    case (int)CubeMapFace.NegativeY:
                        upVec.X = 0.0F;
                        upVec.Y = 0.0F;
                        upVec.Z = 1.0F;

                        lookAt.X = 0.0F;
                        lookAt.Y = -1.0F;
                        lookAt.Z = 0.0F;
                        break;
                    case (int)CubeMapFace.NegativeZ:
                        upVec.X = 0.0F;
                        upVec.Y = 1.0F;
                        upVec.Z = 0.0F;

                        lookAt.X = 0.0F;
                        lookAt.Y = 0.0F;
                        lookAt.Z = -1.0F;
                        break;

                    case (int)CubeMapFace.PositiveX:
                        upVec.X = 0.0F;
                        upVec.Y = 1.0F;
                        upVec.Z = 0.0F;

                        lookAt.X = 1.0F;
                        lookAt.Y = 0.0F;
                        lookAt.Z = 0.0F;
                        break;
                    case (int)CubeMapFace.PositiveY:
                        upVec.X = 0.0F;
                        upVec.Y = 0.0F;
                        upVec.Z = -1.0F;

                        lookAt.X = 0.0F;
                        lookAt.Y = 1.0F;
                        lookAt.Z = 0.0F;
                        break;
                    case (int)CubeMapFace.PositiveZ:
                        upVec.X = 0.0F;
                        upVec.Y = 1.0F;
                        upVec.Z = 0.0F;

                        lookAt.X = 0.0F;
                        lookAt.Y = 0.0F;
                        lookAt.Z = 1.0F;
                        break;
                }

                // faces pointing less than 45 degrees different than the camera don't need to be updated
                Matrix transform = srs.CameraTransform;
                Vector3 cameraDir = MatrixUtil.MatrixGetRow(1, ref transform);
                cameraDir.Normalize();

                float dot = Vector3.Dot(cameraDir, lookAt);
                if (dot > 0.707f)
                    continue;

                cross = Vector3.Cross(upVec, lookAt);
                cross.Normalize();

                _camera._transform.M11 = cross.X;
                _camera._transform.M12 = cross.Y;
                _camera._transform.M13 = cross.Z;

                _camera._transform.M21 = lookAt.X;
                _camera._transform.M22 = lookAt.Y;
                _camera._transform.M23 = lookAt.Z;

                _camera._transform.M31 = upVec.X;
                _camera._transform.M32 = upVec.Y;
                _camera._transform.M33 = upVec.Z;

                _camera._transform.M41 = _position.X;
                _camera._transform.M42 = _position.Y;
                _camera._transform.M43 = _position.Z;

                _sceneRenderState.Gfx = GFXDevice.Instance;
                _sceneRenderState.SceneGraph = srs.SceneGraph;
                _sceneRenderState.CameraTransform = _camera._transform;
                _sceneRenderState.View = Matrix.Invert(_camera._transform);
                _sceneRenderState.IsReflectPass = true;

                GFXDevice.Instance.Device.SetRenderTarget(0, _renderTarget.Instance, (CubeMapFace)i);

                Vector4 color = new Vector4(1.0f, 0.0f, 0.0f, 1.0f);
                GFXDevice.Instance.Device.Clear(ClearOptions.DepthBuffer | ClearOptions.Target, color, 1.0f, 0);

                _frustum.SetFrustum(0.2f, _camera.FarDistance, (float)(Math.PI / 2), 1.0f, _camera._transform);
                _sceneRenderState.Frustum = _frustum;

                _sceneRenderState.Projection = GFXDevice.Instance.SetFrustum((float)(Math.PI / 2), 1.0f, 0.2f, 1000.0f);

                SceneRenderer.RenderManager.Render(_sceneRenderState);

                // resolve to get the render data into the texture (required on xbox)
                GFXDevice.Instance.Device.SetRenderTarget(0, null);
            }

            TorqueEngineComponent.Instance.ReapplyMainRenderTarget();

            GFXDevice.Instance.Device.Viewport = oldViewport;
        }



        void _FillCubeTextures(ref Texture[] faces)
        {
            for (int i = 0; i < 6; i++)
            {
                Texture2D face = (Texture2D)faces[i];
                uint[] data = new uint[face.Width * face.Height];
                face.GetData<uint>(data);
                _cubeTexture.Instance.SetData<uint>(_faceList[i], data);
            }
        }

        #endregion


        #region Private, protected, internal fields

        Frustum _frustum = new Frustum();
        Resource<TextureCube> _cubeTexture;
        Resource<RenderTargetCube> _renderTarget;

        readonly CubeMapFace[] _faceList =
        {
            CubeMapFace.PositiveX, CubeMapFace.NegativeX,
            CubeMapFace.PositiveY, CubeMapFace.NegativeY,
            CubeMapFace.PositiveZ, CubeMapFace.NegativeZ
        };

        SceneRenderState _sceneRenderState;
        CubemapCamera _camera = null;

        string[] _cubeFaceFilenames = new string[6];
        Texture[] _cubeFaces = new Texture[6];
        int _dynamicUpdatePriority = 2;

        Vector3 _position;

        #endregion

        #region IDisposable Members

        public virtual void Dispose()
        {
            Destroy();
        }

        #endregion
    }
}
