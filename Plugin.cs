using Menu;
using RWCustom;
using BepInEx;
using BepInEx.Logging;
using UnityEngine;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.ComponentModel;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using AssetBundles;
using HUD;
using Music;
using System.IO;
using System.Drawing;

namespace RWMusicPlayerMain {

    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    public class RWMusicPlayerMod : BaseUnityPlugin
    {
        public const string PLUGIN_GUID = "enerispro.rwmusicplayer";
        public const string PLUGIN_NAME = "Rain World Music Player";
        public const string PLUGIN_VERSION = "1.0.1";
        public bool isInit;
        public static bool songPlaying = false;
        public static bool playMusicInGame = true;
        public static global::ProcessManager.ProcessID MusicPlayerProcessID = new global::ProcessManager.ProcessID("MusicPlayer", true);
        private AudioSystem audioSystem;
        public void OnEnable()
        {
            /* This is called when the mod is loaded. */

            if (isInit) // If the mod has been initialised, return from subroutine.
                return; // This is needed to avoid double-subscribing the hooked function after a hot reload of the code with a tool such as RainReloader.
            isInit = true;

            // subscribe your PlayerUpdateHook to the Player.Update method from the game
            //Hooks
            try
            {
                //On.ProcessManager.RequestMainProcessSwitch_ProcessID += ProcessManager_RequestMainProcessSwitch_ProcessID;
                On.ProcessManager.PostSwitchMainProcess += ProcessManager_PostSwitchMainProcess;
                On.Menu.MainMenu.ctor += MainMenu_ctor;
                On.Menu.MainMenu.Singal += MainMenu_Singal;
                On.Menu.MainMenu.Update += MainMenu_Update;
                On.RainWorldGame.Update += RainWorldGame_Update;
                On.Player.Update += PlayerUpdateHook;
                On.Music.MusicPlayer.Update += MusicPlayer_Update;
                UnityEngine.Debug.Log("Music Player process registered");
                audioSystem = new AudioSystem();
            }
            catch (Exception ex)
            {

                UnityEngine.Debug.LogError($"Error in OnEnable: {ex}");
            }

        }

        public void OnDisable()
        {
            /* This is called when the mod is unloaded.
             * As this is possible only with a hot reload tool, this is done solely to ensure compatibility with them.
             */

            if (!isInit) // If there was an error in initialisation, it may be false.
                return;
            isInit = false;

            // unsubscribes PlayerUpdateHook from Player.Update, ensuring it won't have any effect with the mod disabled
            On.Player.Update -= PlayerUpdateHook;
            On.RainWorldGame.Update -= RainWorldGame_Update;
            On.Menu.MainMenu.Update -= MainMenu_Update;
            On.Music.MusicPlayer.Update -= MusicPlayer_Update;
            On.Menu.MainMenu.ctor -= MainMenu_ctor;
            On.Menu.MainMenu.Singal -= MainMenu_Singal;
            //On.ProcessManager.RequestMainProcessSwitch_ProcessID -= ProcessManager_RequestMainProcessSwitch_ProcessID;
            On.ProcessManager.PostSwitchMainProcess -= ProcessManager_PostSwitchMainProcess;
        }

        private void ProcessManager_RequestMainProcessSwitch_ProcessID(On.ProcessManager.orig_RequestMainProcessSwitch_ProcessID orig, ProcessManager self, ProcessManager.ProcessID ID)
        {
            // handles something
        }
        private void ProcessManager_PostSwitchMainProcess(On.ProcessManager.orig_PostSwitchMainProcess orig, global::ProcessManager self, global::ProcessManager.ProcessID ID)
		{
			bool flag = ID == RWMusicPlayerMod.MusicPlayerProcessID;
			if (flag)
			{
				self.currentMainLoop = new MusicPlayerMenu(self);
			}
            orig(self, ID);
        }



        void PlayerUpdateHook(On.Player.orig_Update orig, Player self, bool eu)
        {
            // Whenever Player.Update gets called by the game, it takes a detour into your code here instead.
            // Do anything that you need to happen when the player updates in here.
            orig(self, eu); // Then, use this to to tell the game that it needs to run the normal code (and other mods' hooks) now.

            // And optionally, you can have more code here too, after orig
            // In general, you will want to always try to call orig
            // If you skip calling orig, this prevents all other mods from running their own hooks
            // and it also stops the game from doing the vanilla behavior.

        }

        private void MusicPlayer_Update(On.Music.MusicPlayer.orig_Update orig, MusicPlayer self)
        {
            orig(self);
            audioSystem.checkSong(self);
        }

        private void MainMenu_ctor(On.Menu.MainMenu.orig_ctor orig, MainMenu self, ProcessManager manager, bool showRegion)
        {
            // Call original constructor first
            orig(self, manager, showRegion);

            // Add our button to the main page
            Vector2 buttonPos = new Vector2(683f, 250f);
            MusicPlayerButton myButton = new MusicPlayerButton(
                self,
                self.pages[0],
                "Music Player",
                "musicplayerSignal",
                buttonPos,
                new Vector2(110f, 30f)
            );

            self.AddMainMenuButton(myButton, () =>
            {
                // This code will run when the button is clicked
                UnityEngine.Debug.Log("Music Player Clicked.");
                //MusicPlayerProcess musicProcess = new MusicPlayerProcess(self.manager);
                self.manager.RequestMainProcessSwitch(MusicPlayerProcessID);
                self.PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);
            }, self.mainMenuButtons.Count - 2);
        }

