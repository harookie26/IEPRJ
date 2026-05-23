using UnityEngine;

public class MainPainting : MonoBehaviour
{
    [SerializeField] private GameObject cover1;
    [SerializeField] private GameObject cover2;
    [SerializeField] private GameObject cover3;
    [SerializeField] private GameObject cover4;

    [SerializeField] PaintbrushChanneller paintbrushChanneller;
    private TrailFollowDynamic trailFollow;

    public bool paint1Done = false;
    public bool paint2Done = false;
    public bool paint3Done = false;
    public bool paint4Done = false;

    private void Start()
    {
        // assign to fields (do not shadow)
        if (paintbrushChanneller == null)
        {
            paintbrushChanneller = GameObject.FindGameObjectWithTag("Player")?.GetComponent<PaintbrushChanneller>();
        }

        trailFollow = FindFirstObjectByType<TrailFollowDynamic>();
    }

    private void Update()
    {
        SetProgress();
        UpdatePaintingCovers();
        UpdateTrailTarget();
    }

    private void UpdatePaintingCovers()
    {
        if (paintbrushChanneller == null)
            return;

        // Use explicit painting IDs
        if (cover1 != null) cover1.SetActive(!paint1Done);
        if (cover2 != null) cover2.SetActive(!paint2Done);
        if (cover3 != null) cover3.SetActive(!paint3Done);
        if (cover4 != null) cover4.SetActive(!paint4Done);

        //Debug.Log("MainPainting.UpdatePaintingCovers: Updating cover states.");
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

    private void SetProgress()
    {
        if (paintbrushChanneller.HasCompletedPainting("paint1"))
            paint1Done = true;
        if (paintbrushChanneller.HasCompletedPainting("paint2"))
            paint2Done = true;
        if (paintbrushChanneller.HasCompletedPainting("paint3"))
            paint3Done = true;
        if (paintbrushChanneller.HasCompletedPainting("paint4"))
            paint4Done = true;
    }
}
