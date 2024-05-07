using System.Collections;
using UnityEngine;

namespace Assets.Scripts
{
    public class Node //Clase que representa un nodo de masa discreta de un mallado envolvente de tetraedros de un objeto 3D en el modelo masa-muelle.
    {
        public int vertexID; //ID del vértice del mallado al que está asociado el nodo.
        public float mass = 0; //Masa del nodo en Kg. Cada tetraedro al que pertenezca el nodo le aportará 1/4 de su masa.
        public Vector3 pos; //Posición del nodo en el espacio 3D.
        public Vector3 vel; //Velocidad del nodo en el espacio 3D.
        public Vector3 force; //Fuerza que está siendo aplicada sobre el nodo.
        public bool fixedNode = false; //Booleano que indica si se trata de un nodo fijo. Por defecto ninguno lo es.

        public Node(int id, Vector3 initPos) //Constructor de objetos de la clase nodo en su estado inicial.
        {
            vertexID = id;
            pos = initPos;
        }
    }
}