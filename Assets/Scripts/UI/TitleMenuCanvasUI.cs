using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using ElementalHexTactics3D.Combat;
using ElementalHexTactics3D.Turn;
using ElementalHexTactics3D.CameraControl;

namespace ElementalHexTactics3D.UI
{
    /// <summary>
    /// UGUI 2D Canvas controller for the Game Title Screen, Story Prologue modal,
    /// How To Play guide, Options modal, and In-Game Pause menu.
    /// Title: "I Reincarnated as a Beast Talker, Now I'm Collecting Beasts!"
    /// </summary>
    public class TitleMenuCanvasUI : MonoBehaviour
    {
        public static TitleMenuCanvasUI Instance { get; private set; }

        [Header("Root Panels")]
        [SerializeField] private GameObject titlePanel;
        [SerializeField] private GameObject inGameHudPanel;
        [SerializeField] private GameObject storyModal;
        [SerializeField] private GameObject howToPlayModal;
        [SerializeField] private GameObject optionsModal;
        [SerializeField] private GameObject pauseModal;

        [Header("Main Menu Buttons")]
        [SerializeField] private Button btnPlay;
        [SerializeField] private Button btnStory;
        [SerializeField] private Button btnHowToPlay;
        [SerializeField] private Button btnOptions;
        [SerializeField] private Button btnExit;

        [Header("In-Game & Pause Buttons")]
        [SerializeField] private Button btnInGameMenu;
        [SerializeField] private Button btnResume;
        [SerializeField] private Button btnPauseOptions;
        [SerializeField] private Button btnRestart;
        [SerializeField] private Button btnReturnTitle;

        [Header("Modal Back Buttons")]
        [SerializeField] private Button btnBackStory;
        [SerializeField] private Button btnBackHowToPlay;
        [SerializeField] private Button btnBackOptions;

        [Header("Options Controls")]
        [SerializeField] private Button btnAudioToggle;
        [SerializeField] private Text txtAudioStatus;
        [SerializeField] private Button btnAiToggle;
        [SerializeField] private Text txtAiStatus;
        [SerializeField] private Button btnDisplayToggle;
        [SerializeField] private Text txtDisplayStatus;

        // State Tracking
        private bool isInGame = false;
        private bool isPaused = false;
        private GameObject activeModal = null;
        private bool returnToPauseFromOptions = false;

