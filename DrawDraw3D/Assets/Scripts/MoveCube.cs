/**
 * Ardity (Serial Communication for Arduino + Unity)
 * Author: Daniel Wilches <dwilches@gmail.com>
 *
 * This work is released under the Creative Commons Attributions license.
 * https://creativecommons.org/licenses/by/2.0/
 */

using UnityEngine;
using System.Threading;
using System.Collections.Generic;


/**
 * This class allows a Unity program to continually check for messages from a
 * serial device.
 *
 * It creates a Thread that communicates with the serial port and continually
 * polls the messages on the wire.
 * That Thread puts all the messages inside a Queue, and this SerialController
 * class polls that queue by means of invoking SerialThread.GetSerialMessage().
 *
 * The serial device must send its messages separated by a newline character.
 * Neither the SerialController nor the SerialThread perform any validation
 * on the integrity of the message. It's up to the one that makes sense of the
 * data.
 */
public class MoveCube : MonoBehaviour
{
    /* ARDITY COMMUNICATION STUFF*/
    [Tooltip("Port name with which the SerialPort object will be created.")]
    public string portName = "COM6";
    [Tooltip("Baud rate that the serial device is using to transmit data.")]
    public int baudRate = 9600;
    [Tooltip("Reference to an scene object that will receive the events of connection, " +
             "disconnection and the messages from the serial device.")]
    public GameObject messageListener;
    [Tooltip("After an error in the serial communication, or an unsuccessful " +
             "connect, how many milliseconds we should wait.")]
    public int reconnectionDelay = 1000;
    [Tooltip("Maximum number of unread data messages in the queue. " +
             "New messages will be discarded.")]
    public int maxUnreadMessages = 1;

    /* MOVEMENT STUFF */
    public float movementMultiplier = 0;
    public float xSpeed;
    public float ySpeed;
    public float zSpeed;
    public Rigidbody rb;

    /* MECHANICS AND SPAWN CONTROL */
    public Vector3 respawnPoint;
    public Transform playerCursor;
    public GameObject current_colour;
    public List<GameObject> colours = new List<GameObject>();
    public GameObject line;
    public int undoPointer = 0;
    public List<GameObject> lines = new List<GameObject>();
    public int button;
    public int colour;
    public bool isPressed = false;
    public float clearTimer = 2;


    // Constants used to mark the start and end of a connection. There is no
    // way you can generate clashing messages from your serial device, as I
    // compare the references of these strings, no their contents. So if you
    // send these same strings from the serial device, upon reconstruction they
    // will have different reference ids.
    public const string SERIAL_DEVICE_CONNECTED = "__Connected__";
    public const string SERIAL_DEVICE_DISCONNECTED = "__Disconnected__";

    // Internal reference to the Thread and the object that runs in it.
    protected Thread thread;
    protected SerialThreadLines serialThread;

    // Invoked when a line of data is received from the serial device.
    void OnMessageArrived(string msg)
    {
        string[] data = msg.Split(',');
        button = int.Parse(data[0]);
        colour = int.Parse(data[4]);

        // MOVEMENT OF Y AXIS
        if (float.Parse(data[2]) != 0)
        {
            ySpeed += float.Parse(data[2]);
            rb.AddForce(Vector3.up * (ySpeed) * movementMultiplier);
            if(rb.velocity.sqrMagnitude > 1)
            {
                Vector3.up.Normalize();
            }
        }
        else
        {
            ySpeed = 0;
            rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
        }
        // MOVEMENT OF X AXIS
        if (float.Parse(data[1]) != 0)
        {
            xSpeed += float.Parse(data[1]);
            rb.AddForce(Vector3.right * (xSpeed) * movementMultiplier);
            if (rb.velocity.sqrMagnitude > 1)
            {
                Vector3.right.Normalize();
            }
        }
        else
        {
            xSpeed = 0;
            rb.velocity = new Vector3(0, rb.velocity.y, rb.velocity.z);
        }
        // MOVEMENT OF Z AXIS
        if (float.Parse(data[3]) != 0)
        {
            zSpeed += float.Parse(data[3]);
            rb.AddForce(Vector3.forward * (zSpeed) * movementMultiplier);
            if (rb.velocity.sqrMagnitude > 1)
            {
                Vector3.forward.Normalize();
            }
        }
        else
        {
            zSpeed = 0;
            rb.velocity = new Vector3(rb.velocity.x, rb.velocity.y, 0);
        }
    }

    // Invoked when a connect/disconnect event occurs. The parameter 'success'
    // will be 'true' upon connection, and 'false' upon disconnection or
    // failure to connect.
    void OnConnectionEvent(bool success)
    {
        if (success)
            Debug.Log("Connection established");
        else
            Debug.Log("Connection attempt failed or disconnection detected");
    }

    // ------------------------------------------------------------------------
    // Invoked whenever the SerialController gameobject is activated.
    // It creates a new thread that tries to connect to the serial device
    // and start reading from it.
    // ------------------------------------------------------------------------
    void OnEnable()
    {
        serialThread = new SerialThreadLines(portName,
                                             baudRate,
                                             reconnectionDelay,
                                             maxUnreadMessages);
        thread = new Thread(new ThreadStart(serialThread.RunForever));
        thread.Start();
    }

