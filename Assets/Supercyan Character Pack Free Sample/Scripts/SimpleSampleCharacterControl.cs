using System.Collections.Generic;
using UnityEngine;

public class SimpleSampleCharacterControl : MonoBehaviour
{
    private enum ControlMode
    {
        /// <summary>
        /// Up moves the character forward, left and right turn the character gradually and down moves the character backwards
        /// </summary>
        Tank,
        /// <summary>
        /// Character freely moves in the chosen direction from the perspective of the camera
        /// </summary>
        Direct
    }

    [SerializeField] private float m_moveSpeed = 2;
    [SerializeField] private float m_turnSpeed = 200;
    [SerializeField] private float m_jumpForce = 4;

    [SerializeField] private Animator m_animator = null;
    [SerializeField] private Rigidbody m_rigidBody = null;

    [SerializeField] private ControlMode m_controlMode = ControlMode.Direct;

    [SerializeField] float minX;
    [SerializeField] float maxX;
    [SerializeField] float minY;
    [SerializeField] float maxY;
    [SerializeField] float positionX;
    [SerializeField] float positionZ;
    [SerializeField][Range(0, 0.2f)] float mouvementSensibility;
    [SerializeField][Range(1, 20)] float expo = 10;


    // [SerializeField]  [Range(0, 1)] float x2 = 0.2f;
    // [SerializeField]  [Range(0, 1)] float y2 = 0.2f;

    private float m_currentV = 0;
    private float m_currentH = 0;

    private readonly float m_interpolation = 10;
    private readonly float m_walkScale = 0.33f;
    private readonly float m_backwardsWalkScale = 0.16f;
    private readonly float m_backwardRunScale = 0.66f;

    private bool m_wasGrounded;
    private Vector3 m_currentDirection = Vector3.zero;

    private float m_jumpTimeStamp = 0;
    private float m_minJumpInterval = 0.25f;
    private bool m_jumpInput = false;

    private bool m_isGrounded;

    private List<Collider> m_collisions = new List<Collider>();
    private Vector3 localPosition, direction;
    private Quaternion localRotation, tempRotation;
    private float deltaTime, velocity;

    private void Awake()
    {
        if (!m_animator) { gameObject.GetComponent<Animator>(); }
        if (!m_rigidBody) { gameObject.GetComponent<Animator>(); }
        localPosition = new Vector3(positionX, -0.5f, positionZ);
        localRotation = new Quaternion(0, 0, 0, 0);
    }

