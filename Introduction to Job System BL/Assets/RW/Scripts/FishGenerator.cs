using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;    // for NativeArrays
using Unity.Burst;
using UnityEngine.Jobs;
using math = Unity.Mathematics.math;
using random = Unity.Mathematics.Random;

public class FishGenerator : MonoBehaviour
{
    [Header("References")]
    public Transform waterObject;
    public Transform fishPrefab;
    public Transform fishParent;
    // velocities keep track of the velocity of each fish throughout the lifetime of the game, so that you can simulate continuous movement.
    private NativeArray<Vector3> velocities;
    // TransformAccessArray is used in place of a NativeArray of transforms, as you can’t pass reference types between threads. So, Unity provides a TransformAccessArray, which contains the value type information of a transform including its position, rotation and matrices. The added advantage is, any modification you make to an element of the TransformAccessArray will directly impact the transform in the scene.
    private TransformAccessArray transformAccessArray;

    [Header("Spawn Settings")]
    public int amountOfFish;
    public Vector3 spawnBounds;
    public float spawnHeight;
    public int swimChangeFrequency;
    private PositionUpdateJob positionUpdateJob;
    private JobHandle positionUpdateJobHandle;

    [Header("Settings")]
    public float swimSpeed;
    public float turnSpeed;

    private void Start() {
        // Initialize velocities with a persistent allocator of size amountOfFish, which is a pre-declared variable.
        velocities = new NativeArray<Vector3>(amountOfFish, Allocator.Persistent);
        // Initialize transformAccessArray with size amountOfFish.
        transformAccessArray = new TransformAccessArray(amountOfFish);

        // Instantiate the fish gameObjects and populate the transform access array with the fish transforms
        for (int i=0; i<amountOfFish; i++) {
            float distanceX = Random.Range(-spawnBounds.x / 2, spawnBounds.x / 2);
            float distanceZ = Random.Range(-spawnBounds.z / 2, spawnBounds.z / 2);
            // Create a random spawn point within spawnBounds.
            Vector3 spawnPoint = (transform.position + Vector3.up * spawnHeight) + new Vector3(distanceX, 0, distanceZ);
            // Instantiate objectPrefab, which is a fish, at spawnPoint with no rotation.
            Transform t = (Transform)Instantiate(fishPrefab, spawnPoint, Random.rotation, fishParent);
            // Add the instantiated transform to transformAccessArray.
            transformAccessArray.Add(t);
        }
    }

    private void Update() {
        // all the variables within the main thread set the job's data. seed gets the current millisecond from the system time to ensure a different seed for each call.
        positionUpdateJob = new PositionUpdateJob() {
            objectVelocities = velocities,
            jobDeltaTime = Time.deltaTime,
            swimSpeed = this.swimSpeed,
            turnSpeed = this.turnSpeed,
            time = Time.time,
            swimChangeFrequency = this.swimChangeFrequency,
            center = waterObject.position,
            bounds = spawnBounds,
            seed = System.DateTimeOffset.Now.Millisecond
        };

        // schedule positionUpdateJob. Note that each job type has its own Schedule() parameters. A IJobParallelForTransform takes a TransformAccessArray.
        positionUpdateJobHandle = positionUpdateJob.Schedule(transformAccessArray);
    }

    private void LateUpdate() {
        positionUpdateJobHandle.Complete();
    }

    private void OnDrawGizmos() {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireCube(transform.position + Vector3.up * spawnHeight, spawnBounds);
    }

    private void OnDestroy(){ 
        transformAccessArray.Dispose();
        velocities.Dispose();
    }

    [BurstCompile] struct PositionUpdateJob : IJobParallelForTransform {
        public NativeArray<Vector3> objectVelocities;

        public Vector3 bounds;
        public Vector3 center;

        public float jobDeltaTime;
        public float time;
        public float swimSpeed;
        public float turnSpeed;
        public int swimChangeFrequency;

        public float seed;

        // NOTE - this version of Execute also passes a TransformAccess variable
        public void Execute(int i, TransformAccess transform) {
            // Sets the current velocity of the fish.
            Vector3 currentVelocity = objectVelocities[i];
            // Uses Unity's Mathematics library to create a psuedorandom number generator that creates a seed by using the index and system time.
            random randomGen = new random((uint)(i * time + 1 + seed));
            // Moves the transform along its local forward direction, using localToWorldMatrix.
            transform.position += transform.localToWorldMatrix.MultiplyVector(new Vector3(0,0,1)) * swimSpeed * jobDeltaTime * randomGen.NextFloat(0.3f, 1.0f);

            // Rotates the transform in the direction of currentVelocity.
            if (currentVelocity != Vector3.zero) {
                transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(currentVelocity), turnSpeed * jobDeltaTime);
            }

            // Prevent fish out of water
            Vector3 currentPosition = transform.position;

            bool randomize = true;

            // check the position of the transform against the boundaries. If it's outside, the velocity flips towards the center.
            if (currentPosition.x > center.x + bounds.x / 2 ||
                currentPosition.x < center.x - bounds.x / 2 ||
                currentPosition.z > center.z + bounds.z / 2 ||
                currentPosition.z < center.z - bounds.z / 2 )
            {
                Vector3 internalPosition = new Vector3(
                    center.x + randomGen.NextFloat(-bounds.x / 2, bounds.x / 2)/1.3f, 
                    0, 
                    center.z + randomGen.NextFloat(-bounds.z / 2, bounds.z / 2)/1.3f);

                currentVelocity = (internalPosition - currentPosition).normalized;

                objectVelocities[i] = currentVelocity;

                transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(currentVelocity), turnSpeed * jobDeltaTime * 2);

                randomize = false;
            }

            // If the transform is within the boundaries, there's a small possibility that the direction will shift to give the fish a more natural movement.
            if (randomize) {
                if (randomGen.NextInt(0, swimChangeFrequency) <= 2) {
                    objectVelocities[i] = new Vector3(randomGen.NextFloat(-1f, 1f), 0, randomGen.NextFloat(-1f, 1f));
                }
            }

        }
    }
}