using UnityEngine;

public class ResolutionAdapter : MonoBehaviour 
{
    // Start 在场景首帧设置窗口；切换场景或其他脚本可随后覆盖此设置。
    void Start()
    {
        // 将宽度和高度替换为游戏需要的特殊分辨率
        // 该尺寸为硬编码运行时配置，应与目标平台和 UI 适配策略保持一致。
        int width = 1890; 
        int height = 1417; 
        
        // 设置游戏窗口分辨率为指定尺寸，布尔值表示是否全屏
        // false 请求窗口模式；平台、全屏设置或显示器限制可能调整最终实际分辨率。
        Screen.SetResolution(width, height, false); 
    }
}