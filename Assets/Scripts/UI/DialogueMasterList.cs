using System.Collections;
using System.Runtime.CompilerServices;
using UnityEngine;
using static UnityEngine.UIElements.UxmlAttributeDescription;

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

    public class ObjectiveMechanics_Cinematic
    {
        // Trigger: Player idle at game start or long idle -> encourage movement (W/A/S/D)
        public static Dialogue MOVE_CINEMATIC = new Dialogue(
            "MECHC_MOVE",
            "Guide",
            new string[]
            {
            "Step forward. The floor remembers every footfall.",
            "Move on... the hall will not open itself to stillness.",
            "Take a breath and shift your weight. The museum answers to motion."
            }
        );

        // Trigger: Player toggles slow-walk (Z) or enters fragile area -> gentle movement hint
        public static Dialogue WALK_CINEMATIC = new Dialogue(
            "MECHC_WALK",
            "Guide",
            new string[]
            {
            "Breathe and ease your step � we must not startle the frames.",
            "Slow your pace. Small sounds keep secrets safe.",
            "Tread lightly. Some memories crack under heavy feet."
            }
        );

        // Trigger: Player near an interactable (proximity) -> suggest interacting (E) but cinematic
        public static Dialogue INTERACT_CINEMATIC = new Dialogue(
            "MECHC_INTERACT",
            "Guide",
            new string[]
            {
            "Reach out. The canvas may return what it took.",
            "Touch it with care... it remembers her hands.",
            "Put your fingers near the surface. See if the memory stirs."
            }
        );

        // Trigger: Player pressed E but no effect -> tactical cinematic hint (follow trail)
        public static Dialogue NOTHING_MOVES_CINEMATIC = new Dialogue(
            "MECHC_NOTHING",
            "Guide",
            new string[]
            {
            "If the piece sleeps, follow where the paint led.",
            "No answer here... trace the marks and follow the trail.",
            "When a frame is silent, the path speaks. Follow it."
            }
        );

        // Trigger: Hide available or ghost near -> cinematic hide instruction (F)
        public static Dialogue HIDE_CINEMATIC = new Dialogue(
            "MECHC_HIDE",
            "Guide",
            new string[]
            {
            "Find shadow. Fold into it and hold your breath.",
            "Slip behind the darkness�stay still and let me work.",
            "Bury yourself in a corner of night. I will watch the light."
            }
        );

        // Trigger: Player presses Tab to switch to Guide control -> cinematic takeover
        public static Dialogue TAB_TAKEOVER_CINEMATIC = new Dialogue(
            "MECHC_TAB_TAKEOVER",
            "Guide",
            new string[]
            {
            "Hand me the moment. I will move where your feet cannot.",
            "Your hands rest; mine will weave through the frames.",
            "Stay safe in the dark. I will press on ahead."
            }
        );

        // Trigger: While _player hidden and Guide operating -> objective progress lines (Guide finishes mechanics)
        public static Dialogue GUIDE_OPERATE_CINEMATIC = new Dialogue(
            "MECHC_GUIDE_OPERATE",
            "Guide",
            new string[]
            {
            "A smear right there� I�ll pry it loose.",
            "I brush away the rust of memory... watch the frame soften.",
            "One stroke more and the piece relaxes. Stay hidden."
            }
        );

        // Trigger: Player returns control (Tab again) -> cinematic return
        public static Dialogue TAB_RETURN_CINEMATIC = new Dialogue(
            "MECHC_TAB_RETURN",
            "Guide",
            new string[]
            {
            "Your hands again. I will wait in the frame.",
            "I rest my bristles. Move when you are ready.",
            "Step out slowly... the air has changed while you were gone."
            }
        );

        // Trigger: Objective completed (one painting) -> cinematic confirmation
        public static Dialogue OBJECTIVE_DONE_CINEMATIC = new Dialogue(
            "MECHC_DONE",
            "Guide",
            new string[]
            {
            "One memory eased. The room exhales a little.",
            "It quiets... another piece lets go.",
            "We have unstitched a fragment. There are more to mend."
            }
        );

        // Trigger: All objectives complete -> cinematic final mechanics cue
        public static Dialogue ALL_DONE_CINEMATIC = new Dialogue(
            "MECHC_ALL_DONE",
            "Guide",
            new string[]
            {
            "All the frames pulse together. The path upward shines faintly.",
            "She grows quieter now. The last door waits upstairs.",
            "We have gathered her pieces... the way forward opens."
            }
        );

        // Trigger: Ghost proximity while _player hidden or moving -> cinematic warning
        public static Dialogue GHOST_NEAR_CINEMATIC = new Dialogue(
            "MECHC_GHOST_NEAR",
            "Guide",
            new string[]
            {
            "She breathes near. Flatten yourself against the dark.",
            "Hold very still... she remembers movement.",
            "Do not meet her face. Let the shadows hold you."
            }
        );

        // Trigger: Quick accessibility variant (optional) � still cinematic but includes key
        public static Dialogue ACCESSIBLE_CINEMATIC = new Dialogue(
            "MECHC_ACCESS",
            "Guide",
            new string[]
            {
            "To examine, press E � but move gently when you do.",
            "Press Tab to send me forward. Hide with F and I will continue.",
            "W/A/S/D to move; Z to walk. Tread lightly."
            }
        );
    }

    /* Suggested trigger mapping (implementation notes)
     - Idle at start or long idle -> MOVE_CINEMATIC
     - Enter room -> GO_TO_ROOM (use ObjectiveGuides_Controls.GO_TO_ROOM or similar cinematic line)
     - Near interactable -> INTERACT_CINEMATIC
     - E pressed but no result -> NOTHING_MOVES_CINEMATIC or FOLLOW_TRAIL_PROMPT
     - F available or ghost proximity -> HIDE_CINEMATIC
     - Tab pressed to switch to Guide -> TAB_TAKEOVER_CINEMATIC -> switch control -> GUIDE_OPERATE_CINEMATIC
     - Tab pressed to return -> TAB_RETURN_CINEMATIC
     - Objective complete -> OBJECTIVE_DONE_CINEMATIC
     - All objectives complete -> ALL_DONE_CINEMATIC
     - Ghost near -> GHOST_NEAR_CINEMATIC (priority over filler)
     - Use ACCESSIBLE_CINEMATIC when accessibility toggle enabled
    
    Screenshot reference(use in PR or paste):
     */ 


}
