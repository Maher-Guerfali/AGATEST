using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Individual card behavior with flip animations, hover effects, and interaction handling.
/// Each card represents one item in the matching game grid.
/// 
/// VISUAL STRUCTURE:
/// Card GameObject
/// └── FlipRoot (Transform)
///     ├── FrontImage (Image) - The card face (animal, number, etc.)
///     └── BackImage (Image) - The card back (uniform design)
/// 
/// ANIMATION SYSTEM:
/// - Flip: 3D rotation effect via X-scale manipulation (scale 1 → 0 → 1)
/// - Hover: Scale up to hoverScale (default 1.08x) with smooth easing
/// - Click: Flip animation followed by GameManager notification
/// 
/// STATE MACHINE:
/// Face Down (Initial) → Revealed (Flipped) → Matched (Permanent) or Back to Face Down
/// 
/// INTERACTION RULES:
/// - Can only flip when: not flipping, not matched, not already revealed
/// - Cannot interact during: preview, animations, game over
/// - Matched cards: permanently revealed and non-interactive
/// 
/// Author: [Your Team Name]
/// Last Modified: 2025
/// </summary>
public class Card : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler
{
    #region Inspector References

    [Header("Graphics (UI Images)")]
    [Tooltip("Child transform that will be scaled on X axis to create flip effect")]
    public Transform flipRoot;

    [Tooltip("Image component displaying the card front (inside flipRoot)")]
    public Image frontImage;

    [Tooltip("Image component displaying the card back (inside flipRoot)")]
    public Image backImage;

    [Header("Flip")]
    [Tooltip("Duration of flip animation in seconds")]
    public float flipTime = 0.28f;

    [Header("Hover")]
    [Tooltip("Scale multiplier when hovering (1.08 = 8% larger)")]
    public float hoverScale = 1.08f;

    [Tooltip("Duration of hover scale animation")]
    public float hoverAnimTime = 0.12f;

    [Header("State")]
    [Tooltip("Unique identifier used for matching (e.g., 0-7 for 8 pairs)")]
    public int faceId;

    /// <summary>
    /// True if this card has been successfully matched and is permanently revealed.
    /// Matched cards cannot be interacted with.
    /// </summary>
    public bool IsMatched { get; private set; } = false;

    /// <summary>
    /// True if the card is currently showing its front face.
    /// False when showing back face.
    /// </summary>
    public bool IsRevealed { get; private set; } = false;

    #endregion

    #region Private State Variables

    /// <summary>
    /// Flag indicating flip animation is in progress.
    /// Prevents interaction during animation.
    /// </summary>
    private bool isFlipping = false;

    /// <summary>
    /// Flag indicating click is being processed.
    /// Prevents multiple simultaneous clicks.
    /// </summary>
    private bool isClickAnimating = false;

    /// <summary>
    /// Flag controlling whether card can be clicked.
    /// Set to false for matched cards or during comparisons.
    /// </summary>
    private bool isInteractable = true;

    /// <summary>
    /// Flag tracking if mouse is currently over the card.
    /// Used to maintain hover state after animations.
    /// </summary>
    private bool isHovered = false;

    /// <summary>
    /// Stores the original scale of the card for hover animations.
    /// Set in Awake() and used as baseline for scaling.
    /// </summary>
    private Vector3 baseScale;

    /// <summary>
    /// Reference to currently running scale animation coroutine.
    /// Stored to allow cancellation when starting new scale animation.
    /// </summary>
    private Coroutine currentScaleAnimation;

    /// <summary>
    /// Reference to mismatch hide coroutine.
    /// Stored to allow cancellation if card is matched before hide completes.
    /// </summary>
    private Coroutine mismatchCoroutine;

    #endregion

    #region Unity Lifecycle

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Initializes base scale and ensures flipRoot is assigned.
    /// </summary>
    void Awake()
    {
        baseScale = transform.localScale;

        if (flipRoot == null)
        {
            flipRoot = transform;
            Debug.LogWarning("[Card] flipRoot not assigned, using self transform");
        }
    }

    #endregion

    #region Initialization

