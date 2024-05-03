﻿using System.Collections;
using UnityEngine;

namespace Assets.Scripts
{
    public class Tetrahedron
    {
        public Node node0;
        public Node node1;
        public Node node2;
        public Node node3;

        private float volume;

        public Tetrahedron(Node node0, Node node1, Node node2, Node node3)
        {
            this.node0 = node0;
            this.node1 = node1;
            this.node2 = node2;
            this.node3 = node3;

            volume = CalculateVolume(this.node0.pos, this.node1.pos, this.node2.pos, this.node3.pos);
        }

        private float CalculateVolume(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
        {
            Vector3 A = p1 - p0;
            Vector3 B = p2 - p0;
            Vector3 C = p3 - p0;

            return Mathf.Abs(Vector3.Dot(A, Vector3.Cross(B, C))) / 6;
        }

        public Vector4 CalculateBarycentricCoordinates(Vector3 P)
        {
            float w0 = CalculateVolume(P, node1.pos, node2.pos, node3.pos) / volume;
            float w1 = CalculateVolume(node0.pos, P, node2.pos, node3.pos) / volume;
            float w2 = CalculateVolume(node0.pos, node1.pos, P, node3.pos) / volume;
            float w3 = CalculateVolume(node0.pos, node1.pos, node2.pos, P) / volume;

            return new Vector4(w0, w1, w2, w3);
        }
    }
}