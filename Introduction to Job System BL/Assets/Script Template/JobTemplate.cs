using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.Jobs;
using Unity.Collections;
using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;

public class JobTemplate : MonoBehaviour {

  // 1) Declare Variables here including NativeContainers
  private int shipCount = 100;
  private NativeArray<Vector3> shipVelocities;
  private NativeArray<Vector3> shipForces;
  private NativeArray<float> shipMasses;
  // 3a) Declare jobHandle, Job
  private JobHandle applyForcesJobHandle;
  private ApplyForcesJob applyForcesJob;

  private void Start() {
    // 2) Initialize data here
    shipVelocities = new NativeArray<Vector3>(shipCount, Allocator.Persistent);
    shipForces = new NativeArray<Vector3>(shipCount, Allocator.Persistent);
    shipMasses = new NativeArray<float>(shipCount, Allocator.Persistent);
  }

  private void Update() {
    // 3) Instantiate / initialize the job, jobhandle and assign the variables within the job
    applyForcesJob = new ApplyForcesJob() {
      velocities = shipVelocities,
      forces = shipForces,
      masses = shipMasses
    };
    // 4) Schdeule the job ("schdule early, complete late")
    applyForcesJobHandle = applyForcesJob.Schedule(shipCount, 8);
  }

  private void LateUpdate() {
    // 5) Ensure completion of all jobs which returns control to main thread.  Assign data to gameObjects, etc as needed
    // 5a) Complete the job ("schedule early, complete late")
    applyForcesJobHandle.Complete();
    // 5b) Assign data to gameObjects, etc as needed
    shipVelocities = applyForcesJob.velocities;
  }

  private void OnDestroy() {
    // 6) Dispose of NativeContainers (IMPORTANT TO AVOID MEMORY LEAKS!)
    shipVelocities.Dispose();
    shipForces.Dispose();
    shipMasses.Dispose();
  }

  
  // 0) The actual job (struct type)
  [BurstCompile]
  private struct ApplyForcesJob : IJobParallelFor { 
    // 0a) Job variables
    public NativeArray<Vector3> velocities;
    public NativeArray<Vector3> forces;
    public NativeArray<float> masses;

    public void Execute(int i) {
      // 0b) Job execution code
      velocities[i] += (masses[i] * velocities[i]);
    }
  }



}