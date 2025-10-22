using UnityEngine;

public class ToysInScene : MonoBehaviour
{
    public GameObject[] Toys = new GameObject[6];
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void OnEnable() {
        for (int i = 0; i < Toys.Length; i++)
        {
            Toys[i].SetActive(PlayerPrefs.GetInt(PlayerPrefKeys.toyPrefix + i, i == 0 ? 1 : 0) == 1);
        }
    }
}
