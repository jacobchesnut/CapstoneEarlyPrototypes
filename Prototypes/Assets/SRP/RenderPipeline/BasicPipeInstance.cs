using Oculus.Voice.Windows;
using System.Collections;
using System.Collections.Generic;
// - 
using UnityEngine;
using UnityEngine.Rendering; // Import this namespace for rendering supporting functions
using UnityEngine.SceneManagement;
using UnityEngine.XR;
using GUID = System.String;

namespace OpenRT
{
    using GeometryInstanceBuffer = ComputeBuffer; // (float)
    using ObjectLevelAccelerationGeometryBuffer = ComputeBuffer; // (float)
    using ObjectLevelAccelerationGeometryMappingCollectionBuffer = ComputeBuffer; // (int)
    using PrimitiveBuffer = ComputeBuffer; // (Primitive)
    using TopLevelAccelerationBuffer = ComputeBuffer; // (RTBoundingBoxToGPU)
    using TopLevelAccelerationGeometryMappingCollectionBuffer = ComputeBuffer; // (int)
    using WorldToLocalBuffer = ComputeBuffer;   // (Matrix4x4)
    using ISIdx = System.Int32;

    public class BasicPipeInstance : RenderPipeline // Our own renderer should subclass RenderPipeline
    {
        //if true, blocks execution until the dispatch for rendering is complete.
        //this tells how long a frame takes to render
        private const bool MEASURE_DISPATCH_TIME = true;


        private readonly static string s_bufferName = "Ray Tracing Render Camera";


        // -
        private SortedList<ISIdx, GeometryInstanceBuffer> m_gemoetryInstanceBuffer = new SortedList<ISIdx, GeometryInstanceBuffer>();
        private SortedList<ISIdx, ObjectLevelAccelerationGeometryBuffer> m_objectLevelAccGeoBuffers = new SortedList<ISIdx, ObjectLevelAccelerationGeometryBuffer>();
        private SortedList<ISIdx, ObjectLevelAccelerationGeometryMappingCollectionBuffer> m_objectLevelAccGeoMapBuffers = new SortedList<ISIdx, ObjectLevelAccelerationGeometryMappingCollectionBuffer>();
        private PrimitiveBuffer m_primitiveBuffer;
        // -
        private List<ComputeBuffer> m_computeBufferForLights;
        private List<ComputeBuffer> computeBufferForLights
        {
            get
            {
                m_computeBufferForLights = m_computeBufferForLights ?? new List<ComputeBuffer>();
                return m_computeBufferForLights;
            }
        }
        private List<ComputeBuffer> m_computeBufferForMaterials;
        private List<ComputeBuffer> computeBufferForMaterials
        {
            get
            {
                m_computeBufferForMaterials = m_computeBufferForMaterials ?? new List<ComputeBuffer>();
                return m_computeBufferForMaterials;
            }
        }
        private SceneParseResult sceneParseResult;
        private TopLevelAccelerationBuffer m_topLevelAcc;
        private TopLevelAccelerationGeometryMappingCollectionBuffer m_topLevelAccGeoMap;
        // -
        private WorldToLocalBuffer m_worldToPrimitiveBuffer;

        private List<RenderPipelineConfigObject> m_allConfig; // A list of config objects containing all global rendering settings   
        private RenderPipelineConfigObject m_config;

        private Color m_clearColor = Color.black;
        private RenderTexture m_target;
        private RenderTexture m_lowResTextures;
        //variables for TAA
        private RenderTexture[] m_TAATextures;
        private int m_CurrentTAAFrame = 0;
        private int m_MaxTAAFrame = 3;
        private float m_TAAWeightFactor = 0.9f;
        //private RenderTexture m_half;
        //private RenderTexture m_quarter;
        //private RenderTexture m_eighth;

        private ComputeShader m_mainShader;
        private CommandBuffer commands;
        private ComputeBuffer empty;
        private int kIndex = 0;

        private ComputeBuffer m_lightInfoBuffer;

        private Material m_blurMaterial = null; //material for blur post process (set on create)

        //additional for if we need to only be rendering for one frame (currently commented out below)
        private bool onlyOnce = false;
        //additional for not constantly reloading material buffer
        private bool reloadMaterials = true;
        private bool clearMaterials = false;

        private bool disableRendering = false;

        public BasicPipeInstance(Color clearColor, ComputeShader mainShader, List<RenderPipelineConfigObject> allConfig, Material BlurMaterial)
        {
            m_clearColor = clearColor;
            m_mainShader = mainShader;
            m_allConfig = allConfig;
            m_blurMaterial = BlurMaterial;

            if (m_mainShader == null)
            {
                Debug.LogError("Main Shader is gone");
                return;
            }
            commands = new CommandBuffer { name = s_bufferName };

            kIndex = mainShader.FindKernel("CSMain");

            m_config = m_allConfig[0];

        }

