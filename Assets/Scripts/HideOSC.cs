using UnityEngine;

public class HideOSC : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
    #if UNITY_ANDROID || UNITY_IOS
        gameObject.SetActive(true);
    #else
        gameObject.SetActive(false);
    #endif
    }
}
