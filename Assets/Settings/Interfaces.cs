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
}