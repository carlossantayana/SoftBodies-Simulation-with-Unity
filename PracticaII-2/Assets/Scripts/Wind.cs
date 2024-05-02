using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wind : MonoBehaviour //Componente que se encarga de gestionar el cambio de dirección e intensidad del viento, así como de realizar su representación visual.
{
    //Dirección del viento.
    private Vector3 _windDirection;
    [HideInInspector]
    public Vector3 WindDirection {  get { return _windDirection; } set {  _windDirection = value; } }

    //Intensidad del viento. Toma valores entre 0 y 1. Se inicializa a 0.
    private float _windIntensity = 0f;
    [HideInInspector]
    public float WindIntensity { get { return _windIntensity; } set { _windIntensity = value; } }

    public float maxWindForce = 1f; //Fuerza máxima que puede alcanzar el viento. Modificable desde el inspector.

    private float _rotationSpeed = 60f; //Velocidad con la que cambia la dirección del viento.
    private float _intensityIncrementationSpeed = 0.5f; //Velocidad con la que cambia la intensidad del viento.

    private const int CYLINDER_DEFAULT_SIZE = 2; //Constante en la que almacenamos la longitud por defecto de un cilindro de escala 1.
    private int _cylinderMaxSize = 10; //Variable en la que asignar la longitud máxima que adquirirá el cilindro al alcanzar el viento su máxima intensidad.

    private float _translationSpeed; //Velocidad a la que se moveran los "gizmos" utilizados para representar el viento.

    //Objetos que harán de "gizmos" para representar la intensidad y dirección del viento.
    GameObject intensity_gizmo;
    GameObject direction_gizmo;

    // Start is called before the first frame update
    void Start()
    {
        //Se buscan los "gizmos" que son objetos hijos del gameObject "Wind", el cual tiene este componente.
        intensity_gizmo = transform.Find("Wind_Intensity_Gizmo").gameObject;
        direction_gizmo = transform.Find("Wind_Direction_Gizmo").gameObject;

        _translationSpeed = _cylinderMaxSize * _intensityIncrementationSpeed;
        _windDirection = transform.forward; //Al inicio, la dirección del viento será la del vector forward del gameObject "Wind".
    }

    // Update is called once per frame
    void Update()
    {
        //Al pulsar las teclas de las flechas izquierda o derecha, el gameObject "Wind" rotará, y se actualizará la dirección del viento en función de su vector forward,
        //ahora rotado.
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            transform.Rotate(new Vector3(0f, -_rotationSpeed*Time.deltaTime, 0f));
            _windDirection = transform.forward;
        }

        if (Input.GetKey(KeyCode.RightArrow))
        {
            transform.Rotate(new Vector3(0f, _rotationSpeed * Time.deltaTime, 0f));
            _windDirection = transform.forward;
        }

        //Al pulsar las teclas de flecha hacia arriba y hacia abajo, incrementará y decrementará la intensidad del viento respectivamente,
        //manteniéndola entre los valores de 0 y 1. Se escalan y trasladan los "gizmos" para reflejar estos cambios.
        if (Input.GetKey(KeyCode.UpArrow))
        {
            if (_windIntensity < 1) //Si la intensidad del viento es inferior a 1.
            {
                _windIntensity += _intensityIncrementationSpeed * Time.deltaTime; //Se incrementa en _intensityIncrementationSpeed unidades por segundo.
                intensity_gizmo.transform.Translate(0, Time.deltaTime * _translationSpeed / 2, 0, Space.Self);
                direction_gizmo.transform.Translate(0, Time.deltaTime * _translationSpeed, 0, Space.Self);
            }
            else //Si se alcanza la intensidad de 1 o se supera.
            {
                _windIntensity = 1; //Se reasigna a 1.
            }

            intensity_gizmo.transform.localScale = new Vector3(intensity_gizmo.transform.localScale.x, _windIntensity * _cylinderMaxSize/CYLINDER_DEFAULT_SIZE, intensity_gizmo.transform.localScale.z);
        }

        if (Input.GetKey(KeyCode.DownArrow))
        {
            if(_windIntensity > 0) //Si la intensidad del viento es superior a 0.
            {
                _windIntensity -= _intensityIncrementationSpeed * Time.deltaTime; //Se decrementa en _intensityIncrementationSpeed unidades por segundo.
                intensity_gizmo.transform.Translate(0, -Time.deltaTime * _translationSpeed / 2, 0, Space.Self);
                direction_gizmo.transform.Translate(0, -Time.deltaTime * _translationSpeed, 0, Space.Self);
            }
            else //Si se alcanza la intensidad de 0 o se sobrepasa.
            {
                _windIntensity = 0; //Se reasigna a 0.
            }

            intensity_gizmo.transform.localScale = new Vector3(intensity_gizmo.transform.localScale.x, _windIntensity * _cylinderMaxSize / CYLINDER_DEFAULT_SIZE, intensity_gizmo.transform.localScale.z);
        }
    }
}
