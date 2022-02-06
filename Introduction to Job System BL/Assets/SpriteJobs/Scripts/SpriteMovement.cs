using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.Jobs;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Collections;    // optimized version of System.Collections built for safe multi-threading. Includes NativeArray, NativeList, NativeQueue, NativeHashMap, NativeMultiHashMap.

public class SpriteMovement : MonoBehaviour {
    // Variables and NativeArrays
    private NativeArray<Vector3> velocities;
    private TransformAccessArray spriteTransformArray;

    private void Start() {
        // Initialize Data
    }

    private void Update() {
        // Create the job and assign all variables within the job
    }

    private void LateUpdate() {
        // Ensure completion of the job and setting vertices
    }

    private void OnDestroy() {
        // Dispose of all NativeArrays
        velocities.Dispose();
        spriteTransformArray.Dispose();
    }

    // IJob struct
    [BurstCompile]
    private struct DoWork : IJob {
        // Job variables here

        public void Execute () {
            // Job execution code
        }
    }

    // IJobParallelFor struct
    [BurstCompile]
    private struct UpdateMeshJob : IJobParallelFor {
        // Job variables here

        public void Execute (int i) {
            // Job execution code
        }
    }

    // IJobParallelForTransform struct
    [BurstCompile]
    struct PositionUpdateJob : IJobParallelForTransform {
        // Job variables here

        // NOTE - this version of Execute also passes a TransformAccess variable
        public void Execute(int i, TransformAccess transformTA) {

        }
    }
}