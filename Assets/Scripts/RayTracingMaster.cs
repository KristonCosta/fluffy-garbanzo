﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RayTracingMaster : MonoBehaviour
{

    public Light DirectionalLight;
    public ComputeShader RayTracingShader;
    public Texture SkyboxTexture;
    public Vector2 SphereRadius = new Vector2(3.0f, 8.0f);
    public uint SpheresMax = 100;
    public float SpherePlacementRadius = 100.0f;
    private ComputeBuffer _sphereBuffer;
    private RenderTexture _target;
    private Camera _camera;
    private int _sphereCount;


    private uint _currentSample = 0;
    private Material _addMaterial;

    private void Awake()
    {
        _camera = GetComponent<Camera>();
    }

    private void OnEnable() 
    {
        _currentSample = 0;
        SetUpScene();
    }

    private void OnDisable() 
    {
        if (_sphereBuffer != null) _sphereBuffer.Release();
    }

    private void SetUpScene()  
    {
        List<Sphere> spheres = new List<Sphere>();
        Sphere _sphere = new Sphere();
        _sphere.position = new Vector3(0.0f, 0.0f, 0.0f);
        _sphere.radius = 1.0f;
        _sphere.albedo = new Vector3(0.5f, 0.5f, 0.5f);
        _sphere.specular = new Vector3(0.5f, 0.5f, 0.5f);
        spheres.Add(_sphere);
    
        for (int i = 0; i < SpheresMax; i++)  
        {
            Sphere sphere = new Sphere(); 
            sphere.radius = SphereRadius.x + Random.value * (SphereRadius.y - SphereRadius.x);
            Vector2 randomPos = Random.insideUnitCircle * SpherePlacementRadius;
            sphere.position = new Vector3(randomPos.x, sphere.radius, randomPos.y);
            bool tooClose = false;
            foreach (Sphere other in spheres) 
            {
                float minDist = sphere.radius + other.radius;
                if (Vector3.SqrMagnitude(sphere.position - other.position) < minDist * minDist)
                {
                    tooClose = true;
                }
            }
            if (tooClose) continue;
            print(sphere.position);
            Color color = Random.ColorHSV();
            bool metal = Random.value < 0.5f;
            if (metal) {
                print("Metal");
            } else {
                print("Not metal");
            }
            sphere.albedo = metal ? Vector3.zero : new Vector3(color.r, color.g, color.b);
            sphere.specular = metal ? new Vector3(color.r, color.g, color.b) : Vector3.one * 0.04f;
            spheres.Add(sphere);
        }
        _sphereBuffer = new ComputeBuffer(spheres.Count, 40);
        _sphereBuffer.SetData(spheres);
        _sphereCount = spheres.Count;
    }

    struct Sphere 
    {
        public Vector3 position;
        public float radius;
        public Vector3 albedo;
        public Vector3 specular;
    }

    private void Update() 
    {
        if (transform.hasChanged) 
        {
            _currentSample = 0;
            transform.hasChanged = false;
        }
        if (DirectionalLight.transform.hasChanged)
        {
            _currentSample = 0; 
            DirectionalLight.transform.hasChanged = false;
        } 
    }

    private void SetShaderParameters()
    {   
        Vector3 l = DirectionalLight.transform.forward;
        RayTracingShader.SetInt("_NumOfSpheres", _sphereCount);
        RayTracingShader.SetBuffer(0, "_Spheres", _sphereBuffer);
        RayTracingShader.SetVector("_DirectionalLight", new Vector4(l.x, l.y, l.z, DirectionalLight.intensity));
        RayTracingShader.SetTexture(0, "_SkyboxTexture", SkyboxTexture);
        RayTracingShader.SetVector("_PixelOffset", new Vector2(Random.value, Random.value));
        RayTracingShader.SetMatrix("_CameraToWorld",
            _camera.cameraToWorldMatrix);
        RayTracingShader.SetMatrix("_CameraInverseProjection",
            _camera.projectionMatrix.inverse);
    }


    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        SetShaderParameters();
        Render(destination);
    }

    private void Render(RenderTexture destination)
    {
        InitRenderTexture();

        RayTracingShader.SetTexture(0, "Result", _target);
        int threadGroupsX = Mathf.CeilToInt(Screen.width / 8.0f);
        int threadGroupsY = Mathf.CeilToInt(Screen.height / 8.0f);
        RayTracingShader.Dispatch(0, threadGroupsX, threadGroupsY, 1);
        if (_addMaterial == null) 
        {
            _addMaterial = new Material(Shader.Find("Hidden/AddShader"));
        }
        _addMaterial.SetFloat("_Sample", _currentSample);
    
        Graphics.Blit(_target, destination, _addMaterial);
        _currentSample++;
    }

    private void InitRenderTexture()
    {
        if (_target == null || _target.width != Screen.width ||
            _target.height != Screen.height)
        {
            if (_target != null)
            {
                _target.Release();
            }

            _target = new RenderTexture(
                Screen.width,
                Screen.height,
                0,
                RenderTextureFormat.ARGBFloat,
                RenderTextureReadWrite.Linear);
            _target.enableRandomWrite = true;
            _target.Create();
            _currentSample = 0;
        }
    }
}