        public bool IsInGame => isInGame;
        public bool IsPaused => isPaused || activeModal != null;
        public bool IsOnTitleScreen => !isInGame && activeModal == null;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }
        }

        private void Start()
        {
            BindButtonEvents();
            UpdateOptionsDisplay();

            // Default initial state: On Title Screen
            ShowTitleScreen();
        }

        private void Update()
        {
            // Global Escape key
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                HandleEscapeKey();
            }
        }

        private void BindButtonEvents()
        {
            if (btnPlay != null) btnPlay.onClick.AddListener(OnPlayClicked);
            if (btnStory != null) btnStory.onClick.AddListener(() => OpenModal(storyModal));
            if (btnHowToPlay != null) btnHowToPlay.onClick.AddListener(() => OpenModal(howToPlayModal));
            if (btnOptions != null) btnOptions.onClick.AddListener(() => { returnToPauseFromOptions = false; OpenModal(optionsModal); });
            if (btnExit != null) btnExit.onClick.AddListener(OnExitClicked);

            if (btnInGameMenu != null) btnInGameMenu.onClick.AddListener(OpenPauseMenu);
            if (btnResume != null) btnResume.onClick.AddListener(ResumeGame);
            if (btnPauseOptions != null) btnPauseOptions.onClick.AddListener(() => { returnToPauseFromOptions = true; OpenModal(optionsModal); });
            if (btnRestart != null) btnRestart.onClick.AddListener(OnRestartClicked);
            if (btnReturnTitle != null) btnReturnTitle.onClick.AddListener(OnReturnTitleClicked);

            if (btnBackStory != null) btnBackStory.onClick.AddListener(CloseActiveModal);
            if (btnBackHowToPlay != null) btnBackHowToPlay.onClick.AddListener(CloseActiveModal);
            if (btnBackOptions != null) btnBackOptions.onClick.AddListener(CloseActiveModal);

            if (btnAudioToggle != null) btnAudioToggle.onClick.AddListener(ToggleAudio);
            if (btnAiToggle != null) btnAiToggle.onClick.AddListener(ToggleAI);
            if (btnDisplayToggle != null) btnDisplayToggle.onClick.AddListener(ToggleDisplay);
        }

        public void ShowTitleScreen()
        {
            isInGame = false;
            isPaused = false;

            if (titlePanel != null) titlePanel.SetActive(true);
            if (inGameHudPanel != null) inGameHudPanel.SetActive(false);
            CloseAllModals();
        }

        public void OnPlayClicked()
        {
            PlaySoundClick();
            isInGame = true;
            isPaused = false;

            if (titlePanel != null) titlePanel.SetActive(false);
            if (inGameHudPanel != null) inGameHudPanel.SetActive(true);
            CloseAllModals();

            if (TacticalCameraController.Instance != null)
            {
                TacticalCameraController.Instance.ResetToTacticalView();
            }

            if (CombatFeedbackManager.Instance != null)
            {
                CombatFeedbackManager.Instance.ShowBanner(
                    "ADVENTURE BEGINS!", 
                    "Rank-F Tamer enters the tactical dungeon plateau. Defeat the tyrant forces!", 
                    1.4f, 
                    new Color(0.2f, 0.8f, 1.0f)
                );
            }
        }

        public void OpenPauseMenu()
        {
            PlaySoundClick();
            isPaused = true;
            if (pauseModal != null) pauseModal.SetActive(true);
        }

        public void ResumeGame()
        {
            PlaySoundClick();
            isPaused = false;
            if (pauseModal != null) pauseModal.SetActive(false);
            CloseAllModals();
        }

        public void OpenModal(GameObject modal)
        {
            PlaySoundClick();
            CloseAllModals();
            activeModal = modal;
            if (activeModal != null) activeModal.SetActive(true);
        }

        public void CloseActiveModal()
        {
            PlaySoundClick();
            if (activeModal != null)
            {
                activeModal.SetActive(false);
                activeModal = null;
            }

            if (returnToPauseFromOptions && isInGame)
            {
                returnToPauseFromOptions = false;
                OpenPauseMenu();
            }
        }

        private void CloseAllModals()
        {
            if (storyModal != null) storyModal.SetActive(false);
            if (howToPlayModal != null) howToPlayModal.SetActive(false);
            if (optionsModal != null) optionsModal.SetActive(false);
            if (pauseModal != null) pauseModal.SetActive(false);
            activeModal = null;
        }

        public void HandleEscapeKey()
        {
            if (activeModal != null)
            {
                CloseActiveModal();
            }
            else if (isInGame)
            {
                if (isPaused) ResumeGame();
                else OpenPauseMenu();
            }
        }

        public void OnRestartClicked()
        {
            PlaySoundClick();
            ResumeGame();
            if (TurnManager3D.Instance != null)
            {
                TurnManager3D.Instance.RestartBattle();
            }
        }

        public void OnReturnTitleClicked()
        {
            PlaySoundClick();
            ShowTitleScreen();
            if (TurnManager3D.Instance != null)
            {
                TurnManager3D.Instance.RestartBattle();
            }
        }

        public void OnExitClicked()
        {
            PlaySoundClick();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void ToggleAudio()
        {
            if (SoundManager3D.Instance != null)
            {
                SoundManager3D.Instance.IsMuted = !SoundManager3D.Instance.IsMuted;
            }
            PlaySoundClick();
            UpdateOptionsDisplay();
        }

        private void ToggleAI()
        {
            if (TurnManager3D.Instance != null)
            {
                TurnManager3D.Instance.DisableEnemyAI = !TurnManager3D.Instance.DisableEnemyAI;
            }
            PlaySoundClick();
            UpdateOptionsDisplay();
        }

        private void ToggleDisplay()
        {
            Screen.fullScreen = !Screen.fullScreen;
            PlaySoundClick();
            UpdateOptionsDisplay();
        }

        private void UpdateOptionsDisplay()
        {
            if (txtAudioStatus != null)
            {
                bool isMuted = SoundManager3D.Instance != null && SoundManager3D.Instance.IsMuted;
                txtAudioStatus.text = isMuted ? "Sound: MUTED" : "Sound: ON";
            }

            if (txtAiStatus != null)
            {
                bool aiOff = TurnManager3D.Instance != null && TurnManager3D.Instance.DisableEnemyAI;
                txtAiStatus.text = aiOff ? "Enemy AI: DISABLED (Testing)" : "Enemy AI: ENABLED (Normal)";
            }

            if (txtDisplayStatus != null)
            {
                txtDisplayStatus.text = Screen.fullScreen ? "Display: Fullscreen" : "Display: Windowed";
            }
        }

        private void PlaySoundClick()
        {
            if (SoundManager3D.Instance != null)
            {
                SoundManager3D.Instance.PlayButtonClick();
            }
        }
    }
}

