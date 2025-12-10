using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour {
    [SerializeField] public Text bulletAmountText;
    [SerializeField] public Image shotCoolTimeImage;

    /// <summary>
    /// ’e”‚ğXV
    /// </summary>
    public void UpdateBulletAmountText(int bulletAmount, int maxAmountText) {
        bulletAmountText.text = bulletAmount + " / " + maxAmountText;
    }

    /// <summary>
    /// ËŒ‚ŠÔŠu‚Ì‰æ‘œ
    /// </summary>
    public void UpdateShotCoolTimeImage(float fillAmount) {
        if(fillAmount >= 1) {
            shotCoolTimeImage.gameObject.SetActive(false);
        }
        else {
            if(!shotCoolTimeImage.gameObject.activeSelf) {
                shotCoolTimeImage.gameObject.SetActive(true);
            }
            shotCoolTimeImage.fillAmount = fillAmount;
        }
    }
}
