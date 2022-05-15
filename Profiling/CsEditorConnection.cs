using System;
using UnityEngine;
using UnityEditor;
using System.Text;
using UnityEditor.Networking.PlayerConnection;
using UnityEngine.Networking.PlayerConnection;
using UnityEngine.Profiling;
using UnityEditorInternal;
using System.IO;
using OfficeOpenXml;
using System.Collections.Generic;
public class CsEditorConnection : EditorWindow
{
    #region srp
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

    };
   static void InitRecorderList()
    {
        for (int i = 0; i < recordersList.Length; i++)
        {
            var sampler = Sampler.Get(recordersList[i].name);
            if (sampler.isValid)
			{
                recordersList[i].recorder = sampler.GetRecorder();
            }            
            else if (recordersList[i].oldName != null)
            {
                sampler = Sampler.Get(recordersList[i].oldName);
                if (sampler.isValid)
				{
                    recordersList[i].recorder = sampler.GetRecorder();
                }
                    
            }
        }
    }

    #endregion

    public static readonly Guid kMsgSendEditorToPlayer = new Guid("FD713788-B5AE-49FF-8B2C-F311B9CB0CC1");
    public static readonly Guid kMsgSendPlayerToEditor = new Guid("FD713788-B5AE-49FF-8B2C-F311B9CB0CC2");

    public static readonly Guid kMsgSendDestory = new Guid("FD713788-B5AE-49FF-8B2C-F311B9CB0CC3");
    public static readonly Guid kMsgSendSaveReset = new Guid("FD713788-B5AE-49FF-8B2C-F311B9CB0CC4");

    const int c_nPlayerInfoCount = 8;

    static string startTime;
    static string endTime;

    static byte[] aBytePlayerInfo;
    static double m_AccDeltaTime = 0f;
    static double m_SaveDeltaTime = 0f;
    static int m_frameCount;

    readonly float kAverageStatDuration = 1.0f;

    enum PlayerInfo
    {
        QuestNo,
        LocationId,
        PlayerPos,
        CameraPos,
        CameraRot,
        Direction,
        GraphicQuality
    };

    class PlayerInfoData
	{
        public string cpuRenderingTime;
        public string fps;

        public string srpObjectCall;
        public string srpShadowCall;
        public string stdObjectCall;
        public string stdShadowCall;

        public string drawCall;
        public string setPassCall;
        public string verts;
        public string tri;

        public string locationId; // -> 엑셀 입력 때 SceneName으로 변환 입력
        public string questNo; // -> 메인 퀘스트 NO
        public string directing;
        public string playerPosition; // 플레이어 포지션
        public string cameraPosition; // 
        public string cameraRotation;

        public string qualityNo;
    }

    static int[] anProfilerData = new int[4];
    static string strPlayerInfo = string.Empty;
    static string[] aStrPlayerInfo = new string[c_nPlayerInfoCount];
    static List<PlayerInfoData> listInfo = new List<PlayerInfoData>();


    [MenuItem("TA/EditorConnection #2", false, priority: 2)]
    static void Init()
    {
        CsEditorConnection window = (CsEditorConnection)EditorWindow.GetWindow(typeof(CsEditorConnection));
        window.Show();
        window.titleContent = new GUIContent("EditorConnection");

        startTime = DateTime.Now.ToString();

        InitRecorderList();
    }

    void OnEnable()
    {
        EditorConnection.instance.Initialize();
        EditorConnection.instance.Register(kMsgSendPlayerToEditor, OnMessageEvent);
        EditorConnection.instance.Register(kMsgSendDestory, OnMessageDestory);
    }

    void OnDisable()
    {
        EditorConnection.instance.Unregister(kMsgSendPlayerToEditor, OnMessageEvent);
        EditorConnection.instance.Unregister(kMsgSendDestory, OnMessageDestory);
        EditorConnection.instance.DisconnectAll();
    }

    private void OnMessageEvent(MessageEventArgs args)
    {
        aBytePlayerInfo = args.data;
    }

    private void OnMessageDestory(MessageEventArgs args)
    {
        aBytePlayerInfo = null;
    }

    void OnGUI()
    {
        var playerCount = EditorConnection.instance.ConnectedPlayers.Count;
     
        if (playerCount > 0)
        {
            GUILayout.Label("플레이어가 연결되었습니다.");
            GUILayout.Label("입력된 데이터 수 : " + listInfo.Count);
            GUILayout.Label("인게임이 아니거나 전체 팝업이 켜져있을 경우\n 데이터가 입력되지 않습니다.");
            if (GUILayout.Button("Save Data"))
            {
                SaveData();
            }

            if (GUILayout.Button("Reset Data"))
            {
                ResetData();
            }
        }
        else
        {
            GUILayout.Label("연결된 플레이어가 없습니다.");

			if (listInfo != null && listInfo.Count > 0)
			{
                SaveData();
            }
        }

		
    }

    void Update()
	{

        if (EditorConnection.instance.ConnectedPlayers.Count <= 0 || !(aBytePlayerInfo != null && aBytePlayerInfo.Length != 0))
		{
            RazCounters();
            return;
        }
           

        m_AccDeltaTime = EditorApplication.timeSinceStartup;
        m_frameCount++;

        for (int i = 0; i < recordersList.Length; i++)
        {
            if (recordersList[i].recorder != null)
            {
                recordersList[i].recorder.CollectFromAllThreads();
                recordersList[i].accTime += recordersList[i].recorder.elapsedNanoseconds / 1000000.0f;      // acc time in ms
                recordersList[i].callCount += recordersList[i].recorder.sampleBlockCount;
            }
        }

        if (m_AccDeltaTime >= m_SaveDeltaTime + kAverageStatDuration)
        {

            float ooFrameCount = 1.0f / (float)m_frameCount;

            PlayerInfoData data = new PlayerInfoData();
            var statistics = ProfilerDriver.GetGraphStatisticsPropertiesForArea(ProfilerArea.Rendering);
            foreach (var propertyName in statistics)
            {
                var id = ProfilerDriver.GetStatisticsIdentifierForArea(ProfilerArea.Rendering, propertyName);
                var buffer = new float[1];
                ProfilerDriver.GetStatisticsValues(id, ProfilerDriver.lastFrameIndex, 1, buffer, out var maxValue);
                if (propertyName == "Batches") data.drawCall = ((int)buffer[0]).ToString();
                else if (propertyName == "SetPass Calls") data.setPassCall = ((int)buffer[0]).ToString();
                else if (propertyName == "Triangles") data.tri = ((int)buffer[0]).ToString();
                else if (propertyName == "Vertices") data.verts = ((int)buffer[0]).ToString();
            }
         
            strPlayerInfo = Encoding.ASCII.GetString(aBytePlayerInfo);

            if (strPlayerInfo.Length != 0)
            {
                aStrPlayerInfo = strPlayerInfo.Split(":".ToCharArray());
				if (aStrPlayerInfo.Length >= 8 )
				{
                    data.cameraPosition = aStrPlayerInfo[0];
                    data.cameraRotation = aStrPlayerInfo[1];
                    data.fps = aStrPlayerInfo[2];
				}
				else
				{
                    Debug.LogError("데이터의 갯수가 맞지 않습니다.");
				}
			}

            m_SaveDeltaTime = m_AccDeltaTime;

            listInfo.Add(data);

            RazCounters();
        }
    }
    static void RazCounters()
    {
        m_AccDeltaTime = 0.0f;
        m_frameCount = 0;
        aBytePlayerInfo = null;
        for (int i = 0; i < recordersList.Length; i++)
        {
            recordersList[i].accTime = 0.0f;
            recordersList[i].callCount = 0;
        }
    }

	void SaveData()
    {
        endTime = DateTime.Now.ToString();

        if (listInfo == null || listInfo.Count <= 0)
            return;

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
            //worksheet.Column(2).Width = 10;
            //worksheet.Column(3).Width = 12;
            //worksheet.Column(4).Width = 12;
            //worksheet.Column(5).Width = 12;
            //worksheet.Column(6).Width = 12;
            worksheet.Column(2).Width = 12;
            worksheet.Column(3).Width = 12;
            worksheet.Column(4).Width = 20;
            worksheet.Column(5).Width = 20;
            //worksheet.Column(11).Width = 10;
            //worksheet.Column(12).Width = 10;
            worksheet.Column(6).Width = 10;
            worksheet.Column(7).Width = 10;
            worksheet.Column(8).Width = 10;
            worksheet.Column(9).Width = 10;
            worksheet.Column(10).Width = 10;
            worksheet.Column(11).Width = 20;
            worksheet.Column(12).Width = 20;
            worksheet.Column(13).Width = 20;
            worksheet.Column(14).Width = 10;

            for (int i = 1; i <= 21; i++)
            {
                worksheet.Column(i).Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
            }

            string[] tableTitle = new string[] { "FPS", "Draw Call", "SetPass Call", "Verts","Tri", "DRate", "VRate",
               "cameraPosition","cameraRotation"};

            for (int i = 0; i < tableTitle.Length; i++)
            {
                worksheet.Cells[3, i + 1].Value = tableTitle[i];
                worksheet.Cells[3, i + 1].Style.Font.Bold = true;
                worksheet.Cells[3, i + 1].Style.Font.Size = 12;
                worksheet.Cells[3, i + 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
            }

			float averagefps = 0f;
			float averageDrawCall = 0f;
			float averageVert = 0f;

			for (int i = 0; i < listInfo.Count; i++)
			{
				averagefps += float.Parse(listInfo[i].fps);
				averageVert += float.Parse(listInfo[i].verts);

				averageDrawCall += float.Parse(listInfo[i].drawCall);
			}
			averagefps /= listInfo.Count;
			averageVert /= listInfo.Count;
			averageDrawCall /= listInfo.Count;

			worksheet.Cells[1, 1].Value = $"{startTime} ~ {endTime}";
            worksheet.Cells[1, 1].Style.Font.Bold = true;
            worksheet.Cells[1, 1].Style.Font.Size = 15;
            worksheet.Cells[1, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Left;

            worksheet.Cells[2, 1].Value = $"Total {listInfo.Count} Record -> Average [ FPS : {averagefps:F2} / Draw Call : {averageDrawCall:F0} / Verts {averageVert:F0} ]";
            worksheet.Cells[2, 1].Style.Font.Bold = true;
            worksheet.Cells[2, 1].Style.Font.Size = 15;
            worksheet.Cells[2, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Left;

            for (int i = 0; i < listInfo.Count; i++)
            {
                worksheet.Cells[i + 4, 1].Value = $"{listInfo[i].fps:F2}";
                worksheet.Cells[i + 4, 2].Value = listInfo[i].drawCall;
                worksheet.Cells[i + 4, 3].Value = listInfo[i].setPassCall;
                worksheet.Cells[i + 4, 4].Value = listInfo[i].verts;
                worksheet.Cells[i + 4, 5].Value = listInfo[i].tri;

                float dRate = float.Parse(listInfo[i].drawCall) / averageDrawCall;
                worksheet.Cells[i + 4, 6].Value = $"{(dRate * 100):F0}%";

                float vRate = float.Parse(listInfo[i].verts) / averageVert;
                worksheet.Cells[i + 4, 7].Value = $"{(vRate * 100):F0}%";

                worksheet.Cells[i + 4, 8].Value = listInfo[i].cameraPosition;
                worksheet.Cells[i + 4, 9].Value = listInfo[i].cameraRotation;

            }

            package.Save();
            exportPath = exportPath.Replace('/', '\\');
#if UNITY_EDITOR_WIN
            System.Diagnostics.Process p = new System.Diagnostics.Process();
            p.StartInfo.FileName = "explorer.exe";
            p.StartInfo.Arguments = (@" /select," + exportPath);
            p.Start();
#endif
            this.Close();
        }

        #endregion
    }


    void ResetData()
    {
        startTime = DateTime.Now.ToString();
        listInfo.Clear();
    }
}