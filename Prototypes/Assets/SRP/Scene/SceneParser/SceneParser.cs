using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OpenRT
{

    using ISIdx = System.Int32; // IntersectShaderIndex

    public class SceneParser
    {
        private const bool DEBUG_MEASURE_TIME = true;

        private bool firstTime = true;

        private static SceneParser _sharedInstance = new SceneParser();

        public SceneParseResult sceneParseResult;

        private List<RenderConnector> connections = new List<RenderConnector>();

        //for maintaining connections to RTRenderers, so that if one is
        //dirty we can just remove info related to that one in particular
        public struct RenderConnector
        {
            public RTRenderer r_renderer;
            public Primitive r_primitive;
            public Matrix4x4 r_worldToPrimitive;
            public int r_intersectIndex;
            public RTMaterial r_material;
            public RTBoundingBox r_box;
            public int r_assignedPrimitiveID;
            public int r_geoOffset;
            public int r_mapOffset;
            public int r_dataOffset;
        }

        public static SceneParser Instance
        {
            get { return _sharedInstance; }
        }

        private SceneParser()
        {
            sceneParseResult = new SceneParseResult();
        }

        public List<RTLight> GetAllLights(GameObject[] roots)
        {
            List<RTLight> lights = new List<RTLight>();

            foreach (var root in roots)
            {
                lights.AddRange(root.GetComponentsInChildren<RTLight>());
            }

            return lights;
        }

        public List<RTRenderer> GetAllRenderers(GameObject[] roots)
        {
            List<RTRenderer> renderers = new List<RTRenderer>();

            foreach (var root in roots)
            {
                renderers.AddRange(root.GetComponentsInChildren<RTRenderer>());
            }

            return renderers;
        }

        public bool IsAllGeometriesDirty(List<RTRenderer> renderers)
        {
            bool isDirty = false;

            renderers.ForEach(r =>
            {
                if (r.geometry != null)
                {
                    isDirty |= r.geometry.IsDirty();
                }
            });

            return isDirty;
        }


        public List<RTRenderer> WhichGeometriesDirty(List<RTRenderer> renderers)
        {
            List<RTRenderer> dirtyGeo = new List<RTRenderer>();
            renderers.ForEach(r =>
            {
                if (r.geometry != null)
                {
                    if (r.geometry.IsDirty())
                    {
                        //UnityEngine.Debug.Log("Geo is dirty!");
                        dirtyGeo.Add(r);
                    }
                }
            });
            return dirtyGeo;
        }

        public bool IsAllLightsDirty(List<RTLight> lights)
        {
            bool isDirty = false;

            lights.ForEach(r =>
            {
                isDirty |= r.IsDirty();
            });

            return isDirty;
        }

        public SceneParseResult ParseScene(Scene scene)
        {
            //stopwatch to measure scene parsing times
            Stopwatch timer = new Stopwatch();

            GameObject[] roots = scene.GetRootGameObjects();

            timer.Start();
            //if (firstTime)
            //{
            //    ParseGeometry(roots,
            //                  ref sceneParseResult);
            //    firstTime = false;
            //}
            //else
            //{
                ParseDirtyGeometry(roots, ref sceneParseResult);
            //}
            timer.Stop();
            if (DEBUG_MEASURE_TIME)
            {
                UnityEngine.Debug.Log("Time to parse Geometry is: " + timer.Elapsed);
            }
            timer.Reset();

            timer.Start();
            ParseLight(
                roots,
                ref sceneParseResult);
            timer.Stop();
            if (DEBUG_MEASURE_TIME)
            {
                UnityEngine.Debug.Log("Time to parse Light is: " + timer.Elapsed);
            }
            timer.Reset();

            timer.Start();
            sceneParseResult.TopLevelBVH.Construct();
            timer.Stop();
            if (DEBUG_MEASURE_TIME)
            {
                UnityEngine.Debug.Log("Time to construct BVH tree is: " + timer.Elapsed);
            }
            timer.Reset();

            return sceneParseResult;
        }

        private void ParseLight(
            GameObject[] roots,
            ref SceneParseResult sceneParseResult)
        {
            var lights = GetAllLights(roots);

            if (!IsAllLightsDirty(lights))
            {
                // All the lights are unchange, no need to rebuild
                return;
            }

            sceneParseResult.ClearAllLights();

            foreach (var light in lights)
            {
                if (light.gameObject.activeInHierarchy)
                {
                    int lightInstanceIndex = sceneParseResult.AddLight(light);
                }
            }
        }

        private void ParseGeometry(
            GameObject[] roots,
            ref SceneParseResult sceneParseResult)
        {
            var renderers = GetAllRenderers(roots);

            if (!IsAllGeometriesDirty(renderers) && sceneParseResult.Primitives.Count != 0)
            {
                // All the geometries are unchange, no need to rebuild
                return;
            }

            // TODO: Optimize dynamic array generation
            sceneParseResult.ClearAllPrimitives();
            sceneParseResult.ClearAllGeometries();
            sceneParseResult.ClearAllMaterials();
            sceneParseResult.ClearTopLevelBVH();

            foreach (var renderer in renderers)
            {
                if (renderer.gameObject.activeInHierarchy)
                {
                    RTMaterial material = renderer.material;
                    if (renderer.geometry == null || !renderer.geometry.IsGeometryValid() || material == null)
                    {
                        continue;
                    }

                    var closestShaderGUID = renderer.material.GetClosestHitGUID();
                    int closestShaderIndex = CustomShaderDatabase.Instance.GUIDToShaderIndex(closestShaderGUID, EShaderType.ClosestHit);
                    var intersectShaderGUID = renderer.geometry.GetIntersectShaderGUID();
                    int intersectShaderIndex = CustomShaderDatabase.Instance.GUIDToShaderIndex(intersectShaderGUID, EShaderType.Intersect);

                    if (!sceneParseResult.GeometryStride.ContainsKey(intersectShaderIndex))
                    {
                        sceneParseResult.GeometryStride.Add(intersectShaderIndex, renderer.geometry.IsAccelerationStructure() ? 0 : renderer.geometry.GetStride());
                    }

                    if (renderer.geometry.IsAccelerationStructure())
                    {
                        // Such as Low-Level BVH (RTMeshBVH)
                        int mapOffset = sceneParseResult.ObjectLevelAccGeoMapCursor(intersectShaderIndex);
                        int geoOffset = sceneParseResult.ObjectLevelAccGeoCursor(intersectShaderIndex);
                        ((IRTMeshBVH)(renderer.geometry)).BuildBVHAndTriangleList(geoLocalToGlobalIndexOffset: geoOffset,
                                                                                  mappingLocalToGlobalIndexOffset: mapOffset);

                        List<float> geoInsData = renderer.geometry.GetGeometryInstanceData(geoLocalToGlobalIndexOffset: geoOffset,
                                                                                           mappingLocalToGlobalIndexOffset: mapOffset);
                        sceneParseResult.AddAccelerationStructureGeometry(
                            accelerationStructureData: geoInsData,
                            accelGeometryMapping: renderer.geometry.GetAccelerationStructureGeometryMapping(geoLocalToGlobalIndexOffset: geoOffset,
                                                                                                            mappingLocalToGlobalIndexOffset: mapOffset),
                            accelGeometryData: renderer.geometry.GetAccelerationStructureGeometryData(geoLocalToGlobalIndexOffset: geoOffset,
                                                                                                      mappingLocalToGlobalIndexOffset: mapOffset),
                            intersectIndex: intersectShaderIndex
                        );
                    }
                    else
                    {
                        // Standardized Geometry (Sphere, Triangle)
                        List<float> geoInsData = renderer.geometry.GetGeometryInstanceData(geoLocalToGlobalIndexOffset: 0, mappingLocalToGlobalIndexOffset: 0);  // No offset
                        sceneParseResult.AddGeometryData(
                            geometryData: geoInsData,
                            intersectIndex: intersectShaderIndex
                        );
                    }

                    int startIndex = sceneParseResult.AddGeometryCount(
                        count: renderer.geometry.GetCount(),
                        intersectIndex: intersectShaderIndex
                    );

                    int materialInstanceIndex = sceneParseResult.AddMaterial(material);
                    Matrix4x4 worldToPrim = renderer.gameObject.transform.worldToLocalMatrix;
                    sceneParseResult.AddWorldToPrimitive(ref worldToPrim);

                    sceneParseResult.AddPrimitive(new Primitive(
                        geometryIndex: intersectShaderIndex,
                        geometryInstanceBegin: startIndex,
                        geometryInstanceCount: renderer.geometry.GetCount(),
                        materialIndex: closestShaderIndex,
                        materialInstanceIndex: materialInstanceIndex,
                        transformIndex: sceneParseResult.WorldToPrimitive.Count - 1
                    ));

                    var boxOfThisObject = renderer.geometry.GetTopLevelBoundingBox(assginedPrimitiveId: sceneParseResult.Primitives.Count - 1);
                    sceneParseResult.AddBoundingBox(boxOfThisObject);
                }
            }
        }

        //instead of reloading all geometry when something is changed, only reload what is changed
        private void ParseDirtyGeometry(
            GameObject[] roots,
            ref SceneParseResult sceneParseResult)
        {

            var renderers = GetAllRenderers(roots);

            //if (!IsAllGeometriesDirty(renderers) && sceneParseResult.Primitives.Count != 0)
            //{
            // All the geometries are unchange, no need to rebuild
            //    return;
            //}


            List<RTRenderer> dirtyGeometry = WhichGeometriesDirty(renderers);
            //UnityEngine.Debug.Log("dirty geometry is: " + dirtyGeometry.Count);

            foreach (RTRenderer renderer in dirtyGeometry)
            {
                RenderConnector oldConnection = new RenderConnector(); //cannot be null
                bool foundMatch = false;
                //find old connection
                UnityEngine.Debug.LogWarning("Testing renderer " + renderer.gameObject.name);
                foreach(RenderConnector connection in connections)
                {
                    UnityEngine.Debug.LogWarning("Testing connecting renderer " + connection.r_renderer.gameObject.name);
                    if (renderer.gameObject == connection.r_renderer.gameObject)
                    {
                        UnityEngine.Debug.LogWarning("found match");
                        oldConnection = connection;
                        foundMatch = true;
                        break;
                    }
                }

                /*
                //attempt to only modify data?
                if (foundMatch) //dirty, but only from transform changes...
                {
                    
                    //manually set transform matrix
                    oldConnection.r_worldToPrimitive.m00 = renderer.gameObject.transform.worldToLocalMatrix.m00;
                    oldConnection.r_worldToPrimitive.m01 = renderer.gameObject.transform.worldToLocalMatrix.m01;
                    oldConnection.r_worldToPrimitive.m02 = renderer.gameObject.transform.worldToLocalMatrix.m02;
                    oldConnection.r_worldToPrimitive.m03 = renderer.gameObject.transform.worldToLocalMatrix.m03;
                    oldConnection.r_worldToPrimitive.m10 = renderer.gameObject.transform.worldToLocalMatrix.m10;
                    oldConnection.r_worldToPrimitive.m11 = renderer.gameObject.transform.worldToLocalMatrix.m11;
                    oldConnection.r_worldToPrimitive.m12 = renderer.gameObject.transform.worldToLocalMatrix.m12;
                    oldConnection.r_worldToPrimitive.m13 = renderer.gameObject.transform.worldToLocalMatrix.m13;
                    oldConnection.r_worldToPrimitive.m20 = renderer.gameObject.transform.worldToLocalMatrix.m20;
                    oldConnection.r_worldToPrimitive.m21 = renderer.gameObject.transform.worldToLocalMatrix.m21;
                    oldConnection.r_worldToPrimitive.m22 = renderer.gameObject.transform.worldToLocalMatrix.m22;
                    oldConnection.r_worldToPrimitive.m23 = renderer.gameObject.transform.worldToLocalMatrix.m23;
                    oldConnection.r_worldToPrimitive.m30 = renderer.gameObject.transform.worldToLocalMatrix.m30;
                    oldConnection.r_worldToPrimitive.m31 = renderer.gameObject.transform.worldToLocalMatrix.m31;
                    oldConnection.r_worldToPrimitive.m32 = renderer.gameObject.transform.worldToLocalMatrix.m32;
                    oldConnection.r_worldToPrimitive.m33 = renderer.gameObject.transform.worldToLocalMatrix.m33;

                    sceneParseResult.ChangeWorldToPrimitive(oldConnection.r_worldToPrimitive, oldConnection.r_assignedPrimitiveID);
                    
                    //this should be all that has to change
                    //sceneParseResult.RemoveWorldToPrimitive(oldConnection);
                    sceneParseResult.RemoveTopLevelBVH(oldConnection);
                    //and so...
                    //sceneParseResult.AddWorldToPrimitive(renderer.gameObject.transform.worldToLocalMatrix);
                    //oldConnection.r_worldToPrimitive = renderer.gameObject.transform.worldToLocalMatrix;
                    //oldConnection.r_primitive.setTransformIndex(sceneParseResult.WorldToPrimitive.Count - 1); //new end of list

                    var boxOfThisObject = renderer.geometry.GetTopLevelBoundingBox(assginedPrimitiveId: oldConnection.r_assignedPrimitiveID);
                    sceneParseResult.AddBoundingBox(boxOfThisObject);
                    oldConnection.r_box = boxOfThisObject;
                    continue;
                }
                */


                if (foundMatch) //only modify if there is something to modify (we may be here because new object, in which case do not modify!)
                {
                    //need to change world to primitive matrix... no actually? it's related to primitive so I will if I have to change primitive

                    //need to change acceleration structure geometry
                    //rebuild triangle list (should be same size but different numbers because this actually takes into account our new world position)
                    ((IRTMeshBVH)(renderer.geometry)).BuildBVHAndTriangleList(geoLocalToGlobalIndexOffset: oldConnection.r_geoOffset,
                                                                                  mappingLocalToGlobalIndexOffset: oldConnection.r_mapOffset);
                    //grab data
                    List<float> geoInsData = renderer.geometry.GetGeometryInstanceData(geoLocalToGlobalIndexOffset: oldConnection.r_geoOffset,
                                                                                       mappingLocalToGlobalIndexOffset: oldConnection.r_mapOffset);
                    sceneParseResult.ModAccelerationStructureGeometry(
                        accelerationStructureData: geoInsData,
                        accelGeometryData: renderer.geometry.GetAccelerationStructureGeometryData(geoLocalToGlobalIndexOffset: oldConnection.r_geoOffset,
                                                                                                  mappingLocalToGlobalIndexOffset: oldConnection.r_mapOffset),
                        accelGeometryMapping: renderer.geometry.GetAccelerationStructureGeometryMapping(geoLocalToGlobalIndexOffset: oldConnection.r_geoOffset,
                                                                                                        mappingLocalToGlobalIndexOffset: oldConnection.r_mapOffset),
                        intersectIndex: oldConnection.r_intersectIndex,
                        oldConnection.r_geoOffset,
                        oldConnection.r_mapOffset,
                        oldConnection.r_dataOffset
                    );

                    sceneParseResult.RemoveTopLevelBVH(oldConnection);
                    var boxOfThisObject = renderer.geometry.GetTopLevelBoundingBox(assginedPrimitiveId: oldConnection.r_assignedPrimitiveID);
                    sceneParseResult.AddBoundingBox(boxOfThisObject);
                    oldConnection.r_box = boxOfThisObject;

                    continue;

                    //sceneParseResult.RemovePrimitive(oldConnection);
                    //sceneParseResult.RemoveGeometry(oldConnection); //currently causing rest of scene to disappear
                    //sceneParseResult.RemoveMaterial(oldConnection);
                    //sceneParseResult.RemoveTopLevelBVH(oldConnection);
                    //connections.Remove(oldConnection);
                }
                
                RenderConnector newConnection = new RenderConnector();
                newConnection.r_renderer = renderer;

                //from here is mostly the same as regular parse geometry
                if (renderer.gameObject.activeInHierarchy)
                {
                    UnityEngine.Debug.Log("printing addition for object " + renderer.gameObject.name);

                    RTMaterial material = renderer.material;
                    newConnection.r_material = material;
                    if (renderer.geometry == null || !renderer.geometry.IsGeometryValid() || material == null)
                    {
                        continue;
                    }

                    var closestShaderGUID = renderer.material.GetClosestHitGUID();
                    int closestShaderIndex = CustomShaderDatabase.Instance.GUIDToShaderIndex(closestShaderGUID, EShaderType.ClosestHit);
                    var intersectShaderGUID = renderer.geometry.GetIntersectShaderGUID();
                    int intersectShaderIndex = CustomShaderDatabase.Instance.GUIDToShaderIndex(intersectShaderGUID, EShaderType.Intersect);

                    newConnection.r_intersectIndex = intersectShaderIndex;

                    if (!sceneParseResult.GeometryStride.ContainsKey(intersectShaderIndex))
                    {
                        sceneParseResult.GeometryStride.Add(intersectShaderIndex, renderer.geometry.IsAccelerationStructure() ? 0 : renderer.geometry.GetStride());
                        UnityEngine.Debug.Log("adding intersect shader index " + intersectShaderIndex + " and stride " + (renderer.geometry.IsAccelerationStructure() ? 0 : renderer.geometry.GetStride()));
                    }

                    if (renderer.geometry.IsAccelerationStructure())
                    {
                        // Such as Low-Level BVH (RTMeshBVH)

                        //we get the cursors here to tell us the end point of these lists *currently*
                        //this changes after we add this object, so the next one starts at the end of this one's data
                        int mapOffset = sceneParseResult.ObjectLevelAccGeoMapCursor(intersectShaderIndex);
                        newConnection.r_mapOffset = mapOffset;
                        int geoOffset = sceneParseResult.ObjectLevelAccGeoCursor(intersectShaderIndex);
                        newConnection.r_geoOffset = geoOffset;
                        newConnection.r_dataOffset = sceneParseResult.ObjectLevelAccDataCursor(intersectShaderIndex);

                        ((IRTMeshBVH)(renderer.geometry)).BuildBVHAndTriangleList(geoLocalToGlobalIndexOffset: geoOffset,
                                                                                  mappingLocalToGlobalIndexOffset: mapOffset);

                        List<float> geoInsData = renderer.geometry.GetGeometryInstanceData(geoLocalToGlobalIndexOffset: geoOffset,
                                                                                           mappingLocalToGlobalIndexOffset: mapOffset);
                        sceneParseResult.AddAccelerationStructureGeometry(
                            accelerationStructureData: geoInsData,
                            accelGeometryMapping: renderer.geometry.GetAccelerationStructureGeometryMapping(geoLocalToGlobalIndexOffset: geoOffset,
                                                                                                            mappingLocalToGlobalIndexOffset: mapOffset),
                            accelGeometryData: renderer.geometry.GetAccelerationStructureGeometryData(geoLocalToGlobalIndexOffset: geoOffset,
                                                                                                      mappingLocalToGlobalIndexOffset: mapOffset),
                            intersectIndex: intersectShaderIndex
                        );
                        string geoInsDataDebug = "";
                        foreach(float f in geoInsData)
                        {
                            geoInsDataDebug += "[" + f + "] ";
                        }
                        UnityEngine.Debug.Log("adding geoInsData " + geoInsDataDebug);
                        UnityEngine.Debug.Log("adding geomapping " + renderer.geometry.GetAccelerationStructureGeometryMapping(geoLocalToGlobalIndexOffset: geoOffset, mappingLocalToGlobalIndexOffset: mapOffset).ToArray().ToString());
                        UnityEngine.Debug.Log("adding geodata " + renderer.geometry.GetAccelerationStructureGeometryData(geoLocalToGlobalIndexOffset: geoOffset, mappingLocalToGlobalIndexOffset: mapOffset).ToArray().ToString());
                        UnityEngine.Debug.Log("mapoffset is " + mapOffset + " and geoOffest is " + geoOffset);
                    }
                    else
                    {
                        // Standardized Geometry (Sphere, Triangle)
                        List<float> geoInsData = renderer.geometry.GetGeometryInstanceData(geoLocalToGlobalIndexOffset: 0, mappingLocalToGlobalIndexOffset: 0);  // No offset
                        sceneParseResult.AddGeometryData(
                            geometryData: geoInsData,
                            intersectIndex: intersectShaderIndex
                        );
                        //string geoInsDataDebug = "";
                        //foreach (float f in geoInsData)
                        //{
                        //    geoInsDataDebug += "[" + f + "] ";
                        //}
                        //UnityEngine.Debug.Log("adding geoInsData " + geoInsDataDebug);
                    }

                    int startIndex = sceneParseResult.AddGeometryCount(
                        count: renderer.geometry.GetCount(),
                        intersectIndex: intersectShaderIndex
                    );
                    UnityEngine.Debug.Log("adding geometry count " + renderer.geometry.GetCount());

                    int materialInstanceIndex = sceneParseResult.AddMaterial(material);
                    UnityEngine.Debug.Log("adding material index " + materialInstanceIndex);

                    newConnection.r_worldToPrimitive = renderer.gameObject.transform.worldToLocalMatrix;
                    sceneParseResult.AddWorldToPrimitive(ref newConnection.r_worldToPrimitive);
                    UnityEngine.Debug.Log("adding world to primitive " + newConnection.r_worldToPrimitive);

                    newConnection.r_primitive = new Primitive(
                        geometryIndex: intersectShaderIndex,
                        geometryInstanceBegin: startIndex,
                        geometryInstanceCount: renderer.geometry.GetCount(),
                        materialIndex: closestShaderIndex,
                        materialInstanceIndex: materialInstanceIndex,
                        transformIndex: sceneParseResult.WorldToPrimitive.Count - 1
                    );
                    sceneParseResult.AddPrimitive(newConnection.r_primitive);
                    UnityEngine.Debug.Log("adding primitive with geoinstance begin " + startIndex + ", closestShaderIndex " + closestShaderIndex + ", transformIndex " + (sceneParseResult.WorldToPrimitive.Count - 1));



                    var boxOfThisObject = renderer.geometry.GetTopLevelBoundingBox(assginedPrimitiveId: sceneParseResult.Primitives.Count - 1);
                    sceneParseResult.AddBoundingBox(boxOfThisObject);
                    newConnection.r_box = boxOfThisObject;
                    newConnection.r_assignedPrimitiveID = sceneParseResult.Primitives.Count - 1;
                    connections.Add(newConnection);
                }
                
            }

            
        }
    }
}