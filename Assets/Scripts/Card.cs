using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class Card : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler
{
    [Header("Graphics (UI Images)")]
    public Transform flipRoot;      // child transform that will be flipped (scale X)
    public Image frontImage;        // inside flipRoot
    public Image backImage;         // inside flipRoot

    [Header("Flip")]
    public float flipTime = 0.28f;

    [Header("Hover")]
    public float hoverScale = 1.08f;
    public float hoverAnimTime = 0.12f;

    [Header("State")]
    public int faceId;
    public bool IsMatched { get; private set; } = false;
    public bool IsRevealed { get; private set; } = false;

    // Internal state
    private bool isFlipping = false;
    private bool isClickAnimating = false;
    private bool isInteractable = true;
    private bool isHovered = false;
    private Vector3 baseScale;
    private Coroutine currentScaleAnimation;
    private Coroutine mismatchCoroutine;

    void Awake()
    {
        baseScale = transform.localScale;
        if (flipRoot == null)
            flipRoot = transform;
    }

    public void Initialize(int id, Sprite frontSprite, Sprite backSprite)
    {
        faceId = id;
        if (frontImage) frontImage.sprite = frontSprite;
        if (backImage) backImage.sprite = backSprite;

        IsMatched = false;
        IsRevealed = false;
        isFlipping = false;
        isClickAnimating = false;
        isInteractable = true;
        isHovered = false;

        transform.localScale = baseScale;
        if (flipRoot != null) flipRoot.localScale = Vector3.one;
        if (frontImage) frontImage.gameObject.SetActive(false);
        if (backImage) backImage.gameObject.SetActive(true);

        StopAllAnimations();
    }

    public void OnClicked()
    {
        if (!CanClick()) return;

        Debug.Log($"Card {faceId} clicked");
        StartCoroutine(HandleClick());
    }

    private bool CanClick()
    {
        bool canClick = isInteractable && !isFlipping && !isClickAnimating && !IsMatched && !IsRevealed;
        Debug.Log($"Card {faceId} CanClick: {canClick} (interactable:{isInteractable}, flipping:{isFlipping}, clickAnim:{isClickAnimating}, matched:{IsMatched}, revealed:{IsRevealed})");
        return canClick;
    }

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

    // Called by GameManager when cards match

    public void MarkMatched()
    {
        Debug.Log($"Card {faceId} marked as matched");
        IsMatched = true;
        IsRevealed = true; // Ensure it stays revealed
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

        Debug.Log($"Card {faceId} matched state applied - front visible, not interactable");
    }

    // Called by GameManager when cards don't match
    public void HideAfterMismatch()
    {
        Debug.Log($"Card {faceId} hiding after mismatch");
        if (mismatchCoroutine != null)
        {
            StopCoroutine(mismatchCoroutine);
        }
        mismatchCoroutine = StartCoroutine(HandleMismatch());
    }

    private IEnumerator HandleMismatch()
    {
        // Wait a moment, then flip back
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
        Debug.Log($"Card {faceId} ready for interaction again");
    }

    // Main flip animation
    private IEnumerator FlipCard(bool showFront)
    {
        isFlipping = true;

        float halfTime = flipTime * 0.5f;

        // First half: scale X down to 0
        yield return StartCoroutine(FlipToEdge());

        // Swap the visuals at the edge
        if (frontImage) frontImage.gameObject.SetActive(showFront);
        if (backImage) backImage.gameObject.SetActive(!showFront);
        IsRevealed = showFront;

        // Second half: scale X back to 1
        yield return StartCoroutine(FlipFromEdge());

        isFlipping = false;
        Debug.Log($"Card {faceId} flip complete. IsRevealed: {IsRevealed}");
    }

    private IEnumerator FlipToEdge()
    {
        float halfTime = flipTime * 0.5f;
        float t = 0f;
        Vector3 startScale = flipRoot.localScale;

        while (t < halfTime)
        {
            t += Time.deltaTime;
            float progress = t / halfTime;
            float easedProgress = 1f - Mathf.Pow(1f - progress, 3f); // Ease out

            float scaleX = Mathf.Lerp(startScale.x, 0f, easedProgress);
            flipRoot.localScale = new Vector3(scaleX, startScale.y, startScale.z);

            yield return null;
        }

        flipRoot.localScale = new Vector3(0f, startScale.y, startScale.z);
    }

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

        flipRoot.localScale = Vector3.one;
    }

    // Hover handlers
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!CanHover()) return;

        isHovered = true;
        AnimateToScale(baseScale * hoverScale, hoverAnimTime);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;

        if (!CanHover()) return;

        AnimateToScale(baseScale, hoverAnimTime);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // Just visual feedback, no scaling change
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (CanClick())
        {
            OnClicked();
        }
    }

    private bool CanHover()
    {
        return isInteractable && !isFlipping && !isClickAnimating && !IsMatched;
    }

    // Scale animation helper
    private void AnimateToScale(Vector3 targetScale, float duration)
    {
        if (currentScaleAnimation != null)
        {
            StopCoroutine(currentScaleAnimation);
        }

        currentScaleAnimation = StartCoroutine(ScaleAnimation(targetScale, duration));
    }

    private IEnumerator ScaleAnimation(Vector3 targetScale, float duration)
    {
        Vector3 startScale = transform.localScale;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float progress = Mathf.Clamp01(t / duration);
            float easedProgress = 1f - Mathf.Pow(1f - progress, 3f); // Ease out

            transform.localScale = Vector3.Lerp(startScale, targetScale, easedProgress);
            yield return null;
        }

        transform.localScale = targetScale;
        currentScaleAnimation = null;
    }

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

    // Public methods for game manager
    public void Reveal()
    {
        if (!isFlipping && !IsRevealed && !IsMatched)
        {
            StartCoroutine(FlipCard(true));
        }
    }

    public void HideInstant()
    {
        Debug.Log($"Card {faceId} hide instant called");
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

            Debug.Log($"Card {faceId} reset to initial state");
        }
    }

    public void ResetCard()
    {
        // Stop all ongoing coroutines
        StopAllAnimations();

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
    }
}