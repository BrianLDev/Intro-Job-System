using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.Jobs;
using Unity.Collections;
using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;
using TMPro;

public class StarManager : MonoBehaviour {
  [SerializeField] private Transform starPrefab;
  [SerializeField] private int starsToSpawn = 500;
  private int maxStars = 20000;
  private Transform newStar;
  // job related variables
  private TransformAccessArray starTfmAccessArray;
  private NativeList<Vector3> starVelocities;
  private NativeList<float> starMasses;
  private JobHandle applyGravityJobHandle, moveStarsJobHandle;
  private ApplyGravityJob applyGravityJob;
  private MoveStarsJob moveStarsJob;


  private void Start() {
    starVelocities = new NativeList<Vector3>(maxStars, Allocator.Persistent);
    starMasses = new NativeList<float>(maxStars, Allocator.Persistent);

    for (int i=0; i<starsToSpawn; i++) {
      Unity.Mathematics.Random rand = new Unity.Mathematics.Random();
      Vector3 pos = new Vector3(rand.NextFloat(), rand.NextFloat(), rand.NextFloat()) * 20;
      SpawnStar(pos);
    }
  }

  private void Update() {

    applyGravityJob = new ApplyGravityJob() {
      // tfmAccessArray = starTfmAccessArray,
      velocities = starVelocities
    };

    moveStarsJob = new MoveStarsJob() {
      velocities = starVelocities,
    };

    if (starTfmAccessArray.length > 0) {
      applyGravityJobHandle = applyGravityJob.Schedule(starTfmAccessArray.length, 8);
      moveStarsJobHandle = moveStarsJob.Schedule(starTfmAccessArray, applyGravityJobHandle);
    }
  }

  private void LateUpdate() {
    // applyGravityJobHandle.Complete();   // not required since moveStars is denepent on this
    moveStarsJobHandle.Complete();
  }

  private void OnDestroy() {
    starTfmAccessArray.Dispose();
    starVelocities.Dispose();
    starMasses.Dispose();
  }

  private void SpawnStar(Vector3 pos, float mass=-1) {
    if (mass < 0)
      mass = new Unity.Mathematics.Random().NextFloat() * 1000;
    newStar = Instantiate(starPrefab, pos, Quaternion.identity).transform;
    newStar.localScale *= mass;
    starTfmAccessArray.Add(newStar);
    starMasses.Add(mass);
    Vector3 vel = new Vector3();
    starVelocities.Add(vel);
  }

  [BurstCompile]
  private struct ApplyGravityJob : IJobParallelFor {
    // [ReadOnly] public NativeList<Transform> transforms;  // TODO: FIX THIS
    public NativeList<Vector3> velocities;
    public NativeList<Vector3> masses;
    Vector3 force;

    public void Execute(int i) {
      // for (int j=0; j<tfmAccessArray.length; j++) {
      //   // TODO: CALCULATE ACTUAL GRAVITY FORCE. THIS IS SIMPLE TEST
      //   force = (tfmAccessArray[j].position - tfmAccessArray[i].position);
      //   force /= force.magnitude;
      //   velocities[i] += force;
      // }
    }
  }
  
  [BurstCompile]
  private struct MoveStarsJob : IJobParallelForTransform { 
    public NativeArray<Vector3> velocities;

    public void Execute(int i, TransformAccess transform) {
      transform.position += velocities[i];
    }
  }


}