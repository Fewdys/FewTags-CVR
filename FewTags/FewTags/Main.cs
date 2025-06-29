﻿using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.Networking.IO.Global;
using ABI_RC.Core.Networking.IO.Instancing;
using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using ABI_RC.Core.UI;
using FewTags.Utils;
using Harmony;
using MelonLoader;
using Newtonsoft.Json;
using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Policy;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;

// Thanks To Edward7 For The Original Base

namespace FewTags
{
    public class Main : MelonMod
    {
        public static bool ChatBoxLoaded { get; private set; }

        private static float a;
        private static float b;
        private static float g;
        private static float r;

        static bool isOverlay = false;
        private static Json._Tags s_tags { get; set; }
        public static string s_rawTags { get; set; }
        private static List<Json.User> _userArr { get; set; }
        private static GameObject s_namePlate { get; set; }
        private static GameObject s_dev { get; set; }
        private static GameObject s_MainPlateHolder { get; set; }
        private static GameObject s_BigPlateHolder { get; set; }
        private static GameObject s_textMeshProGmj { get; set; }
        private static GameObject s_textMeshProGmj2 { get; set; }
        private static Transform s_plateTransform { get; set; }
        private static TMPro.TextMeshProUGUI s_Logo { get; set; }
        public static List<CVRPlayerEntity> p = new List<CVRPlayerEntity>();

        private HarmonyInstance _hInstance { get; } = new HarmonyInstance(Guid.NewGuid().ToString());

        public override void OnApplicationStart()
        {
            MelonLogger.Msg("Initializing.");
            MelonLogger.Msg("FewTags Loaded. Press Slash To Reload Tags");
            MelonLogger.Msg(ConsoleColor.Magenta, "Nameplate Overlay/Nameplate ESP - Keybind: RightCTRL + O");
            DownloadString();
            _hInstance.Patch(typeof(PlayerNameplate).GetMethod(nameof(PlayerNameplate.UpdateNamePlate)), null, typeof(Main).GetMethod(nameof(OnPlayerJoin), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).ToNewHarmonyMethod());
        }


        public static void NameplateOverlayLog(bool set)
        {
            p = CVRPlayerManager.Instance.NetworkPlayers;
            if (set == true)
            {
                MelonLogger.Msg(System.ConsoleColor.Green, "(Tagged Players) Nameplate ESP On");
            }
            else if (set == false)
            {
                MelonLogger.Msg(System.ConsoleColor.Red, "(Tagged Players) Nameplate ESP Off");
            }
        }

        public static void NameplateESP(PlayerNameplate player)
        {
            s_uId = player.transform.parent.name;
            s_user = s_tags.records.FirstOrDefault(x => x.UserId == s_uId);
            if (s_user == null) return;
            if (GameObject.Find($"{s_uId}/[NamePlate]/Canvas") != null)
            {
                if (GameObject.Find($"{s_uId}/[NamePlate]/Canvas/Content") != null)
                    GameObject.Find($"{s_uId}/[NamePlate]/Canvas/Content").GetComponentsInChildren<TMPro.TextMeshProUGUI>().All(m => m.isOverlay = isOverlay);
                if (GameObject.Find($"{s_uId}/[NamePlate]/Canvas/FewTags-Default") != null)
                    GameObject.Find($"{s_uId}/[NamePlate]/Canvas/FewTags-Default").GetComponentsInChildren<TMPro.TextMeshProUGUI>().All(m => m.isOverlay = isOverlay);
                if (GameObject.Find($"{s_uId}/[NamePlate]/Canvas/FewTags-NamePlate") != null)
                    GameObject.Find($"{s_uId}/[NamePlate]/Canvas/FewTags-NamePlate").GetComponentsInChildren<TMPro.TextMeshProUGUI>().All(m => m.isOverlay = isOverlay);
            }
        }

