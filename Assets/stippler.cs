using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Unity.VisualStudio.Editor;
using UnityEditor;
using UnityEngine;

[ExecuteInEditMode]
public class stippler : MonoBehaviour
{
    [SerializeField]
    private Texture2D _image;

    [SerializeField, Range(0, 10000)]
    private int _numDots;

    [SerializeField, Range(0, 10)]
    private float _dotSize;

    [SerializeField]
    private Material _instanceMaterial;

    [SerializeField]
    private Mesh _sphereMesh;

    [SerializeField, Range(0.0f, 1.0f)]
    private float _blurStrength = 0.5f;

    [SerializeField, Range(0.9f, 1.0f)]
    private float _decay = 0.99f;

    [SerializeField]
    bool _renderPoints = true;
    private ComputeBuffer _pointBuffer;
    private RenderTexture _densityTexture;
    private RenderTexture _imageTexture;
    private RenderTexture _swapTexture;

    [SerializeField]
    private ComputeShader _stipplerShader;

    private GameObject _imagePlane;
    private GameObject _densityPlane;

    private Material _basicTextureMaterial;
    private Material _densityTextureMaterial;

    private Dictionary<string, int> _kernelMap;

    int _densityKernel;
    int _xBlurKernel;
    int _yBlurKernel;
    int _moveParticlesKernel;

    private void OnDisable()
    {
        Debug.Log("OnDisable");
        _pointBuffer?.Release();
        _densityTexture?.Release();
        _imageTexture?.Release();
        _swapTexture?.Release();
        if (_imagePlane != null)
        {
            DestroyImmediate(_imagePlane);
        }

        if (_densityPlane != null)
        {
            DestroyImmediate(_densityPlane);
        }

        if (_basicTextureMaterial != null)
        {
            DestroyImmediate(_basicTextureMaterial);
        }

        if (_densityTextureMaterial != null)
        {
            DestroyImmediate(_densityTextureMaterial);
        }
    }

    GameObject createImagePlane(Texture image, Material material, string name)
    {

        GameObject planeObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
        planeObj.name = name;
        planeObj.transform.localScale = new Vector3(-_image.width, _image.height, 1);
        planeObj.transform.position = new Vector3(_image.width * 0.5f, _image.height * 0.5f, 0);
        planeObj.GetComponent<MeshRenderer>().material = material;
        material.SetTexture("_MainTex", image);
        return planeObj;
    }

    void OnEnable()
    {
        _kernelMap = new Dictionary<string, int>();
        _basicTextureMaterial = new Material(Shader.Find("Standard"));
        _densityTextureMaterial = new Material(Shader.Find("Standard"));

        _densityTexture = new RenderTexture(_image.width, _image.height, 0, RenderTextureFormat.ARGBFloat);
        _densityTexture.enableRandomWrite = true;
        _densityTexture.Create();

        _imageTexture = new RenderTexture(_image.width, _image.height, 0, RenderTextureFormat.ARGBFloat);
        _imageTexture.enableRandomWrite = true;
        _imageTexture.Create();

        _swapTexture = new RenderTexture(_image.width, _image.height, 0, RenderTextureFormat.ARGBFloat);
        _swapTexture.enableRandomWrite = true;
        _swapTexture.Create();

        _pointBuffer = new ComputeBuffer(_numDots, sizeof(float) * 2);
        _pointBuffer.SetData(GeneratePoints());

        _densityKernel = _stipplerShader.FindKernel("WriteDensity");
        _xBlurKernel = _stipplerShader.FindKernel("XBlur");
        _yBlurKernel = _stipplerShader.FindKernel("YBlur");
        _moveParticlesKernel = _stipplerShader.FindKernel("MoveParticles");

        _imagePlane = createImagePlane(_image, _basicTextureMaterial, "imagePlane");
        _densityPlane = createImagePlane(_densityTexture, _densityTextureMaterial, "densityPlane");
        
        _imagePlane.SetActive(false);
        _renderPoints = false;

        var zdist = Math.Max(_image.width, _image.height);  
        Camera.main.transform.position = new Vector3(_image.width * 0.5f, _image.height * 0.5f, -zdist);

    }

    private Vector2[] GeneratePoints()
    {
        var points = new Vector2[_numDots];
        for (int i = 0; i < _numDots; i++)
        {
            points[i] = new Vector2(UnityEngine.Random.Range(0, _image.width), UnityEngine.Random.Range(0, _image.height));
        }
        return points;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            OnDisable();
            OnEnable();
        }

        if (Input.GetKeyDown(KeyCode.Z))
        {
            _imagePlane.SetActive(!_imagePlane.activeSelf);
        }

        if (Input.GetKeyDown(KeyCode.X))
        {
            _densityPlane.SetActive(!_densityPlane.activeSelf);
        }


        if (_instanceMaterial == null || _sphereMesh == null)
        {
            return;
        }

        _stipplerShader.SetBuffer(_densityKernel, "_PointBuffer", _pointBuffer);
        _stipplerShader.SetTexture(_densityKernel, "_DensityTexture", _densityTexture);
        _stipplerShader.Dispatch(_densityKernel, _numDots / 8, 1, 1);

        _stipplerShader.SetFloat("_BlurStrength", _blurStrength);
        _stipplerShader.SetFloat("_Decay", _decay);

        var xDim = Mathf.CeilToInt(_image.width / 8.0f);
        var yDim = Mathf.CeilToInt(_image.height / 8.0f);
    
        _stipplerShader.SetTexture(_xBlurKernel, "_DensityTexture", _densityTexture);
        _stipplerShader.SetTexture(_xBlurKernel, "_ResultTexture", _swapTexture);
        _stipplerShader.Dispatch(_xBlurKernel, xDim, yDim, 1);

        _stipplerShader.SetTexture(_yBlurKernel, "_ResultTexture", _densityTexture);
        _stipplerShader.SetTexture(_yBlurKernel, "_DensityTexture", _swapTexture);
        _stipplerShader.Dispatch(_yBlurKernel, xDim, yDim, 1);

        _instanceMaterial.SetBuffer("_PointBuffer", _pointBuffer);
        _instanceMaterial.SetFloat("_DotSize", _dotSize);

        if (_renderPoints)
        {
            Graphics.DrawMeshInstancedProcedural(
                _sphereMesh,
                0,
                _instanceMaterial,
                new Bounds(Vector3.zero, new Vector3(_image.width, _image.height, 1)),
                _numDots
            );
        }
    }
}
