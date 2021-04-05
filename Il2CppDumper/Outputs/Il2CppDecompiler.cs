using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using static Il2CppDumper.Il2CppConstants;

namespace Il2CppDumper
{
    public class Il2CppDecompiler
    {
        // CBFMKGACLNE -> interface
        public static Dictionary<string, string> obfu_map = new Dictionary<string, string> {
            { "OGHLFADACAG", "Rpc" },
            { "ECELNFALKOB", "MessageStatus" },
            { "EOCEBECJBHG", "ClientType" },
            { "GameData_OFKOJOKOOAK", "GameData_PlayerInfo" },
            { "GameStartManager_ACONCACHDMF", "GameStartManager_Status" },
            { "InnerNetClient_CJDCOJJNIGL", "InnerNetClient_GameState" },
            { "IKHMOFOFDHI", "Sprites" },
            { "ShipStatus_LDCDLOPHBKL", "ShipStatus_MapType" },
            { "LGBKLKNAINN", "MapArea" },
            { "PNENNLOJJMG", "DeathType"},
            { "BDJGBMPCPBK", "ConnectionMode" },
            { "GLOFNFLNLAM", "GameMode" },
            { "EEJDFMLHCPB", "DiscoverState" },
            { "JBJHCLOILBF", "EndGameReason" },
            { "INCILCOCGMB", "BanReason" },
            { "JAJKFDICDFD", "RoomConnectionError" },
            { "FCKMHIKGFBO", "PlayerVoteStatus" },
            { "IntroCutscene_OIBLPHFGCPC", "IntroCutscene_Status" },
            { "ServerManager_KGJEBPIDCIF", "ServerManager_Status" },
            { "JCLEEAHKPKG", "playerInfo" },
            { "FMAAJCIEMEH", "PlayerId" },
            { "CBEJMNMADDB", "playerControl" },
            { "LNFMCJAPLBH", "PlayerName" },
            { "_c_5__2", "title_color" },
            { "_fade_5__3", "color" },
            { "_impColor_5__4", "subtitle_color" },
            { "__4__this", "_this" },
            { "KAPJFCMEBJE", "PlayerDeathReason" },
            { "CBFIAGIGOFA", "TaskType" },
            { "NormalPlayerTask_OHBIFBFAGPH", "NormalPlayerTask_State" }
        };

        public static string[] klasses = new string[] {
            "Behaviour",
            "Bounds",
            "Color",
            "ColorSpace",
            "Component",
            "DefaultFormat",
            "DestroyableSingleton",
            "FilterMode",
            "FormatUsage",
            "GameObject",
            "GraphicsFormat",
            "HideFlags",
            "Material",
            "MaterialGlobalIlluminationFlags",
            "Matrix4x4",
            "MonoBehaviour",
            "Object",
            "PrimitiveType",
            "Quaternion",
            "Rect",
            "Renderer",
            "RenderTextureSubElement",
            "RotationOrder",
            "Scene",
            "SendMessageOptions",
            "ShaderPropertyFlags",
            "ShadowCastingMode",
            "Space",
            "Sprite",
            "SpriteDrawMode",
            "SpriteMaskInteraction",
            "SpriteMeshType",
            "SpritePackingMode",
            "SpritePackingRotation",
            "SpriteRenderer",
            "SpriteSortPoint",
            "SpriteTileMode",
            "Texture",
            "Texture2D",
            "TextureCreationFlags",
            "TextureFormat",
            "TextureWrapMode",
            "Transform",
            "Vector2",
            "Vector3",
            "Vector4",


            "MessageReader",
            "MessageWriter",
            "SendOption",
            "EventArgs",
            "DataReceivedEventArgs",

            "AmongUsClient",
            "BanReason",
            "ButtonBehavior",
            "GameData",
            "GameData_PlayerInfo",
            "GameStartManager_Status",
            "EndGameManager",
            "EndGameReason",
            "DiscoverState",
            "DeathType",
            "MeetingHud",
            "PlayerControl",
            "ConnectionMode",
            "IntroCutscene",
            "IntroCutscene_Status",
            "KillButtonManager",
            "Rpc",
            "GameMode",
            "GameStartManager",
            "InnerNetClient",
            "InnerNetClient_GameState",
            "InnerNetObject",
            "PlayerControl",
            "PlayerTask",
            "PlayerVoteStatus",
            "PlayerDeathReason",
            "HudManager",
            "KillReason",
            "MessageStatus",
            "MapArea",
            "MapBehaviour",
            "NormalPlayerTask",
            "NormalPlayerTask_State",
            "ClientType",
            "RoomConnectionError",
            "ServerManager",
            "ServerManager_Status",
            "ShipStatus",
            "ShipStatus_MapType",
            "Sprites",
            "StringNames",
            "TextRenderer",
            "TaskType",
            "UiElement",
            "UseButtonManager",
            "Vent",
            "VoteStatus",

            "List",
            "List_Enumerator",
            "Dictionary",
        };
        

