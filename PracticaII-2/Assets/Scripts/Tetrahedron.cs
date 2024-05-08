using System.Collections;
using UnityEngine;

namespace Assets.Scripts
{
    //Clase Tetraedro. Representa un tetraedro del mallado envolvente del objeto 3D.
    public class Tetrahedron
    {
        //Cada tetraedro está compuesto de 4 nodos:
        public Node node0;
        public Node node1;
        public Node node2;
        public Node node3;

        public float volume; //Volumen del tetraedro. Se calcula a partir de las posiciones de sus vértices.
        public float mass; //Masa del tetraedro. Dadas la densidad y el volumen, se obtiene la masa de este.
        private float density; //Densidad definida para el objeto 3D, se recibe en el constructor.

        //Constructor de objetos tetraedro. Recibe los 4 nodos que lo componen y la densidad del objeto 3D.
        public Tetrahedron(Node node0, Node node1, Node node2, Node node3, float density)
        {
            this.node0 = node0;
            this.node1 = node1;
            this.node2 = node2;
            this.node3 = node3;

            volume = CalculateVolume(this.node0.pos, this.node1.pos, this.node2.pos, this.node3.pos); //Se calcula el volumen a partir de las posiciones de sus nodos.
            this.density = density;
            mass = this.density * volume; //Se despeja la masa de la fórmula de la densidad y se calcula.

            //Se reparte la masa del tetraedro entre sus cuatro nodos.
            this.node0.mass += mass / 4;
            this.node1.mass += mass / 4;
            this.node2.mass += mass / 4;
            this.node3.mass += mass / 4;
        }

        //Método que calcula el volumen de un tetraedro a partir de las posiciones de sus cuatro vértices.
        //Para ello se aplica la siguiente fórmula: V = |(r1-r0)*(r2-r0)x(r3-r0)| / 6
        private float CalculateVolume(Vector3 r0, Vector3 r1, Vector3 r2, Vector3 r3)
        {
            //Se utilizan A, B y C como vectores auxiliares para almacenar las restas de vectores posición.
            Vector3 A = r1 - r0;
            Vector3 B = r2 - r0;
            Vector3 C = r3 - r0;

            //Devuelve el volumen tras aplicar la fórmula.
            return Mathf.Abs(Vector3.Dot(A, Vector3.Cross(B, C))) / 6;
        }

        //Método que ofrece la clase Tetraedro para calcular las coordenadas baricéntricas de un punto P respecto al tetraedro que invoque la función.
        public Vector4 CalculateBarycentricCoordinates(Vector3 P)
        {
            //Las coordenadas baricéntricas de un punto respecto a un tetraedro son 4 valores, 1 por nodo del tetraedro.
            //La fórmula de cada valor es la siguiente: wi = Vi/V
            //Se calculan como el volumen del tetraedro formado por el punto P y el resto de nodos del tetraedro excepto el i, entre el volumen del tetraedro original.
            float w0 = CalculateVolume(P, node1.pos, node2.pos, node3.pos) / volume;
            float w1 = CalculateVolume(node0.pos, P, node2.pos, node3.pos) / volume;
            float w2 = CalculateVolume(node0.pos, node1.pos, P, node3.pos) / volume;
            float w3 = CalculateVolume(node0.pos, node1.pos, node2.pos, P) / volume;

            //Se devuelven ordenadas en forma de Vector4.
            return new Vector4(w0, w1, w2, w3);
        }

        //Método que ofrece la clase Tetraedro para determinar si un punto P se encuentra en el interior del tetraedro que invoque la función.
        public bool TetrahedronContainsPoint(Vector3 P)
        {
            //Para determinar si un punto P se encuentra contenido en el tetraedro se debe comparar su posición con respecto a las normales de cada una de las caras del tetraedro.
            //Teniendo en cuenta la ordenación de los nodos dada por la herramienta TetGen:
            //1º: se obtienen dos vectores pertenecientes a cada cara.
            //2º: se calcula el producto vectorial que devuelve el vector en sentido hacia el interior del tetraedro. Se debe tener en cuenta que Unity utiliza
            //un sistema de coordenadas a izquierdas, por lo que el producto vectorial se hace a la inversa.
            //3º: se obtiene un vector que va desde uno de los vértices de la cara hasta el punto P.
            //4º: se calcula el producto escalar entre el vector normal de la cara y el vector que va hacia el punto P.
            //5º: si el producto escalar es positivo, significa que ambos vectores apuntan hacia el mismo lado de la cara del tetraedro, de manera que el punto P estaría hacia
            //el interior respecto a la cara.
            //6º: si es positivo en todas las caras, el punto P está en el interior del tetraedro. Con que uno no sea positivo, no estaría contenido en el tetraedro.

            //CARA DE LOS NODOS 0, 1 Y 2 DEL TETRAEDRO
            Vector3 v01 = node1.pos - node0.pos;
            Vector3 v02 = node2.pos - node0.pos;
            Vector3 normal012 = Vector3.Cross(v02, v01).normalized;
            Vector3 v0P = (P - node0.pos).normalized;
            float dot012 = Vector3.Dot(normal012, v0P);

            //CARA DE LOS NODOS 0, 1 Y 3 DEL TETRAEDRO
            Vector3 v03 = node3.pos - node0.pos;
            Vector3 normal013 = Vector3.Cross(v01, v03).normalized;
            float dot013 = Vector3.Dot(normal013, v0P);

            //CARA DE LOS NODOS 1, 2 Y 3 DEL TETRAEDRO
            Vector3 v21 = node1.pos - node2.pos;
            Vector3 v23 = node3.pos - node2.pos;
            Vector3 normal123 = Vector3.Cross(v23, v21).normalized;
            Vector3 v2P = (P - node2.pos).normalized;
            float dot123 = Vector3.Dot(normal123, v2P);

            //CARA DE LOS NODOS 0, 2 Y 3 DEL TETRAEDRO
            Vector3 normal023 = Vector3.Cross(v03, v02).normalized;
            float dot023 = Vector3.Dot(normal023, v0P);

            if (dot012 >= 0 && dot013 >= 0 && dot123 >= 0 && dot023 >= 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}