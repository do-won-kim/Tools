
#if UNITY_EDITOR


using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;

public class Editor_Profiler : EditorWindow
{
    public struct ProfileData
    {
        public float cpuRenderingTime;
        public float fps;
        public float srpObjectCall;
        public float srpShadowCall;
        public float stdObjectCall;
        public float stdShadowCall;
        public float drawCall;
        public float shadowCasters;
        public float verts;
        public int tri;

        public Vector3 cameraPosition;
        public Vector3 cameraRotation;
    }

    #region SRP Profiler

    static readonly float kAverageStatDuration = 1.0f;
    static float m_AccDeltaTime = 0f;
    static int m_frameCount;

    internal class RecorderEntry
    {
        public string name;
        public string oldName;
        public int callCount;
        public float accTime;
        public Recorder recorder;
    };

    enum SRPBMarkers
    {
        kStdRenderDraw,
        kStdShadowDraw,
        kSRPBRenderDraw,
        kSRPBShadowDraw,
        kRenderThreadIdle,
        kStdRenderApplyShader,
        kStdShadowApplyShader,
        kSRPBRenderApplyShader,
        kSRPBShadowApplyShader,
        kPrepareBatchRendererGroupNodes,
        kplayerinfo
    };

    static RecorderEntry[] recordersList =
    {
        new RecorderEntry() { name="RenderLoop.Draw" },
        new RecorderEntry() { name="Shadows.Draw" },
        new RecorderEntry() { name="SRPBatcher.Draw", oldName="RenderLoopNewBatcher.Draw" },
        new RecorderEntry() { name="SRPBatcherShadow.Draw", oldName="ShadowLoopNewBatcher.Draw" },
        new RecorderEntry() { name="RenderLoopDevice.Idle" },
        new RecorderEntry() { name="StdRender.ApplyShader" },
        new RecorderEntry() { name="StdShadow.ApplyShader" },
        new RecorderEntry() { name="SRPBRender.ApplyShader" },
        new RecorderEntry() { name="SRPBShadow.ApplyShader" },
        new RecorderEntry() { name="PrepareBatchRendererGroupNodes" },
        new RecorderEntry() { name="playerinfo :" }

    };

    static void InitRecorderList()
    {
        for (int i = 0; i < recordersList.Length; i++)
        {
            var sampler = Sampler.Get(recordersList[i].name);
            if (sampler.isValid)
                recordersList[i].recorder = sampler.GetRecorder();
            else if (recordersList[i].oldName != null)
            {
                sampler = Sampler.Get(recordersList[i].oldName);
                if (sampler.isValid)
                    recordersList[i].recorder = sampler.GetRecorder();
            }
        }
    }

    #endregion

    static readonly int c_nDefaultNum = -1;
    static string startTime;
    static string endTime;

    static List<ProfileData> datas;
    static DateTime dateTime;
    public static Camera camera;

    static Editor_Profiler window;
    [MenuItem("TA/Open Profiler #1", false, priority: 1)]
    static void OpenWindow()
    {
        if (EditorApplication.isPlaying)
        {

            window = GetWindow<Editor_Profiler>("Profiler");

            datas = new List<ProfileData>();

            startTime = DateTime.Now.ToString();

            InitRecorderList();

            RazCounters();

            EditorApplication.update += Update;

            EditorApplication.playModeStateChanged += EditorApplication_playModeStateChanged;
            EditorApplication.pauseStateChanged += EditorApplication_pauseStateChanged;

            dateTime = DateTime.Now;
        }
        else
        {
            EditorUtility.DisplayDialog("TAEditor_Profiler", "인게임 내에서만 가능합니다.", "확인");
        }
    }

    static void EditorApplication_pauseStateChanged(PauseState obj)
    {
        EditorApplication.pauseStateChanged -= EditorApplication_pauseStateChanged;
        EditorApplication.playModeStateChanged -= EditorApplication_playModeStateChanged;
        SaveData();
    }

    static void EditorApplication_playModeStateChanged(PlayModeStateChange obj)
    {
        EditorApplication.pauseStateChanged -= EditorApplication_pauseStateChanged;
        EditorApplication.playModeStateChanged -= EditorApplication_playModeStateChanged;
        SaveData();
    }

