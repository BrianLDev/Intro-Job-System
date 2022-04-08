using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.Jobs;
using Unity.Collections;
using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;

public class Star : MonoBehaviour {
  // top 3 values are public to make it easy for StarManager to access all star values.
  // TODO: MAKE PRIVATE IF EXTENDING THIS PROJECT INTO SOMETHING BIGGER AND PROTECTION NEEDED
  public float mass;   // TODO: need to sync with mass in rigidbody
  public float3 velocity;
  public float3 gravityForce;
  private Rigidbody rb;
  // job related variables - to be called by StarManager
  private NativeList<float3> distances;
  public CalcDistancesJob calcDistancesJob;
  public ApplyGravityJob applyGravityJob;

  private void Start() {
    velocity = gravityForce = float3.zero;
    rb = GetComponent<Rigidbody>();
    distances = new NativeList<float3>(Allocator.Persistent);
  }

  public void Initialize(float starMass) {
    mass = starMass;
    transform.localScale = Vector3.one * mass;
  }

  private void OnDestroy() {
    distances.Dispose();
  }

  // NOTE: jobs will be called by StarManager since they have List of all stars
  [BurstCompile]
  public struct CalcDistancesJob : IJobParallelForTransform {
    [ReadOnly] public float3 starPos;
    public NativeList<float3> results;
    private float3 dist;

    public void Execute(int i, [ReadOnly] TransformAccess tfm) {
      results[i] = math.distance(starPos, tfm.position);  // check this...will prob cause an error due to list length not defined
    }
  }

  [BurstCompile]
  public struct ApplyGravityJob : IJobParallelForTransform {
    [ReadOnly] public float starMass;
    public float3 gravForce;  // remember to reset gravForce to 0 before running this!
    [ReadOnly] public NativeList<float> masses;
    [ReadOnly] public NativeList<float3> distances;
    [ReadOnly] public float G; // can't initialize value from within struct so the G const has to be passed in

    public void Execute(int i, TransformAccess tfm) {
      gravForce += G * (starMass * masses[i]) / math.pow(distances[i], 2);
      tfm.position += (Vector3)gravForce;
    }
  }
  

}