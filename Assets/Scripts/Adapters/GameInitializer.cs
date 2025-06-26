using UnityEngine;

namespace Adapters
{
    public class GameInitializer : MonoBehaviour
    {
        [SerializeField] private GameManager _gameManager;

        public void Awake ()
        {
            //_gameManager.SpawnPlayer("player1");
        }
    }
}
