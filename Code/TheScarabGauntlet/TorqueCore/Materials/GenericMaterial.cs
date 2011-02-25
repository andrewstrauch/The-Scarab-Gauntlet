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
using GarageGames.Torque.Lighting;
using GarageGames.Torque.RenderManager;
using GarageGames.Torque.SceneGraph;



namespace GarageGames.Torque.Materials
{
    /// <summary>
    /// The type of bind for arbitrary bindings.
    /// </summary>
    public enum BindType
    {
        /// <summary>
        /// Bound by name.
        /// </summary>
        Name,

        /// <summary>
        /// Bound by semantic.
        /// </summary>
        Semantic
    }



    /// <summary>
    /// A binding of a name to an effect parameter.
    /// </summary>
    public class GenericMaterialBind
    {

        #region Public properties, operators, constants, and enums

        /// <summary>
        /// The type of bind - by name or semantic.
        /// </summary>
        public BindType BindType
        {
            get { return _bindType; }
            set { _bindType = value; }
        }



        /// <summary>
        /// The address of the bind. Either the name of the parameter or the semantic, depending
        /// on BindType.
        /// </summary>
        public string BindAddress
        {
            get { return _bindAddress; }
            set { _bindAddress = value; }
        }



        /// <summary>
        /// The actual effect parameter.
        /// </summary>
        public EffectParameter Parameter
        {
            get { return _effectParameter; }
            set { _effectParameter = value; }
        }

        #endregion


        #region Private, protected, internal fields

        BindType _bindType = BindType.Name;
        string _bindAddress = String.Empty;
        EffectParameter _effectParameter;

        #endregion
    }



    /// <summary>
    /// A float binding of an effect parameter.
    /// </summary>
    public class GenericMaterialFloatBind : GenericMaterialBind
    {

        #region Public properties, operators, constants, and enums

        /// <summary>
        /// The float value.
        /// </summary>
        public Vector4 Value
        {
            get { return _value; }
            set { _value = value; }
        }

        #endregion


        #region Private, protected, internal fields

        Vector4 _value;

        #endregion
    }



    /// <summary>
    /// A texture binding of an effect parameter.
    /// </summary>
    public class GenericMaterialTextureBind : GenericMaterialBind
    {

        #region Public properties, operators, constants, and enums

        /// <summary>
        /// The filename of the texture.
        /// </summary>
        public string TextureFilename
        {
            get { return _textureFilename; }
            set { _textureFilename = value; }
        }

        #endregion


        #region Private, protected, internal fields

        private string _textureFilename;
        internal Resource<Texture> _texture;

        #endregion
    }



    /// <summary>
    /// An entry into a list of techniques that is used for automatic fallbacks if shader model
    /// requirements for certain techniques aren't met.
    /// </summary>
    public class TechniqueChainEntry
    {

        #region Public properties, operators, constants, and enums

        /// <summary>
        /// The name of the technique.
        /// </summary>
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }



        /// <summary>
        /// The minimum shader profile that this technique requires.
        /// </summary>
        public ShaderProfile Profile
        {
            get { return _profile; }
            set { _profile = value; }
        }

        #endregion


        #region Private, protected, internal fields

        protected string _name = String.Empty;
        protected ShaderProfile _profile = ShaderProfile.PS_1_1;

