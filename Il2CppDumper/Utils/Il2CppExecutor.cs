using System;
using System.Collections.Generic;
using System.Linq;

namespace Il2CppDumper
{
    public class Il2CppExecutor
    {
        public Metadata metadata;
        public Il2Cpp il2Cpp;
        private static readonly Dictionary<int, string> TypeString = new Dictionary<int, string>
        {
            {1,"void"},
            {2,"bool"},
            {3,"char"},
            {4,"int8_t"},
            {5,"uint8_t"},
            {6,"int16_t"},
            {7,"uint16_t"},
            {8,"int"},
            {9,"unsigned int"},
            {10,"long"},
            {11,"unsigned long"},
            {12,"float"},
            {13,"double"},
            {14,"cs::string*"},
            {22,"TypedReference"},
            {24,"intptr_t"},
            {25,"uintptr_t"},
            {28,"object"},
        };
        public ulong[] customAttributeGenerators;

        public Il2CppExecutor(Metadata metadata, Il2Cpp il2Cpp)
        {
            this.metadata = metadata;
            this.il2Cpp = il2Cpp;

            if (il2Cpp.Version >= 27)
            {
                customAttributeGenerators = new ulong[metadata.imageDefs.Sum(x => x.customAttributeCount)];
                foreach (var imageDef in metadata.imageDefs)
                {
                    var imageDefName = metadata.GetStringFromIndex(imageDef.nameIndex);
                    var codeGenModule = il2Cpp.codeGenModules[imageDefName];
                    var pointers = il2Cpp.ReadClassArray<ulong>(il2Cpp.MapVATR(codeGenModule.customAttributeCacheGenerator), imageDef.customAttributeCount);
                    pointers.CopyTo(customAttributeGenerators, imageDef.customAttributeStart);
                }
            }
            else
            {
                customAttributeGenerators = il2Cpp.customAttributeGenerators;
            }
        }

        public string GetTypeName(Il2CppType il2CppType, bool addNamespace, bool is_nested, bool is_pointer = true)
        {
            /*
            if (il2CppType.data.klassIndex)
            var typeDef = metadata.typeDefs[typeDefIndex];
            var typeName = executor.GetTypeDefName(typeDef, false, true);*/

            switch (il2CppType.type)
            {
                case Il2CppTypeEnum.IL2CPP_TYPE_ARRAY:
                    {
                        var arrayType = il2Cpp.MapVATR<Il2CppArrayType>(il2CppType.data.array);
                        var elementType = il2Cpp.GetIl2CppType(arrayType.etype);
                        return $"{GetTypeName(elementType, addNamespace, false)}[{new string(',', arrayType.rank - 1)}]";
                    }
                case Il2CppTypeEnum.IL2CPP_TYPE_SZARRAY:
                    {
                        var elementType = il2Cpp.GetIl2CppType(il2CppType.data.type);
                        return $"cs::array<{GetTypeName(elementType, addNamespace, false, false)}>*";
                    }
                case Il2CppTypeEnum.IL2CPP_TYPE_PTR:
                    {
                        var oriType = il2Cpp.GetIl2CppType(il2CppType.data.type);
                        return $"{GetTypeName(oriType, addNamespace, false)}*";
                    }
                case Il2CppTypeEnum.IL2CPP_TYPE_VAR:
                case Il2CppTypeEnum.IL2CPP_TYPE_MVAR:
                    {
                        var param = GetGenericParameteFromIl2CppType(il2CppType);
                        return metadata.GetStringFromIndex(param.nameIndex);
                    }
                case Il2CppTypeEnum.IL2CPP_TYPE_CLASS:
                case Il2CppTypeEnum.IL2CPP_TYPE_VALUETYPE:
                case Il2CppTypeEnum.IL2CPP_TYPE_GENERICINST:
                    {
                        string str = string.Empty;
                        string str_params = string.Empty;
                        string str_pointer = "*";
                        if (!is_pointer) str_pointer = "";

                        Il2CppTypeDefinition typeDef;
                        Il2CppGenericClass genericClass = null;
                        if (il2CppType.type == Il2CppTypeEnum.IL2CPP_TYPE_GENERICINST)
                        {
                            genericClass = il2Cpp.MapVATR<Il2CppGenericClass>(il2CppType.data.generic_class);
                            typeDef = GetGenericClassTypeDefinition(genericClass);
                        }
                        else
                        {
                            typeDef = GetTypeDefinitionFromIl2CppType(il2CppType);
                        }
                        if (typeDef.declaringTypeIndex != -1)
                        {
                            // nested
                            str += GetTypeName(il2Cpp.types[typeDef.declaringTypeIndex], addNamespace, true, true);
                            str += "_";
                        }
                        else if (addNamespace)
                        {
                            var @namespace = metadata.GetStringFromIndex(typeDef.namespaceIndex);
                            if (@namespace != "")
                            {
                                @namespace = @namespace.Replace(".", "::");
                                str += @namespace + "::";
                            }
                        }

                        var typeName = metadata.GetStringFromIndex(typeDef.nameIndex);

                        var index = typeName.IndexOf("`");
                        if (index != -1)
                        {
                            str += typeName.Substring(0, index);
                        }
                        else
                        {
                            str += typeName;
                        }

                        if (is_nested)
                        {
                            return str;
                        }

                        if (genericClass != null)
                        {
                            var genericInst = il2Cpp.MapVATR<Il2CppGenericInst>(genericClass.context.class_inst);
                            str_params += GetGenericInstParams(genericInst);
                        }
                        else if (typeDef.genericContainerIndex >= 0)
                        {
                            var genericContainer = metadata.genericContainers[typeDef.genericContainerIndex];
                            str_params += GetGenericContainerParams(genericContainer);
                        }

                        if (il2CppType.type == Il2CppTypeEnum.IL2CPP_TYPE_VALUETYPE)
                            str_pointer = "";

                        str = str.Replace("<", "_");
                        str = str.Replace(">", "_");
                        str = Il2CppDecompiler.deobfu(str);
                        return str + str_params + str_pointer;
                    }
                default:
                    return TypeString[(int)il2CppType.type];
            }
        }

