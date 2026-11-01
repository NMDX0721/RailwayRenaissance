using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace RailwayRenaissance.UI.Tablet.Account
{
    /// <summary>
    /// 平板“开机激活 / 注册小米账号”向导，参考小米设备首次开机的设置向导：
    /// 欢迎 → 用户协议 → 网络 → 登录小米账号 → 手机号 → 验证码 → 设置密码 → 完善资料 → 完成。
    /// 账号只保存在本机（见 <see cref="AccountService"/>）；验证码以“短信”形式在界面里弹出，不联网。
    /// 可由剧情（Day0 拆封平板）或“设置 → 小米账号”调用。
    /// </summary>
    public sealed class TabletSetupWizard
    {
        public enum Flow { Activation, RegisterOnly }

        private static readonly Color[] AvatarColors =
        {
            new Color(0.97f, 0.52f, 0.30f), new Color(0.30f, 0.60f, 0.95f), new Color(0.25f, 0.75f, 0.50f),
            new Color(0.62f, 0.45f, 0.95f), new Color(0.95f, 0.45f, 0.62f), new Color(0.35f, 0.72f, 0.82f)
        };

        private readonly Flow _flow;
        private readonly bool _allowSkip;
        private readonly Action<bool> _onFinished;
        private readonly Action<string, string> _smsSink;
        private readonly List<string> _steps = new List<string>();
        private readonly Font _font;
        private readonly Font _fontBold;

        private VisualElement _root;
        private VisualElement _body;
        private VisualElement _dots;
        private VisualElement _smsBanner;
        private Button _backBtn;
        private IVisualElementScheduledItem _timer;
        private int _index;
        private bool _finished;

        // 向导内收集的数据
        private string _phone = string.Empty;
        private string _password = string.Empty;
        private string _nickname = "旅人";
        private int _avatar;
        private bool _agreedDevice;
        private bool _agreedAccount;
        private float _resendReadyAt;

        public VisualElement Root => _root;
        public string CurrentStep => _index >= 0 && _index < _steps.Count ? _steps[_index] : string.Empty;

        private TabletSetupWizard(Flow flow, bool allowSkip, Action<bool> onFinished, Action<string, string> smsSink)
        {
            _flow = flow;
            _allowSkip = allowSkip;
            _onFinished = onFinished;
            _smsSink = smsSink;
            _font = TabletFonts.Ui();
            _fontBold = TabletFonts.UiBold();
        }

        /// <param name="host">向导铺满的父元素（剧情根或平板根）。</param>
        /// <param name="allowSkip">是否显示“暂不登录”。剧情里拆封平板这一幕不允许跳过。</param>
        /// <param name="onFinished">参数 true 表示已创建账号，false 表示暂不登录。</param>
        /// <param name="smsSink">验证码短信的额外去处（如平板通知中心），可为空。</param>
        public static TabletSetupWizard Show(VisualElement host, Flow flow, bool allowSkip, Action<bool> onFinished, Action<string, string> smsSink = null)
        {
            var wizard = new TabletSetupWizard(flow, allowSkip, onFinished, smsSink);
            wizard.Build(host);
            return wizard;
        }

        public void Close()
        {
            _timer?.Pause();
            _timer = null;
            if (_root != null && _root.parent != null) _root.RemoveFromHierarchy();
        }

        // ================= 骨架 =================

        private void Build(VisualElement host)
        {
            if (_flow == Flow.Activation)
            {
                _steps.Add("welcome");
                _steps.Add("agreement");
                _steps.Add("network");
            }
            if (!AccountService.IsRegistered)
            {
                if (_flow == Flow.Activation) _steps.Add("account");
                _steps.Add("phone");
                _steps.Add("code");
                _steps.Add("password");
                _steps.Add("profile");
            }
            _steps.Add("done");

            _root = new VisualElement { name = "tablet-setup-wizard" };
            _root.style.position = Position.Absolute;
            _root.style.left = 0; _root.style.top = 0; _root.style.right = 0; _root.style.bottom = 0;
            _root.style.backgroundColor = new Color(0.965f, 0.965f, 0.975f, 1f);
            _root.style.alignItems = Align.Center;
            _root.pickingMode = PickingMode.Position;

            var top = new VisualElement();
            top.style.height = 64;
            top.style.width = Length.Percent(100);
            top.style.flexDirection = FlexDirection.Row;
            top.style.alignItems = Align.Center;
            top.style.justifyContent = Justify.SpaceBetween;
            top.style.paddingLeft = 20; top.style.paddingRight = 20;
            top.style.flexShrink = 0;
            _root.Add(top);

            _backBtn = MakeTextButton("‹ 返回", () => Go(_index - 1));
            _backBtn.name = "wizard-back";
            top.Add(_backBtn);

            _dots = new VisualElement();
            _dots.style.flexDirection = FlexDirection.Row;
            _dots.style.alignItems = Align.Center;
            top.Add(_dots);

            var spacer = new VisualElement();
            spacer.style.width = 72;
            top.Add(spacer);

            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            scroll.style.flexGrow = 1;
            scroll.style.width = Length.Percent(100);
            _root.Add(scroll);

            _body = new VisualElement { name = "wizard-body" };
            _body.style.alignSelf = Align.Center;
            _body.style.width = Length.Percent(92);
            _body.style.maxWidth = 620;
            _body.style.paddingBottom = 40;
            scroll.Add(_body);

            _smsBanner = new VisualElement { name = "wizard-sms-banner", pickingMode = PickingMode.Ignore };
            _smsBanner.style.position = Position.Absolute;
            _smsBanner.style.top = 14;
            _smsBanner.style.left = Length.Percent(50);
            _smsBanner.style.translate = new Translate(Length.Percent(-50), 0);
            _smsBanner.style.width = 460;
            _smsBanner.style.maxWidth = Length.Percent(92);
            _smsBanner.style.display = DisplayStyle.None;
            _root.Add(_smsBanner);

            host.Add(_root);
            Go(0);
        }

        private void Go(int index)
        {
            if (_finished) return;
            _timer?.Pause();
            _timer = null;
            if (index < 0) return;
            if (index >= _steps.Count) { Finish(AccountService.IsRegistered); return; }
            _index = index;
            _body.Clear();
            _backBtn.style.visibility = (_index == 0 || CurrentStep == "done") ? Visibility.Hidden : Visibility.Visible;
            RefreshDots();
            switch (CurrentStep)
            {
                case "welcome": BuildWelcome(); break;
                case "agreement": BuildAgreement(); break;
                case "network": BuildNetwork(); break;
                case "account": BuildAccountIntro(); break;
                case "phone": BuildPhone(); break;
                case "code": BuildCode(); break;
                case "password": BuildPassword(); break;
                case "profile": BuildProfile(); break;
                case "done": BuildDone(); break;
            }
        }

        private void Finish(bool registered)
        {
            if (_finished) return;
            _finished = true;
            _timer?.Pause();
            AccountService.SetupDone = true;
            Close();
            _onFinished?.Invoke(registered);
        }

        private void RefreshDots()
        {
            _dots.Clear();
            for (int i = 0; i < _steps.Count; i++)
            {
                var dot = new VisualElement();
                bool active = i == _index;
                dot.style.width = active ? 22 : 8; dot.style.height = 8;
                dot.style.marginLeft = 4; dot.style.marginRight = 4;
                dot.style.borderTopLeftRadius = dot.style.borderTopRightRadius = 4;
                dot.style.borderBottomLeftRadius = dot.style.borderBottomRightRadius = 4;
                dot.style.backgroundColor = active ? TabletTheme.AccentOnLight : new Color(0, 0, 0, 0.14f);
                _dots.Add(dot);
            }
        }

        // ================= 各步骤 =================

        private void BuildWelcome()
        {
            var hero = new VisualElement();
            hero.style.alignItems = Align.Center;
            hero.style.marginTop = 70; hero.style.marginBottom = 40;
            _body.Add(hero);
            hero.Add(Text("Xiaomi HyperOS", 52, new Color(0.55f, 0.12f, 0.36f), true));
            var sub = Text("欢迎使用 澎湃OS 32", 20, new Color(0.30f, 0.28f, 0.36f));
            sub.style.marginTop = 12;
            hero.Add(sub);

            var card = Card();
            card.Add(InfoRow("语言", "简体中文"));
            card.Add(InfoRow("地区", "朝鲜（阿里郎内网）"));
            _body.Add(card);

            _body.Add(Primary("开始", () => Go(_index + 1)));
        }

        private void BuildAgreement()
        {
            _body.Add(Title("用户协议与隐私政策", "使用本设备前，请阅读并同意以下条款"));

            var card = Card();
            card.style.maxHeight = 260;
            var inner = new ScrollView(ScrollViewMode.Vertical);
            inner.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            inner.style.maxHeight = 240;
            inner.Add(Body("《小米用户协议》摘要：本设备及其系统软件由嘉颖徐铁路基金配发，供铁路运营与调度工作使用。使用本设备即表示你同意遵守车载内网的使用规范。"));
            inner.Add(Body("《隐私政策》摘要：本设备运行在车载内网沙箱中，不连接公共网络。你在本设备上创建的账号与资料只保存在本机；音乐应用的在线账号由对应服务独立处理。"));
            inner.Add(Body("以上为游戏内虚构条款，仅用于营造设备首次使用的氛围。"));
            card.Add(inner);
            _body.Add(card);

            var agreeRow = CheckRow("我已阅读并同意《小米用户协议》和《隐私政策》", _agreedDevice, v => { _agreedDevice = v; });
            _body.Add(agreeRow.row);

            var next = Primary("同意并继续", () =>
            {
                if (!_agreedDevice) { Toast("请先勾选同意用户协议与隐私政策"); return; }
                Go(_index + 1);
            });
            _body.Add(next);
            _body.Add(Secondary("不同意", () => Toast("需要同意用户协议才能继续使用本设备")));
        }

        private void BuildNetwork()
        {
            _body.Add(Title("连接网络", "本设备使用车载内网"));
            var card = Card();
            card.Add(InfoRow("阿里郎内网", "已连接"));
            _body.Add(card);
            _body.Add(Body("此终端运行在车载内网沙箱中，没有公共 Wi-Fi 与蜂窝网络，无需手动配置。"));
            _body.Add(Primary("下一步", () => Go(_index + 1)));
        }

        private void BuildAccountIntro()
        {
            _body.Add(Title("登录小米账号", "登录后可使用云空间、查找设备与家庭服务"));
            var card = Card();
            card.Add(InfoRow("云空间", "同步设置与资料"));
            card.Add(InfoRow("查找设备", "定位这台平板"));
            card.Add(InfoRow("家庭服务", "与家人共享"));
            _body.Add(card);
            _body.Add(Primary("注册小米账号", () => Go(_index + 1)));
            if (_allowSkip)
            {
                _body.Add(Secondary("暂不登录", () =>
                {
                    ShowSkipConfirm();
                }));
            }
        }

        private void ShowSkipConfirm()
        {
            _body.Clear();
            _body.Add(Title("暂不登录？", "跳过后部分功能需要登录小米账号才能使用"));
            _body.Add(Body("你可以稍后在“设置 → 小米账号”中登录或注册。"));
            _body.Add(Primary("返回登录", () => Go(_index)));
            _body.Add(Secondary("暂不登录", () => Finish(false)));
        }

        private void BuildPhone()
        {
            _body.Add(Title("注册小米账号", "使用手机号注册，我们会发送验证码"));

            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginBottom = 12;
            var region = new Label(AccountService.DefaultRegion);
            Style(region, 16, new Color(0.10f, 0.10f, 0.12f), false);
            region.style.height = 52; region.style.minWidth = 120;
            region.style.unityTextAlign = TextAnchor.MiddleCenter;
            region.style.backgroundColor = Color.white;
            RoundAll(region, 14);
            region.style.marginRight = 10;
            row.Add(region);
            var phoneField = Field("手机号", false, 11, _phone);
            phoneField.name = "wizard-phone";
            phoneField.style.flexGrow = 1;
            row.Add(phoneField);
            _body.Add(row);

            var err = ErrorLabel();
            _body.Add(err);
            phoneField.RegisterValueChangedCallback(evt =>
            {
                string digits = OnlyDigits(evt.newValue);
                if (digits != evt.newValue) phoneField.SetValueWithoutNotify(digits);
                _phone = digits;
                err.text = string.Empty;
            });

            var agreeRow = CheckRow("同意《小米账号用户协议》和《隐私政策》", _agreedAccount, v => { _agreedAccount = v; });
            _body.Add(agreeRow.row);

            _body.Add(Primary("获取验证码", () =>
            {
                string e = AccountService.ValidatePhone(_phone);
                if (e != null) { err.text = e; return; }
                if (!_agreedAccount) { err.text = "请先勾选同意账号用户协议与隐私政策"; return; }
                err.text = string.Empty;
                SendCode();
                Go(_index + 1);
            }));
        }

        private void BuildCode()
        {
            _body.Add(Title("输入验证码", "验证码已发送至 " + AccountService.DefaultRegion.Substring(AccountService.DefaultRegion.IndexOf('+')) + " " + _phone));

            var codeField = Field("6 位验证码", false, 6, string.Empty);
            codeField.name = "wizard-code";
            codeField.RegisterValueChangedCallback(evt =>
            {
                string digits = OnlyDigits(evt.newValue);
                if (digits != evt.newValue) codeField.SetValueWithoutNotify(digits);
            });
            _body.Add(codeField);

            var err = ErrorLabel();
            _body.Add(err);
            codeField.RegisterValueChangedCallback(_ => err.text = string.Empty);

            var resend = Secondary("重新获取", () => { });
            resend.name = "wizard-resend";
            _body.Add(resend);
            resend.clicked += () =>
            {
                if (Time.realtimeSinceStartup < _resendReadyAt) return;
                SendCode();
                err.text = string.Empty;
            };
            _timer = resend.schedule.Execute(() =>
            {
                float left = _resendReadyAt - Time.realtimeSinceStartup;
                resend.text = left > 0 ? $"重新获取（{Mathf.CeilToInt(left)}s）" : "重新获取";
                resend.SetEnabled(left <= 0);
            }).Every(500);

            _body.Add(Primary("下一步", () =>
            {
                string e = AccountService.VerifyCode(_phone, codeField.value);
                if (e != null) { err.text = e; return; }
                err.text = string.Empty;
                Go(_index + 1);
            }));
        }

        private void BuildPassword()
        {
            _body.Add(Title("设置密码", "用于登录小米账号，请牢记"));

            var pw = Field("密码", true, AccountService.MaxPasswordLength, _password);
            pw.name = "wizard-password";
            var confirm = Field("再次输入密码", true, AccountService.MaxPasswordLength, string.Empty);
            confirm.name = "wizard-password-confirm";
            _body.Add(pw);
            confirm.style.marginTop = 10;
            _body.Add(confirm);

            var strengthRow = new VisualElement();
            strengthRow.style.flexDirection = FlexDirection.Row;
            strengthRow.style.alignItems = Align.Center;
            strengthRow.style.marginTop = 12;
            var bars = new VisualElement[3];
            for (int i = 0; i < 3; i++)
            {
                bars[i] = new VisualElement();
                bars[i].style.height = 6; bars[i].style.flexGrow = 1;
                bars[i].style.marginRight = 6;
                RoundAll(bars[i], 3);
                strengthRow.Add(bars[i]);
            }
            var strengthLabel = Text("", 13, new Color(0.45f, 0.45f, 0.5f));
            strengthLabel.style.width = 64; strengthLabel.style.unityTextAlign = TextAnchor.MiddleRight;
            strengthRow.Add(strengthLabel);
            _body.Add(strengthRow);

            _body.Add(Body($"{AccountService.MinPasswordLength}–{AccountService.MaxPasswordLength} 位，需同时包含字母和数字。"));

            var showBtn = MakeTextButton("显示密码", () => { });
            showBtn.style.alignSelf = Align.FlexStart;
            bool shown = false;
            showBtn.clicked += () =>
            {
                shown = !shown;
                pw.isPasswordField = !shown;
                confirm.isPasswordField = !shown;
                showBtn.text = shown ? "隐藏密码" : "显示密码";
            };
            _body.Add(showBtn);

            var err = ErrorLabel();
            _body.Add(err);

            Action refresh = () =>
            {
                int level = string.IsNullOrEmpty(pw.value) ? -1 : AccountService.PasswordStrength(pw.value);
                Color[] palette = { new Color(0.93f, 0.35f, 0.30f), new Color(0.97f, 0.70f, 0.20f), new Color(0.25f, 0.75f, 0.45f) };
                for (int i = 0; i < 3; i++)
                    bars[i].style.backgroundColor = i <= level ? palette[Mathf.Max(level, 0)] : new Color(0, 0, 0, 0.10f);
                strengthLabel.text = level < 0 ? string.Empty : level == 0 ? "弱" : level == 1 ? "中" : "强";
            };
            pw.RegisterValueChangedCallback(_ => { refresh(); err.text = string.Empty; });
            confirm.RegisterValueChangedCallback(_ => err.text = string.Empty);
            refresh();

            _body.Add(Primary("下一步", () =>
            {
                string e = AccountService.ValidatePassword(pw.value, _phone);
                if (e != null) { err.text = e; return; }
                if (pw.value != confirm.value) { err.text = "两次输入的密码不一致"; return; }
                _password = pw.value;
                err.text = string.Empty;
                Go(_index + 1);
            }));
        }

        private void BuildProfile()
        {
            _body.Add(Title("完善资料", "设置你的昵称和头像"));

            var avatarRow = new VisualElement();
            avatarRow.style.flexDirection = FlexDirection.Row;
            avatarRow.style.justifyContent = Justify.Center;
            avatarRow.style.marginBottom = 18;
            var circles = new List<VisualElement>();
            var nameField = Field("昵称", false, AccountService.MaxNicknameLength, _nickname);
            nameField.name = "wizard-nickname";

            Action refreshAvatars = null;
            for (int i = 0; i < AvatarColors.Length; i++)
            {
                int idx = i;
                var c = new Label();
                c.style.width = 56; c.style.height = 56;
                c.style.marginLeft = 6; c.style.marginRight = 6;
                c.style.unityTextAlign = TextAnchor.MiddleCenter;
                Style(c, 24, Color.white, true);
                RoundAll(c, 28);
                c.style.backgroundColor = AvatarColors[i];
                c.RegisterCallback<ClickEvent>(_ => { _avatar = idx; refreshAvatars(); });
                circles.Add(c);
                avatarRow.Add(c);
            }
            refreshAvatars = () =>
            {
                string initial = string.IsNullOrWhiteSpace(nameField.value) ? "旅" : nameField.value.Trim().Substring(0, 1);
                for (int i = 0; i < circles.Count; i++)
                {
                    var lab = (Label)circles[i];
                    lab.text = i == _avatar ? initial : string.Empty;
                    float w = i == _avatar ? 3f : 0f;
                    lab.style.borderTopWidth = lab.style.borderBottomWidth = lab.style.borderLeftWidth = lab.style.borderRightWidth = w;
                    Color bc = new Color(0.1f, 0.1f, 0.12f, 0.85f);
                    lab.style.borderTopColor = lab.style.borderBottomColor = lab.style.borderLeftColor = lab.style.borderRightColor = bc;
                }
            };
            nameField.RegisterValueChangedCallback(_ => refreshAvatars());
            refreshAvatars();
            _body.Add(avatarRow);
            _body.Add(nameField);

            var err = ErrorLabel();
            _body.Add(err);

            _body.Add(Primary("完成注册", () =>
            {
                string e = AccountService.ValidateNickname(nameField.value);
                if (e != null) { err.text = e; return; }
                _nickname = nameField.value.Trim();
                e = AccountService.Register(_phone, _password, _nickname, _avatar);
                if (e != null) { err.text = e; return; }
                Go(_index + 1);
            }));
        }

        private void BuildDone()
        {
            _backBtn.style.visibility = Visibility.Hidden;
            var p = AccountService.Profile;
            var hero = new VisualElement();
            hero.style.alignItems = Align.Center;
            hero.style.marginTop = 60; hero.style.marginBottom = 28;
            _body.Add(hero);
            hero.Add(Text(p != null ? "小米账号已创建" : "设置完成", 34, new Color(0.10f, 0.10f, 0.12f), true));
            var sub = Text(p != null ? "欢迎，" + p.nickname : "你可以稍后在“设置 → 小米账号”中登录", 18, new Color(0.40f, 0.40f, 0.46f));
            sub.style.marginTop = 10;
            hero.Add(sub);

            if (p != null)
            {
                var card = Card();
                card.Add(InfoRow("昵称", p.nickname));
                card.Add(InfoRow("小米 ID", p.xiaomiId));
                card.Add(InfoRow("手机号", AccountService.MaskedPhone(p)));
                _body.Add(card);
            }
            _body.Add(Primary("开始使用", () => Finish(p != null)));
        }

        // ================= 验证码短信 =================

        private void SendCode()
        {
            string code = AccountService.IssueCode(_phone);
            _resendReadyAt = Time.realtimeSinceStartup + 60f;
            string body = $"【小米】你的验证码是 {code}，5 分钟内有效。请勿向他人泄露。";
            ShowSms(body);
            _smsSink?.Invoke("短信", body);
        }

        private void ShowSms(string text)
        {
            _smsBanner.Clear();
            var card = new VisualElement();
            card.style.backgroundColor = new Color(0.12f, 0.12f, 0.14f, 0.96f);
            RoundAll(card, 20);
            card.style.paddingLeft = 18; card.style.paddingRight = 18;
            card.style.paddingTop = 12; card.style.paddingBottom = 12;
            var head = Text("短信 · 现在", 12, new Color(0.72f, 0.72f, 0.78f));
            card.Add(head);
            var body = Text(text, 15, Color.white);
            body.style.whiteSpace = WhiteSpace.Normal;
            body.style.marginTop = 4;
            card.Add(body);
            _smsBanner.Add(card);
            _smsBanner.style.display = DisplayStyle.Flex;
            _smsBanner.schedule.Execute(() => _smsBanner.style.display = DisplayStyle.None).StartingIn(5000);
        }

        // ================= 控件 =================

        private void Style(Label l, int size, Color color, bool bold)
        {
            l.style.fontSize = size;
            l.style.color = color;
            var f = bold ? _fontBold : _font;
            if (f != null) l.style.unityFontDefinition = FontDefinition.FromFont(f);
        }

        private Label Text(string text, int size, Color color, bool bold = false)
        {
            var l = new Label(text);
            Style(l, size, color, bold);
            l.pickingMode = PickingMode.Ignore;
            return l;
        }

        private VisualElement Title(string title, string subtitle)
        {
            var box = new VisualElement();
            box.style.marginTop = 24; box.style.marginBottom = 22;
            var t = Text(title, 34, new Color(0.10f, 0.10f, 0.12f), true);
            box.Add(t);
            if (!string.IsNullOrEmpty(subtitle))
            {
                var s = Text(subtitle, 16, new Color(0.45f, 0.45f, 0.50f));
                s.style.marginTop = 8; s.style.whiteSpace = WhiteSpace.Normal;
                box.Add(s);
            }
            return box;
        }

        private Label Body(string text)
        {
            var l = Text(text, 14, new Color(0.40f, 0.40f, 0.46f));
            l.style.whiteSpace = WhiteSpace.Normal;
            l.style.marginTop = 6; l.style.marginBottom = 6;
            return l;
        }

        private Label ErrorLabel()
        {
            var l = Text(string.Empty, 14, new Color(0.88f, 0.28f, 0.24f));
            l.name = "wizard-error";
            l.style.whiteSpace = WhiteSpace.Normal;
            l.style.marginTop = 6; l.style.marginBottom = 6;
            return l;
        }

        private VisualElement Card()
        {
            var c = new VisualElement();
            c.style.backgroundColor = Color.white;
            RoundAll(c, 22);
            c.style.paddingLeft = 20; c.style.paddingRight = 20;
            c.style.paddingTop = 6; c.style.paddingBottom = 6;
            c.style.marginBottom = 16;
            return c;
        }

        private VisualElement InfoRow(string title, string value)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.justifyContent = Justify.SpaceBetween;
            row.style.alignItems = Align.Center;
            row.style.paddingTop = 15; row.style.paddingBottom = 15;
            row.style.borderBottomWidth = 1;
            row.style.borderBottomColor = new Color(0, 0, 0, 0.06f);
            row.Add(Text(title, 16, new Color(0.10f, 0.10f, 0.12f)));
            var v = Text(value, 15, new Color(0.55f, 0.55f, 0.60f));
            v.style.whiteSpace = WhiteSpace.Normal;
            row.Add(v);
            return row;
        }

        private (VisualElement row, Action<bool> set) CheckRow(string text, bool initial, Action<bool> onChanged)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginTop = 10; row.style.marginBottom = 14;
            row.pickingMode = PickingMode.Position;
            bool value = initial;
            var box = new Label();
            box.style.width = 24; box.style.height = 24;
            box.style.marginRight = 12;
            box.style.unityTextAlign = TextAnchor.MiddleCenter;
            Style(box, 16, Color.white, true);
            RoundAll(box, 12);
            box.pickingMode = PickingMode.Ignore;
            Action paint = () =>
            {
                box.text = value ? "✓" : string.Empty;
                box.style.backgroundColor = value ? TabletTheme.AccentOnLight : Color.white;
                box.style.borderTopWidth = box.style.borderBottomWidth = box.style.borderLeftWidth = box.style.borderRightWidth = 2;
                Color bc = value ? TabletTheme.AccentOnLight : new Color(0, 0, 0, 0.25f);
                box.style.borderTopColor = box.style.borderBottomColor = box.style.borderLeftColor = box.style.borderRightColor = bc;
            };
            paint();
            row.Add(box);
            var label = Text(text, 15, new Color(0.25f, 0.25f, 0.30f));
            label.style.whiteSpace = WhiteSpace.Normal; label.style.flexShrink = 1;
            row.Add(label);
            row.RegisterCallback<ClickEvent>(_ => { value = !value; paint(); onChanged?.Invoke(value); });
            return (row, v => { value = v; paint(); });
        }

        private TextField Field(string placeholder, bool password, int maxLength, string value)
        {
            var tf = new TextField { value = value ?? string.Empty };
            if (maxLength > 0) tf.maxLength = maxLength;
            tf.isPasswordField = password;
            if (password) tf.maskChar = '•';
            tf.textEdition.placeholder = placeholder;
            tf.textEdition.hidePlaceholderOnFocus = true;
            tf.style.height = 52;
            tf.style.marginLeft = 0; tf.style.marginRight = 0; tf.style.marginTop = 0; tf.style.marginBottom = 0;
            tf.style.backgroundColor = Color.white;
            RoundAll(tf, 14);
            tf.style.paddingLeft = 12; tf.style.paddingRight = 12;
            var input = tf.Q(className: "unity-base-text-field__input");
            if (input != null)
            {
                input.style.backgroundColor = Color.clear;
                input.style.borderTopWidth = input.style.borderBottomWidth = input.style.borderLeftWidth = input.style.borderRightWidth = 0;
                input.style.fontSize = 17;
                input.style.color = new Color(0.10f, 0.10f, 0.12f);
                if (_font != null) input.style.unityFontDefinition = FontDefinition.FromFont(_font);
            }
            return tf;
        }

        private Button Primary(string text, Action onClick)
        {
            var b = new Button(onClick) { text = text };
            b.style.height = 56;
            b.style.marginTop = 12;
            b.style.fontSize = 18;
            b.style.color = Color.white;
            b.style.backgroundColor = TabletTheme.AccentOnLight;
            b.style.borderTopWidth = b.style.borderBottomWidth = b.style.borderLeftWidth = b.style.borderRightWidth = 0;
            RoundAll(b, 28);
            if (_fontBold != null) b.style.unityFontDefinition = FontDefinition.FromFont(_fontBold);
            return b;
        }

        private Button Secondary(string text, Action onClick)
        {
            var b = new Button(onClick) { text = text };
            b.style.height = 52;
            b.style.marginTop = 8;
            b.style.fontSize = 16;
            b.style.color = new Color(0.30f, 0.30f, 0.36f);
            b.style.backgroundColor = new Color(0, 0, 0, 0.06f);
            b.style.borderTopWidth = b.style.borderBottomWidth = b.style.borderLeftWidth = b.style.borderRightWidth = 0;
            RoundAll(b, 26);
            if (_font != null) b.style.unityFontDefinition = FontDefinition.FromFont(_font);
            return b;
        }

        private Button MakeTextButton(string text, Action onClick)
        {
            var b = new Button(onClick) { text = text };
            b.style.height = 44;
            b.style.width = 72;
            b.style.fontSize = 16;
            b.style.color = TabletTheme.AccentOnLight;
            b.style.backgroundColor = Color.clear;
            b.style.borderTopWidth = b.style.borderBottomWidth = b.style.borderLeftWidth = b.style.borderRightWidth = 0;
            b.style.unityTextAlign = TextAnchor.MiddleLeft;
            if (_font != null) b.style.unityFontDefinition = FontDefinition.FromFont(_font);
            return b;
        }

        private void Toast(string message)
        {
            ShowSms(message);   // 复用顶部提示条
        }

        private static void RoundAll(VisualElement e, float r)
        {
            e.style.borderTopLeftRadius = e.style.borderTopRightRadius = r;
            e.style.borderBottomLeftRadius = e.style.borderBottomRightRadius = r;
        }

        private static string OnlyDigits(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            var sb = new System.Text.StringBuilder(s.Length);
            for (int i = 0; i < s.Length; i++) if (s[i] >= '0' && s[i] <= '9') sb.Append(s[i]);
            return sb.ToString();
        }
    }
}
