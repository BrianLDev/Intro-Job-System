/*
 * Copyright (c) 2020 Razeware LLC
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * Notwithstanding the foregoing, you may not use, copy, modify, merge, publish, 
 * distribute, sublicense, create a derivative work, and/or sell copies of the 
 * Software in any work that is designed, intended, or marketed for pedagogical or 
 * instructional purposes related to programming, coding, application development, 
 * or information technology.  Permission for such use, copying, modification,
 * merger, publication, distribution, sublicensing, creation of derivative works, 
 * or sale is expressly withheld.
 *    
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Jobs;
using Unity.Collections;
using Unity.Burst;
using Unity.Jobs;
using System.Threading.Tasks;
using Unity.Mathematics;

public class WaveGenerator : MonoBehaviour
{
    [Header("Wave Parameters")]
    public float waveScale;
    public float waveOffsetSpeed;
    public float waveHeight;

    [Header("References and Prefabs")]
    public MeshFilter waterMeshFilter;
    private Mesh waterMesh;

    //Private Mesh Job Properties
    NativeArray<Vector3> waterVertices;
    NativeArray<Vector3> waterNormals;

    //Job Handles
    UpdateMeshJob meshModificationJob;
    JobHandle meshModificationJobHandle;

    private void Start()
    {
        InitialiseData();   
    }

    //This is where the appropriate mesh verticies are loaded in
    private void InitialiseData()
    {
        waterMesh = waterMeshFilter.mesh;

        //This allows Unity to make background modifications so that it can update the mesh quicker
        waterMesh.MarkDynamic();

        //The verticies will be reused throughout the life of the program so the Allocator has to be set to Persistent
        waterVertices = new NativeArray<Vector3>(waterMesh.vertices, Allocator.Persistent);
        waterNormals = new NativeArray<Vector3>(waterMesh.normals, Allocator.Persistent);
    }

    private void Update()
    {
        //Creating a job and assigning the variables within the Job
        meshModificationJob = new UpdateMeshJob()
        {
            vertices = waterVertices,
            normals = waterNormals,
            offsetSpeed = waveOffsetSpeed,
            time = Time.time,
            scale = waveScale,
            height = waveHeight
        };

        //Setup of the job handle
        meshModificationJobHandle = meshModificationJob.Schedule(waterVertices.Length, 64);
    }

    private void LateUpdate()
    {
        //Ensuring the completion of the job
        meshModificationJobHandle.Complete();

        //Set the vertices directly
        waterMesh.SetVertices(meshModificationJob.vertices);
        
        //Most expensive
        waterMesh.RecalculateNormals();
    }

    private void OnDestroy()
    {
        // make sure to Dispose any NativeArrays when you're done
        waterVertices.Dispose();
        waterNormals.Dispose();
    }

    [BurstCompile]
    private struct UpdateMeshJob : IJobParallelFor
    {
        public NativeArray<Vector3> vertices;
        public NativeArray<Vector3> normals;

        [ReadOnly]
        public float offsetSpeed;

        [ReadOnly]
        public float time;

        [ReadOnly]
        public float scale;

        [ReadOnly]
        public float height;

        public void Execute(int i)
        {
            //Vertex values are always between -1 and 1 (facing partially upwards)
            if (normals[i].z > 0f)
            {
                var vertex = vertices[i];

                float noiseValue = Noise(vertex.x * scale + offsetSpeed * time, vertex.y * scale + offsetSpeed * time);

                vertices[i] = new Vector3(vertex.x , vertex.y, noiseValue * height + 0.3f);
            }
        }

        private float Noise(float x, float y)
        {
            float2 pos = math.float2(x, y);
            return noise.snoise(pos);
        }
    }  
}