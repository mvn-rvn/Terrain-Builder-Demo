using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Rendering.PostProcessing;

public class PlayerMovement : MonoBehaviour
{
    public float speed = 15.5f;
    public float jump_height = 0.025f;
    public float sensitivity = 4.5f;
    float x_mouse = 0f;
    float y_mouse = 0f;
    CharacterController controller;
    public float gravity = 0.05f;
    float velocity = 0f;
    bool double_jump = false;
    bool is_dead = false;
    float strafe = 0f;
    Rigidbody body;
    float horizontal;
    float vertical;
    bool is_dashing = false;
    float dash_duration = 0;
    public float max_dash_duration = 0.25f;
    public float dash_multiplier = 2.5f;
    float fov;
    Vector3 forward_dir;
    Vector3 before_pos;
    Vector3 after_pos;
    float dash_vertical = 0f;
    float dash_horizontal;
    float max_strafe = 1f;
    float strafe_speed = 20f;
    float stamina = 2f;
    float potential_stamina = 3f;
    public float dash_fov = 5f;
    public float dash_recharge_rate = 1.25f;
    GameObject dash_particles;
    PostProcessVolume fx;
    Vignette vignette;
    float vignette_intensity;
    bool grounded = false;
    // Start is called before the first frame update
    void Start()
    {
        controller = GetComponent<CharacterController>();
        body = GetComponent<Rigidbody>();
        GameObject.Find("Death Particles").GetComponent<ParticleSystem>().Clear();
        GameObject.Find("Death Particles").GetComponent<ParticleSystem>().Stop();
        GameObject.Find("Text").GetComponent<Text>().text = SceneManager.GetActiveScene().name;
        Invoke("HideText", 2f);
        fov = Camera.main.fieldOfView;
        forward_dir = transform.forward;
        Cursor.lockState = CursorLockMode.Locked;
        dash_particles = GameObject.Find("Dash Particles");
        dash_particles.GetComponent<ParticleSystem>().Clear();
        dash_particles.GetComponent<ParticleSystem>().Stop();
        fx = Camera.main.GetComponent<PostProcessVolume>();
        vignette = fx.profile.GetSetting<Vignette>();
        vignette_intensity = vignette.intensity.value;
        vignette.intensity.value = 0;
    }

