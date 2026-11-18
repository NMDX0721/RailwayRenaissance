import csv, io, json, os
from PIL import Image
from reportlab.lib import colors
from reportlab.lib.pagesizes import A4
from reportlab.lib.styles import ParagraphStyle
from reportlab.lib.units import mm
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.platypus import SimpleDocTemplate, Paragraph, Spacer, Table, TableStyle, Image as RLImage, PageBreak, KeepTogether

ROOT = r"D:\Unity Project\RailwayRenaissance"
os.chdir(ROOT)
OUT = "tools/ato_track/report/ATO_3D_closed_loop_report.pdf"
TMP = "tools/ato_track/report/_img"
os.makedirs(TMP, exist_ok=True)

pdfmetrics.registerFont(TTFont("YH", r"C:\Windows\Fonts\msyh.ttc", subfontIndex=0))
pdfmetrics.registerFont(TTFont("YHB", r"C:\Windows\Fonts\msyhbd.ttc", subfontIndex=0))
pdfmetrics.registerFontFamily("YH", normal="YH", bold="YHB", italic="YH", boldItalic="YHB")

ink = colors.HexColor("#1f2933"); sub = colors.HexColor("#5b6770"); accent = colors.HexColor("#1f5f8b"); soft = colors.HexColor("#eef3f7")
base = ParagraphStyle("b", fontName="YH", fontSize=9.6, leading=15, textColor=ink, wordWrap="CJK")
h1 = ParagraphStyle("h1", parent=base, fontName="YHB", fontSize=20, leading=26, spaceAfter=4)
h2 = ParagraphStyle("h2", parent=base, fontName="YHB", fontSize=13, leading=18, textColor=accent, spaceBefore=10, spaceAfter=4)
small = ParagraphStyle("s", parent=base, fontSize=8.2, leading=12, textColor=sub)
cell = ParagraphStyle("c", parent=base, fontSize=8.6, leading=12)
cellh = ParagraphStyle("ch", parent=cell, fontName="YHB", textColor=colors.white)
bul = ParagraphStyle("bul", parent=base, leftIndent=10, bulletIndent=0, spaceAfter=2)

def P(t, s=base): return Paragraph(t, s)
def B(t): return Paragraph(t, bul, bulletText="•")

def load(f): return json.load(open(f, encoding="utf-8"))
flat = load("tools/compile/ato_line_closed_loop.json")
graded = load("tools/compile/ato_line_closed_loop_graded.json")
fps = {(g, n): load("tools/compile/ato_line_closed_loop%s_%dfps.json" % ("_graded" if g else "", n)) for g in (False, True) for n in (30, 144)}

def run(d, name): return next(r for r in d["runs"] if r["name"] == name)
CN = {"Smooth": "平稳", "AirSaving": "省气", "Punctual": "准点"}

def jpg(src, w=1100):
    im = Image.open(src).convert("RGB"); h = int(im.height * w / im.width)
    dst = os.path.join(TMP, os.path.basename(src).replace(".png", ".jpg")); im.resize((w, h), Image.LANCZOS).save(dst, quality=88); return dst, w / h

def figure(src, caption, width=84 * mm):
    path, ratio = jpg(src); return [RLImage(path, width=width, height=width / ratio), P(caption, small)]

def table(rows, widths, head=True):
    data = [[P(c, cellh if (head and i == 0) else cell) if isinstance(c, str) else c for c in r] for i, r in enumerate(rows)]
    t = Table(data, colWidths=widths, repeatRows=1)
    st = [("GRID", (0, 0), (-1, -1), .4, colors.HexColor("#c9d3db")), ("VALIGN", (0, 0), (-1, -1), "MIDDLE"),
          ("TOPPADDING", (0, 0), (-1, -1), 3), ("BOTTOMPADDING", (0, 0), (-1, -1), 3)]
    if head: st.append(("BACKGROUND", (0, 0), (-1, 0), accent))
    for i in range(1, len(rows)):
        if i % 2 == 0: st.append(("BACKGROUND", (0, i), (-1, i), soft))
    t.setStyle(TableStyle(st)); return t

