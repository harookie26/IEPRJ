using UnityEngine;

public class EnableOnDestroy : MonoBehaviour
{
    [SerializeField] private GameObject toBeDestroyed;
    [SerializeField] private GameObject toBeEnabled;

    private void Update()
    {
        if (toBeDestroyed == null)
        {
            if (toBeEnabled != null)
            {
                toBeEnabled.SetActive(true);
            }
        }
    }
}
