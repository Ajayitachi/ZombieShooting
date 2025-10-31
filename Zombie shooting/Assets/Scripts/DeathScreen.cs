using UnityEngine;
using UnityEngine.UI;

public class DeathScreen : MonoBehaviour
{
    public Image targetImage;
    public Text TargetText;
    public float duration = 5f;
    public bool showDeadScreen = false;
    private float targetAplha = 1f;
    private float startAlpha;
    private float elepsedTime =0f;

     void  Start()
    {
        startAlpha = targetImage.color.a;
       
    }
    void  Update()
    {
      if (showDeadScreen)
        {
            if (elepsedTime < duration)
            {
                float newAlpha = Mathf.Lerp(startAlpha, targetAplha, elepsedTime / duration);
                Color newColor = targetImage.color;
                newColor.a = newAlpha;
                targetImage.color = newColor;

                Color newTextAlpha = TargetText.color;
                newTextAlpha.a = newAlpha;
                TargetText.color = newTextAlpha;
                elepsedTime += Time.deltaTime;
            } 
            else
            {
                Time.timeScale = 0f;
            }

        } 
    }
}
