using Assets.Scripts;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Componente que permite animar una tela utilizando el método masa-muelle.
public class MassSpringCloth : MonoBehaviour
{
    public bool paused; //Booleano que se encarga de pausar y reanudar la animación.

    //Enumeración de los métodos de integración a utilizar.
    public enum Integration
    {
        ExplicitEulerIntegration = 0,
        SymplecticEulerIntegration = 1,
    }

    public Integration integrationMethod; //Variable con la que escoger el método de integración a utilizar.

    Mesh mesh; //Mallado triangular de la tela.
    Vector3[] vertices; //Array que almacena en cada posición una copia de la posición 3D de cada vértice del mallado.
    List<Node> nodes; //Lista de objetos de la clase nodo que almacenan las propiedas físicas de los vértices del mallado para el cálculo de la animación.
    List<Spring> springs = new List<Spring>(); //Lista de objetos de la clase muelle que almacena las propiedades físicas de cada muelle y los 2 vértices que lo componen
                                               //para el cálculo de la animación.
    int[] triangles; //Lista que almacena 3 enteros por triángulo de la malla.
    List<Edge> edges = new List<Edge>(); //Lista que almacena todas las aristas de la malla.

    public float clothMass = 1f; //Masa total de la tela, repartida equitativamente entre cada uno de los nodos de masa que la componen.
    private float clothMassChangeCheck; //Variable para comprobar si cambió el valor de la masa.

    public float tractionSpringStiffness = 20f; //Constante de rigidez de los muelles de tracción. La tela no es muy elástica.
    private float tractionSpringStiffnessChangeCheck; //Variable para comprobar si cambió el valor de la rigidez de tracción.

    public float flexionSpringStiffness = 6f; //Constante de rigidez de los muelles de flexión. Sin embargo, sí se dobla fácilmente.
    private float flexionSpringStiffnessChangeCheck; //Variable para comprobar si cambió el valor de la rigidez de flexión.

    public float dAbsolute = 0.002f; //Constante de amortiguamiento (damping) absoluto sobre la velocidad de los nodos.
    public float dDeformation = 0.02f; //Constante de amortiguamiento de la deformación de los muelles.

    public Vector3 g = new Vector3(0.0f, 9.8f, 0.0f); //Constante gravitacional.

    public float h = 0.01f; //Tamaño del paso de integración de las físicas de la animación.
    private float hChangeCheck; //Variable para comprobar si cambió el valor de el paso de integración.

    public int substeps = 1; //Número de subpasos. Se divide la integración las veces que indique por frame.
    private int substepsChangeCheck; //Variable para comprobar si cambió el valor de el número de substeps.

    private float h_def; //Paso efectivo finalmente utilizado en la integración. Puede diferir de h en caso de que substeps > 1.

    private Wind wind; //Componente del viento y sus propiedades para aplicar la fuerza de este sobre la tela.

