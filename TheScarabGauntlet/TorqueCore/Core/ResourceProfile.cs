//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;



namespace GarageGames.Torque.Core
{
    /// <summary>
    /// Base class for Resource Profiles.
    /// </summary>
    public class BaseResourceProfile
    {
        #region Public properties, operators, constants, and enums

        /// <summary>
        /// Read only. Returns whether the resource associated with this profile should become
        /// invalidated when the graphics device is disposed.
        /// </summary>
        public virtual bool InvalidateOnDeviceDispose
        {
            get { return false; }
        }



        /// <summary>
        /// Read only. Returns whether the resource associated with this profile should become
        /// invalidated when the graphics device is lost or reset.
        /// </summary>
        public virtual bool InvalidateOnDeviceReset
        {
            get { return false; }
        }



        /// <summary>
        /// Read only. Returns whether the resource associated with this profile should be 
        /// disposed when the resource becomes invalid.
        /// </summary>
        public virtual bool DisposeOnInvalidate
        {
            get { return false; }
        }

        #endregion
    }



    /// <summary>
    /// Resource profile for anything loaded from a content manager.
    /// </summary>
    public class ContentManagerProfile : BaseResourceProfile
    {
        #region Public properties, operators, constants, and enums

        /// <summary>
        /// This resource profile will always invalidate the associated resource
        /// when the graphics device is disposed.
        /// </summary>
        public override bool InvalidateOnDeviceDispose
        {
            get { return true; }
        }



        /// <summary>
        /// This resource profile will not invalidate the assocated resource
        /// when the graphics device is lost or reset.
        /// </summary>
        public override bool InvalidateOnDeviceReset
        {
            get { return true; }
        }



        /// <summary>
        /// This resource profile will not dispose of the associated resource
        /// when that resource becomes invalid.
        /// </summary>
        public override bool DisposeOnInvalidate
        {
            get { return false; }
        }

        #endregion
    }



    /// <summary>
    /// Resource profile for all D3D resources that we create (not resources loaded from a content manager).
    /// </summary>
    public class D3DResourceProfile : BaseResourceProfile
    {
        #region Constructors

        public D3DResourceProfile(TextureUsage textureUsage, BufferUsage bufferUsage)
        {
            _textureUsage = textureUsage;
            _bufferUsage = bufferUsage;
        }

        #endregion


        #region Public properties, operators, constants, and enums

        /// <summary>
        /// This resource profile will invalidate the associated resource, if the
        /// ResourceManagementMode is Manual, when the graphics device is disposed.
        /// </summary>
        //public override bool InvalidateOnDeviceDispose
        //{
        //    get { return _mode == ResourceManagementMode.Manual; }
        //}

        /// <summary>
        /// This resource profile will invalidate the assocated resource, if the
        /// ResourceManagementMode is Manual, when the graphics device is lost or reset.
        /// </summary>
        //public override bool InvalidateOnDeviceReset
        //{
        //    get { return _mode == ResourceManagementMode.Manual; }
        //}



        /// <summary>
        /// This resource profile will always dispose of the associated resource when the
        /// resource becomes invalid.
        /// </summary>
        public override bool DisposeOnInvalidate
        {
            get { return true; }
        }

        #endregion


        #region Private, protected, internal methods

        public TextureUsage _textureUsage;
        public BufferUsage _bufferUsage;

        #endregion
    }



    /// <summary>
    /// Resource profile for D3D index buffers.
    /// </summary>
    public class D3DIndexBufferProfile : D3DResourceProfile
    {
        #region Constructors

        public D3DIndexBufferProfile(BufferUsage usage) : base(TextureUsage.None, usage) { }

        #endregion

        public override bool InvalidateOnDeviceReset
        {
            get { return true; }
        }
    }



    /// <summary>
    /// Resource profile for D3D vertex buffers.
    /// </summary>
    public class D3DVertexBufferProfile : D3DResourceProfile
    {
        #region Constructors
        public D3DVertexBufferProfile(BufferUsage usage)
            : base(TextureUsage.None, usage)
        {
        }
        #endregion

        public override bool InvalidateOnDeviceReset
        {
            get { return true; }
        }

    }



    /// <summary>
    /// Resource profile for D3D texture cubes.
    /// </summary>
    public class D3DTextureCubeProfile : D3DResourceProfile
    {
        #region Constructors

        public D3DTextureCubeProfile(TextureUsage usage) : base(usage, BufferUsage.None) { }

        #endregion
    }



    /// <summary>
    /// Resource profile for D3D render targets.
    /// </summary>
    public class D3DRenderTargetCubeProfile : D3DResourceProfile
    {
        #region Constructors

        public D3DRenderTargetCubeProfile(TextureUsage usage) : base(usage, BufferUsage.None) { }

        #endregion
    }



    /// <summary>
    /// Predefined resource profiles.
    /// </summary>
    public class ResourceProfiles
    {
        #region Static methods, fields, constructors

        public static D3DIndexBufferProfile ManualStaticIBProfile = new D3DIndexBufferProfile(BufferUsage.WriteOnly);
        public static D3DIndexBufferProfile AutomaticStaticIBProfile = new D3DIndexBufferProfile(BufferUsage.WriteOnly);
        public static D3DIndexBufferProfile ManualDynamicIBProfile = new D3DIndexBufferProfile(BufferUsage.WriteOnly);
        public static D3DIndexBufferProfile AutomaticDynamicIBProfile = new D3DIndexBufferProfile(BufferUsage.WriteOnly);

        public static D3DVertexBufferProfile ManualStaticVBProfile = new D3DVertexBufferProfile(BufferUsage.WriteOnly);
        public static D3DVertexBufferProfile AutomaticStaticVBProfile = new D3DVertexBufferProfile(BufferUsage.WriteOnly);
        public static D3DVertexBufferProfile ManualDynamicVBProfile = new D3DVertexBufferProfile(BufferUsage.WriteOnly);
        public static D3DVertexBufferProfile AutomaticDynamicVBProfile = new D3DVertexBufferProfile(BufferUsage.WriteOnly);

        public static D3DTextureCubeProfile ManualTextureCubeProfile = new D3DTextureCubeProfile(TextureUsage.None);
        public static D3DTextureCubeProfile AutomaticTextureCubeProfile = new D3DTextureCubeProfile(TextureUsage.None);

        public static D3DResourceProfile ManualGenericProfile = new D3DResourceProfile(TextureUsage.None, BufferUsage.None);
        public static D3DResourceProfile AutomaticGenericProfile = new D3DResourceProfile(TextureUsage.None, BufferUsage.None);

        public static ContentManagerProfile DefaultContentManagerProfile = new ContentManagerProfile();

        public static D3DRenderTargetCubeProfile ManualRenderTargetCubeProfile = new D3DRenderTargetCubeProfile(TextureUsage.None);

        #endregion
    }
}
