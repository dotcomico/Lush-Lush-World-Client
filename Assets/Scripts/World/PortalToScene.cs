using UnityEngine;
using UnityEngine.SceneManagement;

namespace LushWorld.World
{
    // Attach to a trigger-collider sphere. When the Player tag enters, loads the target scene.
    public class PortalToScene : MonoBehaviour
    {
        [SerializeField] private string _targetScene = "WorldTestScene";

        private bool _triggered;

        private void OnTriggerEnter(Collider other)
        {
            if (_triggered) return;
            if (!other.CompareTag("Player")) return;

            _triggered = true;
            SceneManager.LoadScene(_targetScene);
        }
    }
}
