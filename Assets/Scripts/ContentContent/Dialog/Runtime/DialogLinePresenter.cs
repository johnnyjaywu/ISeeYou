using System;
using System.Collections;
using ContentContent.Audio;
using UnityEngine;

namespace ContentContent.Dialog
{
    public class DialogLinePresenter : MonoBehaviour
    {
        private DialogLine currentLine;
        private bool isSkipping;
        private CoroutineHandle monitorRoutine;
        private CoroutineHandle playbackRoutine;
        private SoundHandle soundHandle;
        private DialogView view;

        public bool WaitForInput { get; set; } = true;
        public bool IsBusy => playbackRoutine is { IsRunning: true } || soundHandle is { IsPlaying: true };

        private void Awake()
        {
            view = GetComponent<DialogView>();
        }

        public event Action PresentingStarted;
        public event Action PresentingFinished;

        public DialogLinePresenter Present(DialogLine line, bool animateOpen = true, Action onLineFinished = null)
        {
            // Stop any running routines
            Stop(false);

            currentLine = line;

            // Set up the view
            SpeakerProfile profile = DialogSettings.GetProfile(line.speakerID);
            view.SetSpeakerName(profile != null && !profile.speakerName.IsNullOrEmpty()
                ? profile.speakerName
                : line.speakerID);
            view.SetSpeakerPortrait(profile?.GetPortrait(line.portraitID));

            view.Open(animateOpen);

            playbackRoutine = PlaybackRoutine().Run();
            monitorRoutine = MonitorPlayback(onLineFinished).Run();
            PresentingStarted?.Invoke();
            return this;
        }

        public void FastForward()
        {
            isSkipping = true;
        }

        public void Stop(bool closeView = true, bool animateClose = true, Action onClosed = null)
        {
            if (playbackRoutine != null)
            {
                playbackRoutine.Stop();
                playbackRoutine = null;
            }

            if (monitorRoutine != null)
            {
                monitorRoutine.Stop();
                monitorRoutine = null;
            }

            currentLine = null;
            isSkipping = false;
            soundHandle = null;

            if (closeView)
                view.Close(animateClose, onClosed);
        }

        private IEnumerator PlaybackRoutine()
        {
            SoundData voiceLine = DialogSettings.GetVoiceLine(currentLine.voiceLineID);
            if (voiceLine != null) soundHandle = SoundManager.Instance.Play(voiceLine);

            string fullText = currentLine.text;
            view.SetText(fullText);
            view.SetVisibleCharacters(0);

            float secondsPerCharacter = view.Style?.typeSpeed ?? 0.05f;
            if (secondsPerCharacter <= 0)
                view.SetVisibleCharacters(fullText.Length);
            else
                for (var i = 0; i <= fullText.Length; i++)
                {
                    if (isSkipping)
                    {
                        view.SetVisibleCharacters(fullText.Length);
                        break;
                    }

                    view.SetVisibleCharacters(i + 1);
                    yield return new WaitForSeconds(secondsPerCharacter);
                }

            currentLine = null;
            playbackRoutine = null;
        }

        private IEnumerator MonitorPlayback(Action onLineFinished = null)
        {
            yield return new WaitWhile(() => IsBusy);

            onLineFinished?.Invoke();
            PresentingFinished?.Invoke();

            monitorRoutine = null;
        }

        public float GetEstimatedReadingTime()
        {
            return currentLine != null ? currentLine.text.Length * 0.1f : 0;
        }

        public float GetEstimatedPlaybackTime()
        {
            if (currentLine == null) return 0;

            SoundData voiceLine = DialogSettings.GetVoiceLine(currentLine.voiceLineID);

            // 1. Get Settings (Mirroring Defaults in DialogLinePresenter)
            float speed = view.Style?.typeSpeed ?? 0.05f;

            // Typing Time
            float typingTime = currentLine.text.Length * speed;

            // Audio Time
            float audioTime = voiceLine != null ? voiceLine.GetDurationRange().y : 0;

            // Take the longer of the two
            return Mathf.Max(typingTime, audioTime);
        }
    }

    public static class DialogLinePresenterExtensions
    {
        public static IEnumerator WaitAutoContinue(this DialogLinePresenter linePresenter, float extraDelay = 0)
        {
            if (linePresenter == null) yield break;

            // Get the reading buffer time before the line is finished
            float readingBuffer = Mathf.Max(0,
                linePresenter.GetEstimatedReadingTime() - linePresenter.GetEstimatedPlaybackTime());

            yield return new WaitWhile(() => linePresenter.IsBusy);

            // Combine it with the passed in extra delay
            float finalDelay = Mathf.Max(1, readingBuffer + extraDelay); // Wait at least 1 second
            yield return new WaitForSeconds(finalDelay);
        }

        public static IEnumerator WaitForInput(this DialogLinePresenter linePresenter, Func<bool> skipCondition,
            Func<bool> continueCondition)
        {
            if (linePresenter == null) yield break;

            // Phase 1: While typing/audio is playing
            while (linePresenter.IsBusy)
            {
                // Check if user wants to skip
                if (skipCondition != null && skipCondition.Invoke()) linePresenter.FastForward();

                yield return null;
            }

            // Phase 2: Buffer Frame
            // Crucial: Wait 1 frame so the button press that triggered 'Skip' 
            // doesn't immediately trigger 'Advance' in the same frame.
            yield return null;

            // Phase 3: Wait for user to advance
            if (continueCondition != null) yield return new WaitUntil(continueCondition);

            // Buffer 2: Prevent "Advance" input from triggering "Skip" on the NEXT line
            // By waiting one frame here, we ensure the 'PlayDialog' loop cannot 
            // start the next line's logic until the button press frame has passed.
            yield return null;
        }
    }
}