        // Keybind To Re-Fetch The Tags (Prevents The Need Of Having To Restart You're Game)
        public override void OnUpdate()
        {
            if (Input.GetKeyDown(KeyCode.Slash))
            {
                if (CVR_MenuManager.Instance.quickMenuCollider.enabled == true || ViewManager.Instance.IsAnyMenuOpen == true) { return; } // we don't wanna accidently reload tags while menu's are open
                else
                {
                    ReloadString();
                    MelonLogger.Msg("Reloaded Tags, Please Rejoin World If Needed.");
                    CohtmlHud.Instance.ViewDropText("FewTags", "Connected", "Downloading Tags");
                }
            }

            // Overlay Toggle
            if (Input.GetKey(KeyCode.RightShift) && Input.GetKeyDown(KeyCode.O))
            {
                if (CVR_MenuManager.Instance.quickMenuCollider.enabled == true || ViewManager.Instance.IsAnyMenuOpen == true) { return; }
                else
                {
                    isOverlay = !isOverlay;
                    NameplateOverlayLog(isOverlay);
                    if (p.Count != 0)
                    {
                        try
                        {
                            foreach (var player in p)
                            {
                                NameplateESP(player.PlayerNameplate);
                            }
                        }
                        catch { }
                    }
                }
            }
        }

        // Used For The Keybind To Update The Tags
        private void ReloadString()
        {
            try
            {
                s_tags.records.Clear();
            }
            catch // not gonna let yourself know if there was an issue clearing?? let me fix that for you
            {
                MelonLogger.Msg(ConsoleColor.Yellow, "Couldn't Clear Tags List - Probably Because There Were No Tags To Clear");
            }
            DownloadString();
        }

        private static string s_uId { get; set; }
        private static Json.User s_user { get; set; }

        private static void OnPlayerJoin(PlayerNameplate __instance)
        {
            s_uId = __instance.transform.parent.name;
            s_user = s_tags.records.FirstOrDefault(x => x.UserId == s_uId);
            if (s_user == null) return;
            if (GameObject.Find($"{s_uId}/[NamePlate]/Canvas/Content") != null)
            {
                GameObject.Find($"{s_uId}/[NamePlate]/Canvas/Content").GetComponentsInChildren<TMPro.TextMeshProUGUI>().All(m => m.isOverlay = isOverlay);
            }
            if (GameObject.Find($"{s_uId}/[NamePlate]/Canvas/FewTags-Default") == null) // why wasnt there a check for if it already exists?
            {
                    GenerateDefaultPlate(s_uId, "<b><color=#ff0000>-</color> <color=#ff7f00>F</color><color=#ffbf00>e</color><color=#ffff00>w</color><color=#80ff00>T</color><color=#00ff00>a</color><color=#00ff80>g</color><color=#00ffff>s</color> <color=#0000ff>-</color></b>", 0, new Color32(byte.Parse(s_user.Color[0].ToString()), byte.Parse(s_user.Color[1].ToString()), byte.Parse(s_user.Color[2].ToString()), byte.Parse(s_user.Color[3].ToString())));
            }
            if (GameObject.Find($"{s_uId}/[NamePlate]/Canvas/FewTags-NamePlate") == null) // why wasnt there a check for if it already exists?
            {
                for (int i = 0; i < s_user.NamePlatesText.Length; i++)
                {
                    GeneratePlate(s_uId, s_user.NamePlatesText[i], i, new Color32(byte.Parse(s_user.Color[0].ToString()), byte.Parse(s_user.Color[1].ToString()), byte.Parse(s_user.Color[2].ToString()), byte.Parse(s_user.Color[3].ToString())));
                }
            }
            if (GameObject.Find($"{s_uId}/[NamePlate]/Canvas/FewTags-BigNamePlate") == null) // again why wasnt there a check for if it already exists?
            {
                for (int i = 0; i < s_user.BigPlatesText.Length; i++)
                {
                    GenerateBigPlate(s_uId, s_user.BigPlatesText[i], i);
                }
            }
            /*CreateLogo(s_uId);*/
        }

