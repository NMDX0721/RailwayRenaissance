using System;
using UnityEngine;

[Serializable]
public struct VNExitData
{
    public float startMoney;
    public int startTrust;
    public int startTrainCondition;
    public CrewData[] crew;
    public string[] completedFlags;
    public string[] unlockedRegions;
    public string difficulty;
    public string playerAlias;
}

[Serializable]
public struct CrewData
{
    public string id;
    public string name;
    public string role;
    public int skillLevel;
    public float fatigue;
    public string specialty;
}