        //tells us to reload the buffers for materials, call if something has changed
        public void reloadMaterialsBuffers()
        {
            clearMaterials = true;
        }

        protected override void Render(ScriptableRenderContext renderContext, Camera[] cameras)
        {
            return; //here to satisfy abstract class
        }

        public void setMaxTAAFrames(int frames)
        {
            m_MaxTAAFrame = frames;
        }

        public void setTAAWeight(float weight)
        {
            m_TAAWeightFactor = weight;
        }

        //removed override to make public
        public void Render(ScriptableRenderContext renderContext, Camera[] cameras, RenderTexture[] texToWriteTo, bool pPressed, ShaderFoveatedInfo foveatedInfo, bool runNoFoveation, bool runOnlyOneSample) // This is the function called every frame to draw on the screen
        {
            string timeElapsed;
            GlobalTimer.StartStopwatch(1);
            onlyOnce = pPressed;
            m_mainShader.SetBool("_runNoFoveated", runNoFoveation);
            m_mainShader.SetBool("_onlyOneSample", runOnlyOneSample);
            m_mainShader.SetBool("_UseTAAObjectID", foveatedInfo._UseTAAObjectID);

            disableRendering = foveatedInfo._DisableRendering;


            if (m_mainShader == null)
            {
                return;
            }
            GlobalTimer.StartStopwatch(2);
            RunParseScene();
            timeElapsed = GlobalTimer.EndStopwatch(2);
            Debug.Log("Time to parse scene is " + timeElapsed);
            //time loading buffers
            GlobalTimer.StartStopwatch();
            GlobalTimer.StartStopwatch(2);
            RunLoadGeometryToBuffer(sceneParseResult,
                                    ref m_topLevelAcc,
                                    ref m_objectLevelAccGeoBuffers,
                                    ref m_objectLevelAccGeoMapBuffers,
                                    ref m_primitiveBuffer,
                                    ref m_worldToPrimitiveBuffer,
                                    ref m_gemoetryInstanceBuffer,
                                    ref m_topLevelAccGeoMap);
            timeElapsed = GlobalTimer.EndStopwatch(2);
            Debug.Log("Time to load geometry buffer is " + timeElapsed);
            GlobalTimer.StartStopwatch(2);
            if (reloadMaterials)
            {
                RunLoadMaterialToBuffer(computeBufferForMaterials,
                                        sceneParseResult,
                                        ref m_mainShader);
                reloadMaterials = false;
            }
            timeElapsed = GlobalTimer.EndStopwatch(2);
            Debug.Log("Time to load material buffer is " + timeElapsed);
            GlobalTimer.StartStopwatch(2);
            RunLoadLightToBuffer(computeBufferForLights,
                                 sceneParseResult,
                                 ref m_lightInfoBuffer,
                                 ref m_mainShader);
            timeElapsed = GlobalTimer.EndStopwatch(2);
            Debug.Log("Time to load light buffer is " + timeElapsed);
            timeElapsed = GlobalTimer.EndStopwatch();
            Debug.Log("Time to load buffers is " + timeElapsed);
            GlobalTimer.StartStopwatch(2);
            RunSetAmbientToMainShader(m_config);
            RunSetMissShader(m_mainShader, m_config);
            RunSetRayGenerationShader(m_config.rayGenId);
            RunSetGeometryInstanceToMainShader(sceneParseResult.Primitives.Count,
                                               ref m_topLevelAcc,
                                               ref m_objectLevelAccGeoBuffers,
                                               ref m_objectLevelAccGeoMapBuffers,
                                               ref m_primitiveBuffer,
                                               ref m_worldToPrimitiveBuffer,
                                               CustomShaderDatabase.Instance.ShaderNameList(EShaderType.Intersect),
                                               ref m_gemoetryInstanceBuffer,
                                               ref m_topLevelAccGeoMap);
            RunSetLightsToMainShader(sceneParseResult.Lights.Count, ref m_lightInfoBuffer);
            timeElapsed = GlobalTimer.EndStopwatch(2);
            Debug.Log("Time to load variables is " + timeElapsed);

            int i = 0; //iterator for going through each camera's render texture copy here
            foreach (var camera in cameras)
            {
                GlobalTimer.StartStopwatch(2);
                RunTargetTextureInit(ref m_target,ref m_lowResTextures, ref m_TAATextures, camera);
                RunClearCanvas(commands, camera);
                RunSetCameraToMainShader(camera, i);
                RunRayTracing(ref commands, m_target, m_lowResTextures);
                RunSetFoveatedVariables(foveatedInfo, i);
                RunSetBlurVariables(foveatedInfo, i);
                timeElapsed = GlobalTimer.EndStopwatch(2);
                Debug.Log("Time to load for " + camera.name + " is " + timeElapsed);
                RunSendTextureToUnity(commands, m_target, renderContext, camera, texToWriteTo[i], runNoFoveation);
                i++;
            }

            m_CurrentTAAFrame++;
            if(m_CurrentTAAFrame >= m_MaxTAAFrame)
            {
                m_CurrentTAAFrame = 0;
            }

            //render to VR headset
            //int vrPCount = m_displaySubsystem.GetRenderPassCount();
            //Debug.Log("vr render pass count is: " + vrPCount);
            GlobalTimer.StartStopwatch(2);
            RunBufferCleanUp();
            timeElapsed = GlobalTimer.EndStopwatch(2);
            Debug.Log("Time to clean up buffer is " + timeElapsed);
            timeElapsed = GlobalTimer.EndStopwatch(1);
            Debug.Log("Time for full rendering is " + timeElapsed);
        }

