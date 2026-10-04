#  — 2D 平台动作原型（Unity 2022）

从 Test 的 Unity 6 原型迁移至 Unity 2022.3，使用 C# 和 URP 的 2D 平台动作游戏原型，用于验证角色移动、跳跃、攻击、敌人行为和关卡配置。当前使用方块素材与 Tilemap 搭建测试场景，角色和敌人的主要数值通过 ScriptableObject 配置，并提供运行时调试面板。

本文根据 **2026-10-04** 的源码、已保存场景和配置资源整理。下文的“当前值”指资源文件中的值，功能状态区分场景接入情况与待完成的代码框架；迁移后的验证结果见“迁移记录与验证”；策划调参入口与源项目保持一致。

## 版本与环境

| 项目 | 当前配置 | 来源 / 说明 |
| --- | --- | --- |
| 游戏版本 | **0.1.0** | `Project Settings > Player > Version`，对应 `bundleVersion`；不代表已发布 Git Tag |
| Unity Editor | **2022.3.17f1c1** | [ProjectVersion.txt](ProjectSettings/ProjectVersion.txt)，修订号 `a01dafa986ec` |
| 项目名称 | `Test2` | Player Settings 中的 Product Name |
| 公司名称 | `DefaultCompany` | 发布前按实际项目修改 |
| 主场景 | `Assets/Scene/GameScene.unity` | 当前 Build Settings 中唯一启用的场景 |
| Standalone Build Number | `0` | 当前 Player Settings 配置 |
| Android Version Code | `1` | 当前 Player Settings 配置，未验证 Android 构建 |
| 输入设置 | `Both` | 新 Input System 用于角色输入，旧 Input 用于调试面板快捷键 |
| 项目阶段 | 玩法原型 | 暂无完整菜单、复活流程或正式角色动画 |

主要包版本取自 [manifest.json](Packages/manifest.json) 与 [packages-lock.json](Packages/packages-lock.json)：

| 包 | 版本 | 当前用途 / 状态 |
| --- | --- | --- |
| Universal Render Pipeline | `14.0.9` | 在 Unity 2022 中重新创建 PC / Mobile 管线与 Renderer；默认使用 PC 管线 |
| Input System | `1.19.0` | 移动、跳跃、攻击和暂停输入 |
| Cinemachine | `3.1.7` | 相机跟随与构图 |
| 2D Sprite / 2D Tilemap | 均为 `1.0.0` | 方块素材、Tile Palette 和关卡地形 |
| uGUI | `1.0.0` | 已安装；当前暂停 / 死亡面板尚未配置 |
| Test Framework | `1.1.33` | 玩家攻击框定位、父节点跟随与检测范围的 EditMode 回归测试 |

## 打开与运行

1. 在 Unity Hub 中使用 **2022.3.17f1c1** 添加并打开项目根目录。
2. 等待资源导入与脚本编译完成。
3. 打开 [GameScene.unity](Assets/Scene/GameScene.unity)。
4. 点击 Play，并将输入焦点放在 Game 窗口。
5. 使用下方按键体验原型；按反引号键打开调试面板。

构建时在 `File > Build Settings` 中选择目标平台，并确认场景列表包含 `GameScene`。本地构建输出放在 `Builds/`，由 `.gitignore` 排除。

### 默认操作

| 操作 | 按键 | 当前行为 |
| --- | --- | --- |
| 左右移动 | `A` / `D` | 仅读取 Move 的水平分量 |
| 跳跃 | `Space` | 支持离地宽限、跳跃输入缓存与松键截断 |
| 攻击 | `J` | 进入攻击状态；连击和实际命中接入情况见下文 |
| 暂停 / 继续 | `Esc` | 切换 `Time.timeScale`；当前没有暂停菜单 |
| 调试面板 | 反引号键，即英文键盘 Esc 下方的键 | 显示 / 隐藏 `Debug Config`，也可点击面板中的 `X` 关闭 |

