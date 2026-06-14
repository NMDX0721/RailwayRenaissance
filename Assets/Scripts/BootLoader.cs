using UnityEngine;
using UnityEngine.SceneManagement;

public class BootLoader : MonoBehaviour
{
    private void Start()
    {
        // 先加载登录场景
        SceneManager.LoadScene("Login");
    }
}