        private Il2CppExecutor executor;
        private Metadata metadata;
        private Il2Cpp il2Cpp;
        private Dictionary<Il2CppMethodDefinition, string> methodModifiers = new Dictionary<Il2CppMethodDefinition, string>();

        public Il2CppDecompiler(Il2CppExecutor il2CppExecutor)
        {
            executor = il2CppExecutor;
            metadata = il2CppExecutor.metadata;
            il2Cpp = il2CppExecutor.il2Cpp;
        }


        public string cpp_value(string value)
        {
            if (value == "False") return "false";
            if (value == "True") return "true";

            value = value.Replace(",", ".");
            return value;
        }

        static public string deobfu(string name)
        {
            if (obfu_map.ContainsKey(name)) return obfu_map[name];

            return name;
        }

        public string clear_name(string name)
        {
            /*
            if (name == "<Module>") return "module";

            if (name.Contains(".<") || name.StartsWith("<"))
            {
                var index = name.IndexOf('<');
                var index_end = name.IndexOf('>');
                name.Remove(index);
                name.Remove(index_end);
            }*/
            name = name.Replace("<", "_");
            name = name.Replace(">", "_");
            name = name.Replace("::", "_");

            return name;
        }

        private string header_include(Il2CppType type)
        {
            string output;
            string type_name = "";
            string namespaze = "";
            string template_decl = "";

            switch (type.type)
            {
                case Il2CppTypeEnum.IL2CPP_TYPE_ARRAY:
                case Il2CppTypeEnum.IL2CPP_TYPE_SZARRAY:
                    {
                        return "#include <cs/array.hpp>";
                    }
                    
                        
                case Il2CppTypeEnum.IL2CPP_TYPE_MVAR: return "";
                case Il2CppTypeEnum.IL2CPP_TYPE_VALUETYPE: return $"#include <{include_path(type)}.hpp>";
                case Il2CppTypeEnum.IL2CPP_TYPE_STRING:
                    {
                        return "#include <cs/string.hpp>";
                    }

                case Il2CppTypeEnum.IL2CPP_TYPE_GENERICINST:
                    {
                        Il2CppTypeDefinition typeDef;
                        Il2CppGenericClass genericClass = il2Cpp.MapVATR<Il2CppGenericClass>(type.data.generic_class);

                        typeDef = executor.GetGenericClassTypeDefinition(genericClass);

                        var @namespace = metadata.GetStringFromIndex(typeDef.namespaceIndex);
                        if (@namespace != "")
                        {
                            type_name += @namespace + ".";
                        }

                        type_name += metadata.GetStringFromIndex(typeDef.nameIndex);
                        var index = type_name.IndexOf("`");
                        if (index != -1) type_name = type_name.Substring(0, index);
                        template_decl = "template <class...> ";//Il2CppExecutor.GetGenericContainerParams(genericContainer, generic_decl)

                        break;
                    }

                default:
                    {
                        if (type.type > Il2CppTypeEnum.IL2CPP_TYPE_R8)
                            type_name = executor.GetTypeName(type, true, false);
                        else return "";
                        break;
                    }
            }

            type_name = type_name.Replace(".", "::");
            type_name = type_name.Replace("*", "");

            var nsindex = type_name.LastIndexOf("::");
            if (nsindex > 0)
            {
                namespaze = type_name.Substring(0, nsindex);
                type_name = type_name.Substring(nsindex + 2);
                output = "namespace " + namespaze + "{ " + template_decl + "struct " + deobfu(type_name) + "; }";
            }
            else output = "struct " + deobfu(type_name) + ";";

            return output;
        }

