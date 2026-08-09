using UnityEngine;
using UnityEngine.EventSystems;

public sealed class WorldMapStageHotspot : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private WorldMapScreen worldMap;
    [SerializeField] private int stageNumber = 1;

    public void Configure(WorldMapScreen screen, int number)
    {
        worldMap = screen;
        stageNumber = number;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        worldMap?.SelectAndEnterStage(stageNumber);
    }
}
