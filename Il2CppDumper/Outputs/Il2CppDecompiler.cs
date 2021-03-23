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
        public static Dictionary<string, string> obfu_map = new Dictionary<string, string> {
            { "OGHLFADACAG", "Rpc" }
        };

        public static string[] klasses = new string[] {
            "GameData",
            "PlayerControl",
            "Rpc",
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

        private string deobfu(string name)
        {
            if (obfu_map.ContainsKey(name)) return obfu_map[name];

            return name;
        }

    private string clear_name(string name)
        {
            name = name.Replace("<>", "_");

            return name;
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
                try
                {
                    var imageName = metadata.GetStringFromIndex(imageDef.nameIndex);
                    var typeEnd = imageDef.typeStart + imageDef.typeCount;
                    for (int typeDefIndex = imageDef.typeStart; typeDefIndex < typeEnd; typeDefIndex++)
                    { 
                        var types = new HashSet<Il2CppType>();
                        var includes = new HashSet<string>();

                        var typeDef = metadata.typeDefs[typeDefIndex];
                        var typeName = executor.GetTypeDefName(typeDef, false, true);
                        var extends = new List<string>();
                        var namespaze = metadata.GetStringFromIndex(typeDef.namespaceIndex);

                        if (typeName.Contains("<")) continue;

                        typeName = typeName.Replace("::", "_");
                        typeName = deobfu(typeName);

                        // skip
                        if (!klasses.Contains(typeName)) continue;

                        // outputs
                        string headers = "#pragma once\n#include <ark/class.hpp>\n";
                        string statics_def = "";
                        string methods_rva = "";
                        string fields = "";
                        string methods = "";
                        string klass = "";
                        string klass_inerhit = "";

                        // file
                        string file_path = "au/" + namespaze + "/";
                        file_path += typeName + ".hpp";
                        Console.WriteLine("Make file " + typeName + " @ " + file_path);

                        Directory.CreateDirectory(Path.GetDirectoryName(file_path));
                        var writer = new StreamWriter(new FileStream(outputDir + file_path, FileMode.Create), new UTF8Encoding(false));

                        if (typeDef.parentIndex >= 0)
                        {
                            var parent = il2Cpp.types[typeDef.parentIndex];
                            var parentName = executor.GetTypeName(parent, true, false, false);
                            if (!typeDef.IsValueType && !typeDef.IsEnum && parentName != "object")
                            {
                                extends.Add(parentName);
                                types.Add(parent);
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


                        //if ((typeDef.flags & TYPE_ATTRIBUTE_ABSTRACT) != 0 && (typeDef.flags & TYPE_ATTRIBUTE_SEALED) != 0)
                        //writer.Write("static ");
                        //else if ((typeDef.flags & TYPE_ATTRIBUTE_INTERFACE) == 0 && (typeDef.flags & TYPE_ATTRIBUTE_ABSTRACT) != 0)
                        //writer.Write("abstract ");
                        if (!typeDef.IsValueType && !typeDef.IsEnum && (typeDef.flags & TYPE_ATTRIBUTE_SEALED) != 0)
                            writer.Write("final ");
                        //if ((typeDef.flags & TYPE_ATTRIBUTE_INTERFACE) != 0)
                        //writer.Write("interface ");
                        else if (typeDef.IsEnum)
                            klass += "enum ";
                        else if (typeDef.IsValueType)
                            klass += "struct ";
                        else
                            klass += "struct ";

                        klass += typeName;

                        //if ((typeDef.flags & TYPE_ATTRIBUTE_NESTED_PUBLIC) != 0)
                            //klass += "NESTD";


                        if (!typeDef.IsEnum)
                        {
                            if (extends.Count > 0)
                                klass_inerhit = $" : ark::meta<{typeName}, {string.Join(", ", extends)}>";
                            else klass_inerhit = $" : ark::meta<{typeName}>";
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
                                var field_name = metadata.GetStringFromIndex(fieldDef.nameIndex);


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
                                            statics_def += "\n" + return_type + " " + executor.GetTypeDefName(typeDef, true, true) + "::" + field_name + "() { return statics()->" + field_name + "();";
                                        }
                                    }
                                    
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
                                                fields += $"{value}";
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
                            methods += "\n    // Methods\n\n";
                            var methodEnd = typeDef.methodStart + typeDef.method_count;
                            for (var i = typeDef.methodStart; i < methodEnd; ++i)
                            {
                                var methodDef = metadata.methodDefs[i];
                                ulong fixedMethodPointer = 0;

                                var methodPointer = il2Cpp.GetMethodPointer(imageName, methodDef);
                                if (methodPointer > 0)
                                {
                                    fixedMethodPointer = il2Cpp.GetRVA(methodPointer);
                                }

                                methods += "    ";
                                var methodReturnType = il2Cpp.types[methodDef.returnType];
                                types.Add(methodReturnType);

                                var methodName = metadata.GetStringFromIndex(methodDef.nameIndex);
                                methodName = methodName.Replace(".ctor", "ctor");
                                methodName = methodName.Replace(".cctor", "cctor");

                                if (methodDef.genericContainerIndex >= 0)
                                {
                                    var genericContainer = metadata.genericContainers[methodDef.genericContainerIndex];
                                    methodName += executor.GetGenericContainerParams(genericContainer);
                                }
                                if (methodReturnType.byref == 1)
                                {
                                    methods += "ref ";
                                }

                                methods += $"{executor.GetTypeName(methodReturnType, true, false)} {methodName}(";

                                // rva
                                methods_rva += $"\n    method_rva({typeName}::{methodName}, 0x{fixedMethodPointer:X})";

                                var parameterStrs = new List<string>();
                                var parameter_names = new List<string>();
 
                                for (var j = 0; j < methodDef.parameterCount; ++j)
                                {
                                    var parameterStr = "";
                                    var parameterDef = metadata.parameterDefs[methodDef.parameterStart + j];
                                    var parameterName = metadata.GetStringFromIndex(parameterDef.nameIndex);
                                    var parameterType = il2Cpp.types[parameterDef.typeIndex];
                                    types.Add(parameterType);
                                    var parameterTypeName = executor.GetTypeName(parameterType, true, false, true);

                                    parameter_names.Add(parameterName);

                                    if (parameterType.byref == 1)
                                    {
                                        if ((parameterType.attrs & PARAM_ATTRIBUTE_OUT) != 0 && (parameterType.attrs & PARAM_ATTRIBUTE_IN) == 0)
                                        {
                                            parameterStr += "out ";
                                        }
                                        else if ((parameterType.attrs & PARAM_ATTRIBUTE_OUT) == 0 && (parameterType.attrs & PARAM_ATTRIBUTE_IN) != 0)
                                        {
                                            parameterStr += "in ";
                                        }
                                        else
                                        {
                                            parameterStr += "ref ";
                                        }
                                    }

                                    parameterStr += $"{parameterTypeName} {parameterName}";
                                    if (metadata.GetParameterDefaultValueFromIndex(methodDef.parameterStart + j, out var parameterDefault) && parameterDefault.dataIndex != -1)
                                    {
                                        if (TryGetDefaultValue(parameterDefault.typeIndex, parameterDefault.dataIndex, out var value))
                                        {
                                            parameterStr += " = ";
                                            if (value is string str)
                                            {
                                                parameterStr += $"\"{str.ToEscapedString()}\"";
                                            }
                                            else if (value is char c)
                                            {
                                                var v = (int)c;
                                                parameterStr += $"'\\x{v:x}'";
                                            }
                                            else if (value != null)
                                            {
                                                parameterStr += $"{value}";
                                            }
                                        }
                                        else
                                        {
                                            parameterStr += $" /*Metadata offset 0x{value:X}*/";
                                        }
                                    }
                                    parameterStrs.Add(parameterStr);
                                }

                                var methodCall = $"return method_call({methodName}, {string.Join(", ", parameter_names)}); ";
                                if (parameter_names.Count() == 0) 
                                    methodCall = $"return method_call({methodName}); ";

                                methods += string.Join(", ", parameterStrs);
                                methods += ") { " + methodCall + "} ";
                                methods += $" // 0x{fixedMethodPointer:X} // ";
                                methods += GetModifiers(methodDef);
                                methods += "\n";
                            }
                        }

                        // headers
                        foreach(Il2CppType type in types)
                        {
                            if ((int)type.type > 0x0d && (int)type.type != 0x1c)
                            {
                                includes.Add($"#include <{include_path(type)}.hpp>\n");
                            }
                        }
                        foreach (string include in includes)
                        {
                            headers += include;
                        }


                        //
                        writer.Write("//" + file_path + "\n");
                        writer.Write(headers);
                        writer.Write("\nnamespace " + metadata.GetStringFromIndex(typeDef.namespaceIndex) + " {\n");
                        writer.Write(klass + klass_inerhit + "\n{\n");

                        writer.Write(fields);
                        writer.Write(methods);

                        writer.Write("\n};\n");
                        writer.Write("\n}\n\n");

                        writer.Write(statics_def);

                        writer.Write("\n\nnamespace ark::method_info \n{");
                        writer.Write(methods_rva);
                        writer.Write("\n} // ark::method_info");

                        writer.Close();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("ERROR: Some errors in dumping " + e.Message);
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
