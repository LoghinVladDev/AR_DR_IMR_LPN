using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class MenuPopulation : MonoBehaviour
{
    public Dropdown DesignerDropdown;
    public Dropdown SeasonDropdown;
    public Dropdown ClothTypeDropdown;
    public Dropdown ClothDropdown;

    public PoseEstimation poseEstimation;

    private Dictionary<string,
        Dictionary<string,
            Dictionary<string,
                Dictionary<string,
                    object // Vuforia object
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

            ClothTypeDropdown.ClearOptions();
            ClothTypeDropdown.AddOptions(collectionDropdownDataList);

            ClothTypeDropdown.value = 0;
            ClothTypeDropdown.onValueChanged.Invoke(0);
        });

        ClothTypeDropdown.onValueChanged.AddListener(delegate {
            string designerOption = DesignerDropdown.options[DesignerDropdown.value].text;
            string seasonOption = SeasonDropdown.options[SeasonDropdown.value].text;
            string clothTypeOption = ClothTypeDropdown.options[ClothTypeDropdown.value].text;

            List<Dropdown.OptionData> clothDropdownDataList = clothesData[designerOption][seasonOption][clothTypeOption].Keys
                .Select(x => new Dropdown.OptionData(x))
                .ToList()
            ;

            ClothDropdown.ClearOptions();
            ClothDropdown.AddOptions(clothDropdownDataList);

            ClothDropdown.value = 0;
            ClothDropdown.onValueChanged.Invoke(0);
        });

        ClothDropdown.onValueChanged.AddListener(delegate {
            string designer = DesignerDropdown.options[DesignerDropdown.value].text;
            string season = SeasonDropdown.options[SeasonDropdown.value].text;
            string clothType = ClothTypeDropdown.options[ClothTypeDropdown.value].text;
            string cloth = ClothDropdown.options[ClothDropdown.value].text;

            string path = "Clothes/" + designer + "/" + season + "/" + clothType + "/" + cloth.Replace(".fbx", "");
            Debug.Log(path);
            GameObject newObject = Instantiate(Resources.Load(path)) as GameObject;

            if (poseEstimation.clothMesh != null)
                poseEstimation.clothMesh.SetActive(false);
            poseEstimation.clothMesh = newObject;
        });
    }

    private void PopulateMenu()
    {
        clothesData = new Dictionary<string,
            Dictionary<string,
                Dictionary<string,
                    Dictionary<string,
                        object
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
                            object
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
                        object
                    >
                >()
            );

            ParseClothTypeCategories(seasonDir, designerName, seasonDir.Name);
        }
    }
    private void ParseClothTypeCategories(DirectoryInfo collectionDir, string designerName, string seasonName)
    {
        foreach (DirectoryInfo clothTypeDir in collectionDir.GetDirectories())
        {
            clothesData[designerName][seasonName].Add(
                clothTypeDir.Name,
                new Dictionary<string,
                    object
                >()
            );

            ParseClothCategories(clothTypeDir, designerName, seasonName, clothTypeDir.Name);
        }
    }

    private void ParseClothCategories(DirectoryInfo clothTypeDir, string designerName, string seasonName, string clothTypeName)
    {
        foreach (FileInfo clothDir in clothTypeDir.GetFiles("*.fbx"))
        {
            clothesData[designerName][seasonName][clothTypeName].Add(
                clothDir.Name,
                null
            );
        }
    }
}