        public string GetTypeDefName(Il2CppTypeDefinition typeDef, bool addNamespace, bool genericParameter, bool generic_decl = false)
        {
            var prefix = string.Empty;
            if (typeDef.declaringTypeIndex != -1)
            {
                prefix = GetTypeName(il2Cpp.types[typeDef.declaringTypeIndex], addNamespace, true) + "_";
            }
            else if (addNamespace)
            {
                var @namespace = metadata.GetStringFromIndex(typeDef.namespaceIndex);
                @namespace = @namespace.Replace(".", "::");
                if (@namespace != "")
                {
                    prefix = @namespace + "::";
                }
            }
            var typeName = metadata.GetStringFromIndex(typeDef.nameIndex);
            if (typeDef.genericContainerIndex >= 0)
            {
                var index = typeName.IndexOf("`");
                if (index != -1)
                {
                    typeName = typeName.Substring(0, index);
                    typeName = typeName.Replace("<", "_");
                    typeName = typeName.Replace(">", "_");
                }
                if (genericParameter)
                {
                    var genericContainer = metadata.genericContainers[typeDef.genericContainerIndex];
                    typeName += GetGenericContainerParams(genericContainer, generic_decl);
                    if (generic_decl) return "template " + GetGenericContainerParams(genericContainer, generic_decl);
                }
            }
            else
            {
                typeName = typeName.Replace("<", "_");
                typeName = typeName.Replace(">", "_");
            }

            //typeName = Il2CppDecompiler.deobfu(typeName);

            return Il2CppDecompiler.deobfu(prefix + typeName);
        }

        public string GetGenericInstParams(Il2CppGenericInst genericInst)
        {
            var genericParameterNames = new List<string>();
            var pointers = il2Cpp.MapVATR<ulong>(genericInst.type_argv, genericInst.type_argc);
            for (int i = 0; i < genericInst.type_argc; i++)
            {
                var il2CppType = il2Cpp.GetIl2CppType(pointers[i]);
                genericParameterNames.Add(Il2CppDecompiler.deobfu(GetTypeName(il2CppType, true, false, false)));
            }
            return $"<{string.Join(", ", genericParameterNames)}>";
        }

        public string GetGenericContainerParams(Il2CppGenericContainer genericContainer, bool generic_decl = false)
        {
            string str_class = "";
            if (generic_decl) str_class = "class ";
            var genericParameterNames = new List<string>();
            for (int i = 0; i < genericContainer.type_argc; i++)
            {
                var genericParameterIndex = genericContainer.genericParameterStart + i;
                var genericParameter = metadata.genericParameters[genericParameterIndex];
                //Il2CppDecompiler.types.Add(genericParameter.GetType());
                var parameterName = metadata.GetStringFromIndex(genericParameter.nameIndex);
                parameterName = parameterName.Replace(".", "_");
                genericParameterNames.Add(str_class + parameterName);
            }
            return $"<{string.Join(", ", genericParameterNames)}>";
        }

