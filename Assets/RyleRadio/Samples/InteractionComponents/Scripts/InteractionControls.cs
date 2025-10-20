using RyleRadio.Components;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class InteractionControls : MonoBehaviour
{
    [SerializeField] private RadioOutput output;
    [SerializeField] private Text text;
    [SerializeField] private RectTransform indicator;

    [Space(8)]
    [SerializeField] private Vector2 indicatorXRange;

    [Space(8)]
    [SerializeField] private float tuneSpeed = 200;

    private void Update()
    {
        // if shift is held down, tune faster
        if (Input.GetKeyDown(KeyCode.LeftShift))
            tuneSpeed *= 2;

        if (Input.GetKeyUp(KeyCode.LeftShift))
            tuneSpeed /= 2;

        // move the tune left and right based on keys
        if (Input.GetKey(KeyCode.Q))
            output.Tune -= tuneSpeed * Time.deltaTime;

        if (Input.GetKey(KeyCode.E))
            output.Tune += tuneSpeed * Time.deltaTime;

        // set the text showing the tune number
        text.text = (Mathf.Round(output.DisplayTune * 10) / 10).ToString();

        // move the indicator to the correct position
        indicator.transform.localPosition = new Vector3(Mathf.Lerp(indicatorXRange.x, indicatorXRange.y, output.Tune01), 0, 0);
    }
}
