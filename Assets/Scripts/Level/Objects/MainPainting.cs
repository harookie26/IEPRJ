using UnityEngine;

public class MainPainting : MonoBehaviour
{
    [SerializeField] private GameObject cover1;
    [SerializeField] private GameObject cover2;
    [SerializeField] private GameObject cover3;
    [SerializeField] private GameObject cover4;

    private PaintbrushChanneller paintbrushChanneller;
    private TrailFollowDynamic trailFollow;

    private void Start()
    {
        // assign to fields (do not shadow)
        paintbrushChanneller = FindFirstObjectByType<PaintbrushChanneller>();
        trailFollow = FindFirstObjectByType<TrailFollowDynamic>();
    }

    private void Update()
    {
        UpdatePaintingCovers();
        UpdateTrailTarget();
    }

    private void UpdatePaintingCovers()
    {
        if (paintbrushChanneller == null)
            return;

        // Use explicit painting IDs
        if (cover1 != null) cover1.SetActive(!paintbrushChanneller.HasCompletedPainting("paint1"));
        if (cover2 != null) cover2.SetActive(!paintbrushChanneller.HasCompletedPainting("paint2"));
        if (cover3 != null) cover3.SetActive(!paintbrushChanneller.HasCompletedPainting("paint3"));
        if (cover4 != null) cover4.SetActive(!paintbrushChanneller.HasCompletedPainting("paint4"));
    }

    private void UpdateTrailTarget()
    {
        if (paintbrushChanneller == null || trailFollow == null)
            return;

        if (paintbrushChanneller.HasCompletedPainting("paint1")) trailFollow.ChangeTarget(3);
        else if (paintbrushChanneller.HasCompletedPainting("paint2")) trailFollow.ChangeTarget(4);
        else if (paintbrushChanneller.HasCompletedPainting("paint3")) trailFollow.ChangeTarget(5);
        else if (paintbrushChanneller.HasCompletedPainting("paint4")) trailFollow.ChangeTarget(6);

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