        private void MainMenu_Update(On.Menu.MainMenu.orig_Update orig, MainMenu self)
        {
            orig(self);
        }

        private void RainWorldGame_Update(On.RainWorldGame.orig_Update orig, RainWorldGame self)
        {
            orig(self);
            if (playMusicInGame)
            {
                audioSystem.SetCurrentProcess(self);
                MusicPlayer musicPlayer = audioSystem.GetMusicPlayer();
                MainLoopProcess currProcess = audioSystem.currentProcess;
                Song mysong = null;

                if (musicPlayer.song == null)
                {
                    songPlaying = false;
                }
                else
                {
                    songPlaying = true;
                }

                if (!songPlaying)
                {
                    //can play custom song
                    mysong = audioSystem.LoadSong(musicPlayer, "OnePieceOP26", (ulong)currProcess.myTimeStacker);
                    audioSystem.latest_song_requested = mysong.name;
                    audioSystem.PlaySong(musicPlayer, mysong, Time.time, 10f);
                    audioSystem.LogMusicInfo();
                }

                /*if (!songsLoaded && mysong == null)
                {
                    mysong = audioSystem.LoadSong(musicPlayer, "OnePieceOP26", (ulong)currProcess.myTimeStacker);
                    audioSystem.latest_song_requested = mysong.name;
                    UnityEngine.Debug.Log($"loaded ionknow {audioSystem.latest_song_requested}");
                    audioSystem.PlaySong(musicPlayer, mysong, Time.time, 10f);
                    audioSystem.LogMusicInfo();
                    songsLoaded = true;
                }
                else
                {
                    audioSystem.LogMusicInfo();
                    if (musicPlayer.song != mysong || musicPlayer.nextSong != mysong)
                    {
                        //audioSystem.PlaySong(musicPlayer, mysong, Time.time, 10f);
                        audioSystem.LogMusicInfo();
                    }
                }*/
            }
        }

        private void MainMenu_Singal(On.Menu.MainMenu.orig_Singal orig, MainMenu self, MenuObject sender, string message)
        {
            /*if (message == "musicplayerSignal")
            {
                Logger.LogInfo("Music Player Clicked.");
                self.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.MainMenu);
                self.PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);
                return; // Don't call original method
            }*/

            // For all other signals, call original method
            orig(self, sender, message);
        }

