//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using GarageGames.Torque.Core;
using GarageGames.Torque.GFX;
using GarageGames.Torque.Util;
using GarageGames.Torque.XNA;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;



namespace GarageGames.Torque.Materials
{
    /// <summary>
    /// This class provides helper functionality for managing XNA Effect objects.
    /// </summary>
    public class EffectManager
    {

        #region Static methods, fields, constructors

        /// <summary>
        /// Gets a parameter for an effect by the parameter's SAS bind name.
        /// </summary>
        /// <param name="effect">The effect to retrieve the parameter from.</param>
        /// <param name="sasBind">The SAS bind name.</param>
        /// <returns>The parameter.</returns>
        static public EffectParameter GetParameterBySasBind(Resource<Effect> effect, string sasBind)
        {
            foreach (EffectParameter param in effect.Instance.Parameters)
            {
                EffectAnnotation ant = param.Annotations["SasBindAddress"];
                if (ant == null)
                    continue;

                string val = ant.GetValueString();
                if (val == null)
                    continue;

                if (val.Equals(sasBind, StringComparison.OrdinalIgnoreCase))
                    return param;
            }
            return null;
        }



        /// <summary>
        /// Gets a parameter for an effect by the name of the parameter's semantic. The semantic is
        /// the register the parameter is associated (POSITION, TEXCOORD0, etc).
        /// </summary>
        /// <param name="effect">The effect to retrieve the parameter from.</param>
        /// <param name="semantic">The name of the semantic for the parameter to retrieve.</param>
        /// <returns>The parameter.</returns>
        static public EffectParameter GetParameterBySemantic(Resource<Effect> effect, string semantic)
        {
            foreach (EffectParameter param in effect.Instance.Parameters)
            {
                if ((param.Semantic != null) && param.Semantic.Equals(semantic, StringComparison.OrdinalIgnoreCase))
                    return param;
            }
            return null;
        }



        /// <summary>
        /// Gets a parameter for an effect by the name of the parameter. The parameter name is the
        /// same as the variable name in the effect file.
        /// </summary>
        /// <param name="effect">The effect to retrieve the parameter from.</param>
        /// <param name="paramName">The name of the parameter to retrieve.</param>
        /// <returns>The parameter.</returns>
        static public EffectParameter GetParameter(Resource<Effect> effect, string paramName)
        {
            return effect.Instance.Parameters[paramName];
        }



        /// <summary>
        /// Sets the value of an effect parameter, first checking if the parameter is null.
        /// </summary>
        /// <param name="param">The parameter to set.</param>
        /// <param name="value">The value of the parameter.</param>
        static public void SetParameter(EffectParameter param, bool value)
        {
            if (param != null)
                param.SetValue(value);
        }



        /// <summary>
        /// Sets the value of an effect parameter, first checking if the parameter is null.
        /// </summary>
        /// <param name="param">The parameter to set.</param>
        /// <param name="value">The value of the parameter.</param>
        static public void SetParameter(EffectParameter param, bool[] value)
        {
            if (param != null)
                param.SetValue(value);
        }



        /// <summary>
        /// Sets the value of an effect parameter, first checking if the parameter is null.
        /// </summary>
        /// <param name="param">The parameter to set.</param>
        /// <param name="value">The value of the parameter.</param>
        static public void SetParameter(EffectParameter param, int value)
        {
            if (param != null)
                param.SetValue(value);
        }



        /// <summary>
        /// Sets the value of an effect parameter, first checking if the parameter is null.
        /// </summary>
        /// <param name="param">The parameter to set.</param>
        /// <param name="value">The value of the parameter.</param>
        static public void SetParameter(EffectParameter param, int[] value)
        {
            if (param != null)
                param.SetValue(value);
        }



        /// <summary>
        /// Sets the value of an effect parameter, first checking if the parameter is null.
        /// </summary>
        /// <param name="param">The parameter to set.</param>
        /// <param name="value">The value of the parameter.</param>
        static public void SetParameter(EffectParameter param, float value)
        {
            if (param != null)
                param.SetValue(value);
        }



        /// <summary>
        /// Sets the value of an effect parameter, first checking if the parameter is null.
        /// </summary>
        /// <param name="param">The parameter to set.</param>
        /// <param name="value">The value of the parameter.</param>
        static public void SetParameter(EffectParameter param, float[] value)
        {
            if (param != null)
                param.SetValue(value);
        }



