using System.Collections;
using UnityEngine;

namespace Assets.Scripts
{
    public class Point
    {
        public Vector3 pos;
        private Tetrahedron containerTetrahedron;
        private Vector4 barycentricCoordinates;

        public Point(Vector3 position)
        {
            this.pos = position;
        }

        public void UpdatePoint()
        {
            pos = barycentricCoordinates[0] * containerTetrahedron.node0.pos + barycentricCoordinates[1] * containerTetrahedron.node1.pos
                + barycentricCoordinates[2] * containerTetrahedron.node2.pos + barycentricCoordinates[3] * containerTetrahedron.node3.pos;
        }

        public void SetContainerTetrahedron(Tetrahedron tetrahedron)
        {
            this.containerTetrahedron = tetrahedron;
            this.barycentricCoordinates = this.containerTetrahedron.CalculateBarycentricCoordinates(this.pos);
        }
    }
}