        private void RunSetBlurVariables(ShaderFoveatedInfo foveatedInfo, int camNumber)
        {
            m_blurMaterial.SetFloat("_widthPix", TryCreateJoePipeline.RENDER_TEXTURE_WIDTH);
            m_blurMaterial.SetFloat("_heightPix", TryCreateJoePipeline.RENDER_TEXTURE_HEIGHT);
            m_blurMaterial.SetVector("_frustumVector", foveatedInfo._frustumVector[camNumber]);
            m_blurMaterial.SetVector("_viewVector", foveatedInfo._viewVector[camNumber]);
            if(camNumber == 0)
            {
                m_blurMaterial.SetFloat("_offsetAngleX", -11f); //assume left eye
            }
            else
            {
                m_blurMaterial.SetFloat("_offsetAngleX", 11f);
            }
            m_blurMaterial.SetFloat("_offsetAngleY", 10f);
        }

        private void RunSetFoveatedVariables(ShaderFoveatedInfo foveatedInfo, int camNumber)
        {

            m_mainShader.SetVector("_frustumVector", foveatedInfo._frustumVector[camNumber]);
            m_mainShader.SetVector("_viewVector", foveatedInfo._viewVector[camNumber]);
            //m_mainShader.SetFloat("_innerAngleMax", Mathf.Deg2Rad * foveatedInfo._innerAngleMax);
            m_mainShader.SetBool("_showTint", foveatedInfo._showTint);
            m_mainShader.SetBool("_showOverlay", foveatedInfo._showOverlay);
            m_mainShader.SetFloat("_debugRegionBorderSize", foveatedInfo._debugRegionBorderSize);
            m_mainShader.SetFloat("_weightDecreaseFactor", m_TAAWeightFactor);
            m_mainShader.SetInt("_totalNoTAAFrames", m_MaxTAAFrame);
            m_mainShader.SetInt("_temporalFramePosition", m_CurrentTAAFrame);

            //calibration info
            m_mainShader.SetFloat("_yOffset", foveatedInfo._YOffset);
            m_mainShader.SetFloat("_boundaryAngleMax", foveatedInfo._BorderAngle);

            if (camNumber == 0)
            {
                //left eye
                m_mainShader.SetTexture(0, "_PastTexture", m_TAATextures[0]);
                //calibration info
                m_mainShader.SetFloat("_xOffset", foveatedInfo._XOffset);
                m_mainShader.SetFloat("_innerAngleMax", foveatedInfo._FirstQualityOffsetLeft);
                m_mainShader.SetFloat("_secondAngleMax", foveatedInfo._SecondQualityOffsetLeft);
                m_mainShader.SetFloat("_thirdAngleMax", foveatedInfo._ThirdQualityOffsetLeft);
            }
            else
            {
                //right eye
                m_mainShader.SetTexture(0, "_PastTexture", m_TAATextures[1]);
                //calibration info
                m_mainShader.SetFloat("_xOffset", -foveatedInfo._XOffset);
                m_mainShader.SetFloat("_innerAngleMax", foveatedInfo._FirstQualityOffsetRight);
                m_mainShader.SetFloat("_secondAngleMax", foveatedInfo._SecondQualityOffsetRight);
                m_mainShader.SetFloat("_thirdAngleMax", foveatedInfo._ThirdQualityOffsetRight);
            }

        }

        private void RunLoadMaterialToBuffer(List<ComputeBuffer> computeShadersForMaterials,
                                             SceneParseResult sceneParseResult,
                                             ref ComputeShader mainShader)
        {
            sceneParseResult.ClearAllMaterials();

            PipelineMaterialToBuffer.MaterialsToBuffer(computeShadersForMaterials,
                                                       sceneParseResult,
                                                       ref mainShader);
        }

        private void RunParseScene()
        {
            var scene = SceneManager.GetActiveScene();

            sceneParseResult = SceneParser.Instance.ParseScene(scene);
        }

