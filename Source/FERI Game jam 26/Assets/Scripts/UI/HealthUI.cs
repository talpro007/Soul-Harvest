using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class HealthUI : MonoBehaviour
{
    [SerializeField] private Image heartPrefab;
    [SerializeField] private Sprite fullHeart;
    [SerializeField] private Sprite emptyHeart;

    private List<Image> hearts = new List<Image>();

    public void SetMaxHealth(int max)
    {
        foreach (Image im in hearts) { Destroy(im.gameObject); }

        hearts.Clear();

        for(int i=0; i<max; i++)
        {
            hearts.Add(Instantiate(heartPrefab, transform));
        }

    }

    public void SetHealth(int current)
    {
        for( int i=0; i<hearts.Count; i++)
        {
            hearts[i].sprite = i < current ? fullHeart : emptyHeart;
        }
    }
}