角色实际使用 [PlayerControls.inputactions](Assets/inputAction/PlayerControls.inputactions) 和生成的 `PlayerControls.cs`。修改按键时编辑该 Input Actions 资源并应用更改，保留 `Generate C# Class` 开启，确保生成代码同步；不要直接修改生成的 C# 文件。另一个 `InputSystem_Actions.inputactions` 不是当前角色输入读取器使用的动作资源。

## 当前实现情况

| 模块 | 已实现内容 | 当前场景状态 |
| --- | --- | --- |
| 玩家移动 | 加速、减速、空中水平移动、朝向切换、接地检测 | 已接入 `Player` |
| 跳跃 | 起跳、离地宽限 Coyote Time、跳跃输入缓存、松键截断、下降速度限制 | 已接入；下降重力计算仍有待核验问题 |
| 玩家状态机 | `Idle / Run / Jump / Fall / Attack / Hurt / Dead` | 已接入；Animator 未赋值 |
| 攻击与连击 | 按配置数组执行攻击段，连击窗口内再次输入可进入下一段；提供动画事件开关攻击框 | 框架已接入，当前仅配置 1 段，缺少玩家攻击动画事件 |
| 命中与生命值 | 矩形范围检测、伤害、无敌计时、击退、血量与死亡事件、命中特效 / 音效接口 | 已接入；玩家另有一个常驻测试攻击框 |
| 命中停顿 | 命中时通过 `HitStop` 暂停并恢复时间缩放 | 已接入，命中调用固定 `0.05s` |
| 敌人 AI | 巡逻、边界 / 地面 / 墙检测、前方扇形感知、攻击前摇 / 后摇 / 冷却、受伤、死亡后销毁 | 已接入 `Enemy`；当前 `Wall Layer` 为空，无独立追击状态 |
| 相机 | Cinemachine 跟随、位置阻尼、死区与画面构图 | 已跟随 `Player`，当前主相机保存为透视投影 |
| 地形 | Grid、Tilemap、TilemapCollider2D 与 Tile Palette | 已有测试地形与方块素材 |
| 调试调参 | Player、Combo Hitbox、Enemy、Camera、Combat 五个折叠区 | 已挂载到 `GameManage`；Camera 区仅提示去 Inspector 调整 |
| 暂停与重开 | 时间缩放切换、重新加载当前场景的 `Restart()` | 暂停已接入；重开方法没有按钮绑定 |
| 检查点 / 终点 / 触发事件 | `Checkpoint`、`LevelEnd`、`TriggerZone` 脚本 | 已有代码，当前场景没有对应实例 |
| 游戏标记 | 设置 / 清除标记、事件通知、列表导入 / 导出、跨场景保留对象 | `GameFlags` 已挂载，仅运行时内存数据，未接入磁盘存档 |
| 死亡界面与复活 | 提供死亡面板和复活方法的基础代码 | 尚未完成：死亡回调未接到 GameManager，复活未恢复 HP / 状态 |

当前没有接入具体 Boss、任务、对话、联网玩法或单向平台穿透机制。标记常量和已安装的包不等于对应玩法已经实现。

## 策划调参入口与保存方式

| 想调整什么 | 在哪里调整 | 适用方式 |
| --- | --- | --- |
| 玩家移动、跳跃、攻击段 | `Assets/ScriptableObject/Character Data.asset` | 在 Project 中选中资源，通过 Inspector 编辑 |
| 敌人移动、感知、攻击数值 | `Assets/ScriptableObject/Enemy Data.asset` | 在 Project 中选中资源，通过 Inspector 编辑 |
| 实时比较手感 | Play 状态下按反引号，打开 `Debug Config` | 滑块直接修改上述共享数据资源，范围见下表 |
| 血量、地面层、检测点、组件引用 | Hierarchy 中的 `Player` / `Enemy` | 编辑对应组件；血量注意重复 Health 问题 |
| 敌人攻击框大小与命中资源 | `Assets/Perfabs/HitboxPerfab.prefab` 的 `Hitbox` | 攻击时部分字段会被 EnemyData 覆盖 |
| 相机视野与跟随手感 | Hierarchy 中的 `CinemachineCamera` | 编辑 Cinemachine Camera / Position Composer |
| 地形 | `Grid/Tilemap`，配合 `Assets/tileMap/New Tile Palette.prefab` | 使用 Tile Palette 绘制与修改地图 |
| 后续检查点、终点、事件 | 新建场景对象，挂对应脚本与触发 Collider2D | 当前场景尚无这些实例，接入说明见后文 |