    /// <summary>
    /// Initializes the card with its identity and sprites.
    /// MUST be called after instantiation before card can be used.
    /// 
    /// Sets card to initial state:
    /// - Face down (back showing)
    /// - Not matched
    /// - Not revealed
    /// - Interactable
    /// - Normal scale
    /// 
    /// Call this when:
    /// - Creating new grid
    /// - Reusing card from object pool
    /// - Loading saved game
    /// </summary>
    /// <param name="id">Unique face ID for matching (e.g., 0-7 for 8 pairs)</param>
    /// <param name="frontSprite">Sprite to show on front face</param>
    /// <param name="backSprite">Sprite to show on back face</param>
    public void Initialize(int id, Sprite frontSprite, Sprite backSprite)
    {
        faceId = id;

        if (frontImage) frontImage.sprite = frontSprite;
        if (backImage) backImage.sprite = backSprite;

        // Reset all state flags
        IsMatched = false;
        IsRevealed = false;
        isFlipping = false;
        isClickAnimating = false;
        isInteractable = true;
        isHovered = false;

        // Reset scales
        transform.localScale = baseScale;
        if (flipRoot != null) flipRoot.localScale = Vector3.one;

        // Show back, hide front
        if (frontImage) frontImage.gameObject.SetActive(false);
        if (backImage) backImage.gameObject.SetActive(true);

        // Stop any running animations
        StopAllAnimations();

        Debug.Log($"[Card] Initialized card with faceId={id}");
    }

    #endregion

    #region Click Handling

    /// <summary>
    /// Main click handler called by Unity UI Button or pointer events.
    /// Validates click is allowed, performs flip, and notifies GameManager.
    /// </summary>
    public void OnClicked()
    {
        if (!CanClick()) return;

        Debug.Log($"[Card {faceId}] Clicked");
        StartCoroutine(HandleClick());
    }

    /// <summary>
    /// Validates if the card can currently be clicked.
    /// 
    /// REQUIREMENTS for clickable card:
    /// - isInteractable = true (not disabled)
    /// - isFlipping = false (not animating)
    /// - isClickAnimating = false (not processing click)
    /// - IsMatched = false (not already matched)
    /// - IsRevealed = false (not already face up)
    /// 
    /// RECOMMENDED FIX: Add GameManager state check:
    /// bool gameManagerReady = GameManager.Instance != null 
    ///     && GameManager.Instance.IsGameStarted 
    ///     && !GameManager.Instance.IsPreviewActive;
    /// </summary>
    /// <returns>True if card can be clicked, false otherwise</returns>
    private bool CanClick()
    {
        bool canClick = isInteractable && !isFlipping && !isClickAnimating && !IsMatched && !IsRevealed;

        Debug.Log($"[Card {faceId}] CanClick: {canClick} " +
                  $"(interactable:{isInteractable}, flipping:{isFlipping}, " +
                  $"clickAnim:{isClickAnimating}, matched:{IsMatched}, revealed:{IsRevealed})");

        return canClick;
    }

    /// <summary>
    /// Coroutine that handles the complete click sequence.
    /// 
    /// SEQUENCE:
    /// 1. Set flags to prevent additional clicks
    /// 2. Stop any mismatch animations in progress
    /// 3. Flip card to reveal front face
    /// 4. Play flip sound
    /// 5. Notify GameManager to register the flip
    /// 6. Reset click animation flag
    /// 
    /// NOTE: Interactable is NOT restored here - GameManager controls that
    /// </summary>
    private IEnumerator HandleClick()
    {
        isClickAnimating = true;
        isInteractable = false;

        // Stop any ongoing mismatch coroutine
        if (mismatchCoroutine != null)
        {
            StopCoroutine(mismatchCoroutine);
            mismatchCoroutine = null;
        }

        // Flip the card to reveal
        yield return StartCoroutine(FlipCard(true));

        // Play sound
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayFlip();

        // Notify game manager AFTER the flip is complete
        if (GameManager.Instance != null)
            GameManager.Instance.RegisterFlip(this);

        isClickAnimating = false;
        // Don't set interactable back to true here - let game manager handle it
    }

