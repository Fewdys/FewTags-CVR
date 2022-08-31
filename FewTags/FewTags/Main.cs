using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MelonLoader;
using Newtonsoft.Json;
using System.IO;
using FewTags.Utils;
using System.Net;
using ABI_RC.Core.Player;
using ABI_RC.Core.UI;
using ABI_RC.Core;
using Harmony;
using UnityEngine;
using System.Collections;
using ABI_RC.Core.Base.Jobs;
using UnityEngine.UI;

//Thanks To Edward7 For The Original Base

namespace FewTags
{
    public class Main : MelonMod
    {
        public static bool NocturnalTagsLoaded { get; private set; }

        private static float a;
        private static float b;
        private static float g;
        private static float r;

        private static List<Json.User> _userArr { get; set; }
        private static GameObject s_namePlate { get; set; }
        private static GameObject s_dev { get; set; }
        private static GameObject s_MainPlateHolder { get; set; }
        private static GameObject s_BigPlateHolder { get; set; }
        private static GameObject s_textMeshProGmj { get; set; }
        private static GameObject s_textMeshProGmj2 { get; set; }
        private static Transform s_plateTransform { get; set; }
        private static TMPro.TextMeshProUGUI s_Logo { get; set; }

        private HarmonyInstance _hInstance { get; } = new HarmonyInstance(Guid.NewGuid().ToString());

        public override void OnApplicationStart()
        {
            NocturnalTagsLoaded = MelonHandler.Mods.Any(m => m.Info.Name == "Nocturnal Plates");
            MelonLogger.Msg("Initializing.");
            MelonLogger.Msg("FewTags Loaded. Press Slash To Reload Tags");
            DownloadString();
            _hInstance.Patch(typeof(PlayerNameplate).GetMethod(nameof(PlayerNameplate.UpdateNamePlate)), null, typeof(Main).GetMethod(nameof(OnPlayerJoin),System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).ToNewHarmonyMethod());
            MelonCoroutines.Start(WaitForNamePlate());

        }

        private static IEnumerator WaitForNamePlate()
        {
            while (Resources.FindObjectsOfTypeAll<PuppetMaster>() == null)
                yield return null;
            s_namePlate = Resources.FindObjectsOfTypeAll<PuppetMaster>().FirstOrDefault(x => x.name == "_NetworkedPlayerObject").transform.Find("[NamePlate]/Canvas/Content").gameObject;
        }

        public override void OnUpdate()
        {
            if (Input.GetKeyDown(KeyCode.Slash))
            {
                ReloadString();
                MelonLogger.Msg("Reloaded Tags, Please Rejoin World.");
                CohtmlHud.Instance.ViewDropText("FewTags", "Connected", "Connected To FewTags");
            }
        }

        private void ReloadString()
        {
            _userArr.Clear();
            DownloadString();
        }

        private static string s_uId { get; set; }
        private static Json.User s_user { get; set; }

        private static void OnPlayerJoin(PlayerNameplate __instance)
        {
            s_uId = __instance.transform.parent.name;
            s_user = _userArr.FirstOrDefault(x => x.UserId == s_uId);
            if (s_user == null) return;
            for (int i = 0; i < s_user.NamePlatesText.Length; i++)
                GeneratePlate(s_uId, s_user.NamePlatesText[i], i, new Color32(byte.Parse(s_user.Color[0].ToString()), byte.Parse(s_user.Color[1].ToString()), byte.Parse(s_user.Color[2].ToString()), byte.Parse(s_user.Color[3].ToString())));
            for (int i = 0; i < s_user.BigPlatesText.Length; i++)
                GenerateBigPlate(s_uId, s_user.BigPlatesText[i], i);
            CreateLogo(s_uId);
        }