### 推荐工作流程

1. **正式配置**：在停止 Play 后编辑数据资源、场景或 Prefab，并保存。
2. **实时试调**：Play 后打开面板，调整滑块，观察移动、跳跃、敌人攻击节奏；Combat 区可查看玩家状态、接地和朝向。
3. **确认生效**：下面注明“未接入”的字段目前不会改变对应玩法；`gravityScale` 和初始化 HP 等需要重新开始运行。
4. **落地结果**：停止 Play 后核对并保存最终值。面板没有保存、导出或回滚功能，直接修改 ScriptableObject；编辑器内的资源数值可能保留，不能套用普通场景对象退出 Play 自动恢复的经验。构建程序中的修改只在内存里，重启后恢复打包时的数据。
5. **协作提交**：数据调整提交 `.asset`，场景调整提交 `.unity`，Prefab 调整提交 `.prefab`，新增资源连同 `.meta` 提交，并检查版本差异中的实际数值。

相同数据资源会影响所有引用它的角色 / 敌人。需要不同配置时，可复制现有资源，或通过 `Assets > Create > Game > Character / Enemy` 新建资源，再赋给对象的 `Data` 字段；调试面板的 `Character Data / Enemy Data` 引用也要同步到实际测试资源。

### 玩家移动与跳跃

资源：[Character Data.asset](Assets/ScriptableObject/Character%20Data.asset)。字段名与 Inspector 对应；时间单位为秒，距离和速度以 Unity 世界单位计。

| 字段 | 当前值 | 调试面板范围 | 作用与生效说明 |
| --- | --- | --- | --- |
| `moveSpeed` | `8` | `1–20` | 水平目标速度，越大移动越快 |
| `acceleration` | `60` | `10–200` | 追上目标速度的速率，越大起步 / 空中转向越快 |
| `deceleration` | `80` | `10–200` | 停步、攻击和受伤时的水平减速速率 |
| `jumpForce` | `14` | `5–25` | 起跳时直接设置的纵向速度，越大通常跳得越高 |
| `gravityScale` | `3` | `1–8` | 在 PlayerController 的 `Awake()` 赋给 Rigidbody2D；面板修改后需重启运行，或直接修改运行中的 Rigidbody2D |
| `fallGravityMultiplier` | `1.8` | `1–4` | 设计用于下降重力修正；当前公式方向存在问题，见“待完善事项”，不能按越大下降越快理解 |
| `maxFallSpeed` | `20` | 仅 Inspector | 空中状态下限制向下速度 |
| `coyoteTime` | `0.1` | `0–0.3` | 离开地面后仍可接受跳跃的时间 |
| `jumpBufferTime` | `0.1` | `0–0.3` | 提前按下跳跃的输入保留时间 |
| `jumpCutMultiplier` | `0.5` | `0–1` | 上升中松开跳跃键时，对当前纵向速度乘此系数；越小短跳截断越明显 |

### 玩家生命值与战斗配置

| 字段 / 入口 | 当前值 | 调试面板范围 | 当前是否生效 |
| --- | --- | --- | --- |
| `Player > Health > Max HP` | 两个 Health 组件均为 `3` | 仅 Inspector | 当前实际 HP 的初始化来源；运行前修改，先处理重复组件 |
| `CharacterData.maxHP` | `100` | 仅 Inspector | **未接入玩家 Health**，改这里不会自动改变玩家实际血量 |
| `CharacterData.invincibleTime` | `0.8` | Combat：`0–2` | **未接入命中流程**；动态攻击框目前固定 `0.5s`，常驻测试框为 `0.8s` |
| `CharacterData.hitStopTime` | `0.08` | Combat：`0–0.3` | **未接入命中流程**；`Hitbox` 命中停顿固定调用 `0.05s` |
| `CharacterData.hurtKnockback` | `(5, 5)` | 仅 Inspector | **未接入**；实际击退由攻击者的 `Hitbox.knockback` 决定 |
| 玩家受伤状态时长 | `0.2s` | 无 | 固定在 `PlayerController` 代码中 |

