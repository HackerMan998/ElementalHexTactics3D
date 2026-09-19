#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using ElementalHexTactics3D.UI;

namespace ElementalHexTactics3D.Editor
{
    /// <summary>
    /// Automation builder to generate a complete, high-fidelity 2D UGUI Canvas hierarchy
    /// in SampleScene with custom AI-generated fantasy RPG button & panel sprites.
    /// Accessible via menu: "Elemental Hex 3D" -> "Generate 2D Title Canvas".
    /// </summary>
    public static class TitleMenuCanvasBuilder
    {
        private const string SpritesDir = "Assets/Sprites/UI";
        private const string BtnNormalPath = "Assets/Sprites/UI/UI_Button_Normal.png";
        private const string BtnHighlightPath = "Assets/Sprites/UI/UI_Button_Highlight.png";
        private const string PanelFramePath = "Assets/Sprites/UI/UI_Panel_Frame.png";

        [MenuItem("Elemental Hex 3D/Generate 2D Title Canvas", false, 2)]
        public static void GenerateTitleCanvas()
        {
            Debug.Log("<color=#FFD54F><b>[2D UI Builder]</b></color> Configuring UI sprite textures & generating 2D Canvas...");

            // 1. Ensure Sprite Texture Import Settings
            ConfigureSpriteImporter(BtnNormalPath);
            ConfigureSpriteImporter(BtnHighlightPath);
            ConfigureSpriteImporter(PanelFramePath);

            Sprite btnNormal = AssetDatabase.LoadAssetAtPath<Sprite>(BtnNormalPath);
            Sprite btnHighlight = AssetDatabase.LoadAssetAtPath<Sprite>(BtnHighlightPath);
            Sprite panelFrame = AssetDatabase.LoadAssetAtPath<Sprite>(PanelFramePath);

            // 2. Ensure EventSystem exists
            EnsureEventSystem();

            // 3. Remove existing Canvas if present to recreate cleanly
            GameObject existingCanvas = GameObject.Find("Canvas_TitleMenu");
            if (existingCanvas != null)
            {
                Undo.DestroyObjectImmediate(existingCanvas);
            }

            // 4. Create Canvas Root
            GameObject canvasObj = new GameObject("Canvas_TitleMenu");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();
            TitleMenuCanvasUI menuUI = canvasObj.AddComponent<TitleMenuCanvasUI>();

            Font defaultFont = GetCleanFont();

            // 5. Panel: Background Diorama Dimmer
            GameObject dimmerObj = CreateUIPanel("Panel_DioramaDimmer", canvasObj.transform);
            RectTransform dimmerRect = dimmerObj.GetComponent<RectTransform>();
            SetStretchAll(dimmerRect);
            Image dimmerImg = dimmerObj.AddComponent<Image>();
            dimmerImg.color = new Color(0.04f, 0.06f, 0.10f, 0.76f);

            // Top Gold Decorative Stripe
            GameObject stripeObj = CreateUIPanel("TopGoldStripe", dimmerObj.transform);
            RectTransform stripeRect = stripeObj.GetComponent<RectTransform>();
            stripeRect.anchorMin = new Vector2(0f, 1f);
            stripeRect.anchorMax = new Vector2(1f, 1f);
            stripeRect.pivot = new Vector2(0.5f, 1f);
            stripeRect.sizeDelta = new Vector2(0f, 6f);
            stripeRect.anchoredPosition = Vector2.zero;
            Image stripeImg = stripeObj.AddComponent<Image>();
            stripeImg.color = new Color(0.98f, 0.75f, 0.15f, 0.95f);

            // 6. Panel: Title Card (Header)
            GameObject headerObj = CreateUIPanel("Panel_TitleHeader", dimmerObj.transform);
            RectTransform headerRect = headerObj.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0.5f, 1f);
            headerRect.anchorMax = new Vector2(0.5f, 1f);
            headerRect.pivot = new Vector2(0.5f, 1f);
            headerRect.sizeDelta = new Vector2(860f, 210f);
            headerRect.anchoredPosition = new Vector2(0f, -55f);

            Image headerImg = headerObj.AddComponent<Image>();
            if (panelFrame != null)
            {
                headerImg.sprite = panelFrame;
                headerImg.type = Image.Type.Simple;
            }
            else
            {
                headerImg.color = new Color(0.08f, 0.11f, 0.16f, 0.95f);
            }

