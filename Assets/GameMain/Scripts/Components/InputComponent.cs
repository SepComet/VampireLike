using UnityEngine;

namespace Components
{
    public class InputComponent : MonoBehaviour
    {
        private PlayerInputActions _inputActions;
        [SerializeField] private bool _isListening = false;

        [SerializeField] private Vector3 _direction = Vector3.zero;
        public Vector3 Direction => _direction;

        public void OnInit()
        {
            _inputActions = new();
        }

        public void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            if (_isListening && _inputActions != null)
            {
                ReadInput();
            }
        }

        public void OnReset()
        {
            _inputActions = null;
            _isListening = false;
        }

        private void ReadInput()
        {
            Vector2 rawDir = _inputActions.Game.Move.ReadValue<Vector2>();
            _direction = new Vector3(rawDir.x, 0, rawDir.y);
        }

        public void SetListening(bool isListening)
        {
            _isListening = isListening;
            if (_isListening)
            {
                _inputActions.Enable();
            }
            else
            {
                _inputActions.Disable();
            }
        }
    }
}