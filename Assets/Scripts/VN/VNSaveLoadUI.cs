using UnityEngine;
using UnityEngine.UIElements;

public class VNSaveLoadUI : MonoBehaviour
{
    private UIDocument uiDoc;
    private Font gameFont;
    private VNSaveSystem saveSystem;

    private VisualElement panel;
    private VisualElement header;
    private Label titleLabel;
    private UnityEngine.UIElements.Button closeBtn;
    private VisualElement slotContainer;

    private bool isOpen;
    private bool isSaveMode;
    private System.Action<int> onSlotSelected;
    private Coroutine feedbackRoutine;

    private void ShowSaveFeedback()
    {
        if (titleLabel == null) return;
        string original = titleLabel.text;
        titleLabel.text = "已保存";
        if (feedbackRoutine != null) StopCoroutine(feedbackRoutine);
        feedbackRoutine = StartCoroutine(RestoreTitleAfterDelay(original));
    }

    private System.Collections.IEnumerator RestoreTitleAfterDelay(string original)
    {
        yield return new WaitForSeconds(1.5f);
        if (titleLabel != null) titleLabel.text = original;
        feedbackRoutine = null;
    }

    public bool IsOpen => isOpen;

    private static readonly Color Gold = new Color(200f / 255f, 150f / 255f, 80f / 255f, 1f);
    private static readonly Color GoldDim = new Color(200f / 255f, 150f / 255f, 80f / 255f, 0.4f);
    private static readonly Color GoldBright = new Color(230f / 255f, 190f / 255f, 110f / 255f, 1f);
    private static readonly Color BgDark = new Color(0.08f, 0.05f, 0.03f, 0.92f);
    private static readonly Color SlotBg = new Color(0.12f, 0.07f, 0.04f, 0.85f);
    private static readonly Color SlotBgHover = new Color(0.18f, 0.11f, 0.06f, 0.9f);
    private static readonly Color BtnBg = new Color(0.25f, 0.14f, 0.08f, 0.9f);
    private static readonly Color BtnBgHover = new Color(0.35f, 0.2f, 0.1f, 0.95f);
    private static readonly Color DeleteBg = new Color(0.4f, 0.12f, 0.1f, 0.7f);
    private static readonly Color DeleteBgHover = new Color(0.55f, 0.15f, 0.12f, 0.9f);

    public void Init(UIDocument document, VNSaveSystem system)
    {
        uiDoc = document;
        saveSystem = system;
        gameFont = Resources.Load<Font>("Fonts/zpix");
        BuildUI();
    }

    private FontDefinition GetFontDef()
    {
        return new FontDefinition { font = gameFont };
    }

