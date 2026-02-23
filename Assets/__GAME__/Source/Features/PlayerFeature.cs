using __GAME__.Source.Save;
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

            var player = GameObject.FindWithTag("Player");
            _playerTransform = player.transform;

            LoadPlayerPosition();
        }

        private void LoadPlayerPosition()
        {
            if (Main.data.HasSave)
            {
                _playerTransform.position = Main.data.PlayerPosition.ToVector3();
            }
        }

        public override void OnSave()
        {
            if (_playerTransform == null)
                return;

            Main.data.HasSave = true;

            Main.data.PlayerPosition = new SerializableVector3(_playerTransform.position);
        }
    }
}