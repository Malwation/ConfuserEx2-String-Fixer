using System;
using System.IO;
using System.Reflection;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;

namespace cex2string
{
    internal class Program
    {

        public static Assembly asm;
        public static ModuleDefMD module;
        static void Main(string[] args)
        {
            asm = Assembly.LoadFrom(@"path");
            module = StringFixer(ModuleDefMD.Load(@"path"));
            SaveAssembly(module, "_fixed");

            Console.ReadKey();
        }

        static ModuleDefMD StringFixer(ModuleDefMD module)
        {
            int num = 0;
            
            foreach (TypeDef type in module.Types)
            {
                if (!type.HasMethods) continue;

                foreach (MethodDef method in type.Methods)
                {
                    if (!method.HasBody) continue;
                    if (method.Body.Instructions.Count <= 4) continue;

                    for (int i = 0; i < method.Body.Instructions.Count; i++)
                    {
                        Module manifestModule = asm.ManifestModule;
                        if (method.Body.Instructions[i].OpCode == OpCodes.Call && 
                            method.Body.Instructions[i].Operand.ToString().Contains("tring>") && 
                            method.Body.Instructions[i].Operand is MethodSpec && 
                            method.Body.Instructions[i - 1].IsLdcI4())
                        {
                            MethodSpec methodSpec = method.Body.Instructions[i].Operand as MethodSpec;
                            int ldcI4Value = (int)method.Body.Instructions[i - 1].GetLdcI4Value();
                            string text = (string)manifestModule.ResolveMethod(methodSpec.MDToken.ToInt32()).Invoke(null, new object[] { ldcI4Value });
                            method.Body.Instructions[i].OpCode = OpCodes.Nop;
                            method.Body.Instructions[i - 1].OpCode = OpCodes.Ldstr;
                            method.Body.Instructions[i - 1].Operand = text;
                            num++;
                            
                        }
                    }
                }
            }
            Console.WriteLine(string.Format("Decrypted {0} strings", num));
            return module;
        }

        


        static void SaveAssembly(ModuleDefMD module, string ext)
        {
            var writerOptions = new NativeModuleWriterOptions(module, true);
            writerOptions.Logger = DummyLogger.NoThrowInstance;
            writerOptions.MetadataOptions.Flags = (MetadataFlags.PreserveTypeRefRids | MetadataFlags.PreserveTypeDefRids | MetadataFlags.PreserveFieldRids | MetadataFlags.PreserveMethodRids | MetadataFlags.PreserveParamRids | MetadataFlags.PreserveMemberRefRids | MetadataFlags.PreserveStandAloneSigRids | MetadataFlags.PreserveEventRids | MetadataFlags.PreservePropertyRids | MetadataFlags.PreserveTypeSpecRids | MetadataFlags.PreserveMethodSpecRids | MetadataFlags.PreserveStringsOffsets | MetadataFlags.PreserveUSOffsets | MetadataFlags.PreserveBlobOffsets | MetadataFlags.PreserveAll | MetadataFlags.AlwaysCreateGuidHeap | MetadataFlags.PreserveExtraSignatureData | MetadataFlags.KeepOldMaxStack);
            module.NativeWrite(Path.GetDirectoryName(module.Location) + @"\" + Path.GetFileNameWithoutExtension(module.Location) + ext + ".exe", writerOptions);
        }

  
}
}
