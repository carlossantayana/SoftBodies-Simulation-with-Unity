using System.Collections;
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

            CalculateVolume();
        }

        private void CalculateVolume()
        {
            Vector3 A = node1.pos - node0.pos;
            Vector3 B = node2.pos - node0.pos;
            Vector3 C = node3.pos - node0.pos;

            volume = Mathf.Abs(Vector3.Dot(A, Vector3.Cross(B, C))) / 6;
        }

        public Vector4 CalculateBarycentricCoordinates(Vector3 P)
        {

        }
    }
}