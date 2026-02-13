using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;

public class DroneVLM_Client : MonoBehaviour
{
    public Camera droneCamera;
    public DroneMovementController movementController;
    private string serverUrl = "http://127.0.0.1:5000/pilot";
    
    // 통신 중인지 확인하는 플래그
    private bool isWaitingForVLM = false; 

    void Start()
    {
        // InvokeRepeating 대신 코루틴 루프 시작
        StartCoroutine(VisionLoop());
    }

    IEnumerator VisionLoop()
    {
        while (true)
        {
            // VLM이 대답을 안 했으면 대기
            if (!isWaitingForVLM)
            {
                StartCoroutine(CaptureAndSend());
            }
            // 1초마다 상태 체크 (부하 방지)
            yield return new WaitForSeconds(1.0f); 
        }
    }

    IEnumerator CaptureAndSend()
    {
        isWaitingForVLM = true; // 통신 시작 잠금
        Debug.Log("VLM에게 사진 전송 중...");

        // 1. 카메라 캡처
        RenderTexture rt = new RenderTexture(512, 512, 24);
        droneCamera.targetTexture = rt;
        Texture2D screenShot = new Texture2D(512, 512, TextureFormat.RGB24, false);
        droneCamera.Render();
        RenderTexture.active = rt;
        screenShot.ReadPixels(new Rect(0, 0, 512, 512), 0, 0);
        droneCamera.targetTexture = null;
        RenderTexture.active = null;
        Destroy(rt);

        byte[] bytes = screenShot.EncodeToJPG();
        string base64Image = System.Convert.ToBase64String(bytes);
        string jsonPayload = "{\"image\":\"" + base64Image + "\"}";

        UnityWebRequest request = new UnityWebRequest(serverUrl, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        // 2. 서버 응답 대기
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("VLM 응답 수신 완료!");
            ExecuteCommand(request.downloadHandler.text);
        }
        else
        {
            Debug.LogError("통신 에러: " + request.error);
        }

        isWaitingForVLM = false; // 통신 완료 잠금 해제! 다음 사진 찍을 준비.
    }

    void ExecuteCommand(string jsonResponse)
    {
        string cleanCommand = jsonResponse.Replace("\"", "").Trim();
        if (movementController != null)
        {
            movementController.ReceiveCommand(cleanCommand);
        }
    }
}