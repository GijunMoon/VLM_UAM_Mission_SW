# Week Dev Challenge - 3일차

# 🦅 Cognitive-VLM-Pilot: Edge AI for Autonomous UAVs
VLM(Vision-Language Model)을 활용해 상황을 인지하고 판단하는 하이브리드 드론 제어 아키텍처입니다.

# 1. 프로젝트 배경
**NASA의 화성 탐사 로버에서 영감을 받다**
> NASA의 최신 연구(EELS Project, Cognitive Rover)에 따르면, 통신이 지연되는 화성이나 미지의 환경에서는 단순한 장애물 회피(Obstacle Avoidance)만으로는 부족합니다. 로봇은 "이 지형이 무엇이며(What), 어떻게 상호작용해야 하는가(How)"를 스스로 판단해야 합니다.

본 프로젝트는 이 개념을 지구의 산악 구조(SAR) 및 험지 착륙 시나리오에 적용했습니다. 고성능 클라우드 없이, 제한된 엣지 디바이스에서 작동하는 경량화된 VLM을 통해 드론에게 '인지 능력'을 부여합니다.

# 2. 왜 VLM을 도입하는가?
왜 빠르고 정확한 YOLO나 Lidar 대신 느린 VLM을 쓸까요? 기하학적(Geometric) 센서는 '의미(Semantics)'를 보지 못하기 때문입니다.

# 3. 시스템 아키텍처 (Hybrid Architecture)
빠른 반사신경(Fast Loop)과 깊은 사고(Slow Loop)를 결합한 Dual-Loop 구조를 채택했습니다.

Layer 1: Fast Loop (Reflexive)
- 역할: 자세 제어, 충돌 회피, 즉각적인 경로 수정.

- 기술: RL Based Autopilot, CV.

- 목표: "추락하지 않고, 목표 지점까지 이동한다."

Layer 2: Slow Loop (Cognitive)
- 역할: 고차원 의사결정, 착륙지 재질 분석, 미션 적합성 판단.

- 기술: ROS 2 + VLM .

- 목표: "현재 상황이 안전한지 판단하고, 다음 행동을 계획한다."

# 4. 주요 기능 (Key Capabilities)
1. 맥락 기반 인명 수색

2. 의미론적 착륙지 평가

3. 비정형 위험 감지

# 5. 기술 스택 (Tech Stack)
Hardware
- Drone Platform: PX4 based Platform

- Edge Computer: NVIDIA Jetson Orin Nano

- Sensors: Monocular Camera

Software
- OS: Ubuntu 22.04 LTS

- Middleware: ROS 2 Humble

- AI Engine: Ollama (Local Inference Server)

- Core Model: smolvlm (256M params)

- Simulation: Unity 3D (자체개발)
