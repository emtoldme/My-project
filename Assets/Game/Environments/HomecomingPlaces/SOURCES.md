# HomecomingPlaces 资源来源

- 三处Prefab（P_HC_OldHome / P_HC_OldSilo / P_HC_OldGuitar）由 `Editor/HomecomingPlacesPrefabBuilder.cs` 菜单
  “Reverb/Homecoming/生成三处地点Prefab（HC-A）”用Unity内置原始几何生成，**当前仓库中尚未生成，未在Unity中验证**。
- 材质全部复用 `Assets/Game/Art/VisualIdentity/HomecomingR3/` 既有R3材质（项目自有），针线盒尝试 VisualIdentity Brushed_Brass（找不到则回退Old iron，并打印警告）。
- 无外部素材、无参考图入库。所有碰撞体在生成时移除，不产生空气墙。
