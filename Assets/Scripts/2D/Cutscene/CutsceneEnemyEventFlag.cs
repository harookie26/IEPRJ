using UnityEngine;
using static EventNames;

public class CutsceneEnemyEventFlag : MonoBehaviour
{
    [SerializeField] private string cutsceneName;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the object that entered the trigger is on the "Player" layer.
        if (other.gameObject.layer == LayerMask.NameToLayer("Enemy"))
        {
            CutsceneManager.Instance.PlayCutscene(cutsceneName);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        // Check if the object that exited the trigger is on the "Player" layer.
        if (other.gameObject.layer == LayerMask.NameToLayer("Enemy"))
        {
            Destroy(gameObject);
        }
    }
}