            // Header Texts
            CreateUIText("Text_Badge", headerObj.transform, "✦ LIGHT NOVEL / MANHWA EDITION • PROLOGUE: RANK-F ✦", defaultFont, 16, FontStyle.Bold, new Color(0.22f, 0.74f, 0.96f), new Vector2(0f, 62f), new Vector2(800f, 24f));
            CreateUIText("Text_Title1", headerObj.transform, "I REINCARNATED AS A BEAST TALKER,", defaultFont, 28, FontStyle.Bold, Color.white, new Vector2(0f, 28f), new Vector2(800f, 36f));
            CreateUIText("Text_Title2", headerObj.transform, "NOW I'M COLLECTING BEASTS!", defaultFont, 32, FontStyle.Bold, new Color(0.98f, 0.75f, 0.15f), new Vector2(0f, -10f), new Vector2(800f, 40f));
            CreateUIText("Text_Subtitle", headerObj.transform, "〜 2.5D Tactical Hex Battle & Primordial Resonance 〜", defaultFont, 16, FontStyle.Italic, new Color(0.72f, 0.78f, 0.86f), new Vector2(0f, -46f), new Vector2(800f, 24f));
            CreateUIText("Text_Version", headerObj.transform, "Elemental Hex Tactics 3D • TGFI Pre-Alpha v0.2.0 • Solo Dev by Neal Sage", defaultFont, 12, FontStyle.Normal, new Color(0.55f, 0.60f, 0.68f), new Vector2(0f, -74f), new Vector2(800f, 20f));

            // 7. Panel: Menu Buttons (Center)
            GameObject menuBoxObj = CreateUIPanel("Panel_MenuButtons", dimmerObj.transform);
            RectTransform menuBoxRect = menuBoxObj.GetComponent<RectTransform>();
            menuBoxRect.anchorMin = new Vector2(0.5f, 0.5f);
            menuBoxRect.anchorMax = new Vector2(0.5f, 0.5f);
            menuBoxRect.pivot = new Vector2(0.5f, 0.5f);
            menuBoxRect.sizeDelta = new Vector2(440f, 360f);
            menuBoxRect.anchoredPosition = new Vector2(0f, -80f);

            VerticalLayoutGroup vlg = menuBoxObj.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 14;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            Button btnPlay = CreateCustomButton("Btn_PlayAdventure", menuBoxObj.transform, "⚔️ PLAY ADVENTURE", defaultFont, 21, btnHighlight != null ? btnHighlight : btnNormal, new Color(1f, 0.85f, 0.25f));
            Button btnStory = CreateCustomButton("Btn_StoryLore", menuBoxObj.transform, "📖 STORY & LORE (Rank-F)", defaultFont, 19, btnNormal, Color.white);
            Button btnHowToPlay = CreateCustomButton("Btn_HowToPlay", menuBoxObj.transform, "🎮 HOW TO PLAY (Tactics)", defaultFont, 19, btnNormal, Color.white);
            Button btnOptions = CreateCustomButton("Btn_Options", menuBoxObj.transform, "⚙️ OPTIONS (Settings)", defaultFont, 19, btnNormal, Color.white);
            Button btnExit = CreateCustomButton("Btn_Exit", menuBoxObj.transform, "🚪 EXIT GAME", defaultFont, 19, btnNormal, new Color(0.95f, 0.45f, 0.45f));

            // 8. Modals Container
            GameObject modalsContainer = CreateUIPanel("Panel_ModalsContainer", canvasObj.transform);
            SetStretchAll(modalsContainer.GetComponent<RectTransform>());

