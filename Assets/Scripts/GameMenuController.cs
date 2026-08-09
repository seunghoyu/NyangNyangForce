using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public sealed class GameMenuController : MonoBehaviour
{
    private enum View { Closed, Pause, Settings, ConfirmRestart, ConfirmExitStage, ConfirmQuit, GameOver, StageClear }
    private enum SettingsTab { Audio, Keyboard, Screen }

    private static readonly Vector2Int[] Resolutions =
    {
        new Vector2Int(480, 270), new Vector2Int(960, 540),
        new Vector2Int(1280, 720), new Vector2Int(1920, 1080)
    };
    private static readonly string[] GameOverLines =
    {
        "수철햄의 형광펜은 너무 뾰족했다.",
        "수철햄... 고마해라. 마이 묵었다 아이가.",
        "가방은 못 찾고 바닥만 찾았다.",
        "김냥이의 아홉 목숨 중 하나가 로그아웃했다."
    };
    private static readonly string[] StageTwoGameOverLines =
    {
        "코드가 김냥이보다 먼저 도망쳤다.",
        "물은 깊었고 디버그 로그는 짧았다.",
        "이번 오류는 재현이 너무 잘됐다.",
        "김냥이의 컴파일이 잠시 중단되었다."
    };
    private static int previousGameOverLine = -1;

    private View view;
    private SettingsTab settingsTab;
    private string stageScene;
    private bool battleEnded;
    private int selected;
    private int settingsRow;
    private int resolutionIndex = 3;
    private string bindingField;
    private string clearMessage;
    private string rewardName;
    private string nextScene;
    private string gameOverMessage;
    private Texture2D gameOverLoopTexture;

    public bool IsBlockingGameplay => view != View.Closed;

    public void Initialize(string currentStage)
    {
        stageScene = currentStage;
        GameSettingsService.Load();
        for (int i = 0; i < Resolutions.Length; i++)
            if (Resolutions[i].x == GameSettingsService.Data.resolutionWidth && Resolutions[i].y == GameSettingsService.Data.resolutionHeight)
                resolutionIndex = i;
    }

    public void ShowGameOver()
    {
        battleEnded = true;
        view = View.GameOver;
        selected = 0;
        string[] lines = stageScene == "Stage 2" ? StageTwoGameOverLines : GameOverLines;
        int line = previousGameOverLine < 0
            ? Random.Range(0, lines.Length)
            : (previousGameOverLine + Random.Range(1, lines.Length)) % lines.Length;
        previousGameOverLine = line;
        gameOverMessage = lines[line];
        Time.timeScale = 0f;
    }

    public void ShowStageClear(string message, string reward, string destination)
    {
        battleEnded = true;
        clearMessage = message;
        rewardName = reward;
        nextScene = destination;
        view = View.StageClear;
        selected = 0;
        Time.timeScale = 0f;
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;
        if (HandleBindingInput(keyboard)) return;

        if (!battleEnded && keyboard.escapeKey.wasPressedThisFrame)
        {
            if (view == View.Closed) OpenPause();
            else if (view == View.Pause) CloseMenu();
            else if (view == View.Settings) { view = View.Pause; selected = 1; }
            else if (IsConfirmation) { view = View.Pause; selected = 0; }
        }

        if (view == View.Settings)
        {
            HandleSettingsKeyboard(keyboard);
            return;
        }

        int count = view == View.Pause ? 5 : view == View.GameOver ? 2 : view == View.StageClear ? StageClearButtonCount : IsConfirmation ? 2 : 0;
        if (count <= 0) return;
        if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame) selected = (selected - 1 + count) % count;
        if (keyboard.downArrowKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame) selected = (selected + 1) % count;
        if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame)
            ActivateCurrent();
    }

    private bool IsConfirmation => view == View.ConfirmRestart || view == View.ConfirmExitStage || view == View.ConfirmQuit;
    private int StageClearButtonCount => nextScene == "World Map" ? 2 : 3;

    private bool HandleBindingInput(Keyboard keyboard)
    {
        if (bindingField == null) return false;
        foreach (KeyControl key in keyboard.allKeys)
        {
            if (!key.wasPressedThisFrame) continue;
            if (key.keyCode != Key.Escape) SetBinding(bindingField, (int)key.keyCode);
            bindingField = null;
            GameSettingsService.Save();
            return true;
        }
        return true;
    }

    private void ActivateCurrent()
    {
        GameSfx.Play(GameSfxId.Button);
        if (view == View.Pause) ActivatePauseItem(selected);
        else if (view == View.GameOver) { if (selected == 0) ReloadStage(); else ReturnToWorldMap(); }
        else if (view == View.StageClear)
        {
            if (selected == 0) ReloadStage();
            else if (selected == 1) LoadResultDestination();
            else ReturnToWorldMap();
        }
        else if (IsConfirmation)
        {
            if (selected == 0)
            {
                if (view == View.ConfirmQuit) QuitGame();
                else if (view == View.ConfirmRestart) ReloadStage();
                else ReturnToWorldMap();
            }
            else { view = View.Pause; selected = 0; }
        }
    }

    private void OpenPause() { view = View.Pause; selected = 0; Time.timeScale = 0f; }
    private void CloseMenu() { view = View.Closed; Time.timeScale = 1f; }

    private void OnDestroy()
    {
        if (!battleEnded) Time.timeScale = 1f;
    }

    private void OnGUI()
    {
        if (view == View.Closed) return;
        Matrix4x4 backdropMatrix = PixelUiTheme.BeginReferenceCanvas();
        PixelUiTheme.DrawBackdrop();
        PixelUiTheme.EndReferenceCanvas(backdropMatrix);
        float contentScale = view == View.Pause ? 0.8f : view == View.Settings || view == View.GameOver || view == View.StageClear ? 0.5f : 1f;
        Matrix4x4 previous = PixelUiTheme.BeginReferenceCanvas(contentScale);
        if (view == View.Pause) DrawPause();
        else if (view == View.Settings) DrawSettings();
        else if (view == View.GameOver) DrawResult(false);
        else if (view == View.StageClear) DrawResult(true);
        else DrawConfirmation();
        PixelUiTheme.EndReferenceCanvas(previous);
    }

    private void DrawPause()
    {
        Rect panel = new Rect(112f, 18f, 256f, 234f);
        PixelUiTheme.DrawPanel(panel);
        PixelUiTheme.Title(new Rect(panel.x, panel.y + 12f, panel.width, 32f), "일시정지");
        string[] items = { "계속하기", "설정", "처음부터", "스테이지 나가기", "게임 종료" };
        for (int i = 0; i < items.Length; i++)
        {
            Rect row = new Rect(panel.x + 38f, panel.y + 52f + i * 30f, panel.width - 76f, 24f);
            if (row.Contains(Event.current.mousePosition)) selected = i;
            if (PixelUiTheme.Button(row, items[i], selected == i)) { selected = i; ActivatePauseItem(i); }
        }
        PixelUiTheme.Hint(new Rect(panel.x + 12f, panel.yMax - 22f, panel.width - 24f, 14f), "ENTER 선택   ESC 계속하기");
    }

    private void ActivatePauseItem(int index)
    {
        if (index == 0) CloseMenu();
        else if (index == 1) { view = View.Settings; settingsRow = 0; }
        else if (index == 2) { view = View.ConfirmRestart; selected = 1; }
        else if (index == 3) { view = View.ConfirmExitStage; selected = 1; }
        else { view = View.ConfirmQuit; selected = 1; }
    }

    private void DrawConfirmation()
    {
        Rect panel = new Rect(82f, 58f, 316f, 154f);
        PixelUiTheme.DrawPanel(panel);
        string question = view == View.ConfirmQuit ? "게임을 종료할까요?" : view == View.ConfirmRestart ? "현재 스테이지를 처음부터 시작할까요?" : "스테이지를 나가 월드맵으로 이동할까요?";
        PixelUiTheme.Label(new Rect(panel.x + 24f, panel.y + 27f, panel.width - 48f, 44f), question, TextAnchor.MiddleCenter, PixelUiTheme.Text, true);
        Rect yes = new Rect(panel.x + 36f, panel.y + 87f, 108f, 28f);
        Rect no = new Rect(panel.xMax - 144f, panel.y + 87f, 108f, 28f);
        if (yes.Contains(Event.current.mousePosition)) selected = 0;
        if (no.Contains(Event.current.mousePosition)) selected = 1;
        if (PixelUiTheme.Button(yes, "확인", selected == 0)) { selected = 0; ActivateCurrent(); }
        if (PixelUiTheme.Button(no, "취소", selected == 1)) { selected = 1; ActivateCurrent(); }
        PixelUiTheme.Hint(new Rect(panel.x + 12f, panel.yMax - 20f, panel.width - 24f, 14f), "ENTER 선택   ESC 취소");
    }

    private void DrawResult(bool clear)
    {
        Rect panel = new Rect(76f, 34f, 328f, 202f);
        Color emphasis = clear ? PixelUiTheme.Gold : PixelUiTheme.Danger;
        PixelUiTheme.DrawPanel(panel, clear ? PixelUiTheme.Border : PixelUiTheme.Danger);
        PixelUiTheme.Title(new Rect(panel.x, panel.y + 10f, panel.width, 31f), clear ? "STAGE CLEAR" : "GAME OVER", emphasis);
        string message = clear ? (string.IsNullOrEmpty(clearMessage) ? "스테이지를 클리어했습니다!" : clearMessage) : gameOverMessage;
        PixelUiTheme.Label(new Rect(panel.x + 24f, panel.y + 43f, panel.width - 48f, 29f), message, TextAnchor.MiddleCenter, PixelUiTheme.Text, true);
        if (clear && !string.IsNullOrEmpty(rewardName))
        {
            Rect reward = new Rect(panel.x + 42f, panel.y + 73f, panel.width - 84f, 29f);
            PixelUiTheme.DrawInset(reward);
            PixelUiTheme.Label(reward, "획득 보상  " + rewardName, TextAnchor.MiddleCenter, PixelUiTheme.Gold);
        }
        string[] buttons = clear
            ? nextScene == "World Map" ? new[] { "다시 도전", "월드맵 이동" } : new[] { "다시 도전", "다음 스테이지", "월드맵 이동" }
            : new[] { "다시 도전", "월드맵 이동" };
        if (!clear) DrawGameOverLoop(new Rect(panel.center.x - 15f, panel.y + 75f, 30f, 30f));
        float startY = clear ? panel.y + 111f : panel.y + 112f;
        for (int i = 0; i < buttons.Length; i++)
        {
            Rect row = new Rect(panel.x + 66f, startY + i * 27f, panel.width - 132f, 22f);
            if (row.Contains(Event.current.mousePosition)) selected = i;
            if (PixelUiTheme.Button(row, buttons[i], selected == i, clear ? PixelUiTheme.Accent : PixelUiTheme.Danger)) { selected = i; ActivateCurrent(); }
        }
        PixelUiTheme.Hint(new Rect(panel.x + 12f, panel.yMax - 18f, panel.width - 24f, 12f), "ENTER 선택");
    }

    private void DrawGameOverLoop(Rect rect)
    {
        if (gameOverLoopTexture == null)
        {
            gameOverLoopTexture = Resources.Load<Texture2D>("UI/Player/GameOver/gameover_roop");
            if (gameOverLoopTexture != null) gameOverLoopTexture.filterMode = FilterMode.Point;
        }
        if (gameOverLoopTexture == null) return;
        int frame = Mathf.FloorToInt(Time.unscaledTime * 10f) % 4;
        Rect uv = new Rect(frame / 4f, 0f, 0.25f, 1f);
        GUI.DrawTextureWithTexCoords(rect, gameOverLoopTexture, uv, true);
    }

    private void DrawSettings()
    {
        Rect panel = new Rect(18f, 7f, 444f, 256f);
        PixelUiTheme.DrawPanel(panel);
        PixelUiTheme.Title(new Rect(panel.x, panel.y + 5f, panel.width, 28f), "설정");
        string[] tabs = { "오디오", "키보드", "화면" };
        for (int i = 0; i < tabs.Length; i++)
        {
            Rect tab = new Rect(panel.x + 40f + i * 121f, panel.y + 35f, 121f, 22f);
            if (PixelUiTheme.Tab(tab, tabs[i], (int)settingsTab == i)) { settingsTab = (SettingsTab)i; settingsRow = 0; }
        }
        Rect content = new Rect(panel.x + 24f, panel.y + 65f, panel.width - 48f, 151f);
        PixelUiTheme.DrawInset(content);
        if (settingsTab == SettingsTab.Audio) DrawAudioSettings(content);
        else if (settingsTab == SettingsTab.Keyboard) DrawKeyboardSettings(content);
        else DrawScreenSettings(content);
        int contentRows = SettingsContentRowCount;
        Rect defaults = new Rect(panel.x + 31f, panel.yMax - 35f, 100f, 23f);
        Rect back = new Rect(panel.xMax - 131f, panel.yMax - 35f, 100f, 23f);
        if (PixelUiTheme.Button(defaults, "기본값", settingsRow == contentRows)) { settingsRow = contentRows; GameSettingsService.ResetToDefaults(); }
        if (PixelUiTheme.Button(back, "돌아가기", settingsRow == contentRows + 1)) { GameSettingsService.Save(); view = View.Pause; selected = 1; }
        PixelUiTheme.Hint(new Rect(panel.x + 140f, panel.yMax - 33f, panel.width - 280f, 18f), "TAB 탭   ESC 뒤로");
    }

    private int SettingsContentRowCount => settingsTab == SettingsTab.Audio ? 4 : settingsTab == SettingsTab.Keyboard ? 8 : 3;

    private void DrawAudioSettings(Rect content)
    {
        GameSettingsData data = GameSettingsService.Data;
        string[] labels = { "전체 음량", "배경음", "효과음" };
        float[] values = { data.masterVolume, data.musicVolume, data.sfxVolume };
        for (int i = 0; i < 3; i++)
        {
            float y = content.y + 15f + i * 34f;
            PixelUiTheme.Label(new Rect(content.x + 18f, y, 80f, 20f), labels[i], TextAnchor.MiddleLeft, settingsRow == i ? PixelUiTheme.Accent : PixelUiTheme.Text);
            values[i] = PixelUiTheme.Slider(new Rect(content.x + 105f, y + 2f, 190f, 16f), values[i], settingsRow == i);
            PixelUiTheme.Label(new Rect(content.xMax - 72f, y, 54f, 20f), Mathf.RoundToInt(values[i] * 100f) + "%", TextAnchor.MiddleRight);
        }
        data.masterVolume = values[0]; data.musicVolume = values[1]; data.sfxVolume = values[2];
        data.muted = PixelUiTheme.Toggle(new Rect(content.x + 18f, content.y + 116f, 170f, 22f), "전체 음소거", data.muted, settingsRow == 3);
        GameSettingsService.ApplyGlobal();
        AudioSource stageMusic = GetComponent<AudioSource>();
        if (stageMusic != null) stageMusic.volume = 0.55f * data.musicVolume;
    }

    private void DrawKeyboardSettings(Rect content)
    {
        string[] labels = { "왼쪽 이동", "오른쪽 이동", "위 조준", "아래/내려가기", "점프", "대시/내려차기", "앉기", "기본 공격" };
        string[] fields = { "moveLeft", "moveRight", "aimUp", "aimDown", "jump", "dash", "crouch", "attack" };
        for (int i = 0; i < labels.Length; i++)
        {
            int column = i / 4;
            int row = i % 4;
            float x = content.x + 12f + column * 190f;
            float y = content.y + 10f + row * 33f;
            PixelUiTheme.Label(new Rect(x, y, 90f, 20f), labels[i], TextAnchor.MiddleLeft, settingsRow == i ? PixelUiTheme.Accent : PixelUiTheme.Text);
            Rect keyRect = new Rect(x + 94f, y, 80f, 20f);
            string value = bindingField == fields[i] ? "키 입력..." : CompactKeyLabel(GetBinding(fields[i]));
            if (PixelUiTheme.SmallButton(keyRect, value, settingsRow == i)) { settingsRow = i; bindingField = fields[i]; }
        }
    }

    private void DrawScreenSettings(Rect content)
    {
        GameSettingsData data = GameSettingsService.Data;
        data.fullscreen = PixelUiTheme.Toggle(new Rect(content.x + 24f, content.y + 18f, 180f, 24f), "전체 화면", data.fullscreen, settingsRow == 0);
        PixelUiTheme.Label(new Rect(content.x + 24f, content.y + 59f, 80f, 22f), "해상도", TextAnchor.MiddleLeft, settingsRow == 1 ? PixelUiTheme.Accent : PixelUiTheme.Text);
        Vector2Int resolution = Resolutions[resolutionIndex];
        Rect resolutionRect = new Rect(content.x + 116f, content.y + 59f, 150f, 22f);
        if (PixelUiTheme.Button(resolutionRect, resolution.x + " × " + resolution.y, settingsRow == 1)) resolutionIndex = (resolutionIndex + 1) % Resolutions.Length;
        Rect applyRect = new Rect(content.x + 24f, content.y + 103f, 150f, 24f);
        if (PixelUiTheme.Button(applyRect, "화면 설정 적용", settingsRow == 2)) ApplyDisplaySettings();
        PixelUiTheme.Label(new Rect(content.x + 190f, content.y + 99f, content.width - 210f, 35f), "픽셀 퍼펙트 배율을 우선하여 표시합니다.", TextAnchor.MiddleLeft, PixelUiTheme.Border, true);
    }

    private void HandleSettingsKeyboard(Keyboard keyboard)
    {
        if (keyboard.escapeKey.wasPressedThisFrame) { GameSettingsService.Save(); view = View.Pause; selected = 1; return; }
        if (keyboard.tabKey.wasPressedThisFrame) { settingsTab = (SettingsTab)(((int)settingsTab + 1) % 3); settingsRow = 0; return; }
        int total = SettingsContentRowCount + 2;
        if (keyboard.upArrowKey.wasPressedThisFrame) settingsRow = (settingsRow - 1 + total) % total;
        if (keyboard.downArrowKey.wasPressedThisFrame) settingsRow = (settingsRow + 1) % total;
        if (keyboard.leftArrowKey.wasPressedThisFrame) AdjustSetting(-1);
        if (keyboard.rightArrowKey.wasPressedThisFrame) AdjustSetting(1);
        if (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame) ActivateSettingRow();
    }

    private void AdjustSetting(int direction)
    {
        GameSettingsData data = GameSettingsService.Data;
        if (settingsTab == SettingsTab.Audio && settingsRow < 3)
        {
            if (settingsRow == 0) data.masterVolume = Mathf.Clamp01(data.masterVolume + direction * 0.05f);
            if (settingsRow == 1) data.musicVolume = Mathf.Clamp01(data.musicVolume + direction * 0.05f);
            if (settingsRow == 2) data.sfxVolume = Mathf.Clamp01(data.sfxVolume + direction * 0.05f);
            GameSettingsService.ApplyGlobal();
        }
        else if (settingsTab == SettingsTab.Screen && settingsRow == 1)
            resolutionIndex = (resolutionIndex + direction + Resolutions.Length) % Resolutions.Length;
    }

    private void ActivateSettingRow()
    {
        int contentRows = SettingsContentRowCount;
        if (settingsRow == contentRows) { GameSettingsService.ResetToDefaults(); return; }
        if (settingsRow == contentRows + 1) { GameSettingsService.Save(); view = View.Pause; selected = 1; return; }
        if (settingsTab == SettingsTab.Audio && settingsRow == 3) GameSettingsService.Data.muted = !GameSettingsService.Data.muted;
        else if (settingsTab == SettingsTab.Keyboard)
        {
            string[] fields = { "moveLeft", "moveRight", "aimUp", "aimDown", "jump", "dash", "crouch", "attack" };
            bindingField = fields[settingsRow];
        }
        else if (settingsTab == SettingsTab.Screen)
        {
            if (settingsRow == 0) GameSettingsService.Data.fullscreen = !GameSettingsService.Data.fullscreen;
            else if (settingsRow == 1) resolutionIndex = (resolutionIndex + 1) % Resolutions.Length;
            else ApplyDisplaySettings();
        }
    }

    private void ApplyDisplaySettings()
    {
        Vector2Int resolution = Resolutions[resolutionIndex];
        GameSettingsService.Data.resolutionWidth = resolution.x;
        GameSettingsService.Data.resolutionHeight = resolution.y;
        GameSettingsService.Save();
        GameSettingsService.ApplyDisplay();
    }

    private void ReloadStage() => SceneTransition.Load(stageScene);
    private void ReturnToWorldMap() => SceneTransition.Load("World Map");
    private void LoadResultDestination() => SceneTransition.Load(string.IsNullOrEmpty(nextScene) ? "World Map" : nextScene);

    private static void QuitGame()
    {
        Time.timeScale = 1f;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private int GetBinding(string field)
    {
        GameSettingsData data = GameSettingsService.Data;
        if (field == "moveLeft") return data.moveLeft; if (field == "moveRight") return data.moveRight;
        if (field == "aimUp") return data.aimUp; if (field == "aimDown") return data.aimDown;
        if (field == "jump") return data.jump; if (field == "dash") return data.dash;
        if (field == "crouch") return data.crouch; return data.attack;
    }

    private static string CompactKeyLabel(int keyValue)
    {
        Key key = (Key)keyValue;
        if (key == Key.LeftArrow) return "←";
        if (key == Key.RightArrow) return "→";
        if (key == Key.UpArrow) return "↑";
        if (key == Key.DownArrow) return "↓";
        if (key == Key.LeftShift) return "L-SHIFT";
        if (key == Key.RightShift) return "R-SHIFT";
        if (key == Key.Space) return "SPACE";
        return key.ToString();
    }

    private void SetBinding(string field, int value)
    {
        GameSettingsData data = GameSettingsService.Data;
        int previous = GetBinding(field);
        string[] fields = { "moveLeft", "moveRight", "aimUp", "aimDown", "jump", "dash", "crouch", "attack" };
        foreach (string other in fields)
        {
            if (other != field && GetBinding(other) == value) { AssignBinding(other, previous); break; }
        }
        AssignBinding(field, value);
    }

    private static void AssignBinding(string field, int value)
    {
        GameSettingsData data = GameSettingsService.Data;
        if (field == "moveLeft") data.moveLeft = value; else if (field == "moveRight") data.moveRight = value;
        else if (field == "aimUp") data.aimUp = value; else if (field == "aimDown") data.aimDown = value;
        else if (field == "jump") data.jump = value; else if (field == "dash") data.dash = value;
        else if (field == "crouch") data.crouch = value; else data.attack = value;
    }
}
