using RyleRadio.Components.Base;
using RyleRadio.Tracks;
using UnityEngine;
using UnityEngine.Rendering;

namespace RyleRadio.Components
{
    // a script that performs actions on a radio, such as playing tracks, stopping them, etc
    [AddComponentMenu("Ryle Radio/Radio Interactor")]
    public class RadioInteractor : RadioOutputTrackAccessor
    {
        // the tracks this interactor applies to- to apply to differen tracks, we need another interactor
        [Multiselect("TrackNames")]
        [SerializeField] private int affectedTracks;

        // the method by which this interactor chooses a RadioTrackPlayer when stopping, pausing, or resetting
        [SerializeField] private RadioOutput.MultiplePlayersSelector playerSelector;

        // whether or not this should print out all debug information about actions performed on tracks
        [SerializeField] private bool debugAll = false;


        // plays affected tracks as looped tracks
        public void PlayLoop()
        {
            // apply the loop play to all affected tracks
            DoTrackAction(affectedTracks, id => output.PlayLoop(id));
        }

        // plays affected tracks as one-shots
        public void PlayOneShot()
        {
            // apply the oneshot play to all affected tracks
            DoTrackAction(affectedTracks, id => output.PlayOneShot(id));
        }

        // stops a player for affected tracks
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

        // pauses/unpauses a player for affected tracks
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

        // pauses a player for affected tracks
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

        // unpauses a player for affected tracks
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
        
        // resets the progress of a player for affected tracks to 0
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