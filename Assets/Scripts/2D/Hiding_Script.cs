using UnityEngine;

public class Hiding_Script : MonoBehaviour
{

    GameObject protagonist = GameObject.FindGameObjectWithTag("Player");
    [SerializeField] private float timer;
    private bool isUsed;



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        isUsed = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (isUsed == true)
        {
            timer -= Time.deltaTime;

            if (timer <= 0)
            {
                protagonist.SetActive(true);
                Destroy(this);
            }
        }
    }

    void OnHiding()
    {
        protagonist.SetActive(false);
        isUsed = true;
    }
}