    // Update is called once per frame
    void Update()
    {
        //dash checking
        if(Input.GetButtonDown("Sprint") && !is_dead && stamina >= 1f && !is_dashing) {
            if(Input.GetAxisRaw("Vertical") != 0 || Input.GetAxisRaw("Horizontal") != 0) {
                before_pos = transform.position;
                is_dashing = true;
                dash_vertical = Input.GetAxisRaw("Vertical");
                dash_horizontal = Input.GetAxisRaw("Horizontal");
                stamina -= 1f;
                potential_stamina -= 1f;
                dash_particles.GetComponent<ParticleSystem>().Play();
                Invoke("DashExit", max_dash_duration);
            }
        }
        if(potential_stamina < 2f) {
            potential_stamina += Time.deltaTime * dash_recharge_rate;
        } else if(potential_stamina > 2f) {
            potential_stamina = 2f;
        }
        GameObject.Find("Stamina").GetComponent<Text>().text = "<color=navy><b>" + stamina.ToString("#.00") + "</b></color>\n<color=#00000050>" + potential_stamina.ToString("#.00") + "</color>";

        // for later reference: https://answers.unity.com/questions/1482829/get-y-rotation-between-two-vector3-pointsget-y-rot.html

        //camera looking and FOV
        if(!is_dead) {
            x_mouse -= Input.GetAxis("Mouse Y") * sensitivity;
            y_mouse += Input.GetAxis("Mouse X") * sensitivity;
            transform.eulerAngles = new Vector3(0, y_mouse, 0);
            Camera.main.transform.position = new Vector3(transform.position.x, transform.position.y + 1, transform.position.z);
            Camera.main.transform.eulerAngles = new Vector3(Mathf.Clamp(x_mouse, -90, 90), y_mouse, strafe);
            if(is_dashing) {
                if(dash_vertical != 0 && dash_horizontal == 0) {
                    Camera.main.fieldOfView = Mathf.Lerp(Camera.main.fieldOfView, fov - (dash_fov * dash_vertical), Time.deltaTime * strafe_speed);
                }
                vignette.intensity.value = Mathf.Lerp(vignette.intensity.value, vignette_intensity, Time.deltaTime * strafe_speed);
            } else {
                Camera.main.fieldOfView = Mathf.Lerp(Camera.main.fieldOfView, fov, Time.deltaTime * strafe_speed);
                vignette.intensity.value = Mathf.Lerp(vignette.intensity.value, 0, Time.deltaTime * strafe_speed);
            }
        }

        //gravity
        velocity -= gravity * Time.deltaTime;
        if(!is_dead && !is_dashing) {
            controller.Move(new Vector3(0, velocity, 0) * Time.deltaTime);
        }
        if(controller.isGrounded) {
            //VERY IMPORTANT: CALL CONTROLLER.MOVE FIRST AND THEN CHECK ISGROUNDED
            velocity = 0f;
            double_jump = true;
            stamina = potential_stamina;
        }
        if(Input.GetButtonDown("Jump")) {
            if(controller.isGrounded) {
                velocity = jump_height;
            } else if(double_jump == true) {
                velocity = jump_height;
                double_jump = false;
            }
        }

        //ground checking, cause for some reason controller.isGrounded doesn't work with setting dash transform.forward
        RaycastHit groundinfo;
        grounded = Physics.SphereCast(transform.position, 0.4f, Vector3.down, out groundinfo, 0.69f);
        if(grounded) {
            controller.stepOffset = 0.3f;
        } else {
            controller.stepOffset = 0f;
        }
        if(grounded && groundinfo.transform.tag == "ground") {
            //in case I need the code to specifically check if the raycast detects ground and not just detect anything
        }

        //movement
        if(controller.isGrounded) {
            horizontal = Input.GetAxisRaw("Horizontal") * speed;
            vertical = Input.GetAxisRaw("Vertical") * speed;
        } else {
            //Current air friction is set to 5. Adjust in Input Controller if needed
            horizontal = Input.GetAxis("Horizontal") * speed;
            vertical = Input.GetAxis("Vertical") * speed;
        }
        //Make player move during dash, set dash timer
        if(is_dashing && dash_duration < max_dash_duration) {
            horizontal = dash_horizontal * speed * dash_multiplier;
            vertical = dash_vertical * speed * dash_multiplier;
            max_strafe = 3.5f;
            strafe_speed = 40f;
            dash_duration += Time.deltaTime;
        } else {
            is_dashing = false;
            dash_duration = 0;
            max_strafe = 1f;
            strafe_speed = 20f;
        }
        if(horizontal != 0 && vertical != 0) {
            float is_horiz_negative;
            float is_verti_negative;
            if(horizontal < 0) {
                is_horiz_negative = -1;
            } else {
                is_horiz_negative = 1;
            }
            if(vertical < 0) {
                is_verti_negative = -1;
            } else {
                is_verti_negative = 1;
            }
            horizontal = Mathf.Sqrt(((horizontal * horizontal) / 2)) * is_horiz_negative;
            vertical = Mathf.Sqrt(((vertical * vertical) / 2)) * is_verti_negative;
        }
        if(Input.GetAxisRaw("Horizontal") > 0) {
            strafe = Mathf.Lerp(strafe, -max_strafe, Time.deltaTime * strafe_speed);
        } else if(Input.GetAxisRaw("Horizontal") < 0) {
            strafe = Mathf.Lerp(strafe, max_strafe, Time.deltaTime * strafe_speed);
        } else {
            strafe = Mathf.Lerp(strafe, 0f, Time.deltaTime * 20);
        }
        if(is_dashing && !grounded && dash_horizontal == 0 && dash_vertical > 0) {
            forward_dir = Camera.main.transform.forward;
        } else {
            forward_dir = transform.forward;
        }
        if(!is_dead) {
            controller.Move((transform.right * horizontal + forward_dir * vertical) * Time.deltaTime);
        }

        //falling off map
        if(transform.position.y < -15 && !is_dead) {
            Death();
        }

        //quit
        if(Input.GetKey(KeyCode.Escape)) {
            Application.Quit();
        }
    }

    void DashExit() {
        after_pos = transform.position;
        velocity = (after_pos.y - before_pos.y) / max_dash_duration / dash_multiplier;
        dash_particles.GetComponent<ParticleSystem>().Stop();
    }

    //obstacle detection
    void OnTriggerEnter(Collider collider) {
        if(collider.gameObject.tag == "Obstacle") {
            Death();
        }
        if(collider.gameObject.tag == "Finish") {
            GameObject.Find("Text").GetComponent<Text>().text = "GG \n\nAdvancing to next level...";
            Invoke("Advance", 2);
        }
    }

    void Death() {
        is_dead = true;
        Camera.main.transform.position -= Camera.main.transform.forward * 5;
        //Camera.main.transform.eulerAngles = new Vector3(0, Camera.main.transform.eulerAngles.y, 0);
        body.constraints = RigidbodyConstraints.FreezeAll;
        GetComponent<Renderer>().enabled = false;
        GameObject.Find("Death Particles").transform.position = transform.position;
        GameObject.Find("Death Particles").SetActive(true);
        GameObject.Find("Death Particles").GetComponent<ParticleSystem>().Play();
        Invoke("Restart", 1.5f);
    }

    void Restart() {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void HideText() {
        GameObject.Find("Text").GetComponent<Text>().text = " ";
    }

    void Advance() {
        string[] next_level = SceneManager.GetActiveScene().name.Split(' ');
        int level_number = int.Parse(next_level[1]);
        if(level_number == 3) {
            SceneManager.LoadScene("Level 1");
        } else {
            level_number += 1;
            SceneManager.LoadScene("Level " + level_number.ToString());
        }
    }
}
