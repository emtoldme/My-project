# 第一轮三个隔离实验：整合复核

2026-10-12 / Codex。基线main@9c565ee0ca87c6858145e5f656996aa7711f916b。保留四位作者提交，在integration/reverb-experiments-01集中导入与小范围修复；未改正式Core/Input/Views、S1/S2场景、Package版本或渲染管线。

## 收到的成果

| 任务 | 作者交付头SHA | 内容与作者验证范围 |
|---|---|---|
| E-01 / 2号 | bbe5fe62b3db22454ba2e678db9a5c3cb2e7b19a | 场景装配工具、声波池、ShaderGraph生成尝试与HLSL备选；未运行Unity |
| E-02 / 1号 | f9ea2752ffffd7faad7b71448e3a62e76a652637 | 纯C#夹具、四种组合对照、6条dotnet用例及运行项目；作者dotnet通过，Unity未验证 |
| E-03 / 音效总监 | 2a96f7df3c58010daa02c8df3739f7bc0a169981 | DSP双轨控制器、测试GUI、两条16秒TEST_ONLY WAV；未附Unity实测 |
| A-R01 / 主美 | 412f2b9d454f6fee7df2e150504e93a850ee8372 | 资源接入与美术审查规范，未制作正式美术 |

两个分支各自新增Assets/Experiments.meta产生add/add冲突，整合保留E-01父目录GUID，子资产GUID不改；没有让作者相互合并或改同一共享入口。

## 修正与实际观察

- E-01修ShaderGraph反射调用的可选参数/Slot构造、输出Alpha槽、Block描述读取及双面/不写深度设置；不把HLSL备选存在当图已通过。Mesh更新改为保留持久引用/GUID。模型尺寸/落地报告改为移动后重新测量。禁用时清池、临时Mesh销毁，开关关闭期间仍响应Restart清理。
- E-02派生事件基于父事件计划时间调度，避免一帧跨过回声和传播时把子事件错误推到下一帧；暂停期间拒绝直接命中。补两条能复现这两个问题的检查。
- E-03验证双轨采样率/长度/声道和加载状态；暂停取消未来调度，恢复用共同采样点和DSP起点重新安排，禁用/销毁停声。采样差用最短环形距离，A/B/A夹读丢弃跨音频更新的观察；不在测量过程中强制seek遮掩漂移。统计实际采样回绕和DSP估计循环，两者分别记录。
- A-R01纠正URP Lit通道：Metallic纹理R=金属/A=平滑，Occlusion槽读取G；通用ORM必须转换/显式拆分。根据包内LitInput.hlsl核对；作者具体服装/色卡/HDR/字体为提案，非用户批准最终创作。
- 实测FBX importer globalScale=1、fileScale=0.01；Visual适配后玩家高1.8m、脚底Y=0、玩法根比例1。原FBX厘米单位可用受控适配处理，不强行重写第三方资源。实际通路仍是现有机甲TEST_ONLY，不是乡村美术。

## 本轮实际验证

执行源码提交f1f3155d91d8f9c8d2fa8a90d82ce1eeb9b412e3；随后仅补文档、证据与任务卡。Unity6000.4.3f1 / URP17.4.0 / Windows，PlayMode使用D3D11。

| 检查 | 实际结果 | 证据 |
|---|---|---|
| Unity EditMode | 125/125通过，0失败（原117＋装备实验8） | editmode-final.xml |
| Unity实验PlayMode | 2/2通过，0失败，约165秒 | playmode-final.xml |
| E-01真实战斗事件 | Shot/Hit各1；波纹实际渲染差异1447像素；暂停冻结、禁用清理通过 | e01-runtime.txt、e01-wave-on/off.png |
| E-02对照 | 无模块/A/B/A+B总伤害10/15/14/23，队列清空 | e02-comparison.txt |
| E-03真实16秒双轨 | 实际回绕10次；起音及三次恢复的音频线程首信号差均小于64个输出样点，最后一次0 | e03-runtime.txt、测试源码及XML |
| 资源身份 | 377个唯一GUID，无重复；实验目录未缺meta | metadata-check.json |

ShaderGraph实际生成、绑定和绘制；粒子由一个真实Unity ParticleSystem手动按游戏dt推进，不是方块冒充粒子。截图是1280×720相机捕获，不含IMGUI，只证明接入和可见性，仍是旧机甲占位与素色地面，没有正式乡村画面。

首次运行暴露了MonoBehaviour字段初始化中创建MaterialPropertyBlock的Unity错误，以及timeSamples主线程最大差1024而非作者预想小于64的问题。前者已修复；后者没有通过seek掩盖，而是重新界定测量：主线程游标只检验缓冲范围，另用音频线程起音探针检查已知非静音TEST_ONLY信号的共同起点。最终运行998754次稳定读取、丢弃7次跨更新读取，最大环形游标差1024（本机buffer=1024×4）。最终门限为主线程差不超过2个buffer；这不能证明持续亚缓冲相位精度。音频线程首信号探针也不等于任意音乐相关性或硬件输出捕获。

本轮未跑原S2的14项PlayMode、未打新Windows包、未采正式Profiler/GPU数据、未测200敌压力。保留旧包与原项目窗口，不以历史测试代替本轮证据。

## 音频事实与边界

两条原WAV均48kHz、24bit、立体声、768000帧/16秒，样点峰值约-2dBFS；首尾阶跃约0.471/0.487满幅。离线对齐叠加，原GUI增益1/0.7和0.8/1达到约+1.234/+1.522dBFS，不能以单轨-2/-3dB保证母线不削波。详见audio-audit.json；该结果不是True Peak或真实声卡输出捕获。

实验控制器已加入0.5输出trim（约-6.02dB）。同样三组离线对齐混音样点峰值分别约-4.787/-4.499/-7.192dBFS，超过满幅的样点均为0；它只给当前测试信号留余量，不修复循环接缝，也不代表True Peak或最终母带通过。

全部轨道只是技术测试信号。没有真人听感、硬件输出相位、无爆音、正式配器/风格或音频来源链验收。源首尾阶跃不适合作无缝音乐示范；本轮不修编曲或冒充正式音乐制作。当前脚本级分层增益不等于已接入正式AudioMixer资源。

## 使用与下一步

E-01场景Assets/Experiments/VisualPipeline/Scenes/E01_VisualPipeline.unity；E-03场景Assets/Experiments/AudioSync/E03_AudioSync.unity。E-02结果e02-comparison.txt及作者DotnetRunner入口。场景不加入默认BuildSettings或替换旧试玩包；用户无需重复验收灰盒画面。本轮暂不打Windows包。

技术结论只适用于隔离夹具与实测范围，不代表正式乡村画质、正式装备接线、设备矩阵或大量敌人性能已通过。用户明确选择暂时继续技术实验，正式美术稍后准备。下一轮只复核规则适配边界、现有声波小规模性能和测试音频V2循环接缝，主美校对技术规范，不索要正式素材。Lore继续后排，不自动开始大型系统。四位下一项具体任务见Docs/Integration/REVERB_TASKS_02.md。