    static void Update()
    {
        if (EditorApplication.isPlaying)
        {
            m_AccDeltaTime += Time.unscaledDeltaTime;
            m_frameCount++;

            for (int i = 0; i < recordersList.Length; i++)
            {
                if (recordersList[i].recorder != null)
                {
                    recordersList[i].accTime += recordersList[i].recorder.elapsedNanoseconds / 1000000.0f;      // acc time in ms
                    recordersList[i].callCount += recordersList[i].recorder.sampleBlockCount;
                }
            }

            if (m_AccDeltaTime >= kAverageStatDuration)
            {

                float ooFrameCount = 1.0f / (float)m_frameCount;

                float avgStdRender = recordersList[(int)SRPBMarkers.kStdRenderDraw].accTime * ooFrameCount;
                float avgStdShadow = recordersList[(int)SRPBMarkers.kStdShadowDraw].accTime * ooFrameCount;
                float avgSRPBRender = recordersList[(int)SRPBMarkers.kSRPBRenderDraw].accTime * ooFrameCount;
                float avgSRPBShadow = recordersList[(int)SRPBMarkers.kSRPBShadowDraw].accTime * ooFrameCount;
                float avgPIRPrepareGroupNodes = recordersList[(int)SRPBMarkers.kPrepareBatchRendererGroupNodes].accTime * ooFrameCount;

                ProfileData data = new ProfileData();

                data.cpuRenderingTime = avgStdRender + avgStdShadow + avgSRPBRender + avgSRPBShadow + avgPIRPrepareGroupNodes;

                data.fps = (float)m_frameCount / m_AccDeltaTime;
                data.srpObjectCall = recordersList[(int)SRPBMarkers.kSRPBRenderApplyShader].callCount / m_frameCount;
                data.srpShadowCall = recordersList[(int)SRPBMarkers.kSRPBShadowApplyShader].callCount / m_frameCount;
                data.stdObjectCall = recordersList[(int)SRPBMarkers.kStdRenderApplyShader].callCount / m_frameCount;
                data.stdShadowCall = recordersList[(int)SRPBMarkers.kStdShadowApplyShader].callCount / m_frameCount;

                data.drawCall = UnityStats.drawCalls;
                data.verts = UnityStats.vertices;
                data.tri = UnityStats.triangles;

                if (camera != null)
                {
                    data.cameraPosition = camera.transform.position;
                    data.cameraRotation = camera.transform.eulerAngles;
                }
                else
                {
                    data.cameraPosition = Camera.main.transform.position;
                    data.cameraRotation = Camera.main.transform.eulerAngles;
                }


                datas.Add(data);

                RazCounters();
            }
        }
    }

    static void RazCounters()
    {
        m_AccDeltaTime = 0.0f;
        m_frameCount = 0;
        for (int i = 0; i < recordersList.Length; i++)
        {
            recordersList[i].accTime = 0.0f;
            recordersList[i].callCount = 0;
        }
    }

