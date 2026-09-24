using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

namespace UserProfile.UI
{
    public class UserNameField : MonoBehaviour
    {
        [SerializeField] private GameObject _normalState = null;
        [SerializeField] private GameObject _editingState = null;
    
        [Space]
    
        [SerializeField] private Text _nameText = null;

        [SerializeField] private InputField _editNameField = null;

        private void Awake()
        {
            _normalState.SetActive(true);
            _editingState.SetActive(false);
        
            _editNameField.onEndEdit.AddListener(SaveNewName);
            _editNameField.onValidateInput += ValidateInput;

            UserProfileStorage.OnChangedUserName += UpdateNameText;

            UpdateNameText(UserProfileStorage.UserName);
        }
        
        private char ValidateInput(string input, int charIndex, char addedChar)
        {
            if (Regex.IsMatch(addedChar.ToString(), "[^a-zA-Z0-9]"))
            {
                addedChar = '\0';
            }

            return addedChar;
        }

        private void OnDestroy()
        {
            UserProfileStorage.OnChangedUserName -= UpdateNameText;
        }

        private void SaveNewName(string name)
        {
            _normalState.SetActive(true);
            _editingState.SetActive(false);

            UserProfileStorage.UserName = name;
        }

        private void UpdateNameText(string name)
        {
            _nameText.text = name;
        }

        public void StartEditing()
        {
            _normalState.SetActive(false);
            _editingState.SetActive(true);

            _editNameField.text = UserProfileStorage.UserName;
        
            _editNameField.ActivateInputField();
            
            _editNameField.caretPosition = UserProfileStorage.UserName.Length;
        }
    }
}