        private static float s_textCount { get; set; }
        private static GameObject s_imageHolder { get; set; }


        private static void GenerateDefaultPlate(string uid, string plateText, int multiplier, Color32 color)
        {
            // This Was Used For Testing Mainly To Check Lengths Of Things (Sorta Math Related I Guess)
            // MelonLogger.Msg("---PlateText---");
            // MelonLogger.Msg(plateText);
            // MelonLogger.Msg("---PlateText Length---");
            // MelonLogger.Msg(plateText.Length);

            if (plateText == null) { return; } // why make the plate if it has no text to start with?
            else if (uid == null) { return; } // this one isnt really needed tbh but personally I like to check to prevent code that shouldn't be running to run
            else
            {
                try  // Try Catch For Incase The Tag Somehow Manages To Mess Up -- Improved For You <3
                {
                    s_textCount = plateText.Contains("<color=") ? plateText.Length - (Regex.Matches(plateText, "<color=").Count != 1 ? Regex.Matches(plateText, "<color=").Count * 23 - 3 : 20) : plateText.Length;
                    s_MainPlateHolder = GameObject.Instantiate(GameObject.Find(uid + "[NamePlate]/Canvas/Content"), GameObject.Find(uid + "[NamePlate]/Canvas").transform);
                    s_MainPlateHolder.transform.localPosition = new Vector3(0, -0.155f - (multiplier) * 0.0778f, 0);
                    s_MainPlateHolder.name = "FewTags-Default"; // why were you not naming the object to be able to find it if needed?
                    s_MainPlateHolder.layer = 69; // ;)
                    s_imageHolder = s_MainPlateHolder.transform.Find("Image").gameObject;
                    s_imageHolder.GetComponent<UnityEngine.UI.Image>().color = color;
                    try // This Is Just Here Incase The Paths of Objects To Destroy Change (A Try Catch In A Try Catch May Seem Repetitive & Stupid, But It's For The Sake Of Knowing Whats Wrong Easily)
                    {
                        //GameObject.Destroy(s_MainPlateHolder.transform.Find("Image/FriendsIndicator").gameObject);
                        //GameObject.Destroy(s_MainPlateHolder.transform.Find("Image/ObjectMaskSlave").gameObject);
                        //GameObject.Destroy(s_MainPlateHolder.transform.Find("Image/UserImage").gameObject);
                        //GameObject.Destroy(s_MainPlateHolder.transform.Find("Image/Image").gameObject);
                        GameObject.Destroy(s_MainPlateHolder.transform.Find("Disable with Menu").gameObject);
                        GameObject.Destroy(s_MainPlateHolder.transform.Find("Image").gameObject);
                    }
                    catch { MelonLogger.Msg(ConsoleColor.DarkRed, $"Failed To Destroy One Or More Objects On Created FewTags-Nameplate ({uid})"); }
                    s_MainPlateHolder.transform.localScale = new Vector3(0.3f, 0.3f, 1);
                    s_imageHolder.transform.localScale = new Vector3(1, 0.5f, 1);
                    s_imageHolder.GetComponent<RectTransform>().sizeDelta = new Vector2(s_textCount / 10, 0.5f);
                    s_textMeshProGmj = s_MainPlateHolder.transform.Find("TMP:Username").gameObject;
                    s_textMeshProGmj.transform.localScale = new Vector3(0.58f, 0.58f, 1);
                    s_textMeshProGmj.transform.localPosition = Vector3.zero;
                    s_textMeshProGmj.gameObject.GetComponent<UnityEngine.RectTransform>().anchoredPosition = new Vector2(-0.05f, 0f);
                    var tmpc = s_textMeshProGmj.GetComponent<TMPro.TextMeshProUGUI>(); // why make life more diffucult and not just do this?
                    tmpc.text = plateText;
                    tmpc.alignment = TMPro.TextAlignmentOptions.Center;
                    tmpc.autoSizeTextContainer = true;
                    tmpc.enableCulling = true;
                    tmpc.material.enableInstancing = true;
                    tmpc.isOverlay = isOverlay;

                    // Done Just For Removing The Text Under Devs/Mods - Doesn't Effect Being Able To See Who Is A Dev/Mod ect. (Done For Personal Preference To Make Things Cleaner)
                    s_dev = GameObject.Find("/" + uid + "[NamePlate]/Canvas/Content/Disable with Menu").gameObject.GetComponent<RectTransform>().gameObject;
                    s_dev.transform.gameObject.SetActive(false);
                }
                catch // why not have it tell us where something went wrong instead of just basically catching it each time it fails
                {
                    if (uid != null)
                    {
                        MelonLogger.Msg(ConsoleColor.DarkRed, $"Failed To Create Nameplate On {uid}");
                    }
                    else
                    {
                        MelonLogger.Msg(ConsoleColor.DarkRed, $"Failed To Create Nameplate");
                    }
                }
            }
        }
        private static void GeneratePlate(string uid, string plateText, int multiplier, Color32 color)
        {
            if (string.IsNullOrEmpty(plateText) || string.IsNullOrEmpty(uid)) return;

            try
            {
                // Calculate the text count, adjusting for any color tags
                s_textCount = plateText.Contains("<color=")
                    ? plateText.Length - (Regex.Matches(plateText, "<color=").Count * 23 - 3)
                    : plateText.Length;

                // Instantiate the main plate holder and set its position, scale, and layer
                s_MainPlateHolder = GameObject.Instantiate(GameObject.Find(uid + "[NamePlate]/Canvas/Content"), GameObject.Find($"/{uid}[NamePlate]/Canvas").transform);
                s_MainPlateHolder.transform.localPosition = new Vector3(0, -0.210f - (multiplier) * 0.0618f, 0);
                s_MainPlateHolder.name = "FewTags-NamePlate";
                s_MainPlateHolder.layer = 69;

                // Configure the image holder and remove unnecessary components
                s_imageHolder = s_MainPlateHolder.transform.Find("Image").gameObject;
                s_imageHolder.GetComponent<UnityEngine.UI.Image>().color = color;

                try
                {
                    // Safely destroy unneeded components
                    GameObject.Destroy(s_MainPlateHolder.transform.Find("Disable with Menu").gameObject);
                    GameObject.Destroy(s_imageHolder);
                }
                catch
                {
                    MelonLogger.Msg(ConsoleColor.DarkRed, $"Failed To Destroy One Or More Objects On Created FewTags-Nameplate ({uid})");
                }

                // Adjust the scale and size of the main plate and text container
                s_MainPlateHolder.transform.localScale = new Vector3(0.3f, 0.3f, 1);

                // Reference to TextMeshPro GameObject and adjust its scale and position
                s_textMeshProGmj = s_MainPlateHolder.transform.Find("TMP:Username").gameObject;
                s_textMeshProGmj.transform.localScale = new Vector3(0.58f, 0.58f, 1);
                s_textMeshProGmj.transform.localPosition = Vector3.zero;
                s_textMeshProGmj.GetComponent<RectTransform>().anchoredPosition = new Vector2(-0.05f, 0f);

                var tmpc = s_textMeshProGmj.GetComponent<TMP_Text>();

                // Set TextMeshPro properties
                tmpc.alignment = TMPro.TextAlignmentOptions.Center;
                tmpc.autoSizeTextContainer = true;
                tmpc.enableCulling = true;
                tmpc.material.enableInstancing = true;
                tmpc.isOverlay = isOverlay;

                // Add and configure the AnimationManager
                AnimationManager animationManager = s_textMeshProGmj.gameObject.AddComponent<AnimationManager>();
                animationManager.textMeshPro = tmpc;

                // Parse and add animations based on tags
                if (plateText.StartsWith("@cyln"))
                {
                    animationManager.animationTypes.Add(AnimationType.CYLN);
                    plateText = plateText.Remove(0, "@cyln".Length).Insert(0, "");
                }
                if (plateText.StartsWith("@letter"))
                {
                    animationManager.animationTypes.Add(AnimationType.LetterByLetter);
                    plateText = plateText.Remove(0, "@letter".Length).Insert(0, "");
                }
                if (plateText.StartsWith("@rain"))
                {
                    animationManager.animationTypes.Add(AnimationType.Rainbow);
                    plateText = plateText.Remove(0, "@rain".Length).Insert(0, "");
                }
                if (plateText.StartsWith("@sr"))
                {
                    animationManager.animationTypes.Add(AnimationType.SmoothRainbow);
                    plateText = plateText.Remove(0, "@sr".Length).Insert(0, "");
                }

                // Set the final text after removing the tags
                tmpc.text = plateText;

                // Start the animation
                animationManager.Start();
            }
            catch (Exception e)
            {
                MelonLogger.Msg(ConsoleColor.DarkRed, $"Failed To Create Nameplate for {uid}: {e}");
            }
        }



