using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SettingsSceneController : MonoBehaviour
{
    [SerializeField] private Button _backBtn;

    private void OnEnable()
    {
        _backBtn.onClick.AddListener(OnPressBackBtn);
    }

    private void OnDisable()
    {
        _backBtn.onClick.RemoveAllListeners();
    }

    private void OnPressBackBtn()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