`Health` 提供 `OnHPChanged`、`OnDamaged`、`OnDeath` 事件与 `Heal()` 方法，但当前没有血条展示或完整死亡 / 复活界面。

### 玩家攻击段与连击

在 `Character Data > Combo` 数组中调整每一段攻击。代码支持多段，**当前资源实际只有 1 段**，并没有配置好的三段连击。

| 每段字段 | 当前第 1 段值 | 调试面板范围 | 作用与限制 |
| --- | --- | --- | --- |
| `animName` | `Attck1` | 仅 Inspector | 当前仅保存字符串，代码没有用它播放动画 |
| `damage` | `2` | `0–10` | 动态攻击框伤害，创建时通过 `Mathf.RoundToInt` 转为整数 |
| `duration` | `1s` | `0.05–1` | 单段攻击结束时间；窗口内再次输入可提前进入下一段 |
| `comboWindowStart` | `0s` | `0–1` | 从本段开始计时，允许连击输入的起点 |
| `comboWindowEnd` | `0s` | `0–1` | 允许连击输入的终点 |
| `hitboxOffset` | `(1, 0)` | X：`-2–3`；Y：`-1–2` | 相对 `Hitbox Origin` 的局部偏移，由玩家父节点统一翻转；正 X 向面朝方向偏移，负 X 向反方向偏移 |
| `hitboxSize` | `(1, 1)` | X / Y：`0.2–3` | 动态攻击框的检测宽高 |
| `knockback` | `0` | `0–15` | 水平击退量，纵向击退为该值的 `0.5` 倍 |
| `sfx` | 未赋值 | 仅 Inspector | 当前同时用于攻击开始和命中时的音效 |
| `vfxPrefab` | 未赋值 | 仅 Inspector | 成功命中时生成的特效 Prefab |

配置有效连击窗口时，应保证 `0 ≤ comboWindowStart ≤ comboWindowEnd ≤ duration`，并留出可操作的时间区间。当前窗口为 `[0, 0]`；增加数组元素后还需要设置各段窗口。窗口外的攻击输入不会缓存到下一段。

动态攻击框依赖攻击动画事件调用 `PlayerAttackCombo.AE_OpenHitbox()` 和 `AE_CloseHitbox()`。当前玩家未配置 Animator、动画片段或这些事件，因此只改 Combo 参数不足以完成攻击表现和命中。

另有 `Player/hitbox` 上的常驻 `Hitbox`，当前伤害 `1`、大小 `(1, 1)`、击退 `3`、无敌时间 `0.8s`，目标层为 Enemy。它持续进行范围检测，不由 `J` 键开关，也不读取 Combo 的伤害 / 大小；每次启用期间，对同一个 Collider 只进行一次命中尝试。测试时要区分这个常驻框与动画事件生成的动态框。

#### 攻击框偏移规则

动态攻击框挂在 `Hitbox Origin` 下，`hitboxOffset` 使用该节点的局部坐标。玩家通过父节点 `scale.x` 切换朝向，攻击框继承一次翻转，代码不再额外将偏移 X 乘以朝向。例如原点在世界 X=100，偏移为 `(1, 0)` 时，朝右生成在 X=101，朝左生成在 X=99。

Gizmos 使用 `Hitbox Origin.TransformPoint()` 将同一局部偏移转换到世界坐标，使预览中心与实际生成中心一致，也包含原点的平移、旋转和缩放。策划只需配置一份偏移，不需要为左右朝向分别填写数值。

### 敌人数值与 AI

资源：[Enemy Data.asset](Assets/ScriptableObject/Enemy%20Data.asset)。

