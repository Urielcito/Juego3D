using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveCamera : MonoBehaviour
{
    //cameraPosition es el lugarcito que le hacemos al objeto CameraPos que esta adentro del jugador
    public Transform cameraPosition;

    // Update is called once per frame
    private void LateUpdate()
    {
        transform.position = cameraPosition.position; // Al tener acceso al cameraPosition, lo que hacemos aca es modificar la posicion del objeto actual (CameraHolder, que contiene la camara first-person), y le asignamos la misma que tiene el objeto CameraPos del jugador.
    } // La camara real va afuera del jugador porque todo lo que es Rigidbodys y elementos fisicos de Unity estan capeados a 50 fps, si la camara es hijo de un objeto de fisicas o un rigidbody entonces toda su rotacion y movimiento tambien van a estar capeados a 50 fps.
}