        private static void GeneratePlate(string uid, string plateText, int multiplier,Color32 color)
        {
            MelonLogger.Msg("---PlateText Length---");
            MelonLogger.Msg(plateText);
            MelonLogger.Msg(plateText.Length);

            try
            {
            int number1 = 5;
            int length = 0;
            string[] splitted = plateText.Split(new string[] { "<color=" }, StringSplitOptions.None);
            foreach (string s in splitted)
                {
                    //weeeeeeeeeeeeeeeeeeeeeeeeeee idk any other way of doing this lmaoo
                    number1++;
                    if (s.Length == 13)
                    {
                        length = s.Length;
                    }
                    else if (s.Length == 8)
                    {
                        length = s.Length;
                    }
                    else if (plateText.Length > number1)
                    {
                        length = plateText.Length / 20;
                    }
                    else if (plateText.Length > 125)
                    {
                        length = plateText.Length / 7;
                    }
                    else if (plateText.Length > 255)
                    {
                        length = plateText.Length / 20;
                    }
                    else if (plateText.Length == 225)
                    {
                        length = plateText.Length/ 20;
                    }
                    else if (plateText.Length == 344)
                    {
                        length = plateText.Length / 20;
                    }
                    else if (plateText.Length < 345)
                    {
                        length = plateText.Length / 20;
                    }
                    else if (plateText.Length > 445)
                    {
                        length = plateText.Length / 10;
                    }
                    else if (plateText.Length > 330)
                    {
                        length = plateText.Length / 8;
                    }
                    else if (plateText.Length == 52)
                    {
                        length = plateText.Length / 3;
                    }
                    else if (plateText.Length > 600)
                    {
                        length = plateText.Length / 8;
                    }
                    else if (plateText.Length < 52)
                    {
                        length = plateText.Length / 3;
                    }
                    else if (plateText.Length < 16)
                    {
                        length = plateText.Length;
                    }
                
                else if (plateText.Length == 1000)
                {
                    GameObject.Destroy(s_MainPlateHolder.transform.Find("Image").gameObject);
                }
                else if (plateText.Length > 1000)
                {
                    GameObject.Destroy(s_MainPlateHolder.transform.Find("Image").gameObject);
                }

            MelonLogger.Msg("---s.Lengths---");
            MelonLogger.Msg(s.Length);
            MelonLogger.Msg("---Splitted Lengths---");
            MelonLogger.Msg(splitted.Length);
                
                }
                s_MainPlateHolder = GameObject.Instantiate(s_namePlate, GameObject.Find("/" + uid + "[NamePlate]/Canvas").transform);
                s_MainPlateHolder.transform.localPosition = new Vector3(0, -0.155f - (multiplier) * 0.0778f, 0);
                s_MainPlateHolder.transform.Find("Image").gameObject.GetComponent<UnityEngine.UI.Image>().color = color;
                GameObject.Destroy(s_MainPlateHolder.transform.Find("Image/FriendsIndicator").gameObject);
                GameObject.Destroy(s_MainPlateHolder.transform.Find("Image/ObjectMaskSlave").gameObject);
                GameObject.Destroy(s_MainPlateHolder.transform.Find("Disable with Menu").gameObject);
                s_MainPlateHolder.transform.localScale = new Vector3(0.3f, 0.3f, 1);
                s_MainPlateHolder.transform.Find("Image").transform.localScale = length == 1 ? new Vector3(1, 0.5f, 1) : new Vector3(length * 0.069f, 0.5f, 1);
                s_textMeshProGmj = s_MainPlateHolder.transform.Find("TMP:Username").gameObject;
                s_textMeshProGmj.transform.localScale = new Vector3(0.58f, 0.58f, 1);
                s_textMeshProGmj.transform.localPosition = Vector3.zero;
                s_textMeshProGmj.GetComponent<TMPro.TextMeshProUGUI>().text = plateText;
                s_textMeshProGmj.GetComponent<TMPro.TextMeshProUGUI>().alignment = TMPro.TextAlignmentOptions.Center;
                s_textMeshProGmj.GetComponent<TMPro.TextMeshProUGUI>().autoSizeTextContainer = true;
                s_textMeshProGmj.gameObject.GetComponent<UnityEngine.RectTransform>().anchoredPosition = new Vector2(-0.05f, 0f);


                //Done Just For Removing The Text Under Devs/Mods ect
                s_dev = GameObject.Find("/" + uid + "[NamePlate]/Canvas/Content/Disable with Menu").gameObject.GetComponent<RectTransform>().gameObject;
                s_dev.transform.gameObject.SetActive(false);
            }
            catch { }
        }

