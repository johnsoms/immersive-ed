﻿using UnityEngine;
using UnityEngine.Profiling;

namespace TrueClouds
{
    public enum Quality
    {
        Low = 0, Medium = 5, High = 10
    }

    [ExecuteInEditMode]
    [RequireComponent(typeof(Camera))]
    public abstract class CloudCamera : MonoBehaviour
    {
        #region PUBLIC_VARIABLES
        #region EDITOR_VARIABLES

        [Tooltip("layers that contains clouds")]
        public LayerMask CloudsMask;
        [Tooltip("layers that block view at clouds")]
        public LayerMask WorldBlockingMask;

        [Range(1, 10)]
        public int ResolutionDivider = 3;
        [Range(1, 4)]
        public int WorldDepthResolutionDivider = 2;

        public bool LateCut = true;
        
        public float BlurRadius = 10;
        public Quality BlurQuality = Quality.High;
        public float LateCutThreshohld = 0.0f;
        public float LateCutPower = 1.5f;

        public bool UseDepthFiltering = false;
        public float DepthFilteringPower = 0f;

        public bool UseNoise = false;
        public Texture2D Noise;
        public Vector3 Wind = new Vector3(2, 1, 3);
        public float NoiseScale = 1;
        public float DepthNoiseScale = 1;
        public float NormalNoisePower = 1;
        public float DepthNoisePower = 1;
        public float DisplacementNoisePower = 1;

        public float NoiseSinTimeScale = .2f;

        public float DistanceToClouds = 10;

        public Transform Light;

        public bool UseRamp = false;

        public Texture Ramp;

        public Color LightColor = Color.white;
        public Color ShadowColor = new Color(0.6f, 0.72f, 0.84f);
        public float LightEnd = 0.75f;

        public float HaloPower = 3;
        public float HaloDistance = 0.5f;

        [Range(0, 10)]
        public float FallbackDistance = 1;
        
        #endregion

        #region DEFAULT_VALUES
        public Shader blurFastShader;
        public Shader blurShader;
        public Shader blurHQShader;

        public Shader depthBlurShader;
        public Shader depthShader;
        public Shader cloudShader;

        public Shader clearColorShader;
        #endregion
        #endregion

        #region PRIVATE_VARIABLES
        private RenderTexture _worldDepth, _cloudDepth;
        private RenderTexture _fromRT, _toRT, _cloudMain, _worldBlit;

        private Material _renderMaterial;
        private Material _blurMaterial;
        private Material _depthBlurMaterial;
        private Material _clearColorMaterial;

        private Camera _camera;
        private Camera _tempCamera;

        private static int LIGHT_DIR_ID;
        private static int LIGHT_POS_ID;
        private static int MAIN_COLOR_ID;
        private static int SHADOW_COLOR_ID;
        private static int LIGHT_END_ID;
        private static int WORLD_DEPTH_ID;
        private static int CAMERA_DEPTH_ID;
        private static int NORMALS_ID;
        private static int RAMP_ID;
        private static int NOISE_ID;
        private static int NORMAL_NOISE_POWER_ID;
        private static int DEPTH_NOISE_POWER_ID;
        private static int DISPLACEMENT_NOISE_POWER_ID;
        private static int NOISE_SIN_TIME_ID;
        private static int NOISE_PARAMS_ID;
        private static int FALLBACK_DIST_ID;
        private static int CAMERA_ROTATION_ID;
        private static int NEAR_PLANE_ID;
        private static int FAR_PLANE_ID;
        private static int CAMERA_DIR_LD;
        private static int CAMERA_DIR_RD;
        private static int CAMERA_DIR_LU;
        private static int CAMERA_DIR_RU;
        private static int HALO_POWER_ID;
        private static int HALO_DISTANCE_ID;
        private static int BLUR_SIZE_ID;
        private static int LATE_CUT_THRESHOLD;
        private static int LATE_CUT_POWER;
        private static int DEPTH_FILTERING_POWER;

        #endregion

