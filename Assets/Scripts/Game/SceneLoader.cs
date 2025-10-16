using UnityEngine;
using UnityEngine.SceneManagement; // Cần thiết để quản lý Scene

public class SceneLoader : MonoBehaviour
{
    // Hàm này sẽ được gọi khi nút được nhấn
    public void LoadMainMenu()
    {
        // Tên của scene Main Menu
        SceneManager.LoadScene("MainMenu");

        // *Lưu ý: Đảm bảo tên "MainMenu" trùng khớp chính xác với tên file scene của bạn*
    }
}