# 首批用户素材源档案

2026-10-12。用户提供，原始来源见intake-manifest.json（SHA256/大小/源路径）。Assets/ReverbInTheRye下为Unity导入副本；本目录保留说明、GLB、离线预览和Blender源稿，避免Unity重复导入或隐式调用Blender。

- Ground：v0.2，3组1024贴图；原说明prototyping_only=true。原Ground脚本有反斜杠字符编译错误，源文件以.txt存档，Unity使用Foliage包随附的修正版。
- Foliage：v0.1，6个OBJ/GLB、Atlas与风动Shader；原型用途、阴影/风动待实际检查，LOD1与LOD0轮廓不等。
- SonicVFX：v0.1，候选C#/Shader；其源README明确不是正式美术定稿。当前源码懒创建特效池，Time.deltaTime、粒子自身时间与Shader的_Time必须由Codex适配后才能接暂停玩法。
- Weapon：读取用户已解压目录中的FBX、6套PBR和最新.blend；材质ZIP不包含FBX/.blend。建模脚本有绝对路径及清空/保存场景操作，作为源码保存，不自动执行。URP玻璃是透明近似，不能复现Blender传输/折射。
- CharacterWIP：主角Blockout v01、稻草人ArmRig v04及预览。仅入源档案；没有批准角色质量、完整动画或挂点，不替换游戏角色。

manifest记录导入时的输入内容；生成的meta/材质/Prefab/检查工具不在原始输入列表。后续工程修复应在验证文档列明，不回写用户外部原件。
