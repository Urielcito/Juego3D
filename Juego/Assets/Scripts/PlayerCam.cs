using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCam : MonoBehaviour
{
    public float sensX; // Sensibilidad del mouse en el eje X
    public float sensY; // Idem para eje Y

    public Transform orientation; //Objeto adentro del jugador que guarda la orientacion en la que esta.

    float xRotation; //Rotaciones en X e Y de la camara.
    float yRotation;
    
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked; // Al iniciar el script, bloquea el mouse en el centro y lo hace invisible.
        Cursor.visible = false;
    }

    void LateUpdate() // Al actualizarse el script,
    {
        float mouseX = Input.GetAxisRaw("Mouse X") * Time.deltaTime * sensX; // Consigue los valores crudos (raw) del mouse y los convierte en un valor alterado por el deltaTime y la sensibilidad definida anteriormente.
        float mouseY = Input.GetAxisRaw("Mouse Y") * Time.deltaTime * sensY;

        yRotation += mouseX; // Le suma mouseX a la rotacion Y de la camara

        xRotation -= mouseY; // Le resta mouseY a la rotacion X de la camara 

        // Para entender esto imaginemosnos la camara como si fuese un lapiz que apunta hacia adelante
        // Para que el lapiz, en la vida real, gire horizontalmente debemos "rotarlo" al rededor de su eje Y (como si hubiera un palo recto vertical en el centro del lapiz..), si rotamos el lapiz 90 grados
        // en el sentido de las agujas del reloj, ahora la punta que antes apuntaba hacia adelante apunta hacia la derecha.
        // Para hacer que el lapiz ahora apunte hacia arriba, hay que "rotarlo" al rededor de su eje X (como si hubiera otro palo que lo atraviesa desde el centro perpendicular al otro palo, como si fuera una cruz),
        // y si rotamos el lapiz 90 grados en sentido antihorario, ahora la punta del lapiz (que antes apuntaba hacia la derecha) apunta hacia arriba.
        // La punta del lapiz representa adonde estamos mirando con nuestro personaje, entonces lo que hacemos nosotros al girar el mouse hacia la izquierda o derecha,
        // seria como si estuvieramos desatornillando o atornillando un tornillo en el piso, y si movemos el mouse hacia arriba o hacia abajo,
        // es como si el tornillo en vez de estar en el piso esta en la pared. PERDON si no se entiende gurises pero fue la manera mas de bebe de 5 años que se me ocurrio para explicar, si no entienden les hago un paint

        xRotation = Mathf.Clamp(xRotation, -90f, 90f); // Esto limita la cantidad que podemos mirar hacia arriba o hacia abajo hasta los 90 grados en ambas direcciones

        transform.rotation = Quaternion.Euler(xRotation, yRotation, 0); // Se aplica la rotacion que calculamos al transform.rotation (transform se refiere a la propiedad Transform del objeto, que en Unity te deja cambiar la posicion y la rotacion, por lo tanto al usar transform.position estamos accediendo a esas propiedades y modificandolas desde el codigo directamente)
        orientation.rotation = Quaternion.Euler(0, yRotation, 0); // Se aplica SOLAMENTE la rotacion horizontal que calculamos en la Orientacion, va a servir para rotar el personaje, la rotacion vertical no se aplica porque sino el personaje quedaria acostado en el aire o algo por el estilo

    }
}