    private void BuildUI()
    {
        var root = uiDoc.rootVisualElement;
        var fontDef = GetFontDef();

        panel = new VisualElement { name = "saveload-panel" };
        panel.style.position = Position.Absolute;
        panel.style.top = 0;
        panel.style.left = 0;
        panel.style.right = 0;
        panel.style.bottom = 0;
        panel.style.backgroundColor = BgDark;
        panel.style.display = DisplayStyle.None;
        panel.pickingMode = PickingMode.Position;
        root.Add(panel);

        header = new VisualElement { name = "saveload-header" };
        header.style.flexDirection = FlexDirection.Row;
        header.style.justifyContent = Justify.SpaceBetween;
        header.style.alignItems = Align.Center;
        header.style.paddingLeft = 50;
        header.style.paddingRight = 50;
        header.style.paddingTop = 24;
        header.style.paddingBottom = 24;
        header.style.borderBottomWidth = 1;
        header.style.borderBottomColor = new Color(Gold.r, Gold.g, Gold.b, 0.2f);
        panel.Add(header);

        // Title with decorative element
        var titleGroup = new VisualElement();
        titleGroup.style.flexDirection = FlexDirection.Row;
        titleGroup.style.alignItems = Align.Center;

        var titleDeco = new Label("\u25C6");
        titleDeco.style.marginRight = 12;
        titleDeco.style.fontSize = 16;
        titleDeco.style.color = new Color(Gold.r, Gold.g, Gold.b, 0.5f);
        titleDeco.style.unityFontDefinition = fontDef;
        titleGroup.Add(titleDeco);

        titleLabel = new Label("存档");
        titleLabel.style.fontSize = 30;
        titleLabel.style.color = GoldBright;
        titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        titleLabel.style.unityFontDefinition = fontDef;
        titleGroup.Add(titleLabel);

        var titleDeco2 = new Label("\u25C6");
        titleDeco2.style.fontSize = 16;
        titleDeco2.style.color = new Color(Gold.r, Gold.g, Gold.b, 0.5f);
        titleDeco2.style.unityFontDefinition = fontDef;
        titleGroup.Add(titleDeco2);

        header.Add(titleGroup);

        closeBtn = new UnityEngine.UIElements.Button(() => ClosePanel()) { text = "\u2715" };
        closeBtn.style.width = 44;
        closeBtn.style.height = 44;
        closeBtn.style.fontSize = 20;
        closeBtn.style.color = new Color(1f, 1f, 1f, 0.6f);
        closeBtn.style.backgroundColor = new Color(0.2f, 0.12f, 0.08f, 0.5f);
        closeBtn.style.unityTextAlign = TextAnchor.MiddleCenter;
        closeBtn.style.unityFontDefinition = fontDef;
        closeBtn.style.borderTopWidth = 1;
        closeBtn.style.borderBottomWidth = 1;
        closeBtn.style.borderLeftWidth = 1;
        closeBtn.style.borderRightWidth = 1;
        closeBtn.style.borderTopColor = GoldDim;
        closeBtn.style.borderBottomColor = GoldDim;
        closeBtn.style.borderLeftColor = GoldDim;
        closeBtn.style.borderRightColor = GoldDim;
        closeBtn.style.borderTopLeftRadius = 6;
        closeBtn.style.borderTopRightRadius = 6;
        closeBtn.style.borderBottomLeftRadius = 6;
        closeBtn.style.borderBottomRightRadius = 6;

        closeBtn.RegisterCallback<PointerEnterEvent>(e =>
        {
            closeBtn.style.backgroundColor = new Color(0.4f, 0.15f, 0.1f, 0.8f);
            closeBtn.style.color = new Color(1f, 0.8f, 0.6f, 1f);
        });
        closeBtn.RegisterCallback<PointerLeaveEvent>(e =>
        {
            closeBtn.style.backgroundColor = new Color(0.2f, 0.12f, 0.08f, 0.5f);
            closeBtn.style.color = new Color(1f, 1f, 1f, 0.6f);
        });
        header.Add(closeBtn);

        // Slot container
        slotContainer = new VisualElement { name = "slot-container" };
        slotContainer.style.flexDirection = FlexDirection.Column;
        slotContainer.style.alignItems = Align.Center;
        slotContainer.style.justifyContent = Justify.Center;
        slotContainer.style.flexGrow = 1;
        slotContainer.style.paddingLeft = 30;
        slotContainer.style.paddingRight = 30;
        slotContainer.style.paddingTop = 16;
        slotContainer.style.paddingBottom = 20;
        panel.Add(slotContainer);
    }

    public void OpenSavePanel(System.Action<int> callback)
    {
        isSaveMode = true;
        onSlotSelected = callback;
        titleLabel.text = "存档";
        RefreshSlots();
        panel.style.display = DisplayStyle.Flex;
        isOpen = true;
    }

    public void OpenLoadPanel(System.Action<int> callback)
    {
        isSaveMode = false;
        onSlotSelected = callback;
        titleLabel.text = "读档";
        RefreshSlots();
        panel.style.display = DisplayStyle.Flex;
        isOpen = true;
    }

    public void ClosePanel()
    {
        panel.style.display = DisplayStyle.None;
        isOpen = false;
        onSlotSelected = null;
    }

