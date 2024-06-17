using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


public class ScreenshotEditorWindow : EditorWindow
{
    private int screenShotFactor = 1;

    [MenuItem("ECS Forests/Screenshot Tool")]
    public static void ShowExample()
    {
        ScreenshotEditorWindow wnd = GetWindow<ScreenshotEditorWindow>();
        wnd.titleContent = new GUIContent("Screenshot Tool");
    }

    public void OnGUI()
    {
        //27552
        //102030
        //134469

        EditorGUILayout.LabelField("Upscale factor");
        screenShotFactor = EditorGUILayout.IntSlider(screenShotFactor, 1, 4);

        EditorGUILayout.LabelField("Path to save to");
        string path = EditorGUILayout.TextField("Assets/Data");

        if (GUILayout.Button(new GUIContent("Screenshot")))
        {
            if (!Application.isPlaying)
                return;

            string fileName = "screen-" + GUID.Generate().ToString() + ".jpg";
            ScreenCapture.CaptureScreenshot(path + "/" + fileName, screenShotFactor);
        }
    }
}
