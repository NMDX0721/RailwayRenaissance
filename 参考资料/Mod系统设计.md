# Mod系统设计

> 版本：v1.0  
> 设计原则：数据驱动，所有内容以JSON定义，支持热加载

---

## 一、Mod包结构

```
mods/
├── my_mod/
│   ├── manifest.json        # Mod元数据
│   ├── events/              # 自定义事件
│   │   └── my_events.json
│   ├── trains/              # 自定义列车
│   │   └── my_trains.json
│   ├── characters/          # 自定义角色
│   │   └── my_characters.json
│   ├── techs/               # 自定义科技
│   │   └── my_techs.json
│   └── assets/              # 资源文件
│       ├── icons/
│       └── audio/
```

---

## 二、manifest.json格式

```json
{
  "id": "my_mod",
  "name": "我的Mod",
  "version": "1.0.0",
  "author": "玩家名称",
  "description": "Mod描述",
  "dependencies": [],
  "conflicts": [],
  "load_order": 100
}
```

---

## 三、数据驱动内容

所有可Mod内容见各设计文档的JSON示例：
- 事件：铁龙竞争系统.md
- 列车：世界观与车辆设定.md
- 角色：角色设定.md
- 科技：科技树设计.md

---

*本文档为Mod系统的框架设计，具体实现待开发。*