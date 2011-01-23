//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;



namespace GarageGames.Torque.Util
{
    /// <summary>
    /// An enum to specify different interpolation modes.
    /// </summary>
    public enum InterpolationMode
    {
        Linear,
        EaseInOut,
        EaseIn,
        EaseOut
    }

    /// <summary>
    /// A helper utility for interpolation.
    /// </summary>
    public class InterpolationHelper
    {
        #region Static methods, fields, constructors

        /// <summary>
        /// Interpolate using the default linear interpolation between two values.
        /// </summary>
        /// <param name="from">The starting value to interpolate from.</param>
        /// <param name="to">The end value to interpolate to.</param>
        /// <param name="delta">The percentage of progress between the two. Ranges from 0.0 to 1.0 are acceptable.</param>
        /// <returns>The current value between from and to based on the delta specified.</returns>
        static public float Interpolate(float from, float to, float delta)
        {
            return _InterpolateLinear(from, to, delta);
        }

        /// <summary>
        /// Interpolate between two values using the specified interpolation mode.
        /// </summary>
        /// <param name="from">The starting value to interpolate from.</param>
        /// <param name="to">The end value to interpolate to.</param>
        /// <param name="delta">The percentage of progress between the two. Ranges from 0.0 to 1.0 are acceptable.</param>
        /// <param name="mode">The interpolation mode to used, specified by the InterpolationMode enum. ex: InterpolationMode.Sigmoid</param>
        /// <returns>The current value between from and to based on the delta specified.</returns>
        static public float Interpolate(float from, float to, float delta, InterpolationMode mode)
        {
            // return the results of different methods based on which mode is specified
            if (mode == InterpolationMode.Linear)
            {
                // use linear for entire interpolation curve
                return _InterpolateLinear(from, to, delta);
            }
            else if (mode == InterpolationMode.EaseInOut)
            {
                // use sigmoid for the entire interpolation curve
                return _InterpolateSigmoid(from, to, delta);
            }
            else if (mode == InterpolationMode.EaseIn)
            {
                // use sigmoid for only the first half of the curve
                if (delta <= 0.5f)
                    return _InterpolateSigmoid(from, to, delta);
                else
                    return _InterpolateLinear(from, to, delta);
            }
            else if (mode == InterpolationMode.EaseOut)
            {
                // use sigmoid for only the last half of the curve
                if (delta >= 0.5f)
                    return _InterpolateSigmoid(from, to, delta);
                else
                    return _InterpolateLinear(from, to, delta);
            }

            // default to linear
            return _InterpolateLinear(from, to, delta);
        }

        static private float _InterpolateLinear(float from, float to, float delta)
        {
            // clamp dela between 0 and 1
            delta = MathHelper.Clamp(delta, 0.0f, 1.0f);

            // calculate resultant interpolation
            return (from * (1.0f - delta)) + (to * delta);
        }

        static private float _InterpolateSigmoid(float from, float to, float delta)
        {
            // avoid looping
            if (delta >= 1.0f)
                return to;

            // expand the range of delta and clamp it between -1 and 1
            delta = MathHelper.Clamp((delta - 0.5f) * 2.0f, -1.0f, 1.0f);

            // calculate interpolator value using sigmoid function
            float sigmoid = MathHelper.Clamp(1.0f / (1.0f + (float)Math.Pow(2.718282f, -15.0f * delta)), 0.0f, 1.0f);

            // calculate resultant interpolation
            return (from * (1.0f - sigmoid)) + (to * sigmoid);
        }

        #endregion
    }
}
