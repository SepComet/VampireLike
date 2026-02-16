//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using Definition;
using GameFramework.Localization;
using StarForce;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;

namespace UI
{
    public class SettingForm : UGuiForm
    {
        [SerializeField] private Toggle _musicMuteToggle = null;
        [SerializeField] private Slider _musicVolumeSlider = null;

        [SerializeField] private Toggle _soundMuteToggle = null;
        [SerializeField] private Slider _soundVolumeSlider = null;

        [SerializeField] private Toggle _uiSoundMuteToggle = null;
        [SerializeField] private Slider _uiSoundVolumeSlider = null;

        [SerializeField] private CanvasGroup _languageTipsCanvasGroup = null;

        [SerializeField] private Toggle _englishToggle = null;
        [SerializeField] private Toggle _chineseSimplifiedToggle = null;
        [SerializeField] private Toggle _chineseTraditionalToggle = null;
        [SerializeField] private Toggle _koreanToggle = null;

        private Language _selectedLanguage = Language.Unspecified;

        public void OnMusicMuteChanged(bool isOn)
        {
            GameEntry.Sound.Mute("Music", !isOn);
            _musicVolumeSlider.gameObject.SetActive(isOn);
        }

        public void OnMusicVolumeChanged(float volume)
        {
            GameEntry.Sound.SetVolume("Music", volume);
        }

        public void OnSoundMuteChanged(bool isOn)
        {
            GameEntry.Sound.Mute("Sound", !isOn);
            _soundVolumeSlider.gameObject.SetActive(isOn);
        }

        public void OnSoundVolumeChanged(float volume)
        {
            GameEntry.Sound.SetVolume("Sound", volume);
        }

        public void OnUISoundMuteChanged(bool isOn)
        {
            GameEntry.Sound.Mute("UISound", !isOn);
            _uiSoundVolumeSlider.gameObject.SetActive(isOn);
        }

        public void OnUISoundVolumeChanged(float volume)
        {
            GameEntry.Sound.SetVolume("UISound", volume);
        }

        public void OnEnglishSelected(bool isOn)
        {
            if (!isOn)
            {
                return;
            }

            _selectedLanguage = Language.English;
            RefreshLanguageTips();
        }

        public void OnChineseSimplifiedSelected(bool isOn)
        {
            if (!isOn)
            {
                return;
            }

            _selectedLanguage = Language.ChineseSimplified;
            RefreshLanguageTips();
        }

        public void OnChineseTraditionalSelected(bool isOn)
        {
            if (!isOn)
            {
                return;
            }

            _selectedLanguage = Language.ChineseTraditional;
            RefreshLanguageTips();
        }

        public void OnKoreanSelected(bool isOn)
        {
            if (!isOn)
            {
                return;
            }

            _selectedLanguage = Language.Korean;
            RefreshLanguageTips();
        }

        public void OnSubmitButtonClick()
        {
            if (_selectedLanguage == GameEntry.Localization.Language)
            {
                Close();
                return;
            }

            GameEntry.Setting.SetString(Constant.Setting.Language, _selectedLanguage.ToString());
            GameEntry.Setting.Save();

            GameEntry.Sound.StopMusic();
            UnityGameFramework.Runtime.GameEntry.Shutdown(ShutdownType.Restart);
        }

#if UNITY_2017_3_OR_NEWER
        protected override void OnOpen(object userData)
#else
        protected internal override void OnOpen(object userData)
#endif
        {
            base.OnOpen(userData);

            _musicMuteToggle.isOn = !GameEntry.Sound.IsMuted("Music");
            _musicVolumeSlider.value = GameEntry.Sound.GetVolume("Music");

            _soundMuteToggle.isOn = !GameEntry.Sound.IsMuted("Sound");
            _soundVolumeSlider.value = GameEntry.Sound.GetVolume("Sound");

            _uiSoundMuteToggle.isOn = !GameEntry.Sound.IsMuted("UISound");
            _uiSoundVolumeSlider.value = GameEntry.Sound.GetVolume("UISound");

            _selectedLanguage = GameEntry.Localization.Language;
            switch (_selectedLanguage)
            {
                case Language.English:
                    _englishToggle.isOn = true;
                    break;

                case Language.ChineseSimplified:
                    _chineseSimplifiedToggle.isOn = true;
                    break;

                case Language.ChineseTraditional:
                    _chineseTraditionalToggle.isOn = true;
                    break;

                case Language.Korean:
                    _koreanToggle.isOn = true;
                    break;

                default:
                    break;
            }
        }

#if UNITY_2017_3_OR_NEWER
        protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
#else
        protected internal override void OnUpdate(float elapseSeconds, float realElapseSeconds)
#endif
        {
            base.OnUpdate(elapseSeconds, realElapseSeconds);

            if (_languageTipsCanvasGroup.gameObject.activeSelf)
            {
                _languageTipsCanvasGroup.alpha = 0.5f + 0.5f * Mathf.Sin(Mathf.PI * Time.time);
            }
        }

        private void RefreshLanguageTips()
        {
            _languageTipsCanvasGroup.gameObject.SetActive(_selectedLanguage != GameEntry.Localization.Language);
        }
    }
}