using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.Player;
using ABI_RC.Core.UI;
using FewTags.Utils;
using Harmony;
using MelonLoader;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Thanks To Edward7 For The Original Base

namespace FewTags
{
    public class Main : MelonMod
    {
        private const string TagsUrl = "https://raw.githubusercontent.com/Fewdys/FewTags-CVR/main/FewTags-CVR.json";
        private const int TagLayer = 69;

        private const string DefaultName = "FewTags-Default";
        private const string PlateName = "FewTags-NamePlate";
        private const string BigPlateName = "FewTags-BigNamePlate";

        private static readonly Color BigPlateColor = new Color(0f, 0f, 0f, 0.55f);
        private static readonly Regex RichTextRegex = new Regex("<[^>]+>", RegexOptions.Compiled);
        private static readonly Regex SizeRegex = new Regex(@"<size=(\d+)", RegexOptions.Compiled);

        private const string DefaultPlateText =
            "<b><color=#ff0000>-</color> <color=#ff7f00>F</color><color=#ffbf00>e</color><color=#ffff00>w</color>" +
            "<color=#80ff00>T</color><color=#00ff00>a</color><color=#00ff80>g</color><color=#00ffff>s</color> <color=#0000ff>-</color></b>";

        private static readonly KeyValuePair<string, AnimationType>[] AnimationTags =
        {
            new KeyValuePair<string, AnimationType>("@cyln",   AnimationType.CYLN),
            new KeyValuePair<string, AnimationType>("@letter", AnimationType.LetterByLetter),
            new KeyValuePair<string, AnimationType>("@rain",   AnimationType.Rainbow),
            new KeyValuePair<string, AnimationType>("@sr",     AnimationType.SmoothRainbow),
        };

        public static string s_rawTags { get; set; }

        private static bool s_overlay;
        private static int s_downloading;
        private static Dictionary<string, TagRecord> s_lookup = new Dictionary<string, TagRecord>(StringComparer.Ordinal);
        private static volatile Dictionary<string, TagRecord> s_pendingLookup; // Written by the download thread, consumed in OnUpdate
        private static readonly Dictionary<string, TagEntry> s_entries = new Dictionary<string, TagEntry>(StringComparer.Ordinal); // One entry per player (by user id). This is what prevents duplicates.
        private static readonly List<string> s_pruneScratch = new List<string>();

        private HarmonyInstance _hInstance { get; } = new HarmonyInstance(Guid.NewGuid().ToString());

        private sealed class TagRecord
        {
            public readonly Json.User User;
            public readonly string Signature; // changes whenever the user's tag data changes

            public TagRecord(Json.User user, string signature)
            {
                User = user;
                Signature = signature;
            }
        }

        private sealed class TagEntry
        {
            public string Uid;
            public string Signature;
            public Transform Canvas;
            public readonly List<GameObject> Objects = new List<GameObject>();
            public readonly List<TMP_Text> Texts = new List<TMP_Text>();

            public bool IsCurrent(string signature, Transform canvas)
            {
                if (Canvas == null || Canvas != canvas || Signature != signature) return false;
                for (int i = 0; i < Objects.Count; i++)
                    if (Objects[i] == null) return false;
                return true;
            }

            public void ApplyOverlay(bool value)
            {
                for (int i = 0; i < Texts.Count; i++)
                {
                    TMP_Text t = Texts[i];
                    if (t != null && t.isOverlay != value) t.isOverlay = value;
                }
            }

            public void Destroy()
            {
                for (int i = 0; i < Objects.Count; i++)
                    if (Objects[i] != null) UnityEngine.Object.Destroy(Objects[i]);
                Objects.Clear();
                Texts.Clear();
            }
        }

        public override void OnApplicationStart()
        {
            MelonLogger.Msg("Initializing.");
            MelonLogger.Msg("FewTags Loaded. Press Slash To Reload Tags");
            MelonLogger.Msg(ConsoleColor.Magenta, "Nameplate Overlay/Nameplate ESP - Keybind: RightShift + O");
            DownloadTagsAsync();
            _hInstance.Patch(
                typeof(PlayerNameplate).GetMethod(nameof(PlayerNameplate.UpdateNamePlateSettings)),
                null,
                typeof(Main).GetMethod(nameof(OnPlayerJoin), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).ToNewHarmonyMethod());
        }