# ---- speed/position chart from the traces
def chart():
    import matplotlib; matplotlib.use("Agg")
    import matplotlib.pyplot as plt
    from matplotlib import font_manager
    font_manager.fontManager.addfont(r"C:\Windows\Fonts\msyh.ttc"); plt.rcParams["font.family"] = "Microsoft YaHei"
    fig, axs = plt.subplots(1, 2, figsize=(10, 3.4), sharey=True)
    for ax, f, title in ((axs[0], "tools/compile/ato_line_closed_loop_trace.csv", "平直线"), (axs[1], "tools/compile/ato_line_closed_loop_graded_trace.csv", "坡度线")):
        rows = list(csv.DictReader(open(f, encoding="utf-8")))
        for name, col in (("Smooth", "#1f5f8b"), ("AirSaving", "#2f9e6f"), ("Punctual", "#d9822b")):
            xs = [float(r["position"]) for r in rows if r["run"] == name]; ys = [float(r["speed"]) for r in rows if r["run"] == name]
            ax.plot(xs, ys, color=col, lw=1.4, label=CN[name])
        lim = [(float(r["position"]), float(r["limit"])) for r in rows if r["run"] == "Smooth"]
        ax.step([a for a, _ in lim], [b for _, b in lim], where="post", color="#9aa5ae", lw=1.2, ls="--", label="限速")
        ax.set_title(title); ax.set_xlabel("里程 / m"); ax.grid(alpha=.25); ax.set_xlim(0, 900)
    axs[0].set_ylabel("速度 / (m/s)"); axs[0].legend(frameon=False, fontsize=8)
    fig.tight_layout(); p = os.path.join(TMP, "speed.png"); fig.savefig(p, dpi=160); plt.close(fig); return p

story = []
story += [P("ATO 3D 试验线路与 900 米闭环", h1),
          P("铁路复兴 · 自动驾驶研究 阶段成果（2026-11-06）", small), Spacer(1, 6),
          P("<b>一句话结论：</b>V3 控制器在新做的 3D 试验线路上，用正常帧循环跑完了 900 米任务。三种目标（平稳 / 省气 / 准点）在平直线和带坡度的线路上都自动停稳，没有超速、紧急制动或人工干预；30、60、144 FPS 的结果完全一致。这是名义工况下的开发结果，不是独立测试，也不是安全验证。"), Spacer(1, 6)]

story += [P("1. 做了什么", h2),
          B("<b>轨道模块套件：</b>用 Blender 脚本生成 25 米和 5 米平直模块，以及 ±10‰、±20‰ 的 25 米坡度模块。轨距 1.435 米，枕距 0.625 米；道砟表面的石块是真实几何（每 25 米约 8500 块），带两级 LOD。模块只需按片段表拼接。"),
          B("<b>车辆循迹：</b>同一张片段表既用来摆放轨道，也用来生成车辆跟随的线路，所以看到的轨道和车辆走的线是同一份数据。坡度线上车体按前后参考点连线俯仰，转向架随局部坡度俯仰；坡度同时送入物理后端。"),
          B("<b>末端保护：</b>原来固定按 500 米旧检查线判断，现在按当前线路的起止判断，合规运行不触发。"),
          B("<b>900 米闭环：</b>V3 通过真实驾驶室指令驱动 50 Hz 后端，位姿每帧跟随后端。只有开机和充风（306 秒）是手动推进，任务开始后全部走正常帧循环。"),
          B("<b>场景：</b>使用项目自己的晴天环境，新建草地与随线路起伏的路基；截图采用 SMAA 加 2 倍超采样。")]