    // Start is called before the first frame update
    void Start()
    {
        paused = true; //Al comienzo de la ejecución, la animación se encuentra pausada.

        //Se inicializa el valor de los comprobadores al valor inicial de las variables originales.
        clothMassChangeCheck = clothMass;
        flexionSpringStiffnessChangeCheck = flexionSpringStiffness;
        tractionSpringStiffnessChangeCheck = tractionSpringStiffness;
        hChangeCheck = h;
        substepsChangeCheck = substeps;

        wind = GameObject.Find("Wind").GetComponent<Wind>(); //Se almacena el componente del viento del gameObject "Wind".

        h_def = h / substeps; //El paso efectivo es igual al paso base divido entre el número de subpasos a realizar por frame. Se utiliza finalmente un paso inferior,
                              //lo que supone controlar mejor el margen de error.

        mesh = gameObject.GetComponent<MeshFilter>().mesh; //Se almacena una referencia al mallado del gameObject.
        vertices = mesh.vertices; //Se almacena una copia de cada uno de los vértices del mallado en un array.
        nodes = new List<Node>(vertices.Length); //Se crea una lista con tantos nodos como vértices.

        for (int i = 0; i < vertices.Length; i++)
        {
            //Se insertan en la lista cada uno de los vértices, almacenándose su identificador, su posición en coordenadas globales,
            //y la parte proporcional que le corresponde de la masa total de la tela.
            nodes.Add(new Node(i, transform.TransformPoint(vertices[i]), clothMass / vertices.Length));
        }


        foreach (Node node in nodes) //Para cada nodo
        {
            //Se buscan los Fixer hijos de la tela.
            foreach (Fixer fixer in gameObject.GetComponentsInChildren<Fixer>()) //Para cada Fixer
            {
                if (!node.fixedNode) //Si aún no se ha fijado
                {
                    node.fixedNode = fixer.CheckFixerContainsPoint(node.pos); //Se comprueba si el fixer actual lo contiene, y por tanto, lo fija.
                }
            }
        }


        triangles = mesh.triangles; //Array que almacena en 3 posiciones consecutivas los índices de los vértices de cada triángulo.

        for (int i = 0; i < triangles.Length; i += 3) //Recorremos los triángulos.
        {
            //Se crean las 3 aristas del triángulo
            Edge A = new Edge(triangles[i], triangles[i + 1], triangles[i + 2]);
            Edge B = new Edge(triangles[i], triangles[i + 2], triangles[i + 1]);
            Edge C = new Edge(triangles[i + 1], triangles[i + 2], triangles[i]);

            //Se añaden al array de aristas.
            edges.Add(A); edges.Add(B); edges.Add(C);
        }

        //Para eliminar aristas duplicadas y crear la estructura DCEL se utiliza el siguiente algoritmo de coste O(N*log(N)):

        edges.Sort(); //Se necesita tener las aristas ordenadas. De esta forma, al recorrer el array, podemos detectar si justo se repitió una arista.

        Edge previousEdge = null; //Almacenamos una referencia a la arista anterior

        for (int i = 0; i < edges.Count; i++) //Recorremos las aristas.
        {
            if (edges[i].Equals(previousEdge)) //Si la arista actual es igual a la anterior (es una arista repetida)
            {
                //Aprovechamos el vértice opuesto a la arista para crear el muelle de flexión. Se almacena el tipo de muelle en forma de string.
                springs.Add(new Spring(flexionSpringStiffness, nodes[edges[i].vertexOther], nodes[previousEdge.vertexOther], "flexion"));
            }
            else //Si no
            {
                //Agregamos un muelle de tracción en la arista. Se almacena el tipo de muelle en forma de string.
                springs.Add(new Spring(tractionSpringStiffness, nodes[edges[i].vertexA], nodes[edges[i].vertexB], "traction"));
            }

            previousEdge = edges[i]; //Actualizamos la referencia a la arista anterior.
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyUp(KeyCode.P)) //Pulsar la tecla P activa/desactiva la pausa de la animación.
        {
            paused = !paused;
        }

        //En caso de que alguna de las copias de los valores originales difiera de este (pues puede ser modificado desde el inspector), se actualizará la masa de los nodos,
        //la rigidez de los muelles, o el tamaño del paso efectivo de integración. A su vez, se actualizará la copia, una vez realizados los cambios.
        //Esto nos permite actualizar la masa de los nodos, la rigidez de los muelles y el paso de integración efectivo en tiempo de ejecución.

        if (clothMassChangeCheck != clothMass)
        {
            UpdateNodeMass();
            clothMassChangeCheck = clothMass;
        }

        if (flexionSpringStiffnessChangeCheck != flexionSpringStiffness)
        {
            UpdateSpringStiffness();
            flexionSpringStiffnessChangeCheck = flexionSpringStiffness;
        }

        if (tractionSpringStiffnessChangeCheck != tractionSpringStiffness)
        {
            UpdateSpringStiffness();
            tractionSpringStiffnessChangeCheck = tractionSpringStiffness;
        }

        if (hChangeCheck != h)
        {
            UpdateIntegrationStep();
            hChangeCheck = h;
        }

        if (substepsChangeCheck != substeps)
        {
            UpdateIntegrationStep();
            substepsChangeCheck = substeps;
        }
    }

