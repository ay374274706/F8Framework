using System.Collections.Generic;
using System.Reflection;
using Microsoft.CSharp;
using System.CodeDom.Compiler;
using System.IO;
using System;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using Excel;
using UnityEditor.Compilation;
using Assembly = System.Reflection.Assembly;
#if UNITY_WEBGL
using LitJson;
#else
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
#endif

namespace F8Framework.Core.EditorGenerate
{
    public class ExcelDataTool : ScriptableObject
    {
        public const string CODE_NAMESPACE = "F8Framework.F8ExcelDataClass"; //由表生成的数据类型均在此命名空间内

        public const string
            BinDataFolder = "/AssetBundles/Config/BinConfigData"; //序列化的数据文件都会放在此文件夹内,此文件夹位于Resources文件夹下用于读取数据
        public const string DataManagerFolder = "/F8Framework/ConfigData/F8DataManager"; //Data代码路径
        public const string DataManagerName = "F8DataManager.cs"; //Data代码脚本名
        public const string ExcelPath = "/StreamingAssets/config"; //需要导表的目录
        public const string DLLFolder = "/F8Framework/ConfigData"; //存放dll目录
        public const string FileIndexFile = "config/fileindex.txt"; //fileindex文件目录
        private static Dictionary<string, string> codeList; //存放所有生成的类的代码

        private static Dictionary<string, List<ConfigData[]>> dataDict; //存放所有数据表内的数据，key：类名  value：数据

        // 使用StringBuilder来优化字符串的重复构造
        private static StringBuilder FileIndex = new StringBuilder();
        
        private static string GetScriptPath()
        {
            MonoScript monoScript = MonoScript.FromScriptableObject(CreateInstance<ExcelDataTool>());

            // 获取脚本在 Assets 中的相对路径
            string scriptRelativePath = AssetDatabase.GetAssetPath(monoScript);

            // 获取绝对路径并规范化
            string scriptPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", scriptRelativePath));

            return scriptPath;
        }
        
        private static void CreateAsmdefFile()
        {
            // 创建.asmdef文件的路径
            string asmrefPath = Application.dataPath + DLLFolder + "/" + CODE_NAMESPACE + ".asmdef";
            
            FileTools.CheckFileAndCreateDirWhenNeeded(asmrefPath);
            // 创建一个新的.asmdef文件
            string asmdefContent = @"{
    ""name"": ""F8Framework.F8ExcelDataClass"",
    ""references"": [
        ""F8Framework.Core"",
        ""LitJson""
    ],
    ""includePlatforms"": [],
    ""excludePlatforms"": [],
    ""allowUnsafeCode"": false,
    ""overrideReferences"": false,
    ""precompiledReferences"": [],
    ""autoReferenced"": true,
    ""defineConstraints"": [],
    ""versionDefines"": [],
    ""noEngineReferences"": false
}";

            // 将内容写入.asmdef文件
            FileTools.SafeWriteAllText(asmrefPath, asmdefContent);
        }
        
