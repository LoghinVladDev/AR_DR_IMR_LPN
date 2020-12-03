using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class MenuPopulation : MonoBehaviour
{
    public Dropdown DesignerDropdown;
    public Dropdown SeasonDropdown;
    public Dropdown CollectionDropdown;
    public Dropdown ClothTypeDropdown;
    public Dropdown ClothDropdown;

    private Dictionary<string,
        Dictionary<string,
            Dictionary<string,
                Dictionary<string,
                    Dictionary<string,
                        object // Vuforia object
                    >
                >
            >
        >
    > clothesData;

    private static string clothesDirectoryPath = "Assets/Resources/Clothes";

    void Start()
    {
        DropdownMenuValueChangedHandlers();

        PopulateMenu();
    }

    private void DropdownMenuValueChangedHandlers()
    {
        DesignerDropdown.onValueChanged.AddListener(delegate {
            string designerOption = DesignerDropdown.options[DesignerDropdown.value].text;

            List<Dropdown.OptionData> seasonDropdownDataList = clothesData[designerOption].Keys
                .Select(x => new Dropdown.OptionData(x))
                .ToList()
            ;

            SeasonDropdown.ClearOptions();
            SeasonDropdown.AddOptions(seasonDropdownDataList);

            SeasonDropdown.value = 0;
            SeasonDropdown.onValueChanged.Invoke(0);
        });

        SeasonDropdown.onValueChanged.AddListener(delegate {
            string designerOption = DesignerDropdown.options[DesignerDropdown.value].text;
            string seasonOption = SeasonDropdown.options[SeasonDropdown.value].text;

            List<Dropdown.OptionData> collectionDropdownDataList = clothesData[designerOption][seasonOption].Keys
                .Select(x => new Dropdown.OptionData(x))
                .ToList()
            ;

            CollectionDropdown.ClearOptions();
            CollectionDropdown.AddOptions(collectionDropdownDataList);

            CollectionDropdown.value = 0;
            CollectionDropdown.onValueChanged.Invoke(0);
        });

        CollectionDropdown.onValueChanged.AddListener(delegate {
            string designerOption = DesignerDropdown.options[DesignerDropdown.value].text;
            string seasonOption = SeasonDropdown.options[SeasonDropdown.value].text;
            string collectionOption = CollectionDropdown.options[CollectionDropdown.value].text;

            List<Dropdown.OptionData> clothTypeDropdownDataList = clothesData[designerOption][seasonOption][collectionOption].Keys
                .Select(x => new Dropdown.OptionData(x))
                .ToList()
            ;

            ClothTypeDropdown.ClearOptions();
            ClothTypeDropdown.AddOptions(clothTypeDropdownDataList);

            ClothTypeDropdown.value = 0;
            ClothTypeDropdown.onValueChanged.Invoke(0);
        });

        ClothTypeDropdown.onValueChanged.AddListener(delegate {
            string designerOption = DesignerDropdown.options[DesignerDropdown.value].text;
            string seasonOption = SeasonDropdown.options[SeasonDropdown.value].text;
            string collectionOption = CollectionDropdown.options[CollectionDropdown.value].text;
            string clothTypeOption = ClothTypeDropdown.options[ClothTypeDropdown.value].text;

            List<Dropdown.OptionData> clothDropdownDataList = clothesData[designerOption][seasonOption][collectionOption][clothTypeOption].Keys
                .Select(x => new Dropdown.OptionData(x))
                .ToList()
            ;

            ClothDropdown.ClearOptions();
            ClothDropdown.AddOptions(clothDropdownDataList);

            ClothDropdown.value = 0;
            ClothDropdown.onValueChanged.Invoke(0);
        });

        ClothDropdown.onValueChanged.AddListener(delegate {
            
        });
    }

    private void PopulateMenu()
    {
        clothesData = new Dictionary<string,
            Dictionary<string,
                Dictionary<string,
                    Dictionary<string,
                        Dictionary<string,
                            object
                        >
                    >
                >
            >
        >();
        DirectoryInfo clothesDataDir = new DirectoryInfo(clothesDirectoryPath);
        ParseDesignerCategories(clothesDataDir);

        List<Dropdown.OptionData> designerDropdownDataList = clothesData.Keys
                .Select(x => new Dropdown.OptionData(x))
                .ToList()
            ;
        DesignerDropdown.ClearOptions();
        DesignerDropdown.AddOptions(designerDropdownDataList);
        DesignerDropdown.value = 0;
        DesignerDropdown.onValueChanged.Invoke(0);
    }

    private void ParseDesignerCategories(DirectoryInfo clothesDataDir)
    {
        foreach (DirectoryInfo designerDir in clothesDataDir.GetDirectories())
        {
            clothesData.Add(
                designerDir.Name,
                new Dictionary<string,
                    Dictionary<string,
                        Dictionary<string,
                            Dictionary<string,
                                object
                            >
                        >
                    >
                >()
            );

            ParseSeasonCategories(designerDir, designerDir.Name);
        }
    }

    private void ParseSeasonCategories(DirectoryInfo designerDir, string designerName)
    {
        foreach (DirectoryInfo seasonDir in designerDir.GetDirectories())
        {
            clothesData[designerName].Add(
                seasonDir.Name,
                new Dictionary<string,
                    Dictionary<string,
                        Dictionary<string,
                            object
                        >
                    >
                >()
            );

            ParseCollectionCategories(seasonDir, designerName, seasonDir.Name);
        }
    }

    private void ParseCollectionCategories(DirectoryInfo seasonDir, string designerName, string seasonName)
    {
        foreach (DirectoryInfo collectionDir in seasonDir.GetDirectories())
        {
            clothesData[designerName][seasonName].Add(
                collectionDir.Name,
                new Dictionary<string,
                    Dictionary<string,
                        object
                    >
                >()
            );

            ParseClothTypeCategories(collectionDir, designerName, seasonName, collectionDir.Name);
        }
    }

    private void ParseClothTypeCategories(DirectoryInfo collectionDir, string designerName, string seasonName, string collectionName)
    {
        foreach (DirectoryInfo clothTypeDir in collectionDir.GetDirectories())
        {
            clothesData[designerName][seasonName][collectionName].Add(
                clothTypeDir.Name,
                new Dictionary<string,
                    object
                >()
            );

            ParseClothCategories(clothTypeDir, designerName, seasonName, collectionName, clothTypeDir.Name);
        }
    }

    private void ParseClothCategories(DirectoryInfo clothTypeDir, string designerName, string seasonName, string collectionName, string clothTypeName)
    {
        foreach (DirectoryInfo clothDir in clothTypeDir.GetDirectories())
        {
            clothesData[designerName][seasonName][collectionName][clothTypeName].Add(
                clothDir.Name,
                null
            );
        }
    }
}