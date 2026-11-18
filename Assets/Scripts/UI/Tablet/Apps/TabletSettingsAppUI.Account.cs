using System;
using UnityEngine;
using UnityEngine.UIElements;
using RailwayRenaissance.UI.Tablet.Account;
using static RailwayRenaissance.UI.Tablet.TabletUIHelper;

namespace RailwayRenaissance.UI.Tablet.Apps
{
    /// <summary>设置 → 小米账号：账号中心（改昵称、换头像、改密码、忘记密码）与注册入口。</summary>
    public static partial class TabletSettingsAppUI
    {
        private static VisualElement s_settingsWin;

        private static readonly Color[] AccountAvatarColors =
        {
            new Color(0.97f, 0.52f, 0.30f), new Color(0.30f, 0.60f, 0.95f), new Color(0.25f, 0.75f, 0.50f),
            new Color(0.62f, 0.45f, 0.95f), new Color(0.95f, 0.45f, 0.62f), new Color(0.35f, 0.72f, 0.82f)
        };

        private static void RefreshSettingsPage()
        {
            if (s_settingsWin != null && s_settingsWin.panel != null) BuildSettingsPage(s_settingsWin);
        }

        private static void PostSystemNotice(string title, string body)
        {
            RailwayRenaissance.UI.Tablet.SystemShell.TabletNotify.PostNotice(title, body);
        }

        /// <summary>账号页顶部卡片：已登录进账号中心，未登录进注册向导。</summary>
        public static void OpenAccountEntry()
        {
            if (AccountService.IsRegistered) ShowAccountCenter();
            else StartRegistration();
        }

        private static void StartRegistration()
        {
            var host = GameMainUI.Instance != null ? GameMainUI.Instance.OverlayHost : null;
            if (host == null) { ShowToast("请从平板桌面进入"); return; }
            TabletSetupWizard.Show(host, TabletSetupWizard.Flow.RegisterOnly, true, registered =>
            {
                if (registered) ShowToast("小米账号已创建");
                RefreshSettingsPage();
            }, (title, body) => PostSystemNotice(title, body));
        }

        // ================= 账号中心 =================

        private static void ShowAccountCenter()
        {
            var win = BuildInnerPanel("小米账号", ShowAccountCenter);
            if (win == null) return;
            var p = AccountService.Profile;
            if (p == null) return;

            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            scroll.style.flexGrow = 1;
            scroll.style.paddingLeft = 8; scroll.style.paddingRight = 8;
            win.Add(scroll);

            var hero = new VisualElement();
            hero.style.alignItems = Align.Center;
            hero.style.marginTop = 10; hero.style.marginBottom = 22;
            hero.Add(BuildAvatarCircle(p, 96));
            var name = new Label(p.nickname);
            name.style.fontSize = 26; name.style.color = InkPrimary; name.style.unityFontDefinition = FdBold();
            name.style.marginTop = 14;
            hero.Add(name);
            var idLine = new Label("小米 ID：" + p.xiaomiId);
            idLine.style.fontSize = 14; idLine.style.color = InkSecondary; idLine.style.unityFontDefinition = Fd();
            idLine.style.marginTop = 4;
            hero.Add(idLine);
            scroll.Add(hero);

            AddSectionHeader(scroll, "基本信息");
            var info = BuildWhiteCard();
            info.name = "account-center-info";
            info.Add(BuildInfoRow("头像", "更换", null, () => ShowAvatarPage()));
            info.Add(BuildInfoRow("昵称", p.nickname, null, () => ShowRenamePage()));
            info.Add(BuildInfoRow("小米 ID", p.xiaomiId));
            info.Add(BuildInfoRow("绑定手机", AccountService.MaskedPhone(p)));
            scroll.Add(info);

            AddSectionHeader(scroll, "账号安全");
            var sec = BuildWhiteCard();
            sec.name = "account-center-security";
            sec.Add(BuildInfoRow("修改密码", p.passwordChangedCount > 0 ? "已修改 " + p.passwordChangedCount + " 次" : null, null, () => ShowChangePasswordPage()));
            sec.Add(BuildInfoRow("忘记密码", null, "通过短信验证码重置密码", () => ShowResetPasswordPage()));
            scroll.Add(sec);

            var note = new Label("账号资料只保存在这台设备上，不会上传。");
            note.style.fontSize = 13; note.style.color = InkTertiary; note.style.unityFontDefinition = Fd();
            note.style.marginTop = 8; note.style.marginLeft = 6;
            scroll.Add(note);
        }

        private static VisualElement BuildAvatarCircle(AccountProfile p, float size)
        {
            return TabletAvatar.Build(p, size);
        }