        private string include_path(Il2CppType type)
        {
            string output;
            string type_name = "";

            switch (type.type)
            {
                case Il2CppTypeEnum.IL2CPP_TYPE_ARRAY:
                case Il2CppTypeEnum.IL2CPP_TYPE_SZARRAY:
                {
                    return "cs/array";
                }
                case Il2CppTypeEnum.IL2CPP_TYPE_STRING:
                {
                    return "cs/string";
                }
                    
                case Il2CppTypeEnum.IL2CPP_TYPE_GENERICINST:
                {
                    Il2CppTypeDefinition typeDef;
                    Il2CppGenericClass genericClass = il2Cpp.MapVATR<Il2CppGenericClass>(type.data.generic_class);

                    typeDef = executor.GetGenericClassTypeDefinition(genericClass);

                    var @namespace = metadata.GetStringFromIndex(typeDef.namespaceIndex);
                    if (@namespace != "")
                    {
                        type_name += @namespace + ".";
                    }

                    type_name += metadata.GetStringFromIndex(typeDef.nameIndex);
                    var index = type_name.IndexOf("`");
                    if (index != -1) type_name = type_name.Substring(0, index);
                    break;
                }

                default:
                    type_name = executor.GetTypeName(type, true, false);
                    break;
            }

            type_name = deobfu(type_name);
            type_name = type_name.Replace(".", "/");
            type_name = type_name.Replace("::", "/");
            type_name = type_name.Replace("*", "");
            output = "au/" + type_name;

            return output;
        }

