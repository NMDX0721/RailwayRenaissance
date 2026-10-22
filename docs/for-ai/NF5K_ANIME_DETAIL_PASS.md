# NF-5K 动漫风细节轮次

2026-10-22，用户确认视觉方向：VN角色与背景均为像素，3D偏动漫，内容与机械逻辑硬核。共用车辆结构、涂装、色彩与场景身份，不用照片级随机脏污替代细节。

本轮`NF5KAnimeDetailPass.Apply`补充两侧连续折边导水沿、落水端和四门上方滴水沿；四扇外门改为带密封垫、底座、支脚、圆杆握持部、锁芯与紧固件的五金候选。握持部与底座间留约33mm几何空隙，未做人体工学认证。新五金附属各自CabSideDoor，旧方块外把手仅隐藏Renderer，旧交互/门碰撞逻辑保留；锁芯是静态细节，没有新增钥匙交互。

新增外观件不带Collider，因此既有44角度门扇扫掠仍只验证原门体/座椅等体积，不代表新五金完整扫掠认证。另检查四组握持件在半开门状态的位移并恢复关门，确保没有留在车壁上。连续导水沿按当前车体候选设计，没有宣称还原某一原车结构。

材质保留NF-5K原配色，清理主车漆和车顶材质的细碎底色/法线纹理，降低钢轮锈点与高频颗粒强度；保留踏面磨亮、辐板薄氧化和独立金属五金。仍使用URP/Lit柔和光照，没有启用全局卡通描边或阶梯阴影。

调用接在CabInteriorBlockoutSetup的IndustrialFinish之后，便于持续生成；独立BatchBuild同步CabInteriorBlockout、CabDaylightReview、CabSimulationReview及CabRailMotionReview，不重建驾驶台或走行部运动绑定。

出图另加正侧检查光，浅灰背景，不保存到车辆灯光。图片`tools/cab_reference/interior_review/anime_train_detail_bright.png`、`anime_train_door_bright.png`。验证日志`tools/compile/cab_anime_detail_bright.log`：四门五金跟随与门扇扫掠、运动后端接线、上下文交互、驾驶Play Mode；未验证完整外廓限界、雨水模拟、运行中梯子收纳和移动端性能。
