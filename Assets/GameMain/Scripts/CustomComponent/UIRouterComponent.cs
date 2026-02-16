using System;
using System.Collections.Generic;
using Definition.Enum;
using UnityGameFramework.Runtime;
using UI;

namespace CustomComponent
{
    public class UIRouterComponent : GameFrameworkComponent
    {
        private readonly Dictionary<UIFormType, IUIFormController> _routeControllers = new();

        public void BindUIUseCase(UIFormType uiFormType, IUIUseCase useCase)
        {
            IUIFormController controller = GetOrCreateController(uiFormType);
            controller.BindUseCase(useCase);
        }

        public int? OpenUI(UIFormType uiFormType, object userData = null)
        {
            IUIFormController controller = GetOrCreateController(uiFormType);
            return controller.OpenUI(userData);
        }

        public void CloseUI(UIFormType uiFormType)
        {
            IUIFormController controller = GetOrCreateController(uiFormType);
            controller.CloseUI();
        }

        private IUIFormController GetOrCreateController(UIFormType uiFormType)
        {
            if (_routeControllers.TryGetValue(uiFormType, out IUIFormController controller))
            {
                return controller;
            }

            string typename = $"UI.{uiFormType}Controller";
            Type controllerType = Type.GetType(typename);
            if (controllerType == null)
            {
                controller = new DefaultUIFormController(uiFormType);
            }
            else
            {
                controller = (IUIFormController)Activator.CreateInstance(controllerType);
            }

            _routeControllers.Add(uiFormType, controller);
            return controller;
        }

        private void OnDestroy()
        {
            foreach (KeyValuePair<UIFormType, IUIFormController> pair in _routeControllers)
            {
                pair.Value.CloseUI();
            }

            _routeControllers.Clear();
        }

        private class DefaultUIFormController : IUIFormController
        {
            private readonly UIFormType _uiFormType;
            private int? _lastSerialId;

            public DefaultUIFormController(UIFormType uiFormType)
            {
                _uiFormType = uiFormType;
            }

            public int? OpenUI(object userData = null)
            {
                _lastSerialId = GameEntry.UI.OpenUIForm(_uiFormType, userData);
                return _lastSerialId;
            }

            public void CloseUI()
            {
                if (_lastSerialId.HasValue)
                {
                    GameEntry.UI.CloseUIForm(_lastSerialId.Value);
                    _lastSerialId = null;
                    return;
                }

                UGuiForm uiForm = GameEntry.UI.GetUIForm(_uiFormType);
                if (uiForm != null)
                {
                    GameEntry.UI.CloseUIForm(uiForm);
                }
            }

            public void BindUseCase(IUIUseCase useCase)
            {
            }
        }

    }
}