        public void Decompile(Config config, string outputDir)
        {
            //var writer = new StreamWriter(new FileStream(outputDir + "dump.cs", FileMode.Create), new UTF8Encoding(false));

            //dump image
            for (var imageIndex = 0; imageIndex < metadata.imageDefs.Length; imageIndex++)
            {
                //var imageDef = metadata.imageDefs[imageIndex];
                //writer.Write($"// Image {imageIndex}: {metadata.GetStringFromIndex(imageDef.nameIndex)} - {imageDef.typeStart}\n");
            }
            //dump type
            foreach (var imageDef in metadata.imageDefs)
            {
                string typeName = "";
                try
                {
                    var imageName = metadata.GetStringFromIndex(imageDef.nameIndex);

                    var typeEnd = imageDef.typeStart + imageDef.typeCount;
                    for (int typeDefIndex = imageDef.typeStart; typeDefIndex < typeEnd; typeDefIndex++)
                    { 
                        var types = new HashSet<Il2CppType>();
                        var hpp_includes = new HashSet<string>();
                        var cpp_includes = new HashSet<string>();

                        bool is_generic = false;
                        bool is_interface = false;
                        var typeDef = metadata.typeDefs[typeDefIndex];
                        typeName = executor.GetTypeDefName(typeDef, false, false);
                        var genericTypeName = executor.GetTypeDefName(typeDef, false, true);
                        var generic_decl = executor.GetTypeDefName(typeDef, false, true, true);
                        var extends = new List<string>();
                        var namespaze = metadata.GetStringFromIndex(typeDef.namespaceIndex);
                        namespaze = namespaze.Replace(".", "::");

                        typeName = clear_name(typeName);
                        typeName = deobfu(typeName);

                        // if (typeName == "Object" && namespaze == "unityEngine") extends.add(il2cppObject);

                        var fullTypeName = typeName;
                        if (namespaze != "") fullTypeName = namespaze + "::" + typeName;

                        if (executor.GetTypeDefName(typeDef, false, true).Contains("<")) is_generic = true;

                        // skip
                        //if ((typeDef.flags & TYPE_ATTRIBUTE_INTERFACE) != 0) continue;
                        //if ((typeDef.flags & TYPE_ATTRIBUTE_NESTED_FAM_OR_ASSEM) != 0) continue;
                        if (klasses.Count() > 0 && !klasses.Contains(typeName)) continue;

                        // outputs
                        string hpp_headers = "";
                        string cpp_headers = "";
                        string statics_def = "";
                        string meta_statics = "";
                        string methods_rva = "";
                        string fields = "";
                        string methods = "";
                        string methods_decl = "";
                        string methods_def = "";
                        string klass = "";
                        string klass_meta = "";
                        string klass_inerhit = "";

                        // file
                        string file_path = "au/" + namespaze.Replace("::", "/") + "/";
                        file_path += typeName;

                        StreamWriter hpp_writer;
                        StreamWriter cpp_writer;
                        try
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(file_path));

                            hpp_writer = new StreamWriter(new FileStream(outputDir + file_path + ".hpp", FileMode.Create), new UTF8Encoding(false));
                            cpp_writer = new StreamWriter(new FileStream(outputDir + file_path + ".cpp", FileMode.Create), new UTF8Encoding(false));
                        }
                        catch (Exception) { continue; }

                        if (typeDef.parentIndex >= 0)
                        {
                            var parent = il2Cpp.types[typeDef.parentIndex];
                            var parentName = executor.GetTypeName(parent, true, false, false);
                            if (!typeDef.IsValueType && !typeDef.IsEnum && parentName != "object")
                            {
                                extends.Add(parentName);
                                //types.Add(parent);
                                hpp_includes.Add("#include <" + include_path(parent) + ".hpp>\n");
                            }
                        }
                        if (typeDef.interfaces_count > 0)
                        {
                            for (int i = 0; i < typeDef.interfaces_count; i++)
                            {
                                var @interface = il2Cpp.types[metadata.interfaceIndices[typeDef.interfacesStart + i]];
                                //extends.Add(executor.GetTypeName(@interface, false, false));
                            }
                        }


                        if (is_generic) klass += generic_decl + "\n";



                        if (typeDef.IsEnum)
                            klass += "enum class ";
                        else
                            klass += "struct ";

                        klass += typeName;

                        genericTypeName = deobfu(genericTypeName.Replace("::", "_"));
                        if (!typeDef.IsEnum)
                        {
                            if (extends.Count > 0)
                                klass_inerhit = $" : ark::meta<{genericTypeName}, {string.Join(", ", extends)}>";
                            else klass_inerhit = $" : ark::meta<{genericTypeName}>";
                            klass_meta = $"ark_meta(\"{namespaze}\", \"{typeName}\", \"\");";
                        }

                        

                        //dump field
                        if (config.DumpField && typeDef.field_count > 0)
                        {
                            fields += "\n    // Fields\n\n";
                            var fieldEnd = typeDef.fieldStart + typeDef.field_count;
                            for (var i = typeDef.fieldStart; i < fieldEnd; ++i)
                            {
                                var fieldDef = metadata.fieldDefs[i];
                                var fieldType = il2Cpp.types[fieldDef.typeIndex];
                                var isStatic = false;
                                var isConst = false;

                                var field_typename = executor.GetTypeName(fieldType, true, false, true);
                                field_typename = deobfu(field_typename);
                                var field_name = metadata.GetStringFromIndex(fieldDef.nameIndex);
                                field_name = deobfu(field_name);
                                field_name = clear_name(field_name);


                                types.Add(fieldType);

                                fields += "    ";
                                var access = fieldType.attrs & FIELD_ATTRIBUTE_FIELD_ACCESS_MASK;

                                // enum
                                if (typeDef.IsEnum)
                                {
                                    if (i == typeDef.fieldStart) klass_inerhit = " : " + field_typename;
                                    else
                                    {
                                        fields += field_name;

                                        if (metadata.GetFieldDefaultValueFromIndex(i, out var fieldDefaultValue) && fieldDefaultValue.dataIndex != -1)
                                        {
                                            if (TryGetDefaultValue(fieldDefaultValue.typeIndex, fieldDefaultValue.dataIndex, out var value))
                                            {
                                                fields += $" = ";
                                                if (value is string str)
                                                {
                                                    fields += $"\"{str.ToEscapedString()}\"";
                                                }
                                                else if (value is char c)
                                                {
                                                    var v = (int)c;
                                                    fields += $"'\\x{v:x}'";
                                                }
                                                else if (value != null)
                                                {
                                                    fields += $"{value}";
                                                }
                                            }
                                            else
                                            {
                                                fields += $" /*Metadata offset 0x{value:X}*/";
                                            }
                                        }
                                        fields += ",";
                                    }
                                }
                                // class
                                else
                                {
                                    if ((fieldType.attrs & FIELD_ATTRIBUTE_LITERAL) != 0)
                                    {
                                        isConst = true;
                                        fields += "inline static constexpr ";
                                    }
                                    else
                                    {
                                        if ((fieldType.attrs & FIELD_ATTRIBUTE_STATIC) != 0)
                                        {
                                            isStatic = true;
                                            fields += "static ";
                                            var return_type = executor.GetTypeName(fieldType, true, false, true);
                                            statics_def += "\n" + return_type + " " + executor.GetTypeDefName(typeDef, true, true) + "::" + field_name + "() { return statics()->" + field_name + "; }";
                                            meta_statics += "\n    " + return_type + " " + field_name + ";";
                                        }
                                    }

                                    if (field_typename == "object") field_typename = typeName + "*";
                                    if (isConst && field_typename == "cs::string*") field_typename = "const char*";

                                    fields += $"{field_typename} {field_name}";
                                    if (isStatic) fields += "()";

                                    if (metadata.GetFieldDefaultValueFromIndex(i, out var fieldDefaultValue) && fieldDefaultValue.dataIndex != -1)
                                    {
                                        if (TryGetDefaultValue(fieldDefaultValue.typeIndex, fieldDefaultValue.dataIndex, out var value))
                                        {
                                            fields += $" = ";
                                            if (value is string str)
                                            {
                                                fields += $"\"{str.ToEscapedString()}\"";
                                            }
                                            else if (value is char c)
                                            {
                                                var v = (int)c;
                                                fields += $"'\\x{v:x}'";
                                            }
                                            else if (value != null)
                                            {
                                                fields += cpp_value($"{value}");
                                            }
                                        }
                                        else
                                        {
                                            fields += $" /*Metadata offset 0x{value:X}*/";
                                        }
                                    }
                                    if (config.DumpFieldOffset && !isConst)
                                        fields += $"; // 0x{il2Cpp.GetFieldOffsetFromIndex(typeDefIndex, i - typeDef.fieldStart, i, typeDef.IsValueType, isStatic):X}";
                                    else
                                        fields += ";\n";
                                }
                                fields += "\n";
                            }
                        }
                        
                        //dump method
                        if (config.DumpMethod && typeDef.method_count > 0)
                        {
                            methods_decl += "\n    // Methods\n\n";
                            methods_def += "\n    // Methods\n\n";

                            var methodEnd = typeDef.methodStart + typeDef.method_count;
                            List<string> method_names = new List<string>();
    
                            for (var i = typeDef.methodStart; i < methodEnd; ++i)
                            {
                                var methodDef = metadata.methodDefs[i];
                                ulong fixedMethodPointer = 0;

                                var methodPointer = il2Cpp.GetMethodPointer(imageName, methodDef);
                                if (methodPointer > 0)
                                {
                                    fixedMethodPointer = il2Cpp.GetRVA(methodPointer);
                                }

                                methods_decl += "    ";
                                methods_def += "    ";
                                var methodReturnType = il2Cpp.types[methodDef.returnType];
                                types.Add(methodReturnType);

                                var methodName = metadata.GetStringFromIndex(methodDef.nameIndex);
                                methodName = deobfu(methodName);
                                methodName = methodName.Replace(".", "");
                                methodName = clear_name(methodName);

                                var original_method_name = methodName;
                                int name_count = method_names.Where(x => x.Equals(original_method_name)).Count();
                                if (name_count > 0)
                                {
                                    methodName += name_count.ToString();
                                }
                                method_names.Add(original_method_name);

                                var is_generic_method = false;
                                var comment = "";
                                if (methodDef.genericContainerIndex >= 0)
                                {
                                    var genericContainer = metadata.genericContainers[methodDef.genericContainerIndex];
                                    is_generic_method = true;
                                    //methodName = "// " + methodName + executor.GetGenericContainerParams(genericContainer);
                                    comment = "// ";
                                }
                                if (methodReturnType.byref == 1)
                                {
                                    methods_decl += "/*ref*/ ";
                                }

                                var methodReturnTypeName = executor.GetTypeName(methodReturnType, true, false);
                                methods_decl += comment + $"{methodReturnTypeName} {methodName}(";
                                methods_def += comment + $"{methodReturnTypeName} {typeName}::{methodName}(";

                                // rva
                                //methods_rva += $"\n    method_rva({executor.GetTypeDefName(typeDef, true, true)}::{methodName}, 0x{fixedMethodPointer:X})";

                                var rvamethod_name = $"{ executor.GetTypeDefName(typeDef, true, true) }::{methodName}";
                                var rvamethd_adress = $"0x{fixedMethodPointer:X}";

                                if (fixedMethodPointer != 0) methods_rva += "\n    template<> inline uintptr_t rva<&" + rvamethod_name + "> () { return " + rvamethd_adress + "; }";

                                var parameters_decl = new List<string>();
                                var parameters_def = new List<string>();
                                var parameter_names = new List<string>();
 
                                for (var j = 0; j < methodDef.parameterCount; ++j)
                                {
                                    var parameterStr_decl = "";
                                    var parameterStr_def = "";
                                    var parameterDef = metadata.parameterDefs[methodDef.parameterStart + j];
                                    var parameterName = metadata.GetStringFromIndex(parameterDef.nameIndex);
                                    parameterName = clear_name(parameterName);
                                    parameterName = deobfu(parameterName);
                                    var parameterType = il2Cpp.types[parameterDef.typeIndex];
                                    types.Add(parameterType);
                                    var parameterTypeName = executor.GetTypeName(parameterType, true, false, true);
                                    parameterTypeName = deobfu(parameterTypeName);
                                    if (parameterTypeName == "object") parameterTypeName = typeName + "*";

                                    parameter_names.Add(parameterName);

                                    if (parameterType.byref == 1)
                                    {
                                        if ((parameterType.attrs & PARAM_ATTRIBUTE_OUT) != 0 && (parameterType.attrs & PARAM_ATTRIBUTE_IN) == 0)
                                        {
                                            parameterStr_decl += "/*out*/ ";
                                        }
                                        else if ((parameterType.attrs & PARAM_ATTRIBUTE_OUT) == 0 && (parameterType.attrs & PARAM_ATTRIBUTE_IN) != 0)
                                        {
                                            parameterStr_decl += "/*in*/ ";
                                        }
                                        else
                                        {
                                            parameterStr_decl += "/*ref*/ ";
                                        }
                                    }

                                    parameterStr_decl += $"{parameterTypeName} {parameterName}";
                                    parameterStr_def += $"{parameterTypeName} {parameterName}";

                                    if (metadata.GetParameterDefaultValueFromIndex(methodDef.parameterStart + j, out var parameterDefault) && parameterDefault.dataIndex != -1)
                                    {
                                        if (TryGetDefaultValue(parameterDefault.typeIndex, parameterDefault.dataIndex, out var value))
                                        {
                                            parameterStr_decl += " = ";
                                            if (value is string str)
                                            {
                                                parameterStr_decl += $"\"{str.ToEscapedString()}\"";
                                            }
                                            else if (value is char c)
                                            {
                                                var v = (int)c;
                                                parameterStr_decl += $"'\\x{v:x}'";
                                            }
                                            else if (value != null)
                                            {
                                                value = cpp_value(value.ToString());
                                                parameterStr_decl += $"{parameterTypeName}({value})";
                                            }
                                        }
                                        else
                                        {
                                            parameterStr_decl += $" /*Metadata offset 0x{value:X}*/";
                                        }
                                    }

                                    parameters_decl.Add(parameterStr_decl);
                                    parameters_def.Add(parameterStr_def);
                                }

                                var methodCall = $"return method_call({methodName}, {string.Join(", ", parameter_names)}); ";
                                if (parameter_names.Count() == 0) 
                                    methodCall = $"return method_call({methodName}); ";

  
                                methods_decl += string.Join(", ", parameters_decl);
                                methods_decl += ");";
                                methods_decl += $" // 0x{fixedMethodPointer:X} // ";
                                methods_decl += GetModifiers(methodDef);
                                methods_decl += "\n";

                                methods_def += string.Join(", ", parameters_def);
                                methods_def += ") { " + methodCall + "} ";
                                methods_def += $" // 0x{fixedMethodPointer:X} // ";
                                methods_def += "\n";
                                
                            }
                        }

                        // headers
                        foreach(Il2CppType type in types)
                        {
                            if (! ((int)type.type < 0x0e
                                || type.type == Il2CppTypeEnum.IL2CPP_TYPE_MVAR
                                || type.type == Il2CppTypeEnum.IL2CPP_TYPE_VAR
                                || type.type == Il2CppTypeEnum.IL2CPP_TYPE_I
                                || type.type == Il2CppTypeEnum.IL2CPP_TYPE_U))
                            {
                                if (executor.GetTypeName(type, false, true) != typeName)
                                {
                                    hpp_includes.Add(header_include(type) + "\n");
                                }

                                /*
                                if ((int)type.type != 0x1c && (int)type.type != 0x18 && (int)type.type != 0x19 && (int)type.type != 0x1e)
                                    cpp_includes.Add($"#include <{include_path(type)}.hpp>\n");

                                if (is_generic || (type.type == Il2CppTypeEnum.IL2CPP_TYPE_GENERICINST || type.type == Il2CppTypeEnum.IL2CPP_TYPE_VALUETYPE))
                                    hpp_includes.Add($"\n#include <{include_path(type)}.hpp>\n");
                                else if (executor.GetTypeName(type, false, true) != typeName)
                                {
                                    var fwd = fwd_decl(type);
                                    if (fwd != "") hpp_includes.Add(fwd + "\n");
                                }*/
                            }
                        }

                        foreach (string include in hpp_includes) hpp_headers += include;
                        foreach (string include in cpp_includes) cpp_headers += include;

                        meta_statics = "namespace ark\n{\ntemplate<>\nstruct meta_statics<" + fullTypeName + ">\n{" + meta_statics + "\n};\n} // ark\n";

                        if (is_interface || is_generic)
                        {
                            meta_statics = "";
                            statics_def = "";
                            methods_rva = "";
                        }

                        // hpp
                        hpp_writer.Write("//" + file_path + "\n#pragma once\n#include <ark/class.hpp>\n");
                        hpp_writer.Write(hpp_headers + "\n");

                        if (namespaze != "") hpp_writer.Write("\nnamespace " + namespaze + " {\n");

                        hpp_writer.Write(klass + klass_inerhit + "\n{\n" + klass_meta + "\n");

                        hpp_writer.Write(fields);

                        hpp_writer.Write(methods_decl);

                        hpp_writer.Write("\n};\n");
                        if (namespaze != "") hpp_writer.Write("\n} // ns");

                        hpp_writer.Write("\n\n");

                        hpp_writer.Write(meta_statics);

                        hpp_writer.Write("\n\nnamespace ark::method_info \n{");
                        hpp_writer.Write(methods_rva);
                        hpp_writer.Write("\n} // ark::method_info");

                        hpp_writer.Close();

                        if (!is_generic)
                        {
                            cpp_writer.Write("#include <" + file_path + ".hpp>\n");
                            cpp_writer.Write(cpp_headers);
                            if (namespaze != "") cpp_writer.Write("\nnamespace " + namespaze + " {\n");
                            cpp_writer.Write(methods_def);
                            if (namespaze != "")  cpp_writer.Write("\n}\n\n");

                            cpp_writer.Write(statics_def);

                            cpp_writer.Close();
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("ERROR: Some errors in dumping " + e.Message + " (" + typeName + ")");
                }
            }
        }

