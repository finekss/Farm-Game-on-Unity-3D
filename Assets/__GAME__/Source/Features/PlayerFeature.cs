using UnityEngine;

namespace __GAME__.Source.Features
{
    public class PlayerFeature : FeatureBase
    {
        private Transform _playerTransform;
        private EventBus _bus;

        public override void Start()
        {
            _bus = Main.Get<EventBus>();

            // Находим игрока в сцене
            var player = GameObject.FindWithTag("Player");
            _playerTransform = player.transform;

            LoadPlayerPosition();
        }

        private void LoadPlayerPosition()
        {
            if (Main.data.hasSave)
            {
                _playerTransform.position = Main.data.playerPosition.ToVector3();
                _playerTransform.rotation = Main.data.playerRotation.ToQuaternion();
            }
        }

        public override void OnSave()
        {
            if (_playerTransform == null)
                return;

            Main.data.hasSave = true;

            Main.data.playerPosition = new GameData.SerializableVector3(_playerTransform.position);
            Main.data.playerRotation = new GameData.SerializableQuaternion(_playerTransform.rotation);
        }
    }
}