            // Modal: Story Prologue
            GameObject storyModalObj = CreateModalPanel("Modal_StoryPrologue", modalsContainer.transform, "📖 STORY PROLOGUE: THE WEAKEST TAMER'S AWAKENING", defaultFont, panelFrame);
            string storyContent = 
                "<b><color=#38BDF8>[ The Reincarnation ]</color></b>\n" +
                "You were summoned from modern Earth into <b>Terranox</b>, a brutal fantasy realm where nobility and authority belong exclusively to beast summoners.\n\n" +
                "<b><color=#EF5350>[ The Tyrant's Cruelty: Cursed Collars ]</color></b>\n" +
                "Imperial aristocrats treat beasts as disposable war tools, torturing them into obedience with painful <b>Cursed Collars</b>. Having zero physical strength or destructive battle mana, the Adventurer's Guild stamped you as a pathetic <b>Rank-F Trash Tamer</b>.\n\n" +
                "<b><color=#FBBF24>[ The Divine Cheat: Beast Resonance ]</color></b>\n" +
                "Unknown to anyone, you possess the God-Given Cheat: <b>[Beast Talker & Primordial Resonance]</b>. You understand the souls and cries of beasts! When treated with empathy, your beasts awaken dormant elemental powers, physically terraforming the 3D hex earth!\n\n" +
                "<b><color=#34D399>[ Your Mission: Break Chains & Awaken Titans ]</color></b>\n" +
                "Enter dangerous 3D dungeon plateaus, defeat corrupt summoners to shatter their cursed collars, gather ancient <b>Elemental Cores</b>, and hatch apocalyptic Titans!";
            CreateUIText("Text_StoryBody", storyModalObj.transform, storyContent, defaultFont, 15, FontStyle.Normal, new Color(0.85f, 0.88f, 0.94f), new Vector2(0f, 10f), new Vector2(660f, 320f), TextAnchor.UpperLeft);
            Button btnBackStory = CreateCustomButton("Btn_BackStory", storyModalObj.transform, "🔙 BACK TO MENU", defaultFont, 18, btnNormal, Color.white, height: 48);
            SetAnchoredPos(btnBackStory.GetComponent<RectTransform>(), new Vector2(0f, -195f), new Vector2(240f, 48f));

            // Modal: How to Play
            GameObject howToPlayModalObj = CreateModalPanel("Modal_HowToPlay", modalsContainer.transform, "🎮 TACTICAL COMBAT & TERRAFORM GUIDE", defaultFont, panelFrame);
            string howToPlayContent = 
                "<b><color=#38BDF8>1. Camera Controls:</color></b> <b>Q / E</b> Rotate 60° | <b>WASD</b> Pan Diorama | <b>Scroll</b> Zoom in/out\n" +
                "<b><color=#38BDF8>2. Tactical Orders:</color></b> <b>Left-Click</b> to Select Unit / Cast Spell | <b>Right-Click</b> to Cancel\n\n" +
                "<b><color=#FBBF24>3. Dynamic Terraforming (Divinity Style):</color></b>\n" +
                "• 🔥 <b>Fireball:</b> Scorches grass into molten <b>Magma</b> (damages enemies, gives Fire Titan +2 ATK!).\n" +
                "• 💧 <b>Water Blast:</b> Extinguishes lava into <b>Steam Smokescreens</b>; forms water pools.\n" +
                "• 🪨 <b>Earth Spire:</b> Raises high <b>Stone Pillars</b> (creates physical barriers & collision surfaces).\n\n" +
                "<b><color=#EF5350>4. Kinetic Push & Wall Slams (Into the Breach):</color></b>\n" +
                "• 💥 <b>Kinetic Shove:</b> Push enemies 1 hex away. Slamming an enemy into a Stone Pillar, cliff, or another unit triggers <b>💥 -2 HP WALL SLAM damage</b>!\n" +
                "• Shove fragile enemy Tamers into lava or water to neutralize them instantly!\n\n" +
                "<b><color=#34D399>5. Siphon Land & Cataclysm:</color></b>\n" +
                "• ⚡ <b>Siphon:</b> Your Titan drains active lava into Barren Earth to harvest <b>Elemental Cores</b>.\n" +
                "• 🌋 <b>Magma Cataclysm:</b> Spend 3 Cores to trigger a screen-shattering volcanic blast wiping the field!";
            CreateUIText("Text_HowToPlayBody", howToPlayModalObj.transform, howToPlayContent, defaultFont, 14, FontStyle.Normal, new Color(0.85f, 0.88f, 0.94f), new Vector2(0f, 15f), new Vector2(680f, 330f), TextAnchor.UpperLeft);
            Button btnBackHowToPlay = CreateCustomButton("Btn_BackHowToPlay", howToPlayModalObj.transform, "🔙 BACK TO MENU", defaultFont, 18, btnNormal, Color.white, height: 48);
            SetAnchoredPos(btnBackHowToPlay.GetComponent<RectTransform>(), new Vector2(0f, -195f), new Vector2(240f, 48f));