        protected virtual void Awake()
        {
            _camera = GetComponent<Camera>();
            GameObject child = new GameObject("cloud camera");
            child.hideFlags = HideFlags.HideAndDontSave;

            child.transform.parent = transform;
            child.transform.localPosition = Vector3.zero;
            child.transform.localRotation = Quaternion.identity;
            _tempCamera = child.AddComponent<Camera>();
            _tempCamera.CopyFrom(_camera);
            _tempCamera.enabled = false;
        }

        private void OnEnable()
        {
            // cleaning old render textures to support script reloading
            // don't worry, they will be set up again in UpdateChangedSettings
            CleanupRenderTextures();
            SetupShaderIDs();
        }
        private void OnDisable()
        {
            CleanupRenderTextures();
        }

        private void SetupShaderIDs()
        {
            LIGHT_DIR_ID = Shader.PropertyToID("_LightDir");
            LIGHT_POS_ID = Shader.PropertyToID("_LightPos");
            MAIN_COLOR_ID = Shader.PropertyToID("_MainColor");
            SHADOW_COLOR_ID = Shader.PropertyToID("_ShadowColor");
            LIGHT_END_ID = Shader.PropertyToID("_LightEnd");
            WORLD_DEPTH_ID = Shader.PropertyToID("_WorldDepth");
            CAMERA_DEPTH_ID = Shader.PropertyToID("_CameraDepth");
            NORMALS_ID = Shader.PropertyToID("_NormalTex");
            RAMP_ID = Shader.PropertyToID("_Ramp");
            NOISE_ID = Shader.PropertyToID("_Noise");
            NOISE_PARAMS_ID = Shader.PropertyToID("_NoiseParams");
            NORMAL_NOISE_POWER_ID = Shader.PropertyToID("_NormalNoisePower");
            NOISE_SIN_TIME_ID = Shader.PropertyToID("_NoiseSinTime");
            DEPTH_NOISE_POWER_ID = Shader.PropertyToID("_DepthNoisePower");
            DISPLACEMENT_NOISE_POWER_ID = Shader.PropertyToID("_DisplacementNoisePower");
            FALLBACK_DIST_ID = Shader.PropertyToID("_FallbackDist");
            CAMERA_ROTATION_ID = Shader.PropertyToID("_CameraRotation");
            NEAR_PLANE_ID = Shader.PropertyToID("_NearPlane");
            FAR_PLANE_ID = Shader.PropertyToID("_FarPlane");
            CAMERA_DIR_LD = Shader.PropertyToID("_CameraDirLD");
            CAMERA_DIR_RD = Shader.PropertyToID("_CameraDirRD");
            CAMERA_DIR_LU = Shader.PropertyToID("_CameraDirLU");
            CAMERA_DIR_RU = Shader.PropertyToID("_CameraDirRU");
            HALO_POWER_ID = Shader.PropertyToID("_HaloPower");
            HALO_DISTANCE_ID = Shader.PropertyToID("_HaloDistance");
            BLUR_SIZE_ID = Shader.PropertyToID("_BlurSize");
            LATE_CUT_THRESHOLD = Shader.PropertyToID("_LateCutThreshohld");
            LATE_CUT_POWER = Shader.PropertyToID("_LateCutPower");
            DEPTH_FILTERING_POWER = Shader.PropertyToID("_DepthFilteringPower");
        }

        private void SetupRenderTextures()
        {
            _cloudMain = GetTemporaryTexture(ResolutionDivider, FilterMode.Bilinear);

            _worldDepth = GetTemporaryTexture(WorldDepthResolutionDivider, FilterMode.Bilinear);
            if (WorldDepthResolutionDivider != 1)
            {
                _worldBlit = GetTemporaryTexture(WorldDepthResolutionDivider, FilterMode.Bilinear);
            }

            _cloudDepth = GetTemporaryTexture(ResolutionDivider, FilterMode.Bilinear);

            _fromRT = GetTemporaryTexture(ResolutionDivider, FilterMode.Bilinear);
            _toRT = GetTemporaryTexture(ResolutionDivider, FilterMode.Bilinear);

            _lastScreenHeight = Screen.height;
            _lastScreenWidth = Screen.width;
        }

