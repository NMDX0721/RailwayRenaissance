using System;
using System.IO;
using UnityEngine;

namespace RailwayRenaissance.Core.Auth
{
    /// <summary>
    /// 本地认证数据模型，替代原 LoginManager.AuthData。
    /// 保留完全相同的 JSON 字段以实现 auth.json 无缝读取。
    /// </summary>
    [Serializable]
    public class AuthData
    {
        public string username;
        public string password;
        public string lastLogin;
    }

    /// <summary>
    /// 本地用户认证服务。解耦自旧版 LoginManager 中的本地账户文件与凭证逻辑。
    /// 统一负责 auth.json 读写、凭证验证、保存与删除。
    /// </summary>
    public static class UserAuthService
    {
        public static string AuthFilePath => Path.Combine(Application.persistentDataPath, "auth.json");

        public static bool HasCredential()
        {
            if (!File.Exists(AuthFilePath)) return false;
            try
            {
                var auth = LoadAuthData();
                return auth != null && !string.IsNullOrEmpty(auth.username);
            }
            catch
            {
                return false;
            }
        }

        public static AuthData LoadAuthData()
        {
            if (!File.Exists(AuthFilePath)) return null;
            try
            {
                string json = File.ReadAllText(AuthFilePath);
                if (string.IsNullOrEmpty(json)) return null;
                return JsonUtility.FromJson<AuthData>(json);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[UserAuthService] 无法解析 auth.json: {ex.Message}");
                return null;
            }
        }

        public static bool SaveAuthData(string username, string password)
        {
            try
            {
                var auth = new AuthData
                {
                    username = username,
                    password = password,
                    lastLogin = DateTime.Now.ToString("o")
                };
                string json = JsonUtility.ToJson(auth, true);
                File.WriteAllText(AuthFilePath, json);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UserAuthService] 保存 auth.json 失败: {ex.Message}");
                return false;
            }
        }

        public static bool UpdateLastLogin(AuthData auth)
        {
            if (auth == null) return false;
            try
            {
                auth.lastLogin = DateTime.Now.ToString("o");
                string json = JsonUtility.ToJson(auth, true);
                File.WriteAllText(AuthFilePath, json);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UserAuthService] 更新 lastLogin 失败: {ex.Message}");
                return false;
            }
        }

        public static void ClearCredential()
        {
            try
            {
                if (File.Exists(AuthFilePath))
                {
                    File.Delete(AuthFilePath);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[UserAuthService] 删除 auth.json 失败: {ex.Message}");
            }
        }

        public static bool ValidateCredential(string username, string password, out string errorHint)
        {
            if (string.IsNullOrEmpty(username))
            {
                errorHint = "请输入用户名";
                return false;
            }
            if (string.IsNullOrEmpty(password))
            {
                errorHint = "请输入密码";
                return false;
            }

            var auth = LoadAuthData();
            if (auth == null || auth.username != username || auth.password != password)
            {
                errorHint = "用户名或密码错误";
                return false;
            }

            UpdateLastLogin(auth);
            PlayerPrefs.SetString("Username", auth.username);
            PlayerPrefs.Save();
            errorHint = string.Empty;
            return true;
        }
    }
}
