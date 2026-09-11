using UnityEngine;

// 在启动时同步分辨率相关显示设置
public class ResolutionAdapter : MonoBehaviour
{
// 读取初始依赖并同步首帧状态
    private void Start()
    {
// 检查 Start 的前置条件
        if (SettingsManager.Instance != null)
        {
// 使用 Start 所需功能
            SettingsManager.Instance.ApplyDisplaySettings();
        }
    }
}