        public override void OnUpdate()
        {
            // Apply freshly downloaded tags on the main thread
            Dictionary<string, TagRecord> pending = s_pendingLookup;
            if (pending != null)
            {
                s_pendingLookup = null;
                s_lookup = pending;
                RefreshAllPlayers();
            }

            if (Input.GetKeyDown(KeyCode.Slash))
            {
                if (ViewManager.Instance == null || ViewManager.Instance.IsAnyMenuOpen) return; // don't reload while menus are open
                MelonLogger.Msg("Reloading Tags...");
                CohtmlHud.Instance.ViewDropText("FewTags", "Connected", "Downloading Tags");
                DownloadTagsAsync();
            }

            if (Input.GetKey(KeyCode.RightShift) && Input.GetKeyDown(KeyCode.O))
            {
                if (ViewManager.Instance == null || ViewManager.Instance.IsAnyMenuOpen) return;
                if (CVR_MenuManager.Instance != null && CVR_MenuManager.Instance.quickMenuCollider.enabled) return;

                s_overlay = !s_overlay;
                NameplateOverlayLog(s_overlay);
                foreach (TagEntry entry in s_entries.Values)
                    entry.ApplyOverlay(s_overlay);
            }
        }

        public static void NameplateOverlayLog(bool set)
        {
            if (set) MelonLogger.Msg(ConsoleColor.Green, "(Tagged Players) Nameplate ESP On");
            else MelonLogger.Msg(ConsoleColor.Red, "(Tagged Players) Nameplate ESP Off");
        }

        private static void DownloadTagsAsync()
        {
            if (Interlocked.Exchange(ref s_downloading, 1) == 1) return; // already downloading

            Task.Run(() =>
            {
                try
                {
                    string raw;
                    using (WebClient wc = new WebClient())
                        raw = wc.DownloadString(TagsUrl);

                    Json._Tags tags = JsonConvert.DeserializeObject<Json._Tags>(raw);
                    var lookup = new Dictionary<string, TagRecord>(StringComparer.Ordinal);
                    if (tags != null && tags.records != null)
                    {
                        foreach (Json.User u in tags.records)
                        {
                            if (u == null || string.IsNullOrEmpty(u.UserId)) continue;
                            lookup[u.UserId] = new TagRecord(u, JsonConvert.SerializeObject(u));
                        }
                    }

                    s_rawTags = raw;
                    s_pendingLookup = lookup;
                    MelonLogger.Msg(ConsoleColor.Green, "Downloaded Tags");
                }
                catch (Exception e)
                {
                    MelonLogger.Msg(ConsoleColor.DarkRed, $"Error Downloading Tags (Likely A Github Issue or A Internet/Service Issue): {e.GetType().Name}: {e.Message}");
                }
                finally
                {
                    Interlocked.Exchange(ref s_downloading, 0);
                }
            });
        }

        private static void OnPlayerJoin(PlayerNameplate __instance)
        {
            try { Ensure(__instance); }
            catch (Exception e) { MelonLogger.Msg(ConsoleColor.DarkRed, $"FewTags error: {e}"); }
        }

        private static void RefreshAllPlayers()
        {
            var manager = CVRPlayerManager.Instance;
            if (manager == null || manager.NetworkPlayers == null) return;

            foreach (CVRPlayerEntity player in manager.NetworkPlayers)
            {
                if (player == null || player.PlayerNameplate == null) continue;
                try { Ensure(player.PlayerNameplate); }
                catch (Exception e) { MelonLogger.Msg(ConsoleColor.DarkRed, $"FewTags error: {e}"); }
            }
        }

        // Creates the tag set if missing, rebuilds it if the data/objects changed, otherwise does nothing.
        private static void Ensure(PlayerNameplate nameplate)
        {
            if (nameplate == null) return;

            Transform overhead = FindAncestor(nameplate.transform, "[Overhead]");
            if (overhead == null || overhead.parent == null) return;
            string uid = overhead.parent.name;

            if (uid == "_PLAYERLOCAL")
            {
                uid = nameplate.Player.PlayerId;
            }

            TagEntry entry;
            s_entries.TryGetValue(uid, out entry);

            TagRecord record;
            s_lookup.TryGetValue(uid, out record);

            if (record == null) // not a tagged user (or tags were removed on reload)
            {
                if (entry != null)
                {
                    entry.Destroy();
                    s_entries.Remove(uid);
                }
                return;
            }

            Transform canvas = overhead.Find("Canvas");
            if (canvas == null) return;

            if (entry != null && entry.IsCurrent(record.Signature, canvas))
            {
                entry.ApplyOverlay(s_overlay); // nothing to create, just keep overlay state in sync
                return;
            }

            if (entry != null)
            {
                entry.Destroy();
                s_entries.Remove(uid);
            }

            PruneDeadEntries();

            TagEntry built = Build(uid, record, canvas);
            if (built != null) s_entries[uid] = built;
        }

        private static void PruneDeadEntries()
        {
            s_pruneScratch.Clear();
            foreach (KeyValuePair<string, TagEntry> kv in s_entries)
                if (kv.Value.Canvas == null) s_pruneScratch.Add(kv.Key);
            foreach (string key in s_pruneScratch)
                s_entries.Remove(key);
        }

