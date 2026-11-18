using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace DragonMaidClock
{
    public sealed class SleepArtwork : MonoBehaviour
    {
        [Serializable]
        private class ArtSource
        {
            public string pngUrl = "";
            public string anUrl = "";
        }

        [Serializable]
        private class ArtConfig
        {
            public ArtSource nightWatch = null;
            public ArtSource morning = null;
            public ArtSource icons = null;
        }

        private const int IconIndex = 2;
        private const int IconCount = 16;
        private readonly Texture2D[] defaults = new Texture2D[3];
        private readonly Texture2D[] replacements = new Texture2D[3];
        private readonly bool[] hasAnimation = new bool[2];
        private readonly HashSet<UnityWebRequest> pending = new HashSet<UnityWebRequest>();
        private AnArtworkPlayer player;

        public Texture2D Icons => replacements[IconIndex] != null ? replacements[IconIndex] : defaults[IconIndex];

        public void Initialize(Texture2D night, Texture2D morning, Texture2D icons)
        {
            Release();
            defaults[(int)ArtworkSlot.NightWatch] = night;
            defaults[(int)ArtworkSlot.Morning] = morning;
            defaults[IconIndex] = icons;
            player = GetComponent<AnArtworkPlayer>();
            string path = Application.streamingAssetsPath.TrimEnd('/', '\\') + "/artwork-urls.json";
            if (Uri.TryCreate(path, UriKind.Absolute, out var uri))
                StartCoroutine(Read(uri, "artwork-urls.json", bytes => Configure(uri, bytes)));
        }

        public Texture2D GetArtwork(ArtworkSlot slot)
        {
            int index = (int)slot;
            if (player != null && hasAnimation[index])
            {
                try
                {
                    var frame = player.GetFrame(slot);
                    if (frame != null) return frame;
                }
                catch (Exception error)
                {
                    hasAnimation[index] = false;
                    Debug.LogWarning($"Sleep artwork: AN player stopped for {slot} ({error.GetType().Name}).");
                }
            }
            return replacements[index] != null ? replacements[index] : defaults[index];
        }

        private void Configure(Uri configUri, byte[] bytes)
        {
            ArtConfig config;
            try
            {
                // Windows text editors may add a UTF-8 BOM.
                config = JsonUtility.FromJson<ArtConfig>(Encoding.UTF8.GetString(bytes).TrimStart('\uFEFF'));
            }
            catch (ArgumentException)
            {
                Debug.LogWarning("Sleep artwork: invalid artwork-urls.json.");
                return;
            }
            if (config == null) return;
            ConfigureSlot(configUri, ArtworkSlot.NightWatch, config.nightWatch);
            ConfigureSlot(configUri, ArtworkSlot.Morning, config.morning);
            QueuePng(configUri, config.icons, IconIndex, "icons");
        }

        private void ConfigureSlot(Uri configUri, ArtworkSlot slot, ArtSource source)
        {
            QueuePng(configUri, source, (int)slot, slot.ToString());
            if (player == null || source == null || !Resolve(configUri, source.anUrl, out var uri)) return;
            try
            {
                player.Configure(slot, uri.AbsoluteUri);
                hasAnimation[(int)slot] = true;
            }
            catch (Exception error)
            {
                Debug.LogWarning($"Sleep artwork: AN player could not configure {slot} ({error.GetType().Name}).");
            }
        }

        private void QueuePng(Uri configUri, ArtSource source, int index, string label)
        {
            if (source == null || !Resolve(configUri, source.pngUrl, out var uri)) return;
            StartCoroutine(Read(uri, label, bytes => ApplyPng(index, label, bytes)));
        }

        private static bool Resolve(Uri configUri, string address, out Uri uri)
        {
            uri = null;
            if (string.IsNullOrWhiteSpace(address)) return false;
            address = address.Trim();
            // A drive-letter base treats %23 as a literal filename. Use file:/// before joining URLs.
            if (configUri.IsFile) configUri = new Uri(configUri.AbsoluteUri);
            if ((configUri.IsFile || configUri.Scheme == "jar") && !Uri.TryCreate(address, UriKind.Absolute, out _))
            {
                // A relative local filename may contain a literal fragment or query delimiter.
                address = address.Replace("#", "%23").Replace("?", "%3F");
            }
            if (Uri.TryCreate(configUri, address, out uri) &&
                (uri.IsFile || uri.Scheme == Uri.UriSchemeHttps ||
                 (uri.Scheme == Uri.UriSchemeHttp && uri.IsLoopback) ||
                 (uri.Scheme == "jar" && configUri.Scheme == "jar"))) return true;
            Debug.LogWarning("Sleep artwork: unsupported asset URL.");
            return false;
        }

        private IEnumerator Read(Uri uri, string label, Action<byte[]> loaded)
        {
            var request = UnityWebRequest.Get(uri.AbsoluteUri);
            pending.Add(request);
            try
            {
                request.timeout = 12;
                UnityWebRequestAsyncOperation operation = null;
                try { operation = request.SendWebRequest(); }
                catch (InvalidOperationException) { Debug.LogWarning("Sleep artwork: could not request " + label); }
                if (operation == null) yield break;
                yield return operation;
                if (request.result == UnityWebRequest.Result.Success)
                    loaded(request.downloadHandler.data);
                else
                    Debug.LogWarning("Sleep artwork: could not load " + label);
            }
            finally
            {
                if (pending.Remove(request)) request.Dispose();
            }
        }

        private void ApplyPng(int index, string label, byte[] bytes)
        {
            if (bytes == null || bytes.Length < 24 || bytes[0] != 137 || bytes[1] != 80 ||
                bytes[2] != 78 || bytes[3] != 71 || bytes[4] != 13 || bytes[5] != 10 ||
                bytes[6] != 26 || bytes[7] != 10)
            {
                Debug.LogWarning("Sleep artwork: expected a PNG for " + label);
                return;
            }

            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(bytes, true) || (index == IconIndex && texture.width != texture.height * IconCount))
            {
                Destroy(texture);
                Debug.LogWarning("Sleep artwork: invalid PNG dimensions or data for " + label);
                return;
            }
            texture.wrapMode = TextureWrapMode.Clamp;
            replacements[index] = texture;
        }

        private void OnDestroy() => Release();

        private void Release()
        {
            StopAllCoroutines();
            foreach (var request in pending)
            {
                request.Abort();
                request.Dispose();
            }
            pending.Clear();
            Array.Clear(hasAnimation, 0, hasAnimation.Length);
            for (int i = 0; i < replacements.Length; ++i)
            {
                if (replacements[i] != null) Destroy(replacements[i]);
                replacements[i] = null;
            }
        }
    }
}
