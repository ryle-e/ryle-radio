using NaughtyAttributes;
using RyleRadio.Components.Base;
using RyleRadio.Tracks;
using UnityEngine;

namespace RyleRadio.Components
{
    /// <summary>
    /// A component that performs actions on a radio, such as playing tracks, stopping them, etc
    /// <br><br>Very useful when writing custom code for a radio, or when using a \ref RadioObserver
    /// </summary>
    [AddComponentMenu("Ryle Radio/Radio Interactor")]
    public class RadioInteractor : RadioOutputTrackAccessor
    {
        /// <summary>
        /// The tracks that this interactor applies to
        /// </summary>
        /// <remarks>Much like in \ref RadioObserver , you need multiple RadioInteractors to perform actions on different track groups</remarks>
        [Multiselect("TrackNames")]
        [SerializeField] private int affectedTracks;

        /// <summary>
        /// The method by which this interactor selects a RadioTrackPlayer when one is needed. For example, if \ref Stop() is called and an affected track has multiple active players, the interactor needs to pick which player to stop- it chooses according to this variable
        /// </summary>
        [Foldout("Advanced Settings"), SerializeField] 
        private RadioOutput.MultiplePlayersSelector playerSelector;

        /// <summary>
        /// Whether or not extra debug information should be printed from this component
        /// </summary>
        [Foldout("Advanced Settings"), SerializeField] 
        private bool debugAll = false;


        /// <summary>
        /// Plays \ref affectedTracks with looping players
        /// </summary>
        public void PlayLoop()
        {
            // apply the loop play to all affected tracks
            DoTrackAction(affectedTracks, id => output.PlayLoop(id));
        }

        /// <summary>
        /// Plays \ref affectedTracks with one-shot players
        /// </summary>
        public void PlayOneShot()
        {
            // apply the oneshot play to all affected tracks
            DoTrackAction(affectedTracks, id => output.PlayOneShot(id));
        }

        /// <summary>
        /// Stops a player on each \ref affectedTracks
        /// </summary>
        public void Stop()
        {
            // apply the stop to all affected tracks
            DoTrackAction(affectedTracks, id =>
            {
                // if there is a RadioTrackPlayer active for this track,
                if (output.TryGetPlayer(id, out RadioTrackPlayer player, false, playerSelector))
                    player.Stop(); // stop it
                else
                { // otherwise, let us know
                    if (debugAll)
                        Debug.LogWarning($"No players with ID {id} are currently playing, and so none have been stopped!");
                }
            });
        }

        /// <summary>
        /// Pauses/unpauses a player on each \ref affectedTracks
        /// </summary>
        public void FlipPause()
        {
            // apply the pause flip to all affected tracks
            DoTrackAction(affectedTracks, id =>
            {
                // if there is a RadioTrackPlayer active for this track,
                if (output.TryGetPlayer(id, out RadioTrackPlayer player, false, playerSelector))
                    player.Paused = !player.Paused; // pause/unpause it
                else
                { // otherwise, let us know
                    if (debugAll)
                        Debug.LogWarning($"No players with ID {id} are currently playing, and so none have been stopped!");
                }
            });
        }

        /// <summary>
        /// Pauses a player on each \ref affectedTracks
        /// </summary>
        public void Pause()
        {
            // apply the pause to all affected tracks
            DoTrackAction(affectedTracks, id =>
            {
                // if there is a RadioTrackPlayer active for this track,
                if (output.TryGetPlayer(id, out RadioTrackPlayer player, false, playerSelector))
                    player.Paused = true; // pause it
                else
                { // otherwise, let us know
                    if (debugAll)
                        Debug.LogWarning($"No players with ID {id} are currently playing, and so none have been stopped!");
                }
            });
        }

        /// <summary>
        /// Unpauses a player on each \ref affectedTracks
        /// </summary>
        public void Unpause()
        {
            // apply the unpause to all affected tracks
            DoTrackAction(affectedTracks, id =>
            {
                // if there is a RadioTrackPlayer active for this track,
                if (output.TryGetPlayer(id, out RadioTrackPlayer player, false, playerSelector))
                    player.Paused = false; // unpause it
                else
                { // otherwise, let us know
                    if (debugAll)
                        Debug.LogWarning($"No players with ID {id} are currently playing, and so none have been stopped!");
                }
            });
        }
        
        /// <summary>
        /// Resets the progress of a player on each \ref affectedTracks
        /// </summary>
        public void ResetProgress()
        {
            // apply the reset to all affected tracks
            DoTrackAction(affectedTracks, id =>
            {
                // if there is a RadioTrackPlayer active for this track,
                if (output.TryGetPlayer(id, out RadioTrackPlayer player, false, playerSelector))
                    player.ResetProgress(); // reset its progress (restart the playback)
                else
                { // otherwise, let us know
                    if (debugAll)
                        Debug.LogWarning($"No players with ID {id} are currently playing, and so none have been stopped!");
                }
            });
        }
    }

}