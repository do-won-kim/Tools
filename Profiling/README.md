## 프로파일링을 위해 게임 플레이 중 Drawcall, Vertex, FPS등을 저장하고 엑셀로 뽑는 툴
### 공통  
엑셀 출력을 위해 epplus를 사용하였습니다.  
https://www.nuget.org/packages/EPPlus/  
***  
### Editor  
#### Editor_Profiler.cs  

![editor00](https://user-images.githubusercontent.com/73415970/168468428-69fb36bf-6b6b-4594-a35f-0900aa291254.png)  
  
shift 1 혹은 TA - open profiler에 들어간다.(이때 게임 실행 중이 아니면 인 게임중에만 사용이 가능하다는 팝업이 나온다.)
  
![editor01](https://user-images.githubusercontent.com/73415970/168468441-23a1d989-93f6-46cc-be50-266d7f83606d.png)  
  
해당 팝업이 나온 후 부터 데이터 저장중에 들어간다.  
테스트 하고 싶은 구간을 플레이 한 후 save data 버튼을 누른다.  
  
![editor02](https://user-images.githubusercontent.com/73415970/168468444-f1a587f8-e6cf-4663-a2ba-bd7f66e30b72.png)  
  
엑셀로 저장이 된다.  
  
### Android
***
#### CsEditorConnection.cs, CsSendPlayerInfo.cs  
  
##### vertex같은 데이터는 프로파일러에서 카메라위치 데이터는 CsSendPlayerInfo에서 전송을 해줍니다.  
##### 테스트할 씬에 CsSendPlayerInfo 스크립트를 넣어주셔야 정상동작됩니다.  
##### 카메라 위치 말고도 진행중인 상황을 알 수 있는(퀘스트 번호,캐릭터 위치등)추가 데이터가 필요하면 CsSendPlayerInfo스크립트에 추가  
##### CsEditorConnection 스크립트의 savedata 함수와 update함수 수정해주시면 됩니다.  
1.스마트폰의 개발자모드를 킵니다.  
2.https://docs.unity3d.com/kr/2019.4/Manual/profiler-profiling-applications.html android 원격프로파일링 절차에 따라 앱과 연결해줍니다.  
![android00](https://user-images.githubusercontent.com/73415970/168469044-6b351c8b-6779-4b14-8def-0db63d529794.PNG)  
3.데이터를 프로파일러에서 가져오기 때문에 프로파일러를 열어줍니다(data 저장중에 열려 있어야함).  
![editor00](https://user-images.githubusercontent.com/73415970/168468428-69fb36bf-6b6b-4594-a35f-0900aa291254.png)  
4.shift 2 혹은 TA - EditorConnection에 들어간다.  
![android01](https://user-images.githubusercontent.com/73415970/168469047-906f2f76-b740-4a00-9d09-5cc91207a0b0.PNG)  
![android02](https://user-images.githubusercontent.com/73415970/168469049-15617c3b-4989-42fe-9d54-e7a448bcb378.PNG)  
5.CsSendPlayerInfo와 정상적으로 연결되었으면 아래와 같은 이미지로 팝업창이 바뀝니다.  
6.테스트 후 save data 버튼을 누르면 edior와 같이 엑셀로 저장가능  



