﻿using GameAnalyticsSDK;
using GooglePlayGames;
using UnityEngine;
using UnityEngine.SocialPlatforms;

public class ControllerPlayer : ControllerCharacter {
    private DisplayPlayerHealth _healthDisplay;
    private SpringJoint         _spring;
    private Vector2             _touchOrigin = -Vector2.one;

    public float      springForce;
    public GameObject touchTarget;

    
    /**
     * Function Start
     *  Runs when the GameObject that this script is attached to is
     *  initialised.
     *  Overrides the Start function of the ControllerCharacter Class
     */
    protected new void Start() {
        base.Start();

        _healthDisplay = GameObject.FindWithTag("Display_Player_Health_Shield").GetComponent<DisplayPlayerHealth>();
        Boundary = GameObject.FindGameObjectWithTag("PlayArea").GetComponent<ManagerBoundary>().playerBoundary;
        MainCam = Camera.main;
        _spring = gameObject.GetComponent<SpringJoint>();
        _spring.spring = 0.0f;

        HitPoints = GameManager.GetPlayerMaxHealth();
    }

    // Called BEFORE THE START of every frame to get the Player's intentions for this frame.
    private new void Update() {
        base.Update();

        // Has the Player lost the game?
        if (HitPoints <= 0) {
            if (Social.localUser.authenticated) {
                GameAnalytics.NewProgressionEvent(GAProgressionStatus.Fail,"Level_01");
                PlayGamesPlatform.Instance.Events.IncrementEvent(GPGSIds.event_total_deaths, 1);

                Social.ReportProgress(GPGSIds.achievement_you_died, 100.0f,
                    success => {
                        Debug.Log(success
                            ? "Player Died Success"
                            : "Player Died Fail");
                    });
            }

            StartCoroutine(GameManager.WaitTime());

            GameManager.GameOver();
        }

        GetPlayerInput();
    }

    // Called just BEFORE THE END of every frame to deal with the physics engine changes, ready for the next frame.
    private void FixedUpdate() {
        var tempRb = GetComponent<Rigidbody>();

        // Clamp the Player Position to within the bounds of the screen
        tempRb.position = new Vector3(
            Mathf.Clamp(GetComponent<Rigidbody>().position.x, Boundary.xMin, Boundary.xMax),
            0.0f,
            Mathf.Clamp(GetComponent<Rigidbody>().position.z, Boundary.zMin, Boundary.zMax)
        );

        // Clamp the Player Rotation to within reasonable bounds
        tempRb.rotation = Quaternion.Euler(
            Mathf.Clamp(GetComponent<Rigidbody>().rotation.x, -95, -85),
            0.0f,
            Mathf.Clamp(GetComponent<Rigidbody>().rotation.z, 85, 95)
        );

        gameObject.transform.position = tempRb.position;
    }

    // Called by Update to get input from the Player.
    private void GetPlayerInput() {
#if UNITY_STANDALONE || UNITY_WEBPLAYER || UNITY_EDITOR

        // Is the Player using a mouse/keyboard input device

        // Get the position of the mouse
        if (Input.GetMouseButton(0)) {
            _touchOrigin = Input.mousePosition;

            touchTarget.transform.position = ScreenToWorldCoord(Input.mousePosition);
            _spring.spring = springForce;
        } else {
            _spring.spring = 0.0f;
        }

        if (Input.GetButton("PrimaryFire")) {
            if (!ReadyToShoot()) return;

            Fire();
        }
#elif UNITY_IOS || UNITY_ANDROID || UNITY_WP8 || UNITY_IPHONE
        // If the Player is using a touchscreen input device

        //Check if Input has registered more than zero touches
        if (Input.touchCount > 0) {
            //Store the first touch detected.
            var myTouch = Input.touches[0];

            if (!IsPointerOverUIObject()) {
                //Check if the phase of that touch equals Began
                if (myTouch.phase == TouchPhase.Began) {
                    touchTarget.transform.position = ScreenToWorldCoord(myTouch.position);
                    _spring.spring = springForce;
                } else if (myTouch.phase == TouchPhase.Moved) {
                    touchTarget.transform.position = ScreenToWorldCoord(myTouch.position);
                }

                //If the touch phase is not Began, and instead is equal to Ended
                else if (myTouch.phase == TouchPhase.Ended) {
                    _spring.spring = 0.0f;
                }
            }
        }
#endif // End Device specific code
    }

    // Handles Player attacks.
    public override void Fire() {
        // Increment the User's Shot Count Achievements 
        if (Social.localUser.authenticated) {
            PlayGamesPlatform.Instance.Events.IncrementEvent(GPGSIds.event_shots_fired, 1);

            PlayGamesPlatform.Instance.IncrementAchievement(
                GPGSIds.achievement_10_shots_fired, 1,
                success => {
                    Debug.Log(success
                        ? "Update 10 Shots Fired Success"
                        : "Update 10 Shots Fired Fail");
                });

            PlayGamesPlatform.Instance.IncrementAchievement(
                GPGSIds.achievement_25_shots_fired, 1,
                success => {
                    Debug.Log(success
                        ? "Update 20 Shots Fired Success"
                        : "Update 20 Shots Fired Fail");
                });

            PlayGamesPlatform.Instance.IncrementAchievement(
                GPGSIds.achievement_50_shots_fired, 1,
                success => {
                    Debug.Log(success
                        ? "Update 50 Shots Fired Success"
                        : "Update 50 Shots Fired Fail");
                });
        }

        foreach (Transform child in gameObject.transform.Find("ShotSpawns"))
            if (child.gameObject.activeSelf) {
                var bullet = ManagerPoolShot.instance.GetPooledObject("Shot_Player_Main");
                if (bullet != null) {
                    bullet.transform.position = child.position;
                    bullet.transform.rotation = child.rotation;
                    bullet.GetComponent<Rigidbody>().velocity =
                        child.transform.forward * bullet.GetComponent<ControllerProjectile>().speed;
                    bullet.SetActive(true);
                }
            }
    }

    protected override void OnCollisionEnter(Collision other) {
        // If the Player has been shot, but not by themselves.
        if (other.gameObject.tag.Contains("Shot") && !other.gameObject.tag.Contains("Player")) {
            var damage = other.gameObject.GetComponent<ControllerProjectile>().power;

            _healthDisplay.RemoveHealth(damage);

            other.gameObject.GetComponent<Rigidbody>().velocity = Vector3.zero;
            other.gameObject.GetComponent<Rigidbody>().rotation = Quaternion.Euler(Vector3.zero);
            other.gameObject.SetActive(false);
        }
    }
}