    //La integración de las físicas se realiza en la actualización de paso fijo, pues así se evita la acumulación de error.
    private void FixedUpdate()
    {
        if (paused) //Si la animación está pausada no hacemos nada.
        {
            return;
        }

        //Si la animación no está pausada.
        for (int step = 0; step < substeps; step++) //Se realizan uno o varios substeps.
        {
            switch (integrationMethod) //En función del método de integración seleccionado se ejecuta uno u otro.
            {
                case Integration.ExplicitEulerIntegration:
                    IntegrateExplicitEuler();
                    break;
                case Integration.SymplecticEulerIntegration:
                    IntegrateSymplecticEuler();
                    break;
                default:
                    Debug.Log("Error: método de integración no encontrado");
                    break;
            }

            foreach (Spring spring in springs) //Se recorre la lista de muelles tras realizar la integración.
            {
                spring.UpdateSpring(); //Se recalculan los datos del muelle en el siguiente instante.
            }

            for (int i = 0; i < vertices.Length; i++)
            {
                //Se actualiza la copia del array de vértices, pasando de coordenadas globales a locales las nuevas posiciones de los nodos.
                vertices[i] = transform.InverseTransformPoint(nodes[i].pos);
            }

            mesh.vertices = vertices; //Se asigna al array de vértices del mallado la copia del array de vértices modificado.
            mesh.RecalculateBounds(); //Se recalculan los bordes de la malla.
        }
    }

    void IntegrateExplicitEuler() //Método que realiza la integración de la velocidad y la posición utilizando Euler Explícito.
    {
        foreach (Node node in nodes) //Para cada nodo
        {
            if (!node.fixedNode) //Que no sea fijo
            {
                node.pos += h_def * node.vel; //Se integra primero la posición, con la velocidad del paso actual.
            }

            node.force = -node.mass * g; //Se aplica la fuerza de la gravedad

            //Se agrega la fuerza del viento en función de su intensidad, de la máxima fuerza que puede alcanzar, y su dirección. También se agrega
            //un cierto grado de aleatoriedad para cada nodo en cada frame, pues el viento nunca permanece completamente constante.
            node.force += (wind.WindIntensity * wind.maxWindForce * Random.Range(0f, 1f)) * wind.WindDirection;

            ApplyDampingNode(node); //Se aplica la fuerza de amortiguamiento absoluto al nodo
        }

        //Para cada muelle, se aplica la fuerza elástica a los dos nodos que lo componen, en sentidos opuestos por el principio de acción y reacción.
        foreach (Spring spring in springs)
        {
            spring.nodeA.force += -spring.k * (spring.lenght - spring.lenght0) * spring.dir;
            spring.nodeB.force += spring.k * (spring.lenght - spring.lenght0) * spring.dir;
            ApplyDampingSpring(spring); //Se aplica la fuerza de amortiguamiento de la deformación a cada uno de los nodos que componen el muelle.
        }

        foreach (Node node in nodes) //Para cada nodo
        {
            if (!node.fixedNode) //Que no sea fijo
            {
                node.vel += h_def * node.force / node.mass; //Se integra la velocidad con la fuerza actual calculada.
            }
        }
    }

    void IntegrateSymplecticEuler() //Método que realiza la integración de la velocidad y la posición utilizando Euler Simpléctico.
    {
        foreach (Node node in nodes) //Para cada nodo
        {
            node.force = -node.mass * g; //Se aplica la fuerza de la gravedad.

            //Se agrega la fuerza del viento en función de su intensidad, de la máxima fuerza que puede alcanzar, y su dirección. También se agrega
            //un cierto grado de aleatoriedad para cada nodo en cada frame, pues el viento nunca permanece completamente constante.
            node.force += (wind.WindIntensity * wind.maxWindForce * Random.Range(0f, 1f)) * wind.WindDirection;

            ApplyDampingNode(node); //Se aplica la fuerza de amortiguamiento absoluto al nodo
        }

        //Para cada muelle, se aplica la fuerza elástica a los dos nodos que lo componen, en sentidos opuestos por el principio de acción y reacción.
        foreach (Spring spring in springs)
        {
            spring.nodeA.force += -spring.k * (spring.lenght - spring.lenght0) * spring.dir;
            spring.nodeB.force += spring.k * (spring.lenght - spring.lenght0) * spring.dir;
            ApplyDampingSpring(spring); //Se aplica la fuerza de amortiguamiento de la deformación a cada uno de los nodos que componen el muelle.
        }

        foreach (Node node in nodes) //Para cada nodo
        {
            if (!node.fixedNode) //Que no sea fijo
            {
                node.vel += h_def * node.force / node.mass; //Se integra la velocidad con la fuerza actual calculada.
                node.pos += h_def * node.vel; //Se utiliza la velocidad en el paso siguiente para integrar la nueva posición.
            }
        }
    }

