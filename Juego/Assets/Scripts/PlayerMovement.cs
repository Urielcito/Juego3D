using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{

    [Header("Movement")] // Los headers son los apartados en los que podemos cambiar los valores en Unity, los valores que se cambian son los que se declaran justo debajo (solo los publicos).
    private float moveSpeed;
    public float walkSpeed; // Distintas velocidades para los distintos estados de movimiento
    public float sprintSpeed;
    public float groundDrag;

    [Header("Jumping")]
    public float jumpForce; // Fuerza del impulso vertical que agregamos al personaje cuando saltamos
    public float jumpCooldown; // Enfriamiento del salto 
    public float airMultiplier; // Multiplicador de la velocidad cuando estamos en el aire
    bool readyToJump;

    [Header("Crouching")]
    public float crouchSpeed; // Velocidad al entrar en el estado crouching
    public float crouchYScale; // Tamanio al que se encoge el personaje y su hitbox cuando nos agachamos (para cosas como pasar por huecos y eso)
    private float startYScale; // Tamanio que tiene el personaje justo antes de agacharse (tamanio original)
    

    [Header("Keybinds")]
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode crouchKey = KeyCode.LeftControl;
    public KeyCode sprintKey = KeyCode.LeftShift;

    [Header("Ground Check")]
    public float playerHeight; // Extrae la escala Y del objeto PlayerObj
    public float rayScale; // Tamano del rayo que apunta hacia el piso para calcular si el personaje esta tocando el piso
    public LayerMask whatIsGround; // Capa que se le pone a los objetos con los que queremos que nos apliquen friccion al personaje (groundDrag)
    bool grounded; // bool para chequear si el personaje esta en el piso

    [Header("Slope Handling")]
    public float maxSlopeAngle;
    public RaycastHit slopeHit;
    public bool exitingSlope;

    public Transform orientation;

    float horizontalInput;
    float verticalInput;

    Vector3 moveDirection; // Un Vector3 es un conjunto de 3 valores (x,y,z) como esta es la direccion del movimiento guarda los valores que representan como nos movemos en los 3 ejes (pa arriba pa abajo y pa los costados)

    Rigidbody rb;

    public MovementState state; // Estado de movimiento
    public enum MovementState // Tipo de valor que nosotros creamos para almacenar los distintos estados y referenciarlos en el codigo
    {
        walking, sprinting, crouching, air
    }

    //^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^//
    //-------------------------------------------------------- PARAMETROS ------------------------------------------------------------------//
    //vvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvv START, UPDATE, FIXEDUPDATE vvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvv//
    

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true; // Si no hacemos freezeRotation el personaje se cae :v
        ResetJump();
        startYScale = playerHeight; // La propiedad startYScale usada para agacharnos se inicializa como la altura del personaje
    }

    private void Update()
    {

        grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * rayScale, whatIsGround); // Dispara un rayo hacia abajo de X tamanio y se fija si toco la layer llamada whatIsGround (esencialmente devuelve true si estamos tocando el piso)
        // Cada vez que se actualiza el script chequeamos:
        // - Si el jugador esta apretando alguna tecla
        // - El estado en el que deberia estar el personaje, dependiendo principalmente de la entrada del jugador
        // - La velocidad que deberia tener el personaje, relativo a la superficie en la que esta, si esta agachado o saltando
        MyInput();
        SpeedControl(); 
        StateHandler();

        if (grounded) // En este if asignamos la friccion que va a aplicarse al jugador si este se localiza en el suelo
            rb.drag = groundDrag;
        else
            rb.drag = 0;

    }

    private void FixedUpdate() // El metodo FixedUpdate() es un ciclo que corre independientemente del Update() normal y el LateUpdate() a 50 Hz.
    {
        MovePlayer(); // Por cada ciclo de FixedUpdate calculamos la posicion, direccion y velocidad del movimiento general del jugador.
    }

    //----------------------------------------------------------------ACCIONES Y MOVIMIENTOS-------------------------------------------------------//

    private void MyInput() // En este metodo se chequean las entradas del jugador.
    {
        horizontalInput = Input.GetAxisRaw("Horizontal"); 
        verticalInput = Input.GetAxisRaw("Vertical");
        // Aparentemente Horizontal y Vertical tienen por defecto a las teclas WASD, no se como cambiar esto todavia.

        if (Input.GetKey(jumpKey) && readyToJump && grounded) // Si el usuario toca el espacio, podemos saltar y estamos en el piso...
        {
            readyToJump = false;

            Jump();

            Invoke(nameof(ResetJump), jumpCooldown);
        } // Hace que no podamos saltar de nuevo (readyToJump = False), que saltemos e invoca al metodo ResetJump (que pone nevamente en true el valor readyToJump) con un delay de jumpCooldown segundos.


        
        if (Input.GetKeyDown(crouchKey)) //empezar a agacharse
        {
            transform.localScale = new Vector3(transform.localScale.x, crouchYScale, transform.localScale.z); // Transforma la escala del personaje SOLO de la Y.
            rb.AddForce(Vector3.down * 10f, ForceMode.Impulse); // Si no le aplicaramos esta fuerza al personaje quedariamos en el aire porque la escala reduce el tamanio desde arriba y desde abajo simultaneamente
        }
        if (Input.GetKeyUp(crouchKey)) //dejamos de agacharnos
        {
            transform.localScale = new Vector3(transform.localScale.x, startYScale, transform.localScale.z); // Transformamos la escala Y del personaje a su valor original.
        }

    }

    private void Jump() // Metodo en el que entramos cuando el jugador presiona la tecla ESPACIO
    {
        exitingSlope = true; // Pone esta variable en true para que al estar en una superficie inclinada active la gravedad y nos deje saltar.
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z); // Deja la velocidad Y en 0 para que la fuerza que vamos a aplicarle no se vea influenciada de ninguna manera y hace que el personaje SIEMPRE salte la misma cantidad.

        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse); // Impulso vertical que le provocamos al jugador para que "salte"

    }



    private void MovePlayer() // Aca se calcula la direccion en la que se mueve:
    {
        moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput; // Agregandole las inputs horizontal y vertical a las propiedades forward y right de la Orientacion ubicada en el personaje.

        if (OnSlope() && !exitingSlope) // Manejo de superficies inclinadas...
        {
            rb.AddForce(GetSlopeMoveDirection() * moveSpeed * 20f, ForceMode.Force); // La fuerza aplicada al jugador cuando estamos en una superficie inclinada debe ser paralela al angulo de inclinacion de la superficie, para eso llamamos al metodo GetSlopeMoveDirection()

            if (rb.velocity.y > 0) // Esta fuerza hacia abajo la aplicamos porque de la manera en la que implementamos el movimiento en inclinaciones, el terreno o plataforma "empuja" al jugador en la misma direccion del angulo de inclinacion, por lo tanto empuja al jugador hacia arriba en cierto grado, por lo que hay que corregirlo constantemente para que no se despegue de la superficie.
            {
                rb.AddForce(Vector3.down * 80f, ForceMode.Force);
            }
        }

        if (grounded) // Se aplica la fuerza que mueve al personaje sobre una superficie recta.
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);



        else if (!grounded) // Se aplica la fuerza que mueve el personaje que esta en el aire.
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f * airMultiplier, ForceMode.Force);


        rb.useGravity = !OnSlope(); // Se desactiva la gravedad cuando el personaje se encuentra en una superficie inclinada para que no se resbale hacia abajo al quedarse quieto.
    }


    //-------------------------------------------------CHEQUEOS Y CONTROLES--------------------------------------------------//

    private void SpeedControl()
    {

        if (OnSlope() && !exitingSlope) // Cuando el personaje esta en una superficie inclinada y no esta queriendo salir de ella:
        {
            if (rb.velocity.magnitude > moveSpeed)
                rb.velocity = rb.velocity.normalized * moveSpeed; // Chequea si la velocidad general (en cualquier direccion) del personaje es mas grande de la que deberia, y la corrige.
        }
        else // Cuando el personaje no esta en una superficie inclinada: (en una superficie recta o en el aire)
        {
            Vector3 flatVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

            if (flatVel.magnitude > moveSpeed)
            {
                Vector3 limitedVel = flatVel.normalized * moveSpeed;
                rb.velocity = new Vector3(limitedVel.x, rb.velocity.y, limitedVel.z); // Se hace el chequeo comun y corriente en la velocidad. Me gustaria explicar en que se diferencian exactamente pero son las 5:16 AM
            }
        }
    }

    private bool OnSlope() // Chequea si el jugador esta en una superficie inclinada.
    {
        if (Physics.Raycast(transform.position, Vector3.down, out slopeHit, playerHeight * rayScale))  // Dispara un rayo que devuelve el angulo con el que choca contra la superficie inclinada como slopeHit.
        {
            float angle = Vector3.Angle(Vector3.up, slopeHit.normal); // Almacena el slopeHit en la variable angle despues de filtrarla un poquito.
            return angle < maxSlopeAngle && angle != 0; // Devuelve que estamos en una superficie inclinada solo si el angulo de ella no es demasiado grande y si no es un angulo proveniente de una superficie recta (en esos casos no se realiza el SlopeHandling porque no es necesario)
        }

        return false; // Si el rayo no toca nada, devuelve que no estamos en una superficie inclinada.
    }

    private Vector3 GetSlopeMoveDirection() // Toma toda la informacion que tenemos de la superficie inclinada y proyecta la nueva direccion en la que debemos aplicar la fuerza de movimiento.
    {
        return Vector3.ProjectOnPlane(moveDirection, slopeHit.normal).normalized;
    }

    private void ResetJump() // Auto explicativo..
    {
        readyToJump = true;
        exitingSlope = false;
    }

    private void StateHandler() // Chequeamos en que estado deberia estar el jugador..
    {
        // Modo - Agachado
        if (Input.GetKey(crouchKey))
        {
            state = MovementState.crouching;
            moveSpeed = crouchSpeed;
        }
        // Modo - Corriendo
        if (grounded && Input.GetKey(sprintKey))
        {
            state = MovementState.sprinting;
            moveSpeed = sprintSpeed;
        }

        // Modo - Walking
        else if (grounded && !Input.GetKey(crouchKey))
        {
            state = MovementState.walking;
            moveSpeed = walkSpeed;
        }
        // Modo - Aire
        else
        {
            state = MovementState.air;
        }
    }// Creo que es autoexplicativo, sino me preguntan
}