    private void RefreshSlots()
    {
        slotContainer.Clear();
        slotContainer.style.flexDirection = FlexDirection.Row;
        slotContainer.style.flexWrap = Wrap.Wrap;
        slotContainer.style.justifyContent = Justify.SpaceBetween;
        var fontDef = GetFontDef();

        // Find latest save for slot 0
        VNSaveData latestSave = null;
        int latestSlot = -1;
        for (int i = 1; i < saveSystem.MaxSlotCount; i++)
        {
            var data = saveSystem.LoadGame(i);
            if (data != null)
            {
                // Try to parse timestamp to find the latest
                System.DateTime dt;
                if (System.DateTime.TryParse(data.timestamp, out dt))
                {
                    if (latestSave == null || dt > System.DateTime.Parse(latestSave.timestamp))
                    {
                        latestSave = data;
                        latestSlot = i;
                    }
                }
                else
                {
                    // Fallback: just take the last non-null
                    latestSave = data;
                    latestSlot = i;
                }
            }
        }

        for (int i = 0; i < saveSystem.MaxSlotCount; i++)
        {
            int slotIndex = i;
            var saveData = (i == 0) ? latestSave : saveSystem.LoadGame(i);
            bool isLatestSlot = (i == 0);

            // Slot card
            var slotElement = new VisualElement();
            // Slot 0: full width, others: half width (2 columns)
            if (isLatestSlot)
            {
                slotElement.style.width = new Length(100, LengthUnit.Percent);
                slotElement.style.height = 100;
            }
            else
            {
                slotElement.style.width = new Length(48, LengthUnit.Percent);
                slotElement.style.height = 85;
            }
            slotElement.style.marginBottom = 10;
            slotElement.style.flexDirection = FlexDirection.Row;
            slotElement.style.alignItems = Align.Center;
            slotElement.style.backgroundColor = SlotBg;
            slotElement.style.borderTopWidth = 1;
            slotElement.style.borderBottomWidth = 1;
            slotElement.style.borderLeftWidth = 1;
            slotElement.style.borderRightWidth = 1;
            slotElement.style.borderTopColor = GoldDim;
            slotElement.style.borderBottomColor = GoldDim;
            slotElement.style.borderLeftColor = GoldDim;
            slotElement.style.borderRightColor = GoldDim;
            slotElement.style.borderTopLeftRadius = 8;
            slotElement.style.borderTopRightRadius = 8;
            slotElement.style.borderBottomLeftRadius = 8;
            slotElement.style.borderBottomRightRadius = 8;
            slotElement.style.overflow = Overflow.Hidden;

            // Hover effect
            slotElement.RegisterCallback<PointerEnterEvent>(e =>
            {
                slotElement.style.backgroundColor = SlotBgHover;
                slotElement.style.borderTopColor = Gold;
                slotElement.style.borderBottomColor = Gold;
                slotElement.style.borderLeftColor = Gold;
                slotElement.style.borderRightColor = Gold;
            });
            slotElement.RegisterCallback<PointerLeaveEvent>(e =>
            {
                slotElement.style.backgroundColor = SlotBg;
                slotElement.style.borderTopColor = GoldDim;
                slotElement.style.borderBottomColor = GoldDim;
                slotElement.style.borderLeftColor = GoldDim;
                slotElement.style.borderRightColor = GoldDim;
            });

            // Left accent bar
            var accentBar = new VisualElement();
            accentBar.style.width = 5;
            accentBar.style.backgroundColor = saveData != null
                ? new Color(Gold.r, Gold.g, Gold.b, 0.7f)
                : new Color(1f, 1f, 1f, 0.15f);
            slotElement.Add(accentBar);

            // Info section — 50% left
            var infoContainer = new VisualElement();
            infoContainer.style.flexDirection = FlexDirection.Column;
            infoContainer.style.width = new Length(50, LengthUnit.Percent);
            infoContainer.style.paddingLeft = 15;
            infoContainer.style.paddingRight = 8;
            infoContainer.style.paddingTop = 12;
            infoContainer.style.paddingBottom = 12;
            infoContainer.style.justifyContent = Justify.Center;

            // Slot number badge
            var badgeRow = new VisualElement();
            badgeRow.style.flexDirection = FlexDirection.Row;
            badgeRow.style.alignItems = Align.Center;
            badgeRow.style.marginBottom = 6;

            var slotBadge = new Label(isLatestSlot ? "★" : (i).ToString());
            slotBadge.style.marginRight = 8;
            slotBadge.style.fontSize = 22;
            slotBadge.style.color = new Color(0.08f, 0.05f, 0.03f, 1f);
            slotBadge.style.unityFontStyleAndWeight = FontStyle.Bold;
            slotBadge.style.unityTextAlign = TextAnchor.MiddleCenter;
            slotBadge.style.unityFontDefinition = fontDef;
            slotBadge.style.width = 30;
            slotBadge.style.height = 28;
            slotBadge.style.backgroundColor = saveData != null
                ? new Color(Gold.r, Gold.g, Gold.b, 0.85f)
                : new Color(1f, 1f, 1f, 0.2f);
            slotBadge.style.borderTopLeftRadius = 4;
            slotBadge.style.borderTopRightRadius = 4;
            slotBadge.style.borderBottomLeftRadius = 4;
            slotBadge.style.borderBottomRightRadius = 4;
            badgeRow.Add(slotBadge);

            var slotLabel = new Label(isLatestSlot ? "最新存档" : "槽位 " + (i));
            slotLabel.style.fontSize = 20;
            slotLabel.style.color = saveData != null ? GoldBright : new Color(1f, 1f, 1f, 0.4f);
            slotLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            slotLabel.style.unityFontDefinition = fontDef;
            badgeRow.Add(slotLabel);

            infoContainer.Add(badgeRow);

            if (saveData != null)
            {
                var chapterText = saveData.scriptName;
                if (saveData.sceneIndex >= 0)
                    chapterText += "  \u2022  第" + (saveData.sceneIndex + 1) + "章  对话" + (saveData.dialogueIndex + 1);
                var infoLabel = new Label(chapterText);
                infoLabel.style.fontSize = 20;
                infoLabel.style.color = new Color(1f, 1f, 1f, 0.65f);
                infoLabel.style.unityFontDefinition = fontDef;
                infoLabel.style.marginBottom = 4;
                infoContainer.Add(infoLabel);

                var timeLabel = new Label(saveData.timestamp);
                timeLabel.style.fontSize = 17;
                timeLabel.style.color = new Color(1f, 1f, 1f, 0.35f);
                timeLabel.style.unityFontDefinition = fontDef;
                infoContainer.Add(timeLabel);
            }
            else
            {
                var emptyLabel = new Label("\u2014  空槽位  \u2014");
                emptyLabel.style.fontSize = 20;
                emptyLabel.style.color = new Color(1f, 1f, 1f, 0.3f);
                emptyLabel.style.unityFontDefinition = fontDef;
                infoContainer.Add(emptyLabel);
            }

            slotElement.Add(infoContainer);

            // Action buttons area — 50% right
            var btnArea = new VisualElement();
            btnArea.style.flexDirection = FlexDirection.Row;
            btnArea.style.alignItems = Align.Center;
            btnArea.style.justifyContent = Justify.Center;
            btnArea.style.width = new Length(50, LengthUnit.Percent);
            btnArea.style.paddingRight = 10;

            var btnText = isSaveMode ? "保存" : "读取";
            if (isSaveMode && saveData != null)
                btnText = "覆盖";

            var actionBtn = new UnityEngine.UIElements.Button(() =>
            {
                onSlotSelected?.Invoke(slotIndex);
                if (isSaveMode)
                {
                    RefreshSlots();
                    ShowSaveFeedback();
                }
                else
                {
                    ClosePanel();
                }
            })
            { text = btnText };

            actionBtn.style.width = 80;
            actionBtn.style.height = 36;
            actionBtn.style.fontSize = 18;
            actionBtn.style.color = new Color(1f, 1f, 1f, 0.9f);
            actionBtn.style.backgroundColor = BtnBg;
            actionBtn.style.unityTextAlign = TextAnchor.MiddleCenter;
            actionBtn.style.unityFontDefinition = fontDef;
            actionBtn.style.borderTopWidth = 1;
            actionBtn.style.borderBottomWidth = 1;
            actionBtn.style.borderLeftWidth = 1;
            actionBtn.style.borderRightWidth = 1;
            actionBtn.style.borderTopColor = GoldDim;
            actionBtn.style.borderBottomColor = GoldDim;
            actionBtn.style.borderLeftColor = GoldDim;
            actionBtn.style.borderRightColor = GoldDim;
            actionBtn.style.borderTopLeftRadius = 5;
            actionBtn.style.borderTopRightRadius = 5;
            actionBtn.style.borderBottomLeftRadius = 5;
            actionBtn.style.borderBottomRightRadius = 5;

            actionBtn.RegisterCallback<PointerEnterEvent>(e =>
            {
                actionBtn.style.backgroundColor = BtnBgHover;
                actionBtn.style.color = GoldBright;
                actionBtn.style.borderTopColor = Gold;
                actionBtn.style.borderBottomColor = Gold;
                actionBtn.style.borderLeftColor = Gold;
                actionBtn.style.borderRightColor = Gold;
            });
            actionBtn.RegisterCallback<PointerLeaveEvent>(e =>
            {
                actionBtn.style.backgroundColor = BtnBg;
                actionBtn.style.color = new Color(1f, 1f, 1f, 0.9f);
                actionBtn.style.borderTopColor = GoldDim;
                actionBtn.style.borderBottomColor = GoldDim;
                actionBtn.style.borderLeftColor = GoldDim;
                actionBtn.style.borderRightColor = GoldDim;
            });

            btnArea.Add(actionBtn);

            // Delete button (only when save data exists)
            if (saveData != null)
            {
                var delBtn = new UnityEngine.UIElements.Button(() =>
                {
                    ShowDeleteConfirm(slotIndex);
                })
                { text = "\u2715" };

                delBtn.style.width = 28;
                delBtn.style.height = 28;
                delBtn.style.fontSize = 14;
                delBtn.style.color = new Color(1f, 1f, 1f, 0.4f);
                delBtn.style.backgroundColor = Color.clear;
                delBtn.style.unityTextAlign = TextAnchor.MiddleCenter;
                delBtn.style.unityFontDefinition = fontDef;

                delBtn.RegisterCallback<PointerEnterEvent>(e =>
                {
                    delBtn.style.backgroundColor = DeleteBg;
                    delBtn.style.color = new Color(1f, 0.6f, 0.5f, 1f);
                });
                delBtn.RegisterCallback<PointerLeaveEvent>(e =>
                {
                    delBtn.style.backgroundColor = Color.clear;
                    delBtn.style.color = new Color(1f, 1f, 1f, 0.4f);
                });

                btnArea.Add(delBtn);
            }

            slotElement.Add(btnArea);
            slotContainer.Add(slotElement);
        }
    }

