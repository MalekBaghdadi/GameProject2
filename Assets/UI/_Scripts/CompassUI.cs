using UnityEngine;
using UnityEngine.UI;

public class CompassUI : MonoBehaviour
{
    public RectTransform needle;    

    float displayedYaw = 0f;
    public float smooth = 8f;

    void Update()
    {
        needle.localEulerAngles = Vector3.Lerp(needle.localEulerAngles, new Vector3(0,0,-displayedYaw), Time.deltaTime * smooth);
    }

    // Public API
    public void SetYaw(float yawDegrees) => displayedYaw = Mathf.Repeat(yawDegrees, 360f);
}