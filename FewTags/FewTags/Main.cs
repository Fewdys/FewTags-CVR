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
using ABI_RC.Core;
using Harmony;
using UnityEngine;
using System.Collections;

//Thanks To Edward7 For The Original Base

namespace FewTags
{
    public class Main : MelonMod
    {
        private static float b;
        private static float g;
        private static float r;

        private static List<Json.User> _userArr { get; set; }
        private static GameObject s_namePlate { get; set; }
        private static GameObject s_BigNamePlate { get; set; }
        private static GameObject s_dev { get; set; }
        private static GameObject s_MainPlateHolder { get; set; }
        private static GameObject s_BigPlateHolder { get; set; }
        private static GameObject s_textMeshProGmj { get; set; }
        private static GameObject s_textMeshProGmj2 { get; set; }

        private HarmonyInstance _hInstance { get; } = new HarmonyInstance(Guid.NewGuid().ToString());

        public override void OnApplicationStart()
        {
            /*   List<Json.User> lista = new List<Json.User>();
               lista.Add(new Json.User()
               {
                   id = 0,
                   NamePlatesText = new string[] { "Nothing" },
                   UserId = "0000"
               });

               File.WriteAllText(Directory.GetCurrentDirectory() + "//Jsonuuuuuuu.text", JsonConvert.SerializeObject(lista));*/
            MelonLogger.Msg("Initializing.");
            DownloadString();
            _hInstance.Patch(typeof(PlayerNameplate).GetMethod(nameof(PlayerNameplate.UpdateNamePlate)), null, typeof(Main).GetMethod(nameof(OnPlayerJoin),System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).ToNewHarmonyMethod());
            MelonCoroutines.Start(WaitForNamePlate());

        }

        private static IEnumerator WaitForNamePlate()
        {
            while (Resources.FindObjectsOfTypeAll<PuppetMaster>() == null)
                yield return null;
            s_namePlate = Resources.FindObjectsOfTypeAll<PuppetMaster>().FirstOrDefault(x => x.name == "_NetworkedPlayerObject").transform.Find("[NamePlate]/Canvas/Content").gameObject;
            s_BigNamePlate = Resources.FindObjectsOfTypeAll<PuppetMaster>().FirstOrDefault(x => x.name == "_NetworkedPlayerObject").transform.Find("[NamePlate]/Canvas/Content").gameObject;
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
        }

        private static void GeneratePlate(string uid, string plateText, int multiplier,Color32 color)
        {
            s_MainPlateHolder = GameObject.Instantiate(s_namePlate, GameObject.Find("/" + uid + "[NamePlate]/Canvas").transform);
            s_MainPlateHolder.transform.localPosition = new Vector3(0,-0.15f - (multiplier) * 0.075f, 0);
            s_MainPlateHolder.transform.Find("Image").gameObject.GetComponent<UnityEngine.UI.Image>().color = color;
            GameObject.Destroy(s_MainPlateHolder.transform.Find("Image/FriendsIndicator").gameObject);
            GameObject.Destroy(s_MainPlateHolder.transform.Find("Image/ObjectMaskSlave").gameObject);
            GameObject.Destroy(s_MainPlateHolder.transform.Find("Disable with Menu").gameObject);
            s_MainPlateHolder.transform.localScale = new Vector3(0.3f, 0.3f, 1);
            s_MainPlateHolder.transform.Find("Image").transform.localScale = new Vector3(1, 0.5f, 1);
            s_textMeshProGmj = s_MainPlateHolder.transform.Find("TMP:Username").gameObject;
            s_textMeshProGmj.transform.localScale = new Vector3(0.58f, 0.58f, 1);
            s_textMeshProGmj.transform.localPosition = Vector3.zero;
            s_textMeshProGmj.GetComponent<TMPro.TextMeshProUGUI>().text = plateText;
            s_textMeshProGmj.GetComponent<TMPro.TextMeshProUGUI>().autoSizeTextContainer = true;

            //Done Just For Removing The Text Under Devs/Mods ect
            s_dev = GameObject.Find("/" + uid + "[NamePlate]/Canvas/Content/Disable with Menu").GetComponent<RectTransform>().gameObject;
            s_dev.transform.gameObject.SetActive(false);
        }

        //Just Gonna Duplicate It For Big Text Because Im Lazy Asf
        private static void GenerateBigPlate(string uid, string plateText, int multiplier)
        {
            s_BigPlateHolder = GameObject.Instantiate(s_BigNamePlate, GameObject.Find("/" + uid + "[NamePlate]/Canvas").transform);
            s_BigPlateHolder.transform.localPosition = new Vector3(0, +0.425f - (multiplier) * 0.075f, 0);
            s_BigPlateHolder.transform.Find("Image").gameObject.GetComponent<UnityEngine.UI.Image>().color = new Color(0f, 0f, 0f, 0f);
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

            private static void DownloadString()
        {
            using (WebClient wc = new WebClient())
                _userArr = JsonConvert.DeserializeObject<List<Json.User>>(wc.DownloadString("https://raw.githubusercontent.com/Fewdys/FewTags-CVR/main/FewTags-CVR.json")); MelonLogger.Msg(_userArr.FirstOrDefault().Color[1]);
        }


    }
}
