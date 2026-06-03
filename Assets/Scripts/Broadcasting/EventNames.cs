using UnityEngine;
using System.Collections;
using System.Runtime.CompilerServices;
using UnityEngine.UnityConsent;


public class EventNames {
	public class GameStateEvents
	{
		public const string ON_GAME_START = "ON_GAME_START";
		public const string ON_GAME_END = "ON_GAME_END";
		public const string ON_GAME_PAUSE = "ON_GAME_PAUSE";
		public const string ON_GAME_RESUME = "ON_GAME_RESUME";
		public const string ON_GAME_RESTART = "ON_GAME_RESTART";
		public const string ON_LEVEL_COMPLETE = "ON_LEVEL_COMPLETE";
		public const string ON_LEVEL_FAILED = "ON_LEVEL_FAILED";
		public const string ON_LEVEL_RELOAD = "ON_LEVEL_RELOAD";

		public const string ON_DEBUG_MODE_ON = "ON_DEBUG_MODE_ON";
		public const string ON_DEBUG_MODE_OFF = "ON_DEBUG_MODE_OFF";
    }

	public class SceneEvents
	{
		public const string ON_SCENE_LOAD = "ON_SCENE_LOAD";
		public const string ON_SCENE_UNLOAD = "ON_SCENE_UNLOAD";
		public const string ON_SCENE_SWITCH = "ON_SCENE_SWITCH";
		public const string ON_SCENE_RELOAD = "ON_SCENE_RELOAD";
    }

	public class LevelEvents
	{
		public const string ON_CORRUPTED_ROOM_TRUE = "ON_CORRUPTED_ROOM_TRUE";
		public const string ON_CORRUPTED_ROOM_FALSE = "ON_CORRUPTED_ROOM_FALSE";

    }

    public static class HintEvents
    {
        public const string HINT1_START = "HINT1_START";
        public const string HINT1_END = "HINT1_END";
        public const string HINT2_START = "HINT2_START";
        public const string HINT2_END = "HINT2_END";
        public const string HINT_FIND_FLASHLIGHT_START = "HINT_FIND_FLASHLIGHT_START";
        public const string HINT3_START = "HINT3_START";
        public const string HINT3_END = "HINT3_END";

        public const string HINT_PAINTING_START = "HINT_PAINTING_START";
        public const string HINT_PAINTING_END = "HINT_PAINTING_END";

        public const string HINT4_START = "HINT4_START";
        public const string HINT4_END = "HINT4_END";
        public const string HINT5_START = "HINT5_START";
        public const string HINT5_END = "HINT5_END";

        public const string ADD_PAINTING_RESTORED = "ADD_PAINTING_RESTORED";

        public const string OPEN_HINT = "OPEN_HINT";
        public const string CLOSE_HINT = "CLOSE_HINT";
    }

    public static class PlayerEvents
    {
		public const string PLAYER_MOVED = "PLAYER_MOVED";
		public const string PLAYER_STOPPED = "PLAYER_STOPPED";
        public const string PLAYER_STARTED_SPRINT = "PLAYER_STARTED_SPRINT";
        public const string PLAYER_STOPPED_SPRINT = "PLAYER_STOPPED_SPRINT";
        public const string PLAYER_HID = "PLAYER_HID";
        public const string PLAYER_REVEALED = "PLAYER_REVEALED";
    }

    public class EnemyEvents
	{
        public const string ENEMY_SPOTTED_PLAYER = "ENEMY_SPOTTED_PLAYER";
        public const string ENEMY_LOST_PLAYER = "ENEMY_LOST_PLAYER";
        public const string ENEMY_SEARCHING = "ENEMY_SEARCHING";
		public const string ENEMY_CATCHED = "ENEMY_CATCHED";
    }

    public static class UIEvents
    {
        public const string HOVER_UI_SHOWN = "HOVER_UI_SHOWN";
        public const string HOVER_UI_HIDDEN = "HOVER_UI_HIDDEN";

		public const string PLAY_DIALOGUE_START = "PLAY_DIALOGUE_START";
		public const string PLAY_DIALOGUE_END = "PLAY_DIALOGUE_END";
    }

	public static class CutsceneEvents
	{
		public const string CUTSCENE_START = "CUTSCENE_START";
		public const string CUTSCENE_END = "CUTSCENE_END";
		public const string CUTSCENE_PAUSE = "CUTSCENE_PAUSE";
		public const string CUTSCENE_RESUME = "CUTSCENE_RESUME";
		public const string CUTSCENE_RESTART = "CUTSCENE_RESTART";
    }

    public static class CameraEvents
    {
        public const string CAMERA_SHAKE = "CAMERA_SHAKE";
    }

}













