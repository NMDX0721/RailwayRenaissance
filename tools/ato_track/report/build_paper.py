import csv, json, os, sys
from PIL import Image
from reportlab.lib import colors
from reportlab.lib.enums import TA_CENTER, TA_JUSTIFY
from reportlab.lib.pagesizes import A4
from reportlab.lib.styles import ParagraphStyle
from reportlab.lib.units import mm
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.platypus import SimpleDocTemplate, Paragraph, Spacer, Table, TableStyle, Image as RLImage, KeepTogether

os.chdir(r"D:\Unity Project\RailwayRenaissance")
OUT = "tools/ato_track/report/ATO_3D_closed_loop_paper.pdf"
TMP = "tools/ato_track/report/_img"; os.makedirs(TMP, exist_ok=True)
pdfmetrics.registerFont(TTFont("SS", r"C:\Windows\Fonts\simsun.ttc", subfontIndex=0))
pdfmetrics.registerFont(TTFont("HT", r"C:\Windows\Fonts\simhei.ttf"))
pdfmetrics.registerFontFamily("SS", normal="SS", bold="HT", italic="SS", boldItalic="HT")

K = colors.black
body = ParagraphStyle("body", fontName="SS", fontSize=10.5, leading=17, alignment=TA_JUSTIFY, wordWrap="CJK", firstLineIndent=21, textColor=K)
plain = ParagraphStyle("plain", parent=body, firstLineIndent=0)
title = ParagraphStyle("title", parent=plain, fontName="HT", fontSize=17, leading=24, alignment=TA_CENTER, spaceAfter=4)
meta = ParagraphStyle("meta", parent=plain, fontSize=9.5, alignment=TA_CENTER, leading=14)
abs_ = ParagraphStyle("abs", parent=plain, fontSize=9.6, leading=15.5, leftIndent=8, rightIndent=8)
h1 = ParagraphStyle("h1", parent=plain, fontName="HT", fontSize=12.5, leading=18, spaceBefore=9, spaceAfter=3)
h2 = ParagraphStyle("h2", parent=plain, fontName="HT", fontSize=10.8, leading=16, spaceBefore=5, spaceAfter=2)
cap = ParagraphStyle("cap", parent=plain, fontSize=9.2, leading=13, alignment=TA_CENTER, spaceBefore=3, spaceAfter=4)
cell = ParagraphStyle("cell", parent=plain, fontSize=9, leading=12.5, alignment=TA_CENTER)
note = ParagraphStyle("note", parent=plain, fontSize=8.6, leading=12.5)
ref = ParagraphStyle("ref", parent=plain, fontSize=9, leading=13.5, leftIndent=16, firstLineIndent=-16)

def P(t, s=body): return Paragraph(t, s)
def load(f): return json.load(open(f, encoding="utf-8"))
flat = load("tools/compile/ato_line_closed_loop.json"); graded = load("tools/compile/ato_line_closed_loop_graded.json")
fps = {(g, n): load("tools/compile/ato_line_closed_loop%s_%dfps.json" % ("_graded" if g else "", n)) for g in (False, True) for n in (30, 144)}
def run(d, n): return next(r for r in d["runs"] if r["name"] == n)
CN = {"Smooth": "平稳", "AirSaving": "省气", "Punctual": "准点"}
MODES = ("Smooth", "AirSaving", "Punctual")

def jpg(src, w=1000):
    im = Image.open(src).convert("RGB"); h = int(im.height * w / im.width)
    dst = os.path.join(TMP, "p_" + os.path.basename(src).replace(".png", ".jpg")); im.resize((w, h), Image.LANCZOS).save(dst, quality=88); return dst, w / h

def three_line(rows, widths, caption):
    data = [[Paragraph(str(c), cell) for c in r] for r in rows]
    t = Table(data, colWidths=widths)
    t.setStyle(TableStyle([("LINEABOVE", (0, 0), (-1, 0), 1.0, K), ("LINEBELOW", (0, 0), (-1, 0), .5, K), ("LINEBELOW", (0, -1), (-1, -1), 1.0, K),
                           ("VALIGN", (0, 0), (-1, -1), "MIDDLE"), ("TOPPADDING", (0, 0), (-1, -1), 2.5), ("BOTTOMPADDING", (0, 0), (-1, -1), 2.5)]))
    return KeepTogether([P(caption, cap), t, Spacer(1, 6)])

