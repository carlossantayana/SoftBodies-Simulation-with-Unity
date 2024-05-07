using Assets.Scripts;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

//Componente que permite animar un s�lido 3D deformable utilizando el m�todo masa-muelle.
//En este caso, las f�sicas actuar�n sobre un mallado de tetraedros que envuelve el asset visual, y los v�rtices de este asset visual seguir�n el movimiento del mallado
//previamente mencionado, aplicando un proceso de skinning.
public class MassSpring : MonoBehaviour
{
    public bool paused; //Booleano que se encarga de pausar y reanudar la animaci�n.

    //Enumeraci�n de los m�todos de integraci�n a utilizar.
    public enum Integration
    {
        ExplicitEulerIntegration = 0,
        SymplecticEulerIntegration = 1,
    }

    public Integration integrationMethod; //Variable con la que escoger el m�todo de integraci�n a utilizar.

    public TextAsset nodesFile; //Documento de texto que contiene las coordenadas iniciales de los v�rtices del mallado de tetraedros que sirve de volumen envolvente.
    Vector3[] envelopeVertices; //Array que almacena las posiciones de los v�rtices de la envolvente (extraidas del documento anterior).

    List<Node> envelopeNodes; //Lista de objetos de la clase nodo que almacenan las propiedas f�sicas de los v�rtices del mallado de tetraedros para el
                              //c�lculo de la animaci�n.
    List<Spring> envelopeSprings = new List<Spring>(); //Lista de objetos de la clase muelle que almacenan las propiedades f�sicas de cada muelle y los 2 v�rtices
                                                       //que lo componen del mallado de tetraedros para el c�lculo de la animaci�n.

    public TextAsset tetrahedronsFile; //Documento de texto que almacena los �ndices de los cuatro v�rtices de cada tetraedro. Estos �ndices comienzan en 1.
    int[] tetrahedrons; //Array que almacena cuatro enteros por tetraedro (extraidos del documento anterior). Estos �ndices comienzan en 0.
    List<Edge> edges = new List<Edge>(); //Lista que almacena todas las aristas de la malla de tetraedros.
    List<Tetrahedron> tetrahedronsList = new List<Tetrahedron>(); //Lista que almacena los tetraedros del mallado con referencias a los nodos que lo componen.

    Mesh assetMesh; //Mallado triangular del objeto 3D o asset visual.
    Vector3[] assetVertices; //Array que almacena en cada posici�n una copia de la posici�n 3D de cada v�rtice del mallado del asset visual.
    List<Point> assetPoints; //Lista de v�rtices del asset visual como objetos de la clase Punto. Adem�s de la posici�n del v�rtice, almacenan una referencia al tetraedro
                             //contenedor y las coordenadas baric�ntricas del v�rtice.

    public float objectDensity = 0.005f; //Densidad de masa del objeto.

    public float tractionSpringStiffnessDensity = 100f; //Constante de densidad de rigidez de los muelles de tracci�n.

    public float dAbsolute = 0.002f; //Constante de amortiguamiento (damping) absoluto sobre la velocidad de los nodos.
    public float dDeformation = 0.02f; //Constante de amortiguamiento de la deformaci�n de los muelles.

    public Vector3 g = new Vector3(0.0f, 9.8f, 0.0f); //Constante gravitacional.

    public float h = 0.01f; //Tama�o del paso de integraci�n de las f�sicas de la animaci�n.
    private float hChangeCheck; //Variable para comprobar si cambi� el valor de el paso de integraci�n.

    public int substeps = 1; //N�mero de subpasos. Se divide la integraci�n las veces que indique por frame.
    private int substepsChangeCheck; //Variable para comprobar si cambi� el valor de el n�mero de substeps.

    private float h_def; //Paso efectivo finalmente utilizado en la integraci�n. Puede diferir de h en caso de que substeps > 1.

    private Wind wind; //Componente del viento y sus propiedades para aplicar la fuerza de este sobre el objeto 3D.

