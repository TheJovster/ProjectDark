using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class SpawnArea
{
    public Vector3 areaSize = new Vector3(10, 0, 10);
    public Vector3 areaOffset = Vector3.zero;
    public int maxObjectsToSpawn = 20;
}
