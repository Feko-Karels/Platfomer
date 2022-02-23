using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerMovement : MonoBehaviour
{
    private readonly int LAST_LEVEL_INDEX = 3;

    public AudioClip coinSound;

    public float speedRunning;
    public float speedFlying;
    public float jump;

    private Rigidbody2D rb;
    private BoxCollider2D boxCollider;
    private Animator animator;
    private AudioManager audioManager;

    private bool onGround;
    private bool onPlatform;
    private bool ableToWalk = true;

    private Vector3 checkpoint;
    private Vector3 start;

    // Start is called before the first frame update
    void Start()
    {

        audioManager = FindObjectOfType<AudioManager>();

        start = transform.position;
        checkpoint = start;

        rb = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();
        animator = GetComponent<Animator>();

        Game.Start();


    }

    // Update is called once per frame
    void Update()
    {
        Move(); //Bewegung des Spielers

        animator.SetInteger("y", (int)(rb.velocity.y * 1000));

        if ((Input.GetKeyDown(KeyCode.Space)||Input.GetKeyDown(KeyCode.UpArrow)) && (onGround || onPlatform))
        {
            if (rb.velocity.y <= 0)
            {
                rb.AddForce(Vector2.up * jump);
            }
        }

    }

    public void setCheckpoint(Vector3 pos)
    {
        checkpoint = pos;
    }

    private void Move()
    {

        float x = 0;
        if (ableToWalk)
        {
            x = Input.GetAxisRaw("Horizontal");
        }
        float moveBy = x * speedRunning;
        rb.velocity = new Vector2(moveBy, rb.velocity.y);

        if (x != 0) //Animation und Richtung des Sprites wird beim laufen geändert
        {
            animator.SetBool("walking", true);
            Mirror((int)x);
        }
        else
        {
            animator.SetBool("walking", false);
        }

    }
    private void Mirror(int hor) //Spiegelt den Sprite (X-Scale *-1)
    {
        Vector3 scale = transform.localScale;
        scale.x = hor * Mathf.Abs(scale.x);
        transform.localScale = scale;
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        GameObject hitObject = collision.collider.gameObject;
        proveCollision(hitObject);

        if (onPlatform && Input.GetAxisRaw("Vertical") == -1) //Mit S durch platform durchfallen -> Collider aus
        {
            if (collision.gameObject.tag == "TwoWayPlatform") //Nur wenn Platform Tag das erlaubt
            {
                PlatformEffector2D platformEffector = collision.gameObject.GetComponent<PlatformEffector2D>();
                platformEffector.rotationalOffset = 180;
                StartCoroutine(TurnAfter(0.4f, platformEffector));
            }
        }
    }
    private void OnCollisionExit2D(Collision2D collision)
    {
        string tag = collision.collider.gameObject.tag;
        if (tag != "Wall")
        {
            if (onGround) { onGround = false; }
            if (onPlatform) { onPlatform = false; }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        GameObject hitObject = collision.gameObject;
        proveCollision(hitObject);
    }

    private void proveCollision(GameObject hitObject)
    {

        switch (hitObject.tag)
        {
            case "Ground":
                onGround = true;
                break;
            case "Wall":
                break;
            case "TwoWayPlatform":
            case "OneWayPlatform":
                onPlatform = true;
                break;
            case "Void":
            case "Enemy":
                Died();
                break;
            case "FinishTag":
                StartCoroutine(Finish(2));
                break;
            case "Coin":
                Destroy(hitObject);
                Game.increaseCoinCounter(1);
                audioManager.Play("Coin");
                break;
            case "Checkpoint":
                break;
            default:
                Debug.LogWarning("Tag not found (Tag: " + hitObject.tag + ", Game Object: " + hitObject.ToString() + ")");
                break;
        }    
    }

    private void Died()
    {
        ableToWalk = false;
        audioManager.Play("Hit");
        StartCoroutine("Die", 1);
        audioManager.Pause("Theme");

    }

    private IEnumerator Die(float sec)
    {
        yield return new WaitForSeconds(sec); //wartet (sec) Sekunden
        Game.decreaselifeCounter();
        if(Game.lifes < 0)
        {
            checkpoint = start;
            Game.ResetLifes();
            GameObject[] points = GameObject.FindGameObjectsWithTag("Checkpoint");
            foreach (GameObject point in points)
            {
                point.GetComponent<Checkpoint>().Reset();
            }
        }
        transform.position = checkpoint;
       
        ableToWalk = true;
        audioManager.Resume("Theme");
    }

    private IEnumerator TurnAfter(float sec, PlatformEffector2D platformEffector)
    {
        yield return new WaitForSeconds(sec);
        platformEffector.rotationalOffset = 0;

    }

    private IEnumerator Finish(float sec)
    {
        audioManager.Play("Win");
        ableToWalk = false;
        audioManager.Pause("Theme");
        yield return new WaitForSeconds(sec);
        int nextScene = SceneManager.GetActiveScene().buildIndex + 1;

        if (nextScene == LAST_LEVEL_INDEX +1)
        {
            nextScene = 1;
        }
        PlayerPrefs.SetInt("Level", nextScene);
        PlayerPrefs.Save();
        SceneManager.LoadScene(nextScene);
        ableToWalk = true;
        audioManager.Resume("Theme");

    }
}
