using UnityEngine;

namespace DragonMaidClock
{
    public enum ArtworkSlot { NightWatch, Morning }

    /// <summary>Attach a project-specific AN decoder beside SleepArtwork to supply animation frames.</summary>
    public abstract class AnArtworkPlayer : MonoBehaviour
    {
        /// <param name="url">Absolute URI to the custom .an file for this slot.</param>
        public abstract void Configure(ArtworkSlot slot, string url);

        /// <summary>Return the current frame, or null to display the static artwork.</summary>
        public abstract Texture2D GetFrame(ArtworkSlot slot);
    }
}