        private void CleanupRenderTextures()
        {
            ReleaseTemporaryTexture(ref _cloudMain);

            ReleaseTemporaryTexture(ref _worldDepth);
            ReleaseTemporaryTexture(ref _worldBlit);

            ReleaseTemporaryTexture(ref _cloudDepth);

            ReleaseTemporaryTexture(ref _fromRT);
            ReleaseTemporaryTexture(ref _toRT);

            _lastScreenWidth = -1;
            _lastScreenHeight = -1;
        }

        private void Start()
        {
            _camera.cullingMask &= ~CloudsMask;

            _renderMaterial = new Material(cloudShader);
            switch (BlurQuality)
            {
                case Quality.Low:
                    _blurMaterial = new Material(blurFastShader);
                    break;
                case Quality.Medium:
                    _blurMaterial = new Material(blurShader);
                    break;
                case Quality.High:
                    _blurMaterial = new Material(blurHQShader);
                    break;
            }
            _depthBlurMaterial = new Material(depthBlurShader);

            _clearColorMaterial = new Material(clearColorShader);
        }

        private RenderTexture GetTemporaryTexture(int divider, FilterMode mode)
        {
            RenderTexture res = RenderTexture.GetTemporary(
                Screen.width / divider, 
                Screen.height / divider, 
                16, 
                RenderTextureFormat.ARGB32, 
                RenderTextureReadWrite.Linear);

            return res;
        }

        private void ReleaseTemporaryTexture(ref RenderTexture texture)
        {
            if (texture != null)
            {
                RenderTexture.ReleaseTemporary(texture);
                texture = null;
            }
        }

        protected void RenderClouds(RenderTexture source, RenderTexture destination)
        {
            Profiler.BeginSample("UpdateChangedSettings");
            UpdateChangedSettings();
            Profiler.EndSample();

            Profiler.BeginSample("copy from camera");
            _tempCamera.CopyFrom(_camera);
            _tempCamera.renderingPath = RenderingPath.Forward;
            _tempCamera.enabled = false;
            Profiler.EndSample();

            ApplyBlits(source, destination);
        }

        private void ApplyBlits(RenderTexture source, RenderTexture destination)
        {
            Graphics.Blit(source, destination);

            Profiler.BeginSample("render depth");

            _tempCamera.clearFlags = CameraClearFlags.Color;
            _tempCamera.backgroundColor = Color.white;

            _worldDepth.DiscardContents();
            _tempCamera.targetTexture = _worldDepth;
            _tempCamera.cullingMask = WorldBlockingMask;
            _tempCamera.RenderWithShader(depthShader, "RenderType");

            _cloudDepth.DiscardContents();
            _tempCamera.targetTexture = _cloudDepth;
            _tempCamera.cullingMask = CloudsMask;
            _tempCamera.RenderWithShader(depthShader, "RenderType");

            Profiler.EndSample();

            Profiler.BeginSample("render main");
            _tempCamera.clearFlags = CameraClearFlags.Color;
            _tempCamera.backgroundColor = new Color(0.5f, 0.5f, 0.5f, 0);
            _tempCamera.cullingMask = CloudsMask;

            _cloudMain.DiscardContents();
            _tempCamera.targetTexture = _cloudMain;
            _tempCamera.Render();
            Profiler.EndSample();

            DrawGreyBorder(_cloudMain);

            _tempCamera.enabled = false;

            Profiler.BeginSample("UpdateShaderValues");
            UpdateShaderValues();
            Profiler.EndSample();

            if (LateCut)
            {
                SwapTextures(ref _toRT, ref _cloudMain);
            }
            else
            {
                SwapTextures();
                Graphics.Blit(_cloudMain, _toRT, _renderMaterial, 0);
            }


            if ((LateCut || UseNoise))
            {
                Profiler.BeginSample("Blur Depth");
                // todo
                _depthBlurMaterial.SetTexture(NORMALS_ID, _toRT);

                SwapTextures(ref _fromRT, ref _cloudDepth);
                Graphics.Blit(_fromRT, _cloudDepth, _depthBlurMaterial, 1);

                SwapTextures(ref _fromRT, ref _cloudDepth);
                Graphics.Blit(_fromRT, _cloudDepth, _depthBlurMaterial, 2);
                _renderMaterial.SetTexture(CAMERA_DEPTH_ID, _cloudDepth);
                _blurMaterial.SetTexture(CAMERA_DEPTH_ID, _cloudDepth);
                Profiler.EndSample();
            }

            Profiler.BeginSample("Blur");
            // blur x
            SwapTextures();
            Graphics.Blit(_fromRT, _toRT, _blurMaterial, 0);

            // blur y
            SwapTextures();
            Graphics.Blit(_fromRT, _toRT, _blurMaterial, 1);
            Profiler.EndSample();

            if ((LateCut || UseNoise))
            {
                if (UseNoise)
                { // apply NOISE to depth
                    SwapTextures(ref _fromRT, ref _cloudDepth);
                    Graphics.Blit(_fromRT, _cloudDepth, _depthBlurMaterial, 0);
                    _renderMaterial.SetTexture(CAMERA_DEPTH_ID, _cloudDepth);
                }
                Profiler.EndSample();
            }

            Profiler.BeginSample("CalculateColor");
            // calculate color
            SwapTextures();
            Graphics.Blit(_fromRT, _toRT, _renderMaterial, 1);
            Profiler.EndSample();

            // divide main function and blitting to the screen since it will run for each pixel of the screen.
            Profiler.BeginSample("Final Blit");

            if (!LateCut)
            {
                // blit to screen using a simple alpha-blend shader
                SwapTextures();
                Graphics.Blit(_fromRT, destination, _renderMaterial, 4);
            }
            else if (WorldDepthResolutionDivider != 1)
            {
                // blit to temporary buffer with lower resolution using depth cutting shader without alpha blending
                _worldBlit.DiscardContents();
                SwapTextures();
                Graphics.Blit(_fromRT, _worldBlit, _renderMaterial, 3);
                // then blit to screen using a simple alpha-blend shader
                Graphics.Blit(_worldBlit, destination, _renderMaterial, 4);
            }
            else
            {
                // blit directly to screen
                SwapTextures();
                Graphics.Blit(_fromRT, destination, _renderMaterial, 2);
            }

            Profiler.EndSample();
        }