    // Start is called before the first frame update
    void Start()
    {
        paused = true; //Al comienzo de la ejecuci�n, la animaci�n se encuentra pausada.

        //Se leen los ficheros del volumen envolvente para almacenar la posici�n de sus v�rtices y cuales conforman cada tetraedro.
        ReadNodesFile();
        ReadTetrahedronsFile();

        //Se inicializa el valor de los comprobadores al valor inicial de las variables originales.
        hChangeCheck = h;
        substepsChangeCheck = substeps;

        wind = GameObject.Find("Wind").GetComponent<Wind>(); //Se almacena el componente del viento del gameObject "Wind".

        h_def = h / substeps; //El paso efectivo es igual al paso base divido entre el n�mero de subpasos a realizar por frame. Se utiliza finalmente un paso inferior,
                              //lo que supone controlar mejor el margen de error.

        envelopeNodes = new List<Node>(envelopeVertices.Length); //Se crea una lista con tantos nodos como v�rtices del mallado de tetraedros.

        for (int i = 0; i < envelopeVertices.Length; i++)
        {
            //Se insertan en la lista cada uno de los v�rtices, almacen�ndose su identificador y su posici�n en coordenadas globales.
            envelopeNodes.Add(new Node(i, transform.TransformPoint(envelopeVertices[i])));
        }


        foreach (Node node in envelopeNodes) //Para cada nodo del mallado de tetraedros
        {
            //Se buscan los Fixer hijos del objeto 3D.
            foreach (Fixer fixer in gameObject.GetComponentsInChildren<Fixer>()) //Para cada Fixer
            {
                if (!node.fixedNode) //Si a�n no se ha fijado
                {
                    node.fixedNode = fixer.CheckFixerContainsPoint(node.pos); //Se comprueba si el fixer actual lo contiene, y por tanto, lo fija.
                }
            }
        }

        for (int i = 0; i < tetrahedrons.Length; i += 4) //Recorremos los tetraedros.
        {
            //Se crea el tetraedro con referencias a los nodos de los que est� compuesto y se a�ade a la lista de tetraedros.
            Tetrahedron tetrahedron = new Tetrahedron(envelopeNodes[tetrahedrons[i]], envelopeNodes[tetrahedrons[i + 1]],
                envelopeNodes[tetrahedrons[i + 2]], envelopeNodes[tetrahedrons[i + 3]], objectDensity);
            tetrahedronsList.Add(tetrahedron);

            //Se crean las 6 aristas del tetraedro. Cada una de estas aristas posee 1/6 del volumen del tetraedro que las contiene.
            Edge A = new Edge(tetrahedrons[i], tetrahedrons[i + 1], tetrahedron.volume / 6);
            Edge B = new Edge(tetrahedrons[i], tetrahedrons[i + 2], tetrahedron.volume / 6);
            Edge C = new Edge(tetrahedrons[i], tetrahedrons[i + 3], tetrahedron.volume / 6);
            Edge D = new Edge(tetrahedrons[i + 1], tetrahedrons[i + 2], tetrahedron.volume / 6);
            Edge E = new Edge(tetrahedrons[i + 1], tetrahedrons[i + 3], tetrahedron.volume / 6);
            Edge F = new Edge(tetrahedrons[i + 2], tetrahedrons[i + 3], tetrahedron.volume / 6);

            //Se a�aden al array de aristas.
            edges.Add(A); edges.Add(B); edges.Add(C); edges.Add(D); edges.Add(E); edges.Add(F);
        }

        //Para eliminar aristas duplicadas y crear la estructura DCEL se utiliza el siguiente algoritmo de coste O(N*log(N)):

        edges.Sort(); //Se necesita tener las aristas ordenadas. De esta forma, al recorrer el array, podemos detectar si justo se repiti� una arista.

        Edge previousEdge = null; //Almacenamos una referencia a la arista anterior.
        Spring previousSpring = null; //Almacenamos una referencia al �ltimo muelle creado.

        for (int i = 0; i < edges.Count; i++) //Recorremos las aristas.
        {
            if (edges[i].Equals(previousEdge)) //Si la arista actual es igual a la anterior (es una arista compartida y por tanto repetida)
            {
                //No se crea un nuevo muelle, pues solo se quiere uno por arista distinta, pero se a�ade su volumen al del �ltimo muelle creado.
                previousSpring.springVolume += edges[i].edgeVolume;
            }
            else //Si no
            {
                //Creamos un muelle de tracci�n en la arista.
                previousSpring = new Spring(tractionSpringStiffnessDensity, envelopeNodes[edges[i].vertexA], envelopeNodes[edges[i].vertexB]);
                //Cada arista que determine un muelle forma parte de un tetraedro distinto y aportan su volumen al del muelle.
                previousSpring.springVolume += edges[i].edgeVolume;
                //Se agrega el muelle creado a la lista de muelles de la envolvente.
                envelopeSprings.Add(previousSpring);
            }

            previousEdge = edges[i]; //Actualizamos la referencia a la arista anterior.
        }

        assetMesh = gameObject.GetComponentInChildren<MeshFilter>().mesh; //Se almacena una referencia al mallado del asset.
                                                                          //Al haberse importado un ".obj", el mallado del objeto se encuentra en un objeto hijo.
        assetVertices = assetMesh.vertices; //Se almacena una copia de cada uno de los v�rtices del mallado del asset en un array.

        assetPoints = new List<Point>(assetVertices.Length); //Se crea una lista con tantos objetos Punto como v�rtices tenga el asset visual.

        for (int i = 0; i < assetVertices.Length; i++)
        {
            assetPoints.Add(new Point(transform.TransformPoint(assetVertices[i]))); //Se a�ade cada v�rtice del asset a la lista de puntos en coordenadas globales.
        }

        foreach (Point point in assetPoints) //Por cada punto del asset visual.
        {
            foreach (Tetrahedron tetrahedron in tetrahedronsList) //Por cada tetraedro de la envolvente.
            {
                if (tetrahedron.TetrahedronContainsPoint(point.pos)) //Si el tetraedro contiene al punto.
                {
                    point.SetContainerTetrahedron(tetrahedron); //Se establece dicho tetraedro como tetraedro contenedor del punto y se calculan entonces sus coordenadas baric�ntricas.
                }
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyUp(KeyCode.P)) //Pulsar la tecla P activa/desactiva la pausa de la animaci�n.
        {
            paused = !paused;
        }

        //En caso de que alguna de las copias de los valores originales difiera de este (pues puede ser modificado desde el inspector), se actualizar� el tama�o del paso efectivo de
        //integraci�n. A su vez, se actualizar� la copia, una vez realizados los cambios.
        //Esto nos permite actualizar el paso de integraci�n efectivo en tiempo de ejecuci�n.

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

    //La integraci�n de las f�sicas se realiza en la actualizaci�n de paso fijo, pues as� se evita la acumulaci�n de error.
    private void FixedUpdate()
    {
        if (paused) //Si la animaci�n est� pausada no hacemos nada.
        {
            return;
        }

        //Si la animaci�n no est� pausada.
        for (int step = 0; step < substeps; step++) //Se realizan uno o varios substeps.
        {
            switch (integrationMethod) //En funci�n del m�todo de integraci�n seleccionado se ejecuta uno u otro.
            {
                case Integration.ExplicitEulerIntegration:
                    IntegrateExplicitEuler();
                    break;
                case Integration.SymplecticEulerIntegration:
                    IntegrateSymplecticEuler();
                    break;
                default:
                    Debug.Log("Error: m�todo de integraci�n no encontrado");
                    break;
            }

            foreach (Spring spring in envelopeSprings) //Se recorre la lista de muelles tras realizar la integraci�n.
            {
                spring.UpdateSpring(); //Se recalculan los datos del muelle en el siguiente instante.
            }

            foreach (Point point in assetPoints) //Se recorre la lista de puntos del asset visual.
            {
                point.UpdatePoint(); //Se actualiza la posici�n de cada punto en funci�n de las nuevas coordenadas integradas de los nodos de su tetraedro contenedor.
            }

            for (int i = 0; i < assetVertices.Length; i++)
            {
                //Se actualiza la copia del array de v�rtices, pasando de coordenadas globales a locales las nuevas posiciones de los puntos.
                assetVertices[i] = transform.InverseTransformPoint(assetPoints[i].pos);
            }

            assetMesh.vertices = assetVertices; //Se asigna al array de v�rtices del mallado la copia del array de v�rtices modificado.
            assetMesh.RecalculateBounds(); //Se recalculan los bordes de la malla.
        }
    }

    void IntegrateExplicitEuler() //M�todo que realiza la integraci�n de la velocidad y la posici�n utilizando Euler Expl�cito.
    {
        foreach (Node node in envelopeNodes) //Para cada nodo
        {
            if (!node.fixedNode) //Que no sea fijo
            {
                node.pos += h_def * node.vel; //Se integra primero la posici�n, con la velocidad del paso actual.
            }

            node.force = -node.mass * g; //Se aplica la fuerza de la gravedad

            //Se agrega la fuerza del viento en funci�n de su intensidad, de la m�xima fuerza que puede alcanzar, y su direcci�n. Tambi�n se agrega
            //un cierto grado de aleatoriedad para cada nodo en cada frame, pues el viento nunca permanece completamente constante.
            node.force += (wind.WindIntensity * wind.maxWindForce * UnityEngine.Random.Range(0f, 1f)) * wind.WindDirection;

            ApplyDampingNode(node); //Se aplica la fuerza de amortiguamiento absoluto al nodo
        }

        //Para cada muelle, se aplica la fuerza el�stica a los dos nodos que lo componen, en sentidos opuestos por el principio de acci�n y reacci�n.
        foreach (Spring spring in envelopeSprings)
        {
            spring.nodeA.force += -(spring.springVolume / Mathf.Pow(spring.lenght0, 2)) * spring.k * (spring.lenght - spring.lenght0)
                * ((spring.nodeA.pos - spring.nodeB.pos) / spring.lenght0);
            spring.nodeB.force += (spring.springVolume / Mathf.Pow(spring.lenght0, 2)) * spring.k * (spring.lenght - spring.lenght0)
                * ((spring.nodeA.pos - spring.nodeB.pos) / spring.lenght0);
            ApplyDampingSpring(spring); //Se aplica la fuerza de amortiguamiento de la deformaci�n a cada uno de los nodos que componen el muelle.
        }

        foreach (Node node in envelopeNodes) //Para cada nodo
        {
            if (!node.fixedNode) //Que no sea fijo
            {
                node.vel += h_def * node.force / node.mass; //Se integra la velocidad con la fuerza actual calculada.
            }
        }
    }

    void IntegrateSymplecticEuler() //M�todo que realiza la integraci�n de la velocidad y la posici�n utilizando Euler Simpl�ctico.
    {
        foreach (Node node in envelopeNodes) //Para cada nodo
        {
            node.force = -node.mass * g; //Se aplica la fuerza de la gravedad.

            //Se agrega la fuerza del viento en funci�n de su intensidad, de la m�xima fuerza que puede alcanzar, y su direcci�n. Tambi�n se agrega
            //un cierto grado de aleatoriedad para cada nodo en cada frame, pues el viento nunca permanece completamente constante.
            node.force += (wind.WindIntensity * wind.maxWindForce * UnityEngine.Random.Range(0f, 1f)) * wind.WindDirection;

            ApplyDampingNode(node); //Se aplica la fuerza de amortiguamiento absoluto al nodo
        }

        //Para cada muelle, se aplica la fuerza el�stica a los dos nodos que lo componen, en sentidos opuestos por el principio de acci�n y reacci�n.
        foreach (Spring spring in envelopeSprings)
        {
            spring.nodeA.force += -(spring.springVolume/Mathf.Pow(spring.lenght0, 2)) * spring.k * (spring.lenght - spring.lenght0) 
                * ((spring.nodeA.pos - spring.nodeB.pos) / spring.lenght0);
            spring.nodeB.force += (spring.springVolume / Mathf.Pow(spring.lenght0, 2)) * spring.k * (spring.lenght - spring.lenght0)
                * ((spring.nodeA.pos - spring.nodeB.pos) / spring.lenght0);
            ApplyDampingSpring(spring); //Se aplica la fuerza de amortiguamiento de la deformaci�n a cada uno de los nodos que componen el muelle.
        }

        foreach (Node node in envelopeNodes) //Para cada nodo
        {
            if (!node.fixedNode) //Que no sea fijo
            {
                node.vel += h_def * node.force / node.mass; //Se integra la velocidad con la fuerza actual calculada.
                node.pos += h_def * node.vel; //Se utiliza la velocidad en el paso siguiente para integrar la nueva posici�n.
            }
        }
    }

    void ApplyDampingNode(Node node) //M�todo que aplica el amortiguamiento absoluto del nodo.
    {
        //Se logra aplicando una fuerza proporcional a la velocidad del nodo ajustada por el coeficiente de amortiguamiento absoluto, en sentido contrario de la velocidad.
        //Simula el rozamiento con el aire.
        node.force += -dAbsolute * node.vel;
    }

    void ApplyDampingSpring(Spring spring) //M�todo que aplica el amortiguamiento de la deformaci�n del muelle.
    {
        //A cada nodo del muelle se le aplica la misma fuerza en sentido contrario por el principio de acci�n y reacci�n.
        //La fuerza de amortiguamiento de la deformaci�n depende de las velocidades relativas de los nodos del muelle y la direcci�n del muelle,
        //ajustada por un coeficiente de amortiguamiento de la deformaci�n.
        spring.nodeA.force += -dDeformation * Vector3.Dot(spring.dir, (spring.nodeA.vel - spring.nodeB.vel)) * spring.dir;
        spring.nodeB.force += dDeformation * Vector3.Dot(spring.dir, (spring.nodeA.vel - spring.nodeB.vel)) * spring.dir;
    }

    private void OnDrawGizmos() //Funci�n de evento de Unity que se ejecuta en cada vuelta del bucle del juego para redibujar los Gizmos.
    {
        if (Application.isPlaying) //Se dibujar�n �nicamente durante la ejecuci�n de la aplicaci�n, pues es al inicio de esta que se crean
                                   //los muelles y nodos de la envolvente.
        {
            Gizmos.color = Color.red; //Muelles de tracci�n de color rojo.

            foreach (Spring spring in envelopeSprings) //Se recorre la lista de muelles.
            {
                Gizmos.DrawLine(spring.nodeA.pos, spring.nodeB.pos); //Se dibuja una l�nea entre el par de nodos del muelle.
            }

            Gizmos.color = Color.black; //Nodos de color negro.

            foreach (Node node in envelopeNodes) //Se recorren los nodos.
            {
                Gizmos.DrawSphere(node.pos, 0.2f); //Se pinta una esfera en cada uno de los nodos.
            }
        }
    } //Estos Gizmos nos permiten ver en tiempo real el movimiento de los nodos y los muelles.

    //M�todo que se llama en caso de que se haya modificado el tama�o del paso de integraci�n o el n�mero de subpasos a realizar por frame, actualizando el paso efectivo.
    private void UpdateIntegrationStep()
    {
        h_def = h / substeps;
    }

    //Parser para leer el fichero de tetraedros.
    void ReadTetrahedronsFile()
    {
        List<int> tetrahedrons = new List<int>();

        string[] lines = tetrahedronsFile.text.Split("\n", System.StringSplitOptions.RemoveEmptyEntries);

        for (int i = 1; i < lines.Length - 1; i++)
        {
            string line = lines[i];
            string[] values = line.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
            for (int j = 1; j < values.Length; j++)
            {
                string value = values[j].Trim('\r');
                tetrahedrons.Add(int.Parse(value) - 1);
            }
        }

        this.tetrahedrons = tetrahedrons.ToArray();
    }

    //Parser para leer el fichero de nodos.
    void ReadNodesFile()
    {
        List<Vector3> vertices = new List<Vector3>();

        string[] lines = nodesFile.text.Split("\n", System.StringSplitOptions.RemoveEmptyEntries);

        for (int i = 1; i < lines.Length - 1; i++)
        {
            string line = lines[i];
            string[] values = line.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
            float valueX = float.Parse(values[1].Trim('\r'), CultureInfo.InvariantCulture);
            float valueY = float.Parse(values[3].Trim('\r'), CultureInfo.InvariantCulture);
            float valueZ = float.Parse(values[2].Trim('\r'), CultureInfo.InvariantCulture);
            Vector3 vertex = new Vector3(valueX, valueY, valueZ);
            vertices.Add(vertex);
        }

        this.envelopeVertices = vertices.ToArray();
    }
}