        public static void LoadAllExcelData()
        {
            string INPUT_PATH = Application.dataPath + ExcelPath;

            FileTools.CheckDirAndCreateWhenNeeded(INPUT_PATH);

            var files = Directory.GetFiles(INPUT_PATH, "*.*", SearchOption.AllDirectories)
                .Where(s => s.EndsWith(".xls") || s.EndsWith(".xlsx")).ToArray();
            if (files == null || files.Length == 0)
            {
                FileTools.SafeCopyFile(
                    FileTools.FormatToUnityPath(FileTools.TruncatePath(GetScriptPath(), 3)) +
                    "/Tests/ExcelTool/StreamingAssets_config/Demo工作表.xlsx",
                    Application.streamingAssetsPath + "/config/Demo工作表.xlsx");
                FileTools.SafeCopyFile(
                    FileTools.FormatToUnityPath(FileTools.TruncatePath(GetScriptPath(), 3)) +
                    "/Tests/Localization/StreamingAssets_config/本地化.xlsx",
                    Application.streamingAssetsPath + "/config/本地化.xlsx");
                files = Directory.GetFiles(INPUT_PATH, "*.*", SearchOption.AllDirectories)
                    .Where(s => s.EndsWith(".xls") || s.EndsWith(".xlsx")).ToArray();
                LogF8.LogError("暂无可以导入的数据表！自动为你创建：【Demo工作表.xlsx / 本地化.xlsx】两个表格！" + ExcelPath + " 目录");
            }
            
            // string F8DataManagerPath = FileTools.FormatToUnityPath(FileTools.TruncatePath(GetScriptPath(), 3)) + "/ConfigData/F8DataManager";
            // FileTools.SafeClearDir(F8DataManagerPath);
            // FileTools.CheckDirAndCreateWhenNeeded(F8DataManagerPath);
            // LogF8.Log(F8DataManagerPath);
            // string F8ExcelDataClassPath = FileTools.FormatToUnityPath(FileTools.TruncatePath(GetScriptPath(), 3)) + "/ConfigData/F8ExcelDataClass";
            // FileTools.SafeClearDir(F8ExcelDataClassPath);
            // FileTools.CheckDirAndCreateWhenNeeded(F8ExcelDataClassPath);
            // string F8ExcelDataClassPathDLL = FileTools.FormatToUnityPath(FileTools.TruncatePath(GetScriptPath(), 3)) + "/ConfigData/" + CODE_NAMESPACE + ".asmdef";
            // FileTools.SafeDeleteFile(F8ExcelDataClassPathDLL);
            // FileTools.SafeDeleteFile(F8ExcelDataClassPathDLL + ".meta");
            // FileTools.SafeDeleteFile(Application.dataPath + DataManagerFolder + "/F8DataManager.asmref");
            CreateAsmdefFile();
            AssetDatabase.Refresh();
            
            if (codeList == null)
            {
                codeList = new Dictionary<string, string>();
            }
            else
            {
                codeList.Clear();
            }

            if (dataDict == null)
            {
                dataDict = new Dictionary<string, List<ConfigData[]>>();
            }
            else
            {
                dataDict.Clear();
            }
            
            FileIndex.Clear();
            FileTools.SafeDeleteFile(URLSetting.CS_STREAMINGASSETS_URL + FileIndexFile);
            FileTools.SafeDeleteFile(URLSetting.CS_STREAMINGASSETS_URL + FileIndexFile + ".meta");
            AssetDatabase.Refresh();
            FileTools.CheckFileAndCreateDirWhenNeeded(URLSetting.CS_STREAMINGASSETS_URL + FileIndexFile);
            foreach (string item in files)
            {
                if (Path.GetFileName(item).StartsWith("~$"))
                {
                    continue;
                }
                GetExcelData(item);
                OnLogCallBack(item.Substring(item.LastIndexOf('\\') + 1));
            }

            if (codeList.Count == 0)
            {
                EditorUtility.DisplayDialog("注意！！！", "\n暂无可以导入的数据表！", "确定");
                throw new Exception("暂无可以导入的数据表！");
            }
            //编译代码,生成包含所有数据表内数据类型的dll
            GenerateCodeFiles(codeList);
            // ScriptGenerator.CreateDataManager(codeList);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            // 等待脚本编译完成
            CompilationPipeline.compilationFinished += (object s) =>
            {
         

                Util.Assembly.SetDomainAssemblies(AppDomain.CurrentDomain.GetAssemblies());

                ScriptGenerator.CreateDataManager(codeList);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                EditorPrefs.SetBool("compilationFinished", true);
            };
        }
     

        private static void OnLogCallBack(string condition)
        {
            FileIndex.Append(condition);
            if (FileIndex.Length <= 0) return;
            using (var sw = File.AppendText(URLSetting.CS_STREAMINGASSETS_URL + FileIndexFile))
            {
                sw.WriteLine(FileIndex.ToString());
            }

            FileIndex.Remove(0, FileIndex.Length);
        }

        //数据表内每一格数据
        class ConfigData
        {
            public string Type; //数据类型
            public string Name; //字段名
            public string Data; //数据值
        }

