using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ExploadBehaviour : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
#if !UNITY_EDITOR
        GameObject canvas = new GameObject("ExploadCanvas");
        canvas.AddComponent<Canvas>();
        canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.GetComponent<Canvas>().sortingOrder = 99;
        DontDestroyOnLoad(canvas);
        GameObject raw_image = new GameObject("OverlayImage");
        raw_image.transform.parent = canvas.transform;
        raw_image.AddComponent<RawImage>();
        
        raw_image.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);
        raw_image.GetComponent<RectTransform>().anchorMin = new Vector2(0, 0);
        raw_image.GetComponent<RectTransform>().anchorMax = new Vector2(1, 1);
        raw_image.GetComponent<RectTransform>().sizeDelta = new Vector2(Screen.width, Screen.height);
        raw_image.GetComponent<RectTransform>().offsetMin = new Vector2(0, 0);
        raw_image.GetComponent<RectTransform>().offsetMax = new Vector2(0, 0);
        raw_image.GetComponent<RawImage>().color = new Color(1, 1, 1, 0.5f);
        raw_image.AddComponent<Expload.OffscreenCEF>();
#endif
    }

    // Update is called once per frame
    void Update()
    {

    }
}