        /// <summary>
        /// Sets the value of an effect parameter, first checking if the parameter is null.
        /// </summary>
        /// <param name="param">The parameter to set.</param>
        /// <param name="value">The value of the parameter.</param>
        static public void SetParameter(EffectParameter param, Matrix value)
        {
            if (param != null)
                param.SetValue(value);
        }



        /// <summary>
        /// Sets the value of an effect parameter, first checking if the parameter is null.
        /// </summary>
        /// <param name="param">The parameter to set.</param>
        /// <param name="value">The value of the parameter.</param>
        static public void SetParameter(EffectParameter param, Matrix[] value)
        {
            if (param != null)
                param.SetValue(value);
        }



        /// <summary>
        /// Sets the value of an effect parameter, first checking if the parameter is null.
        /// </summary>
        /// <param name="param">The parameter to set.</param>
        /// <param name="value">The value of the parameter.</param>
        static public void SetParameter(EffectParameter param, Quaternion value)
        {
            if (param != null)
                param.SetValue(value);
        }



        /// <summary>
        /// Sets the value of an effect parameter, first checking if the parameter is null.
        /// </summary>
        /// <param name="param">The parameter to set.</param>
        /// <param name="value">The value of the parameter.</param>
        static public void SetParameter(EffectParameter param, Quaternion[] value)
        {
            if (param != null)
                param.SetValue(value);
        }



        /// <summary>
        /// Sets the value of an effect parameter, first checking if the parameter is null.
        /// </summary>
        /// <param name="param">The parameter to set.</param>
        /// <param name="value">The value of the parameter.</param>
        static public void SetParameter(EffectParameter param, string value)
        {
            if (param != null)
                param.SetValue(value);
        }



        /// <summary>
        /// Sets the value of an effect parameter, first checking if the parameter is null.
        /// </summary>
        /// <param name="param">The parameter to set.</param>
        /// <param name="value">The value of the parameter.</param>
        static public void SetParameter(EffectParameter param, Texture value)
        {
            if (param != null)
                param.SetValue(value);
        }



        /// <summary>
        /// Sets the value of an effect parameter, first checking if the parameter is null.
        /// </summary>
        /// <param name="param">The parameter to set.</param>
        /// <param name="value">The value of the parameter.</param>
        static public void SetParameter(EffectParameter param, Vector2 value)
        {
            if (param != null)
                param.SetValue(value);
        }



        /// <summary>
        /// Sets the value of an effect parameter, first checking if the parameter is null.
        /// </summary>
        /// <param name="param">The parameter to set.</param>
        /// <param name="value">The value of the parameter.</param>
        static public void SetParameter(EffectParameter param, Vector2[] value)
        {
            if (param != null)
                param.SetValue(value);
        }



        /// <summary>
        /// Sets the value of an effect parameter, first checking if the parameter is null.
        /// </summary>
        /// <param name="param">The parameter to set.</param>
        /// <param name="value">The value of the parameter.</param>
        static public void SetParameter(EffectParameter param, Vector3 value)
        {
            if (param != null)
                param.SetValue(value);
        }



        /// <summary>
        /// Sets the value of an effect parameter, first checking if the parameter is null.
        /// </summary>
        /// <param name="param">The parameter to set.</param>
        /// <param name="value">The value of the parameter.</param>
        static public void SetParameter(EffectParameter param, Vector3[] value)
        {
            if (param != null)
                param.SetValue(value);
        }



        /// <summary>
        /// Sets the value of an effect parameter, first checking if the parameter is null.
        /// </summary>
        /// <param name="param">The parameter to set.</param>
        /// <param name="value">The value of the parameter.</param>
        static public void SetParameter(EffectParameter param, Vector4 value)
        {
            if (param != null)
                param.SetValue(value);
        }



        /// <summary>
        /// Sets the value of an effect parameter, first checking if the parameter is null.
        /// </summary>
        /// <param name="param">The parameter to set.</param>
        /// <param name="value">The value of the parameter.</param>
        static public void SetParameter(EffectParameter param, Vector4[] value)
        {
            if (param != null)
                param.SetValue(value);
        }

        #endregion
    }
}
