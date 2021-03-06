using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.Jobs;
using Unity.Collections;
using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.InputSystem;

// TODO: IMPLEMENT GRID SYSTEM AND USE CELLULAR AUTOMATA INSTEAD OF UNITY 3D SPACE

public class FallingSand : MonoBehaviour {
  [SerializeField] private GameObject sandPrefab;
  [SerializeField] private int sandMax = 10000;
  private float sandMass = 1.0f;
  private Vector3 GRAVITY = new Vector3(0, -9.8f, 0);
  private Mouse mouse;
  private Vector3 mousePos, mouseWorldPos;
  private Camera mainCamera;
  private Transform newSandTfm;
  private TransformAccessArray sandTfmAccessArray;
  private NativeList<Vector3> sandVelocities;
  private NativeList<float> sandMasses;
  private JobHandle applyGravityJobHandle, moveSandJobHandle;
  private ApplyForceJob applyGravityJob;
  private MoveSandJob moveSandJob;

  private void Start() {
    mouse = Mouse.current;
    mainCamera = Camera.main;
    sandTfmAccessArray = new TransformAccessArray(sandMax);
    sandVelocities = new NativeList<Vector3>(sandMax, Allocator.Persistent);
    sandMasses = new NativeList<float>(sandMax, Allocator.Persistent);
  }

  private void Update() {
    // Create new sand particle on mouse click
    // TODO: fix mouse input not matching to screen
    // TODO: separate this out into an input handler script and simply call instantiate sand from there
    if (mouse.leftButton.IsPressed()) {
      // convert mouse pos to world pos
      mousePos = Mouse.current.position.ReadValue();
      mousePos.z = mainCamera.nearClipPlane;
      mouseWorldPos = mainCamera.ScreenToWorldPoint(mousePos);
      mouseWorldPos.z = 0;
      // Instantiate sand and add to NativeLists
      InstantiateSand(mouseWorldPos);
    }

    // set up job: apply gravity
    applyGravityJob = new ApplyForceJob() {
      forceVelocity = GRAVITY,
      velocities = sandVelocities,
      masses = sandMasses,
      deltaTime = Time.deltaTime
    };

    // set up job: move sand
    moveSandJob = new MoveSandJob() {
      velocities = sandVelocities,
      deltaTime = Time.deltaTime
    };

    // schedule jobs
    if (sandTfmAccessArray.length > 0) {
      applyGravityJobHandle = applyGravityJob.Schedule(sandTfmAccessArray.length, 2);
      moveSandJobHandle = moveSandJob.Schedule(sandTfmAccessArray, applyGravityJobHandle);
    }
  }

  private void LateUpdate() {
    // applyGravityJobHandle.Complete();  // shouldn't need to do this since the other job is dependent on it
    moveSandJobHandle.Complete();
  }

  private void OnDestroy() {
    sandTfmAccessArray.Dispose();
    sandVelocities.Dispose();
    sandMasses.Dispose();
  }

  public void InstantiateSand(Vector3 pos) {
      newSandTfm = Instantiate(sandPrefab, mouseWorldPos, Quaternion.identity).transform;
      sandTfmAccessArray.Add(newSandTfm);
      Vector3 velocity = new Vector3();
      sandVelocities.Add(velocity);
      float mass = sandMass;
      sandMasses.Add(mass);
  }

  
  [BurstCompile]
  private struct ApplyForceJob : IJobParallelFor {
    public Vector3 forceVelocity; // note: this is excluding mass
    public NativeArray<Vector3> velocities;
    public NativeArray<float> masses;
    public float deltaTime;

    public void Execute(int i) {
      velocities[i] += (deltaTime * masses[i] * forceVelocity);
    }
  }

  [BurstCompile]
  private struct MoveSandJob : IJobParallelForTransform {
    public NativeArray<Vector3> velocities;
    public float deltaTime;

    public void Execute(int i, TransformAccess transform) {
      if (transform.position.y < 0)
        velocities[i] *= -0.9f;
      transform.position += velocities[i] * deltaTime;

    }
  }

}