    // ------------------------------------------------------------------------
    // Invoked whenever the SerialController gameobject is deactivated.
    // It stops and destroys the thread that was reading from the serial device.
    // ------------------------------------------------------------------------
    void OnDisable()
    {
        // If there is a user-defined tear-down function, execute it before
        // closing the underlying COM port.
        if (userDefinedTearDownFunction != null)
            userDefinedTearDownFunction();

        // The serialThread reference should never be null at this point,
        // unless an Exception happened in the OnEnable(), in which case I've
        // no idea what face Unity will make.
        if (serialThread != null)
        {
            serialThread.RequestStop();
            serialThread = null;
        }

        // This reference shouldn't be null at this point anyway.
        if (thread != null)
        {
            thread.Join();
            thread = null;
        }
    }

    // ------------------------------------------------------------------------
    // Polls messages from the queue that the SerialThread object keeps. Once a
    // message has been polled it is removed from the queue. There are some
    // special messages that mark the start/end of the communication with the
    // device.
    // ------------------------------------------------------------------------
    void Update()
    {
        if (Input.GetKeyDown("space"))
        {
            transform.position = respawnPoint;
        }
        else if (button == 2 && isPressed == false)
        {
            line = Instantiate(current_colour, playerCursor);
            lines.Add(line);
            isPressed = true;
        }
        else if (button == 4 && isPressed == false)
        {
            ClearLastLine();
            isPressed = true;
        }
        else if (button == 0 && isPressed == true)
        {
            if (line != null)
            {
                line.transform.parent = null;
            }
            isPressed = false;
            clearTimer = 2;
        }
        if (button == 4)
        {
            clearTimer -= Time.deltaTime;
            if (clearTimer <= 0)
            {
                ClearLines();
                clearTimer = 2;
            }
        }
        //Change colours
        if (button == 1 || button == 8)
        {                   //If the user changes colour
            current_colour = colours[colour];
        }


        if (Input.GetKey("w")) 
        {
            playerCursor.transform.Translate(Vector3.forward * Time.deltaTime *movementMultiplier);
        }
        if (Input.GetKey("s"))
        {
            playerCursor.transform.Translate(Vector3.back * Time.deltaTime * movementMultiplier);
        }
        if (Input.GetKey("a"))
        {
            playerCursor.transform.Translate(Vector3.left * Time.deltaTime * movementMultiplier);
        }
        if (Input.GetKey("d"))
        {
            playerCursor.transform.Translate(Vector3.right * Time.deltaTime * movementMultiplier);
        }
        if (Input.GetKey(KeyCode.LeftShift))
        {
            playerCursor.transform.Translate(Vector3.up * Time.deltaTime * movementMultiplier);
        }
        if (Input.GetKey(KeyCode.LeftControl))
        {
            playerCursor.transform.Translate(Vector3.down * Time.deltaTime * movementMultiplier);
        }


        // If the user prefers to poll the messages instead of receiving them
        // via SendMessage, then the message listener should be null.
        if (messageListener == null)
            return;

        // Read the next message from the queue
        string message = (string)serialThread.ReadMessage();
        if (message == null)
            return;

        // Check if the message is plain data or a connect/disconnect event.
        if (ReferenceEquals(message, SERIAL_DEVICE_CONNECTED))
            messageListener.SendMessage("OnConnectionEvent", true);
        else if (ReferenceEquals(message, SERIAL_DEVICE_DISCONNECTED))
            messageListener.SendMessage("OnConnectionEvent", false);
        else
            messageListener.SendMessage("OnMessageArrived", message);


        // Draw line
        if (Input.GetKeyDown("x"))
        {
            line = Instantiate(current_colour, playerCursor);
            lines.Add(line);
        }
        else if (Input.GetKeyUp("x"))
        {
            line.transform.parent = null;
        }
        // Clears all lines (make it hold-to-clear, as it is irreversible)
        if (Input.GetKeyDown("r"))
        {
            for (int i = 0; i < lines.Count; i++)
            {
                Destroy(lines[i]);
            }
            lines.Clear();
        }
        // Undo function
        if (Input.GetKeyDown("z")) //undo
        {
            //Doesn't actually remove an element, but set it inactive
        }
    }

    // ------------------------------------------------------------------------
    // Returns a new unread message from the serial device. You only need to
    // call this if you don't provide a message listener.
    // ------------------------------------------------------------------------
    public string ReadSerialMessage()
    {
        // Read the next message from the queue
        return (string)serialThread.ReadMessage();
    }

    // ------------------------------------------------------------------------
    // Puts a message in the outgoing queue. The thread object will send the
    // message to the serial device when it considers it's appropriate.
    // ------------------------------------------------------------------------
    public void SendSerialMessage(string message)
    {
        serialThread.SendMessage(message);
    }

    // ------------------------------------------------------------------------
    // Executes a user-defined function before Unity closes the COM port, so
    // the user can send some tear-down message to the hardware reliably.
    // ------------------------------------------------------------------------
    public delegate void TearDownFunction();
    private TearDownFunction userDefinedTearDownFunction;
    public void SetTearDownFunction(TearDownFunction userFunction)
    {
        this.userDefinedTearDownFunction = userFunction;
    }

    public void ClearLines()
    {
        for (int i = 0; i < lines.Count; i++)
        {
            Destroy(lines[i]);
        }
        lines.Clear();
    }

    public void ClearLastLine()
    {
        if (lines.Count>0) //prevent IndexOutOfRangeException for empty list
        {
            Destroy(lines[lines.Count-1]);
            lines.RemoveAt(lines.Count-1);
        }

    }
}