story += [P("2. 线路", h2),
          P("两条线路里程都是 -60 至 1200 米，里程按水平投影计。平直线全程水平。坡度线起点站前 65 米水平，随后是 ±10‰ 与 ±20‰ 的坡段，净高差为零，900 米停车点落在坡段上。起点必须水平：第一次把起点放在 +10‰ 坡上，松闸两帧内车辆后溜，ATO 因初速为负拒绝接入；坡上起步需要先定义驻车保压，尚未做。"),
          Spacer(1, 4), table([["任务", "内容"], ["路线", "900 米，限速 12 / 18 / 10 m/s 分别从 0 / 250 / 650 米起"], ["时间", "120 秒，容差 ±5 秒"], ["控制器", "V3，三种目标：平稳、省气、准点"], ["帧循环", "固定帧时间 30 / 60 / 144 FPS，后端固定 50 Hz"]], [32 * mm, 140 * mm])]

# results table
rows = [["线路", "目标", "停车位置 / 米", "误差 / 米", "到达 / 秒", "最高速度 m/s", "超限帧", "紧急帧", "干预"]]
for label, d in (("平直线", flat), ("坡度线", graded)):
    for n in ("Smooth", "AirSaving", "Punctual"):
        r = run(d, n); rows.append([label, CN[n], "%.2f" % r["stopPositionM"], "%+.2f" % r["signedErrorM"], "%.1f" % r["arrivalS"], "%.2f" % r["maxSpeedMps"], str(r["limitExceedFrames"]), str(r["emergencyFrames"]), str(r["interventions"])])
story += [P("3. 结果（60 FPS）", h2), table(rows, [18 * mm, 15 * mm, 26 * mm, 20 * mm, 18 * mm, 26 * mm, 16 * mm, 16 * mm, 14 * mm]), Spacer(1, 4),
          B("三趟任务全部自动停稳，没有超限、紧急制动和人工干预；两次制动切换之间最短 25 个 Tick。"),
          B("位姿与后端里程每帧差 0.0 米；轮对接触点与钢轨网格的竖向差：平直线约 1.2e-7 米，坡度线约 2.5e-6 米。这是位姿直接取自线路得到的运动学检查，<b>不是</b>轮轨接触力。"),
          B("<b>准点目标到达 124.9 秒（平直线），离 ±5 秒边界只差 0.1 秒</b>，不能当作裕量充足。"),
          Spacer(1, 4), RLImage(chart(), width=172 * mm, height=172 * mm * 3.4 / 10)]
story += [P("速度曲线来自逐帧记录；虚线是路线限速。", small)]

rows = [["线路", "帧率", "平稳 误差 / 到达", "省气 误差 / 到达", "准点 误差 / 到达"]]
for g, label in ((False, "平直线"), (True, "坡度线")):
    for n in (30, 60, 144):
        d = (graded if g else flat) if n == 60 else fps[(g, n)]
        rows.append([label, "%d FPS" % n] + ["%+.2f 米 / %.1f 秒" % (run(d, k)["signedErrorM"], run(d, k)["arrivalS"]) for k in ("Smooth", "AirSaving", "Punctual")])
story += [P("4. 帧率无关", h2), table(rows, [22 * mm, 22 * mm, 42 * mm, 42 * mm, 42 * mm]), Spacer(1, 3),
          P("30、60、144 FPS 的停车位置和到达时间逐位一致，因为后端按固定 50 Hz 时钟推进，帧率只影响呈现。注意这是<b>固定帧时间</b>的帧循环（Time.captureFramerate），不是真实时钟，也没有测帧率抖动或掉帧。")]

story += [P("5. 暂停、恢复与人工接管", h2),
          table([["项目", "平直线", "坡度线"],
                 ["约 300 米暂停 30 帧后的漂移", "0.0 米", "0.0 米"],
                 ["研究检查点恢复：里程差", "0.0 米", "0.0 米"],
                 ["研究检查点恢复：车体位置差", "2.6e-4 米", "3.9e-3 米"],
                 ["约 400 米手动紧急制动后", "ATO 退出，滑行 53.9 米停稳", "ATO 退出，滑行 72.7 米停稳"],
                 ["停稳后车轮相位", "固定", "固定"]], [66 * mm, 53 * mm, 53 * mm]), Spacer(1, 3),
          P("坡度线上恢复后车体位置差 3.9 毫米，是因为恢复时升沉弹簧被重置，属于呈现层瞬态而不是里程误差。判据拆成：里程差小于 1 毫米，车体位置差小于 8 毫米（升沉弹簧限幅 ±4 毫米的两倍）。接管滑行距离不同是因为坡度线上的接管点落在下坡段。")]