        private void DrawGreyBorder(RenderTexture texture)
        {
            Graphics.SetRenderTarget(texture);
            _clearColorMaterial.SetPass(0);
            GL.LoadPixelMatrix();
            GL.Color(Color.black);
            GL.Begin(GL.LINE_STRIP);
            GL.Vertex(new Vector3(1, 1));
            GL.Vertex(new Vector3(1, Screen.height));
            GL.Vertex(new Vector3(Screen.width, Screen.height));
            GL.Vertex(new Vector3(Screen.width, 1));
            GL.Vertex(new Vector3(1, 1));
            GL.End();
        }

        private void SwapTextures()
        {
            SwapTextures(ref _fromRT, ref _toRT);
        }

        private void SwapTextures(ref RenderTexture a, ref RenderTexture b)
        {
            a.DiscardContents();

            RenderTexture tmp = a;
            a = b;
            b = tmp;
        }

        private void UpdateShaderValues()
        {
            if (Light != null)
            {
                Vector4 lightDir = -Light.transform.forward;
                lightDir.w = Mathf.Max(0, Vector3.Dot(transform.forward, -Light.transform.forward));
                _renderMaterial.SetVector(LIGHT_DIR_ID, lightDir);

                Vector3 lightPoint = -(transform.worldToLocalMatrix * Light.transform.forward);
                lightPoint.z = -lightPoint.z;
                Vector2 lightPos = _camera.projectionMatrix.MultiplyPoint(lightPoint);
                lightPos = lightPos * 0.5f + new Vector2(.5f, .5f);
                _renderMaterial.SetVector(LIGHT_POS_ID, lightPos);
            }

            if (HaloDistance < 0.01f || HaloPower < 0.01f)
            {
                _renderMaterial.DisableKeyword("HALO_ON");
                _renderMaterial.EnableKeyword("HALO_OFF");
            }
            else
            {
                _renderMaterial.DisableKeyword("HALO_OFF");
                _renderMaterial.EnableKeyword("HALO_ON");
            }

            if (LateCut)
            {
                _renderMaterial.DisableKeyword("EARLY_CUT");
                _renderMaterial.EnableKeyword("LATE_CUT");

                _blurMaterial.DisableKeyword("EARLY_CUT");
                _blurMaterial.EnableKeyword("LATE_CUT");
            }
            else
            {
                _renderMaterial.DisableKeyword("LATE_CUT");
                _renderMaterial.EnableKeyword("EARLY_CUT");

                _blurMaterial.DisableKeyword("LATE_CUT");
                _blurMaterial.EnableKeyword("EARLY_CUT");
            }

            if (UseDepthFiltering)
            {
                _blurMaterial.DisableKeyword("DEPTH_FILTERING_OFF");
                _blurMaterial.EnableKeyword("DEPTH_FILTERING_ON");
            }
            else
            {
                _blurMaterial.DisableKeyword("DEPTH_FILTERING_ON");
                _blurMaterial.EnableKeyword("DEPTH_FILTERING_OFF");
            }

            if (UseNoise)
            {
                _renderMaterial.EnableKeyword("NOISE_ON");
                _renderMaterial.DisableKeyword("NOISE_OFF");
            }
            else
            {
                _renderMaterial.DisableKeyword("NOISE_ON");
                _renderMaterial.EnableKeyword("NOISE_OFF");
            }

            if (UseRamp)
            {
                _renderMaterial.EnableKeyword("RAMP_ON");
                _renderMaterial.DisableKeyword("RAMP_OFF");
            }
            else
            {
                _renderMaterial.DisableKeyword("RAMP_ON");
                _renderMaterial.EnableKeyword("RAMP_OFF");
            }

            _renderMaterial.SetColor(MAIN_COLOR_ID, LightColor);
            if (UseRamp)
            {
                _renderMaterial.SetTexture(RAMP_ID, Ramp);
            }
            else
            {
                _renderMaterial.SetColor(SHADOW_COLOR_ID, ShadowColor);
                _renderMaterial.SetFloat(LIGHT_END_ID, LightEnd);
            }

            _renderMaterial.SetTexture(WORLD_DEPTH_ID, _worldDepth);
            _renderMaterial.SetTexture(CAMERA_DEPTH_ID, _cloudDepth);

            if (UseNoise)
            {
                _renderMaterial.SetTexture(NOISE_ID, Noise);
                _depthBlurMaterial.SetTexture(NOISE_ID, Noise);

                Vector4 noiseParams = new Vector4(-Wind.x, -Wind.y, -Wind.z, 1 / (NoiseScale * DistanceToClouds));
                Vector4 depthNoiseParams = new Vector4(-Wind.x, -Wind.y, -Wind.z, 1 / (DepthNoiseScale * DistanceToClouds));

                _renderMaterial.SetVector(NOISE_PARAMS_ID, noiseParams);
                _depthBlurMaterial.SetVector(NOISE_PARAMS_ID, depthNoiseParams);

                _renderMaterial.SetFloat(NORMAL_NOISE_POWER_ID, NormalNoisePower * 0.3f);
                _renderMaterial.SetFloat(DISPLACEMENT_NOISE_POWER_ID, DisplacementNoisePower * 0.07f * DistanceToClouds);
                _depthBlurMaterial.SetFloat(DEPTH_NOISE_POWER_ID, DepthNoisePower * DistanceToClouds);

                Vector3 sinTime = new Vector3(
                    Mathf.Sin((Time.time * NoiseSinTimeScale          ) * 2 * Mathf.PI),
                    Mathf.Sin((Time.time * NoiseSinTimeScale + 0.3333f) * 2 * Mathf.PI),
                    Mathf.Sin((Time.time * NoiseSinTimeScale + 0.6666f) * 2 * Mathf.PI));

                _depthBlurMaterial.SetVector(NOISE_SIN_TIME_ID, sinTime);
            }

            _renderMaterial.SetFloat(FALLBACK_DIST_ID, FallbackDistance);
            _depthBlurMaterial.SetFloat(FALLBACK_DIST_ID, FallbackDistance);

            _renderMaterial.SetMatrix(CAMERA_ROTATION_ID,    transform.localToWorldMatrix);
            _depthBlurMaterial.SetMatrix(CAMERA_ROTATION_ID, transform.localToWorldMatrix);

            _renderMaterial.SetFloat(NEAR_PLANE_ID, _camera.nearClipPlane);
            _depthBlurMaterial.SetFloat(NEAR_PLANE_ID, _camera.nearClipPlane);
            _renderMaterial.SetFloat(FAR_PLANE_ID, _camera.farClipPlane);
            _blurMaterial.SetFloat(FAR_PLANE_ID, _camera.farClipPlane);
            _depthBlurMaterial.SetFloat(FAR_PLANE_ID, _camera.farClipPlane);

            _blurMaterial.SetFloat(LATE_CUT_THRESHOLD, LateCutThreshohld);
            _blurMaterial.SetFloat(LATE_CUT_POWER, LateCutPower);

            float blurRadiusScaled = BlurRadius;
            if (UseDepthFiltering)
            {
                blurRadiusScaled *= Mathf.Pow(DistanceToClouds / _camera.farClipPlane, DepthFilteringPower);
                _blurMaterial.SetFloat(DEPTH_FILTERING_POWER, DepthFilteringPower);
                _depthBlurMaterial.SetFloat(DEPTH_FILTERING_POWER, DepthFilteringPower);
            }
            _depthBlurMaterial.SetFloat(BLUR_SIZE_ID, blurRadiusScaled);
            _blurMaterial.SetFloat(BLUR_SIZE_ID, blurRadiusScaled);

            Matrix4x4 world2local = transform.worldToLocalMatrix;
            Vector4 
                _CameraDirLD = world2local * Point(_camera.ScreenToWorldPoint(new Vector3(0, 0, 1))),
                _CameraDirRD = world2local * Point(_camera.ScreenToWorldPoint(new Vector3(_camera.pixelWidth, 0, 1))),
                _CameraDirLU = world2local * Point(_camera.ScreenToWorldPoint(new Vector3(0, _camera.pixelHeight, 1))),
                _CameraDirRU = world2local * Point(_camera.ScreenToWorldPoint(new Vector3(_camera.pixelWidth, _camera.pixelHeight, 1)));

            _renderMaterial.SetVector(CAMERA_DIR_LD, _CameraDirLD);
            _depthBlurMaterial.SetVector(CAMERA_DIR_LD, _CameraDirLD);
            _renderMaterial.SetVector(CAMERA_DIR_RD, _CameraDirRD);
            _depthBlurMaterial.SetVector(CAMERA_DIR_RD, _CameraDirRD);
            _renderMaterial.SetVector(CAMERA_DIR_LU, _CameraDirLU);
            _depthBlurMaterial.SetVector(CAMERA_DIR_LU, _CameraDirLU);
            _renderMaterial.SetVector(CAMERA_DIR_RU, _CameraDirRU);
            _depthBlurMaterial.SetVector(CAMERA_DIR_RU, _CameraDirRU);

            _renderMaterial.SetFloat(HALO_POWER_ID, HaloPower);
            _renderMaterial.SetFloat(HALO_DISTANCE_ID, HaloDistance / 2);
        }

