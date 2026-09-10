using UnityEngine;

// 定义 ResolutionAdapter 类型
public class ResolutionAdapter : MonoBehaviour
{
// 定义 Start 方法
    private void Start()
    {
// 判断当前条件
        if (SettingsManager.Instance != null)
        {
// 调用 ApplyDisplaySettings
            SettingsManager.Instance.ApplyDisplaySettings();
        }
    }
}