        // ---------- 通用表单控件 ----------

        private static TextField AccountField(string placeholder, bool password, int maxLength, string value = "")
        {
            var tf = new TextField { value = value };
            if (maxLength > 0) tf.maxLength = maxLength;
            tf.isPasswordField = password;
            if (password) tf.maskChar = '•';
            tf.textEdition.placeholder = placeholder;
            tf.textEdition.hidePlaceholderOnFocus = true;
            tf.style.height = 52;
            tf.style.marginLeft = 0; tf.style.marginRight = 0; tf.style.marginTop = 10; tf.style.marginBottom = 0;
            tf.style.backgroundColor = Color.white;
            tf.style.borderTopLeftRadius = tf.style.borderTopRightRadius = 14;
            tf.style.borderBottomLeftRadius = tf.style.borderBottomRightRadius = 14;
            tf.style.paddingLeft = 12; tf.style.paddingRight = 12;
            var input = tf.Q(className: "unity-base-text-field__input");
            if (input != null)
            {
                input.style.backgroundColor = Color.clear;
                input.style.borderTopWidth = input.style.borderBottomWidth = input.style.borderLeftWidth = input.style.borderRightWidth = 0;
                input.style.fontSize = 17;
                input.style.color = InkPrimary;
                input.style.unityFontDefinition = Fd();
            }
            return tf;
        }

        private static Label AccountErrorLabel()
        {
            var l = new Label(string.Empty);
            l.style.fontSize = 14; l.style.color = new Color(0.88f, 0.28f, 0.24f);
            l.style.unityFontDefinition = Fd();
            l.style.whiteSpace = WhiteSpace.Normal;
            l.style.marginTop = 10; l.style.minHeight = 20;
            return l;
        }

        private static Button AccountPrimaryButton(string text, Action onClick)
        {
            var b = new Button(onClick) { text = text };
            b.style.height = 52; b.style.marginTop = 16;
            b.style.fontSize = 17; b.style.color = Color.white;
            b.style.backgroundColor = AccentSystem;
            b.style.borderTopWidth = b.style.borderBottomWidth = b.style.borderLeftWidth = b.style.borderRightWidth = 0;
            b.style.borderTopLeftRadius = b.style.borderTopRightRadius = 26;
            b.style.borderBottomLeftRadius = b.style.borderBottomRightRadius = 26;
            b.style.unityFontDefinition = FdBold();
            return b;
        }

        private static Button AccountLinkButton(string text, Action onClick)
        {
            var b = new Button(onClick) { text = text };
            b.style.height = 44; b.style.marginTop = 6;
            b.style.fontSize = 15; b.style.color = AccentSystem;
            b.style.backgroundColor = Color.clear;
            b.style.borderTopWidth = b.style.borderBottomWidth = b.style.borderLeftWidth = b.style.borderRightWidth = 0;
            b.style.unityFontDefinition = Fd();
            return b;
        }

        private static Label AccountHint(string text)
        {
            var l = new Label(text);
            l.style.fontSize = 14; l.style.color = InkSecondary; l.style.unityFontDefinition = Fd();
            l.style.whiteSpace = WhiteSpace.Normal;
            l.style.marginTop = 6; l.style.marginBottom = 6;
            return l;
        }

        private static VisualElement BuildStrengthBar(TextField pw)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginTop = 12;
            var track = new VisualElement();
            track.style.height = 6; track.style.flexGrow = 1;
            track.style.backgroundColor = new Color(0, 0, 0, 0.10f);
            track.style.overflow = Overflow.Hidden;
            track.style.borderTopLeftRadius = track.style.borderTopRightRadius = 3;
            track.style.borderBottomLeftRadius = track.style.borderBottomRightRadius = 3;
            var fill = new VisualElement();
            fill.style.height = Length.Percent(100);
            track.Add(fill);
            row.Add(track);
            var label = new Label(string.Empty);
            label.style.marginLeft = 10; label.style.minWidth = 18; label.style.fontSize = 13; label.style.color = InkSecondary;
            label.style.unityFontDefinition = Fd();
            row.Add(label);
            Action refresh = () =>
            {
                int level = string.IsNullOrEmpty(pw.value) ? -1 : AccountService.PasswordStrength(pw.value);
                Color[] palette = { new Color(0.93f, 0.35f, 0.30f), new Color(0.97f, 0.70f, 0.20f), new Color(0.25f, 0.75f, 0.45f) };
                fill.style.width = Length.Percent(level < 0 ? 0f : (level + 1) * 100f / 3f);
                fill.style.backgroundColor = palette[Mathf.Max(level, 0)];
                label.text = level < 0 ? string.Empty : level == 0 ? "弱" : level == 1 ? "中" : "强";
            };
            pw.RegisterValueChangedCallback(_ => refresh());
            refresh();
            return row;
        }

