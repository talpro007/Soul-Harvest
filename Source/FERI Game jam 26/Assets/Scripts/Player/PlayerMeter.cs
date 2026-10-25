using UnityEngine;

public class PlayerMeter : MonoBehaviour
{
    
    [SerializeField] private MeterUI meterUI;
    [SerializeField] private int maxMeterLevel = 4;

    private int currentMeterLevel = 0;

    void Start()
    {
        meterUI.SetMeter(currentMeterLevel);
    }

    public void AddMeter()
    {
        currentMeterLevel = Mathf.Clamp(currentMeterLevel + 1, 0, maxMeterLevel);
        meterUI.SetMeter(currentMeterLevel);
    }

    public bool TrySpend(int amount)
    {
        if(currentMeterLevel < amount) { return false; }

        currentMeterLevel -= amount;
        meterUI.SetMeter(currentMeterLevel);
        return true;
    }
}
