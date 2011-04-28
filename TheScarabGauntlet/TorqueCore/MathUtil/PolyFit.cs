//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using GarageGames.Torque.Core;
using Microsoft.Xna.Framework;



namespace GarageGames.Torque.MathUtil
{
    /// <summary>
    /// Methods used for fitting data to polynomial functions.
    /// </summary>
    public class PolyFit
    {
        #region Methods for polynomial curve fitting.

        /// <summary>
        /// Fit a polynomial to the data held in the x and y arrays.  The polynomial will pass
        /// through each point of y at the corresponding point of x.  The resulting polynomial will
        /// be held and poly and can be evaluated with the ComputeData method.
        /// </summary>
        /// <param name="x">Domain array.</param>
        /// <param name="y">Value array.</param>
        /// <param name="poly">Polynomial array.</param>
        public static void FitData(float[] x, float[] y, float[] poly)
        {
            Assert.Fatal(x.Length == y.Length && y.Length == poly.Length, "x,y, and poly arrays must be same length");
            if (x.Length == 0)
                return;

            poly[0] = y[0];
            for (int i = 1; i < x.Length; i++)
            {
                float denom = 1.0f;
                for (int j = 0; j < i; j++)
                    denom *= (x[i] - x[j]);
                float val = _ComputeData(x[i], x, poly, i);
                poly[i] = (y[i] - val) / denom;
            }
        }

        /// <summary>
        /// Fit a polynomial to the data held in the y array.  The polynomial will pass
        /// through each point of y at the equally spaced points between xmin and xmax.
        /// The resulting polynomial will be held and poly and can be evaluated with the 
        /// ComputeData method.
        /// </summary>
        /// <param name="xmin">Minimum of domain.</param>
        /// <param name="xmax">Maximum of domain.</param>
        /// <param name="y">Value array.</param>
        /// <param name="poly">Polynomial array.</param>
        public static void FitData(float xmin, float xmax, float[] y, float[] poly)
        {
            Assert.Fatal(y.Length == poly.Length, "y, and poly arrays must be same length");
            Assert.Fatal(y.Length > 1, "Need at least 2 points to fit data");
            if (y.Length == 0)
                return;

            int N = y.Length;
            float xdelta = (xmax - xmin) / (float)(N - 1);
            poly[0] = y[0];
            for (int i = 1; i < N; i++)
            {
                float denom = 1.0f;
                for (int j = 0; j < i; j++)
                    denom *= xdelta * (float)(i - j);
                float val = _ComputeData(xmin + xdelta * (float)i, xmin, xmax, poly, i);
                poly[i] = (y[i] - val) / denom;
            }
        }

        /// <summary>
        /// Compute polynomial previously created using FitData method.
        /// </summary>
        /// <param name="x0">Value to evaluate at.</param>
        /// <param name="xmin">Minimum of domain.</param>
        /// <param name="xmax">Maximum of domain.</param>
        /// <param name="poly">Polynomial array.</param>
        /// <returns>Value of polynomial at x0.</returns>
        public static float ComputeData(float x0, float xmin, float xmax, float[] poly)
        {
            return _ComputeData(x0, xmin, xmax, poly, poly.Length);
        }

        /// <summary>
        /// Compute polynomial previously created using FitData method.
        /// </summary>
        /// <param name="x0">Value to evaluate at.</param>
        /// <param name="x">Domain array.</param>
        /// <param name="poly">Polynomial array.</param>
        /// <returns>Value of polynomial at x0.</returns>
        public static float ComputeData(float x0, float[] x, float[] poly)
        {
            return _ComputeData(x0, x, poly, x.Length);
        }

        /// <summary>
        /// Compute polynomial previously created using FitData method which
        /// only has 4 source points.  This is a streamlined method for cases
        /// where there are always 4 data points.
        /// </summary>
        /// <param name="x0">Value to evaluate at.</param>
        /// <param name="x">Domain array.</param>
        /// <param name="poly">Polynomial array.</param>
        /// <returns>Value of polynomial at x0.</returns>
        public static float ComputeData4(float x0, float[] x, float[] poly)
        {
            Assert.Fatal(x.Length == poly.Length && x.Length == 4, "x and poly arrays must each be 4 floats long");
            float val = poly[0];
            float mult = x0 - x[0];
            val += poly[1] * mult;
            mult *= x0 - x[1];
            val += poly[2] * mult;
            mult *= x0 - x[2];
            val += poly[3] * mult;
            return val;
        }

        static float _ComputeData(float x0, float xmin, float xmax, float[] poly, int n)
        {
            Assert.Fatal(n <= poly.Length, "partial computation out of range");
            if (poly.Length < 2)
                return 0.0f;

            // clamp the range of x0
            x0 = MathHelper.Clamp(x0, xmin, xmax);

            int N = poly.Length;
            float xdelta = (xmax - xmin) / (float)(N - 1);
            float val = poly[0];
            float mult = 1.0f;
            for (int i = 1; i < n; i++)
            {
                mult *= x0 - (xmin + xdelta * (float)(i - 1));
                val += poly[i] * mult;
            }
            return val;
        }

        static float _ComputeData(float x0, float[] x, float[] poly, int n)
        {
            Assert.Fatal(x.Length == poly.Length, "x and poly arrays must be same length");
            if (x.Length == 0)
                return 0.0f;

            float val = poly[0];
            float mult = 1.0f;
            for (int i = 1; i < n; i++)
            {
                mult *= x0 - x[i - 1];
                val += poly[i] * mult;
            }
            return val;
        }

        #endregion
    }
}
