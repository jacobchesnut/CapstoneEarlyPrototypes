﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OpenRT
{

    /// <summary>
    /// Tree building algorithm from 
    /// Reference: https://github.com/CRCS-Graphics/2020.4.Kaihua.Hu.RealTime-RayTracer
    /// 
    /// </summary>
    public class TopLevelBVH
    {

        public const int HARD_LIMIT_MAX_DEPTH = 31;
        public const int MIN_NUMBER_OF_GEO_IN_BOX = 0;
        public const int NO_MORE_CHILD_NODE = -1;

        private List<RTBoundingBox> m_boxes = new List<RTBoundingBox>();
        private BVHNode m_root;

        public int AddBoundingBox(RTBoundingBox box)
        {
            m_boxes.Add(box);
            //Debug.LogWarning("RTBox added, size at: " + m_boxes.Count + ", and index is: " + m_boxes.IndexOf(box));
            return m_boxes.IndexOf(box);
        }

        public void ModBoundingBox(RTBoundingBox box, int boxID)
        {
            m_boxes[boxID] = box;
        }


        // Debug
        public List<RTBoundingBox> AllBoundingBoxes
        {
            get
            {
                return m_boxes;
            }
        }

        private BVHNode Build(List<RTBoundingBox> boxes, in int depth)
        {

            BVHNode rootNode = BVHNode.CombineAllBoxesAndPrimitives(boxes);
            char rootBoxLongestAxis = rootNode.bounding.longestAxis;

            if (boxes.Count <= 1)
            {
                Debug.Log("NODE IS " + (boxes.Count));
                return rootNode;
            }

            List<RTBoundingBox> left = new List<RTBoundingBox>();
            List<RTBoundingBox> right = new List<RTBoundingBox>();

            var cutting = CuttingPlane(boxes);

            foreach (RTBoundingBox box in boxes)
            {
                switch (rootBoxLongestAxis)
                {
                    case 'x':
                        if (cutting.x < box.center.x)
                        {
                            right.Add(box);
                        }
                        else
                        {
                            left.Add(box);
                        }
                        break;

                    case 'y':
                        if (cutting.y < box.center.y)
                        {
                            right.Add(box);
                        }
                        else
                        {
                            left.Add(box);
                        }
                        break;

                    case 'z':
                        if (cutting.z < box.center.z)
                        {
                            right.Add(box);
                        }
                        else
                        {
                            left.Add(box);
                        }
                        break;
                }
            }

            if (left.Count == 0 || right.Count == 0)
            {
                // Bisect fail (e.g. all children aligned on the division plane), end the tree building
                Debug.Log("NODE IS " + (left.Count + right.Count));
                return rootNode;
            }
            else
            {
                if (left.Count > MIN_NUMBER_OF_GEO_IN_BOX && depth < HARD_LIMIT_MAX_DEPTH)
                {
                    // Continue bisect
                    rootNode.left = Build(left, depth + 1);
                }
                else
                {
                    //Debug.Log("NODE IS " + (left.Count + right.Count));
                }

                if (right.Count > MIN_NUMBER_OF_GEO_IN_BOX && depth < HARD_LIMIT_MAX_DEPTH)
                {
                    // Continue bisect
                    rootNode.right = Build(right, depth + 1);
                }
                else
                {
                    //Debug.Log("NODE IS " + (left.Count + right.Count));
                }

                return rootNode;
            }
        }

        public void Construct()
        {
            m_root = Build(m_boxes, 0);
        }

        public void Clear()
        {
            m_boxes.Clear();
        }

        public void RemoveBoundingBox(int box)
        {
            m_boxes.RemoveAt(box);
        }

        private Vector3 CuttingPlane(List<RTBoundingBox> boxes)
        {
            Vector3 cp = new Vector3(0f, 0f, 0f);
            float div = 1.0f / boxes.Count;
            foreach (var box in boxes)
            {
                cp = cp + (box.center * div);
            }
            return cp;
        }

        public void Flatten(out List<RTBoundingBoxToGPU> flatten,
                            out List<int> accelerationGeometryMapping)
        {
            int id = 0;

            flatten = new List<RTBoundingBoxToGPU>();
            accelerationGeometryMapping = new List<int>();

            Queue<BVHNode> bfs = new Queue<BVHNode>();

            bfs.Enqueue(m_root);
            while (bfs.Count > 0)
            {
                var node = bfs.Dequeue();

                if (node.left != null)
                {
                    id++;
                    node.leftID = id;
                    bfs.Enqueue(node.left);
                }
                if (node.right != null)
                {
                    id++;
                    node.rightID = id;
                    bfs.Enqueue(node.right);
                }

                RTBoundingBox box = node.bounding.SetLeftRight(
                    left: node.leftID,
                    right: node.rightID
                );

                if (node.leftID == NO_MORE_CHILD_NODE && node.rightID == NO_MORE_CHILD_NODE)
                {
                    // Leaf Node
                    // Left child index will be -1 to indicate leaf node
                    // Right child index will be pointing to the Acceleration Structure - Geometry mapping index

                    int cursorToThisPrimitiveList = accelerationGeometryMapping.Count;
                    accelerationGeometryMapping.Add(0);
                    accelerationGeometryMapping.AddRange(box.geoIndices);
                    int cursorToEnd = accelerationGeometryMapping.Count;
                    int numberOfPrimitiveInThisBox = cursorToEnd - 1 - cursorToThisPrimitiveList;
                    accelerationGeometryMapping[cursorToThisPrimitiveList] = numberOfPrimitiveInThisBox;

                    // Secondly, we assign the index to the mapping collection of the same geometry to the right child index
                    // Noted that, this is different from the final mapping collection, which contains ALL the geometries' mapping
                    box.rightID = cursorToThisPrimitiveList;
                }

                flatten.Add(new RTBoundingBoxToGPU(box)); //TODO: Support overlapping bounding box and include all their primitives IDs

            }
        }

        public BVHNode Root
        {
            get
            {
                return m_root;
            }
        }
    }
}