    #endregion

    #region State Change Methods (Called by GameManager)

    /// <summary>
    /// Marks this card as successfully matched.
    /// Called by GameManager when this card is part of a matching group.
    /// 
    /// EFFECTS:
    /// - Sets IsMatched = true
    /// - Sets IsRevealed = true
    /// - Disables interaction permanently
    /// - Ensures front face is visible
    /// - Stops any mismatch animations
    /// - Returns to base scale smoothly
    /// 
    /// Matched cards remain visible and unclickable for rest of game.
    /// </summary>
    public void MarkMatched()
    {
        Debug.Log($"[Card {faceId}] Marked as matched");

        IsMatched = true;
        IsRevealed = true;
        isInteractable = false;

        // Stop any ongoing mismatch animations
        if (mismatchCoroutine != null)
        {
            StopCoroutine(mismatchCoroutine);
            mismatchCoroutine = null;
        }

        // Ensure the card is showing its front face
        if (frontImage) frontImage.gameObject.SetActive(true);
        if (backImage) backImage.gameObject.SetActive(false);

        // Make sure flip root is at normal scale (not flipped)
        if (flipRoot != null) flipRoot.localScale = Vector3.one;

        // Return to base scale smoothly
        AnimateToScale(baseScale, 0.25f);

        Debug.Log($"[Card {faceId}] Matched state applied - front visible, not interactable");
    }

    /// <summary>
    /// Hides card after a mismatch with delay.
    /// Called by GameManager when this card is part of a non-matching group.
    /// 
    /// SEQUENCE:
    /// 1. Wait 1 second (give player time to see the mismatch)
    /// 2. Flip card back to face down
    /// 3. Re-enable interaction
    /// 4. Apply hover state if mouse still over card
    /// </summary>
    public void HideAfterMismatch()
    {
        Debug.Log($"[Card {faceId}] Hiding after mismatch");

        if (mismatchCoroutine != null)
        {
            StopCoroutine(mismatchCoroutine);
        }
        mismatchCoroutine = StartCoroutine(HandleMismatch());
    }

    /// <summary>
    /// Coroutine that handles the mismatch hide sequence.
    /// </summary>
    private IEnumerator HandleMismatch()
    {
        // Wait a moment so player can see the mismatch
        yield return new WaitForSeconds(1.0f);

        // Flip back to hide
        yield return StartCoroutine(FlipCard(false));

        // Re-enable interaction
        isInteractable = true;

        // Apply hover state if still hovered
        if (isHovered)
        {
            AnimateToScale(baseScale * hoverScale, hoverAnimTime);
        }
        else
        {
            AnimateToScale(baseScale, hoverAnimTime);
        }

        mismatchCoroutine = null;
        Debug.Log($"[Card {faceId}] Ready for interaction again");
    }

    #endregion

    #region Flip Animation

    /// <summary>
    /// Main flip animation coroutine.
    /// Creates 3D card flip effect by scaling X axis from 1 → 0 → 1.
    /// 
    /// ALGORITHM:
    /// 1. First half: Scale X from 1 to 0 (flip to edge)
    /// 2. At edge (scale X = 0): Swap front/back visibility
    /// 3. Second half: Scale X from 0 to 1 (flip from edge)
    /// 
    /// Total duration: flipTime (default 0.28 seconds)
    /// Easing: Ease-out for first half, ease-in for second half
    /// </summary>
    /// <param name="showFront">True to show front face, false to show back</param>
    private IEnumerator FlipCard(bool showFront)
    {
        isFlipping = true;

        // First half: scale X down to 0
        yield return StartCoroutine(FlipToEdge());

        // Swap the visuals at the edge
        if (frontImage) frontImage.gameObject.SetActive(showFront);
        if (backImage) backImage.gameObject.SetActive(!showFront);
        IsRevealed = showFront;

        // Second half: scale X back to 1
        yield return StartCoroutine(FlipFromEdge());

        isFlipping = false;
        Debug.Log($"[Card {faceId}] Flip complete. IsRevealed: {IsRevealed}");
    }

