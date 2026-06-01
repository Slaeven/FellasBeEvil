using TMPro;
using UnityEngine;

public class AmmoUI : MonoBehaviour
{
    [SerializeField] private WeaponBase weapon;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private TMP_Text ammoText;
    [SerializeField] private bool showWeaponName = true;
    [SerializeField] private bool showFireMode = true;

    private void Awake()
    {
        if (playerController == null)
            playerController = FindAnyObjectByType<PlayerController>();
    }

    private void Update()
    {
        WeaponBase displayedWeapon = null;

        if (playerController != null)
            displayedWeapon = playerController.EquippedWeapon;

        if (displayedWeapon == null)
            displayedWeapon = weapon;

        if (displayedWeapon == null || ammoText == null)
        {
            if (ammoText != null)
                ammoText.text = "No weapon";

            return;
        }

        string ammoLine = $"{displayedWeapon.CurrentAmmo} / {displayedWeapon.ReserveAmmo}";

        if (!showWeaponName)
        {
            ammoText.text = ammoLine;
            return;
        }

        if (showFireMode && displayedWeapon.FireModeName != "Single")
        {
            ammoText.text = $"{displayedWeapon.DisplayName} [{displayedWeapon.FireModeName}]\n{ammoLine}";
        }
        else
        {
            ammoText.text = $"{displayedWeapon.DisplayName}\n{ammoLine}";
        }
    }
}
