using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Componente que permite comprobar si un Vector3 se encuentra en el interior del Collider del gameObject al que está asociado.
public class Fixer : MonoBehaviour
{
    private Collider fixerCollider;
    private MeshRenderer fixerMeshRenderer;

    private void Awake()
    {
        fixerCollider = gameObject.GetComponent<Collider>();
        fixerMeshRenderer = gameObject.GetComponent<MeshRenderer>();
        fixerMeshRenderer.enabled = false; //Desactivamos el renderizado de la malla del gameObject para que esta no sea visible durante la ejecución de la animación.
    }

    //Método que recibe un Vector3, devolviendo true si el punto se encuentra en el interior del collider y false en caso contrario.
    public bool CheckFixerContainsPoint(Vector3 point)
    {
        return fixerCollider.bounds.Contains(point);
    }
}