        private void OnDestroy()
        {
            On.Player.Update -= PlayerUpdateHook;
            On.Menu.MainMenu.ctor -= MainMenu_ctor;
            On.Menu.MainMenu.Singal -= MainMenu_Singal;
            On.Menu.MainMenu.Update -= MainMenu_Update;
            On.RainWorldGame.Update -= RainWorldGame_Update;
            On.Music.MusicPlayer.Update -= MusicPlayer_Update;
            //On.ProcessManager.RequestMainProcessSwitch_ProcessID -= ProcessManager_RequestMainProcessSwitch_ProcessID;
            On.ProcessManager.PostSwitchMainProcess -= ProcessManager_PostSwitchMainProcess;
        }

    }

    public class MusicPlayerButton : SimpleButton
    {
        public MusicPlayerButton(MainMenu menu, MenuObject owner, string displayText, string singalText, Vector2 pos, Vector2 size)
            : base(menu, owner, displayText, singalText, pos, size)
        {
            this.labelColor = new HSLColor(0.9f, 0.7f, 0.3f);
        }
    }

    public class AtlasLoader
    {
        public static void LoadTexture(string fileName)
        {
            var atlas = Futile.atlasManager;
            string picFolder = "rwmusicplayer/pictures";
            string fullPath = Path.Combine(picFolder, fileName);
            FAtlas bgAtlas = atlas.LoadAtlas("mods/rwmusicplayer/pictures/titlescreen");
            FAtlas singleImage = atlas.LoadImage(fullPath);
        }
    }
    public class AudioSystem
    {
        public MainLoopProcess currentProcess;
        public string latest_song_requested = "";
        private string lastLoggedSong = "";
        private string songtoavoid = "";
        private bool UpdateIntensity;
        public float? vibeIntensity = null;
        public float defaultMusicVolume = 0.5f;
        private float? timeleftofsong;

        public void SetCurrentProcess(MainLoopProcess process)
        {
            currentProcess = process;
        }
        public class AudioData
        {
            public string providedSong;
            public float startedPlayingAt;
        }

        public AudioData audioData = new AudioData();

        private float elapsedTime()
        {
            return (float)((ulong)currentProcess.myTimeStacker / (ulong)(long)currentProcess.framesPerSecond);
        }

        public MusicPlayer GetMusicPlayer()
        {
            MusicPlayer musicPlayer = null;
            try
            {
                musicPlayer = currentProcess.manager.musicPlayer;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"Couldn't get music player: {ex} Maybe currentProcess is null?");
            }
            return musicPlayer;
        }

        public void checkSong(MusicPlayer musicPlayer)
        {
            if (musicPlayer.song == null)
            {
                UnityEngine.Debug.Log($"Current song is null");
            }
            else
            {
                UnityEngine.Debug.Log($"Current song is {musicPlayer.song.name}");
            }
            if (musicPlayer.nextSong == null)
            {
                if (currentProcess != null)
                {
                    /*UnityEngine.Debug.Log($"Next song is null");
                    Song mysong = LoadSong(musicPlayer, "OnePieceOP26", elapsedTime());
                    UnityEngine.Debug.Log("loaded 12");
                    latest_song_requested = mysong.name;
                    UnityEngine.Debug.Log($"loaded ionknow {latest_song_requested}");
                    PlaySong(musicPlayer, mysong, Time.time, 10f);
                    LogMusicInfo();
                    RWMusicPlayerMod.songsLoaded = true;*/
                }
            }
            else
            {
                UnityEngine.Debug.Log($"Next song is {musicPlayer.nextSong.name}");
            }
        }

        public Song LoadSong(MusicPlayer musicPlayer, string providedsong, float? DJstartedat=0f)
        {
            //MusicPlayer musicPlayer = AudioSystem.GetMusicPlayer();
            AudioClip clipclip = null;
            string text = string.Concat(new string[]
            {
                "Music",
                Path.DirectorySeparatorChar.ToString(),
                "Songs",
                Path.DirectorySeparatorChar.ToString(),
                providedsong,
                ".ogg"
            });
            string text2 = AssetManager.ResolveFilePath(text);
            bool flag = text2 != Path.Combine(Custom.RootFolderDirectory(), text.ToLowerInvariant()) && File.Exists(text2);
            if (flag)
            {
                UnityEngine.Debug.Log("It can load the song safetly");
                clipclip = AssetManager.SafeWWWAudioClip("file://" + text2, false, true, AudioType.OGGVORBIS);
            }
            else
            {
                string text3;
                LoadedAssetBundle loadedAssetBundle2 = AssetBundleManager.GetLoadedAssetBundle("music_songs", out text3);
                bool flag2 = loadedAssetBundle2 != null;
                if (flag2)
                {
                    UnityEngine.Debug.Log("Loads the song unsafetly?");
                    clipclip = loadedAssetBundle2.m_AssetBundle.LoadAsset<AudioClip>(providedsong);
                }
            }
            UnityEngine.Debug.Log("loaded 1");
            bool flag3 = clipclip == null;
            Song result;
            if (flag3)
            {
                UnityEngine.Debug.Log("Could not fetch the clip to the requested song " + providedsong);
                result = null;
            }
            else
            {
                UnityEngine.Debug.Log("loaded 2");
                bool willfadein = false;
                bool flag4 = DJstartedat != null;
                if (flag4)
                {
                    float songlength = clipclip.length;
                    bool flag5 = songlength != 0f;
                    if (!flag5)
                    {
                        UnityEngine.Debug.LogError("Couldn't find provided song");
                        songtoavoid = providedsong;
                        return null;
                    }
                    UnityEngine.Debug.Log($"loaded 2.05 {songlength}");
                    UnityEngine.Debug.Log($"loaded 2.1{ DJstartedat.Value}, {elapsedTime()}, {songlength}");
                    float songprogress = (elapsedTime() - DJstartedat.Value) / songlength;
                    UnityEngine.Debug.Log("loaded 2.2");
                    bool flag6 = songprogress < 0.95f;
                    if (!flag6)
                    {
                        UnityEngine.Debug.Log("Music is ending soon, waiting for differently named song");
                        bool flag7 = musicPlayer != null;
                        if (flag7)
                        {
                            Song song2 = musicPlayer.song;
                            if (song2 != null)
                            {
                                song2.FadeOut(20f);
                            }
                            musicPlayer.nextSong = null;
                        }
                        songtoavoid = providedsong;
                        return null;
                    }
                    willfadein = (songprogress > 0.05f);
                }
                UnityEngine.Debug.Log("loaded 3");
                bool flag8 = providedsong == latest_song_requested;
                if (flag8)
                {
                    timeleftofsong = new float?(clipclip.length - ((DJstartedat != null) ? (elapsedTime() - DJstartedat.Value) : 0f));
                }
                bool flag9 = musicPlayer == null;
                if (flag9)
                {
                    audioData.startedPlayingAt = (DJstartedat ?? elapsedTime());
                    audioData.providedSong = providedsong;
                    result = null;
                    UnityEngine.Debug.Log("musicPlayer is null for some reason");
                }
                else
                {
                    UnityEngine.Debug.Log("loaded 4");
                    Song song = new Song(musicPlayer, providedsong, MusicPlayer.MusicContext.Arena) // plays custom music only in arena because of context
                    {
                        priority = 25520764f,
                        stopAtDeath = true,
                        stopAtGate = false,
                        lp = (providedsong == "NA_41 - Random Gods")
                    };
                    bool flag10 = willfadein;
                    if (flag10)
                    {
                        song.fadeInTime = 120f;
                    }
                    MusicPiece.SubTrack sub = song.subTracks[0];
                    sub.source.Pause();
                    sub.source.clip = clipclip;
                    UnityEngine.Debug.Log($"loaded 5 {sub.source.clip}, {sub.source}, {sub}, {song}");
                    sub.isStreamed = true;
                    bool flag11 = !UpdateIntensity;
                    float volumeeeee;
                    if (flag11)
                    {
                        float? num = vibeIntensity;
                        float num2 = 0f;
                        volumeeeee = ((num.GetValueOrDefault() == num2 & num != null) ? defaultMusicVolume : 0f);
                    }
                    else
                    {
                        volumeeeee = Mathf.Pow(1f - vibeIntensity.GetValueOrDefault(), 2.5f) * defaultMusicVolume;
                    }
                    song.baseVolume = volumeeeee;
                    musicPlayer.mainSongMix = ((song.fadeInTime == 0f) ? 1f : 0f);
                    song.volume = song.baseVolume * musicPlayer.mainSongMix;
                    bool optionsReady = sub.piece.musicPlayer.manager.rainWorld.OptionsReady;
                    if (optionsReady)
                    {
                        sub.source.volume = Mathf.Pow(sub.volume * sub.piece.volume * sub.piece.musicPlayer.manager.rainWorld.options.musicVolume, sub.piece.musicPlayer.manager.soundLoader.volumeExponent);
                    }
                    sub.readyToPlay = true;
                    sub.isSynced = false;
                    UnityEngine.Debug.Log($"loaded 6 {sub.source.clip}, {sub.source}, {sub}");
                    song.subTracks[0] = sub;
                    latest_song_requested = song.name;
                    result = song;
                    UnityEngine.Debug.Log($"loaded 7 {song.name}");
                }
            }
            return result;
        }

        public void PlaySong(MusicPlayer musicPlayer, Song song, float time_taken, float? timetobestarted = null)
        {
            UnityEngine.Debug.Log("Playing song: " + ((song != null) ? song.name : null));
            var startedPlayingAt = timetobestarted.Value;
            bool flag = song == null;
            if (flag)
            {
                UnityEngine.Debug.LogError("Song was null");
            }
            else
            {
                UnityEngine.Debug.Log($"loaded 9 {song.name}");
                bool flag2 = song.name != latest_song_requested;
                if (flag2)
                {
                    UnityEngine.Debug.Log("switching song " + song.name + " " + latest_song_requested);
                }
                else
                {
                    UnityEngine.Debug.Log($"loaded 10 {song.name}");
                    //MeadowMusicData musicdata = ((MeadowGameMode)OnlineManager.lobby.gameMode).avatars[0].GetData<MeadowMusicData>();
                    bool flag3 = timetobestarted != null;
                    if (flag3)
                    {
                        audioData.startedPlayingAt = timetobestarted.Value;
                        float calculatedthing = elapsedTime() - timetobestarted.Value;
                        song.subTracks[0].source.time = calculatedthing + (Time.time - time_taken) + 1f + ((musicPlayer.song != null) ? 0.6666667f : 0f);
                        UnityEngine.Debug.Log(string.Concat(new string[]
                        {
                            "Playing from a point ",
                            elapsedTime().ToString(),
                            " ",
                            timetobestarted.Value.ToString(),
                            " which amounts to ",
                            calculatedthing.ToString()
                        }));
                    }
                    else
                    {
                        audioData.startedPlayingAt = elapsedTime();
                    }

                    bool flag4 = musicPlayer.song == null;
                    if (flag4)
                    {
                        musicPlayer.song = song;
                        musicPlayer.song.playWhenReady = true;
                        bool flag5 = musicPlayer.nextSong != null;
                        if (flag5)
                        {
                            musicPlayer.nextSong = null;
                        }
                    }
                    else
                    {
                        bool flag6 = musicPlayer.nextSong != null && (musicPlayer.nextSong.priority >= song.priority || musicPlayer.nextSong.name == song.name);
                        if (flag6)
                        {
                            UnityEngine.Debug.LogError("song collision happened! " + musicPlayer.nextSong.name);
                            return;
                        }
                        musicPlayer.nextSong = song;
                        musicPlayer.nextSong.playWhenReady = false;
                    }
                    UnityEngine.Debug.Log("My song is currently " + audioData.providedSong);
                    UnityEngine.Debug.Log("My song is to be " + song.name);
                }
            }
        }

        public void LogMusicInfo()
        {
            try
            {
                MusicPlayer musicPlayer = GetMusicPlayer();

                // Get current song info with null checks
                string currentSong = "None";
                string nextSong = "None";
                bool isPlaying = false;

                if (musicPlayer.song != null)
                {
                    currentSong = musicPlayer.song.name ?? "Unnamed Song";
                    isPlaying = musicPlayer.song.playWhenReady;
                }

                if (musicPlayer.nextSong != null)
                {
                    nextSong = musicPlayer.nextSong.name ?? "Unnamed Next Song";
                }

                string currentLog = $"{currentSong}|{nextSong}|{isPlaying}";
                if (currentLog != lastLoggedSong)
                {
                    UnityEngine.Debug.Log($"Music Info - Current: {currentSong}, Next: {nextSong}, Playing: {isPlaying}");
                    lastLoggedSong = currentLog;
                }
            }
            catch (Exception ex)
            {
                // Don't log the error to avoid spam, but you can uncomment for debugging:
                UnityEngine.Debug.LogError($"Error in LogMusicInfo: {ex.Message}");
            }
        }
    }

    public class MusicPlayerMenu : Menu.Menu
    {
        private SimpleButton backButton;
        private FLabel titleLabel;
        private FSprite background;

        public MusicPlayerMenu(ProcessManager manager) : base(manager, RWMusicPlayerMod.MusicPlayerProcessID)
        {


            pages.Add(new Menu.Page(this, null, "main", 0));
            //AtlasLoader.LoadTexture("titlescreen.jpg");

            var screenWidth = 1366f;
            var screenHeight = 768f;

            try
            {
                // Create background
                background = new FSprite("pixel");
                background.color = new UnityEngine.Color(0f, 0f, 0f, 0.85f);
                background.scaleX = screenWidth;
                background.scaleY = screenHeight;
                pages[0].Container.AddChild(background);

                // Create title
                titleLabel = new FLabel(Custom.GetFont(), "MUSIC PLAYER");
                titleLabel.SetPosition(683f, 600f);
                titleLabel.color = UnityEngine.Color.white;
                titleLabel.scale = 1.5f;
                pages[0].Container.AddChild(titleLabel);

                // Back button
                var backbtn_size = new Vector2(150f, 40f);
                var backbtn_pos = new Vector2(screenWidth / 2 - backbtn_size[0] / 2, screenHeight / 2 + screenHeight / 10 + backbtn_size[1] / 2);
                backButton = new SimpleButton(this, pages[0], "BACK TO MENU", "back",
                    backbtn_pos, backbtn_size);
                pages[0].subObjects.Add(backButton);

                // Add some music control buttons
                var playbtn_size = new Vector2(80f, 30f);
                var playbtn_pos = new Vector2(screenWidth / 2 - screenWidth / 10 - playbtn_size[0] / 2, screenHeight / 2 - screenHeight / 10 + playbtn_size[1] / 2);
                var playButton = new SimpleButton(this, pages[0], "PLAY", "play",
                    playbtn_pos, playbtn_size);
                pages[0].subObjects.Add(playButton);

                var stopbtn_size = new Vector2(80f, 30f);
                var stopbtn_pos = new Vector2(screenWidth / 2 + screenWidth / 10 - stopbtn_size[0] / 2, screenHeight / 2 - screenHeight / 10 + stopbtn_size[1] / 2);
                var stopButton = new SimpleButton(this, pages[0], "STOP", "stop",
                    stopbtn_pos, stopbtn_size);
                pages[0].subObjects.Add(stopButton);

                UnityEngine.Debug.Log("UI elements created successfully");
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"Error in InitializeUI: {ex}");
            }

        }

        public override void Singal(MenuObject sender, string message)
        {
            try
            {
                UnityEngine.Debug.Log($"MusicPlayerMenu received signal: {message}");

                switch (message)
                {
                    case "back":
                        UnityEngine.Debug.Log("Returning to main menu");
                        manager.RequestMainProcessSwitch(ProcessManager.ProcessID.MainMenu);
                        break;

                    case "play":
                        UnityEngine.Debug.Log("Play button pressed");
                        // Добавьте логику воспроизведения музыки
                        break;

                    case "stop":
                        UnityEngine.Debug.Log("Stop button pressed");
                        // Добавьте логику остановки музыки
                        break;
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"Error in Singal: {ex}");
            }
        }

        public override void Update()
        {
            base.Update();

            // Обработка выхода по Pause button (P/Esc)
            Options.ControlSetup input = manager.rainWorld.options.controls[0];
            if (input.GetButton(3)) //3 - pause button
            {
                manager.RequestMainProcessSwitch(ProcessManager.ProcessID.MainMenu);
                UnityEngine.Debug.Log("Returning to main menu via Pause");
            }
        }

        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            if (background != null && background.isVisible == false)
            {
                background.isVisible = true;
            }
        }

        public override void ShutDownProcess()
        {
            base.ShutDownProcess();
        }
    }
}
