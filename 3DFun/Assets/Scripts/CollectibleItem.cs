using UnityEngine;

public class CollectibleItem : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // Prüft, ob das Objekt, das den Trigger betritt, der Spieler ist
        if (other.CompareTag("Player"))
        {
            // Meldet das Einsammeln an den Singleton-Manager
            if (PillenImClub.Instance != null)
            {
                PillenImClub.Instance.OnCollectibleGathered();
            }

            // Zerstört das Item aus der Szene
            Destroy(gameObject);
        }
    }
}
