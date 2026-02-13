import base64
from flask import Flask, request, jsonify
import requests

app = Flask(__name__)
MODEL_NAME = "smolvlm256m" #로컬 VLM모델
OLLAMA_API_URL = "http://localhost:11434/api/generate"

@app.route('/pilot', methods=['POST'])
def pilot_drone():
    data = request.json
    if not data or 'image' not in data:
        print("이미지 데이터를 받지 못했습니다.")
        return jsonify({"command": "HOVER"}), 400
        
    image_b64 = data.get('image')

    try:
        with open("debug_unity_image.jpg", "wb") as fh:
            fh.write(base64.b64decode(image_b64))
        print("📸 Unity 이미지 저장 완료 (debug_unity_image.jpg 확인!)")
    except Exception as e:
        print("이미지 디코딩 에러:", e)


    prompt = """Look at the image closerly and choose the best description.
    Do NOT descript, just answer.

    Option A: Green grass ground
    Option B: Dense forest or Mountain Cliffs

    Answer with just one letter in A or B.
    Answer:"""
    payload = {
        "model": MODEL_NAME,
        "prompt": prompt,
        "images": [image_b64],
        "stream": False,
        "options": {"temperature": 0.0, "num_predict": 5} # 대답을 길게 못하게 5로 확 줄임
    }
    
    try:
        resp = requests.post(OLLAMA_API_URL, json=payload).json()
        
        raw_response = resp.get("response", "")
        print(f"RAW VLM 출력: [{raw_response}]")
        
        vlm_answer = raw_response.strip().upper()
        
        command = "HOVER"
        
        if vlm_answer == "A" or vlm_answer.startswith("A") or "OPTION A" in vlm_answer:
            command = "LAND"
            print("[안전] Option A (평탄한 잔디) 감지. 착륙을 허가합니다.")
            
        elif vlm_answer == "B" or vlm_answer.startswith("B") or "OPTION B" in vlm_answer:
            command = "MOVE_NEXT" 
            print("[위험] Option B (숲/절벽) 감지. 안전한 곳으로 이동합니다.")
            
        else:
            command = "MOVE_NEXT" 
            print(f"[판단 불가] 안전을 위해 이동합니다.")
            
        print(f"최종 명령: {command}")
        print("-" * 40)
        
        return jsonify({"command": command})
        
    except Exception as e:
        print("API 통신/파싱 에러:", e)
        return jsonify({"command": "HOVER"})

if __name__ == '__main__':
    print("VLM Server Started on port 5000...")
    app.run(host='0.0.0.0', port=5000)