        // Duplicated GeneratePlate And Changed/Added A Bit Because I Was Lazy And Wanted A Specific Spot For Big Text -- You're just like me fr stop being lazy tho :p
        private static void GenerateBigPlate(string uid, string plateText, int multiplier)
        {
            if (plateText == null) { return; } // again why make the plate if it has no text to start with?
            else if (uid == null) { return; } // again this one isnt really needed tbh but personally I like to check to prevent code that shouldn't be running to run
            else
            {
                try  // Try Catch For Incase The Tag Somehow Manages To Mess Up -- Improved For You <3
                {
                    s_BigPlateHolder = GameObject.Instantiate(GameObject.Find(uid + "[NamePlate]/Canvas/Content"), GameObject.Find("/" + uid + "[NamePlate]/Canvas").transform);
                    s_BigPlateHolder.name = "FewTags-BigNamePlate"; // again why were you not naming the object to be able to find it if needed?
                    s_BigPlateHolder.layer = 69; // ;)
                    string[] splited = plateText.Split(new string[] { "<size=" }, StringSplitOptions.None);
                    string sizeString = string.Empty;
                    for (int i = 0; i < splited[1].Length; i++)
                    {
                        if (!char.IsDigit(splited[1][i])) break;
                        sizeString += splited[1][i];
                    }
                    // Moves Big Text Based On Weather Or Not ChatBox Is Loaded -- Idk Why You Were Checking For A Deprecated Mod
                    s_BigPlateHolder.transform.localPosition = new Vector3(0, 0.45f + (int.Parse(sizeString)) * 0.0075f, 0);
                    try // This Is Just Here Incase The Paths of Objects To Destroy Change (A Try Catch In A Try Catch May Seem Repetitive & Stupid, But It's For The Sake Of Knowing Whats Wrong Easily)
                    {
                        //GameObject.Destroy(s_BigPlateHolder.transform.Find("Image").gameObject.GetComponent<UnityEngine.UI.Image>());
                        //GameObject.Destroy(s_BigPlateHolder.transform.Find("Image/FriendsIndicator").gameObject);
                        //GameObject.Destroy(s_BigPlateHolder.transform.Find("Image/ObjectMaskSlave").gameObject);
                        //GameObject.Destroy(s_BigPlateHolder.transform.Find("Image/UserImage").gameObject);
                        GameObject.Destroy(s_BigPlateHolder.transform.Find("Disable with Menu").gameObject);
                        GameObject.Destroy(s_BigPlateHolder.transform.Find("Image").gameObject);
                    }
                    catch { MelonLogger.Msg(ConsoleColor.DarkRed, $"Failed To Destroy One Or More Objects On Created FewTags-BigNameplate ({uid})"); }
                    s_textMeshProGmj2 = s_BigPlateHolder.transform.Find("TMP:Username").gameObject;
                    s_textMeshProGmj2.transform.localPosition = Vector3.zero;
                    s_textMeshProGmj2.transform.localScale = new Vector3(0.03f, 0.03f, 0.03f);
                    var bptmpc = s_textMeshProGmj2.GetComponent<TMPro.TextMeshProUGUI>(); // again why make life more diffucult and not just do this?
                    bptmpc.text = plateText;
                    bptmpc.autoSizeTextContainer = true;
                    bptmpc.color = new Color(r, g, b, 0.55f);
                    bptmpc.enableCulling = true;
                    bptmpc.material.enableInstancing = true;
                    bptmpc.isOverlay = isOverlay;
                }
                catch // again why not have it tell us where something went wrong instead of just basically catching it each time it fails
                {
                    if (uid != null)
                    {
                        MelonLogger.Msg(ConsoleColor.DarkRed, $"Failed To Create BigNameplate On {uid}");
                    }
                    else
                    {
                        MelonLogger.Msg(ConsoleColor.DarkRed, $"Failed To Create BigNameplate");
                    }
                }
            }
        }