        public string GetCustomAttribute(Il2CppImageDefinition imageDef, int customAttributeIndex, uint token, string padding = "")
        {
            if (il2Cpp.Version < 21)
                return string.Empty;
            var attributeIndex = metadata.GetCustomAttributeIndex(imageDef, customAttributeIndex, token);
            if (attributeIndex >= 0)
            {
                var methodPointer = executor.customAttributeGenerators[attributeIndex];
                var fixedMethodPointer = il2Cpp.GetRVA(methodPointer);
                var attributeTypeRange = metadata.attributeTypeRanges[attributeIndex];
                var sb = new StringBuilder();
                for (var i = 0; i < attributeTypeRange.count; i++)
                {
                    var typeIndex = metadata.attributeTypes[attributeTypeRange.start + i];
                    sb.AppendFormat("{0}[{1}] // RVA: 0x{2:X} Offset: 0x{3:X} VA: 0x{4:X}\n",
                        padding,
                        executor.GetTypeName(il2Cpp.types[typeIndex], true, false),
                        fixedMethodPointer,
                        il2Cpp.MapVATR(methodPointer),
                        methodPointer);
                }
                return sb.ToString();
            }
            else
            {
                return string.Empty;
            }
        }

        public string GetModifiers(Il2CppMethodDefinition methodDef)
        {
            if (methodModifiers.TryGetValue(methodDef, out string str))
                return str;
            var access = methodDef.flags & METHOD_ATTRIBUTE_MEMBER_ACCESS_MASK;
            switch (access)
            {
                case METHOD_ATTRIBUTE_PRIVATE:
                    str += "private ";
                    break;
                case METHOD_ATTRIBUTE_PUBLIC:
                    str += "public ";
                    break;
                case METHOD_ATTRIBUTE_FAMILY:
                    str += "protected ";
                    break;
                case METHOD_ATTRIBUTE_ASSEM:
                case METHOD_ATTRIBUTE_FAM_AND_ASSEM:
                    str += "internal ";
                    break;
                case METHOD_ATTRIBUTE_FAM_OR_ASSEM:
                    str += "protected internal ";
                    break;
            }
            if ((methodDef.flags & METHOD_ATTRIBUTE_STATIC) != 0)
                str += "static ";
            if ((methodDef.flags & METHOD_ATTRIBUTE_ABSTRACT) != 0)
            {
                str += "abstract ";
                if ((methodDef.flags & METHOD_ATTRIBUTE_VTABLE_LAYOUT_MASK) == METHOD_ATTRIBUTE_REUSE_SLOT)
                    str += "override ";
            }
            else if ((methodDef.flags & METHOD_ATTRIBUTE_FINAL) != 0)
            {
                if ((methodDef.flags & METHOD_ATTRIBUTE_VTABLE_LAYOUT_MASK) == METHOD_ATTRIBUTE_REUSE_SLOT)
                    str += "sealed override ";
            }
            else if ((methodDef.flags & METHOD_ATTRIBUTE_VIRTUAL) != 0)
            {
                if ((methodDef.flags & METHOD_ATTRIBUTE_VTABLE_LAYOUT_MASK) == METHOD_ATTRIBUTE_NEW_SLOT)
                    str += "virtual ";
                else
                    str += "override ";
            }
            if ((methodDef.flags & METHOD_ATTRIBUTE_PINVOKE_IMPL) != 0)
                str += "extern ";
            methodModifiers.Add(methodDef, str);
            return str;
        }