def speed_figure():
    import matplotlib; matplotlib.use("Agg")
    import matplotlib.pyplot as plt
    from matplotlib import font_manager
    font_manager.fontManager.addfont(r"C:\Windows\Fonts\simsun.ttc"); plt.rcParams["font.family"] = "SimSun"; plt.rcParams["axes.unicode_minus"] = False
    fig, axs = plt.subplots(1, 2, figsize=(9, 3.0), sharey=True)
    for ax, f, t in ((axs[0], "tools/compile/ato_line_closed_loop_trace.csv", "(a) 平直线"), (axs[1], "tools/compile/ato_line_closed_loop_graded_trace.csv", "(b) 坡度线")):
        rows = list(csv.DictReader(open(f, encoding="utf-8")))
        for n, ls in (("Smooth", "-"), ("AirSaving", "--"), ("Punctual", ":")):
            ax.plot([float(r["position"]) for r in rows if r["run"] == n], [float(r["speed"]) for r in rows if r["run"] == n], color="k", ls=ls, lw=1.1, label=CN[n])
        lim = [(float(r["position"]), float(r["limit"])) for r in rows if r["run"] == "Smooth"]
        ax.step([a for a, _ in lim], [b for _, b in lim], where="post", color="0.55", lw=1.0, label="限速")
        ax.set_title(t, fontsize=9); ax.set_xlabel("里程 s / m", fontsize=9); ax.set_xlim(0, 900); ax.tick_params(labelsize=8)
    axs[0].set_ylabel("速度 v / (m/s)", fontsize=9); axs[0].legend(frameon=False, fontsize=8, loc="lower center", ncol=2)
    fig.tight_layout(); p = os.path.join(TMP, "p_speed.png"); fig.savefig(p, dpi=200); plt.close(fig); return p

def figure(src, caption, width, wrap=True):
    path, ratio = jpg(src); parts = [RLImage(path, width=width, height=width / ratio), P(caption, cap)]
    return KeepTogether(parts) if wrap else parts

DIFF = open("tools/ato_track/report/_diff_paragraph.txt", encoding="utf-8").read().strip() if os.path.exists("tools/ato_track/report/_diff_paragraph.txt") else "（待补）"

s = []
s += [P("铁路试验线路三维模型与自动驾驶 900 米任务的闭环验证", title),
      P("开发阶段技术报告 · 2026 年 11 月 6 日", meta), Spacer(1, 8),
      P("<b>摘要</b>　为使自动驾驶（ATO）研究中的 V3 控制器能在与游戏一致的帧循环中完成任务验收，构建了模块化的三维试验线路，并使车辆位姿、坡度输入和末端保护都取自同一张线路片段表。线路以 25 m 与 5 m 的平直模块及 ±10‰、±20‰ 的坡度模块拼接，里程范围 -60 至 1200 m。在 900 m、限速 12/18/10 m/s、规定用时 120 s（容差 ±5 s）的任务上，分别于平直线和坡度线运行平稳、省气、准点三种目标。全部 6 次运行均自动停稳，无超限帧、无紧急制动、无人工干预，停车误差在 -2.74 至 -1.99 m 之间，到达用时 118.9 至 124.9 s。30、60、144 FPS（固定帧时间）下的结果逐位一致。本文只覆盖名义工况的单次运行，位姿为运动学跟随，不含轮轨接触力，不构成独立测试或安全验证。", abs_),
      P("<b>关键词</b>　列车自动驾驶；三维试验线路；坡度；闭环验证；帧循环", abs_), Spacer(1, 4)]