            // Modal: Options
            GameObject optionsModalObj = CreateModalPanel("Modal_Options", modalsContainer.transform, "⚙️ GAME OPTIONS & SETTINGS", defaultFont, panelFrame, width: 560f, height: 420f);
            Button btnAudioToggle = CreateCustomButton("Btn_AudioToggle", optionsModalObj.transform, "Sound: ON", defaultFont, 18, btnNormal, Color.white, height: 48);
            SetAnchoredPos(btnAudioToggle.GetComponent<RectTransform>(), new Vector2(0f, 60f), new Vector2(360f, 48f));
            Text txtAudioStatus = btnAudioToggle.GetComponentInChildren<Text>();

            Button btnAiToggle = CreateCustomButton("Btn_AiToggle", optionsModalObj.transform, "Enemy AI: ENABLED", defaultFont, 18, btnNormal, Color.white, height: 48);
            SetAnchoredPos(btnAiToggle.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(360f, 48f));
            Text txtAiStatus = btnAiToggle.GetComponentInChildren<Text>();

            Button btnDisplayToggle = CreateCustomButton("Btn_DisplayToggle", optionsModalObj.transform, "Display: Windowed", defaultFont, 18, btnNormal, Color.white, height: 48);
            SetAnchoredPos(btnDisplayToggle.GetComponent<RectTransform>(), new Vector2(0f, -60f), new Vector2(360f, 48f));
            Text txtDisplayStatus = btnDisplayToggle.GetComponentInChildren<Text>();

            CreateUIText("Text_AiTip", optionsModalObj.transform, "Tip: You can also toggle Enemy AI during battle with hotkey F1.", defaultFont, 13, FontStyle.Italic, new Color(0.6f, 0.65f, 0.72f), new Vector2(0f, -115f), new Vector2(500f, 25f));
            Button btnBackOptions = CreateCustomButton("Btn_BackOptions", optionsModalObj.transform, "🔙 BACK", defaultFont, 18, btnNormal, Color.white, height: 46);
            SetAnchoredPos(btnBackOptions.GetComponent<RectTransform>(), new Vector2(0f, -165f), new Vector2(220f, 46f));

            // Modal: In-Game Pause
            GameObject pauseModalObj = CreateModalPanel("Modal_InGamePause", modalsContainer.transform, "⏸️ BATTLE PAUSED", defaultFont, panelFrame, width: 440f, height: 380f);
            Button btnResume = CreateCustomButton("Btn_Resume", pauseModalObj.transform, "▶️ RESUME BATTLE", defaultFont, 19, btnHighlight != null ? btnHighlight : btnNormal, new Color(0.98f, 0.75f, 0.15f), height: 48);
            SetAnchoredPos(btnResume.GetComponent<RectTransform>(), new Vector2(0f, 65f), new Vector2(340f, 48f));

            Button btnPauseOptions = CreateCustomButton("Btn_PauseOptions", pauseModalObj.transform, "⚙️ OPTIONS", defaultFont, 18, btnNormal, Color.white, height: 48);
            SetAnchoredPos(btnPauseOptions.GetComponent<RectTransform>(), new Vector2(0f, 8f), new Vector2(340f, 48f));

            Button btnRestart = CreateCustomButton("Btn_Restart", pauseModalObj.transform, "🔄 RESTART BATTLE", defaultFont, 18, btnNormal, new Color(0.98f, 0.65f, 0.25f), height: 48);
            SetAnchoredPos(btnRestart.GetComponent<RectTransform>(), new Vector2(0f, -49f), new Vector2(340f, 48f));

            Button btnReturnTitle = CreateCustomButton("Btn_ReturnTitle", pauseModalObj.transform, "🏠 MAIN MENU", defaultFont, 18, btnNormal, new Color(0.95f, 0.45f, 0.45f), height: 48);
            SetAnchoredPos(btnReturnTitle.GetComponent<RectTransform>(), new Vector2(0f, -106f), new Vector2(340f, 48f));

