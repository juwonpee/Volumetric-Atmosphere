using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Scripting;
using Random = UnityEngine.Random;



[ExecuteInEditMode]
public class WorleyGenerator3D : MonoBehaviour
{
	// Method to send buffers to compute shaders
	public static void sendBufferToComputeShader(ref ComputeShader shader, System.Array data, int stride, string bufferName, int kernel = 0) {
		// Let the GC remove the buffer whenever needed
		ComputeBuffer buffer = new ComputeBuffer(data.Length, stride, ComputeBufferType.Raw);
		buffer.SetData(data);
		shader.SetBuffer(kernel, bufferName, buffer);
	}
	
	/*
		4 Independent channels generated worley noise
		Points are generated and passed as a buffer
	*/

	const int threadGroupSize = 4;
	[SerializeField] ComputeShader worleyShader;
	public RenderTexture worleyTexture;


	[Header("Worley Noise Settings")]
	// Percieved size of texture
	public int resolution = 256;
	public CloudWorleyNoiseSettings noiseSettings;

	
	[Header("Viewer settings")]
	public bool reload = true;
	public bool preview = true;
	[Range(0f,1f)] public float depthSelect;
	public enum TextureChannel_t { R=0, G=1, B=2, A=3, All=4 }
	public TextureChannel_t activeChannel;
	public RenderTexture worleyTexturePreview;

	// Preview Worley Texture
	void OnDrawGizmos() {
		if (preview) {
			// Create texture and reference to shader
			worleyTexturePreview = new RenderTexture(resolution, resolution,0) {
				enableRandomWrite = true,
				dimension = UnityEngine.Rendering.TextureDimension.Tex2D
			};
			worleyShader.SetInt("resolution", resolution);
			int kernel = worleyShader.FindKernel("worleyPreview");
			worleyShader.SetTexture(kernel, "bufferPreview", worleyTexturePreview);

			// Execute
			int numThreadGroups = Mathf.CeilToInt(resolution / threadGroupSize);
			worleyShader.Dispatch(kernel, numThreadGroups, numThreadGroups, 1);

			Gizmos.DrawGUITexture(new Rect(10,10,100,100), worleyTexturePreview);
		}
	}

	
	Vector3[] createPoints(int numPoints, float coverage) {
		// Creating points is more complicated than it should be
		/*
		To minimize GPU processing time, the points are layed out in a grid
		The GPU only indexes the nearest neighbour points
		*/
		// Length: Number of points^3

		// Create points first
		Vector3[] points = new Vector3[numPoints*numPoints*numPoints];
		for (int x = 0; x < numPoints; x++) {
			for (int y = 0; y < numPoints; y++) {
				for (int z = 0; z < numPoints; z++) {
					int index = x*numPoints*numPoints + y*numPoints + z;
					// Probability of having a point here
					if (Random.Range(0f, 1f) < coverage) {
						points[index] = new Vector3(Random.Range(0f,1f),Random.Range(0f,1f),Random.Range(0f,1f));
					}
					else {
						points[index] = new Vector3(1f,1f,1f);
					}
				}
			}
		}
		
		return points;
	}
	
	void Setup() {
		reload = true;
	}

	void Update()
	{	
		if (reload) {
			// Performance tracking
			var timer = System.Diagnostics.Stopwatch.StartNew();

			reload = false;

			// Data to send
			int[] numCells = new int[4];
			float[] intensity = new float[4];
			float[] coverage = new float[4];
			int[] neighbourSearchDepth = new int[4];
			List<Vector3> points = new List<Vector3>();
			
			// Copy data for rough
			numCells[0] = noiseSettings.channel0_numCells;
			numCells[1] = noiseSettings.channel1_numCells;
			numCells[2] = noiseSettings.channel2_numCells;
			numCells[3] = noiseSettings.channel3_numCells;

			intensity[0] = noiseSettings.channel0_intensity;
			intensity[1] = noiseSettings.channel1_intensity;
			intensity[2] = noiseSettings.channel2_intensity;
			intensity[3] = noiseSettings.channel3_intensity;

			coverage[0] = noiseSettings.channel0_coverage;
			coverage[1] = noiseSettings.channel1_coverage;
			coverage[2] = noiseSettings.channel2_coverage;
			coverage[3] = noiseSettings.channel3_coverage;
			
			neighbourSearchDepth[0] = noiseSettings.channel0_neighborSearchDepth;
			neighbourSearchDepth[1] = noiseSettings.channel1_neighborSearchDepth;
			neighbourSearchDepth[2] = noiseSettings.channel2_neighborSearchDepth;
			neighbourSearchDepth[3] = noiseSettings.channel3_neighborSearchDepth;
			
			for (int x = 0; x < 4; x++) {
				points.AddRange(createPoints(numCells[x], coverage[x]));
			}

			// Send data to buffer
			worleyShader.SetInt("resolution", resolution);
			sendBufferToComputeShader(ref worleyShader, numCells, sizeof(int), "numCells");
			sendBufferToComputeShader(ref worleyShader, intensity, sizeof(float), "intensity");
			sendBufferToComputeShader(ref worleyShader, neighbourSearchDepth, sizeof(int), "neighborSearchDepth");
			sendBufferToComputeShader(ref worleyShader, points.ToArray(), sizeof(float)*3, "points");


			// Create texture and reference to shader
			worleyTexture = new RenderTexture(resolution, resolution,0) {
				enableRandomWrite = true,
				dimension = UnityEngine.Rendering.TextureDimension.Tex3D,
				volumeDepth = resolution
			};
			int kernel = worleyShader.FindKernel("worleyCompute");
			worleyShader.SetTexture(kernel, "buffer", worleyTexture);

			// Execute
			int numThreadGroups = Mathf.CeilToInt(resolution / threadGroupSize);
			worleyShader.Dispatch(kernel, numThreadGroups, numThreadGroups, numThreadGroups);



			
			// Performance tracking
			Debug.Log("Completed: " + timer.ElapsedMilliseconds + " ms.");
		}
		
	}

	
}
