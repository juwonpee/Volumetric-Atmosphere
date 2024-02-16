using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

[ExecuteInEditMode, ImageEffectAllowedInSceneView]
public class CloudsMaster : MonoBehaviour
{
    [Header("Main")]
    public Shader shader;
    Material material;
    [SerializeField] CloudContainer container;
    [SerializeField] WorleyGenerator3D noiseGenerator;
    RenderTexture noiseTexture;

    [Header("Cloud Settings")]
    [SerializeField] float scale;
    [SerializeField] Vector3 offset;
    [SerializeField] int sampleCount;
    [SerializeField] float lightReflectionScale;
    [SerializeField] float lightTransmittanceScale;
    [SerializeField] float colorTransmittanceScale;
    [SerializeField] float densityMinimumThreshold;
    // Cloud stuff

    private void OnRenderImage(RenderTexture source, RenderTexture destination) {
        if (container == null) {
            GameObject container_placeholder = new GameObject("Container");
            container = container_placeholder.AddComponent<CloudContainer>();
            
        }
        if (material == null) {
            material = new Material(shader);
        }
        if (noiseGenerator == null) {
            noiseGenerator = transform.AddComponent<WorleyGenerator3D>();
        }
        if (noiseTexture != noiseGenerator.worleyTexture) {
            noiseTexture = noiseGenerator.worleyTexture;
        }

        // Pass container bounds
        material.SetVector("boundsMin", container.boundsMin);
        material.SetVector("boundsMax", container.boundsMax);
        material.SetTexture("cloudNoise", noiseTexture);
        material.SetInt("sampleCount", sampleCount);
        material.SetFloat("scale", 1/scale);
        material.SetVector("offset", (Vector4)offset);
        material.SetFloat("lightReflectionScale", lightReflectionScale);
        material.SetFloat("lightTransmittanceScale", lightTransmittanceScale);
        material.SetFloat("colorTransmittanceScale", colorTransmittanceScale);
        material.SetFloat("densityMinimumThreshold", densityMinimumThreshold);

        /*
        Blit does the following
        sets _MainTex property on material to the source texture
        sets the render target to the destination texture
        draws a full screen quad
        cpoies the src texture to the dest texture with modifications from the shader
        */
        Graphics.Blit(source, destination, material);
    }

    void Start() {
        // Enable depth texture mode for mobile support
        Camera cam = GetComponent<Camera>();
        cam.depthTextureMode = DepthTextureMode.Depth;
        
        // Generate worley noise
        noiseGenerator.reload = true;

        
        Application.targetFrameRate = 60;
    }
}
