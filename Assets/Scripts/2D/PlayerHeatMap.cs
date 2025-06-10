using UnityEngine;

public class PlayerHeatMap : MonoBehaviour
{
    public Sprite heatMapSprite;
    [SerializeField] private Color heatMapColor = new Color(1f, 0.5f, 0f, 0.5f);
    private float heatMapRadius = 2f;

    private GameObject heatMapObject;
    private SpriteRenderer heatMapRenderer;

    [SerializeField] private float minRadius = 2f;
    [SerializeField] private float maxRadius = 10f;
    [SerializeField] private float growSpeed = 0.25f;

    private float currentRadius;
    private bool isGrowing = false;


    void Start()
    {
        // Create a child GameObject for the heat map
        heatMapObject = new GameObject("HeatMapVisual");
        heatMapObject.transform.SetParent(transform);
        heatMapObject.transform.localPosition = Vector3.zero;

        // Add SpriteRenderer and configure it
        heatMapRenderer = heatMapObject.AddComponent<SpriteRenderer>();
        heatMapRenderer.sprite = heatMapSprite;
        heatMapRenderer.color = heatMapColor;
        heatMapRenderer.sortingOrder = 1;

        // Set the scale based on the desired radius
        float spriteDiameter = heatMapRenderer.sprite.bounds.size.x;
        float scale = (heatMapRadius * 2) / spriteDiameter;
        heatMapObject.transform.localScale = new Vector3(scale, scale, 1f);

        currentRadius = minRadius;
        SetRadius(currentRadius);
        // Start hidden
        heatMapObject.SetActive(false);
    }

    void Update()
    {
        if (isGrowing)
        {
            SetRadius(currentRadius + growSpeed * Time.deltaTime);
        }
    }

    // Call this to show or hide the heat map
    public void SetHeatMapVisible(bool visible)
    {
        Debug.Log("SetHeatMapVisible called with: " + visible);
        if (heatMapObject != null)
            heatMapObject.SetActive(visible);
    }

    private void SetRadius(float radius)
    {
        currentRadius = Mathf.Clamp(radius, minRadius, maxRadius);
        if (heatMapRenderer != null && heatMapRenderer.sprite != null)
        {
            float spriteDiameter = heatMapRenderer.sprite.bounds.size.x;
            float scale = (currentRadius * 2) / spriteDiameter;
            heatMapObject.transform.localScale = new Vector3(scale, scale, 1f);
        }
    }

    public void StartGrowing()
    {
        isGrowing = true;
    }

    public void StopGrowingAndReset()
    {
        isGrowing = false;
        SetRadius(minRadius);
    }


}
