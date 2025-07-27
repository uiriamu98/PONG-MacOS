using UnityEngine;

public class P2 : MonoBehaviour
{
    public float moveSpeed;

    private Vector3 initialScale;

    // Reference to the Ball script to check pause state
    public Ball ball;

    private SpriteRenderer sr;

    private float normalAlpha = 1f;
    private float pausedAlpha = 0.1f;

    void Start()
    {
        initialScale = transform.localScale;
        sr = GetComponent<SpriteRenderer>();
        SetAlpha(pausedAlpha); // Start with transparent since game starts paused
    }

    void Update()
    {
        if (ball == null)
            return;

        // Set transparency based on pause state
        if (ball.IsPaused())
        {
            SetAlpha(pausedAlpha);
            return;
        }
        else
        {
            SetAlpha(normalAlpha);
        }

        bool isPressingUp = Input.GetKey(KeyCode.I);
        bool isPressingDown = Input.GetKey(KeyCode.K);

        if (isPressingUp)
        {
            transform.Translate(Vector2.up * Time.deltaTime * moveSpeed);
        }

        if (isPressingDown)
        {
            transform.Translate(Vector2.down * Time.deltaTime * moveSpeed);
        }

        Vector3 pos = transform.position;
        pos.y = Mathf.Clamp(pos.y, -4.5f, 4.5f);
        transform.position = pos;
    }

    public void ResetScale()
    {
        transform.localScale = initialScale;
    }

    private void SetAlpha(float alpha)
    {
        if (sr != null)
        {
            Color c = sr.color;
            c.a = alpha;
            sr.color = c;
        }
    }
}