    private void ShowDeleteConfirm(int slotIndex)
    {
        var root = uiDoc.rootVisualElement;
        var overlay = new VisualElement();
        overlay.style.position = Position.Absolute;
        overlay.style.top = 0; overlay.style.left = 0; overlay.style.right = 0; overlay.style.bottom = 0;
        overlay.style.backgroundColor = new Color(0, 0, 0, 0.5f);
        overlay.style.alignItems = Align.Center;
        overlay.style.justifyContent = Justify.Center;

        var box = new VisualElement();
        box.style.backgroundColor = new Color(0.1f, 0.06f, 0.04f, 0.95f);
        box.style.borderTopWidth = 2; box.style.borderBottomWidth = 2;
        box.style.borderLeftWidth = 2; box.style.borderRightWidth = 2;
        box.style.borderTopColor = GoldDim; box.style.borderBottomColor = GoldDim;
        box.style.borderLeftColor = GoldDim; box.style.borderRightColor = GoldDim;
        box.style.borderTopLeftRadius = 8; box.style.borderTopRightRadius = 8;
        box.style.borderBottomLeftRadius = 8; box.style.borderBottomRightRadius = 8;
        box.style.paddingLeft = 40; box.style.paddingRight = 40;
        box.style.paddingTop = 30; box.style.paddingBottom = 30;
        box.style.alignItems = Align.Center;

        var msg = new Label("确认删除此存档？此操作不可撤销。");
        msg.style.fontSize = 22;
        msg.style.color = new Color(1f, 1f, 1f, 0.9f);
        msg.style.unityFontDefinition = new FontDefinition { font = gameFont };
        msg.style.marginBottom = 20;
        msg.style.whiteSpace = WhiteSpace.Normal;
        box.Add(msg);

        var btnRow = new VisualElement();
        btnRow.style.flexDirection = FlexDirection.Row;

        var confirmBtn = new UnityEngine.UIElements.Button(() =>
        {
            saveSystem.DeleteSave(slotIndex);
            RefreshSlots();
            root.Remove(overlay);
        }) { text = "确认删除" };
        confirmBtn.style.width = 120; confirmBtn.style.height = 40;
        confirmBtn.style.fontSize = 20; confirmBtn.style.marginRight = 10;
        confirmBtn.style.color = new Color(1f, 0.8f, 0.8f, 1f);
        confirmBtn.style.backgroundColor = new Color(0.4f, 0.12f, 0.1f, 0.7f);
        confirmBtn.style.unityTextAlign = TextAnchor.MiddleCenter;
        confirmBtn.style.unityFontDefinition = new FontDefinition { font = gameFont };
        btnRow.Add(confirmBtn);

        var cancelBtn = new UnityEngine.UIElements.Button(() => root.Remove(overlay)) { text = "取消" };
        cancelBtn.style.width = 120; cancelBtn.style.height = 40;
        cancelBtn.style.fontSize = 20; cancelBtn.style.marginLeft = 10;
        cancelBtn.style.color = new Color(1f, 1f, 1f, 0.8f);
        cancelBtn.style.backgroundColor = new Color(0.2f, 0.1f, 0.08f, 0.7f);
        cancelBtn.style.unityTextAlign = TextAnchor.MiddleCenter;
        cancelBtn.style.unityFontDefinition = new FontDefinition { font = gameFont };
        btnRow.Add(cancelBtn);

        box.Add(btnRow);
        overlay.Add(box);
        root.Add(overlay);
    }
}