| 字段 | 当前值 | 调试面板范围 | 作用与生效说明 |
| --- | --- | --- | --- |
| `maxHP` | `3` | 仅 Inspector | EnemyAI 在 `Awake()` 中赋给 Health 的上限；需重启，且当前 HP 初始化顺序尚需完善 |
| `moveSpeed` | `3` | `0–10` | 巡逻水平速度 |
| `patrolDistance` | `4` | `0.5–10` | 从初始化位置向左右各巡逻 4 个单位，不是总长 4 |
| `detectRange` | `4` | `1–15` | 玩家感知距离 |
| `detectAngle` | `90°` | 仅 Inspector | 前方扇形总角度；当前没有墙体遮挡检测 |
| `ignoreY` | `true` | 仅 Inspector | 感知和攻击距离计算忽略高度；高低平台上的玩家也可能满足距离判断 |
| `attackRange` | `1.2` | `0.5–3` | 进入攻击状态的距离阈值，不会自动扩大攻击框 |
| `attackCooldown` | `1.5s` | `0.2–3` | 冷却在初始化与攻击时设置，并在攻击结束后再次重置 |
| `attackWindup` | `0.3s` | `0–1` | 进入攻击状态后，等待多久生成攻击框 |
| `attackRecovery` | `0.3s` | `0–1` | 与前摇一起决定攻击状态总长，不是攻击框存续时长 |
| `attackDamage` | `1` | 仅 Inspector | 创建敌人攻击框时赋值 |
| `knockback` | `3` | 仅 Inspector | 创建敌人攻击框时赋值，纵向击退为该值的 `0.5` 倍 |
| `hurtDuration` | `0.2s` | 仅 Inspector | 敌人受伤状态持续时间 |

敌人引用 [HitboxPerfab.prefab](Assets/Perfabs/HitboxPerfab.prefab)，在 Prefab 的 `Hitbox` 组件中可调 `size`（当前 `(1, 1)`）、`hitVfx` 和 `hitSfx`。生成时 `damage`、`knockback`、`targetLayer`、`owner` 会由 EnemyAI 设置，无敌时间固定为 `0.5s`；攻击框存续时间固定为 `0.15s`，暂时没有数据配置入口。

在 Scene 窗口启用 Gizmos，选中 Enemy 可查看感知扇形、攻击距离和巡逻范围；选中 Player 可查看 Combo 配置框。Gizmos 是辅助显示，实际判定以代码与场景检测结果为准，例如 `ignoreY=true` 时距离判断不使用玩家高度。

### 场景检测点、图层与相机

| 场景对象 / 字段 | 当前配置 | 策划可调整内容 |
| --- | --- | --- |
| `Player > PlayerController > Ground Check` | 引用子对象 `Check`，局部位置 `(0, -0.49, 0)` | 脚底检测位置；检测半径固定为 `0.1` |
| `Player > PlayerController > Ground Layer` | Ground | 哪些层判定为地面 |
| `Player > PlayerAttackCombo > Hitbox Origin` | 引用子对象 `hitbox` | 动态攻击框的生成基准点 |
| `Player > PlayerAttackCombo > Enemy Layer` | Enemy | 玩家动态攻击框的目标层 |
| `Enemy > Ground Check / Wall Check` | `groundCheck` / `wallCheck` | 检测点位置；检测半径固定为 `0.1` |
| `Enemy > Ground Layer / Wall Layer` | Ground / Nothing | 墙层当前为空，需按地形图层配置后才能检测墙 |
| `Enemy > Player Layer` | Player | 敌人攻击框的目标层 |
| `Enemy > Attack Origin` | `attackOrigin`，局部位置 `(0.48, 0, 0)` | 敌人攻击框生成位置 |
| `CinemachineCamera > Tracking Target` | Player | 相机跟随目标 |
| `CinemachineCamera > Lens > Field Of View` | `60°` | 当前透视投影的视野角度 |
| `Cinemachine Position Composer > Camera Distance` | `10` | 跟随时相机与目标的距离 |
| `Cinemachine Position Composer > Damping` | `(0.3, 0.42, 0)` | 水平 / 垂直跟随阻尼 |
| `Composition > Screen Position` | 约 `(0.1185, -0.0689)` | 目标在画面中的构图偏移 |
| `Composition > Dead Zone` | 启用，Size `(0.1, 0.2)` | 目标在画面内小幅移动时相机保持不动的区域 |
| `Lookahead` | 关闭 | 可在 Inspector 配置运动预判 |