        private static Vector4 Point(Vector3 v)
        {
            return new Vector4(v.x, v.y, v.z, 1);
        }

        private int _lastBlurQuality = -1;
        private int _lastResolutionDivider = -1;
        private int _lastWorldResolutionDivider = -1;
        private int _lastScreenWidth = -1;
        private int _lastScreenHeight = -1;
        private void UpdateChangedSettings()
        {
            if (_lastBlurQuality != (int)BlurQuality)
            {
                if (_lastBlurQuality != -1)
                {
                    switch (BlurQuality)
                    {
                        case Quality.Low:
                            _blurMaterial.shader = blurFastShader;
                            break;
                        case Quality.Medium:
                            _blurMaterial.shader = blurShader;
                            break;
                        case Quality.High:
                            _blurMaterial.shader = blurHQShader;
                            break;
                    }
                }
                _lastBlurQuality = (int)BlurQuality;
            }

            if (_lastResolutionDivider != ResolutionDivider || 
                _lastWorldResolutionDivider != WorldDepthResolutionDivider || 
                _lastScreenHeight != Screen.height ||
                _lastScreenWidth != Screen.width)
            {
                CleanupRenderTextures();

                _lastResolutionDivider = ResolutionDivider;
                _lastWorldResolutionDivider = WorldDepthResolutionDivider;

                SetupRenderTextures();
            }
        }
    }

}
