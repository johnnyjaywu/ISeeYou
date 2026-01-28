using UnityEditor;
using UnityEngine;

namespace ContentContent.Audio.Editor
{
    [CustomEditor(typeof(SoundData))]
    public class SoundDataEditor : UnityEditor.Editor
    {
        private AudioClip cachedClip;
        private SoundData data;

        // Waveform Caching
        private Texture2D waveformTexture;

        private void OnEnable()
        {
            data = (SoundData)target;
        }

        private void OnDisable()
        {
            ClearTexture();
        }

        private void ClearTexture()
        {
            if (waveformTexture != null)
            {
                DestroyImmediate(waveformTexture);
                waveformTexture = null;
            }
        }

        public override void OnInspectorGUI()
        {
            // --- HEADER ---
            GUILayout.BeginHorizontal();
            GUILayout.Label("Sound Configuration", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Reset to Defaults", GUILayout.Width(120)))
            {
                Undo.RecordObject(data, "Reset Sound Data Settings");
                data.ResetSettings();
                EditorUtility.SetDirty(data);
            }

            GUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            DrawDefaultInspector();

            DrawVisualizer();
        }

        private void DrawVisualizer()
        {
            EditorGUILayout.Space(15);
            Rect lineRect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(lineRect, new Color(0.3f, 0.3f, 0.3f, 1));

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Preview Controls", EditorStyles.boldLabel);

            // --- BUTTONS ---
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("► Play Preview", GUILayout.Height(30))) AudioPreviewer.Play(data);

            EditorGUI.BeginDisabledGroup(!AudioPreviewer.IsPlaying);
            if (GUILayout.Button("■ Stop", GUILayout.Height(30))) AudioPreviewer.Stop();
            EditorGUI.EndDisabledGroup();
            GUILayout.EndHorizontal();

            // --- WAVEFORM VISUALIZER ---

            EditorGUILayout.Space(5);

            // 1. Determine Clip
            AudioClip clipToShow = null;
            if (AudioPreviewer.IsPlaying)
                clipToShow = AudioPreviewer.GetCurrentClip();
            else if (data.clips != null && data.clips.Length > 0)
                clipToShow = data.clips[0];

            // 2. Reserve Layout Space
            Rect visRect = GUILayoutUtility.GetRect(Screen.width, 60);

            // 3. Draw Background
            EditorGUI.DrawRect(visRect, new Color(0.15f, 0.15f, 0.15f));

            // 4. Draw Waveform
            if (clipToShow != null)
            {
                // Only regenerate if clip changes OR texture is missing
                // Also ensure width is valid (>1) to prevent layout errors
                if ((waveformTexture == null || cachedClip != clipToShow) && visRect.width > 1)
                {
                    ClearTexture(); // IMPORTANT: Destroy old texture before creating new one
                    cachedClip = clipToShow;
                    waveformTexture = PaintWaveformSpectrum(clipToShow, (int)visRect.width, (int)visRect.height,
                        new Color(1f, 0.6f, 0f));
                }

                if (waveformTexture != null) GUI.DrawTexture(visRect, waveformTexture);

                GUI.Label(visRect, $" {clipToShow.name}", EditorStyles.miniLabel);
            }
            else
            {
                GUI.Label(visRect, "No Audio Clip Assigned",
                    new GUIStyle(GUI.skin.label)
                        { alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.gray } });
            }

            // --- INTERACTION & PLAYHEAD ---

            if (clipToShow != null)
            {
                Event evt = Event.current;

                if (visRect.Contains(evt.mousePosition))
                    if (evt.type == EventType.MouseDown || evt.type == EventType.MouseDrag)
                    {
                        if (!AudioPreviewer.IsPlaying) AudioPreviewer.Play(data);

                        float mouseX = evt.mousePosition.x - visRect.x;
                        float normalizedPos = mouseX / visRect.width;

                        AudioPreviewer.SetPlaybackPosition(normalizedPos);
                        Repaint();
                    }

                if (AudioPreviewer.IsPlaying)
                {
                    float progress = AudioPreviewer.GetPlaybackProgress();
                    float playHeadX = visRect.x + visRect.width * progress;

                    var playHeadRect = new Rect(playHeadX, visRect.y, 2, visRect.height);
                    EditorGUI.DrawRect(playHeadRect, Color.yellow);

                    Repaint();
                }
            }
        }

        private Texture2D PaintWaveformSpectrum(AudioClip clip, int width, int height, Color col)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color[] fillColorArray = tex.GetPixels();

            for (var i = 0; i < fillColorArray.Length; i++) fillColorArray[i] = Color.clear;
            tex.SetPixels(fillColorArray);

            if (clip == null)
            {
                tex.Apply();
                return tex;
            }

            // ---------------------------------------------------------
            // FIX: FORCE LOAD DATA
            // If the clip is "Streaming" or "Compressed", GetData fails.
            // We force it to load into memory for this visualization.
            // ---------------------------------------------------------
            if (clip.loadState != AudioDataLoadState.Loaded)
                if (!clip.LoadAudioData())
                {
                    // If it fails to load (too big/streaming), just return empty transparent texture
                    tex.Apply();
                    return tex;
                }

            int samplesToRead = Mathf.Min(clip.samples, 44100 * 60);
            var samples = new float[samplesToRead];

            // Try/Catch for GetData as it can still throw on edge cases
            try
            {
                clip.GetData(samples, 0);
            }
            catch
            {
                tex.Apply();
                return tex;
            }

            int packSize = samples.Length / width + 1;

            for (var x = 0; x < width; x++)
            {
                float max = 0;
                int startSample = x * packSize;
                int endSample = Mathf.Min(startSample + packSize, samples.Length);

                for (int i = startSample; i < endSample; i++)
                {
                    float val = Mathf.Abs(samples[i]);
                    if (val > max) max = val;
                }

                var heightInPixels = (int)(max * height);
                if (heightInPixels == 0) heightInPixels = 1;

                int yStart = height / 2 - heightInPixels / 2;

                for (var y = 0; y < heightInPixels; y++) tex.SetPixel(x, yStart + y, col);
            }

            tex.Apply();
            return tex;
        }
    }
}