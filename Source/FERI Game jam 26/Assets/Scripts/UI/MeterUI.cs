using UnityEngine;
using UnityEngine.UI;

public class MeterUI : MonoBehaviour
{
    [SerializeField] private Image meterImage;
    [SerializeField] private Sprite[] meterSprites;

    public void SetMeter(int level){
        Debug.Log(level);
        level = Mathf.Clamp(level, 0, meterSprites.Length - 1);
        meterImage.sprite = meterSprites[level];
    }
}
