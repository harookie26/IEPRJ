using UnityEngine;
using System.Collections;


public class EventNames {
	public const string ON_UPDATE_SCORE = "ON_UPDATE_SCORE";
	public const string ON_CORRECT_MATCH = "ON_CORRECT_MATCH";
	public const string ON_WRONG_MATCH = "ON_WRONG_MATCH";
	public const string ON_INCREASE_LEVEL = "ON_INCREASE_LEVEL";

	public const string ON_PICTURE_CLICKED = "ON_PICTURE_CLICKED";


	public class GameStateEvents
	{
		public const string ON_GAME_START = "ON_GAME_START";
		public const string ON_GAME_END = "ON_GAME_END";
		public const string ON_GAME_PAUSE = "ON_GAME_PAUSE";
		public const string ON_GAME_RESUME = "ON_GAME_RESUME";
		public const string ON_GAME_RESTART = "ON_GAME_RESTART";
		public const string ON_LEVEL_COMPLETE = "ON_LEVEL_COMPLETE";
		public const string ON_LEVEL_FAILED = "ON_LEVEL_FAILED";
    }

	public class SceneEvents
	{
		public const string ON_SCENE_LOAD = "ON_SCENE_LOAD";
		public const string ON_SCENE_UNLOAD = "ON_SCENE_UNLOAD";
		public const string ON_SCENE_SWITCH = "ON_SCENE_SWITCH";
		public const string ON_SCENE_RELOAD = "ON_SCENE_RELOAD";
    }
    public static class PlayerEvents
    {
        public const string PLAYER_STARTED_SPRINT = "PLAYER_STARTED_SPRINT";
        public const string PLAYER_STOPPED_SPRINT = "PLAYER_STOPPED_SPRINT";
        public const string PLAYER_HID = "PLAYER_HID";
        public const string PLAYER_REVEALED = "PLAYER_REVEALED";
		public const string PLAYER_CHANNELING = "PLAYER_CHANNELING";
		public const string PLAYER_DECHANNELING = "PLAYER_DECHANNELING";
    }

    public class EnemyEvents
	{
        public const string ENEMY_SPOTTED_PLAYER = "ENEMY_SPOTTED_PLAYER";
        public const string ENEMY_LOST_PLAYER = "ENEMY_LOST_PLAYER";
        public const string ENEMY_SEARCHING = "ENEMY_SEARCHING";
        public const string ENEMY_PATROLLING = "ENEMY_PATROLLING";
		public const string ENEMY_CHASING = "ENEMY_CHASING";
    }

    public static class UIEvents
    {
        public const string HOVER_UI_SHOWN = "HOVER_UI_SHOWN";
        public const string HOVER_UI_HIDDEN = "HOVER_UI_HIDDEN";
    }

	public static class ObjectEvents
	{
		public const string OBJECT_HIDING_COOLDOWN = "OBJECT_HIDING_COOLDOWN";
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













