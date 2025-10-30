using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SettingsManager : MonoBehaviour
{
    public Button map1button;
    private string selectedMap;

     void  Start()
    {
       // map1button.onClick.AddListener(() => selectedMap("Map1"));
    }

    private void selectMap(string mapName)
    {
        selectedMap = mapName;
    }

    public void LoadSelectedMap()
    {
        SceneManager.LoadScene(selectedMap);
    }
}