            // Hide modals by default
            storyModalObj.SetActive(false);
            howToPlayModalObj.SetActive(false);
            optionsModalObj.SetActive(false);
            pauseModalObj.SetActive(false);

            // 9. Panel: In-Game HUD
            GameObject hudPanelObj = CreateUIPanel("Panel_InGameHUD", canvasObj.transform);
            SetStretchAll(hudPanelObj.GetComponent<RectTransform>());

            Button btnInGameMenu = CreateCustomButton("Btn_InGameMenu", hudPanelObj.transform, "⚙️ Menu (Esc)", defaultFont, 15, btnNormal, Color.white, height: 38);
            RectTransform inGameMenuRect = btnInGameMenu.GetComponent<RectTransform>();
            inGameMenuRect.anchorMin = new Vector2(1f, 1f);
            inGameMenuRect.anchorMax = new Vector2(1f, 1f);
            inGameMenuRect.pivot = new Vector2(1f, 1f);
            inGameMenuRect.sizeDelta = new Vector2(140f, 38f);
            inGameMenuRect.anchoredPosition = new Vector2(-20f, -20f);

            hudPanelObj.SetActive(false);

            // 10. Wire references to TitleMenuCanvasUI via SerializedObject
            SerializedObject so = new SerializedObject(menuUI);
            so.FindProperty("titlePanel").objectReferenceValue = dimmerObj;
            so.FindProperty("inGameHudPanel").objectReferenceValue = hudPanelObj;
            so.FindProperty("storyModal").objectReferenceValue = storyModalObj;
            so.FindProperty("howToPlayModal").objectReferenceValue = howToPlayModalObj;
            so.FindProperty("optionsModal").objectReferenceValue = optionsModalObj;
            so.FindProperty("pauseModal").objectReferenceValue = pauseModalObj;

            so.FindProperty("btnPlay").objectReferenceValue = btnPlay;
            so.FindProperty("btnStory").objectReferenceValue = btnStory;
            so.FindProperty("btnHowToPlay").objectReferenceValue = btnHowToPlay;
            so.FindProperty("btnOptions").objectReferenceValue = btnOptions;
            so.FindProperty("btnExit").objectReferenceValue = btnExit;

            so.FindProperty("btnInGameMenu").objectReferenceValue = btnInGameMenu;
            so.FindProperty("btnResume").objectReferenceValue = btnResume;
            so.FindProperty("btnPauseOptions").objectReferenceValue = btnPauseOptions;
            so.FindProperty("btnRestart").objectReferenceValue = btnRestart;
            so.FindProperty("btnReturnTitle").objectReferenceValue = btnReturnTitle;

            so.FindProperty("btnBackStory").objectReferenceValue = btnBackStory;
            so.FindProperty("btnBackHowToPlay").objectReferenceValue = btnBackHowToPlay;
            so.FindProperty("btnBackOptions").objectReferenceValue = btnBackOptions;

            so.FindProperty("btnAudioToggle").objectReferenceValue = btnAudioToggle;
            so.FindProperty("txtAudioStatus").objectReferenceValue = txtAudioStatus;
            so.FindProperty("btnAiToggle").objectReferenceValue = btnAiToggle;
            so.FindProperty("txtAiStatus").objectReferenceValue = txtAiStatus;
            so.FindProperty("btnDisplayToggle").objectReferenceValue = btnDisplayToggle;
            so.FindProperty("txtDisplayStatus").objectReferenceValue = txtDisplayStatus;

            so.ApplyModifiedProperties();

            // Mark Scene Dirty
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("<color=#4CAF50><b>[2D UI Builder] SUCCESS!</b></color> 2D Canvas hierarchy, custom fantasy buttons, and modals created cleanly in scene!");
        }