        //Just Gonna Duplicate It For Big Text Because Im Lazy Asf
        private static void GenerateBigPlate(string uid, string plateText, int multiplier)
        {
            try
            {
                s_BigPlateHolder = GameObject.Instantiate(s_namePlate, GameObject.Find("/" + uid + "[NamePlate]/Canvas").transform);
                string[] splited = plateText.Split(new string[] { "<size=" }, StringSplitOptions.None);
                string sizeString = string.Empty;
                for (int i = 0; i < splited[1].Length; i++)
                {
                    if (!char.IsDigit(splited[1][i])) break;
                    sizeString += splited[1][i];
                }
                s_BigPlateHolder.transform.localPosition = NocturnalTagsLoaded ? new Vector3(0, 0.758f + (int.Parse(sizeString)) * 0.0075f, 0) : new Vector3(0, 0.45f + (int.Parse(sizeString)) * 0.0035f, 0);
                GameObject.Destroy(s_BigPlateHolder.transform.Find("Image").gameObject.GetComponent<UnityEngine.UI.Image>());
                GameObject.Destroy(s_BigPlateHolder.transform.Find("Image/FriendsIndicator").gameObject);
                GameObject.Destroy(s_BigPlateHolder.transform.Find("Image/ObjectMaskSlave").gameObject);
                GameObject.Destroy(s_BigPlateHolder.transform.Find("Disable with Menu").gameObject);
                s_textMeshProGmj2 = s_BigPlateHolder.transform.Find("TMP:Username").gameObject;
                s_textMeshProGmj2.transform.localPosition = Vector3.zero;
                s_textMeshProGmj2.GetComponent<TMPro.TextMeshProUGUI>().text = plateText;
                s_textMeshProGmj2.GetComponent<TMPro.TextMeshProUGUI>().autoSizeTextContainer = true;
                s_textMeshProGmj2.GetComponent<TMPro.TextMeshProUGUI>().color = new Color(r, g, b, 0.55f);
                s_textMeshProGmj2.transform.localScale = new Vector3(0.03f, 0.03f, 0.03f);
            }
            catch { }
        }

        public static void CreateLogo(string uid)
        {
            s_plateTransform = GameObject.Find("/" + uid + "[NamePlate]/Canvas").transform;
            s_Logo = GameObject.Instantiate(s_plateTransform.transform.Find("Content/TMP:Username").gameObject, s_plateTransform.transform.Find("Content").transform).gameObject.GetComponent<TMPro.TextMeshProUGUI>();
            s_Logo.text = "<b><i><color=#00FFFF>FT</color>";
            s_Logo.outlineWidth = 0.23f;
            s_Logo.outlineColor = new Color32(0, 0, 0, 255);
            s_Logo.autoSizeTextContainer = true;
            s_Logo.enableAutoSizing = false;
            s_Logo.fontSize = 0.19f;
            s_Logo.transform.localPosition = new Vector3(-1.35f, -0.45f);
            GameObject.Find("/" + uid + "[NamePlate]/Canvas").transform.localScale = new Vector3(0.45f, 0.45f, 1);
        }

        private static void DownloadString()
        {
            using (WebClient wc = new WebClient())
                _userArr = JsonConvert.DeserializeObject<List<Json.User>>(wc.DownloadString("https://raw.githubusercontent.com/Fewdys/FewTags-CVR/main/FewTags-CVR.json"));
        }


    }
}
