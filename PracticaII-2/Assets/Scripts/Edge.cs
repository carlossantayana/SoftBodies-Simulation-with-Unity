using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Clase Arista. Representa una arista de una malla de un objeto 3D y el v�rtice opuesto del tri�ngulo al que forma parte.
public class Edge : IComparable<Edge> 
{
    public int vertexA;
    public int vertexB;
    public int vertexOther;

    //El constructor inserta los v�rtices A y B de forma ordenada.
    public Edge(int vertexA, int vertexB, int vertexOther)
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

        this.vertexOther = vertexOther;
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
