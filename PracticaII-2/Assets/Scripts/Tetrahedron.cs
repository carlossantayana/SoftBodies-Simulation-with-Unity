using System.Collections;
using UnityEngine;

namespace Assets.Scripts
{
    public class Tetrahedron
    {
        Node node1;
        Node node2;
        Node node3;
        Node node4;

        public Tetrahedron(Node node1, Node node2, Node node3, Node node4)
        {
            this.node1 = node1;
            this.node2 = node2;
            this.node3 = node3;
            this.node4 = node4;
        }
    }
}