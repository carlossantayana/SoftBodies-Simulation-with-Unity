using System.Collections;
using UnityEngine;

namespace Assets.Scripts
{
    //Clase Punto. Representa un vértice del mallado del asset visual.
    public class Point
    {
        public Vector3 pos; //Posición del vértice en el espacio 3D en coordenadas globales.
        private Tetrahedron containerTetrahedron; //Tetraedro del mallado envolvente que contiene al vértice.
        private Vector4 barycentricCoordinates; //Coordenadas baricéntricas del vértice en función del tetraedro previo. Se almacenan las 4 coordenadas baricéntricas del vértice en un
                                                //Vector4, de manera que en la posición 0 está la coordenada baricéntrica del nodo 0 del tetraedro contenedor, en la 1 la del nodo 1 y
                                                //así sucesivamente.

        //Constructor de objetos de la clase Punto. Recibe las coordenadas globales de un vértice del asset visual.
        public Point(Vector3 position)
        {
            this.pos = position;
        }

        //Método encargado de actualizar la posición del vértice en función de sus coordenadas baricéntricas y las nuevas posiciones de los nodos de su tetraedro contenedor.
        //Se aplica la siguiente fórmula: pos = sumatorio de i=0 a i=3(wi * ri)
        //Siendo wi la coordenada baricéntrica del vértice para el nodo i del tetraedro, y ri la posición del nodo i del tetraedro.
        public void UpdatePoint()
        {
            pos = barycentricCoordinates[0] * containerTetrahedron.node0.pos + barycentricCoordinates[1] * containerTetrahedron.node1.pos
                + barycentricCoordinates[2] * containerTetrahedron.node2.pos + barycentricCoordinates[3] * containerTetrahedron.node3.pos;
        }

        //Método llamado al encontrar el tetraedro que contiene al punto. Se le asigna el tetraedro contenedor y se utiliza para calcular las coordenadas baricéntricas del vértice.
        public void SetContainerTetrahedron(Tetrahedron tetrahedron)
        {
            this.containerTetrahedron = tetrahedron;
            this.barycentricCoordinates = this.containerTetrahedron.CalculateBarycentricCoordinates(this.pos);
        }
    }
}