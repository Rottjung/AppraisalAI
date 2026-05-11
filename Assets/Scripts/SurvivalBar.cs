using UnityEngine;
using UnityEngine.UI;

public class SurvivalBar : MonoBehaviour
{
    [Header("UI")]
    public Image fill;

    public string stat;

    public bool reverse;

    [Header("Color Gradient")]
    public Gradient colorGradient;

    Sensors sensors;

    private void Awake()
    {
        if (!fill)
            fill = GetComponent<Image>();
        sensors = FindFirstObjectByType<Sensors>();
    }

    void Update()
    {
        sensors.TryGetSignal(stat, out Signal signal);

        fill.fillAmount = signal.Value;

        fill.color = colorGradient.Evaluate(reverse ? 1 - fill.fillAmount : fill.fillAmount);
    }
}