        // At Some Point Will Make This Show Up For Each Person Running The Mod If I Figure Out How To Since I Don't Have A Server Nor Do I Plan On Using One // Small Update On This, Probably Not, Or Atleast Not Anytime Soon
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

        // Downloads The String Of The Json Aka Tags
        private static void DownloadString()
        {
            try
            {
                using (WebClient wc = new WebClient())
                {
                    s_rawTags = wc.DownloadString("https://raw.githubusercontent.com/Fewdys/FewTags-CVR/main/FewTags-CVR.json");
                    s_tags = JsonConvert.DeserializeObject<Json._Tags>(s_rawTags);
                    MelonLogger.Msg(ConsoleColor.Green, "Downloaded Tags");
                }
            }
            catch { MelonLogger.Msg(ConsoleColor.DarkRed, "Error Downloading Tags (Likely A Github Issue or A Internet/Service Issue)"); } // see like this you gave a reason for a possible issue. you should do that way more often when doing try catch

        }
        private static void DownloadString2()
        {
            try
            {
                using (WebClient wc = new WebClient())
                {
                    s_rawTags = wc.DownloadString("https://raw.githubusercontent.com/Fewdys/FewTags-CVR/refs/heads/main/FewTags-CVR.json");

                    if (string.IsNullOrEmpty(s_rawTags))
                    {
                        return;
                    }

                    JSONNode jsonNode = JSON.Parse(s_rawTags);
                    if (jsonNode == null)
                    {
                        return;
                    }

                    s_tags = new Json._Tags { records = new List<Json.User>() };

                    foreach (JSONNode record in jsonNode["records"].AsArray)
                    {
                        List<string> tagsList = new List<string>();
                        foreach (JSONNode tag in record["NamePlatesText"].AsArray)
                        {
                            tagsList.Add(tag.Value);
                        }

                        List<string> bigtagsList = new List<string>();
                        foreach (JSONNode tag in record["BigPlatesText"].AsArray)
                        {
                            bigtagsList.Add(tag.Value);
                        }

                        int[] colorArray = null;

                        if (record["Color"] != null && record["Color"].Count > 0)
                        {
                            colorArray = new int[record["Color"].Count];
                            for (int i = 0; i < record["Color"].Count; i++)
                            {
                                colorArray[i] = record["Color"][i].AsInt;
                            }
                        }

                        Json.User tagEntry = new Json.User
                        {
                            id = record["id"].AsInt,
                            UserId = record["UserID"],
                            NamePlatesText = tagsList.ToArray(),
                            BigPlatesText = tagsList.ToArray(),
                            Color = colorArray,
                        };

                        s_tags.records.Add(tagEntry);
                    }
                    return;
                }
            }
            catch { MelonLogger.Msg(ConsoleColor.DarkRed, "Error Downloading Tags (Likely A Github Issue or A Internet/Service Issue)"); } // see like this you gave a reason for a possible issue. you should do that way more often when doing try catch

        }
    }
}