        public (string, string) GetMethodSpecName(Il2CppMethodSpec methodSpec, bool addNamespace = false)
        {
            var methodDef = metadata.methodDefs[methodSpec.methodDefinitionIndex];
            var typeDef = metadata.typeDefs[methodDef.declaringType];
            var typeName = GetTypeDefName(typeDef, addNamespace, false);
            if (methodSpec.classIndexIndex != -1)
            {
                var classInst = il2Cpp.genericInsts[methodSpec.classIndexIndex];
                typeName += GetGenericInstParams(classInst);
            }
            var methodName = metadata.GetStringFromIndex(methodDef.nameIndex);
            if (methodSpec.methodIndexIndex != -1)
            {
                var methodInst = il2Cpp.genericInsts[methodSpec.methodIndexIndex];
                methodName += GetGenericInstParams(methodInst);
            }
            return (typeName, methodName);
        }

        public Il2CppGenericContext GetMethodSpecGenericContext(Il2CppMethodSpec methodSpec)
        {
            var classInstPointer = 0ul;
            var methodInstPointer = 0ul;
            if (methodSpec.classIndexIndex != -1)
            {
                classInstPointer = il2Cpp.genericInstPointers[methodSpec.classIndexIndex];
            }
            if (methodSpec.methodIndexIndex != -1)
            {
                methodInstPointer = il2Cpp.genericInstPointers[methodSpec.methodIndexIndex];
            }
            return new Il2CppGenericContext { class_inst = classInstPointer, method_inst = methodInstPointer };
        }

        public Il2CppRGCTXDefinition[] GetRGCTXDefinition(string imageName, Il2CppTypeDefinition typeDef)
        {
            Il2CppRGCTXDefinition[] collection = null;
            if (il2Cpp.Version >= 24.2)
            {
                il2Cpp.rgctxsDictionary[imageName].TryGetValue(typeDef.token, out collection);
            }
            else
            {
                if (typeDef.rgctxCount > 0)
                {
                    collection = new Il2CppRGCTXDefinition[typeDef.rgctxCount];
                    Array.Copy(metadata.rgctxEntries, typeDef.rgctxStartIndex, collection, 0, typeDef.rgctxCount);
                }
            }
            return collection;
        }

        public Il2CppRGCTXDefinition[] GetRGCTXDefinition(string imageName, Il2CppMethodDefinition methodDef)
        {
            Il2CppRGCTXDefinition[] collection = null;
            if (il2Cpp.Version >= 24.2)
            {
                il2Cpp.rgctxsDictionary[imageName].TryGetValue(methodDef.token, out collection);
            }
            else
            {
                if (methodDef.rgctxCount > 0)
                {
                    collection = new Il2CppRGCTXDefinition[methodDef.rgctxCount];
                    Array.Copy(metadata.rgctxEntries, methodDef.rgctxStartIndex, collection, 0, methodDef.rgctxCount);
                }
            }
            return collection;
        }

        public Il2CppTypeDefinition GetGenericClassTypeDefinition(Il2CppGenericClass genericClass)
        {
            if (il2Cpp.Version >= 27)
            {
                var il2CppType = il2Cpp.GetIl2CppType(genericClass.type);
                return GetTypeDefinitionFromIl2CppType(il2CppType);
            }
            if (genericClass.typeDefinitionIndex == 4294967295 || genericClass.typeDefinitionIndex == -1)
            {
                return null;
            }
            return metadata.typeDefs[genericClass.typeDefinitionIndex];
        }

        public Il2CppTypeDefinition GetTypeDefinitionFromIl2CppType(Il2CppType il2CppType)
        {
            if (il2Cpp.Version >= 27 && il2Cpp is ElfBase elf && elf.IsDumped)
            {
                var offset = il2CppType.data.typeHandle - metadata.Address - metadata.header.typeDefinitionsOffset;
                var index = offset / (ulong)metadata.SizeOf(typeof(Il2CppTypeDefinition));
                return metadata.typeDefs[index];
            }
            else
            {
                return metadata.typeDefs[il2CppType.data.klassIndex];
            }
        }

        public Il2CppGenericParameter GetGenericParameteFromIl2CppType(Il2CppType il2CppType)
        {
            if (il2Cpp.Version >= 27 && il2Cpp is ElfBase elf && elf.IsDumped)
            {
                var offset = il2CppType.data.genericParameterHandle - metadata.Address - metadata.header.genericParametersOffset;
                var index = offset / (ulong)metadata.SizeOf(typeof(Il2CppGenericParameter));
                return metadata.genericParameters[index];
            }
            else
            {
                return metadata.genericParameters[il2CppType.data.genericParameterIndex];
            }
        }
    }
}
