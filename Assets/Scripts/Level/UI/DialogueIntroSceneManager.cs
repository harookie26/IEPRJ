using DG.Tweening;
using Level.UI;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class DialogueIntroSceneManager : MonoBehaviour
{
    [SerializeField] private List<DialogueEntry> introDialogues;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        DialogueManager.Instance.DisplaySequence(introDialogues);
    }

    // Update is called once per frame
    void Update()
    {

    }
}