        private bool TryGetDefaultValue(int typeIndex, int dataIndex, out object value)
        {
            var pointer = metadata.GetDefaultValueFromIndex(dataIndex);
            var defaultValueType = il2Cpp.types[typeIndex];
            metadata.Position = pointer;
            switch (defaultValueType.type)
            {
                case Il2CppTypeEnum.IL2CPP_TYPE_BOOLEAN:
                    value = metadata.ReadBoolean();
                    return true;
                case Il2CppTypeEnum.IL2CPP_TYPE_U1:
                    value = metadata.ReadByte();
                    return true;
                case Il2CppTypeEnum.IL2CPP_TYPE_I1:
                    value = metadata.ReadSByte();
                    return true;
                case Il2CppTypeEnum.IL2CPP_TYPE_CHAR:
                    value = BitConverter.ToChar(metadata.ReadBytes(2), 0);
                    return true;
                case Il2CppTypeEnum.IL2CPP_TYPE_U2:
                    value = metadata.ReadUInt16();
                    return true;
                case Il2CppTypeEnum.IL2CPP_TYPE_I2:
                    value = metadata.ReadInt16();
                    return true;
                case Il2CppTypeEnum.IL2CPP_TYPE_U4:
                    value = metadata.ReadUInt32();
                    return true;
                case Il2CppTypeEnum.IL2CPP_TYPE_I4:
                    value = metadata.ReadInt32();
                    return true;
                case Il2CppTypeEnum.IL2CPP_TYPE_U8:
                    value = metadata.ReadUInt64();
                    return true;
                case Il2CppTypeEnum.IL2CPP_TYPE_I8:
                    value = metadata.ReadInt64();
                    return true;
                case Il2CppTypeEnum.IL2CPP_TYPE_R4:
                    value = metadata.ReadSingle();
                    return true;
                case Il2CppTypeEnum.IL2CPP_TYPE_R8:
                    value = metadata.ReadDouble();
                    return true;
                case Il2CppTypeEnum.IL2CPP_TYPE_STRING:
                    var len = metadata.ReadInt32();
                    value = metadata.ReadString(len);
                    return true;
                default:
                    value = pointer;
                    return false;
            }
        }
    }
}
