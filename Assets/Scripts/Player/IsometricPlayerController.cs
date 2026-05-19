using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class IsometricPlayerController : MonoBehaviour
{
    [SerializeField] private float   moveSpeed = 5f;
    [SerializeField] private Tilemap walkableTilemap;

    // How far inside the tilemap edge the player must stay (world units)
    // How far ahead of the player to sample the tile (world units) — acts like a body radius
    private const float LookAhead = 0.3f;

    private Rigidbody2D     rb;
    private Animator        animator;
    private VirtualJoystick joystick;

    // Animator parameter names matching elf_fighter.controller
    // Walk blend tree is 2D Simple Directional — driven by float params "x" and "y"
    private static readonly int ParamWalk = Animator.StringToHash("Walk");
    private static readonly int ParamX    = Animator.StringToHash("x");
    private static readonly int ParamY    = Animator.StringToHash("y");

    // Cached each Update so FixedUpdate can consume the same frame's input
    private Vector2 _cachedInput;
    private bool    _isMoving;

    public bool IsMoving => _isMoving;

    private void Awake()
    {
        rb       = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        rb.gravityScale   = 0f;
        rb.freezeRotation = true;

        joystick = FindAnyObjectByType<VirtualJoystick>();

        if (walkableTilemap == null)
            walkableTilemap = FindAnyObjectByType<Tilemap>();
    }

    private void OnEnable()  => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        joystick = FindAnyObjectByType<VirtualJoystick>();
        if (walkableTilemap == null || !walkableTilemap)
            walkableTilemap = FindAnyObjectByType<Tilemap>();
    }


    // Animator runs on Update cadence — update it here, not in FixedUpdate
    private void Update()
    {
        _cachedInput = ReadInput();
        _isMoving    = _cachedInput.sqrMagnitude > 0.01f;

        animator.SetBool(ParamWalk, _isMoving);
        if (_isMoving)
        {
            // Feed raw direction into the 2D blend tree directly
            animator.SetFloat(ParamX, _cachedInput.x);
            animator.SetFloat(ParamY, _cachedInput.y);
        }
    }

    // Physics on FixedUpdate cadence — uses input cached in Update
    private void FixedUpdate()
    {
        if (!_isMoving) { rb.linearVelocity = Vector2.zero; return; }

        Vector2 vel = new Vector2(_cachedInput.x, _cachedInput.y * 0.5f).normalized * moveSpeed;

        // Per-axis tile check: sample LookAhead units ahead on each axis independently
        // so the player can slide along tile borders instead of stopping dead
        if (walkableTilemap != null)
        {
            if (!HasTileAt(rb.position + new Vector2(Mathf.Sign(vel.x) * LookAhead, 0f))) vel.x = 0f;
            if (!HasTileAt(rb.position + new Vector2(0f, Mathf.Sign(vel.y) * LookAhead))) vel.y = 0f;
        }

        rb.linearVelocity = vel;
    }

    private bool HasTileAt(Vector2 worldPos)
    {
        Vector3Int cell = walkableTilemap.WorldToCell(new Vector3(worldPos.x, worldPos.y, 0f));
        return walkableTilemap.HasTile(cell);
    }

    private Vector2 ReadInput()
    {
        if (joystick != null && joystick.Direction.sqrMagnitude > 0.01f)
            return joystick.Direction;

        var keyboard = Keyboard.current;
        if (keyboard == null) return Vector2.zero;

        Vector2 input = Vector2.zero;
        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)    input.y += 1f;
        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)  input.y -= 1f;
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) input.x += 1f;
        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)  input.x -= 1f;

        return input;
    }
}
