using UnityEngine;

// Editor/desktop stand-in for real-world movement. Stage 4 swaps this for a
// sensor-driven IInputSource (GPS heading + step detection) with zero controller changes.
public class KeyboardMouseInputSource : MonoBehaviour, IInputSource
{
    public float Horizontal => Input.GetAxis("Horizontal");
    public float Vertical => Input.GetAxis("Vertical");
    public float LookX => Input.GetAxis("Mouse X");
    public float LookY => Input.GetAxis("Mouse Y");
}