    /// <summary>
    /// First half of flip animation: Scale X from current to 0.
    /// Uses ease-out curve for smooth deceleration.
    /// </summary>
    private IEnumerator FlipToEdge()
    {
        float halfTime = flipTime * 0.5f;
        float t = 0f;
        Vector3 startScale = flipRoot.localScale;

        while (t < halfTime)
        {
            t += Time.deltaTime;
            float progress = t / halfTime;
            float easedProgress = 1f - Mathf.Pow(1f - progress, 3f); // Ease out cubic

            float scaleX = Mathf.Lerp(startScale.x, 0f, easedProgress);
            flipRoot.localScale = new Vector3(scaleX, startScale.y, startScale.z);

            yield return null;
        }

        // Ensure we end exactly at 0
        flipRoot.localScale = new Vector3(0f, startScale.y, startScale.z);
    }

    /// <summary>
    /// Second half of flip animation: Scale X from 0 to 1.
    /// Uses ease-in curve for smooth acceleration.
    /// </summary>
    private IEnumerator FlipFromEdge()
    {
        float halfTime = flipTime * 0.5f;
        float t = 0f;
        Vector3 currentScale = flipRoot.localScale;

        while (t < halfTime)
        {
            t += Time.deltaTime;
            float progress = t / halfTime;
            float easedProgress = Mathf.Pow(progress, 0.7f); // Ease in

            float scaleX = Mathf.Lerp(0f, 1f, easedProgress);
            flipRoot.localScale = new Vector3(scaleX, currentScale.y, currentScale.z);

            yield return null;
        }

        // Ensure we end exactly at 1
        flipRoot.localScale = Vector3.one;
    }

    #endregion

    #region Pointer Event Handlers (Hover)

