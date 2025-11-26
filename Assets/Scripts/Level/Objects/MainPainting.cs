using UnityEngine;

public class MainPainting : MonoBehaviour
{
    [SerializeField] private GameObject cover1;
    [SerializeField] private GameObject cover2;
    [SerializeField] private GameObject cover3;
    [SerializeField] private GameObject cover4;

    private void Update()
    {
        UpdatePaintingCovers();
    }

    private void UpdatePaintingCovers()
    {
        var playerChaneller = FindFirstObjectByType<PlayerChanneller>();
        if (playerChaneller == null) return;

        // Use explicit painting IDs
        cover1.SetActive(!playerChaneller.HasCompletedPainting("paint1"));
        cover2.SetActive(!playerChaneller.HasCompletedPainting("paint2"));
        cover3.SetActive(!playerChaneller.HasCompletedPainting("paint3"));
        cover4.SetActive(!playerChaneller.HasCompletedPainting("paint4"));
    }
}
