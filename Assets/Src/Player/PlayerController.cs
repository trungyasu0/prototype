using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private Vector2 startTouchPosition, endTouchPosition;
    private Touch touch;
    private IEnumerator goCoroutine;
    private bool coroutineAllowed;

    private CharacterController _controller;
    private float _velocityX;
    private float _velocityZ;
    private float _velocity;
    private const float MaxVelocity = 1f;

    private enum State
    {
        Rolling,
        Attacking,
        Any
    }

    private State _playerState;
    private Animator _animator;
    private Rigidbody _rigidbody;

    public float turnSmoothTime = 0.1f;
    public float turnSmoothVelocity;
    public Transform cam;
    public Transform enemyTarget;

    public float speed = 2.0f;
    public float rollSpeed = 3f;
    public float acceleration = 20f;
    public float deceleration = 100f;
    public float rollDistance = 2.5f;

    private float distanceToDes;

    private Vector2 rollDir;

    private void Start()
    {
        _controller = GetComponent<CharacterController>();
        _animator = GetComponent<Animator>();
        _rigidbody = GetComponent<Rigidbody>();
        _playerState = State.Any;
        coroutineAllowed = true;
    }

    private void LateUpdate()
    {
        HandleRotationCam();
    }

    private void HandleRotationCam()
    {
        Vector3 dir = enemyTarget.position - transform.position;
        transform.rotation = Quaternion.LookRotation(dir);
    }

    private void Update()
    {
        CheckMoveOnSwipe();

        ControlPlayerAction();
    }


    private void ControlPlayerAction()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        if (_playerState == State.Any) Move(horizontal, vertical);
        if(_playerState == State.Rolling) Roll(rollDir);
        
        
        if (Input.GetKeyDown(KeyCode.Space))
        {
            OnRoll(horizontal, vertical);
        }
    }

    private void Move(float horizontal, float vertical)
    {
        AniMove();
        Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;
        if (direction.magnitude >= 0.1f)
        {
            Vector3 moveDir = CalculatorDirMove(horizontal, vertical);
            if (_velocity < speed) _velocity += acceleration * Time.deltaTime;
            _controller.Move(moveDir * _velocity * Time.deltaTime);
        }
        else
        {
            if (_velocity > 0) _velocity -= deceleration * Time.deltaTime;
        }
    }

    private void AniMove()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        if (horizontal != 0)
        {
            if (Mathf.Abs(_velocityX) < MaxVelocity)
                _velocityX += acceleration * 2 * Time.deltaTime * horizontal;
        }
        else
        {
            if (_velocityX > 0) _velocityX -= deceleration * Time.deltaTime;
            if (_velocityX < 0) _velocityX += deceleration * Time.deltaTime;
        }

        if (vertical != 0)
        {
            if (Mathf.Abs(_velocityZ) < MaxVelocity)
                _velocityZ += acceleration * 2 * Time.deltaTime * vertical;
        }
        else
        {
            if (_velocityZ > 0) _velocityZ -= deceleration * Time.deltaTime;
            if (_velocityZ < 0) _velocityZ += deceleration * Time.deltaTime;
        }

        _animator.SetFloat("Vertical", _velocityZ);
        _animator.SetFloat("Horizontal", _velocityX);
    }


    private void OnRoll(float horizontal, float vertical)
    {
        if (_playerState != State.Rolling)
        {
            _playerState = State.Rolling;
            _velocity = 0;
            distanceToDes = rollDistance;
            AniRolling(horizontal, vertical);
            rollDir = new Vector2(horizontal, vertical);
        }
    }

    private void Roll(Vector2 rollDir)
    {
        float horizontal = rollDir.x;
        float vertical = rollDir.y;
        if (horizontal == 0 && vertical == 0) vertical = -1;
        Vector3 moveDir = CalculatorDirMove(horizontal, vertical);
        if (_velocity < rollSpeed) _velocity += acceleration * 10 * Time.deltaTime;
        float moveAmount = (moveDir * _velocity * Time.deltaTime).magnitude;

        if (distanceToDes > 0)
        {
            distanceToDes -= moveAmount;
            _controller.Move(moveDir * _velocity * Time.deltaTime);
        }
        else
        {
            if (_velocity > 0) _velocity -= deceleration * Time.deltaTime;
            _playerState = State.Any;
        }
    }


    private void AniRolling(float horizontal, float vertical)
    {
        if (vertical > 0) _animator.SetTrigger("RollForward");
        else if (vertical < 0) _animator.SetTrigger("RollBackward");
        else if (horizontal > 0) _animator.SetTrigger("RollRight");
        else if (horizontal < 0) _animator.SetTrigger("RollLeft");
        else if (horizontal == 0 && vertical == 0) _animator.SetTrigger("RollBackward");

        // StartCoroutine(OnCompleteRollAction());
    }

    private Vector3 CalculatorDirMove(float horizontal, float vertical)
    {
        Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;
        if (direction.magnitude >= 0.1f)
        {
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + cam.eulerAngles.y;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity,
                turnSmoothTime);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);
            Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            return moveDir.normalized;
        }

        return new Vector3(0, 0, 0);
    }




    private void CheckMoveOnSwipe()
    {
        if (Input.touchCount == 0) return;
        if (Input.touchCount > 0)
        {
            touch = Input.GetTouch(0);
        }

        switch (touch.phase)
        {
            case TouchPhase.Began:
                startTouchPosition = touch.position;
                break;

            case TouchPhase.Ended:
                endTouchPosition = touch.position;
                if (endTouchPosition.y > startTouchPosition.y &&
                    Mathf.Abs(touch.deltaPosition.y) > Mathf.Abs(touch.deltaPosition.x))
                {
                    goCoroutine = MoveOnSwipe(new Vector3(0f, 0f, 0.25f));
                }
                else if (endTouchPosition.y < startTouchPosition.y &&
                         Mathf.Abs(touch.deltaPosition.y) > Mathf.Abs(touch.deltaPosition.x))
                {
                    goCoroutine = MoveOnSwipe(new Vector3(0f, 0f, -0.25f));
                }
                else if (endTouchPosition.x < startTouchPosition.x &&
                         Mathf.Abs(touch.deltaPosition.x) > Mathf.Abs(touch.deltaPosition.y))
                {
                    goCoroutine = MoveOnSwipe(new Vector3(-0.25f, 0f, 0));
                }
                else if (endTouchPosition.x > startTouchPosition.x &&
                         Mathf.Abs(touch.deltaPosition.x) > Mathf.Abs(touch.deltaPosition.y))
                {
                    goCoroutine = MoveOnSwipe(new Vector3(0.25f, 0f, 0));
                }

                break;
        }

        StartCoroutine(goCoroutine);
    }

    private IEnumerator MoveOnSwipe(Vector3 direction)
    {
        coroutineAllowed = false;
        for (int i = 0; i <= 2; i++)
        {
            transform.Translate(direction);
            yield return new WaitForSeconds(0.01f);
        }

        coroutineAllowed = true;
    }
}