        private static void FinishAccountSubPage(string toast)
        {
            if (!string.IsNullOrEmpty(toast)) ShowToast(toast);
            GameMainUI.Instance?.NavigateBack();
            ShowAccountCenter();   // 同标题页替换刷新，立刻显示新昵称/头像
            RefreshSettingsPage();
        }

        // ---------- 修改昵称 ----------

        private static void ShowRenamePage()
        {
            var win = BuildInnerPanel("修改昵称", ShowRenamePage);
            if (win == null) return;
            var p = AccountService.Profile;
            if (p == null) return;
            win.Add(AccountHint($"昵称会显示在平板的账号页和通知中，最多 {AccountService.MaxNicknameLength} 个字。"));
            var field = AccountField("昵称", false, AccountService.MaxNicknameLength, p.nickname);
            field.name = "account-rename-field";
            win.Add(field);
            var err = AccountErrorLabel();
            win.Add(err);
            win.Add(AccountPrimaryButton("保存", () =>
            {
                string e = AccountService.Rename(field.value);
                if (e != null) { err.text = e; return; }
                FinishAccountSubPage("昵称已更新");
            }));
        }

        // ---------- 更换头像 ----------

        private static void ShowAvatarPage()
        {
            var win = BuildInnerPanel("更换头像", ShowAvatarPage);
            if (win == null) return;
            var p = AccountService.Profile;
            if (p == null) return;
            win.Add(AccountHint("选一个头像，或者从电脑里选一张图片。"));
            var grid = new VisualElement();
            grid.name = "account-avatar-grid";
            grid.style.flexDirection = FlexDirection.Row;
            grid.style.flexWrap = Wrap.Wrap;
            grid.style.marginTop = 16;
            bool custom = !string.IsNullOrEmpty(p.avatarImage);
            Action<VisualElement, bool> ring = (e, on) =>
            {
                float w = on ? 4f : 0f;
                e.style.borderTopWidth = e.style.borderBottomWidth = e.style.borderLeftWidth = e.style.borderRightWidth = w;
                Color bc = new Color(0.1f, 0.1f, 0.12f, 0.85f);
                e.style.borderTopColor = e.style.borderBottomColor = e.style.borderLeftColor = e.style.borderRightColor = bc;
            };
            for (int i = 0; i < TabletAvatar.PresetCount; i++)
            {
                int idx = i;
                var c = TabletAvatar.BuildPreset(i, 80);
                c.style.marginRight = 14; c.style.marginBottom = 14;
                ring(c, !custom && i == p.avatar);
                c.RegisterCallback<ClickEvent>(_ =>
                {
                    AccountService.SetAvatar(idx);
                    FinishAccountSubPage("头像已更新");
                });
                grid.Add(c);
            }
            var tile = TabletAvatar.BuildCustomTile(80, custom ? TabletAvatar.LoadTexture(p) : null);
            tile.style.marginRight = 14; tile.style.marginBottom = 14;
            ring(tile, custom);
            tile.RegisterCallback<ClickEvent>(_ => PickCustomAvatar());
            grid.Add(tile);
            win.Add(grid);

            var pick = AccountLinkButton("从电脑选择图片…", PickCustomAvatar);
            pick.name = "account-avatar-pick";
            pick.style.alignSelf = Align.FlexStart;
            win.Add(pick);
        }

        private static void PickCustomAvatar()
        {
            if (!TabletFilePicker.Available) { ShowToast("当前平台不支持选择本地图片"); return; }
            string path = TabletFilePicker.PickImage("选择头像图片");
            if (string.IsNullOrEmpty(path)) return;
            string err = AccountService.SetCustomAvatar(path);
            if (err != null) { ShowToast(err); return; }
            FinishAccountSubPage("头像已更新");
        }

        // ---------- 修改密码 ----------