        public void ReloadGeometry()
        {
            SceneParser.Instance.reloadGeom = true;
        }

        private void RunSetMissShader(ComputeShader shader, RenderPipelineConfigObject m_config)
        {
            shader.SetTexture(kIndex, "_SkyboxTexture", m_config.skybox);
        }

        private void RunSecondaryRayStack(ref ComputeShader shader, ComputeBuffer secondaryRayBuffer)
        {
            shader.SetBuffer(kIndex, "_secondaryRayStack", secondaryRayBuffer);
        }

        private void RunTargetTextureInit(ref RenderTexture targetTexture, ref RenderTexture lowResTextures, ref RenderTexture[] TAATextures, Camera sampleCam)
        {
            //if (targetTexture == null || targetTexture.width != sampleCam.scaledPixelWidth || targetTexture.height != sampleCam.scaledPixelHeight)
            if (targetTexture == null || targetTexture.width != TryCreateJoePipeline.RENDER_TEXTURE_WIDTH || targetTexture.height != TryCreateJoePipeline.RENDER_TEXTURE_HEIGHT)
            {
                // Release render texture if we already have one
                if (targetTexture != null)
                {
                    targetTexture.Release();
                }

                // Get a render target for Ray Tracing

                //targetTexture = new RenderTexture(sampleCam.scaledPixelWidth, sampleCam.scaledPixelHeight, 0,
                //    RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
                targetTexture = new RenderTexture(TryCreateJoePipeline.RENDER_TEXTURE_WIDTH, TryCreateJoePipeline.RENDER_TEXTURE_HEIGHT, 0,
                    RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);

                //targetTexture = new RenderTexture(UnityEngine.XR.XRSettings.eyeTextureDesc);
                targetTexture.enableRandomWrite = true;
                targetTexture.Create();

                //recreate low res texture array as well
                if(lowResTextures != null)
                {
                    lowResTextures.Release();
                }

                //lowResTextures = new Texture2DArray(TryCreateJoePipeline.RENDER_TEXTURE_WIDTH, TryCreateJoePipeline.RENDER_TEXTURE_HEIGHT, 3, TextureFormat.RGBA32, 1, false);
                lowResTextures = new RenderTexture(TryCreateJoePipeline.RENDER_TEXTURE_WIDTH, TryCreateJoePipeline.RENDER_TEXTURE_HEIGHT, 32, 
                    RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
                lowResTextures.enableRandomWrite = true;
                lowResTextures.volumeDepth = 4;
                lowResTextures.dimension = TextureDimension.Tex2DArray;
                lowResTextures.Create();

            }
            if(TAATextures == null || TAATextures[0].volumeDepth / 2 != m_MaxTAAFrame)
            {
                if(TAATextures != null)
                {
                    for(int i = 0; i < TAATextures.Length; i++)
                    {
                        TAATextures[i].Release();
                    }
                }

                TAATextures = new RenderTexture[2]; //just left and right eye
                for(int i = 0; i < 2; i++)
                {
                    TAATextures[i] = new RenderTexture(TryCreateJoePipeline.RENDER_TEXTURE_WIDTH, TryCreateJoePipeline.RENDER_TEXTURE_HEIGHT, 32,
                    RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
                    TAATextures[i].enableRandomWrite = true;
                    TAATextures[i].volumeDepth = m_MaxTAAFrame * 2; //2X TAA frames to hold object ID data
                    TAATextures[i].dimension = TextureDimension.Tex2DArray;
                    TAATextures[i].Create();
                }
            }

            
            

            /*
            //now create at half resolution for ray sharing
            if (halfTexture == null || halfTexture.width != TryCreateJoePipeline.RENDER_TEXTURE_WIDTH/2 || halfTexture.height != TryCreateJoePipeline.RENDER_TEXTURE_HEIGHT/2)
            {
                // Release render texture if we already have one
                if (halfTexture != null)
                {
                    halfTexture.Release();
                }

                // Get a render target for Ray Sharing

                halfTexture = new RenderTexture(TryCreateJoePipeline.RENDER_TEXTURE_WIDTH/2, TryCreateJoePipeline.RENDER_TEXTURE_HEIGHT/2, 0,
                    RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);

                //targetTexture = new RenderTexture(UnityEngine.XR.XRSettings.eyeTextureDesc);
                halfTexture.enableRandomWrite = true;
                halfTexture.Create();
            }

            //now create at quarter resolution for ray sharing
            if (quarterTexture == null || quarterTexture.width != TryCreateJoePipeline.RENDER_TEXTURE_WIDTH / 4 || quarterTexture.height != TryCreateJoePipeline.RENDER_TEXTURE_HEIGHT / 4)
            {
                // Release render texture if we already have one
                if (quarterTexture != null)
                {
                    quarterTexture.Release();
                }

                // Get a render target for Ray Sharing

                quarterTexture = new RenderTexture(TryCreateJoePipeline.RENDER_TEXTURE_WIDTH / 4, TryCreateJoePipeline.RENDER_TEXTURE_HEIGHT / 4, 0,
                    RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);

                //targetTexture = new RenderTexture(UnityEngine.XR.XRSettings.eyeTextureDesc);
                quarterTexture.enableRandomWrite = true;
                quarterTexture.Create();
            }

            //now create at eighth resolution for ray sharing
            if (eighthTexture == null || eighthTexture.width != TryCreateJoePipeline.RENDER_TEXTURE_WIDTH / 8 || eighthTexture.height != TryCreateJoePipeline.RENDER_TEXTURE_HEIGHT / 8)
            {
                // Release render texture if we already have one
                if (eighthTexture != null)
                {
                    eighthTexture.Release();
                }

                // Get a render target for Ray Sharing

                eighthTexture = new RenderTexture(TryCreateJoePipeline.RENDER_TEXTURE_WIDTH / 8, TryCreateJoePipeline.RENDER_TEXTURE_HEIGHT / 8, 0,
                    RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);

                //targetTexture = new RenderTexture(UnityEngine.XR.XRSettings.eyeTextureDesc);
                eighthTexture.enableRandomWrite = true;
                eighthTexture.Create();
            }
            */
        }

        private void RunClearCanvas(CommandBuffer buffer, Camera camera)
        {
            CameraClearFlags clearFlags = camera.clearFlags; // Each camera can config its clear flag to determine what should be shown if nothing can be seen by the camera
            //buffer.ClearRenderTarget(
            //    ((clearFlags & CameraClearFlags.Depth) != 0),
            //    ((clearFlags & CameraClearFlags.Color) != 0),
            //    camera.backgroundColor);
        }

        private void LoadBufferWithGeometryInstances(SceneParseResult sceneParseResult,
                                                     ref TopLevelAccelerationBuffer bvhBuffer,
                                                     ref SortedList<ISIdx, ObjectLevelAccelerationGeometryBuffer> objectLvAccGeoBuffers,
                                                     ref SortedList<ISIdx, ObjectLevelAccelerationGeometryMappingCollectionBuffer> objectLvAccGeoMapBuffers,
                                                     ref PrimitiveBuffer primitiveBuffer,
                                                     ref WorldToLocalBuffer worldToPrimitiveBuffer,
                                                     ref SortedList<ISIdx, GeometryInstanceBuffer> gemoetryInstanceBuffers,
                                                     ref TopLevelAccelerationGeometryMappingCollectionBuffer topLevelAccGeoMapColBuffer)
        {
            gemoetryInstanceBuffers.Clear();
            objectLvAccGeoBuffers.Clear();
            objectLvAccGeoMapBuffers.Clear();

            //Debug.Log("geo instances is " + sceneParseResult.GeometryInstances.Count);
            //Debug.Log("more counts are " + sceneParseResult.ObjectLevelAccelerationGeometryMapping.Count);

            var geoInsIter = sceneParseResult.GeometryInstances.GetEnumerator();
            while (geoInsIter.MoveNext())
            {
                var buffer = new GeometryInstanceBuffer(sceneParseResult.GetGeometryInstancesCount(geoInsIter.Current.Key), sceneParseResult.GetGeometryInstancesStride(geoInsIter.Current.Key));
                buffer.SetData(geoInsIter.Current.Value);
                gemoetryInstanceBuffers.Add(geoInsIter.Current.Key, buffer);
            }

            var objectLevelAccGeoIter = sceneParseResult.ObjectLevelAccelerationGeometries.GetEnumerator();
            while (objectLevelAccGeoIter.MoveNext())
            {
                var buffer = new ObjectLevelAccelerationGeometryBuffer(objectLevelAccGeoIter.Current.Value.Count, sizeof(float));
                buffer.SetData(objectLevelAccGeoIter.Current.Value);
                objectLvAccGeoBuffers.Add(objectLevelAccGeoIter.Current.Key, buffer);
            }

            var objectLvAccGeoMapIter = sceneParseResult.ObjectLevelAccelerationGeometryMapping.GetEnumerator();
            while (objectLvAccGeoMapIter.MoveNext())
            {
                var buffer = new ObjectLevelAccelerationGeometryMappingCollectionBuffer(objectLvAccGeoMapIter.Current.Value.Count, sizeof(int));
                buffer.SetData(objectLvAccGeoMapIter.Current.Value);
                objectLvAccGeoMapBuffers.Add(objectLvAccGeoMapIter.Current.Key, buffer);
            }

            var objectLevelAccStrInsIter = sceneParseResult.ObjectLevelAccelerationStructures.GetEnumerator();
            while (objectLevelAccStrInsIter.MoveNext())
            {
                var buffer = new GeometryInstanceBuffer(objectLevelAccStrInsIter.Current.Value.Count, sizeof(float));
                buffer.SetData(objectLevelAccStrInsIter.Current.Value);
                gemoetryInstanceBuffers.Add(objectLevelAccStrInsIter.Current.Key, buffer);
            }

            sceneParseResult.TopLevelBVH.Flatten(
                flatten: out List<RTBoundingBoxToGPU> _flattenBVH,
                accelerationGeometryMapping: out List<int> _topLevelAccelerationGeometryMapping);
            bvhBuffer = new TopLevelAccelerationBuffer(_flattenBVH.Count, RTBoundingBox.stride);
            bvhBuffer.SetData(_flattenBVH);
            primitiveBuffer = new PrimitiveBuffer(sceneParseResult.Primitives.Count, Primitive.GetStride());
            primitiveBuffer.SetData(sceneParseResult.Primitives);
            topLevelAccGeoMapColBuffer = new TopLevelAccelerationGeometryMappingCollectionBuffer(_topLevelAccelerationGeometryMapping.Count, sizeof(int));
            topLevelAccGeoMapColBuffer.SetData(_topLevelAccelerationGeometryMapping);
            worldToPrimitiveBuffer = new WorldToLocalBuffer(sceneParseResult.WorldToPrimitive.Count, sizeof(float) * 16);
            worldToPrimitiveBuffer.SetData(sceneParseResult.WorldToPrimitive);
        }

        private void RunLoadGeometryToBuffer(SceneParseResult sceneParseResult,
                                             ref TopLevelAccelerationBuffer topLevelAcc,
                                             ref SortedList<ISIdx, ObjectLevelAccelerationGeometryBuffer> objectLvAccGeoBuffers,
                                             ref SortedList<ISIdx, ObjectLevelAccelerationGeometryMappingCollectionBuffer> objectLvAccGeoMapBuffers,
                                             ref PrimitiveBuffer primitiveBuffer,
                                             ref WorldToLocalBuffer worldToPrimitiveBuffer,
                                             ref SortedList<ISIdx, GeometryInstanceBuffer> gemoetryInstanceBuffers,
                                             ref TopLevelAccelerationGeometryMappingCollectionBuffer topLevelAccGeoMap)
        {
            LoadBufferWithGeometryInstances(sceneParseResult,
                                            bvhBuffer: ref topLevelAcc,
                                            objectLvAccGeoBuffers: ref objectLvAccGeoBuffers,
                                            objectLvAccGeoMapBuffers: ref objectLvAccGeoMapBuffers,
                                            primitiveBuffer: ref primitiveBuffer,
                                            worldToPrimitiveBuffer: ref worldToPrimitiveBuffer,
                                            gemoetryInstanceBuffers: ref gemoetryInstanceBuffers,
                                            topLevelAccGeoMapColBuffer: ref topLevelAccGeoMap);
        }

        private void RunLoadLightToBuffer(
            List<ComputeBuffer> computeShadersForLights,
            SceneParseResult sceneParseResult,
            ref ComputeBuffer lightInfoBuffer,
            ref ComputeShader mainShader)
        {
            int numberOfLights = sceneParseResult.Lights.Count;

            lightInfoBuffer = new ComputeBuffer(numberOfLights, RTLightInfo.Stride);
            lightInfoBuffer.SetData(sceneParseResult.LightPrimitives);


            PipelineLightslToBuffer.LightsToBuffer(computeShadersForLights,
                                                   sceneParseResult,
                                                   ref mainShader);
        }

        private void RunSetCameraToMainShader(Camera camera, int camNumber)
        {
            m_mainShader.SetMatrix("_CameraToWorld", camera.cameraToWorldMatrix);
            m_mainShader.SetVector("_CameraForward", camera.transform.forward);
            m_mainShader.SetMatrix("_CameraInverseProjection", camera.projectionMatrix.inverse);
            m_mainShader.SetFloat("_CameraOrthographicSize", camera.orthographicSize);
            m_mainShader.SetMatrix("_CameraLocalToWorld", camera.transform.localToWorldMatrix);

            if(camNumber == 0)
            {
                //assume left camera
                m_mainShader.SetBool("_LeftEye", false);
            }
            else
            {
                //assume right camera
                m_mainShader.SetBool("_LeftEye", true);
            }
            
        }

        private void RunSetAmbientToMainShader(RenderPipelineConfigObject config)
        {
            m_mainShader.SetVector("_AmbientLightUpper", config.upperAmbitent);
        }

        private void RunSetRayGenerationShader(int rayGenId)
        {
            m_mainShader.SetInt("_RayGenID", rayGenId);
        }

        private void RunSetGeometryInstanceToMainShader(int count,
                                                        ref TopLevelAccelerationBuffer bvhBuffer,
                                                        ref SortedList<ISIdx, ObjectLevelAccelerationGeometryBuffer> objectLvAccGeoBuffers,
                                                        ref SortedList<ISIdx, ObjectLevelAccelerationGeometryMappingCollectionBuffer> objectLvAccGeoMapBuffers,
                                                        ref PrimitiveBuffer primitiveBuffer,
                                                        ref WorldToLocalBuffer worldToPrimitiveBuffer,
                                                        string[] intersectShaderNames,
                                                        ref SortedList<ISIdx, GeometryInstanceBuffer> geoInsBuffers,
                                                        ref TopLevelAccelerationGeometryMappingCollectionBuffer topLevelAccGeoMapColBuffer)
        {
            m_mainShader.SetInt("_NumOfPrimitive", count);
            m_mainShader.SetBuffer(kIndex, "_Primitives", primitiveBuffer);
            m_mainShader.SetBuffer(kIndex, "_WorldToPrimitives", worldToPrimitiveBuffer);
            m_mainShader.SetBuffer(kIndex, "_BVHTree", bvhBuffer);
            m_mainShader.SetBuffer(kIndex, "_TopLevelAccelerationGeometryMapping", topLevelAccGeoMapColBuffer);

            empty = new ComputeBuffer(1, sizeof(float));

            for (int intersectIdx = 0; intersectIdx < intersectShaderNames.Length; intersectIdx++)
            {
                if (geoInsBuffers.ContainsKey(intersectIdx))
                {
                    m_mainShader.SetBuffer(kIndex, $"_{intersectShaderNames[intersectIdx]}", geoInsBuffers[intersectIdx]);
                }
                else
                {
                    m_mainShader.SetBuffer(kIndex, $"_{intersectShaderNames[intersectIdx]}", empty);
                }
            }
            // -
            // -
            // -
            // -
            // -

            var objectLvAccGeoBuffersIter = objectLvAccGeoBuffers.GetEnumerator();
            while (objectLvAccGeoBuffersIter.MoveNext())
            {
                //Debug.Log("setting geo buffers for " + $"_{intersectShaderNames[objectLvAccGeoBuffersIter.Current.Key]}GeometryData");
                m_mainShader.SetBuffer(kIndex, $"_{intersectShaderNames[objectLvAccGeoBuffersIter.Current.Key]}GeometryData", objectLvAccGeoBuffersIter.Current.Value);
            }
            //Debug.Log("setting geo mapping"); < this is being hit correctly
            var objectLvAccGeoMapBuffersIter = objectLvAccGeoMapBuffers.GetEnumerator();
            while (objectLvAccGeoMapBuffersIter.MoveNext())
            {
                //Debug.Log("setting geo mapping for " + $"_{intersectShaderNames[objectLvAccGeoMapBuffersIter.Current.Key]}GeometryMapping");
                m_mainShader.SetBuffer(kIndex, $"_{intersectShaderNames[objectLvAccGeoMapBuffersIter.Current.Key]}GeometryMapping", objectLvAccGeoMapBuffersIter.Current.Value);
            }
        }

        private void RunSetLightsToMainShader(int count, ref ComputeBuffer lightInfoBuffer)
        {
            m_mainShader.SetInt("_NumOfLights", count);
            m_mainShader.SetBuffer(kIndex, "_Lights", lightInfoBuffer);
        }

        private void RunRayTracing(ref CommandBuffer commands, RenderTexture targetTexture, RenderTexture lowResTextures)
        {
            m_mainShader.SetTexture(kIndex, "Result", targetTexture);
            m_mainShader.SetTexture(kIndex, "_LowResTexture", lowResTextures);
            int threadGroupsX = Mathf.CeilToInt(Screen.width / 8.0f);
            int threadGroupsY = Mathf.CeilToInt(Screen.height / 8.0f);

            // Prevent dispatching 0 threads to GPU (when the editor is starting or there is no screen to render) 
            if (threadGroupsX > 0 && threadGroupsY > 0)
            {
                // m_mainShader.Dispatch(kIndex, threadGroupsX, threadGroupsY, 1);
                //commands.DispatchCompute(computeShader: m_mainShader, kernelIndex: kIndex, threadGroupsX: threadGroupsX, threadGroupsY: threadGroupsY, threadGroupsZ: 1);
            }
        }

        private void RunBufferCleanUp()
        {
            empty.Release();
            m_topLevelAcc.Release();
            m_topLevelAccGeoMap.Release();
            m_primitiveBuffer.Release();
            m_worldToPrimitiveBuffer.Release();
            foreach (var item in m_gemoetryInstanceBuffer)
            {
                item.Value?.Release();
            }
            m_gemoetryInstanceBuffer.Clear();
            foreach (var item in m_objectLevelAccGeoBuffers)
            {
                item.Value?.Release();
            }
            m_objectLevelAccGeoBuffers.Clear();
            foreach (var item in m_objectLevelAccGeoMapBuffers)
            {
                item.Value?.Release();
            }
            m_objectLevelAccGeoMapBuffers.Clear();
            m_lightInfoBuffer?.Release();

            foreach (var lightBuffers in computeBufferForLights)
            {
                lightBuffers.Release();
            }
            computeBufferForLights.Clear();

            if (clearMaterials)
            {
                foreach (var materialBuffers in computeBufferForMaterials)
                {
                    materialBuffers.Release();
                }
                computeBufferForMaterials.Clear();
                reloadMaterials = true;
            }
        }

        private void RunSendTextureToUnity(CommandBuffer commands, RenderTexture targeTexture,
            ScriptableRenderContext renderContext, Camera camera, RenderTexture textureToWriteTo, bool runWithoutFoveation)
        {
            if (MEASURE_DISPATCH_TIME) {
                //GlobalTimer.StartStopwatch();
                //first is to test reading from GPU without dispatch already being called.
                //Texture2D temp = new Texture2D(targeTexture.width, targeTexture.height);
                //RenderTexture.active = targeTexture;
                //temp.ReadPixels(new Rect(0, 0, targeTexture.width, targeTexture.height), 0, 0); //force read from GPU, which forces block until compute shader is finished

                //string timeElapsed = GlobalTimer.EndStopwatch();
                //Debug.Log("Time to extract frame for " + camera.gameObject.name + " is " + timeElapsed);

                GlobalTimer.StartStopwatch();
            }

            //start timer for beginning of rendering until start of next frame
            GlobalTimer.endOnFrameStart.Start();

            int threadGroupsX, threadGroupsY;

            if (!runWithoutFoveation && !disableRendering)
            {
                m_mainShader.SetInt("_runRes", 1);
                threadGroupsX = Mathf.CeilToInt(m_target.width / 8.0f);
                threadGroupsY = Mathf.CeilToInt(m_target.height / 8.0f);
                //run once, this will create the full-res shared texture which is kept internally
                m_mainShader.Dispatch(kernelIndex: kIndex, threadGroupsX: threadGroupsX, threadGroupsY: threadGroupsY, threadGroupsZ: 1);

                m_mainShader.SetInt("_runRes", 2);
                threadGroupsX = Mathf.CeilToInt(m_target.width / 16.0f);
                threadGroupsY = Mathf.CeilToInt(m_target.height / 16.0f);
                //run once, this will create the half-res shared texture which is kept internally
                m_mainShader.Dispatch(kernelIndex: kIndex, threadGroupsX: threadGroupsX, threadGroupsY: threadGroupsY, threadGroupsZ: 1);

                threadGroupsX = Mathf.CeilToInt(m_target.width / 32.0f);
                threadGroupsY = Mathf.CeilToInt(m_target.height / 32.0f);
                m_mainShader.SetInt("_runRes", 3);
                //quarter
                m_mainShader.Dispatch(kernelIndex: kIndex, threadGroupsX: threadGroupsX, threadGroupsY: threadGroupsY, threadGroupsZ: 1);

                threadGroupsX = Mathf.CeilToInt(m_target.width / 64.0f);
                threadGroupsY = Mathf.CeilToInt(m_target.height / 64.0f);
                m_mainShader.SetInt("_runRes", 4);
                //eigth
                m_mainShader.Dispatch(kernelIndex: kIndex, threadGroupsX: threadGroupsX, threadGroupsY: threadGroupsY, threadGroupsZ: 1);
            }


            //now we can run foveated raytracer
            m_mainShader.SetInt("_runRes", 0); //now patch together as post process

            //directly dispatch compute shader and blit to outside texture
            threadGroupsX = Mathf.CeilToInt(m_target.width / 8.0f);
            threadGroupsY = Mathf.CeilToInt(m_target.height / 8.0f);

            if (!disableRendering)
            {
                m_mainShader.Dispatch(kernelIndex: kIndex, threadGroupsX: threadGroupsX, threadGroupsY: threadGroupsY, threadGroupsZ: 1);
            }
            

            //additional setup for post process blur, while blit to final:

            Graphics.Blit(targeTexture, textureToWriteTo, m_blurMaterial);

            if (MEASURE_DISPATCH_TIME)
            {
                //Texture2D temp = new Texture2D(targeTexture.width, targeTexture.height);
                //RenderTexture.active = targeTexture;
                //temp.ReadPixels(new Rect(0, 0, targeTexture.width, targeTexture.height), 0, 0); //force read from GPU, which forces block until compute shader is finished

                string timeElapsed = GlobalTimer.EndStopwatch();
                Debug.Log("Time to render frame for " + camera.gameObject.name + " is " + timeElapsed);
            }

            //commands.Clear(); // Clear the command buffer
        }
    }
}