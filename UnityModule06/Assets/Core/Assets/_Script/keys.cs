using UnityEngine;

public class keys : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;
        OpenDoor[] doors = FindObjectsOfType<OpenDoor>();
        foreach (OpenDoor door in doors)
        {
            door.AddKey();
        }
        Destroy(gameObject);
    }
}
