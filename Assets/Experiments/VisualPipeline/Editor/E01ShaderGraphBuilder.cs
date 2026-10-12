using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

namespace Waffle.Experiments.VisualPipeline.EditorTools
{
    /// <summary>
    /// 用反射调用 UnityEditor.ShaderGraph 的内部类型，生成 URP Unlit + Alpha Clip 图。
    /// 不直接引用内部类型，避免程序集可见性导致实验脚本编不过。
    /// 图里的环公式与 E01_WaveRing.shader 相同。失败时场景继续用那张 HLSL。
    /// </summary>
    public static class E01ShaderGraphBuilder
    {
        public static bool TryCreate(string path, out string detail)
        {
            try
            {
                TryLoad("Unity.ShaderGraph.Editor");
                TryLoad("Unity.RenderPipelines.Universal.Editor");
                Type graphType = Find("UnityEditor.ShaderGraph.GraphData");
                Type targetType = Find("UnityEditor.Rendering.Universal.ShaderGraph.UniversalTarget");
                Type unlitType = Find("UnityEditor.Rendering.Universal.ShaderGraph.UniversalUnlitSubTarget");
                Type blockType = Find("UnityEditor.ShaderGraph.BlockFieldDescriptor");
                Type targetBase = Find("UnityEditor.ShaderGraph.Target");
                if (graphType == null || targetType == null || unlitType == null || blockType == null || targetBase == null)
                {
                    detail = "ShaderGraph 或 URP Target 类型不在当前编辑器里";
                    return false;
                }

                object graph = Activator.CreateInstance(graphType, true);
                graphType.GetMethod("AddContexts").Invoke(graph, null);
                object target = Activator.CreateInstance(targetType, true);
                MethodInfo setSub = targetType.GetMethod("TrySetActiveSubTarget", new[] { typeof(Type) });
                if (setSub == null) setSub = targetType.GetMethod("TrySetActiveSubTarget");
                setSub.Invoke(target, new object[] { unlitType });
                SetMember(target, "alphaClip", true);

                Array blocks = Array.CreateInstance(blockType, 6);
                blocks.SetValue(Block("VertexDescription", "Position"), 0);
                blocks.SetValue(Block("VertexDescription", "Normal"), 1);
                blocks.SetValue(Block("VertexDescription", "Tangent"), 2);
                blocks.SetValue(Block("SurfaceDescription", "BaseColor"), 3);
                blocks.SetValue(Block("SurfaceDescription", "Alpha"), 4);
                blocks.SetValue(Block("SurfaceDescription", "AlphaClipThreshold"), 5);
                Array targets = Array.CreateInstance(targetBase, 1);
                targets.SetValue(target, 0);
                graphType.GetMethod("InitializeOutputs").Invoke(graph, new object[] { targets, blocks });

                Type category = Find("UnityEditor.ShaderGraph.CategoryData");
                object def = category.GetMethod("DefaultCategory", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
                graphType.GetMethod("AddCategory").Invoke(graph, new object[] { def });

                object radius = AddFloat(graph, graphType, "_Radius", 0.4f);
                object width = AddFloat(graph, graphType, "_Width", 0.09f);
                object quad = AddFloat(graph, graphType, "_QuadMeters", 6f);
                object cutoff = AddFloat(graph, graphType, "_Cutoff", 0.5f);
                object color = AddColor(graph, graphType, "_BaseColor", new Color(0.35f, 0.9f, 1f, 1f));
                Wire(graph, graphType, color, "SurfaceDescription.BaseColor");
                Wire(graph, graphType, cutoff, "SurfaceDescription.AlphaClipThreshold");
                object alphaNode = AddRingFunction(graph, graphType, radius, width, quad);
                if (alphaNode != null) WireNode(graph, graphType, alphaNode, 1, "SurfaceDescription.Alpha");

                Type fileUtil = Find("UnityEditor.ShaderGraph.FileUtilities");
                MethodInfo write = fileUtil.GetMethod("WriteShaderGraphToDisk", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                write.Invoke(null, new object[] { path, graph });
                detail = alphaNode != null ? "ring-function" : "properties-only";
                return true;
            }
            catch (Exception e)
            {
                detail = e.GetBaseException().GetType().Name + ": " + e.GetBaseException().Message;
                return false;
            }
        }

        static object AddFloat(object graph, Type graphType, string reference, float value)
        {
            Type propType = Find("UnityEditor.ShaderGraph.Internal.Vector1ShaderProperty");
            object prop = Activator.CreateInstance(propType, true);
            SetMember(prop, "displayName", reference.TrimStart('_'));
            SetMember(prop, "value", value);
            ForceReference(prop, reference);
            graphType.GetMethod("AddGraphInput").Invoke(graph, new object[] { prop, -1 });
            return AddPropertyNode(graph, graphType, prop);
        }

        static object AddColor(object graph, Type graphType, string reference, Color value)
        {
            Type propType = Find("UnityEditor.ShaderGraph.Internal.ColorShaderProperty");
            object prop = Activator.CreateInstance(propType, true);
            SetMember(prop, "displayName", reference.TrimStart('_'));
            SetMember(prop, "value", value);
            ForceReference(prop, reference);
            graphType.GetMethod("AddGraphInput").Invoke(graph, new object[] { prop, -1 });
            return AddPropertyNode(graph, graphType, prop);
        }

        static object AddPropertyNode(object graph, Type graphType, object prop)
        {
            Type nodeType = Find("UnityEditor.ShaderGraph.PropertyNode");
            object node = Activator.CreateInstance(nodeType, true);
            PropertyInfo property = nodeType.GetProperty("property", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (property != null) property.SetValue(node, prop);
            else nodeType.GetField("m_Property", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(node, prop);
            graphType.GetMethod("AddNode").Invoke(graph, new object[] { node, true });
            InvokeIfExists(node, "UpdateNodeAfterDeserialization");
            return node;
        }

        static object AddRingFunction(object graph, Type graphType, object radiusNode, object widthNode, object quadNode)
        {
            Type uvType = Find("UnityEditor.ShaderGraph.UVNode");
            Type fnType = Find("UnityEditor.ShaderGraph.CustomFunctionNode");
            if (uvType == null || fnType == null) return null;
            object uv = Activator.CreateInstance(uvType, true);
            graphType.GetMethod("AddNode").Invoke(graph, new object[] { uv, true });
            object fn = Activator.CreateInstance(fnType, true);
            SetMember(fn, "sourceType", Enum.Parse(Find("UnityEditor.ShaderGraph.Drawing.HlslSourceType"), "String"));
            SetMember(fn, "functionName", "E01_Wave");
            SetMember(fn, "functionBody",
                "float2 p = (UV.xy - 0.5) * QuadMeters;\n" +
                "float band = abs(length(p) - Radius);\n" +
                "Alpha = band < max(Width, 0.0001) ? 1 : 0;\n");
            AddInput(fn, 0, "UV", Find("UnityEditor.ShaderGraph.UVMaterialSlot"), Vector4.zero);
            AddInput(fn, 1, "Radius", Find("UnityEditor.ShaderGraph.Vector1MaterialSlot"), 0.4f);
            AddInput(fn, 2, "Width", Find("UnityEditor.ShaderGraph.Vector1MaterialSlot"), 0.09f);
            AddInput(fn, 3, "QuadMeters", Find("UnityEditor.ShaderGraph.Vector1MaterialSlot"), 6f);
            AddOutput(fn, 4, "Alpha");
            graphType.GetMethod("AddNode").Invoke(graph, new object[] { fn, true });
            Connect(graph, graphType, uv, 0, fn, 0);
            Connect(graph, graphType, radiusNode, 0, fn, 1);
            Connect(graph, graphType, widthNode, 0, fn, 2);
            Connect(graph, graphType, quadNode, 0, fn, 3);
            return fn;
        }

        static void AddInput(object node, int id, string name, Type slotType, object value)
        {
            object slot = CreateSlot(slotType, id, name, false, value);
            InvokeAddSlot(node, slot);
        }

        static void AddOutput(object node, int id, string name)
        {
            Type slotType = Find("UnityEditor.ShaderGraph.Vector1MaterialSlot");
            object slot = CreateSlot(slotType, id, name, true, 0f);
            InvokeAddSlot(node, slot);
        }

        static object CreateSlot(Type slotType, int id, string name, bool output, object value)
        {
            Type slotEnum = Find("UnityEditor.Graphing.SlotType");
            object kind = Enum.Parse(slotEnum, output ? "Output" : "Input");
            foreach (ConstructorInfo ctor in slotType.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                ParameterInfo[] p = ctor.GetParameters();
                if (p.Length == 5 && p[0].ParameterType == typeof(int))
                {
                    object[] args = { id, name, name, kind, value };
                    if (p[4].ParameterType == typeof(Vector4) && value is float f) args[4] = new Vector4(f, f, f, f);
                    try { return ctor.Invoke(args); } catch (ArgumentException) { }
                }
            }
            throw new MissingMethodException(slotType.Name + " ctor");
        }

        static void InvokeAddSlot(object node, object slot)
        {
            MethodInfo add = node.GetType().GetMethod("AddSlot", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            add.Invoke(node, new[] { slot });
        }

        static void Wire(object graph, Type graphType, object propertyNode, string descriptor)
        {
            WireNode(graph, graphType, propertyNode, 0, descriptor);
        }

        static void WireNode(object graph, Type graphType, object fromNode, int fromSlot, string descriptor)
        {
            object block = FindBlock(graph, graphType, descriptor);
            if (block == null) throw new InvalidOperationException("缺少块 " + descriptor);
            Connect(graph, graphType, fromNode, fromSlot, block, 0);
        }

        static void Connect(object graph, Type graphType, object fromNode, int fromSlot, object toNode, int toSlot)
        {
            object a = fromNode.GetType().GetMethod("GetSlotReference").Invoke(fromNode, new object[] { fromSlot });
            object b = toNode.GetType().GetMethod("GetSlotReference").Invoke(toNode, new object[] { toSlot });
            graphType.GetMethod("Connect").Invoke(graph, new[] { a, b });
        }

        static object FindBlock(object graph, Type graphType, string descriptor)
        {
            Type blockNode = Find("UnityEditor.ShaderGraph.BlockNode");
            MethodInfo getNodes = graphType.GetMethod("GetNodes").MakeGenericMethod(blockNode);
            foreach (object node in (IEnumerable)getNodes.Invoke(graph, null))
            {
                object value = node.GetType().GetField("m_SerializedDescriptor", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(node);
                if (value != null && value.ToString() == descriptor) return node;
            }
            return null;
        }

        static object Block(string nest, string name)
        {
            Type fields = Find("UnityEditor.ShaderGraph.BlockFields");
            Type nested = fields.GetNestedType(nest, BindingFlags.Public | BindingFlags.NonPublic);
            return nested.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
        }

        static void ForceReference(object prop, string reference)
        {
            if (!SetMember(prop, "referenceName", reference))
                SetMember(prop, "overrideReferenceName", reference);
        }

        static bool SetMember(object target, string name, object value)
        {
            Type type = target.GetType();
            while (type != null)
            {
                PropertyInfo property = type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (property != null && property.CanWrite)
                {
                    property.SetValue(target, value);
                    return true;
                }
                FieldInfo field = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field == null) field = type.GetField("m_" + char.ToUpper(name[0]) + name.Substring(1), BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null)
                {
                    field.SetValue(target, value);
                    return true;
                }
                type = type.BaseType;
            }
            return false;
        }

        static void InvokeIfExists(object target, string name)
        {
            MethodInfo method = target.GetType().GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (method != null && method.GetParameters().Length == 0) method.Invoke(target, null);
        }

        static void TryLoad(string name)
        {
            try { Assembly.Load(name); }
            catch (Exception) { }
        }

        static Type Find(string fullName)
        {
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = asm.GetType(fullName);
                if (type != null) return type;
            }
            return null;
        }
    }
}
