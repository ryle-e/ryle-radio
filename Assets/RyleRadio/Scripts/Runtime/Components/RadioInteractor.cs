using RyleRadio.Components.Base;
using RyleRadio.Tracks;
using UnityEngine;

namespace RyleRadio.Components
{

    public class RadioInteractor : RadioOutputTrackAccessor
    {

        [Multiselect("TrackNames")]
        [SerializeField] private int affectedTracks;
        [SerializeField] private RadioOutput.MultiplePlayersSelector playerSelector;
        [SerializeField] private bool debugAll = false;

        public void PlayLoop()
        {
            DoTrackAction(affectedTracks, id => output.PlayLoop(id));
        }

        public void PlayOneShot()
        {
            DoTrackAction(affectedTracks, id => output.PlayOneShot(id));
        }

        public void Stop()
        {
            DoTrackAction(affectedTracks, id =>
            {
                if (output.TryGetPlayer(id, out RadioTrackPlayer player, false, playerSelector))
                    player.Stop();
                else
                {
                    if (debugAll)
                        Debug.LogWarning($"No players with ID {id} are currently playing, and so none have been stopped!");
                }
            });
        }

        public void FlipPause()
        {
            DoTrackAction(affectedTracks, id =>
            {
                if (output.TryGetPlayer(id, out RadioTrackPlayer player, false, playerSelector))
                    player.Paused = !player.Paused;
                else
                {
                    if (debugAll)
                        Debug.LogWarning($"No players with ID {id} are currently playing, and so none have been stopped!");
                }
            });
        }

        public void Pause()
        {
            DoTrackAction(affectedTracks, id =>
            {
                if (output.TryGetPlayer(id, out RadioTrackPlayer player, false, playerSelector))
                    player.Paused = true;
                else
                {
                    if (debugAll)
                        Debug.LogWarning($"No players with ID {id} are currently playing, and so none have been stopped!");
                }
            });
        }

        public void Unpause()
        {
            DoTrackAction(affectedTracks, id =>
            {
                if (output.TryGetPlayer(id, out RadioTrackPlayer player, false, playerSelector))
                    player.Paused = false;
                else
                {
                    if (debugAll)
                        Debug.LogWarning($"No players with ID {id} are currently playing, and so none have been stopped!");
                }
            });
        }

        public void ResetProgress()
        {
            DoTrackAction(affectedTracks, id =>
            {
                if (output.TryGetPlayer(id, out RadioTrackPlayer player, false, playerSelector))
                    player.ResetProgress();
                else
                {
                    if (debugAll)
                        Debug.LogWarning($"No players with ID {id} are currently playing, and so none have been stopped!");
                }
            });
        }
    }

}