    /// <summary>
    /// Called when mouse enters card area.
    /// Scales up card if hoverable.
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!CanHover()) return;

        isHovered = true;
        AnimateToScale(baseScale * hoverScale, hoverAnimTime);
    }

    /// <summary>
    /// Called when mouse exits card area.
    /// Returns card to normal scale if hoverable.
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;

        if (!CanHover()) return;

        AnimateToScale(baseScale, hoverAnimTime);
    }

    /// <summary>
    /// Called when mouse button pressed on card.
    /// Currently unused, but can be used for press-down visual feedback.
    /// </summary>
    public void OnPointerDown(PointerEventData eventData)
    {
        // Just visual feedback, no scaling change
        // Could add slight scale down here for "pressed" effect
    }

    /// <summary>
    /// Called when mouse button released on card.
    /// Triggers click if card is clickable.
    /// </summary>
    public void OnPointerUp(PointerEventData eventData)
    {
        if (CanClick())
        {
            OnClicked();
        }
    }

    /// <summary>
    /// Validates if the card can currently show hover effects.
    /// </summary>
    /// <returns>True if card can respond to hover</returns>
    private bool CanHover()
    {
        return isInteractable && !isFlipping && !isClickAnimating && !IsMatched;
    }

    #endregion

    #region Scale Animation Helper

    /// <summary>
    /// Smoothly animates card to target scale.
    /// Cancels any previous scale animation.
    /// </summary>
    /// <param name="targetScale">Scale to animate to</param>
    /// <param name="duration">Animation duration in seconds</param>
    private void AnimateToScale(Vector3 targetScale, float duration)
    {
        if (currentScaleAnimation != null)
        {
            StopCoroutine(currentScaleAnimation);
        }

        currentScaleAnimation = StartCoroutine(ScaleAnimation(targetScale, duration));
    }

    /// <summary>
    /// Coroutine that performs smooth scale animation with ease-out.
    /// </summary>
    private IEnumerator ScaleAnimation(Vector3 targetScale, float duration)
    {
        Vector3 startScale = transform.localScale;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float progress = Mathf.Clamp01(t / duration);
            float easedProgress = 1f - Mathf.Pow(1f - progress, 3f); // Ease out cubic

            transform.localScale = Vector3.Lerp(startScale, targetScale, easedProgress);
            yield return null;
        }

        transform.localScale = targetScale;
        currentScaleAnimation = null;
    }

    /// <summary>
    /// Stops all running animation coroutines.
    /// Called during initialization to ensure clean state.
    /// </summary>
    private void StopAllAnimations()
    {
        if (currentScaleAnimation != null)
        {
            StopCoroutine(currentScaleAnimation);
            currentScaleAnimation = null;
        }
        if (mismatchCoroutine != null)
        {
            StopCoroutine(mismatchCoroutine);
            mismatchCoroutine = null;
        }
    }

    #endregion

    #region Public Utility Methods

    /// <summary>
    /// Reveals the card (shows front face) without animation.
    /// Used by GameManager for preview system.
    /// Only works if card is not flipping, not revealed, and not matched.
    /// </summary>
    public void Reveal()
    {
        if (!isFlipping && !IsRevealed && !IsMatched)
        {
            StartCoroutine(FlipCard(true));
        }
    }

    /// <summary>
    /// Instantly hides the card (shows back face) without animation.
    /// Used when preview ends or when loading saved game.
    /// 
    /// EFFECTS:
    /// - Immediately switches to back face
    /// - Sets IsRevealed = false
    /// - Re-enables interaction
    /// - Maintains hover state if still hovered
    /// </summary>
    public void HideInstant()
    {
        Debug.Log($"[Card {faceId}] Hide instant called");

        if (!IsMatched && IsRevealed)
        {
            // Stop any ongoing mismatch coroutine
            if (mismatchCoroutine != null)
            {
                StopCoroutine(mismatchCoroutine);
                mismatchCoroutine = null;
            }

            // Immediately reset visuals
            if (frontImage) frontImage.gameObject.SetActive(false);
            if (backImage) backImage.gameObject.SetActive(true);
            IsRevealed = false;

            // Reset flip root scale
            if (flipRoot != null) flipRoot.localScale = Vector3.one;

            // Re-enable interaction
            isInteractable = true;

            // Apply hover state if still hovered
            if (isHovered)
            {
                AnimateToScale(baseScale * hoverScale, hoverAnimTime);
            }
            else
            {
                AnimateToScale(baseScale, hoverAnimTime);
            }

            Debug.Log($"[Card {faceId}] Reset to initial state");
        }
    }

    /// <summary>
    /// Completely resets card to initial state.
    /// Stops all animations and coroutines.
    /// Use when recycling card from object pool.
    /// </summary>
    public void ResetCard()
    {
        // Stop all ongoing coroutines
        StopAllAnimations();

        // Reset all flags
        IsMatched = false;
        IsRevealed = false;
        isFlipping = false;
        isClickAnimating = false;
        isInteractable = true;
        isHovered = false;

        // Reset visuals
        if (frontImage) frontImage.gameObject.SetActive(false);
        if (backImage) backImage.gameObject.SetActive(true);

        // Reset scales
        transform.localScale = baseScale;
        if (flipRoot != null) flipRoot.localScale = Vector3.one;

        Debug.Log($"[Card {faceId}] Completely reset");
    }

    #endregion
}

/*
 * USAGE EXAMPLES:
 * 
 * 1. Initialize a new card:
 *    card.Initialize(0, animalSprite, backSprite);
 * 
 * 2. Manually reveal (for preview):
 *    card.Reveal();
 * 
 * 3. Mark as matched (after comparison):
 *    card.MarkMatched();
 * 
 * 4. Hide after mismatch:
 *    card.HideAfterMismatch();
 * 
 * 5. Instantly hide (end preview):
 *    card.HideInstant();
 * 
 * KNOWN ISSUES:
 * 
 * 1. Preview Interaction Bug:
 *    Cards can be clicked during preview countdown.
 *    FIX: Add GameManager check to CanClick():
 *    
 *    private bool CanClick() {
 *        bool gameManagerReady = GameManager.Instance != null 
 *            && GameManager.Instance.IsGameStarted 
 *            && !GameManager.Instance.IsPreviewActive;
 *        return gameManagerReady && isInteractable && !isFlipping 
 *            && !isClickAnimating && !IsMatched && !IsRevealed;
 *    }
 */