﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DigitalRuby.WeatherMaker
{
    public class WeatherMakerExtensionWaterScript<T> : WeatherMakerExtensionScript<T> where T : MonoBehaviour
    {
        [Tooltip("How much cloud cover reduces specular highlights from directional light coming off the water. 0 for none, higher for more reduction.")]
        [Range(0.0f, 4.0f)]
        public float CloudCoverWaterSpecularPower = 2.0f;

        [Tooltip("The minimum specular power for the water.")]
        [Range(0.0f, 1.0f)]
        public float CloudCoverWaterMinSpecular = 0.0f;

        [Tooltip("How much cloud cover reduces reflections coming off the water. 0 for none, higher for more reduction.")]
        [Range(0.0f, 4.0f)]
        public float CloudCoverWaterReflectionPower = 2.0f;

        [Tooltip("The minimum reflection power for the water.")]
        [Range(0.0f, 1.0f)]
        public float CloudCoverWaterMinReflection = 0.1f;
    }
}
