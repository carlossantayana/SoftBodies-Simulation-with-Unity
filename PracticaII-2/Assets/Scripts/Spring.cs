using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.Scripts
{
    public class Spring //Clase que representa un muelle que aporta elasticidad a un objeto en el modelo masa-muelle.
    {
        public float k; //Constante de rigidez del muelle. A mayor k, más rígido será el muelle, oponiéndose a las deformaciones.
        public float lenght; //Longitud del muelle en un instante.
        public float lenght0; //Longitud en reposo del muelle.
        public Vector3 dir; //Dirección del muelle desde el nodo B al nodo A.
        public string springType; //string que almacena si se trata de un muelle de tracción o de flexión.

        //Cada muelle conecta dos nodos:
        public Node nodeA;
        public Node nodeB;

        public Spring(float springStiffness, Node nodeA, Node nodeB, string springType) //Constructor de la clase muelle que lo inicializa a su estado inicial.
        {
            k = springStiffness;
            this.nodeA = nodeA;
            this.nodeB = nodeB;
            this.springType = springType;

            dir = nodeA.pos - nodeB.pos; //Vector dirección del muelle que va del nodo B al nodo A.

            lenght0 = dir.magnitude; //El módulo de ese vector es su longitud en reposo.

            lenght = lenght0; //También es su longitud instantánea inicial.

            dir = Vector3.Normalize(dir); //Normalizamos el vector dirección, pues no necesitamos el módulo para los cálculos.
        }

        //Método utilizado para recalcular la dirección y longitud del muelle en cada instante.
        public void UpdateSpring() 
        {
            dir = nodeA.pos - nodeB.pos; //La nueva dirección.
            lenght = dir.magnitude; //La nueva longitud instantánea. Se puede apreciar que la longitud en reposo no cambia.
            dir = Vector3.Normalize(dir); //Normalizamos la dirección.
        }
    }
}