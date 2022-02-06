using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Jobs;
using Unity.Jobs;
using Unity.Collections;    // optimized version of System.Collections built for safe multi-threading. Includes NativeArray, NativeList, Native Queue, NativeHashMap, NativeMultiHashMap.
using Unity.Burst;
using Unity.Mathematics;

public class WaveGenerator : MonoBehaviour {
#region Variables
    public bool useJobSystem = true;

    [Header("Wave Parameters")]
    public float waveScale;
    public float waveOffsetSpeed;
    public float waveHeight;

    [Header("References and Prefabs")]
    public MeshFilter waterMeshFilter;
    private Mesh waterMesh;

    // JOB SYSTEM VARIABLES
    private NativeArray<Vector3> waterVerticesNA;
    private NativeArray<Vector3> waterNormalsNA;
    /* This JobHandle serves three primary functions:
        - Scheduling a job correctly.  “Schedule Early, Complete Late”
        - Making the main thread wait for a job’s completion.
        - Adding dependencies. Dependencies ensure that a job only starts after another job completes. This prevents two jobs from changing the same data at the same time. It segments the logical flow of your game. */
    private JobHandle meshModificationJobHandle;
    // Reference an UpdateMeshJob (custom struct below) so the entire class can access it.
    private UpdateMeshJob meshModificationJob;
#endregion


#region Methods
    private void Start() {
        waterMesh = waterMeshFilter.mesh;
        // mark the waterMesh as dynamic so Unity can optimize sending vertex changes from the CPU to the GPU.
        waterMesh.MarkDynamic();

        // Initialize waterVertices and waterNormals with the vertices/normals of the waterMesh. Also assign a persistent allocator.
        waterVerticesNA = new NativeArray<Vector3>(waterMesh.vertices, Allocator.Persistent);
        waterNormalsNA = new NativeArray<Vector3>(waterMesh.normals, Allocator.Persistent);
        /* ALLOCATOR TYPES:
            - Temp: Designed for allocations with a lifespan of one frame or less, it has the fastest allocation. It’s not allowed for use in the Job System.
            - TempJob: Intended for allocations with a lifespan of four frames, it offers slower allocation than Temp. Small jobs use them.
            - Persistent: Offers the slowest allocation, but it can last for the entire lifetime of a program. Longer jobs can use this allocation type. (also when you set the NativeArray once and it stays the same without needing to re-initialize after job finishes.) */
    }

    private void Update() {

        if (useJobSystem) {
            // "Schedule early (update), complete late (Late update)"
            // initialize the UpdateMeshJob with all the variables required for the job.
            meshModificationJob = new UpdateMeshJob() {
                vertices = waterVerticesNA,
                normals = waterNormalsNA,
                offsetSpeed = waveOffsetSpeed,
                time = Time.time,
                scale = waveScale,
                height = waveHeight
            };
            // The IJobParallelFor’s Schedule() requires the length of the loop and the batch size. The batch size determines how many segments to divide the work into.  Once scheduled, you cannot interrupt a job.
            meshModificationJobHandle = meshModificationJob.Schedule(waterVerticesNA.Length, 64);
        }
        else {
            UpdateWaterSingleThread();
        }
    }

    private void LateUpdate() {
        if (useJobSystem) {
            // "Schedule early (update), complete late (Late update)"
            
            // Ensures the completion of the job because you can’t get the result of the vertices inside the job before it completes.
            meshModificationJobHandle.Complete();   
            // Unity allows you to directly set the vertices of a mesh from a job. This is a new improvement that eliminates copying the data back and forth between threads.
            waterMesh.SetVertices(meshModificationJob.vertices);
            // recalculate the normals of the mesh so that the lighting interacts with the deformed mesh correctly.
            waterMesh.RecalculateNormals();
        }
    }

    private void OnDestroy() {
        if (useJobSystem) {
            // NativeContainers must be disposed within the lifetime of the allocation. Since you’re using the persistent allocator, it’s sufficient to call Dispose() on OnDestroy().
            waterVerticesNA.Dispose();
            waterNormalsNA.Dispose();
        }
    }

    private void UpdateWaterSingleThread() {
        Vector3 vertex;
        float noiseValue;
        for (int i=0; i<waterMesh.normals.Length; i++) {
            // ensure the wave only affects the vertices facing upwards. This excludes the base of the water.
            if (waterMesh.normals[i].z > 0f) {
                vertex = waterMesh.vertices[i];   // get a reference to the current vertex.
                // sample Simplex noise with scaling and offset transformations.
                noiseValue = NoiseSingleThread(vertex.x * waveScale + waveOffsetSpeed * Time.time, vertex.y * waveScale + waveOffsetSpeed * Time.time);
                // Apply the value of the current vertex within the vertices.
                waterMesh.vertices[i] = new Vector3(vertex.x, vertex.y, noiseValue * waveHeight + 0.3f);
            }
        }
    }

    private float NoiseSingleThread(float x, float y) {
        return noise.snoise(math.float2(x, y));   // note - snoise is actually simplex noise not perlin (similar but improved and not copyrighted)
    }
#endregion

#region Job System struct
    [BurstCompile]
    struct UpdateMeshJob : IJobParallelFor {
        /* JOB TYPES (INTERFACES)
            - IJob: The standard job, which can run in parallel with all the other jobs you’ve scheduled. Used for multiple unrelated operations.
            - IJobParallelFor: All ParallelFor jobs allow you to perform the same independent operation for each element of a native container within a fixed number of iterations. Unity will automatically segment the work into chunks of defined sizes.
            - IJobParallelForTransform: A ParallelFor job type that’s specialized to operate on transforms.
            --- NOTE: each job must be a struct, and must have an Execute() method
        */
        public NativeArray<Vector3> vertices;   // a public NativeArray to read and write vertex data between the job and the main thread.
        
        [ReadOnly] // The [ReadOnly] tag tells the Job System that you only want to read the data from the main thread.
        public NativeArray<Vector3> normals; 

        // These variables control how the Perlin noise function acts. The main thread passes them in.
        public float offsetSpeed;
        public float scale;
        public float height;
        // Note that you cannot access statics such as Time.time within a job. Instead, you pass them in as variables during the job’s initialization.
        public float time;

        private float Noise(float x, float y) {
            float2 pos = math.float2(x, y);
            return noise.snoise(pos);   // note - snoise is actually simplex noise not perlin (similar but improved and not copyrighted)
        }

        public void Execute(int i) {
            // ensure the wave only affects the vertices facing upwards. This excludes the base of the water.
            if (normals[i].z > 0f) {
                var vertex = vertices[i];   // get a reference to the current vertex.
                // sample Simplex noise with scaling and offset transformations.
                float noiseValue = Noise(vertex.x * scale + offsetSpeed * time, vertex.y * scale + offsetSpeed * time);
                // Apply the value of the current vertex within the vertices.
                vertices[i] = new Vector3(vertex.x, vertex.y, noiseValue * height + 0.3f);
            }
        }
    }
#endregion
}