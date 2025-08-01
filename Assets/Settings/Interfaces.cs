using System.Collections.Generic;
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
        bool CanChannel(GameObject player);
        void StartChannel(GameObject player);
        void StopChannel();
        void ChannelTick(float deltaTime);
        bool IsChanneling { get; }
        float Progress { get; }
        float ProgressMax { get; }
        bool IsComplete { get; }
    }

    public interface ILinkable
    {
        string LinkID { get; }
        string UniqueID { get; }
    }
}

namespace Game.Level
{
    public interface IRoom
    {
        int Id { get; }
        Vector2 Center { get; }
        IEnumerable<IDoor> ConnectedDoors { get; }

        Bounds Bounds { get; }
    }

    public interface IDoor
    {
        int Id { get; }
        IRoom RoomA { get; }
        IRoom RoomB { get; }
        float Weight { get; }
    }

}