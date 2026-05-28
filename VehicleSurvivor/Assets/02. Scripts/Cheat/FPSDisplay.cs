using UnityEngine;

public class FPSDisplay : MonoBehaviour
{
    private float deltaTime = 0.0f;

    void Update()
    {
        // Time.timeScale의 영향을 받지 않도록 unscaledDeltaTime 사용
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
    }

    void OnGUI()
    {
        int w = Screen.width, h = Screen.height;

        GUIStyle style = new GUIStyle();
        Rect rect = new Rect(10, 10, w, h * 2 / 100);

        style.alignment = TextAnchor.UpperLeft;
        style.fontSize = h * 2 / 50; // 해상도에 맞춰 폰트 크기 조절

        // 프레임이 30 이하로 떨어지면 빨간색, 아니면 초록색으로 표시
        float fps = 1.0f / deltaTime;
        style.normal.textColor = fps < 30.0f ? Color.red : Color.green;

        float msec = deltaTime * 1000.0f;
        string text = string.Format("{0:0.0} ms ({1:0.} fps)", msec, fps);

        GUI.Label(rect, text, style);
    }
}