        private static void ConfigureSpriteImporter(string assetPath)
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                bool dirty = false;
                if (importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    dirty = true;
                }
                if (!importer.alphaIsTransparency)
                {
                    importer.alphaIsTransparency = true;
                    dirty = true;
                }
                if (importer.wrapMode != TextureWrapMode.Clamp)
                {
                    importer.wrapMode = TextureWrapMode.Clamp;
                    dirty = true;
                }
                if (dirty)
                {
                    importer.SaveAndReimport();
                }
            }
        }

        private static void EnsureEventSystem()
        {
            EventSystem es = Object.FindFirstObjectByType<EventSystem>();
            if (es == null)
            {
                GameObject esObj = new GameObject("EventSystem");
                esObj.AddComponent<EventSystem>();
                esObj.AddComponent<InputSystemUIInputModule>();
            }
            else
            {
                // Ensure InputSystemUIInputModule is present for New Input System
                if (es.GetComponent<InputSystemUIInputModule>() == null && es.GetComponent<StandaloneInputModule>() == null)
                {
                    es.gameObject.AddComponent<InputSystemUIInputModule>();
                }
            }
        }

        private static GameObject CreateUIPanel(string name, Transform parent)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.AddComponent<RectTransform>();
            return obj;
        }

        private static Button CreateCustomButton(string name, Transform parent, string label, Font font, int fontSize, Sprite bgSprite, Color textColor, float height = 54f)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);

            RectTransform rect = btnObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(400f, height);

            Image img = btnObj.AddComponent<Image>();
            if (bgSprite != null)
            {
                img.sprite = bgSprite;
                img.type = Image.Type.Simple;
            }
            else
            {
                img.color = new Color(0.12f, 0.16f, 0.22f, 0.95f);
            }

            Button btn = btnObj.AddComponent<Button>();
            ColorBlock colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            colors.selectedColor = Color.white;
            btn.colors = colors;

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            SetStretchAll(textRect);

            Text text = textObj.AddComponent<Text>();
            text.text = label;
            text.font = font;
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = textColor;
            text.raycastTarget = false;

            // Add subtle shadow to button text for crisp visual clarity
            Shadow shadow = textObj.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.9f);
            shadow.effectDistance = new Vector2(1.5f, -1.5f);

            return btn;
        }

        private static GameObject CreateModalPanel(string name, Transform parent, string titleText, Font font, Sprite panelFrame, float width = 740f, float height = 480f)
        {
            GameObject modalRoot = CreateUIPanel(name, parent);
            SetStretchAll(modalRoot.GetComponent<RectTransform>());

            // Modal Dimmer Backdrop
            GameObject backdrop = CreateUIPanel("Backdrop", modalRoot.transform);
            SetStretchAll(backdrop.GetComponent<RectTransform>());
            Image bdImg = backdrop.AddComponent<Image>();
            bdImg.color = new Color(0f, 0f, 0f, 0.82f);

            // Modal Card Frame
            GameObject cardObj = CreateUIPanel("CardFrame", modalRoot.transform);
            RectTransform cardRect = cardObj.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(width, height);
            cardRect.anchoredPosition = Vector2.zero;

            Image cardImg = cardObj.AddComponent<Image>();
            if (panelFrame != null)
            {
                cardImg.sprite = panelFrame;
                cardImg.type = Image.Type.Simple;
            }
            else
            {
                cardImg.color = new Color(0.08f, 0.11f, 0.16f, 0.98f);
            }

            // Header Title Text
            CreateUIText("Text_ModalTitle", cardObj.transform, titleText, font, 20, FontStyle.Bold, new Color(0.98f, 0.75f, 0.15f), new Vector2(0f, (height * 0.5f) - 34f), new Vector2(width - 60f, 32f));

            return cardObj;
        }

        private static Text CreateUIText(string name, Transform parent, string content, Font font, int fontSize, FontStyle style, Color color, Vector2 anchoredPos, Vector2 size, TextAnchor alignment = TextAnchor.MiddleCenter)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent, false);

            RectTransform rect = textObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPos;

            Text text = textObj.AddComponent<Text>();
            text.text = content;
            text.font = font;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            text.supportRichText = true;

            Shadow shadow = textObj.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.95f);
            shadow.effectDistance = new Vector2(1f, -1f);

            return text;
        }

        private static void SetStretchAll(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
        }

        private static void SetAnchoredPos(RectTransform rect, Vector2 pos, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = pos;
        }

        private static Font GetCleanFont()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (font == null)
            {
                Font[] allFonts = Resources.FindObjectsOfTypeAll<Font>();
                if (allFonts != null && allFonts.Length > 0) font = allFonts[0];
            }
            return font;
        }
    }
}
#endif
