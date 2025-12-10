using UnityEngine;

public class OpenDoor : MonoBehaviour
{
    [SerializeField] private GameObject door;
    [SerializeField] private float doorOpenAngle = 90f;
    [SerializeField] private float doorOpenSpeed = 2.0f;
    [SerializeField] private string playerTag = "Player";
    
    private Quaternion doorCloseRotation;
    private Quaternion doorOpenRotation;
    private bool isDoorOpen = false;
    private const float POSITION_THRESHOLD = 0.01f;

    public bool canOpenWithoutKey = true;
    public int nbKeys = 0;

    private void OnEnable()
    {
        if (door == null)
        {
            Debug.LogError("OpenDoor: Missing door reference!", gameObject);
            enabled = false;
        }
        else
        {
            doorCloseRotation = door.transform.rotation;
        }

    }

    private void Update()
    {
        Quaternion targetRotation = isDoorOpen ? doorOpenRotation : doorCloseRotation;
        
        door.transform.rotation = Quaternion.Slerp(
            door.transform.rotation, 
            targetRotation, 
            Time.deltaTime * doorOpenSpeed
        );

        if (nbKeys >= 3)
        {
            canOpenWithoutKey = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            if (canOpenWithoutKey == false)
                return;
            
            // Calculer la direction du joueur par rapport à la porte
            Vector3 doorToPlayer = other.transform.position - door.transform.position;
            Vector3 doorForward = door.transform.forward;
            
            // Déterminer de quel côté le joueur arrive
            float dot = Vector3.Dot(doorToPlayer, doorForward);
            
            // Si dot > 0, le joueur est devant la porte, sinon derrière
            float openDirection = dot > 0 ? 1f : -1f;
            
            // Calculer la rotation d'ouverture basée sur le côté du joueur
            doorOpenRotation = doorCloseRotation * Quaternion.Euler(0, doorOpenAngle * openDirection, 0);
            
            isDoorOpen = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            isDoorOpen = false;
        }
    }

    public void AddKey()
    {
        nbKeys++;
        Debug.Log("Keys collected: " + nbKeys);
    }
}