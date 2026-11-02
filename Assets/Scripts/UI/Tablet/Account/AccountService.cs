using System;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace RailwayRenaissance.UI.Tablet.Account
{
    /// <summary>本机模拟的小米账号资料（不联网）。密码只保存加盐哈希，手机号为游戏内虚构号码。</summary>
    [Serializable]
    public class AccountProfile
    {
        public string xiaomiId;
        public string nickname;
        public string region;
        public string phone;
        public string salt;
        public string passwordHash;
        public int avatar;
        public string registeredAt;
        public int passwordChangedCount;
    }

    /// <summary>
    /// 平板“小米账号”的数据层：注册、改名、改密码、短信验证码。
    /// 设计依据：docs/compose/specs/平板系统设计.md §九——账号是叙事元素，不做真实网络账号；
    /// 这里在此基础上加入本地密码，让注册与改密流程更接近真实小米账号。
    /// </summary>
    public static class AccountService
    {
        public const string DefaultRegion = "朝鲜 +850";
        public const int MinPasswordLength = 8;
        public const int MaxPasswordLength = 16;
        public const int MaxNicknameLength = 12;

        private const string ProfileKey = "XiaomiAccountProfile_v1";
        private const string SetupDoneKey = "TabletSetupDone_v1";
        private const string LegacyNameKey = "XiaomiAccount";   // 平板其他界面读取的昵称

        private static AccountProfile s_cached;
        private static bool s_loaded;

        // 短信验证码（仅内存）
        private static string s_codePhone;
        private static string s_code;
        private static float s_codeExpiresAt;
        private static int s_codeAttempts;

        public static event Action Changed;

        public static bool IsRegistered => Profile != null;

        /// <summary>开机向导是否已经走完（包含“暂不登录”）。</summary>
        public static bool SetupDone
        {
            get => PlayerPrefs.GetInt(SetupDoneKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(SetupDoneKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public static AccountProfile Profile
        {
            get
            {
                if (!s_loaded)
                {
                    s_loaded = true;
                    string json = PlayerPrefs.GetString(ProfileKey, string.Empty);
                    if (!string.IsNullOrEmpty(json))
                    {
                        try { s_cached = JsonUtility.FromJson<AccountProfile>(json); }
                        catch (Exception) { s_cached = null; }
                        if (s_cached != null && string.IsNullOrEmpty(s_cached.xiaomiId)) s_cached = null;
                    }
                }
                return s_cached;
            }
        }

        public static string DisplayName =>
            Profile != null && !string.IsNullOrEmpty(Profile.nickname)
                ? Profile.nickname
                : PlayerPrefs.GetString(LegacyNameKey, "旅人");

        /// <summary>仅供测试：清除全部账号数据与向导状态。</summary>
        public static void ResetForTests()
        {
            PlayerPrefs.DeleteKey(ProfileKey);
            PlayerPrefs.DeleteKey(SetupDoneKey);
            s_cached = null;
            s_loaded = true;
            ClearCode();
        }

        /// <summary>仅供测试：丢弃内存缓存，下次访问重新从 PlayerPrefs 读取。</summary>
        public static void ReloadFromPrefs()
        {
            s_cached = null;
            s_loaded = false;
            ClearCode();
        }

        // ---------------- 校验 ----------------

        public static string ValidatePhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return "请输入手机号";
            string p = phone.Trim();
            for (int i = 0; i < p.Length; i++)
                if (p[i] < '0' || p[i] > '9') return "手机号只能包含数字";
            if (p.Length < 8 || p.Length > 11) return "请输入 8 到 11 位手机号";
            return null;
        }

        /// <summary>密码规则（本作自拟）：8–16 位，同时含字母和数字，不含空格与中文，不能与手机号相同，不能全是同一个字符。</summary>
        public static string ValidatePassword(string password, string phone = null)
        {
            if (string.IsNullOrEmpty(password)) return "请输入密码";
            if (password.Length < MinPasswordLength || password.Length > MaxPasswordLength)
                return $"密码长度需为 {MinPasswordLength}–{MaxPasswordLength} 位";
            bool hasLetter = false, hasDigit = false;
            for (int i = 0; i < password.Length; i++)
            {
                char c = password[i];
                if (c <= ' ' || c > '~') return "密码只能使用英文字母、数字和常见符号，不能含空格或中文";
                if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z')) hasLetter = true;
                else if (c >= '0' && c <= '9') hasDigit = true;
            }
            if (!hasLetter || !hasDigit) return "密码需同时包含字母和数字";
            bool allSame = true;
            for (int i = 1; i < password.Length; i++)
                if (password[i] != password[0]) { allSame = false; break; }
            if (allSame) return "密码过于简单，请不要全部使用同一个字符";
            if (!string.IsNullOrEmpty(phone) && password == phone.Trim()) return "密码不能与手机号相同";
            return null;
        }

        /// <summary>密码强度：0 弱、1 中、2 强（仅用于强度条显示）。</summary>
        public static int PasswordStrength(string password)
        {
            if (string.IsNullOrEmpty(password)) return 0;
            int score = 0;
            bool lower = false, upper = false, symbol = false;
            for (int i = 0; i < password.Length; i++)
            {
                char c = password[i];
                if (c >= 'a' && c <= 'z') lower = true;
                else if (c >= 'A' && c <= 'Z') upper = true;
                else if (c < '0' || c > '9') symbol = true;
            }
            if (password.Length >= 12) score++;
            if (lower && upper) score++;
            if (symbol) score++;
            return score >= 2 ? 2 : score == 1 ? 1 : 0;
        }

        public static string ValidateNickname(string nickname)
        {
            if (string.IsNullOrWhiteSpace(nickname)) return "请输入昵称";
            string n = nickname.Trim();
            if (n.Length > MaxNicknameLength) return $"昵称最多 {MaxNicknameLength} 个字";
            for (int i = 0; i < n.Length; i++)
                if (char.IsControl(n[i])) return "昵称含有不支持的字符";
            return null;
        }

        // ---------------- 短信验证码 ----------------

        /// <summary>生成一条 6 位验证码（有效 5 分钟，最多尝试 5 次）。返回验证码，由界面以“短信”形式展示。</summary>
        public static string IssueCode(string phone)
        {
            var rng = new System.Random(Environment.TickCount ^ (phone != null ? phone.GetHashCode() : 0));
            s_code = rng.Next(100000, 1000000).ToString();
            s_codePhone = phone != null ? phone.Trim() : string.Empty;
            s_codeExpiresAt = Time.realtimeSinceStartup + 300f;
            s_codeAttempts = 0;
            return s_code;
        }

        /// <summary>校验验证码：返回 null 为通过，否则返回错误文案。</summary>
        public static string VerifyCode(string phone, string input)
        {
            if (string.IsNullOrEmpty(s_code)) return "请先获取验证码";
            if (Time.realtimeSinceStartup > s_codeExpiresAt) { ClearCode(); return "验证码已过期，请重新获取"; }
            if (s_codeAttempts >= 5) { ClearCode(); return "验证次数过多，请重新获取验证码"; }
            if (!string.Equals(s_codePhone, phone != null ? phone.Trim() : string.Empty, StringComparison.Ordinal))
                return "验证码与当前手机号不匹配，请重新获取";
            s_codeAttempts++;
            if (string.IsNullOrWhiteSpace(input) || input.Trim() != s_code) return "验证码错误";
            ClearCode();
            return null;
        }

        private static void ClearCode()
        {
            s_code = null; s_codePhone = null; s_codeExpiresAt = 0f; s_codeAttempts = 0;
        }

        // ---------------- 注册 / 改名 / 改密码 ----------------

        /// <summary>注册（调用方应已通过验证码校验）。返回 null 为成功，否则为错误文案。</summary>
        public static string Register(string phone, string password, string nickname, int avatar)
        {
            string err = ValidatePhone(phone) ?? ValidatePassword(password, phone) ?? ValidateNickname(nickname);
            if (err != null) return err;
            if (IsRegistered) return "此设备已登录小米账号";

            string salt = Guid.NewGuid().ToString("N");
            var profile = new AccountProfile
            {
                xiaomiId = NewXiaomiId(),
                nickname = nickname.Trim(),
                region = DefaultRegion,
                phone = phone.Trim(),
                salt = salt,
                passwordHash = Hash(salt, password),
                avatar = Mathf.Clamp(avatar, 0, 5),
                registeredAt = DateTime.Now.ToString("yyyy-MM-dd"),
                passwordChangedCount = 0
            };
            Save(profile);
            SetupDone = true;
            return null;
        }

        public static bool VerifyPassword(string password)
        {
            var p = Profile;
            if (p == null || string.IsNullOrEmpty(password)) return false;
            return Hash(p.salt, password) == p.passwordHash;
        }

        public static string ChangePassword(string oldPassword, string newPassword)
        {
            var p = Profile;
            if (p == null) return "尚未登录小米账号";
            if (!VerifyPassword(oldPassword)) return "当前密码不正确";
            string err = ValidatePassword(newPassword, p.phone);
            if (err != null) return err;
            if (newPassword == oldPassword) return "新密码不能与当前密码相同";
            ApplyNewPassword(p, newPassword);
            return null;
        }

        /// <summary>忘记密码：调用方应已通过验证码校验。</summary>
        public static string ResetPassword(string newPassword)
        {
            var p = Profile;
            if (p == null) return "尚未登录小米账号";
            string err = ValidatePassword(newPassword, p.phone);
            if (err != null) return err;
            ApplyNewPassword(p, newPassword);
            return null;
        }

        public static string Rename(string nickname)
        {
            var p = Profile;
            if (p == null) return "尚未登录小米账号";
            string err = ValidateNickname(nickname);
            if (err != null) return err;
            p.nickname = nickname.Trim();
            Save(p);
            return null;
        }

        public static void SetAvatar(int index)
        {
            var p = Profile;
            if (p == null) return;
            p.avatar = Mathf.Clamp(index, 0, 5);
            Save(p);
        }

        /// <summary>手机号脱敏显示，如 +850 123****8848。</summary>
        public static string MaskedPhone(AccountProfile p)
        {
            if (p == null || string.IsNullOrEmpty(p.phone)) return string.Empty;
            string ph = p.phone;
            string prefix = p.region != null && p.region.Contains("+") ? p.region.Substring(p.region.IndexOf('+')) + " " : string.Empty;
            if (ph.Length <= 4) return prefix + ph;
            int keepHead = Mathf.Min(3, ph.Length - 4);
            return prefix + ph.Substring(0, keepHead) + new string('*', ph.Length - keepHead - 4) + ph.Substring(ph.Length - 4);
        }

        // ---------------- 内部 ----------------

        private static void ApplyNewPassword(AccountProfile p, string newPassword)
        {
            p.salt = Guid.NewGuid().ToString("N");
            p.passwordHash = Hash(p.salt, newPassword);
            p.passwordChangedCount++;
            Save(p);
        }

        private static void Save(AccountProfile p)
        {
            s_cached = p;
            s_loaded = true;
            PlayerPrefs.SetString(ProfileKey, JsonUtility.ToJson(p));
            PlayerPrefs.SetString(LegacyNameKey, p.nickname);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        private static string NewXiaomiId()
        {
            var rng = new System.Random(Guid.NewGuid().GetHashCode());
            var sb = new StringBuilder("7");
            for (int i = 0; i < 9; i++) sb.Append(rng.Next(0, 10));
            return sb.ToString();
        }

        private static string Hash(string salt, string password)
        {
            using (var sha = SHA256.Create())
            {
                byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(salt + ":" + password));
                var sb = new StringBuilder(bytes.Length * 2);
                for (int i = 0; i < bytes.Length; i++) sb.Append(bytes[i].ToString("x2"));
                return sb.ToString();
            }
        }
    }
}
