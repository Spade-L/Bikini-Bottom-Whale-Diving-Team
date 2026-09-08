using UnityEngine;

public class ResolutionAdapter : MonoBehaviour 
{
    // Start 在场景首帧设置窗口；切换场景或其他脚本可随后覆盖此设置
    void Start()
    {
        int width = 1890; 
        int height = 1417; 
        
        Screen.SetResolution(width, height, false); 
    }
}
