using UnityEngine;

public class MainPainting : MonoBehaviour
{
    [SerializeField] private GameObject cover1;
    [SerializeField] private GameObject cover2;
    [SerializeField] private GameObject cover3;
    [SerializeField] private GameObject cover4;

    private PlayerChanneller playerChaneller;
    private TrailFollowDynamic trailFollow;

    private void Start()
    {
        // assign to fields (do not shadow)
        playerChaneller = FindFirstObjectByType<PlayerChanneller>();
        trailFollow = FindFirstObjectByType<TrailFollowDynamic>();
    }

    private void Update()
    {
        UpdatePaintingCovers();
        UpdateTrailTarget();
    }

    private void UpdatePaintingCovers()
    {
        if (playerChaneller == null)
            return;

        // Use explicit painting IDs
        if (cover1 != null) cover1.SetActive(!playerChaneller.HasCompletedPainting("paint1"));
        if (cover2 != null) cover2.SetActive(!playerChaneller.HasCompletedPainting("paint2"));
        if (cover3 != null) cover3.SetActive(!playerChaneller.HasCompletedPainting("paint3"));
        if (cover4 != null) cover4.SetActive(!playerChaneller.HasCompletedPainting("paint4"));
    }

    private void UpdateTrailTarget()
    {
        if (playerChaneller == null || trailFollow == null)
            return;

        if (playerChaneller.HasCompletedPainting("paint1")) trailFollow.ChangeTarget(3);
        else if (playerChaneller.HasCompletedPainting("paint2")) trailFollow.ChangeTarget(4);
        else if (playerChaneller.HasCompletedPainting("paint3")) trailFollow.ChangeTarget(5);
        else if (playerChaneller.HasCompletedPainting("paint4")) trailFollow.ChangeTarget(6);

    }

    // Returns true when all covers are inactive (painting fully revealed)
    public bool IsFullyRevealed()
    {
        return (cover1 != null && !cover1.activeSelf)
            && (cover2 != null && !cover2.activeSelf)
            && (cover3 != null && !cover3.activeSelf)
            && (cover4 != null && !cover4.activeSelf);
    }
}
