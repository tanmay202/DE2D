using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    
    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer sr;
    private Vector2 movement;
    private Vector2 lastMoveDirection = new Vector2(0, -1); // Start facing front

    void Start()
    {
        rb = GetComponentInChildren<Rigidbody2D>();
        animator = GetComponentInChildren<Animator>();
        sr = GetComponentInChildren<SpriteRenderer>();
        
        // Set initial idle direction to front
        animator.SetFloat("MoveX", 0);
        animator.SetFloat("MoveY", -1);
    }

    void Update()
    {
        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");

        // Horizontal priority
        if (movement.x != 0) 
            movement.y = 0;

        // Update animator
        if (movement != Vector2.zero)
        {
            // Moving - update and save the direction
            lastMoveDirection = movement;
            animator.SetFloat("MoveX", movement.x);
            animator.SetFloat("MoveY", movement.y);
        }
        else
        {
            // Idle - maintain last direction instead of forcing front
            animator.SetFloat("MoveX", lastMoveDirection.x);
            animator.SetFloat("MoveY", lastMoveDirection.y);
        }

        HandleFlip();
    }

    void FixedUpdate()
    {
        rb.MovePosition(rb.position + movement.normalized * moveSpeed * Time.fixedDeltaTime);
    }

    void HandleFlip()
    {
        if (movement.x > 0)
            sr.flipX = true;
        else if (movement.x < 0)
            sr.flipX = false;
    }
}