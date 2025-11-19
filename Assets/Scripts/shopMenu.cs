using TMPro;
using UnityEngine;

public class shopMenu : MonoBehaviour
{
    [Header("Menu Panels")]
    public GameObject WholeShop;
    public GameObject firstMenu;
    public GameObject FoodMenu;
    public GameObject ToyMenu;
    public GameObject AccessoryMenu;
    public GameObject CollarsMenu;
    public GameObject HatsMenu;
    public GameObject GlassesMenu;
    public TMP_Text shopTitle;
    private int currentMenuIndex = 0;

    private void FoodMenuOpen()
    {
        FoodMenu.SetActive(true);
        firstMenu.SetActive(false);
        shopTitle.text = "FOOD";
    }

    private void FoodMenuClose()
    {
        FoodMenu.SetActive(false);
        firstMenu.SetActive(true);
        shopTitle.text = "PET SHOP";
    }

    private void ToyMenuOpen()
    {
        ToyMenu.SetActive(true);
        firstMenu.SetActive(false);
        shopTitle.text = "TOYS";
    }

    private void ToyMenuClose()
    {
        ToyMenu.SetActive(false);
        firstMenu.SetActive(true);
        shopTitle.text = "PET SHOP";
    }

    private void AccessoryMenuOpen()
    {
        AccessoryMenu.SetActive(true);
        firstMenu.SetActive(false);
        shopTitle.text = "ACCESSORIES";
    }

    private void OpenCollarsMenu()
    {
        CollarsMenu.SetActive(true);
        AccessoryMenu.SetActive(false);
        shopTitle.text = "COLLARS";
    }

    private void CloseCollarsMenu()
    {
        CollarsMenu.SetActive(false);
        AccessoryMenu.SetActive(true);
        shopTitle.text = "ACCESSORIES";
    }

    private void OpenHatsMenu()
    {
        HatsMenu.SetActive(true);
        AccessoryMenu.SetActive(false);
        shopTitle.text = "HATS";
    }

    private void CloseHatsMenu()
    {
        HatsMenu.SetActive(false);
        AccessoryMenu.SetActive(true);
        shopTitle.text = "ACCESSORIES";
    }

    private void OpenGlassesMenu()
    {
        GlassesMenu.SetActive(true);
        AccessoryMenu.SetActive(false);
        shopTitle.text = "GLASSES";
    }

    private void CloseGlassesMenu()
    {
        GlassesMenu.SetActive(false);
        AccessoryMenu.SetActive(true);
        shopTitle.text = "ACCESSORIES";
    }

    private void AccessoryMenuClose()
    {
        AccessoryMenu.SetActive(false);
        firstMenu.SetActive(true);
        shopTitle.text = "PET SHOP";
    }

    public void OpenMenu(int menuIndex)
    {
        switch (menuIndex)
        {
            case 1:
                FoodMenuOpen();
                break;

            case 2:
                ToyMenuOpen();
                break;

            case 3:
                break;

            case 4:
                AccessoryMenuOpen();
                break;

            case 5:
                OpenCollarsMenu();
                break;

            case 6:
                OpenHatsMenu();
                break;

            case 7:
                OpenGlassesMenu();
                break;
            default:
                return;
        }
        currentMenuIndex = menuIndex;
    }

    public void CloseCurrentMenu()
    {   
        switch (currentMenuIndex)
        {
            case 0:
                WholeShop.SetActive(false);
                break;
            case 1:
                currentMenuIndex = 0; // Reset to no menu open
                FoodMenuClose();
                break;
            case 2:
                currentMenuIndex = 0; // Reset to no menu open
                ToyMenuClose();
                break;
            case 3:
                currentMenuIndex = 0; // Reset to no menu open
                break;
            case 4:
                currentMenuIndex = 0; // Reset to no menu open
                AccessoryMenuClose();
                break;
            case 5:
                currentMenuIndex = 4; // Set to AccessoryMenu index
                CloseCollarsMenu();
                break;
            case 6:
                currentMenuIndex = 4; // Set to AccessoryMenu index
                CloseHatsMenu();
                break;
            case 7:
                currentMenuIndex = 4; // Set to AccessoryMenu index
                CloseGlassesMenu();
                break;
            default:
                return;
        }
    }
}
