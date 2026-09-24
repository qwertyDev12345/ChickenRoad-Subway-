using System;
using UnityEngine;
using UserProfile.Utils;

namespace UserProfile
{
    public static class UserProfileStorage
    {
        public static event Action<Sprite> OnChangedUserIcon = null;
        public static event Action<string> OnChangedUserName = null;

        private const string IMAGE_SAVE_FILE = "icon.png";
        private const string NAME_SAVE_KEY = "UserNameSave";

        private const string DEFAULT_USER_NAME = "Clucky Champ";

        private static Sprite _userIcon = null;
        private static string _userName = null;

        private static Sprite DefaultIcon
        {
            get
            {
                string spriteName = "default_user";
                
                Sprite sprite = Resources.Load<Sprite>(spriteName);

                if (sprite == null)
                    Debug.LogError($"{spriteName} not found in Resources!!!");
                
                return sprite;
            }
        }

        public static Sprite UserIcon
        {
            get => _userIcon;
            set
            {
                if (_userIcon != value)
                {
                    _userIcon = value;

                    OnChangedUserIcon?.Invoke(_userIcon);

                    SpriteSaver.SaveSprite(IMAGE_SAVE_FILE, _userIcon);
                }
            }
        }

        public static string UserName
        {
            get => _userName;
            set
            {
                if (_userName != value)
                {
                    if (string.IsNullOrWhiteSpace(value))
                        value = DEFAULT_USER_NAME;

                    _userName = value;

                    OnChangedUserName?.Invoke(_userName);

                    PlayerPrefs.SetString(NAME_SAVE_KEY, _userName);
                }
            }
        }
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void Init()
        {
            _userIcon = SpriteSaver.LoadSprite(IMAGE_SAVE_FILE);

            if (_userIcon == null)
                _userIcon = DefaultIcon;

            _userName = PlayerPrefs.GetString(NAME_SAVE_KEY, DEFAULT_USER_NAME);
        }
    }
}