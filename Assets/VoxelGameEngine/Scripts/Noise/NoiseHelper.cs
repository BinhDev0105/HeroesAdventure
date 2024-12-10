using UnityEngine;

namespace VoxelGameEngine
{
    public struct NoiseHelper
    {
        public static float RemapValue(float input, float initialMinimum, float initialMaximum, float minimumOutput, float maximumOutput)
        {

            return minimumOutput + (input - initialMinimum) * (maximumOutput - minimumOutput) / (initialMaximum - initialMinimum);
        }

        public static float RemapValue01(float input, float minimumOutput, float maximumOutput)
        {
            return minimumOutput + (input - 0) * (maximumOutput - minimumOutput) / (1 - 0);
        }

        public static int RemapValue01ToInt(float input, float minimumOutput, float maximumOutput)
        {
            return (int)RemapValue01(input, minimumOutput, maximumOutput);
        }

        //public static float Redistribution(float noise, NoiseSettings settings)
        //{
        //    return Mathf.Pow(noise * settings.redistributionModifier, settings.exponent);
        //}
    }
}