        private static void ShowChangePasswordPage()
        {
            var win = BuildInnerPanel("修改密码", ShowChangePasswordPage);
            if (win == null) return;
            var p = AccountService.Profile;
            if (p == null) return;
            win.Add(AccountHint($"为了账号安全，请先输入当前密码。新密码需 {AccountService.MinPasswordLength}–{AccountService.MaxPasswordLength} 位，同时包含字母和数字。"));

            var oldPw = AccountField("当前密码", true, AccountService.MaxPasswordLength);
            oldPw.name = "account-old-password";
            var newPw = AccountField("新密码", true, AccountService.MaxPasswordLength);
            newPw.name = "account-new-password";
            var confirm = AccountField("再次输入新密码", true, AccountService.MaxPasswordLength);
            confirm.name = "account-confirm-password";
            win.Add(oldPw);
            win.Add(newPw);
            win.Add(confirm);
            win.Add(BuildStrengthBar(newPw));

            TabletPasswordEye.Attach(InkSecondary, oldPw, newPw, confirm);

            var err = AccountErrorLabel();
            win.Add(err);
            win.Add(AccountPrimaryButton("确认修改", () =>
            {
                if (newPw.value != confirm.value) { err.text = "两次输入的新密码不一致"; return; }
                string e = AccountService.ChangePassword(oldPw.value, newPw.value);
                if (e != null) { err.text = e; return; }
                PostSystemNotice("小米账号", "你的账号密码已于 " + DateTime.Now.ToString("HH:mm") + " 修改。如非本人操作，请尽快联系系统管理员。");
                FinishAccountSubPage("密码已修改");
            }));
            win.Add(AccountLinkButton("忘记当前密码？", () => ShowResetPasswordPage()));
        }

        // ---------- 忘记密码（短信验证） ----------

        private static void ShowResetPasswordPage()
        {
            var win = BuildInnerPanel("重置密码", ShowResetPasswordPage);
            if (win == null) return;
            var p = AccountService.Profile;
            if (p == null) return;
            win.Add(AccountHint("为绑定手机 " + AccountService.MaskedPhone(p) + " 生成本地验证码，验证后即可设置新密码。验证码显示在通知中心，通知不可用时显示在本页。"));

            var code = AccountField("6 位验证码", false, 6);
            code.name = "account-reset-code";
            win.Add(code);

            var delivery = AccountHint(string.Empty);
            delivery.name = "account-reset-code-delivery";
            delivery.style.display = DisplayStyle.None;
            win.Add(delivery);

            float readyAt = 0f;
            var send = AccountLinkButton("获取验证码", () => { });
            send.name = "account-reset-send";
            send.style.alignSelf = Align.FlexStart;
            send.clicked += () =>
            {
                if (Time.realtimeSinceStartup < readyAt) return;
                string c = AccountService.IssueCode(p.phone);
                readyAt = Time.realtimeSinceStartup + 30f;
                long notification = RailwayRenaissance.UI.Tablet.SystemShell.TabletNotify.PostNotice("短信", "【小米】你的验证码是 " + c + "，5 分钟内有效。请勿向他人泄露。");
                bool delivered = notification >= 0;
                delivery.text = delivered ? string.Empty : "本地验证码为 " + c + "，5 分钟内有效。";
                delivery.style.display = delivered ? DisplayStyle.None : DisplayStyle.Flex;
                ShowToast(delivered ? "验证码已生成，请在通知中心查看" : "验证码已生成，请在本页查看");
            };
            send.schedule.Execute(() =>
            {
                float left = readyAt - Time.realtimeSinceStartup;
                send.text = left > 0 ? $"重新获取（{Mathf.CeilToInt(left)}s）" : "获取验证码";
                send.SetEnabled(left <= 0);
            }).Every(500);
            win.Add(send);

            var newPw = AccountField("新密码", true, AccountService.MaxPasswordLength);
            newPw.name = "account-reset-new-password";
            var confirm = AccountField("再次输入新密码", true, AccountService.MaxPasswordLength);
            confirm.name = "account-reset-confirm-password";
            win.Add(newPw);
            win.Add(confirm);
            win.Add(BuildStrengthBar(newPw));
            TabletPasswordEye.Attach(InkSecondary, newPw, confirm);

            var err = AccountErrorLabel();
            win.Add(err);
            win.Add(AccountPrimaryButton("重置密码", () =>
            {
                // 先校验新密码与确认，再消耗验证码：否则密码有误时验证码已作废，改正后还得重新获取
                string e = AccountService.ValidatePassword(newPw.value, p.phone);
                if (e != null) { err.text = e; return; }
                if (newPw.value != confirm.value) { err.text = "两次输入的新密码不一致"; return; }
                e = AccountService.VerifyCode(p.phone, code.value);
                if (e != null) { err.text = e; return; }
                e = AccountService.ResetPassword(newPw.value);
                if (e != null) { err.text = e; return; }
                PostSystemNotice("小米账号", "你的账号密码已通过短信验证重置。");
                FinishAccountSubPage("密码已重置");
            }));
        }
    }
}
