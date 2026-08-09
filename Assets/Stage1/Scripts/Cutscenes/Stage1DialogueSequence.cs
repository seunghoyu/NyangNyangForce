using System;
using UnityEngine;

public enum Stage1DialogueSpeaker
{
    Player,
    Boss
}

public readonly struct Stage1DialogueLine
{
    public readonly Stage1DialogueSpeaker Speaker;
    public readonly string Text;

    public Stage1DialogueLine(Stage1DialogueSpeaker speaker, string text)
    {
        Speaker = speaker;
        Text = text;
    }
}

/// <summary>
/// Stage 1 컷신의 대사 진행 상태만 담당한다.
/// 화면 배치와 실제 연출은 Stage1Game에 남겨 전투 흐름과 입력 상태가 섞이지 않게 한다.
/// </summary>
public sealed class Stage1DialogueSequence
{
    private Stage1DialogueLine[] lines;
    private float characterInterval;
    private float lineHoldSeconds;
    private int lineIndex;
    private int visibleCharacters;
    private float characterTimer;
    private float lineCompleteAt;

    public bool IsActive { get; private set; }
    public event Action<Stage1DialogueLine, char> CharacterRevealed;
    public Stage1DialogueLine CurrentLine =>
        lines != null && lineIndex >= 0 && lineIndex < lines.Length
            ? lines[lineIndex]
            : default;
    public string VisibleText
    {
        get
        {
            string text = CurrentLine.Text ?? string.Empty;
            return text.Substring(0, Mathf.Min(visibleCharacters, text.Length));
        }
    }

    public void Begin(Stage1DialogueLine[] dialogueLines, float typingInterval, float holdSeconds)
    {
        lines = dialogueLines;
        characterInterval = Mathf.Max(0.001f, typingInterval);
        lineHoldSeconds = Mathf.Max(0f, holdSeconds);
        lineIndex = 0;
        visibleCharacters = 0;
        characterTimer = 0f;
        lineCompleteAt = 0f;
        IsActive = lines != null && lines.Length > 0;
    }

    public void Tick(float deltaTime, float currentTime)
    {
        if (!IsActive) return;

        string text = CurrentLine.Text ?? string.Empty;
        if (visibleCharacters < text.Length)
        {
            characterTimer += deltaTime;
            while (characterTimer >= characterInterval && visibleCharacters < text.Length)
            {
                characterTimer -= characterInterval;
                char character = text[visibleCharacters];
                visibleCharacters++;
                CharacterRevealed?.Invoke(CurrentLine, character);
            }

            if (visibleCharacters >= text.Length)
                lineCompleteAt = currentTime;
            return;
        }

        if (currentTime - lineCompleteAt >= lineHoldSeconds)
            Advance(currentTime);
    }

    /// <summary>
    /// 기존 프롤로그와 동일한 Space 동작: 타이핑 중이면 현재 줄 완성,
    /// 이미 완성된 줄이면 다음 줄로 진행한다.
    /// </summary>
    public void Advance(float currentTime)
    {
        if (!IsActive) return;

        string text = CurrentLine.Text ?? string.Empty;
        if (visibleCharacters < text.Length)
        {
            visibleCharacters = text.Length;
            lineCompleteAt = currentTime;
            return;
        }

        lineIndex++;
        if (lines == null || lineIndex >= lines.Length)
        {
            IsActive = false;
            return;
        }

        visibleCharacters = 0;
        characterTimer = 0f;
        lineCompleteAt = 0f;
    }

    public void Stop()
    {
        IsActive = false;
    }
}