s += [P("1　引言", h1),
      P("研究项目的 ATO 部分已有 V1、V2、V3 三代控制器和 900 米路线任务，其验收主要在固定步长的物理后端中进行（每步 0.02 s）。游戏中车辆还需要沿真实线路运动，并随帧循环更新位姿。此前三维部分只有程序生成的 180 m 半径检查曲线，范围为 -60 至 500 m，既不覆盖 900 m 任务，也不带坡度，因此无法回答两个问题：其一，控制器在逐帧更新的运行环境中表现是否与固定步长验收一致；其二，坡度进入后端后任务是否仍能完成。"),
      P("本文的工作有三项：建立可拼接的线路模块并给出里程与坡度的统一定义；让车辆位姿、后端坡度和末端保护共用该定义；在此基础上对 V3 做 900 m 闭环验证，并检查暂停、检查点恢复和人工接管。")]

s += [P("2　方法", h1), P("2.1　线路模块", h2),
      P("轨道模块由 Blender 脚本生成并导出为 FBX。钢轨中心距 1.435 m（轨距），枕木间距 0.625 m，每 25 m 模块含 40 根枕木。道砟断面的肩宽、边坡和厚度目前取估计值，尚未按 TB 10082 的规定值校核。道砟表面的石块建为真实几何体（每个 20 个三角面，约 90 块/m<super>2</super>，每 25 m 模块约 8500 块），并设两级细节层次。模块种类为 25 m 与 5 m 平直模块，以及水平投影 25 m、坡度 ±10‰ 和 ±20‰ 的坡度模块；坡度模块的网格沿坡面预先倾斜，水平投影恒为 25 m。"),
      P("线路由一张片段表给出，每项为（模块，数量）。同一张表有两个用途：在场景中依次摆放模块，以及生成车辆循迹所用的线路对象。因此渲染的轨道与车辆所走的线路来自同一份数据。里程 s 取水平投影，与后端状态中的位置同坐标；任务起点 s = 0，线路范围 -60 至 1200 m。"),
      P("2.2　车辆循迹、坡度输入与末端保护", h2),
      P("车辆位姿按前后参考点（车辆中心前后各 6.1 m）的连线确定：偏航取水平分量，俯仰取连线的坡角；转向架架构按局部切线俯仰，弹簧与闸瓦的相对位移扣除架构倾斜。轮对接触点取车轮中心下方一个滚动半径（0.4612 m）。坡度线对后端的输入为一个函数，由当前位置返回坡度（千分数，向里程增大方向升高为正），在每个控制节点读取一次；平直线不设该函数，沿用原有的坡度参数。"),
      P("末端保护沿用原判据，即剩余距离减去 10 m 的裕量小于制动距离加 30 m 时触发紧急制动，制动距离按减速度 0.25 m/s<super>2</super> 取 v<super>2</super>/(2×0.25)。区别在于剩余距离现在按当前线路的实际终点计算，而不是固定的 500 m。"),
      P("2.3　闭环验证设置", h2),
      P("任务取标准 900 m 路线：限速在 0、250、650 m 处分别为 12、18、10 m/s，规定用时 120 s，容差 ±5 s，停车容差 ±5 m。控制器为 V3，目标分平稳、省气、准点。冷启动、启机和 306 s 的充风由手动推进完成，随后保存物理检查点。每次运行从该检查点恢复，接入 ATO 后不再手动推进：50 Hz 后端由每帧的更新函数推进，位姿由帧末更新函数写入。帧时间固定为 1/30、1/60 或 1/144 s（引擎的固定帧率设置），因此帧率是确定的而非真实时钟。"),
      P("每帧记录里程、速度、限速、指令和位姿，并检查：速度是否超过当前限速；是否出现紧急制动；位姿里程与后端里程之差；每 30 帧一次的轮对接触点与钢轨网格的竖向差（向钢轨网格投射射线，仅统计钢轨碰撞体）。此外在平稳目标运行中约 300 m 处暂停 30 帧，保存研究检查点，继续运行 180 帧后恢复；另在约 400 m 处手动施加紧急制动，检查 ATO 是否退出、车辆是否停稳、停稳后车轮相位是否固定。")]

