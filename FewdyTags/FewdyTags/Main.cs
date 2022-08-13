using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MelonLoader;
using Newtonsoft.Json;
using System.IO;
using FewdyTags.Utils;
using System.Net;
using ABI_RC.Core.Player;
using ABI_RC.Core;
using Harmony;
using UnityEngine;
using System.Collections;

namespace FewdyTags
{
    public class Main : MelonMod
    {
        private static List<Json.User> _userArr { get; set; }
        private static GameObject s_namePlate { get; set; }
        private static GameObject s_temporaryNamePlate { get; set; }
        private static GameObject s_textMeshProGmj { get; set; }

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
        }

        private static string s_uId { get; set; }
        private static Json.User s_user { get; set; }

        private static void OnPlayerJoin(PlayerNameplate __instance)
        {
            s_uId = __instance.transform.parent.name;
            s_user = _userArr.FirstOrDefault(x => x.UserId == s_uId);
            if (s_user == null) return;
            for (int i = 0; i < s_user.NamePlatesText.Length; i++)
                GeneratePlate(s_uId, s_user.NamePlatesText[i], i,(Color)new Color32(byte.Parse(s_user.Color[0].ToString()), byte.Parse(s_user.Color[1].ToString()), byte.Parse(s_user.Color[2].ToString()), byte.Parse(s_user.Color[3].ToString())));
        }

        private static void GeneratePlate(string uid,string plateText, int multiplier,Color color)
        {
            s_temporaryNamePlate = GameObject.Instantiate(s_namePlate, GameObject.Find("/" + uid + "[NamePlate]/Canvas").transform);
            s_temporaryNamePlate.transform.localPosition = new Vector3(0,-0.15f - (multiplier) * 0.08f, 0);
            s_temporaryNamePlate.transform.Find("Image").gameObject.GetComponent<UnityEngine.UI.Image>().color = color;
            GameObject.Destroy(s_temporaryNamePlate.transform.Find("Image/FriendsIndicator").gameObject);
            GameObject.Destroy(s_temporaryNamePlate.transform.Find("Image/ObjectMaskSlave").gameObject);
            GameObject.Destroy(s_temporaryNamePlate.transform.Find("Disable with Menu").gameObject);
            s_temporaryNamePlate.transform.localScale = new Vector3(0.3f, 0.3f, 1);
            s_temporaryNamePlate.transform.Find("Image").transform.localScale = new Vector3(1, 0.5f, 1);
            s_textMeshProGmj = s_temporaryNamePlate.transform.Find("TMP:Username").gameObject;
            s_textMeshProGmj.transform.localScale = new Vector3(0.58f, 0.58f, 1);
            s_textMeshProGmj.transform.localPosition = Vector3.zero;
            s_textMeshProGmj.GetComponent<TMPro.TextMeshProUGUI>().text = plateText;
        }




        private static void DownloadString()
        {
            //Fewdy Got No Server so here is the heartbreacking part.
            using (WebClient wc = new WebClient())
                _userArr = JsonConvert.DeserializeObject<List<Json.User>>(wc.DownloadString("https://raw.githubusercontent.com/Edward7s/FewTags-CVR/main/FewTags-CVR.json"));         
        }


    }
}
