using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.Jobs;
using Unity.Collections;
using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

public class StarManager : MonoBehaviour {
  [SerializeField] private GameObject starPrefab;
  [SerializeField] private int starsToSpawn = 500;
  [SerializeField] private float spawnRadius = 50f;
  private int maxStars = 20000;
  private Star newStar;
  private Transform newStarTfm;
  // job related variables
  private TransformAccessArray starTfmAccessArray;
  private NativeList<float3> starVelocities;
  private NativeList<float> starMasses;
  private NativeList<JobHandle> calcDistsJobHandles;
  private NativeList<JobHandle> applyGravityJobHandles;
  private JobHandle moveStarsJobHandle;
  private MoveStarsJob moveStarsJob;
  Random rand;
  private uint randomSeed = 1851936439U;  // for deterministic testing

  private void Awake() {
    starTfmAccessArray = new TransformAccessArray(maxStars);
    starVelocities = new NativeList<float3>(maxStars, Allocator.Persistent);
    starMasses = new NativeList<float>(maxStars, Allocator.Persistent);
    calcDistsJobHandles = new NativeList<JobHandle>(maxStars, Allocator.Persistent);
    applyGravityJobHandles = new NativeList<JobHandle>(maxStars, Allocator.Persistent);
    // randomSeed = (uint)UnityEngine.Random.Range(1, 9999999);  // uncomment this for non-deterministic random seed
    rand = new Random(randomSeed);
  }

  private void Start() {
    // Spawn stars
    Vector3 pos;
    float mass;
    for (int i=0; i<starsToSpawn; i++) {
      pos = new Vector3(rand.NextFloat()-.5f, rand.NextFloat()-.5f, rand.NextFloat()) * spawnRadius;
      mass = rand.NextFloat();
      SpawnStar(starPrefab, pos, mass);
    }
  }

  private void Update() {

    // TODO: CALC ALL DISTANCES

    // TODO: APPLY ALL GRAVITY

    moveStarsJob = new MoveStarsJob() {
      velocities = starVelocities,
      deltaTime = Time.deltaTime
    };

    if (starTfmAccessArray.length > 0) {
      // TODO: MAKE SURE MOVESTARS HAPPENS AFTER ALL DISTS CALCED AND GRAVITY APPLIED
      moveStarsJobHandle = moveStarsJob.Schedule(starTfmAccessArray);
    }
  }

  private void LateUpdate() {
    moveStarsJobHandle.Complete();
  }

  private void OnDestroy() {
    starTfmAccessArray.Dispose();
    starVelocities.Dispose();
    starMasses.Dispose();
    calcDistsJobHandles.Dispose();
    applyGravityJobHandles.Dispose();
  }

  private void SpawnStar(GameObject prefab, Vector3 pos, float mass) {   
    newStarTfm = Instantiate(prefab, pos, Quaternion.identity, this.transform).transform;
    starTfmAccessArray.Add(newStarTfm);
    newStar = newStarTfm.gameObject.GetComponent<Star>();
    newStar.Initialize(mass);
    starVelocities.Add(new float3(rand.NextFloat()-.5f, rand.NextFloat()-.5f, rand.NextFloat()-.5f) * 2); // TEST
    // starVelocities.Add(newStar.velocity); // TODO: make sure this is getting the reference to edit velocity and not just the value
    // starMasses.Add(newStar.mass);         // TODO: same here
    calcDistsJobHandles.Add(new JobHandle());
    applyGravityJobHandles.Add(new JobHandle());
  }

  
  [BurstCompile]
  private struct MoveStarsJob : IJobParallelForTransform { 
    public NativeArray<float3> velocities;
    public float deltaTime;

    public void Execute(int i, TransformAccess transform) {
      transform.position += (Vector3)velocities[i] * deltaTime;
    }
  }


}