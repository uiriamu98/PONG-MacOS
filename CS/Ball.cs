using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Ball : MonoBehaviour
{
    public Rigidbody2D rb;
    public GameObject postProcessVolume;

    public float startingSpeed = 6f;
    public P1 p1;

    public int scoreP1 = 0;
    public int scoreP2 = 0;

    public Text scoreP1Text;
    public Text scoreP2Text;

    private bool isResetting = false;
    private bool isPaused = true;
    private bool gameStarted = false;
    private bool canPause = false;
    private int lastSide = 0;
    private int winner = 0;

    private const int scoreLimit = 10;

    public AudioSource scoreAudioSource;
    public AudioSource sfxAudioSource;
    public AudioClip scoreClip;
    public AudioClip resetClip;
    public AudioClip collisionClip;
    public AudioClip gameOverClip;
    public AudioClip pauseClip;

    public Text blueWinText;
    public Text redWinText;

    private Vector2 savedVelocity;
    private float audioPausedTime = 0f;
    private bool unpausePending = false;
    private bool waitingForStart = true;

    private SpriteRenderer sr;
    public GameObject paddle1;
    public GameObject paddle2;
    private SpriteRenderer srPaddle1;
    private SpriteRenderer srPaddle2;

    private float normalAlpha = 1f;
    private float pausedAlpha = 0.1f;

    private Vector3 paddle1WaitingPosition;
    private Vector3 paddle2WaitingPosition;
    private float paddle1WaitingAlpha;
    private float paddle2WaitingAlpha;

    private bool controlLocked = false;

    void Start()
    {
        rb.freezeRotation = true;
        rb.linearVelocity = Vector2.zero;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        scoreP1Text.text = "0";
        scoreP2Text.text = "0";

        if (blueWinText != null) blueWinText.gameObject.SetActive(false);
        if (redWinText != null) redWinText.gameObject.SetActive(false);
        if (postProcessVolume != null) postProcessVolume.SetActive(true);

        sr = GetComponent<SpriteRenderer>();
        if (paddle1 != null) srPaddle1 = paddle1.GetComponent<SpriteRenderer>();
        if (paddle2 != null) srPaddle2 = paddle2.GetComponent<SpriteRenderer>();
        SetAlpha(pausedAlpha);

        isPaused = true;
        gameStarted = false;
        canPause = false;

        SavePaddlesWaitingState();

        StartCoroutine(StartGameAfterDelay(0.5f));
    }

    IEnumerator StartGameAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        canPause = true;
    }

    void Update()
    {
        if (!controlLocked && Input.GetKeyDown(KeyCode.Space) && !isResetting && canPause && !unpausePending)
        {
            if (waitingForStart)
            {
                waitingForStart = false;
                StartCoroutine(StartNewGame());
            }
            else if (isPaused)
            {
                StartCoroutine(DelayedUnpause());
            }
            else
            {
                TogglePause();
            }
        }

        if (!gameStarted || isPaused || isResetting || controlLocked) return;

        if (Mathf.Abs(rb.position.x) >= 13f)
        {
            lastSide = rb.position.x > 0 ? 1 : -1;

            if (lastSide == 1)
            {
                scoreP2 += 1;
                scoreP2Text.text = scoreP2.ToString();
            }
            else
            {
                scoreP1 += 1;
                scoreP1Text.text = scoreP1.ToString();
            }

            bool gameIsOver = (scoreP1 >= scoreLimit || scoreP2 >= scoreLimit);

            if (!gameIsOver)
            {
                if (scoreAudioSource != null && scoreClip != null)
                    scoreAudioSource.PlayOneShot(scoreClip);

                StartCoroutine(ResetBallWithDelay());
            }
            else
            {
                winner = scoreP1 >= scoreLimit ? 1 : 2;
                StartCoroutine(ResetBallWithGameOverSound());
            }
        }

        if (waitingForStart)
        {
            SavePaddlesWaitingState();
        }
    }

    void SavePaddlesWaitingState()
    {
        if (paddle1 != null)
        {
            paddle1WaitingPosition = paddle1.transform.position;
            paddle1WaitingAlpha = srPaddle1 != null ? srPaddle1.color.a : 1f;
        }
        if (paddle2 != null)
        {
            paddle2WaitingPosition = paddle2.transform.position;
            paddle2WaitingAlpha = srPaddle2 != null ? srPaddle2.color.a : 1f;
        }
    }

    IEnumerator DelayedUnpause()
    {
        unpausePending = true;
        canPause = false;

        yield return new WaitForSeconds(0.33f);

        TogglePause();

        canPause = true;
        unpausePending = false;
    }

    IEnumerator StartNewGame()
    {
        isPaused = false;
        Cursor.visible = false;  // Hide cursor when game starts
        gameStarted = true;
        controlLocked = false;

        if (pauseClip != null && sfxAudioSource != null)
        {
            sfxAudioSource.PlayOneShot(pauseClip);
        }

        if (postProcessVolume != null)
            postProcessVolume.SetActive(false);

        rb.isKinematic = false;
        SetAlpha(normalAlpha);
        RestorePaddlesWaitingState();

        yield return PlayResetClipAndLaunchBall();
    }

    IEnumerator PlayResetClipAndLaunchBall()
    {
        isResetting = true;

        if (scoreAudioSource != null && resetClip != null)
        {
            scoreAudioSource.PlayOneShot(resetClip);
            yield return new WaitForSeconds(resetClip.length);
        }

        LaunchBall(Random.value < 0.5f ? 1 : -1);
        isResetting = false;
        canPause = true;

        if (postProcessVolume != null)
            postProcessVolume.SetActive(false);
    }

    void TogglePause()
    {
        isPaused = !isPaused;

        Cursor.visible = isPaused;  // Show cursor only when paused

        if (pauseClip != null && sfxAudioSource != null)
        {
            sfxAudioSource.PlayOneShot(pauseClip);
        }

        if (postProcessVolume != null)
            postProcessVolume.SetActive(isPaused);

        SetAlpha(isPaused ? pausedAlpha : normalAlpha);

        if (isPaused)
        {
            savedVelocity = rb.linearVelocity;
            rb.linearVelocity = Vector2.zero;
            rb.isKinematic = true;

            if (scoreAudioSource != null && scoreAudioSource.isPlaying && scoreAudioSource.clip != null)
            {
                audioPausedTime = scoreAudioSource.time;
                scoreAudioSource.Pause();
            }
        }
        else
        {
            rb.isKinematic = false;
            rb.linearVelocity = savedVelocity;

            if (scoreAudioSource != null && scoreAudioSource.clip != null)
            {
                scoreAudioSource.UnPause();
                scoreAudioSource.time = audioPausedTime;
            }
        }
    }

    void SetAlpha(float alpha)
    {
        if (sr != null)
        {
            Color c = sr.color;
            c.a = alpha;
            sr.color = c;
        }

        if (srPaddle1 != null)
        {
            Color c = srPaddle1.color;
            c.a = alpha;
            srPaddle1.color = c;
        }

        if (srPaddle2 != null)
        {
            Color c = srPaddle2.color;
            c.a = alpha;
            srPaddle2.color = c;
        }
    }

    void RestorePaddlesWaitingState()
    {
        if (paddle1 != null)
        {
            paddle1.transform.position = paddle1WaitingPosition;
            if (srPaddle1 != null)
            {
                Color c = srPaddle1.color;
                c.a = paddle1WaitingAlpha;
                srPaddle1.color = c;
            }
        }
        if (paddle2 != null)
        {
            paddle2.transform.position = paddle2WaitingPosition;
            if (srPaddle2 != null)
            {
                Color c = srPaddle2.color;
                c.a = paddle2WaitingAlpha;
                srPaddle2.color = c;
            }
        }
    }

    public bool IsPaused()
    {
        return isPaused || !gameStarted || controlLocked;
    }

    void FixedUpdate()
    {
        if (!gameStarted || isPaused || isResetting || controlLocked) return;

        rb.angularVelocity = 0f;

        float maxSpeed = startingSpeed * 5f;
        float currentSpeed = rb.linearVelocity.magnitude;

        if (currentSpeed < maxSpeed)
        {
            float speedIncrease = 0.1f;
            Vector2 newVelocity = rb.linearVelocity.normalized * (currentSpeed + speedIncrease);
            rb.linearVelocity = newVelocity.magnitude > maxSpeed ? newVelocity.normalized * maxSpeed : newVelocity;
        }
        else
        {
            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
        }

        ClampBallAngle();
    }

    void LaunchBall(int directionX)
    {
        float angleDegrees = Random.Range(5f, 37.5f);
        float angleRadians = angleDegrees * Mathf.Deg2Rad;
        int verticalDir = Random.value < 0.5f ? 1 : -1;

        float launchSpeed = (scoreP1 >= 6 || scoreP2 >= 6) ? 7f : startingSpeed;

        Vector2 direction = new Vector2(directionX * Mathf.Cos(angleRadians), verticalDir * Mathf.Sin(angleRadians)).normalized;
        rb.linearVelocity = direction * launchSpeed;

        ClampBallAngle();
    }

    IEnumerator ResetBallWithDelay()
    {
        isResetting = true;
        rb.linearVelocity = Vector2.zero;

        if (scoreAudioSource != null && resetClip != null)
            scoreAudioSource.PlayOneShot(resetClip);

        canPause = false;

        yield return new WaitForSeconds(resetClip.length);

        rb.position = Vector2.zero;
        rb.angularVelocity = 0f;
        p1?.ResetScale();

        LaunchBall(-lastSide);

        canPause = true;
        isResetting = false;
    }

    IEnumerator ResetBallWithGameOverSound()
    {
        isResetting = true;
        controlLocked = true;
        rb.linearVelocity = Vector2.zero;

        if (blueWinText != null) blueWinText.gameObject.SetActive(winner == 1);
        if (redWinText != null) redWinText.gameObject.SetActive(winner == 2);

        SetPaddlesAlpha(0f);

        if (scoreAudioSource != null && gameOverClip != null)
        {
            scoreAudioSource.PlayOneShot(gameOverClip);
            yield return new WaitForSeconds(gameOverClip.length + 0.5f);
        }

        if (blueWinText != null) blueWinText.gameObject.SetActive(false);
        if (redWinText != null) redWinText.gameObject.SetActive(false);

        scoreP1 = 0;
        scoreP2 = 0;
        scoreP1Text.text = "0";
        scoreP2Text.text = "0";

        rb.position = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.linearVelocity = Vector2.zero;
        rb.isKinematic = true;
        p1?.ResetScale();

        isPaused = true;
        Cursor.visible = true;  // Show cursor on game over (paused)

        gameStarted = false;
        canPause = true;
        isResetting = false;
        unpausePending = false;
        waitingForStart = true;
        controlLocked = false;

        RestorePaddlesWaitingState();
        SetAlpha(pausedAlpha);

        if (postProcessVolume != null)
            postProcessVolume.SetActive(true);
    }

    void SetPaddlesAlpha(float alpha)
    {
        if (srPaddle1 != null)
        {
            Color c = srPaddle1.color;
            c.a = alpha;
            srPaddle1.color = c;
        }
        if (srPaddle2 != null)
        {
            Color c = srPaddle2.color;
            c.a = alpha;
            srPaddle2.color = c;
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (scoreAudioSource != null && collisionClip != null)
            scoreAudioSource.PlayOneShot(collisionClip);

        if (collision.gameObject == paddle1 || collision.gameObject == paddle2)
        {
            Vector2 velocity = rb.linearVelocity;
            float minY = 0.5f;
            if (Mathf.Abs(velocity.y) < minY)
            {
                float signY = Mathf.Sign(velocity.y) != 0 ? Mathf.Sign(velocity.y) : (rb.position.y > 0 ? -1f : 1f);
                velocity.y = signY * minY;
                velocity = velocity.normalized * rb.linearVelocity.magnitude;
                rb.linearVelocity = velocity;
            }
        }
        ClampBallAngle();
    }

    void ClampBallAngle()
{
    Vector2 velocity = rb.linearVelocity;
    float minY = 1f;  // Stronger vertical correction

    // If ball is too horizontal
    if (Mathf.Abs(velocity.y) < minY)
    {
        float signY = Mathf.Sign(velocity.y);
        if (signY == 0) signY = Random.value < 0.5f ? -1f : 1f; // Random if flat

        velocity.y = signY * minY;

        // Re-normalize to preserve original speed
        velocity = velocity.normalized * rb.linearVelocity.magnitude;
        rb.linearVelocity = velocity;
    }
}

    // <-- Here is the new method you asked for:
    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Quit button pressed. Closing game...");

    }
}