当前 `Main Camera` 的 Projection 为 Perspective，Cinemachine Lens 沿用主相机模式。资源中虽有 `Orthographic Size = 5`，只有使用正交投影时才影响视野。调试面板的 Camera 区没有相机滑块。

当前主要 Layer 为 `Player(3)`、`Enemy(6)`、`Ground(7)`、`OneWay(8)`、`Hitbox(9)`、`Hurtbox(10)`。玩家还使用内置 `Player` Tag，敌人查找玩家与关卡触发都依赖这个 Tag。`OneWay` 等图层名称本身不会实现对应机制。

## 关卡事件的后续接入方式

这些脚本已经存在，但需要新增场景对象并配置引用才能体验：

| 脚本 | Inspector 可配置项 | 接入后行为 / 当前限制 |
| --- | --- | --- |
| `Checkpoint` | `checkpointId`、`respawnPoint` | 玩家进入触发区后记录复活点；未指定点位时使用自身位置，复活流程仍待补齐 |
| `LevelEnd` | `winPanel`、`winSfx` | 玩家进入后显示胜利面板、播放音效并暂停；需另行配置继续 / 重开入口 |
| `TriggerZone` | `flagId`、`triggerOnce`、`onTrigger` | 玩家进入后设置标记并执行 UnityEvent，可在 Inspector 绑定目标组件的公开方法 |
| `GameManager` | `player`、`input`、`deathPanel`、`pausePanel` | 管理暂停、检查点、重开等；当前面板引用均为空 |

触发区域需要 Collider2D 并开启 `Is Trigger`，进入者需要 `Player` Tag。唯一事件使用不同的 `flagId`，避免多个事件共用同一标记。`triggerOnce=true` 时会关闭区域碰撞体；再次加载场景时还可根据 GameFlags 中的标记禁用区域。

`GameFlags` 当前只在内存中保存字符串标记，`GetAll()` / `LoadAll()` 是数据交接接口，尚无文件保存 / 读取。`boss_goblin_defeated`、`room1_cleared`、`talked_to_elder` 是预留常量，尚未对应实际关卡内容。

## 待完善事项与调参限制

1. **生命值组件与初始化**：Player、Enemy 各挂载了两个 Health，逻辑通过 `GetComponent<Health>()` 获取组件，需整理为唯一实例。玩家配置中的 `maxHP=100` 尚未同步到实际 Health；敌人在 `Awake()` 中只更新上限，而 Health 也在 `Awake()` 初始化当前 HP，变更上限时需要统一初始化流程。
2. **玩家动态攻击接入**：补充 Animator、动画片段与攻击框开关事件，明确常驻测试框的用途。当前 `animName` 没有播放逻辑；多段连击还需补充数组元素与有效窗口。
3. **战斗数据接线**：`CharacterData.invincibleTime`、`hitStopTime`、`hurtKnockback` 尚未用于实际命中；当前固定的受伤时长、无敌时间、停顿和敌人攻击框存续时间需要统一为可配置数据。
4. **下降重力**：`ApplyAirMovement()` 中对负的 `Physics2D.gravity.y` 再做减法，额外项会向上补偿，不符合“提高下降重力”的设计意图；需修正公式后再确认该参数的手感。
5. **敌人环境检测**：当前 Wall Layer 为空；地面检测点是否足以提前识别平台边缘需要结合地图实测。感知也没有遮挡判断或独立追击逻辑。
6. **死亡、复活与菜单**：玩家死亡仅切换状态，未调用 `GameManager.OnPlayerDeath()`；死亡 / 暂停面板未赋值。`Respawn()` 目前只恢复时间与尝试移动位置，未恢复 HP、死亡状态或清理攻击状态；零向量复活点也会被当前条件跳过。
7. **关卡与存档**：检查点、终点、触发器尚未布置；标记尚未持久化，相关 UI 和流程仍需接入。

