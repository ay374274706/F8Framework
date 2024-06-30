using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace F8Framework.Core.EditorGenerate
{
    public class F8Helper
    {
        [MenuItem("GenerateCode/Excel导表-生成C#定义文件")]
        public static void LoadAllExcelData()
        {
            ExcelDataTool.LoadAllExcelData();
        }

    }
}