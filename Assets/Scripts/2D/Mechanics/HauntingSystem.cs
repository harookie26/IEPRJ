using UnityEngine;
using Game.ObjectTypes;

public class HauntingSystem
{
    private readonly GameObject player;
    private readonly GameObject ghost;
    private readonly float hauntingDistance;
    private readonly float hiddenHauntingDistance;
    private readonly float maxSanity;

    private IHidable currentHidingSpot;

    public float CurrentSanity { get; private set; }
    public float MaxSanity => maxSanity;

    public HauntingSystem(GameObject player, GameObject ghost, float hauntingDistance, float hiddenHauntingDistance, float hauntingTick, float maxSanity)
    {
        this.player = player;
        this.ghost = ghost;
        this.hauntingDistance = hauntingDistance;
        this.hiddenHauntingDistance = hiddenHauntingDistance;
        this.maxSanity = maxSanity;
        this.CurrentSanity = 0f;
    }

    public void SetHidingSpot(IHidable hidingSpot)
    {
        currentHidingSpot = hidingSpot;
    }

    public void ClearHidingSpot()
    {
        currentHidingSpot = null;
    }

    public bool IsHaunted()
    {
        if (currentHidingSpot != null && currentHidingSpot.IsPlayerHiding)
        {
            return IsHidingPlayerHaunted();
        }
        else
        {
            return IsPlayerHaunted();
        }
    }

    private bool IsHidingPlayerHaunted()
    {
        if (currentHidingSpot is MonoBehaviour hidingMono)
        {
            Vector2 hidingPos = new Vector2(hidingMono.transform.position.x, hidingMono.transform.position.y);
            Vector2 ghostPos = new Vector2(ghost.transform.position.x, ghost.transform.position.y);
            float distance = Vector2.Distance(hidingPos, ghostPos);
            return distance <= hiddenHauntingDistance;
        }
        return false;
    }

    private bool IsPlayerHaunted()
    {
        Vector2 playerPos = new Vector2(player.transform.position.x, player.transform.position.y);
        Vector2 ghostPos = new Vector2(ghost.transform.position.x, ghost.transform.position.y);
        float distance = Vector2.Distance(playerPos, ghostPos);
        return distance <= hauntingDistance;
    }

    public bool UpdateSanity()
    {
        float distance;
        float maxDistance;

        if (currentHidingSpot != null && currentHidingSpot.IsPlayerHiding && currentHidingSpot is MonoBehaviour hidingMono)
        {
            Vector2 hidingPos = new Vector2(hidingMono.transform.position.x, hidingMono.transform.position.y);
            Vector2 ghostPos = new Vector2(ghost.transform.position.x, ghost.transform.position.y);
            distance = Vector2.Distance(hidingPos, ghostPos);
            maxDistance = hiddenHauntingDistance;
        }
        else
        {
            Vector2 playerPos = new Vector2(player.transform.position.x, player.transform.position.y);
            Vector2 ghostPos = new Vector2(ghost.transform.position.x, ghost.transform.position.y);
            distance = Vector2.Distance(playerPos, ghostPos);
            maxDistance = hauntingDistance;
        }

        if (distance > maxDistance)
        {
            CurrentSanity = 0f;
        }
        else
        {
            float t = 1f - Mathf.Clamp01(distance / maxDistance);
            CurrentSanity = Mathf.Lerp(0f, maxSanity, t);
        }

        return CurrentSanity >= maxSanity;
    }
}