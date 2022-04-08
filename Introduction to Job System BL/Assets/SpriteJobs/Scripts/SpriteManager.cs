using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.Jobs;
using Unity.Collections;
using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;

// TODO: UPDATE AND EXPAND THIS TO HANDLE COLLISIONS, SEPARATE INTO COLLISION LAYERS, ETC

public class SpriteManager : MonoBehaviour {
  private int maxSprites = 20000;
  private TransformAccessArray spriteTfmAccessArray;
  private NativeList<Vector3> spriteVelocities;

  private JobHandle moveSpritesJobHandle;
  private MoveSpritesJob moveSpritesJob;


  private void Start() {
    spriteVelocities = new NativeList<Vector3>(maxSprites, Allocator.Persistent);
  }

  private void Update() {
    moveSpritesJob = new MoveSpritesJob() {
      velocities = spriteVelocities,
    };
    moveSpritesJobHandle = moveSpritesJob.Schedule(spriteTfmAccessArray);
  }

  private void LateUpdate() {
    moveSpritesJobHandle.Complete();
  }

  private void OnDestroy() {
    spriteTfmAccessArray.Dispose();
    spriteVelocities.Dispose();
  }

  
  [BurstCompile]
  private struct MoveSpritesJob : IJobParallelForTransform { 
    public NativeArray<Vector3> velocities;

    public void Execute(int i, TransformAccess transform) {
      transform.position += velocities[i];
    }
  }



}