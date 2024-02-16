using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu]
[Serializable]public class CloudWorleyNoiseSettings : ScriptableObject
{

    public string presetName;
    // Number of points per square
    [Header("Channel 0")]
    [Range(2,100)]public int channel0_numCells = 10;
    public float channel0_intensity = 5;
    [Range(0f, 1f)] public float channel0_coverage = 0;
    [Range(1, 10)] public int channel0_neighborSearchDepth = 1;


    [Header("Channel 1")]
    [Range(2,100)]public int channel1_numCells = 10;
    public float channel1_intensity = 5;
    [Range(0f, 1f)] public float channel1_coverage = 0;
    [Range(1, 10)] public int channel1_neighborSearchDepth = 1;


    [Header("Channel 2")]
    [Range(2,100)]public int channel2_numCells = 10;
    public float channel2_intensity = 5;
    [Range(0f, 1f)] public float channel2_coverage = 0;
    [Range(1, 10)] public int channel2_neighborSearchDepth = 1;

    [Header("Channel 3")]
    [Range(2,100)]public int channel3_numCells = 10;
    public float channel3_intensity = 5;
    [Range(0f, 1f)] public float channel3_coverage = 0;
    [Range(1, 10)] public int channel3_neighborSearchDepth = 1;
}
