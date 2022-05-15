## 프로파일링을 위해 게임 플레이 중 Drawcall, Vertex, FPS등을 저장하고 엑셀로 뽑는 툴
### 공통  
엑셀 출력을 위해 epplus를 사용하였습니다.  
https://www.nuget.org/packages/EPPlus/  
***
### Android
***
### Editor  
![editor00](https://user-images.githubusercontent.com/73415970/168468428-69fb36bf-6b6b-4594-a35f-0900aa291254.png)  
  
shift 1 혹은 TA - open profiler에 들어간다.(이때 게임 실행 중이 아니면 인 게임중에만 사용이 가능하다는 팝업이 나온다.)
  
![editor01](https://user-images.githubusercontent.com/73415970/168468441-23a1d989-93f6-46cc-be50-266d7f83606d.png)  
  
해당 팝업이 나온 후 부터 데이터 저장중에 들어간다.  
테스트 하고 싶은 구간을 플레이 한 후 save data 버튼을 누른다.  
  
![editor02](https://user-images.githubusercontent.com/73415970/168468444-f1a587f8-e6cf-4663-a2ba-bd7f66e30b72.png)  
  
엑셀로 저장이 된다.
