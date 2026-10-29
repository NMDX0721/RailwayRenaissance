"""Generate contest figures directly from archived, non-duplicated experiment records."""
import csv,json,pathlib
import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt
from matplotlib import font_manager

ROOT=pathlib.Path(__file__).resolve().parents[3]
OUT=ROOT/'docs/research/figures'
OUT.mkdir(exist_ok=True)
font=pathlib.Path('C:/Windows/Fonts/msyh.ttc')
if font.exists():
    font_manager.fontManager.addfont(str(font));plt.rcParams['font.family']=font_manager.FontProperties(fname=str(font)).get_name()
plt.rcParams['axes.unicode_minus']=False

fig,ax=plt.subplots(figsize=(8,4.6),layout='constrained')
for seed,color in [(63001,'#235789'),(63003,'#dc7c39')]:
    path=ROOT/f'research/ato-v3/followup/{seed}-weak_wet_v2-AirSaving-v3-r0-trace.csv'
    with path.open(encoding='utf-8',newline='') as f:rows=list(csv.DictReader(f))
    ax.plot([float(r['elapsed']) for r in rows],[float(r['position']) for r in rows],color=color,label=f'种子 {seed}')
ax.axhspan(895,905,color='#bedbcc',alpha=.5,label='停车窗 ±5 m');ax.axvspan(115,125,color='#eadaba',alpha=.55,label='到达窗口 115–125 s')
ax.set(xlabel='时间（秒）',ylabel='距起点位置（米）',title='旧测试：两次提前到达的物理回放');ax.legend(loc='lower right');ax.grid(alpha=.25)
fig.savefig(OUT/'early_arrival_replay.png',dpi=180);plt.close(fig)

run=ROOT/'research/ato-v3/followup/runs/20261028T233032799050Z'
variants=[('baseline','原 V3'),('fixed_coast','固定惰行估计'),('fixed_brake','固定制动估计')]
scenarios=[('nominal','名义'),('weak_wet','组合'),('weak_brake','弱制动'),('strong_brake','强制动'),('downhill','下坡'),('uphill','上坡'),('low_grip','低黏着')]
fig,ax=plt.subplots(figsize=(9,4.8),layout='constrained');width=.23
for j,(variant,label) in enumerate(variants):
    rows=json.loads((run/(variant+'-results.json')).read_text(encoding='utf-8'))
    values=[sum(r['success'] for r in rows if r['repeat']==0 and r['scenario']==scenario) for scenario,_ in scenarios]
    bars=ax.bar([i+(j-1)*width for i in range(len(scenarios))],values,width,label=label)
    ax.bar_label(bars,fontsize=8)
ax.set_xticks(range(len(scenarios)),[label for _,label in scenarios]);ax.set_ylim(0,6.9);ax.set_ylabel('成功数（每工况共 6 个种子-模式组合）');ax.set_title('已查看工况上的单项消融（探索性）');ax.legend(ncol=3);ax.grid(axis='y',alpha=.25)
fig.savefig(OUT/'component_ablation.png',dpi=180);plt.close(fig)
print('Wrote figures to',OUT)
