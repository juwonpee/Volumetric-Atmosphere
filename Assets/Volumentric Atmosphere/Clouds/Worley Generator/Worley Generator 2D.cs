using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class WorleyGenerator : MonoBehaviour
{
    const int threadGroupSize = 8;
    public bool restart = true;
    [SerializeField] ComputeShader worleyShader;
    [SerializeField] RenderTexture worleyTexture;
    [SerializeField] Material worley2DMaterial;

    // Percieved size of texture
    public int resolution;
    // Number of points per layer
    public int numCells;
    // Number of layers
    public float intensity;
    public Vector2[] points;

    // Update is called once per frame
    void Update()
    {
        // Create render texture object
        if (restart) {
            restart = false;

            if (worleyTexture != null) worleyTexture.Release();

            worleyTexture = new RenderTexture(resolution, resolution, 0);
            worleyTexture.enableRandomWrite = true;
            worleyTexture.dimension = UnityEngine.Rendering.TextureDimension.Tex2D;
            worleyTexture.Create();

            // Create points
            points = new Vector2[numCells * numCells];
            for (int x = 0; x < numCells; x++) {
                for (int y = 0; y < numCells; y++) {
                    int index = x*numCells + y;
                    points[index] = new Vector2(Random.Range(0f,1f),Random.Range(0f,1f));
                }
            }
            // Create buffer then send to shader
            ComputeBuffer worleyShaderBuffer = new ComputeBuffer(points.Length, sizeof(float) * 2);
            worleyShaderBuffer.SetData(points);
            worleyShader.SetBuffer(0, "points", worleyShaderBuffer);
        }
        //Pass variables to shader
        worleyShader.SetInt("resolution", resolution);
        worleyShader.SetInt("numCells", numCells);
        worleyShader.SetFloat("intensity", intensity);

        // Execute
        worleyShader.SetTexture(0, "buffer", worleyTexture);
        int numThreadGroups = Mathf.CeilToInt(resolution / (float)threadGroupSize);
        worleyShader.Dispatch(0, numThreadGroups, numThreadGroups, 1);

        worley2DMaterial.SetTexture("_MainTex", worleyTexture);
    }
}