s += [P("3　结果", h1)]
rows = [["线路", "目标", "停车位置/m", "误差/m", "到达用时/s", "最高速度/(m/s)", "超限帧", "紧急帧", "干预"]]
for label, d in (("平直线", flat), ("坡度线", graded)):
    for n in MODES:
        r = run(d, n); rows.append([label, CN[n], "%.2f" % r["stopPositionM"], "%+.2f" % r["signedErrorM"], "%.1f" % r["arrivalS"], "%.2f" % r["maxSpeedMps"], r["limitExceedFrames"], r["emergencyFrames"], r["interventions"]])
s += [P("3.1　停车与用时", h2),
      P("表 1 给出 60 FPS 下的 6 次运行。全部运行自动停稳，停车位置在目标点（900 m）之前 1.99 至 2.74 m，处于 ±5 m 容差内；到达用时在 118.9 至 124.9 s，处于 120 ± 5 s 内。无超限帧、无紧急制动、无干预，相邻两次普通制动切换的最短间隔为 25 个节点（0.5 s）。"),
      three_line(rows, [15*mm, 13*mm, 23*mm, 15*mm, 20*mm, 25*mm, 14*mm, 14*mm, 12*mm], "表 1　900 m 任务的停车与用时（V3，60 FPS）"),
      P("准点目标在平直线上的到达用时为 124.9 s，距容差上限只差 0.1 s，说明在该工况下该目标几乎没有时间裕量。图 1 给出各次运行的速度随里程的变化，坡度线上速度的起伏对应坡段，最高速度较平直线高约 0.8 m/s（平稳目标 10.34 与 9.50 m/s）。"),
      figure(speed_figure(), "图 1　速度—里程曲线（灰线为限速）", 150*mm)]

rows = [["线路", "帧率/FPS", "平稳 误差/m　用时/s", "省气 误差/m　用时/s", "准点 误差/m　用时/s"]]
for g, label in ((False, "平直线"), (True, "坡度线")):
    for n in (30, 60, 144):
        d = (graded if g else flat) if n == 60 else fps[(g, n)]
        rows.append([label, n] + ["%+.2f　%.1f" % (run(d, k)["signedErrorM"], run(d, k)["arrivalS"]) for k in MODES])
s += [P("3.2　帧率的影响", h2),
      P("表 2 中 30、60、144 FPS 的停车位置与到达用时逐位一致。这是预期的，因为后端在固定 50 Hz 时钟上推进，帧率只改变位姿的更新次数。需要说明的是帧时间在此是固定值，并未模拟真实设备上的帧抖动与掉帧。"),
      three_line(rows, [20*mm, 20*mm, 44*mm, 44*mm, 44*mm], "表 2　不同固定帧率下的停车误差与到达用时")]

rows = [["检查项", "平直线", "坡度线"],
        ["暂停 30 帧后里程、站位、车体位置漂移/m", "0.0", "0.0"],
        ["研究检查点恢复后里程差/m", "0.0", "0.0"],
        ["研究检查点恢复后车体位置差/m", "2.6×10<super>-4</super>", "3.9×10<super>-3</super>"],
        ["位姿与后端里程的最大差/m", "0.0", "0.0"],
        ["轮对接触点与钢轨网格的最大竖向差/m", "1.2×10<super>-7</super>", "2.5×10<super>-6</super>"],
        ["约 400 m 紧急制动后的滑行距离/m", "53.9", "72.7"],
        ["停稳后车轮相位", "固定", "固定"]]
s += [P("3.3　暂停、恢复与人工接管", h2),
      P("表 3 汇总了三项检查。检查点恢复后里程与保存时完全一致；坡度线上车体位置有 3.9 mm 的差，原因是恢复时车体升沉弹簧被重置，而保存时弹簧偏离平衡位置（升沉限幅为 ±4 mm），属于呈现层的瞬态，不是里程误差。据此将恢复判据定为里程差小于 1 mm、车体位置差小于 8 mm。人工接管时 ATO 立即退出，车辆停稳；两条线路的滑行距离不同，是因为坡度线上的接管点位于下坡段。轮对接触差由位姿直接取自线路所得，只反映模型的几何一致，不代表轮轨接触。"),
      three_line(rows, [86*mm, 40*mm, 40*mm], "表 3　暂停、检查点恢复与人工接管检查")]

