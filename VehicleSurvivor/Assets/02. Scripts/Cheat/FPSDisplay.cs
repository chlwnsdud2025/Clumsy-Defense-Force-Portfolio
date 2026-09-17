using UnityEngine;

public class FPSDisplay : MonoBehaviour
{
    private float deltaTime = 0.0f;
    private GUIStyle style;
    private string text = "";
    private float textTimer;

    void Update()
    {
        // Time.timeScale의 영향을 받지 않도록 unscaledDeltaTime 사용
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;

        // 문자열은 0.25초마다만 갱신 (매 프레임 할당 방지 — 벤치마크 GC 지표 오염 방지)
        textTimer += Time.unscaledDeltaTime;
        if (textTimer < 0.25f) return;
        textTimer = 0f;

        float fps = 1.0f / deltaTime;
        float msec = deltaTime * 1000.0f;
        text = string.Format("{0:0.0} ms ({1:0.} fps)", msec, fps);

        // 활성 몹 수 (EnemyManager가 있는 씬에서만)
        if (EnemyManager.Instance != null)
            text += string.Format("   |   Enemies {0}", EnemyManager.Instance.ActiveEnemies.Count);
    }

    void OnGUI()
    {
        int w = Screen.width, h = Screen.height;

        if (style == null)
        {
            style = new GUIStyle();
            style.alignment = TextAnchor.UpperLeft;
        }
        style.fontSize = h * 2 / 50; // 해상도에 맞춰 폰트 크기 조절

        // 프레임이 30 이하로 떨어지면 빨간색, 아니면 초록색으로 표시
        float fps = 1.0f / deltaTime;
        style.normal.textColor = fps < 30.0f ? Color.red : Color.green;

        GUI.Label(new Rect(10, 10, w, h * 2 / 100), text, style);
    }
}
