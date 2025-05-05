using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Unity.VisualStudio.Editor;
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

    private ComputeBuffer _pointBuffer;
    private RenderTexture _pointTexture;
    private RenderTexture _imageTexture;

    private ComputeShader _stipplerShader; 

    private GameObject _imagePlane;

    private Material _basicTextureMaterial;

    private void OnDisable()
    {
        Debug.Log("OnDisable");
        _pointBuffer?.Release();
        _pointTexture?.Release();
        _imageTexture?.Release(); 

        if (_imagePlane != null){
            DestroyImmediate(_imagePlane);  
        }

        if (_basicTextureMaterial != null){
            DestroyImmediate(_basicTextureMaterial);
        }
    }

    void OnEnable()
    {
        _basicTextureMaterial = new Material(Shader.Find("Standard"));

        _pointTexture = new RenderTexture(_image.width, _image.height, 0, RenderTextureFormat.ARGBFloat);
        _pointTexture.enableRandomWrite = true;
        _pointTexture.Create();

        _imageTexture = new RenderTexture(_image.width, _image.height, 0, RenderTextureFormat.ARGBFloat);
        _imageTexture.enableRandomWrite = true;
        _imageTexture.Create();

        _pointBuffer = new ComputeBuffer(_numDots, sizeof(float) * 2);
        _pointBuffer.SetData(GeneratePoints());

        _stipplerShader = Resources.Load<ComputeShader>("stippler");

        _imagePlane = GameObject.CreatePrimitive(PrimitiveType.Quad);
        _imagePlane.name = "ImagePlane";
        _imagePlane.transform.localScale = new Vector3(-_image.width,  _image.height, 1);
        _imagePlane.transform.position = new Vector3(_image.width * 0.5f, _image.height * 0.5f, 0);

        _imagePlane.GetComponent<MeshRenderer>().material = _basicTextureMaterial;
        _basicTextureMaterial.SetTexture("_MainTex", _image);   

        var zdist = Math.Max(_image.width, _image.height) ; 
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
        if (_instanceMaterial == null || _sphereMesh == null)
        {
            return;
        }

        _instanceMaterial.SetBuffer("_PointBuffer", _pointBuffer);
        _instanceMaterial.SetFloat("_DotSize", _dotSize);

        Graphics.DrawMeshInstancedProcedural(
            _sphereMesh,
            0,
            _instanceMaterial,
            new Bounds(Vector3.zero, new Vector3(_image.width, _image.height, 1)),
            _numDots
        );
    }
}
