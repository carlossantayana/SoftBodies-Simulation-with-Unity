using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Clase Arista. Representa una arista de una malla envolvente de tetraedros de un objeto 3D.
public class Edge : IComparable<Edge> 
{
    //Par de v�rtices que componen una de las aristas del tetraedro.
    public int vertexA;
    public int vertexB;

    public float edgeVolume; //Volumen que le corresponde a una arista perteneciente a un tetraedro. Al tener estos 6 aristas, se trata de 1/6 del
                             //volumen total del tetraedro contenedor.

    //El constructor inserta los v�rtices A y B de forma ordenada y recibe la parte de volumen que le corresponde.
    public Edge(int vertexA, int vertexB, float volume)
    {
        if (vertexA <= vertexB)
        {
            this.vertexA = vertexA;
            this.vertexB = vertexB;
        }
        else
        {
            this.vertexA = vertexB;
            this.vertexB = vertexA;
        }

        edgeVolume = volume;
    }

    //Implementamos el m�todo CompareTo de la interfaz IComparable para poder ordenar los objetos Edge dentro de una lista.
    //Necesario para eliminar las aristas repetidas del mallado.
    public int CompareTo(Edge other)
    {
        if (this.vertexA < other.vertexA)
        {
            return -1;
        }
        else if (this.vertexA == other.vertexA)
        {
            if (this.vertexB < other.vertexB)
            {
                return -1;
            }
            else if(this.vertexB == other.vertexB)
            {
                return 0;
            }
            else
            {
                return 1;
            }
        }
        else
        {
            return 1;
        }
    }

    //Sobreescribimos el m�todo Equals de Object con una llamada a nuestra propia implementaci�n.
    public override bool Equals(object obj)
    {
        return Equals(obj as Edge);
    }

    //Implementamos un m�todo Equals para comprobar si dos aristas son iguales. Necesario para hallar aristas repetidas.
    private bool Equals(Edge other)
    {
        if (other == null)
        {
            return false;
        }

        if (vertexA == other.vertexA && vertexB == other.vertexB)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
}