    private void OnCollisionEnter(Collision collision)
    {
        ContactPoint[] contactPoints = collision.contacts;
        for (int i = 0; i < contactPoints.Length; i++)
        {
            if (Vector3.Dot(contactPoints[i].normal, Vector3.up) > 0.5f)
            {
                if (!m_collisions.Contains(collision.collider))
                {
                    m_collisions.Add(collision.collider);
                }
                m_isGrounded = true;
            }
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        ContactPoint[] contactPoints = collision.contacts;
        bool validSurfaceNormal = false;
        for (int i = 0; i < contactPoints.Length; i++)
        {
            if (Vector3.Dot(contactPoints[i].normal, Vector3.up) > 0.5f)
            {
                validSurfaceNormal = true; break;
            }
        }

        if (validSurfaceNormal)
        {
            m_isGrounded = true;
            if (!m_collisions.Contains(collision.collider))
            {
                m_collisions.Add(collision.collider);
            }
        }
        else
        {
            if (m_collisions.Contains(collision.collider))
            {
                m_collisions.Remove(collision.collider);
            }
            if (m_collisions.Count == 0) { m_isGrounded = false; }
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (m_collisions.Contains(collision.collider))
        {
            m_collisions.Remove(collision.collider);
        }
        if (m_collisions.Count == 0) { m_isGrounded = false; }
    }

    private void Update()
    {
        transform.localPosition = localPosition;
        transform.localRotation = localRotation;
        deltaTime = Time.deltaTime;
        if (!m_jumpInput && Input.GetKey(KeyCode.Space))
        {
            m_jumpInput = true;
        }
    }

    private void FixedUpdate()
    {
        m_animator.SetBool("Grounded", m_isGrounded);

        switch (m_controlMode)
        {
            case ControlMode.Direct:
                DirectUpdate();
                break;

            case ControlMode.Tank:
                TankUpdate();
                break;

            default:
                Debug.LogError("Unsupported state");
                break;
        }

        m_wasGrounded = m_isGrounded;
        m_jumpInput = false;
    }

    private void TankUpdate()
    {
        float v = Input.GetAxis("Vertical");
        float h = Input.GetAxis("Horizontal");

        bool walk = Input.GetKey(KeyCode.LeftShift);

        if (v < 0)
        {
            if (walk) { v *= m_backwardsWalkScale; }
            else { v *= m_backwardRunScale; }
        }
        else if (walk)
        {
            v *= m_walkScale;
        }

        m_currentV = Mathf.Lerp(m_currentV, v, Time.deltaTime * m_interpolation);
        m_currentH = Mathf.Lerp(m_currentH, h, Time.deltaTime * m_interpolation);

        transform.position += transform.forward * m_currentV * m_moveSpeed * Time.deltaTime;
        transform.Rotate(0, m_currentH * m_turnSpeed * Time.deltaTime, 0);

        m_animator.SetFloat("MoveSpeed", m_currentV);

        JumpingAndLanding();
    }
    private float calcPosition(float max, float min, float percentage)
    {
        return (max - min) * percentage + min;
    }
    public void sendData(Coord coord)
    {
        // Récupération des coordonnées
        Vector2 percentage = new Vector2((coord.x / 100), (coord.y / 100));

        // Calcul des positions à partir de pourcentage
        float newPositionX = calcPosition(maxX, minX, percentage.x);
        float newPositionZ = calcPosition(maxY, minY, percentage.y);
        // newPositionX = Mathf.Lerp(newPositionX, positionX, deltaTime * m_interpolation);
        // newPositionZ = Mathf.Lerp(newPositionZ, positionZ, deltaTime * m_interpolation);

        direction = new Vector3(newPositionX, localPosition.y, newPositionZ) - localPosition;
        Debug.DrawRay(localPosition, direction, Color.green, 1);

        if (direction.magnitude > mouvementSensibility)
        {
            // Position
            positionX = newPositionX;
            positionZ = newPositionZ;
            localPosition.x = Mathf.Lerp(localPosition.x, positionX, deltaTime * m_interpolation);
            localPosition.z = Mathf.Lerp(localPosition.z, positionZ, deltaTime * m_interpolation);

            // direction
            float angle = Mathf.Atan2(direction.z, direction.x) * Mathf.Rad2Deg - 90;
            Quaternion angleAxis = Quaternion.AngleAxis(angle, Vector3.down);
            Quaternion newRotation = Quaternion.Slerp(localRotation, angleAxis, deltaTime * m_interpolation);
            localRotation = Quaternion.Lerp(newRotation, localRotation, deltaTime * m_interpolation);
        }
    }


    private void DirectUpdate()
    {
        direction = Vector3.Lerp(direction, Vector3.zero, Time.deltaTime * m_interpolation);
        float magnitude = direction.magnitude * expo;
        if (direction.magnitude < mouvementSensibility)
        {
            magnitude = 0;
        }
        velocity = Mathf.Lerp(velocity, magnitude, Time.deltaTime * m_interpolation);
        m_animator.SetFloat("MoveSpeed", velocity);


        // Vector3 position = transform.position;
        // position.x = Mathf.Lerp(position.x, positionX, Time.deltaTime / 0.5f);
        // position.z = Mathf.Lerp(position.z, positionZ, Time.deltaTime / 0.5f);
        // transform.position = position;

        // float v = Input.GetAxis("Vertical");
        // float h = Input.GetAxis("Horizontal");

        // float positionX = maxX * x;
        // float positionZ = maxY * y - 5;

        // Debug.Log("v" + v);
        // Debug.Log("h" + h);

        // Transform camera = Camera.main.transform;

        // if (Input.GetKey(KeyCode.LeftShift))
        // {
        //     v *= m_walkScale;
        //     h *= m_walkScale;
        // }

        // m_currentV = Mathf.Lerp(m_currentV, v, Time.deltaTime * m_interpolation);
        // m_currentH = Mathf.Lerp(m_currentH, h, Time.deltaTime * m_interpolation);

        // Vector3 direction = camera.forward * m_currentV + camera.right * m_currentH;

        // float directionLength = direction.magnitude;
        // direction.y = 0;
        // direction = direction.normalized * directionLength;

        // if (direction != Vector3.zero)
        // {
        //     m_currentDirection = Vector3.Slerp(m_currentDirection, direction, Time.deltaTime * m_interpolation);

        //     transform.rotation = Quaternion.LookRotation(m_currentDirection);
        //     transform.position += m_currentDirection * m_moveSpeed * Time.deltaTime;

        //     m_animator.SetFloat("MoveSpeed", direction.magnitude);
        // }

        // transform.position = new Vector3(positionX, 0, positionZ);

        JumpingAndLanding();
    }

    private void JumpingAndLanding()
    {
        bool jumpCooldownOver = (Time.time - m_jumpTimeStamp) >= m_minJumpInterval;

        if (jumpCooldownOver && m_isGrounded && m_jumpInput)
        {
            m_jumpTimeStamp = Time.time;
            m_rigidBody.AddForce(Vector3.up * m_jumpForce, ForceMode.Impulse);
        }

        if (!m_wasGrounded && m_isGrounded)
        {
            m_animator.SetTrigger("Land");
        }

        if (!m_isGrounded && m_wasGrounded)
        {
            m_animator.SetTrigger("Jump");
        }
    }
}
