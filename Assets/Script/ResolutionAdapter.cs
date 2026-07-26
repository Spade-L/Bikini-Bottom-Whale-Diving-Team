using UnityEngine;

public class ResolutionAdapter : MonoBehaviour 
{
    void Start()
    {
        // 将宽度和高度替换为游戏需要的特殊分辨率
        int width = 1890; 
        int height = 1417; 
        
        // 设置游戏窗口分辨率为指定尺寸，布尔值表示是否全屏
        Screen.SetResolution(width, height, false); 
    }
}