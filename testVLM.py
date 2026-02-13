import requests
import base64

MODEL_NAME = "smolvlm256m"
OLLAMA_API_URL = "http://localhost:11434/api/generate"
IMAGE_URL = "https://images.unsplash.com/photo-1465056836041-7f43ac27dcb5?q=80&w=1171&auto=format&fit=crop&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D"

def get_vlm_response(prompt, image_b64):
    payload = {
        "model": MODEL_NAME,
        "prompt": prompt,
        "images": [image_b64],
        "stream": False,
        "options": {
            "temperature": 0.0,  # 무작위성 0으로 설정
            "num_predict": 3
        }
    }
    try:
        response = requests.post(OLLAMA_API_URL, json=payload).json()
        return response.get("response", "").strip()
    except Exception as e:
        return "Error"

def run_tuned_mission():
    print("Mission Start...\n")
    
    # 이미지 로드
    img_resp = requests.get(IMAGE_URL)
    img_b64 = base64.b64encode(img_resp.content).decode('utf-8')

    # ==========================================
    # 엔지니어드 프롬프트 셋
    # ==========================================
    tasks = [
        {
            "name": "SAR (인명수색)",
            # 지시(Instruction) + 제약(Constraint) + 트리거(Trigger)
            "prompt": "Look at the image. Is a human visible? Answer **strictly** with 'YES' or 'NO'.\nAnswer:"
        },
        {
            "name": "LANDING (지형판단)",
            "prompt": "Look at the ground. Is it FLAT or ROCKY? Answer with JUST KEYWORD.\nAnswer:"
        },
        {
            "name": "HAZARD (위험감지)",
            "prompt": "Is there fog, snow, or fire? Answer strictly with 'YES' or 'NO'.\nAnswer:"
        }
    ]

    for task in tasks:
        raw_output = get_vlm_response(task["prompt"], img_b64)
        
        # 결과 출력 및 간단 검증
        print(f"[{task['name']}]")
        print(f"   Input Prompt: ...Answer strictly with 'YES' or 'NO'. Answer:")
        print(f"   🤖 Output: '{raw_output}'")
        
        # 모델이 말을 안 듣고 길게 말할 경우를 대비한 안전장치
        clean_output = raw_output.lower().replace(".", "")
        if "yes" in clean_output: final = "✅ DETECTED"
        elif "no" in clean_output: final = "❌ NONE"
        elif "flat" in clean_output: final = "🟢 SAFE (FLAT)"
        elif "rocky" in clean_output: final = "🔴 DANGER (ROCKY)"
        else: final = "⚠️ UNKNOWN"
        
        print(f"   📊 Final Decision: {final}\n")

if __name__ == "__main__":
    run_tuned_mission()