## 攻击框修复与回归测试

**2026-10-04**：已修复玩家动态攻击框的重复翻转。之前玩家朝左时，父节点负缩放与局部偏移上的朝向乘法相互抵消，使非零 X 偏移出现在错误的一侧。现在局部偏移只由父节点翻转，Gizmos 也按相同局部坐标转换显示。

测试脚本：[PlayerAttackComboTests.cs](Assets/Tests/Editor/PlayerAttackComboTests.cs)。在 Unity 中打开 `Window > General > Test Runner`，选择 EditMode，运行 `PlayerAttackComboTests` 可复查。

| 测试范围 | 用例数 | 修复后结果 |
| --- | --- | --- |
| 左右朝向下的正、负、零 X 偏移及 Y 偏移 | `8` | 全部通过 |
| 原点有局部偏移时，攻击框随玩家移动 | `2` | 全部通过 |
| 实际 Physics2D 矩形检测包含前方目标、排除后方目标 | `2` | 全部通过 |

源项目修复前共 `12` 项测试，其中 `5` 项失败；修复后全部通过。Test2 在 Unity **2022.3.17f1c1** 中重新运行，结果为 **12 / 12 通过**。测试调用真实的 `Begin()` 和 `AE_OpenHitbox()`，验证生成位置与 Physics2D 检测范围；玩家攻击动画和事件仍需按前文说明接入。

## 迁移记录与验证

**2026-10-04，游戏版本 0.1.0**：从 Test（Unity `6000.3.20f1`）迁移到 Test2（Unity `2022.3.17f1c1`）。

- 迁入玩法脚本、玩家 / 敌人数据、主场景、Prefab、方块素材、Tile Palette、Input Actions 与攻击框回归测试，并保留 `.meta` GUID。
- `PlayerController`、`EnemyAI`、`Health` 使用 Unity 2022 的 `Rigidbody2D.velocity`，取代 Unity 6 的 `linearVelocity`。
- URP 使用 `14.0.9`，通过目标编辑器重新创建 `PC_RPAsset` / `PC_Renderer` 和 `Mobile_RPAsset` / `Mobile_Renderer`，映射原管线中仍受支持的数值；Graphics 和全部画质档位默认指向 PC 管线。Mobile 管线供后续移动端调优使用，移动端未构建验证。
- 保留 Cinemachine `3.1.7` 的 Camera、Position Composer 和玩家跟随引用。该版本使用 `InputAction.activeValueType`，不能与 Test2 模板的 Input System `1.7.0` 一起编译，因此使用支持 Unity 2022.3 的 `1.19.0` 并重新生成 `PlayerControls.cs`。
- 同步 Player、Enemy、Ground、OneWay、Hitbox、Hurtbox 图层，输入保留 `Both`，游戏版本为 `0.1.0`，入口场景为 `GameScene`。
- 继续使用原有攻击框局部偏移修复，偏移仅随父节点朝向翻转一次。

迁移前的 Test2 资源和配置备份位于 `MigrationBackup~/BeforeUnity2022Port/`，由 `.gitignore` 排除。该目录只用于本地回滚，不是工程运行所需内容。

| 验证项目 | 结果 |
| --- | --- |
| 编辑器脚本编译 | 通过；Console 0 错误、0 警告 |
| 主场景组件、资源与图层引用 | 通过；无丢失脚本 / 损坏 Prefab，Sprite、Tilemap、数据与相机跟随引用有效 |
| 攻击框专项 EditMode 测试 | **12 / 12 通过**，0 失败、0 跳过 |
| Play Mode 移动、跳跃、攻击状态及相机 | 通过；另验证左右转向、朝左动态攻击框、暂停 / 恢复与敌人巡逻；截图无粉色材质 |
| Windows 64 位构建 | **Succeeded**；Development / Mono，0 错误、0 警告，输出 `Builds/Windows/Test2.exe`（约 104.77 MB） |