story += [PageBreak(), P("6. 画面", h2),
          Table([[figure("tools/ato_track/review/loop_ext_120m.png", "平直线，120 米处，外部视角"), figure("tools/ato_track/review/loop_cab_120m.png", "平直线，120 米处，驾驶室视角")],
                 [figure("tools/ato_track/review/loop_ext_650m_graded.png", "坡度线，650 米处，外部视角"), figure("tools/ato_track/review/loop_cab_120m_graded.png", "坡度线，120 米处，驾驶室视角")],
                 [figure("tools/ato_track/review/loop_ext_stop_graded.png", "坡度线，停车位置，外部视角"), figure("tools/ato_track/review/loop_cab_stop_graded.png", "坡度线，停车位置，驾驶室视角")]],
                colWidths=[86 * mm, 86 * mm], style=TableStyle([("VALIGN", (0, 0), (-1, -1), "TOP"), ("BOTTOMPADDING", (0, 0), (-1, -1), 6)]))]

story += [P("7. 局限与未解释的地方", h2),
          B("<b>不是独立测试：</b>每种工况只跑了一遍，没有失败注入，不能写成可靠性或安全结论。"),
          B("<b>平稳目标误差差异未解释：</b>平直线 -2.74 米，坡度线 -2.20 米，交接文档中的开发案例约 -2.02 米。我专门对照过：手动 50 Hz 步进、先切到准点再切回的模式切换、改目标设置，三种变体的结果都是同样的 -2.745 米，所以差异不来自帧循环或这些操作；剩下的可能是两边的场景或起始状态不同，尚未查证。因此本批结果要单独记录，不与既有 54 次回归混用。"),
          B("<b>位姿是运动学跟随：</b>没有轮轨接触力、蠕滑和黏着，坡度只是位置到坡度的映射，坡度变化处没有竖曲线过渡，转向架俯仰在接缝处有阶跃（±10‰ 约 0.57°，±20‰ 约 1.15°）。"),
          B("<b>道砟断面尺寸是估计值：</b>待按 TB 10082 的数值替换；LOD 距离也是估计，平板上的性能没有测。"),
          B("<b>尚未做：</b>弯道、竖曲线、路基与桥梁、道岔、站台、信号机；坡上起步的驻车保压；独立测试集。")]

story += [P("8. 如何复现", h2),
          P("在 Unity 6000.5.8f1 中，批处理入口：AtoLineClosedLoop.Batch（平直线 60 FPS）、BatchGraded（坡度线）、Batch30 / Batch144 / BatchGraded30 / BatchGraded144（其他帧率）、BatchDiag（误差对照）。轨道模块由 tools/ato_track/build_track_module.py（Blender 5.2）生成。证据文件在 tools/compile/ato_line_closed_loop*.json 与同名 _trace.csv，详细说明见 docs/for-ai/ATO_ROUTE_3D.md。改动目前在本地，尚未提交。")]

def footer(c, doc):
    c.saveState(); c.setFont("YH", 8); c.setFillColor(sub); c.drawString(18 * mm, 10 * mm, "ATO 3D 试验线路与 900 米闭环 · 2026-11-06 · 开发阶段成果"); c.drawRightString(A4[0] - 18 * mm, 10 * mm, str(doc.page)); c.restoreState()

SimpleDocTemplate(OUT, pagesize=A4, leftMargin=18 * mm, rightMargin=18 * mm, topMargin=16 * mm, bottomMargin=16 * mm, title="ATO 3D 试验线路与 900 米闭环", author="RailwayRenaissance").build(story, onFirstPage=footer, onLaterPages=footer)
print(OUT, os.path.getsize(OUT) // 1024, "KB")
