using System;
using UnityEngine;

public class DummyScript : MonoBehaviour
{
    public Action onPress;

    private void Start()
    {
        GetComponent<KMSelectable>().Children[0].OnInteract += () => { if(onPress != null) onPress(); return false; };
    }
}
