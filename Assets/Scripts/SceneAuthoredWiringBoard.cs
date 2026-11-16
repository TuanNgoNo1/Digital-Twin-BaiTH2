using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Scene-authored presentation data for the flat wiring board. SocketPoint
/// components remain on the existing Sockets hierarchy; this component only
/// stores their operation/wiring poses and the persistent visual objects.
/// </summary>
public sealed class SceneAuthoredWiringBoard : MonoBehaviour
{
    private readonly Dictionary<Renderer, bool> legacyRenderers = new Dictionary<Renderer, bool>();
    private bool? wiringView;

    // Keep the lesson's operation model and its original socket poses intact.
    public void ShowWiringView(bool show)
    {
        if (wiringView == show || boardRenderer == null) return;
        if (show && Camera.main != null)
        {
            Vector3 center = boardRenderer.bounds.center;
            Quaternion delta = Camera.main.transform.rotation * Quaternion.Inverse(boardRenderer.transform.rotation);
            transform.position = center + delta * (transform.position - center);
            transform.rotation = delta * transform.rotation;
            for (int i = 0; i < wiringPositions.Count; i++)
            {
                wiringPositions[i] = center + delta * (wiringPositions[i] - center);
                wiringRotations[i] = delta * wiringRotations[i];
            }
        }
        if (legacyRenderers.Count == 0)
        {
            foreach (Transform candidate in FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (candidate.name != "Board") continue;
                foreach (Renderer renderer in candidate.root.GetComponentsInChildren<Renderer>(true))
                {
                    if (renderer.GetComponentInParent<WireBody>() == null &&
                        renderer.GetComponentInParent<SceneAuthoredWiringBoard>() == null)
                        legacyRenderers[renderer] = renderer.enabled;
                }
                break;
            }
        }
        foreach (var entry in legacyRenderers)
            if (entry.Key != null) entry.Key.enabled = !show && entry.Value;
        boardRenderer.gameObject.SetActive(show);
        if (jackVisualRoot != null) jackVisualRoot.SetActive(show);
        for (int i = 0; i < sockets.Count; i++)
        {
            SocketPoint socket = sockets[i];
            if (socket == null || !TryGetPoses(socket, out Vector3 operation, out Quaternion operationRotation,
                    out Vector3 wiring, out Quaternion wiringRotation)) continue;
            socket.transform.SetPositionAndRotation(show ? wiring : operation, show ? wiringRotation : operationRotation);
        }
        foreach (WireBody wire in FindObjectsByType<WireBody>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            MoveConnectedPlug(wire.plugA);
            MoveConnectedPlug(wire.plugB);
        }
        wiringView = show;
    }

    private static void MoveConnectedPlug(WirePlug plug)
    {
        if (plug != null && plug.isSnapped && plug.connectedSocket != null)
            plug.transform.SetPositionAndRotation(plug.connectedSocket.transform.position, plug.connectedSocket.transform.rotation);
    }

    public Renderer boardRenderer;
    public Transform socketsRoot;
    public GameObject jackVisualRoot;
    public List<SocketPoint> sockets = new List<SocketPoint>();
    public List<Vector3> operationPositions = new List<Vector3>();
    public List<Quaternion> operationRotations = new List<Quaternion>();
    public List<Vector3> wiringPositions = new List<Vector3>();
    public List<Quaternion> wiringRotations = new List<Quaternion>();

    public bool TryGetPoses(
        SocketPoint socket,
        out Vector3 operationPosition,
        out Quaternion operationRotation,
        out Vector3 wiringPosition,
        out Quaternion wiringRotation)
    {
        int index = sockets.IndexOf(socket);
        bool valid = index >= 0 &&
            index < operationPositions.Count && index < operationRotations.Count &&
            index < wiringPositions.Count && index < wiringRotations.Count;
        if (valid)
        {
            operationPosition = operationPositions[index];
            operationRotation = operationRotations[index];
            wiringPosition = wiringPositions[index];
            wiringRotation = wiringRotations[index];
            return true;
        }

        operationPosition = wiringPosition = socket != null ? socket.transform.position : Vector3.zero;
        operationRotation = wiringRotation = socket != null ? socket.transform.rotation : Quaternion.identity;
        return false;
    }
}
