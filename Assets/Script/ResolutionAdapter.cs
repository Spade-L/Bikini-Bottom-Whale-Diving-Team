using UnityEngine;

public class ResolutionAdapter : MonoBehaviour
{
    private void Start()
    {
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.ApplyDisplaySettings();
        }
    }
}