        private static Transform FindAncestor(Transform t, string name)
        {
            while (t != null)
            {
                if (t.name == name) return t;
                t = t.parent;
            }
            return null;
        }

        private static TagEntry Build(string uid, TagRecord record, Transform canvas)
        {
            Transform nameplate = canvas.Find("Nameplate");
            Transform content = nameplate != null ? nameplate.GetChild(0) : null;
            if (content == null) return null; // nameplate not ready yet, the next settings update will retry

            Json.User user = record.User;
            Color32 color = ToColor32(user.Color);

            var entry = new TagEntry { Uid = uid, Signature = record.Signature, Canvas = canvas };
            entry.Texts.AddRange(content.GetComponentsInChildren<TMP_Text>(true));

            Guard(uid, "default plate", () => CreateDefaultPlate(entry, content, nameplate, color));

            if (user.NamePlatesText != null)
            {
                for (int i = 0; i < user.NamePlatesText.Length; i++)
                {
                    int index = i;
                    string text = user.NamePlatesText[i];
                    Guard(uid, "nameplate #" + i, () => CreatePlate(entry, content, nameplate, text, index));
                }
            }

            if (user.BigPlatesText != null)
            {
                for (int i = 0; i < user.BigPlatesText.Length; i++)
                {
                    string text = user.BigPlatesText[i];
                    Guard(uid, "big nameplate #" + i, () => CreateBigPlate(entry, content, canvas, text));
                }
            }

            entry.ApplyOverlay(s_overlay);
            return entry;
        }

        private static void Guard(string uid, string what, Action action)
        {
            try { action(); }
            catch (Exception e) { MelonLogger.Msg(ConsoleColor.DarkRed, $"Failed To Create {what} on {uid}: {e.Message}"); }
        }

        private static void CreateDefaultPlate(TagEntry entry, Transform content, Transform parent, Color32 color)
        {
            GameObject go = CloneContent(entry, content, parent, DefaultName);
            go.transform.localPosition = new Vector3(0f, -0.155f, 0f);
            go.transform.localScale = new Vector3(0.3f, 0.3f, 1f);

            StripImageComponents(go.transform, false);
            StripUsernameComponents(go.transform, false);

            TMP_Text tmp = ConfigureText(entry, go.transform, DefaultPlateText);

            // Hide the vanilla dev/mod text under the nameplate (cleaner look)
            Transform dev = content.Find("Disable with Menu");
            if (dev != null) dev.gameObject.SetActive(false);

            go.SetActive(true);
        }

        private static void CreatePlate(TagEntry entry, Transform content, Transform parent, string text, int index)
        {
            if (string.IsNullOrEmpty(text)) return;

            GameObject go = CloneContent(entry, content, parent, PlateName);
            go.transform.localPosition = new Vector3(0f, -0.210f - index * 0.0618f, 0f);
            go.transform.localScale = new Vector3(0.3f, 0.3f, 1f);

            StripImageComponents(go.transform, false);
            StripUsernameComponents(go.transform, false);

            TMP_Text tmp = ConfigureText(entry, go.transform, text);

            // Pull animation prefixes (@cyln, @letter, @rain, @sr) off the text
            var animations = new List<AnimationType>();
            text = ParseAnimationPrefixes(text, animations);

            if (animations.Count > 0)
            {
                AnimationManager manager = tmp.gameObject.AddComponent<AnimationManager>();
                manager.textMeshPro = tmp;
                foreach (AnimationType type in animations)
                    manager.animationTypes.Add(type);

                tmp.text = text;

                manager.Start();
            }

            go.SetActive(true);
        }

        private static void CreateBigPlate(TagEntry entry, Transform content, Transform canvas, string text)
        {
            if (string.IsNullOrEmpty(text)) return;

            GameObject go = CloneContent(entry, content, canvas, BigPlateName);

            Match size = SizeRegex.Match(text);
            int sizeValue = size.Success ? int.Parse(size.Groups[1].Value) : 20;
            go.transform.localPosition = new Vector3(0f, 0.45f + sizeValue * 0.0075f, 0f);

            StripImageComponents(go.transform, false);
            StripUsernameComponents(go.transform, false);

            TMP_Text tmp = FindUsernameText(go.transform);

            // Pull animation prefixes (@cyln, @letter, @rain, @sr) off the text
            var animations = new List<AnimationType>();
            text = ParseAnimationPrefixes(text, animations);

            if (animations.Count > 0)
            {
                AnimationManager manager = tmp.gameObject.AddComponent<AnimationManager>();
                manager.textMeshPro = tmp;
                foreach (AnimationType type in animations)
                    manager.animationTypes.Add(type);

                tmp.text = text;

                manager.Start();
            }

            tmp.transform.localPosition = Vector3.zero;
            tmp.transform.localScale = new Vector3(0.03f, 0.03f, 0.03f);
            tmp.text = text;
            tmp.autoSizeTextContainer = true;
            tmp.color = BigPlateColor;
            tmp.enableCulling = true;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.material.enableInstancing = true;
            tmp.isOverlay = s_overlay;
            entry.Texts.Add(tmp);

            go.SetActive(true);
        }

