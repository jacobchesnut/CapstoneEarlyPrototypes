﻿using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
/* commented out due to moq being not recognized
using Moq;

namespace OpenRT.UnitTests.Primitive
{
    public class RTMeshBVH_SerializeRTMeshBVH_UnitTests
    {
        [Test]
        public void Serialize_OneTriangle_BVH()
        {
            // Assign
            Vector3[] vertices = new Vector3[3];
            vertices[0] = new Vector3(0, 0, 0);
            vertices[1] = new Vector3(0, 5, 0);
            vertices[2] = new Vector3(5, 0, 0);

            Vector3[] normals = new Vector3[3];
            normals[0] = new Vector3(0, 0, -1);
            normals[1] = new Vector3(0, 0, -1);
            normals[2] = new Vector3(0, 0, -1);

            Vector2[] uvs = new Vector2[3];
            uvs[0] = new Vector2(0, 0);
            uvs[1] = new Vector2(0.5f, 1);
            uvs[2] = new Vector2(1, 0);

            int[] trianglesVertexOrder = new int[3];
            trianglesVertexOrder[0] = 0;
            trianglesVertexOrder[1] = 1;
            trianglesVertexOrder[2] = 2;

            var actuator = new Mock<RTMeshBVHController.IActuator>();
            actuator.Setup(x => x.GetVertices()).Returns(vertices);
            actuator.Setup(x => x.LocalToWorldVertex(It.IsAny<Vector3>())).Returns<Vector3>(v => v);

            RTMeshBVHController controller = new RTMeshBVHController(actuator: actuator.Object);

            List<List<float>> flatten = new List<List<float>>();
            List<List<int>> accelerationGeometryMappingCollection = new List<List<int>>();

            // Act
            controller.BuildBVHAndTriangleList(0,
                                               normals,
                                               trianglesVertexOrder,
                                               uvs,
                                               vertices);
            RTMeshBVHBuilder.Flatten(ref flatten,
                                     0,
                                     0,
                                     ref accelerationGeometryMappingCollection,
                                     controller.GetRoot());
            List<float> serialized = RTMeshBVHController.SerializeRTMeshBVH(flatten);

            // Assert
            Assert.AreEqual(1, flatten.Count);  // There is only 1 bounding box
            var box = flatten[0];
            Assert.AreEqual(10, box.Count); // 10 fields per box
            Assert.AreEqual(-1, box[0]); // No child node on the left
            Assert.AreEqual(-1, box[1]); // No child node on the right
            Assert.AreEqual(5, box[2]); // max.x
            Assert.AreEqual(5, box[3]); // max.y
            Assert.AreEqual(0, box[4]); // max.z
            Assert.AreEqual(0, box[5]); // min.x
            Assert.AreEqual(0, box[6]); // min.y
            Assert.AreEqual(0, box[7]); // min.z
            Assert.AreEqual(0, box[8]); // primitive begin
            Assert.AreEqual(1, box[9]); // primitive count

            Assert.AreEqual(1, accelerationGeometryMappingCollection.Count);  // There is only 1 triangles

            Assert.AreEqual(1 + 1 + 10 + 14, serialized.Count);   // Size of the BVH + Size of the primitive list + BVH + Primitive List
            Assert.AreEqual(12, serialized[0]); // #1 = the end + 1 of the BVH
            Assert.AreEqual(26, serialized[1]); // #2 = the size of the primitive list
        }
    }
}
*/