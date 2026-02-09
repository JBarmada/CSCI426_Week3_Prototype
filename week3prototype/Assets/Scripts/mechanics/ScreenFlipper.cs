using System.Collections;
using UnityEngine;

public class ScreenFlipper : MonoBehaviour
{
    [Header("Settings")]
    public RectTransform targetToFlip; // Assign the Canvas or Content parent
    public float flipDuration = 1.2f;
    
    // Default to a smooth S-curve
    public AnimationCurve flipCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private bool isFlipping = false;

    public void DoFlip()
    {
        if (isFlipping || targetToFlip == null) return;
        StartCoroutine(FlipRoutine());
    }

    IEnumerator FlipRoutine()
    {
        isFlipping = true;
        float elapsed = 0f;
        Quaternion startRot = targetToFlip.localRotation;
        
        // Target: Spin 360 degrees on Z axis (screen roll)
        Vector3 axis = Vector3.forward; 

        while (elapsed < flipDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / flipDuration);
            float curveVal = flipCurve.Evaluate(t);

            // Rotate 360 degrees
            float angle = curveVal * 360f;
            targetToFlip.localRotation = startRot * Quaternion.AngleAxis(angle, axis);

            yield return null;
        }

        targetToFlip.localRotation = startRot; // Ensure perfect reset
        isFlipping = false;
    }
}