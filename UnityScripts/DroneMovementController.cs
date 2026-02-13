using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

// 드론의 현재 상태 정의
public enum DroneState
{
    Hovering,       // 제자리 비행 (미세한 흔들림)
    MovingToTarget, // 목표지점으로 이동 중 (기울임 효과 적용)
    Landing,        // 착륙 시도 중
    Landed          // 착륙 완료 (프로펠러 정지)
}

public class DroneMovementController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;      // 수평 이동 속도
    public float turnSpeed = 5f;      // 회전 속도 (Y축)
    public float landSpeed = 2f;      // 착륙 하강 속도
    public float hoverHeight = 10f;   // 기본 비행 고도
    public LayerMask groundLayer;     // 지면 레이어 (착륙 감지용)

    [Header("Physics & Animation")]
    public float tiltAmount = 3f;     // 이동 시 최대 기울기 각도
    public float tiltSpeed = 4f;      // 기울어지는 속도
    public float smoothTime = 0.3f;   // 이동 부드러움 정도 (낮을수록 빠름)
    
    [Header("Hover Noise (Idle Effect)")]
    public float bobFrequency = 1.5f;   // 둥둥 떠다니는 빈도
    public float bobAmplitude = 0.01f; // 둥둥 떠다니는 높이 범위

    [Header("Propeller Settings")]
    public List<Transform> propellers; // 프로펠러 오브젝트들 (Inspector에서 할당)
    public float propSpeedMultiplier = 1000f; // 기본 회전 속도

    private Vector3 targetPosition;   // 목표 위치
    private float targetYaw;          // 목표 회전각 (Y축)
    private Vector3 currentVelocity;  // SmoothDamp용 참조 변수 (위치)
    private float yawVelocity;        // SmoothDampAngle용 참조 변수 (회전)
    
    // 현재 상태 확인용
    [SerializeField]
    private DroneState currentState = DroneState.Landed; // 시작은 착륙 상태로 가정
    public Text stateText;

    void Start()
    {
        // 초기화: 현재 위치를 기준으로 시작
        targetPosition = transform.position;
        targetYaw = transform.eulerAngles.y;
        
        // 공중에 떠 있다면 바로 호버링으로 간주
        if (transform.position.y > 1f)
        {
            currentState = DroneState.Hovering;
            targetPosition = transform.position;
        }
    }

    void Update()
    {
        HandlePropellers(); // 프로펠러는 항상 상태에 따라 돔

        switch (currentState)
        {
            case DroneState.Hovering:
                stateText.text = "착륙지점 탐색 중";
                break;
            case DroneState.MovingToTarget:
                ProcessMovement();
                stateText.text = "착륙불가 지점";
                ApplyTilt(); // 이동에 따른 기울기 적용
                break;
            case DroneState.Landing:
                stateText.text = "착륙 중";
                ProcessLanding();
                ApplyLeveling(); // 착륙 중에는 수평 맞추기
                break;
            case DroneState.Landed:
                stateText.text = "착륙";
                // 착륙 상태에서는 위치 고정 및 엔진 끄기 로직 등이 들어갈 수 있음
                break;
        }
    }

    // === 핵심: 부드러운 이동 및 호버링 처리 ===
    void ProcessMovement()
    {
        // 1. 기본 위치 이동 (SmoothDamp)
        // 호버링 중일 때는 Bobbing(위아래 흔들림) 효과를 목표 위치에 더해줌
        Vector3 bobbingOffset = Vector3.zero;
        if (currentState == DroneState.Hovering)
        {
            bobbingOffset = Vector3.up * Mathf.Sin(Time.time * bobFrequency) * bobAmplitude;
        }

        Vector3 finalTargetPos = targetPosition + bobbingOffset;
        transform.position = Vector3.SmoothDamp(transform.position, finalTargetPos, ref currentVelocity, smoothTime, moveSpeed);

        // 2. Y축 회전 (Yaw) 처리
        float currentYaw = transform.eulerAngles.y;
        float smoothedYaw = Mathf.SmoothDampAngle(currentYaw, targetYaw, ref yawVelocity, 0.1f);
        
        // 회전 적용 (여기서는 Y축만, X/Z 기울기는 ApplyTilt에서 처리)
        Vector3 currentEuler = transform.rotation.eulerAngles;
        transform.rotation = Quaternion.Euler(currentEuler.x, smoothedYaw, currentEuler.z);

        // 목표 근처 도달 체크
        if (Vector3.Distance(transform.position, targetPosition) < 0.2f && currentState == DroneState.MovingToTarget)
        {
            currentState = DroneState.Hovering;
        }
    }

    // === 물리적 기울임(Tilt) 효과 ===
    void ApplyTilt()
    {
        // 드론의 이동 속도를 로컬 좌표계로 변환 (앞으로 가면 +Z, 오른쪽으로 가면 +X)
        Vector3 localVelocity = transform.InverseTransformDirection(currentVelocity);

        // 속도에 비례해서 목표 기울기 계산
        // 앞으로 갈 때(Velocity Z > 0) -> 앞으로 숙여야 함(Rotate X > 0)
        // 오른쪽으로 갈 때(Velocity X > 0) -> 오른쪽으로 기울여야 함(Rotate Z < 0)
        float targetPitch = localVelocity.z * tiltAmount; 
        float targetRoll = -localVelocity.x * tiltAmount;

        // 현재 기울기에서 목표 기울기로 부드럽게 전환 (Lerp)
        float currentPitch = transform.localEulerAngles.x;
        float currentRoll = transform.localEulerAngles.z;
        
        // 각도 보정 (0~360도 문제를 -180~180도로 변환하여 계산)
        if (currentPitch > 180) currentPitch -= 360;
        if (currentRoll > 180) currentRoll -= 360;

        float newPitch = Mathf.Lerp(currentPitch, targetPitch, Time.deltaTime * tiltSpeed);
        float newRoll = Mathf.Lerp(currentRoll, targetRoll, Time.deltaTime * tiltSpeed);

        // Y축 회전은 유지하면서 X, Z축 기울기만 적용
        transform.rotation = Quaternion.Euler(newPitch, transform.eulerAngles.y, newRoll);
    }

    // === 착륙 중 수평 맞추기 ===
    void ApplyLeveling()
    {
        // 착륙 중에는 기울기를 0으로 복구
        float currentPitch = transform.eulerAngles.x;
        float currentRoll = transform.eulerAngles.z;

        // 보간을 이용해 0도로 복귀
        float newPitch = Mathf.LerpAngle(currentPitch, 0, Time.deltaTime * tiltSpeed);
        float newRoll = Mathf.LerpAngle(currentRoll, 0, Time.deltaTime * tiltSpeed);

        transform.rotation = Quaternion.Euler(newPitch, transform.eulerAngles.y, newRoll);
    }

    // === 프로펠러 애니메이션 ===
    void HandlePropellers()
    {
        if (propellers == null || propellers.Count == 0) return;

        float currentPropSpeed = 0f;

        // 상태에 따른 회전 속도 설정
        switch (currentState)
        {
            case DroneState.Hovering:
                currentPropSpeed = propSpeedMultiplier;
                break;
            case DroneState.MovingToTarget:
                currentPropSpeed = propSpeedMultiplier * 1.5f; // 이동 시 더 빨리 돔
                break;
            case DroneState.Landing:
                currentPropSpeed = propSpeedMultiplier * 0.8f; // 착륙 시 약간 감속
                break;
            case DroneState.Landed:
                currentPropSpeed = Mathf.Lerp(currentPropSpeed, 0f, Time.deltaTime); // 서서히 멈춤
                break;
        }

        // 모든 프로펠러 회전
        foreach (var prop in propellers)
        {
            if (prop != null)
                prop.Rotate(Vector3.up, currentPropSpeed * Time.deltaTime);
        }
    }

    // === 착륙 로직 처리 ===
    void ProcessLanding()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up, Vector3.down, out hit, 50f, groundLayer))
        {
            float distanceToGround = hit.distance - 1f;

            if (distanceToGround > 0.1f)
            {
                // 부드러운 하강을 위해 Lerp 사용 대신 일정한 속도로 내리되, 바닥에 가까워지면 감속
                float descent = landSpeed * Time.deltaTime;
                if (distanceToGround < 1f) descent *= 0.5f; // 바닥 근처에서 감속

                transform.Translate(Vector3.down * descent, Space.World);
                targetPosition = transform.position; 
            }
            else
            {
                currentState = DroneState.Landed;
                Debug.Log("🛬 착륙 완료!");
            }
        }
        else
        {
            transform.Translate(Vector3.down * landSpeed * Time.deltaTime, Space.World);
        }
    }

    // === 명령 수신 함수 (기존 유지 + 일부 개선) ===
    public void ReceiveCommand(string command)
    {
        if (currentState == DroneState.Landed && !command.Contains("TAKEOFF")) return;

        Debug.Log($"명령 수신: [{command}]");
        command = command.ToUpper().Trim();

        if (command.Contains("TAKEOFF"))
        {
            currentState = DroneState.MovingToTarget;
            targetPosition = new Vector3(transform.position.x, hoverHeight, transform.position.z);
        }
        else if (command.Contains("LAND"))
        {
            currentState = DroneState.Landing;
        }
        else if (command.Contains("HOVER"))
        {
            currentState = DroneState.Hovering;
            targetPosition = transform.position;
        }
        else if (command.Contains("MOVE_NEXT"))
        {
            currentState = DroneState.MovingToTarget;
            targetPosition = transform.position + transform.forward * 10f;
            targetPosition.y = hoverHeight; // 고도 유지
        }
        else if (command.Contains("RETURN"))
        {
            currentState = DroneState.MovingToTarget;
            targetPosition = new Vector3(0, hoverHeight, 0);
        }
    }
}