    void ApplyDampingNode(Node node) //Método que aplica el amortiguamiento absoluto del nodo.
    {
        //Se logra aplicando una fuerza proporcional a la velocidad del nodo ajustada por el coeficiente de amortiguamiento absoluto, en sentido contrario de la velocidad.
        //Simula el rozamiento con el aire.
        node.force += -dAbsolute * node.vel;
    }

    void ApplyDampingSpring(Spring spring) //Método que aplica el amortiguamiento de la deformación del muelle.
    {
        //A cada nodo del muelle se le aplica la misma fuerza en sentido contrario por el principio de acción y reacción.
        //La fuerza de amortiguamiento de la deformación depende de las velocidades relativas de los nodos del muelle y la dirección del muelle,
        //ajustada por un coeficiente de amortiguamiento de la deformación.
        spring.nodeA.force += -dDeformation * Vector3.Dot(spring.dir, (spring.nodeA.vel - spring.nodeB.vel)) * spring.dir;
        spring.nodeB.force += dDeformation * Vector3.Dot(spring.dir, (spring.nodeA.vel - spring.nodeB.vel)) * spring.dir;
    }

    private void OnDrawGizmos() //Función de evento de Unity que se ejecuta en cada vuelta del bucle del juego para redibujar los Gizmos.
    {
        if (Application.isPlaying) //Se dibujarán únicamente durante la ejecución de la aplicación, pues es al inicio de esta que se crean los muelles y nodos de la tela.
        {
            foreach (Spring spring in springs) //Se recorre la lista de muelles, y en función del tipo del muelle se utiliza un color u otro.
            {
                if (spring.springType == "flexion")
                {
                    Gizmos.color = Color.blue; //Muelles de flexión de color azul.
                }
                else
                {
                    Gizmos.color = Color.red; //Muelles de tracción de color rojo.
                }

                Gizmos.DrawLine(spring.nodeA.pos, spring.nodeB.pos); //Se dibuja una línea entre el par de nodos del muelle.
            }

            Gizmos.color = Color.black; //Se cambia a color negro.

            foreach (Node node in nodes) //Se recorren los nodos.
            {
                Gizmos.DrawSphere(node.pos, 0.05f); //Se pinta una esfera en cada uno de los nodos.
            }
        }
    } //Estos Gizmos nos permiten ver en tiempo real el movimiento de los vértices y los distintos tipos de muelles.

    //Método que se llama en caso de que la masa de la tela se haya modificado desde el inspector, actualizando las masas de cada uno de los nodos.
    private void UpdateNodeMass()
    {
        foreach (Node node in nodes)
        {
            node.mass = clothMass / vertices.Length;
        }
    }

    //Método que se llama en caso de que se haya modificado la rigidez de los muelles desde el inspector, actualizando las constantes de rigidez de cada uno de los muelles,
    //respetando sus tipos.
    private void UpdateSpringStiffness()
    {
        foreach (Spring spring in springs)
        {
            if (spring.springType == "flexion")
            {
                spring.k = flexionSpringStiffness;
            }
            else
            {
                spring.k = tractionSpringStiffness;
            }
        }
    }

    //Método que se llama en caso de que se haya modificado el tamaño del paso de integración o el número de subpasos a realizar por frame, actualizando el paso efectivo.
    private void UpdateIntegrationStep()
    {
        h_def = h / substeps;
    }
}