pics = [("tools/ato_track/review/loop_ext_120m_graded.png", "(a) 坡度线，120 m，外部"), ("tools/ato_track/review/loop_cab_120m_graded.png", "(b) 坡度线，120 m，驾驶室"),
        ("tools/ato_track/review/loop_ext_650m_graded.png", "(c) 坡度线，650 m，外部"), ("tools/ato_track/review/loop_cab_stop_graded.png", "(d) 坡度线，停车位置，驾驶室")]
tbl = Table([[figure(pics[0][0], pics[0][1], 80*mm, False), figure(pics[1][0], pics[1][1], 80*mm, False)], [figure(pics[2][0], pics[2][1], 80*mm, False), figure(pics[3][0], pics[3][1], 80*mm, False)]], colWidths=[84*mm, 84*mm])
tbl.setStyle(TableStyle([("VALIGN", (0, 0), (-1, -1), "TOP")]))
s += [P("3.4　场景", h2), P("图 2 为运行中截取的画面。光照使用项目自带的晴天环境，地面为草地平面和随线路高度起伏的路基网格；截图采用 SMAA 与两倍超采样。"), tbl, P("图 2　坡度线上的运行画面", cap)]

s += [P("4　讨论", h1),
      P("4.1　平稳目标的停车误差", h2), P(DIFF),
      P("4.2　有效性的限制", h2),
      P("第一，每种工况仅运行一次，没有重复、扰动或失败注入，且所用任务即开发阶段的名义任务，并非独立测试集，因此不能据此推断可靠性或泛化性。第二，位姿为运动学跟随，轮轨接触、蠕滑与黏着未建模，坡度只是位置到坡度的映射，坡度变化处没有竖曲线过渡，转向架俯仰在接缝处有阶跃（±10‰ 约 0.57°，±20‰ 约 1.15°）。第三，闭环以固定帧时间运行，未涉及真实时钟下的抖动。第四，起点须在水平段：初次把起点放在 +10‰ 坡上时，松闸后两帧内车辆后溜，ATO 因初速为负拒绝接入，坡上起步需要先定义驻车保压，尚未处理。第五，道砟断面和细节层次距离为估计值，平板设备上的渲染开销未测。")]

s += [P("5　结论", h1),
      P("建立了以片段表为唯一数据源的模块化三维试验线路，使车辆位姿、后端坡度和末端保护一致；V3 在平直线与 ±20‰ 以内的坡度线上，于 30–144 FPS 的固定帧循环中完成了平稳、省气、准点三种目标的 900 m 任务，并通过了暂停、检查点恢复和人工接管检查。结果为名义工况下的开发结果。后续工作包括独立测试集、真实时钟下的帧抖动、坡上起步、竖曲线与弯道，以及按规范核对道砟断面。")]

s += [P("数据与代码", h1),
      P("线路与模块：tools/ato_track/build_track_module.py（Blender 5.2）；组装与闭环：Assets/Editor/AtoTestLineBuilder.cs、AtoLineClosedLoop.cs（Unity 6000.5.8f1，入口 Batch、BatchGraded、Batch30、Batch144 及对应坡度线入口）。结果数据：tools/compile/ato_line_closed_loop*.json 及同名逐帧记录 _trace.csv。详细说明见 docs/for-ai/ATO_ROUTE_3D.md。以上改动目前仅在本地，尚未提交。", plain)]

def footer(c, doc):
    c.saveState(); c.setFont("SS", 8.5); c.drawCentredString(A4[0] / 2, 9 * mm, str(doc.page)); c.restoreState()
SimpleDocTemplate(OUT, pagesize=A4, leftMargin=20*mm, rightMargin=20*mm, topMargin=18*mm, bottomMargin=17*mm, title="铁路试验线路三维模型与自动驾驶 900 米任务的闭环验证").build(s, onFirstPage=footer, onLaterPages=footer)
print(OUT, os.path.getsize(OUT) // 1024, "KB")