Play Mode 检查通过虚拟键盘输入触发真实输入和角色逻辑；朝左动态攻击框通过调用现有动画事件接口检查，当前没有配置动画自动调用该接口。Windows 构建已成功生成，未对独立程序完成完整关卡体验测试。

主场景保持源原型的功能状态：没有玩家攻击动画事件，仍有常驻测试攻击框和重复 Health，死亡复活流程尚未完成。迁移不代表这些原有玩法框架已经补齐，具体限制见前文。

## 项目结构

```text
Assets/
├── Scene/GameScene.unity              # 主测试场景
├── Scripts/                           # 玩法、数据定义与调试面板
├── Tests/Editor/                      # 玩家攻击框专项回归测试
├── ScriptableObject/
│   ├── Character Data.asset           # 玩家共享配置
│   └── Enemy Data.asset               # 敌人共享配置
├── inputAction/
│   ├── PlayerControls.inputactions    # 当前角色动作与按键
│   └── PlayerControls.cs              # 自动生成的输入封装
├── Perfabs/                           # 当前实际目录拼写
│   ├── HitboxPerfab.prefab             # 敌人攻击框
│   └── Square.prefab                   # 方块素材 Prefab
├── tileMap/New Tile Palette.prefab    # 地形绘制面板
├── Settings/                          # 在 Unity 2022 中生成的 URP 14 管线和 Renderer
└── Scenes/SampleScene.unity           # Test2 模板场景，保留但不参与构建
Packages/                              # 包清单与依赖锁文件
ProjectSettings/                       # Unity 版本、输入、图层、构建等设置
```

主要脚本职责：

| 脚本 | 职责 |
| --- | --- |
| `CharacterData` / `EnemyData` / `AttackStep` | 定义玩家、敌人与攻击段的数据字段 |
| `PlayerInputReader` / `PlayerController` | 读取输入，执行玩家状态与移动逻辑 |
| `PlayerAttackCombo` / `Hitbox` | 攻击段推进、攻击框创建和命中处理 |
| `Health` / `HitStop` | 生命值、无敌、伤害事件与命中停顿 |
| `EnemyAI` | 敌人巡逻、感知、攻击、受伤与死亡 |
| `GameManager` / `GameFlags` | 全局流程与运行时标记 |
| `Checkpoint` / `LevelEnd` / `TriggerZone` | 关卡触发行为 |
| `DebugConfigPanel` | IMGUI 运行时调参和状态显示 |

## 提交 GitHub 前

应提交 `Assets/`（包含 `.meta`）、`Packages/`、`ProjectSettings/`、本 README 及团队的 `.gitignore`。保留 `.meta`，否则资源 GUID 变化可能导致场景与资源引用丢失。

根目录已生成 [.gitignore](.gitignore)，排除以下 Unity 缓存、IDE 文件和构建产物：

```gitignore
/[Ll]ibrary/
/[Tt]emp/
/[Oo]bj/
/[Ll]ogs/
/[Uu]ser[Ss]ettings/
/[Bb]uild/
/[Bb]uilds/
/.vs/
*.csproj
*.sln
*.slnx
*.user

```

项目名称、公司名称和 Application Identifier 当前仍使用模板值，正式发布前需按项目修改。资源授权按实际来源保留，第三方字体等随包许可文件也应保留。

### 版本维护

- 游戏版本统一在 `Project Settings > Player > Version` 更新，并同步修改本 README 的版本信息。
- Unity Editor 版本以 `ProjectVersion.txt` 为准，包版本以 `manifest.json` / `packages-lock.json` 为准，不用 README 替代这些配置。
- 正式发布时可创建与游戏版本一致的 Git Tag，例如 `v0.1.0`；创建前完成对应平台运行与构建验证。
- 每次新增功能或改变调参入口时，同步更新实现状态、参数当前值和限制说明。

当前 `0.1.0` 快照包含玩家移动 / 跳跃状态机、攻击与生命值框架、敌人 AI、相机跟随、Tilemap 测试地图和运行时调试面板；动画攻击、完整死亡复活、关卡事件布置与存档仍待完善。
