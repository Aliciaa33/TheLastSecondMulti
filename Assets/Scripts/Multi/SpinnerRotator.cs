using UnityEngine;

/// <summary>
/// Attach to the Spinner Image to make it rotate continuously.
/// No setup needed.
/// </summary>
public class SpinnerRotator : MonoBehaviour
{
    [SerializeField] private float degreesPerSecond = 270f;

    void Update()
    {
        transform.Rotate(0f, 0f, -degreesPerSecond * Time.deltaTime);
    }
}
