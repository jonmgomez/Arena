using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Multiplayer.Samples.Utilities.ClientAuthority;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : NetworkBehaviour
{
    [SerializeField] private float walkSpeed = 6f;
    [SerializeField] private float runSpeed = 12f;
    [SerializeField] private float jumpPower = 7f;
    [SerializeField] private float gravity = 10f;

    private Vector3 moveDirection = Vector3.zero;

    [SerializeField] private bool canMove = true;
    private bool isMoving = false;

    public event Action<bool> OnMovementChange;

    private Player player;
    ClientNetworkTransform clientNetworkTransform;
    CharacterController characterController;
    Animator animator;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            enabled = false;
        }
    }

    void Start()
    {
        player = GetComponent<Player>();
        clientNetworkTransform = GetComponent<ClientNetworkTransform>();
        characterController = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        if (!IsOwner)
            return;

        Vector3 forward = transform.TransformDirection(Vector3.forward);
        Vector3 right = transform.TransformDirection(Vector3.right);

        // Press Left Shift to run
        bool isRunning = Input.GetKey(KeyCode.LeftShift);
        float curSpeedX = canMove ? (isRunning ? runSpeed : walkSpeed) * Input.GetAxis("Vertical") : 0;
        float curSpeedY = canMove ? (isRunning ? runSpeed : walkSpeed) * Input.GetAxis("Horizontal") : 0;
        float movementDirectionY = moveDirection.y;
        moveDirection = (forward * curSpeedX) + (right * curSpeedY);

        if (Input.GetButton("Jump") && canMove && characterController.isGrounded)
        {
            moveDirection.y = jumpPower;
        }
        else
        {
            moveDirection.y = movementDirectionY;
        }

        if (!characterController.isGrounded)
        {
            moveDirection.y -= gravity * Time.deltaTime;
        }

        // Update the current hip fire spread using the velocity.
        // The y is either 0 or max, otherwise it will smooth at the top and bottom of the jump.
        player.GetPlayerWeapon().MovementChanged(new Vector3(moveDirection.x, characterController.isGrounded ? 0 : gravity, moveDirection.z));
        characterController.Move(moveDirection * Time.deltaTime);

        if (isMoving && curSpeedX == 0 && curSpeedY == 0)
        {
            isMoving = false;
            OnMovementChange?.Invoke(isMoving);
            clientNetworkTransform.Interpolate = false;
        }
        else if (!isMoving && (curSpeedX != 0 || curSpeedY != 0))
        {
            isMoving = true;
            OnMovementChange?.Invoke(isMoving);
            clientNetworkTransform.Interpolate = true;
        }
    }

    public void SetEnableControls(bool enable)
    {
        canMove = enable;
    }

    public bool IsMoving() => isMoving;
}