        private static GameObject CloneContent(TagEntry entry, Transform content, Transform parent, string name)
        {
            GameObject go = UnityEngine.Object.Instantiate(content.gameObject, parent);
            go.name = name;
            go.layer = TagLayer;
            entry.Objects.Add(go);
            DestroyNamed(go.transform, "Disable with Menu");
            DestroyNamed(go.transform, "FriendsIndicator");
            DestroyNamed(go.transform, "UserImage");
            return go;
        }

        private static void StripImageComponents(Transform root, bool keepGraphic)
        {
            Transform image = root.Find("Image");
            if (image == null) return;

            Component[] comps = image.GetComponents<Component>();

            // Dependents first (Mask etc.), Graphics last, so nothing is removed while something still requires it
            for (int pass = 0; pass < 2; pass++)
            {
                for (int i = 0; i < comps.Length; i++)
                {
                    Component c = comps[i];
                    if (c == null || c is Transform || c is CanvasRenderer) continue;

                    bool isGraphic = c is Graphic;
                    if (isGraphic && keepGraphic) continue;
                    if (isGraphic != (pass == 1)) continue;

                    UnityEngine.Object.Destroy(c);
                }
            }
        }

        private static void StripUsernameComponents(Transform root, bool keepGraphic)
        {
            Transform username = root.Find("TMP:Username");
            if (username == null) return;

            Component[] comps = username.GetComponents<Component>();

            // Dependents first (Mask etc.), Graphics last, so nothing is removed while something still requires it
            for (int pass = 0; pass < 2; pass++)
            {
                for (int i = 0; i < comps.Length; i++)
                {
                    Component c = comps[i];
                    if (c == null || c is Transform || c is CanvasRenderer || c is TMP_Text || c is TextMeshProUGUI) continue;

                    bool isGraphic = c is Graphic;
                    if (isGraphic && keepGraphic) continue;
                    if (isGraphic != (pass == 1)) continue;

                    UnityEngine.Object.Destroy(c);
                }
            }
        }

        private static TMP_Text ConfigureText(TagEntry entry, Transform root, string text)
        {
            TMP_Text tmp = FindUsernameText(root);
            tmp.transform.localScale = new Vector3(0.58f, 0.58f, 1f);
            tmp.transform.localPosition = Vector3.zero;
            tmp.GetComponent<RectTransform>().anchoredPosition = new Vector2(-0.05f, 0f);

            tmp.text = text;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.autoSizeTextContainer = true;
            tmp.enableCulling = true;
            tmp.material.enableInstancing = true;
            tmp.isOverlay = s_overlay;

            entry.Texts.Add(tmp);
            return tmp;
        }

        // Searches all descendants, so it works whether "TMP:Username" sits under Image or directly under Content.
        private static TMP_Text FindUsernameText(Transform root)
        {
            TMP_Text[] all = root.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < all.Length; i++)
                if (all[i].name == "TMP:Username") return all[i];
            if (all.Length > 0) return all[0];
            throw new InvalidOperationException("No TMP text found in cloned nameplate");
        }

        private static void DestroyNamed(Transform root, string name)
        {
            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != root && all[i].name == name)
                {
                    UnityEngine.Object.Destroy(all[i].gameObject);
                    return;
                }
            }
        }

        private static string ParseAnimationPrefixes(string text, List<AnimationType> found)
        {
            bool matched;
            do
            {
                matched = false;
                foreach (KeyValuePair<string, AnimationType> tag in AnimationTags)
                {
                    if (text.StartsWith(tag.Key, StringComparison.Ordinal))
                    {
                        if (!found.Contains(tag.Value)) found.Add(tag.Value);
                        text = text.Substring(tag.Key.Length);
                        matched = true;
                    }
                }
            } while (matched);
            return text;
        }

        private static float VisibleLength(string richText)
        {
            return RichTextRegex.Replace(richText, string.Empty).Length;
        }

        private static byte ClampByte(int v) { return (byte)Mathf.Clamp(v, 0, 255); }

        private static Color32 ToColor32(int[] c)
        {
            if (c == null || c.Length < 3) return new Color32(255, 255, 255, 255);
            return new Color32(ClampByte(c[0]), ClampByte(c[1]), ClampByte(c[2]), c.Length > 3 ? ClampByte(c[3]) : (byte)255);
        }
    }
}