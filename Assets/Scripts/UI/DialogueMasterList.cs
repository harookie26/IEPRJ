using UnityEngine;
using System.Collections;
using System.Runtime.CompilerServices;

public class Dialogue
{
    public string dialogueID;  // ID verification

    /// <summary>
    /// Still Subject to change
    /// </summary>
    public string characterName; // Text for the Title Bar
    [TextArea] public string[] dialogueLines; // Dialogue lines

    public Dialogue(string id, string name, string[] lines)
    {
        dialogueID = id;
        characterName = name;
        dialogueLines = lines;
    }
}

public class DialogueMasterList
{
    public class Tutorial /// Temp Class Group to categorize the dialogue type / location.
    {
        public static Dialogue Intro_1 = new Dialogue(
            "TUTORIAL_INTRO_1",
            "Guide",
            new string[]
            {
                "Welcome to the game!",
                "Use WASD to move around.",
                "Press E to interact with objects."
            }
        );
    }




}
