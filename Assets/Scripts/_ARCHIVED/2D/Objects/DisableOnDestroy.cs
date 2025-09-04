using UnityEngine;

public class DisableOnDestroy : MonoBehaviour
{
    [SerializeField] private GameObject willBeDestroyed;
    [SerializeField] private GameObject toBeDisabled;

    private void Update()
    {
        if (willBeDestroyed == null)
        {
            if (toBeDisabled != null)
            {
                Destroy(toBeDisabled);
            }
        }
    }
}