        #endregion
    }



    /// <summary>
    /// Material used for binding arbitrary parameters and using arbitrary techniques based
    /// on various settings specified at runtime. This can be used for many effects created in
    /// RenderMonkey or FXComposer, as long as Torque X provides the necessary information to
    /// the effect. The effect file itself needs to be set on this material via the EffectFilename
    /// property. The parameters that are available are:
    /// 
    /// LightCount
    /// DirectionalLightCount
    /// PointLightCount
    /// LightColor
    /// LightPosition
    /// LightDirection
    /// LightRange
    /// WorldMatrixCount
    /// WorldMatrix
    /// WorldInverseMatrix
    /// WorldViewMatrix
    /// WorldViewProjectionMatrix
    /// ViewMatrix
    /// ViewProjectionMatrix
    /// ProjectionMatrix
    /// TimeNow
    /// TimeLast
    /// TimeElapsed
    /// TimeFrameNumber
    /// viewportScaleAndOffset
    /// viewportUVMinMax
    /// 
    /// These are all bound based on whether or not the semantic for them is found in the actual
    /// effect file. Also, additional textures and floats can be bound via the FloatBinds and
    /// TextureBinds properties.
    /// 
    /// The technique to use can either be set directly via the Technique property, or by adding
    /// entries to the technique chain. The technique that is used is selected by looking for the
    /// highest entry whose profile is supported by the shader model of the current hardware.
    /// </summary>
    public class GenericMaterial : RenderMaterial
    {
        /// <summary>
        /// Class for wrapping light parameters.
        /// </summary>
        protected class SasLight
        {

            #region Public properties, operators, constants, and enums

            public EffectParameter SasLightColor;
            public EffectParameter SasLightPosition;
            public EffectParameter SasLightDirection;
            public EffectParameter SasLightRange;

            #endregion
        }


        #region Public properties, operators, constants, and enums

        /// <summary>
        /// List of arbitrary float bindings.
        /// </summary>
        public List<GenericMaterialFloatBind> FloatBinds
        {
            get { return _floatBinds; }
            set { _floatBinds = value; }
        }



        /// <summary>
        /// List of arbitrary texture bindings.
        /// </summary>
        public List<GenericMaterialTextureBind> TextureBinds
        {
            get { return _textureBinds; }
            set { _textureBinds = value; }
        }



        /// <summary>
        /// Sets the technique to render with. Return value is always the empty string.
        /// </summary>
        public string Technique
        {
            get { return String.Empty; }
            set
            {
                TechniqueChainEntry entry = new TechniqueChainEntry();
                entry.Name = value;
                entry.Profile = ShaderProfile.PS_1_1;
                _techniqueChain.Clear();
                _techniqueChain.Add(entry);
            }
        }



        /// <summary>
        /// The list of TechniqueChainEntries that is used to select the technique to render with.
        /// </summary>
        public List<TechniqueChainEntry> TechniqueChain
        {
            set { _techniqueChain = value; }
            get { return _techniqueChain; }
        }

        #endregion


        #region Public methods

        public override void Dispose()
        {
            _IsDisposed = true;
            for (int i = 0; i < _textureBinds.Count; i++)
            {
                if (!_textureBinds[i]._texture.IsNull)
                {
                    //_textureBinds[i]._texture.Instance.Dispose();
                    _textureBinds[i]._texture.Invalidate();
                }
            }
            base.Dispose();
        }

        #endregion


        #region Private, protected, internal methods

        protected override string _SetupEffect(SceneRenderState srs, MaterialInstanceData materialData)
        {
            for (int i = 0; i < _textureBinds.Count; i++)
            {
                if ((_textureBinds[i]._texture.IsNull || _textureBinds[i]._texture.IsInvalid) && !string.IsNullOrEmpty(_textureBinds[i].TextureFilename))
                    _textureBinds[i]._texture = ResourceManager.Instance.LoadTexture(_textureBinds[i].TextureFilename);
            }

            if (_techniqueChain.Count < 1)
                return base._SetupEffect(srs, materialData);

            ShaderProfile shaderProfile = srs.Gfx.ShaderProfile;

            TechniqueChainEntry technique = _techniqueChain[0];
            for (int i = 1; i < _techniqueChain.Count; i++)
            {
                if ((technique.Profile > shaderProfile) || (_techniqueChain[i].Profile > technique.Profile && _techniqueChain[i].Profile <= shaderProfile))
                    technique = _techniqueChain[i];
            }

            return technique.Name;
        }



        protected override void _SetupGlobalParameters(SceneRenderState srs, MaterialInstanceData materialData)
        {
            base._SetupGlobalParameters(srs, materialData);

            for (int i = 0; i < _textureBinds.Count; i++)
                EffectManager.SetParameter(_textureBinds[i].Parameter, _textureBinds[i]._texture.Instance);

            for (int i = 0; i < _floatBinds.Count; i++)
                EffectManager.SetParameter(_floatBinds[i].Parameter, _floatBinds[i].Value);

            EffectManager.SetParameter(_sasViewMatrix, srs.View);
            EffectManager.SetParameter(_sasViewInverseMatrix, Matrix.Invert(srs.View));
            EffectManager.SetParameter(_sasViewProjectionMatrix, srs.View * srs.Projection);
            EffectManager.SetParameter(_sasProjectionMatrix, srs.Projection);

            EffectManager.SetParameter(_sasTimeNow, srs.SceneGraph.CurrentTime);
            EffectManager.SetParameter(_sasTimeLast, srs.SceneGraph.LastTime);
            EffectManager.SetParameter(_sasTimeElapsed, srs.SceneGraph.CurrentTime - srs.SceneGraph.LastTime);
            EffectManager.SetParameter(_sasTimeFrameNumber, srs.SceneGraph.CurrentFrame);

            if (_viewportScaleAndOffset != null)
            {
                Viewport view = GFXDevice.Instance.Device.Viewport;
                float width = GFXDevice.Instance.Device.DepthStencilBuffer.Width;
                float height = GFXDevice.Instance.Device.DepthStencilBuffer.Height;

                Vector4 refractview = new Vector4();
                refractview.X = ((float)view.Width / width) * 0.5f;
                refractview.Y = ((float)view.Height / height) * 0.5f;
                refractview.Z = ((float)view.X / width) + refractview.X;
                refractview.W = ((float)view.Y / height) + refractview.Y;
                _viewportScaleAndOffset.SetValue(refractview);
            }

            if (_viewportUVMinMax != null)
            {
                Viewport view = GFXDevice.Instance.Device.Viewport;
                float width = GFXDevice.Instance.Device.DepthStencilBuffer.Width;
                float height = GFXDevice.Instance.Device.DepthStencilBuffer.Height;

                Vector4 uvminmax = new Vector4();
                uvminmax.X = (float)(view.X + 0.5) / width;
                uvminmax.Y = (float)(view.Y + 0.5) / height;
                uvminmax.Z = (float)(view.Width + view.X - 0.5) / width;
                uvminmax.W = (float)(view.Height + view.Y - 0.5) / height;
                _viewportUVMinMax.SetValue(uvminmax);
            }
        }



        protected override void _SetupObjectParameters(RenderInstance renderInstance, SceneRenderState srs)
        {
            base._SetupObjectParameters(renderInstance, srs);

            EffectManager.SetParameter(_sasWorldMatrixCount, 1);
            EffectManager.SetParameter(_sasWorldMatrix, renderInstance.ObjectTransform);
            EffectManager.SetParameter(_sasWorldInverseMatrix, Matrix.Invert(renderInstance.ObjectTransform));
            EffectManager.SetParameter(_sasWorldViewMatrix, renderInstance.ObjectTransform * srs.View);
            EffectManager.SetParameter(_sasWorldViewProjectionMatrix, renderInstance.ObjectTransform * srs.View * srs.Projection);

            List<Light> lights = srs.SceneGraph.GetLights(renderInstance.WorldBox, 8);
            int count = lights.Count;

            EffectManager.SetParameter(_sasLightCount, count);

            for (int i = 0, pi = 0, di = 0; i < count; i++)
            {
                Light light = lights[i];

                _SetParameter(_sasLights.SasLightColor, i, new Vector4(light.AmbientColor, 1.0f));

                if (light is PointLight)
                {
                    _SetParameter(_sasPointLights.SasLightColor, pi, new Vector4(light.DiffuseColor, 1.0f));
                    _SetParameter(_sasPointLights.SasLightRange, pi, 100.0f / light.ConstantAttenuation);
                    _SetParameter(_sasPointLights.SasLightPosition, pi, light.Position);
                    pi++;
                }
                else if (light is DirectionalLight)
                {
                    _SetParameter(_sasDirectionalLights.SasLightColor, di, new Vector4(light.DiffuseColor, 1.0f));
                    _SetParameter(_sasDirectionalLights.SasLightRange, di, 10000000.0f);
                    _SetParameter(_sasDirectionalLights.SasLightPosition, di, light.Position);
                    _SetParameter(_sasDirectionalLights.SasLightDirection, di, (light as DirectionalLight).Direction);
                    di++;
                }
            }
        }



        protected void _SetParameter(EffectParameter param, int index, float data)
        {
            _SetParameter(param, index, new Vector4(data, 0.0f, 0.0f, 0.0f));
        }



        protected void _SetParameter(EffectParameter param, int index, Vector3 data)
        {
            _SetParameter(param, index, new Vector4(data, 0.0f));
        }



        protected void _SetParameter(EffectParameter param, int index, Vector4 data)
        {
            if (param == null)
                return;

            EffectParameter p = param;
            if (param.Elements.Count > 0)
                p = param.Elements.Count > index ? param.Elements[index] : null;

            _SetParameter(p, data);
        }



        protected void _SetParameter(EffectParameter param, Vector4 data)
        {
            if (param == null)
                return;

            switch (param.ColumnCount)
            {
                case 1:
                    param.SetValue(data.X);
                    break;
                case 3:
                    param.SetValue(new Vector3(data.X, data.Y, data.Z));
                    break;
                case 4:
                    param.SetValue(data);
                    break;
            }
        }



        protected override void _LoadParameters()
        {
            base._LoadParameters();

            for (int i = 0; i < _textureBinds.Count; i++)
            {
                if (_textureBinds[i].BindType == BindType.Name)
                    _textureBinds[i].Parameter = EffectManager.GetParameter(Effect, _textureBinds[i].BindAddress);
                else
                    _textureBinds[i].Parameter = EffectManager.GetParameterBySemantic(Effect, _textureBinds[i].BindAddress);
            }

            for (int i = 0; i < _floatBinds.Count; i++)
            {
                if (_floatBinds[i].BindType == BindType.Name)
                    _floatBinds[i].Parameter = EffectManager.GetParameter(Effect, _floatBinds[i].BindAddress);
                else
                    _floatBinds[i].Parameter = EffectManager.GetParameterBySemantic(Effect, _floatBinds[i].BindAddress);
            }

            _LoadLightParameters(_sasLights, "AmbientLight");
            _LoadLightParameters(_sasDirectionalLights, "DirectionalLight");
            _LoadLightParameters(_sasPointLights, "PointLight");

            _sasLightCount = EffectManager.GetParameterBySasBind(Effect, "Sas.NumAmbientLights");
            _sasDirectionalLightCount = EffectManager.GetParameterBySasBind(Effect, "Sas.NumDirectionalLights");
            _sasPointLightCount = EffectManager.GetParameterBySasBind(Effect, "Sas.NumPointLights");

            EffectParameter skel = EffectManager.GetParameterBySasBind(Effect, "Sas.Skeleton");
            if (skel != null)
            {
                _sasWorldMatrix = skel.StructureMembers["MeshToJointToWorld"];
                _sasWorldMatrixCount = skel.StructureMembers["NumJoints"];
            }

            if (_sasWorldMatrix == null)
                _sasWorldMatrix = _GetParameterByDualSasBind("Sas.Skeleton.MeshToJointToWorld[0]", "Sas.Skeleton.MeshToJointToWorld[*]");

            if (_sasWorldMatrixCount == null)
                _sasWorldMatrixCount = EffectManager.GetParameterBySasBind(Effect, "Sas.Skeleton.NumJoints");

            EffectParameter cam = EffectManager.GetParameterBySasBind(Effect, "Sas.Camera");
            if (cam != null)
            {
                _sasViewMatrix = cam.StructureMembers["WorldToView"];
                _sasProjectionMatrix = cam.StructureMembers["Projection"];
            }

            if (_sasViewMatrix == null)
                _sasViewMatrix = EffectManager.GetParameterBySasBind(Effect, "Sas.Camera.WorldToView");
            if (_sasProjectionMatrix == null)
                _sasProjectionMatrix = EffectManager.GetParameterBySasBind(Effect, "Sas.Camera.Projection");

            EffectParameter time = EffectManager.GetParameterBySasBind(Effect, "Sas.Time");
            if (time != null)
            {
                _sasTimeNow = time.StructureMembers["Now"];
                _sasTimeLast = time.StructureMembers["Last"];
                _sasTimeFrameNumber = time.StructureMembers["FrameNumber"];
            }

            if (_sasTimeNow == null)
                _sasTimeNow = EffectManager.GetParameterBySasBind(Effect, "Sas.Time.Now");
            if (_sasTimeLast == null)
                _sasTimeLast = EffectManager.GetParameterBySasBind(Effect, "Sas.Time.Last");
            if (_sasTimeFrameNumber == null)
                _sasTimeFrameNumber = EffectManager.GetParameterBySasBind(Effect, "Sas.Time.FrameNumber");

            if (_sasWorldMatrix == null)
                _sasWorldMatrix = EffectManager.GetParameterBySemantic(Effect, "World");
            if (_sasWorldInverseMatrix == null)
                _sasWorldInverseMatrix = EffectManager.GetParameterBySemantic(Effect, "WorldInverse");
            if (_sasWorldViewMatrix == null)
                _sasWorldViewMatrix = EffectManager.GetParameterBySemantic(Effect, "WorldView");
            if (_sasWorldViewProjectionMatrix == null)
                _sasWorldViewProjectionMatrix = EffectManager.GetParameterBySemantic(Effect, "WorldViewProjection");
            if (_sasViewMatrix == null)
                _sasViewMatrix = EffectManager.GetParameterBySemantic(Effect, "View");
            if (_sasViewInverseMatrix == null)
                _sasViewInverseMatrix = EffectManager.GetParameterBySemantic(Effect, "ViewInverse");
            if (_sasViewProjectionMatrix == null)
                _sasViewProjectionMatrix = EffectManager.GetParameterBySemantic(Effect, "ViewProjection");
            if (_sasProjectionMatrix == null)
                _sasProjectionMatrix = EffectManager.GetParameterBySemantic(Effect, "Projection");
            if (_sasTimeNow == null)
                _sasTimeNow = EffectManager.GetParameterBySemantic(Effect, "Time");
            if (_sasTimeLast == null)
                _sasTimeLast = EffectManager.GetParameterBySemantic(Effect, "LastTime");
            if (_sasTimeElapsed == null)
                _sasTimeElapsed = EffectManager.GetParameterBySemantic(Effect, "ElapsedTime");

            _viewportScaleAndOffset = EffectManager.GetParameterBySemantic(Effect, "ViewportScaleAndOffset");
            _viewportUVMinMax = EffectManager.GetParameterBySemantic(Effect, "ViewportUVMinMax");
        }



        protected void _LoadLightParameters(SasLight light, string bindbase)
        {
            const string sas = "Sas.";
            const string ins = "[*]";
            const string in0 = "[0]";

            string bind_ins = sas + bindbase + ins;
            string bind_in0 = sas + bindbase + in0;

            EffectParameter l = _GetParameterByDualSasBind(bind_in0, bind_ins);
            if (l != null)
            {
                light.SasLightColor = l.StructureMembers["Color"];
                light.SasLightPosition = l.StructureMembers["Position"];
                light.SasLightDirection = l.StructureMembers["Direction"];
                light.SasLightRange = l.StructureMembers["Range"];
            }

            if (light.SasLightColor == null)
                light.SasLightColor = _GetParameterByDualSasBind(bind_in0 + ".Color", bind_ins + ".Color");
            if (light.SasLightPosition == null)
                light.SasLightPosition = _GetParameterByDualSasBind(bind_in0 + ".Position", bind_ins + ".Position");
            if (light.SasLightDirection == null)
                light.SasLightDirection = _GetParameterByDualSasBind(bind_in0 + ".Direction", bind_ins + ".Direction");
            if (light.SasLightRange == null)
                light.SasLightRange = _GetParameterByDualSasBind(bind_in0 + ".Range", bind_ins + ".Range");
        }



        protected EffectParameter _GetParameterByDualSasBind(string first, string second)
        {
            EffectParameter param = EffectManager.GetParameterBySasBind(Effect, first);
            if (param == null)
                param = EffectManager.GetParameterBySasBind(Effect, second);

            return param;
        }



        protected override void _ClearParameters()
        {
            base._ClearParameters();

            for (int i = 0; i < _textureBinds.Count; i++)
                _textureBinds[i].Parameter = null;

            for (int i = 0; i < _floatBinds.Count; i++)
                _floatBinds[i].Parameter = null;

            _sasLights = new SasLight();
            _sasLightCount = null;

            _sasDirectionalLightCount = null;
            _sasDirectionalLights = new SasLight();

            _sasPointLightCount = null;
            _sasPointLights = new SasLight();

            _sasWorldMatrixCount = null;
            _sasWorldMatrix = null;
            _sasWorldInverseMatrix = null;
            _sasWorldViewMatrix = null;
            _sasWorldViewProjectionMatrix = null;
            _sasViewMatrix = null;
            _sasViewInverseMatrix = null;
            _sasViewProjectionMatrix = null;
            _sasProjectionMatrix = null;

            _sasTimeNow = null;
            _sasTimeLast = null;
            _sasTimeElapsed = null;
            _sasTimeFrameNumber = null;

            _viewportScaleAndOffset = null;
            _viewportUVMinMax = null;
        }

        #endregion


        #region Private, protected, internal fields

        SasLight _sasLights = new SasLight();
        EffectParameter _sasLightCount;

        EffectParameter _sasDirectionalLightCount;
        SasLight _sasDirectionalLights = new SasLight();

        EffectParameter _sasPointLightCount;
        SasLight _sasPointLights = new SasLight();

        EffectParameter _sasWorldMatrixCount;
        EffectParameter _sasWorldMatrix;
        EffectParameter _sasWorldInverseMatrix;
        EffectParameter _sasWorldViewMatrix;
        EffectParameter _sasWorldViewProjectionMatrix;
        EffectParameter _sasViewMatrix;
        EffectParameter _sasViewInverseMatrix;
        EffectParameter _sasViewProjectionMatrix;
        EffectParameter _sasProjectionMatrix;

        EffectParameter _sasTimeNow;
        EffectParameter _sasTimeLast;
        EffectParameter _sasTimeElapsed;
        EffectParameter _sasTimeFrameNumber;

        EffectParameter _viewportScaleAndOffset;
        EffectParameter _viewportUVMinMax;

        List<TechniqueChainEntry> _techniqueChain = new List<TechniqueChainEntry>();
        List<GenericMaterialFloatBind> _floatBinds = new List<GenericMaterialFloatBind>();
        List<GenericMaterialTextureBind> _textureBinds = new List<GenericMaterialTextureBind>();

        #endregion
    }
}
