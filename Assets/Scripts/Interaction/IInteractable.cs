using UnityEngine;

public interface IInteractable
{
    void OnInteractStart();  // Called when player first looks at object
    void OnInteractEnd();    // Called when player looks away
    void OnInteractTrigger(); // Called when player presses the interact button
    GameObject GetPopupPrefab(); // Returns the popup prefab for this interactable
    string GetUniqueID(); 
}
