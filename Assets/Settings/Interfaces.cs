using UnityEngine;

namespace Game.ObjectTypes
{
    public interface IHidable
    {
        bool IsAvailable { get; }
        bool IsPlayerHiding { get; }
        void EnterHiding(GameObject player);
        void ExitHiding();
        float CooldownRemaining { get; }
    }

    public interface IChannelable
    {
        // Called once when channeling begins (key held past threshold).
        void StartChannel();

        // Called once when channeling ends (key released).
        void StopChannel();
    }

    public interface ILinkable
    {
        string LinkID { get; }
        string UniqueID { get; }
    }

    public interface IInteractable
    {
        void Interact();
    }

    public interface ICollectible
    {
        void Collect();
        string GetID { get; }
        void Levitate();
    }
}

namespace Game.Level
{
    public interface IRoom
    {
        int Id { get; }
        Vector3 Center { get; }
        //IEnumerable<IDoor> ConnectedDoors { get; }

        Bounds Bounds { get; }
    }

    public interface IStair
    {
        int Id { get; }
        IRoom RoomA { get; }
        IRoom RoomB { get; }
        float Weight { get; }
    }

}

namespace Game.States
{
    public interface IPlayerState
    {
        void Enter();
        void Exit();
        void HandleInput();
        void Tick();
    }

    public interface IPaintbrushState
    {
        void Enter();
        void Exit();
        void HandleInput();
        void Tick();
    }

}