    void OnGUI()
    {


        GUILayout.Label("프로파일링 데이터 수집 중입니다. 플레이를 중지하면, 데이터를 엑셀로 저장합니다. SaveData 버튼을 눌러 즉시 데이터를 저장 가능합니다.");
        GUILayout.Label("측정 중 한 장소에 오래 머무는 것은 데이터 평균 값을 낮춰 데이터의 신빙성을 낮춥니다. 측정 중 쉬지 않고 플레이해주십시오.");
        if (GUILayout.Button("SaveData"))
        {
            SaveData();
        }

        GUILayout.Label("버튼 클릭 시점부터 데이터를 다시 측정합니다.");
        if (GUILayout.Button("Reset Data"))
        {
            startTime = DateTime.Now.ToString();
            datas.Clear();
            RazCounters();
        }
    }
    static void SaveData()
    {
        endTime = DateTime.Now.ToString();

        if (datas == null || datas.Count <= 0)
            return;
        EditorApplication.update -= Update;
        #region Excel
        // Export Excel Data
        string exportPath = EditorUtility.SaveFilePanel("Save SkuInfo File", "", "ProfileData-Export.xlsx", "xlsx");
        if (string.IsNullOrEmpty(exportPath))
        {
            return;
        }
        if (File.Exists(exportPath))
        {
            File.Delete(exportPath);
        }
        using (ExcelPackage package = new ExcelPackage(new FileInfo(exportPath)))
        {
            ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Data");

            worksheet.Column(1).Width = 10;
            worksheet.Column(2).Width = 10;
            worksheet.Column(3).Width = 12;
            worksheet.Column(4).Width = 12;
            worksheet.Column(5).Width = 12;
            worksheet.Column(6).Width = 12;
            worksheet.Column(7).Width = 12;
            worksheet.Column(8).Width = 20;
            worksheet.Column(9).Width = 20;
            worksheet.Column(10).Width = 10;
            worksheet.Column(11).Width = 10;
            worksheet.Column(12).Width = 10;
            worksheet.Column(13).Width = 10;
            worksheet.Column(14).Width = 10;
            worksheet.Column(15).Width = 10;

            for (int i = 1; i <= 20; i++)
            {
                worksheet.Column(i).Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
            }

            string[] tableTitle = new string[] { "FPS", "CR Time", "SRP Obj","SRP Shadow","STD Obj","STD Shadow", "Draw Call", "Verts","Tri", "SRPRate", "STDRate", "DRate", "VRate"
                ,"cameraPosition","cameraRotation","Quality"};

            for (int i = 0; i < tableTitle.Length; i++)
            {
                worksheet.Cells[3, i + 1].Value = tableTitle[i];
                worksheet.Cells[3, i + 1].Style.Font.Bold = true;
                worksheet.Cells[3, i + 1].Style.Font.Size = 12;
                worksheet.Cells[3, i + 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
            }

            float averagefps = 0f;
            float averagesrpCall = 0f;
            float averagestdCall = 0f;
            float averageDrawCall = 0f;
            float averageVert = 0f;


            for (int i = 0; i < datas.Count; i++)
            {
                averagefps += datas[i].fps;
                averagesrpCall += datas[i].srpObjectCall + datas[i].srpShadowCall;
                averagestdCall += datas[i].stdObjectCall + datas[i].stdShadowCall;
                averageVert += datas[i].verts;

                averageDrawCall += datas[i].drawCall;
            }

            averagefps /= datas.Count;
            averagesrpCall /= datas.Count;
            averagestdCall /= datas.Count;
            averageVert /= datas.Count;
            averageDrawCall /= datas.Count;

            worksheet.Cells[1, 1].Value = $"{startTime} ~ {endTime}";
            worksheet.Cells[1, 1].Style.Font.Bold = true;
            worksheet.Cells[1, 1].Style.Font.Size = 15;
            worksheet.Cells[1, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Left;

            worksheet.Cells[2, 1].Value = $"Total {datas.Count} Record -> Average [ FPS : {averagefps:F2} / SRP Object+shadow : {averagesrpCall:F0}/ STD Object+shadow : {averagestdCall:F0}/ Draw Call : {averageDrawCall:F0} / Verts {averageVert:F0} ]";
            worksheet.Cells[2, 1].Style.Font.Bold = true;
            worksheet.Cells[2, 1].Style.Font.Size = 15;
            worksheet.Cells[2, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Left;

            for (int i = 0; i < datas.Count; i++)
            {
                worksheet.Cells[i + 4, 1].Value = $"{datas[i].fps:F2}";
                worksheet.Cells[i + 4, 2].Value = $"{datas[i].cpuRenderingTime:F2}ms";
                worksheet.Cells[i + 4, 3].Value = datas[i].srpObjectCall;
                worksheet.Cells[i + 4, 4].Value = datas[i].srpShadowCall;
                worksheet.Cells[i + 4, 5].Value = datas[i].stdObjectCall;
                worksheet.Cells[i + 4, 6].Value = datas[i].stdShadowCall;

                worksheet.Cells[i + 4, 7].Value = datas[i].drawCall;
                worksheet.Cells[i + 4, 8].Value = datas[i].verts;
                worksheet.Cells[i + 4, 9].Value = datas[i].tri;

                float ssrpRate = (datas[i].srpObjectCall + datas[i].srpShadowCall) / averagesrpCall;
                worksheet.Cells[i + 4, 10].Value = $"{(ssrpRate * 100):F0}%";

                float sstdRate = (datas[i].stdObjectCall + datas[i].stdShadowCall) / averagestdCall;
                worksheet.Cells[i + 4, 11].Value = $"{(sstdRate * 100):F0}%";


                float dRate = datas[i].drawCall / averageDrawCall;
                worksheet.Cells[i + 4, 12].Value = $"{(dRate * 100):F0}%";

                float vRate = datas[i].verts / averageVert;
                worksheet.Cells[i + 4, 13].Value = $"{(vRate * 100):F0}%";


                worksheet.Cells[i + 4, 14].Value = datas[i].cameraPosition;
                worksheet.Cells[i + 4, 15].Value = datas[i].cameraRotation;
            }

            package.Save();
            exportPath = exportPath.Replace('/', '\\');
#if UNITY_EDITOR_WIN
            System.Diagnostics.Process p = new System.Diagnostics.Process();
            p.StartInfo.FileName = "explorer.exe";
            p.StartInfo.Arguments = (@" /select," + exportPath);
            p.Start();
#endif

            window.Close();

        }

        #endregion
    }

    protected void OnDestroy()
    {
        Debug.Log("OnDestroy");
        EditorApplication.update -= Update;

        datas.Clear();
        datas = null;

        window = null;
    }
}

#endif