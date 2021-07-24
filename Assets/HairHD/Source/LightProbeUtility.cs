using UnityEngine;
using UnityEngine.Rendering;

namespace Avol
{
    public static class LightProbeUtility
    {
        // Set SH coefficients to MaterialPropertyBlock
        public static void SetSHCoefficients(
            Vector3 position, MaterialPropertyBlock properties
        )
        {
            SphericalHarmonicsL2 sh;
            LightProbes.GetInterpolatedProbe(position, null, out sh);

            // Constant + Linear
            for (var i = 0; i < 3; i++)
                properties.SetVector(_idSHA[i], new Vector4(
                    sh[i, 3], sh[i, 1], sh[i, 2], sh[i, 0] - sh[i, 6] 
                ));

            // Quadratic polynomials
            for (var i = 0; i < 3; i++)
                properties.SetVector(_idSHB[i], new Vector4(
                    sh[i, 4], sh[i, 6], sh[i, 5] * 3, sh[i, 7]
                ));

            // Final quadratic polynomial
            properties.SetVector(_idSHC, new Vector4(
                sh[0, 8], sh[2, 8], sh[1, 8], 1
            ));
        }

        // Set SH coefficients to Material
        public static void SetSHCoefficients(
            Vector3 position, Material material
        )
        {
            SphericalHarmonicsL2 sh;
            LightProbes.GetInterpolatedProbe(position, null, out sh);

            // Constant + Linear
            for (var i = 0; i < 3; i++)
                material.SetVector(_idSHA[i], new Vector4(
                    sh[i, 3], sh[i, 1], sh[i, 2], sh[i, 0] - sh[i, 6]
                ));

            // Quadratic polynomials
            for (var i = 0; i < 3; i++)
                material.SetVector(_idSHB[i], new Vector4(
                    sh[i, 4], sh[i, 6], sh[i, 5] * 3, sh[i, 7]
                ));

            // Final quadratic polynomial
            material.SetVector(_idSHC, new Vector4(
                sh[0, 8], sh[2, 8], sh[1, 8], 1
            ));
        }

        static string[] _idSHA = {
            "a_unity_SHAr",
            "a_unity_SHAg",
            "a_unity_SHAb"
        };

        static string[] _idSHB = {
            "a_unity_SHBr",
            "a_unity_SHBg",
            "a_unity_SHBb"
        };

        static string _idSHC =
            "a_unity_SHC";
    }
}