        private static void GetExcelData(string inputPath)
        {
            FileStream stream = null;
            try
            {
                stream = File.Open(inputPath, FileMode.Open, FileAccess.Read,FileShare.ReadWrite);
            }
            catch
            {
                EditorUtility.DisplayDialog("注意！！！", "\n请关闭 " + inputPath + " 后再导表！", "确定");
                throw new Exception("请关闭 " + inputPath + " 后再导表！");
            }

            IExcelDataReader excelReader = null;
            if (inputPath.EndsWith(".xls")) excelReader = ExcelReaderFactory.CreateBinaryReader(stream);
            else if (inputPath.EndsWith(".xlsx")) excelReader = ExcelReaderFactory.CreateOpenXmlReader(stream);
            if (!excelReader.IsValid)
            {
                throw new Exception("无法读取的文件:  " + inputPath);
            }
            else
            {
                do // 读取所有的sheet
                {
                    // sheet name
                    string className = excelReader.Name;
                    string[] types = null; //数据类型
                    string[] names = null; //字段名
                    List<ConfigData[]> dataList = new List<ConfigData[]>();
                    int index = 1;

                    //开始读取
                    while (excelReader.Read())
                    {
                        //这里读取的是每一行的数据
                        string[] datas = new string[excelReader.FieldCount];
                        for (int j = 0; j < excelReader.FieldCount; ++j)
                        {
                            datas[j] = excelReader.GetString(j);
                        }

                        //空行不处理
                        if (datas.Length == 0 || string.IsNullOrEmpty(datas[0]))
                        {
                            ++index;
                            continue;
                        }

                        //第1行表示类型
                        if (index == 1) types = datas;
                        //第2行表示变量名
                        else if (index == 2) names = datas;
                        //后面的表示数据
                        else if (index > 2)
                        {
                            if (types == null || names == null || datas == null){
                                throw new Exception("数据错误！["+ className +"]配置表！第" + index + "行" + inputPath);
                            }
                            //把读取的数据和数据类型,名称保存起来,后面用来动态生成类
                            List<ConfigData> configDataList = new List<ConfigData>();
                            for (int j = 0; j < datas.Length; ++j)
                            {
                                ConfigData data = new ConfigData();
                                data.Type = types[j];
                                data.Name = names[j];
                                data.Data = datas[j];
                                if (string.IsNullOrEmpty(data.Type) || string.IsNullOrEmpty(data.Data))
                                    continue; //空的数据不处理
                                configDataList.Add(data);
                            }

                            dataList.Add(configDataList.ToArray());
                        }

                        ++index;
                    }

                    if (string.IsNullOrEmpty(className))
                    {
                        throw new Exception("空的类名（excel页签名）, 路径:  " + inputPath);
                    }

                    if (names != null && types != null)
                    {
                        //根据刚才的数据来生成C#脚本
                        ScriptGenerator generator = new ScriptGenerator(inputPath, className, names, types);
                        //所有生成的类的代码最终保存在这
                        if (codeList.ContainsKey(className))
                        {
                            throw new Exception("类名重复: " + className + " ,路径:  " + inputPath);
                        }
                        codeList.Add(className, generator.Generate());
                        if (dataDict.ContainsKey(className))
                        {
                            throw new Exception("类名重复: " + className + " ,路径:  " + inputPath);
                        }

                        dataDict.Add(className, dataList);
                    }
                } while (excelReader.NextResult()); //excelReader.NextResult() Excel表下一个sheet页有没有数据
            }

            stream.Dispose();
            stream.Close();
        }

        // 生成代码文件
        public static void GenerateCodeFiles(Dictionary<string, string> codeList)
        {
            string path = Application.dataPath + DLLFolder + "/F8ExcelDataClass";
            FileTools.SafeClearDir(path);// 删除旧文件

            // 将每个脚本写入独立的 .cs 文件
            foreach (var kvp in codeList)
            {
                string filePath = $"{path}/{kvp.Key}.cs";
                File.WriteAllText(filePath, kvp.Value);
                LogF8.LogConfig($"已生成代码 " + path + "/<color=#FF9E59>" + kvp.Key + ".cs</color>");
            }
        }
        
     
    }
}