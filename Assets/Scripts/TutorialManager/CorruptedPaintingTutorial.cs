using UnityEngine;

public class CorruptedPaintingTutorial : MonoBehaviour
{
    [SerializeField] private PaintbrushChanneller paintbrushChanneller;
    [SerializeField] private GameObject tutorialCorruptedPaintingObject;
    [SerializeField] private GameObject coverObject;

    // We use this toggle to ensure the code only ever runs ONCE
    private bool isResolved = false;

    // We delete Start() completely and use Update() instead
    void Update()
    {
        // 1. If we already fixed the painting, stop checking forever.
        if (isResolved) return;

        // 2. We wait patiently. The EXACT frame this becomes true 
        // (either from a save loading OR live gameplay), the code runs!
        if (paintbrushChanneller != null && paintbrushChanneller.HasCompletedPainting("000"))
        {
            if (coverObject != null) coverObject.SetActive(false);

            tutorialCorruptedPaintingObject.tag = "Untagged";
            tutorialCorruptedPaintingObject.layer = 0;

            // 3. Lock the script so it never wastes processing power checking again
            isResolved = true;
        }
    }

    public void ApplyCheckpointPhase(int phase)
    {
        bool completed = phase >= 5;

        if (completed)
        {
            if (coverObject != null)
                coverObject.SetActive(false);

            tutorialCorruptedPaintingObject.tag = "Untagged";
            tutorialCorruptedPaintingObject.layer = 0;
        }
        else
        {
            if (coverObject != null)
                coverObject.SetActive(true);

            tutorialCorruptedPaintingObject.tag = "ChannelablePainting";

            int layer =
                LayerMask.NameToLayer("Chanellable");

            if (layer != -1)
                tutorialCorruptedPaintingObject.layer = layer;
        }
    }
}