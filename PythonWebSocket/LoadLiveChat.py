import asyncio
import json
import websockets
import pytchat

# 연동할 유튜브 라이브 스트리밍의 Video ID (예: youtube.com/watch?v=XXXXX 에서 XXXXX 부분)
LIVE_VIDEO_ID = "YOUR_YOUTUBE_LIVE_VIDEO_ID"

async def chat_bridge(websocket):
    print(f"[Bridge] Unity 클라이언트 연결 성공!")
    
    # pytchat을 이용해 유튜브 라이브 채팅 인스턴스 생성
    chat = pytchat.create(video_id=LIVE_VIDEO_ID)
    
    try:
        while chat.is_alive():
            # 새로운 채팅 아이템들을 가져옴
            for c in chat.get().sync_items():
                # 후원(슈퍼챗) 여부 및 금액 판별
                is_superchat = c.amountValue > 0
                
                # 유니티로 보낼 데이터 딕셔너리 구성
                event_data = {
                    "type": "superchat" if is_superchat else "chat",
                    "author": c.author.name,
                    "message": c.message,
                    "amount": c.amountValue,
                    "currency": c.currency
                }
                
                # JSON 문자열로 변환하여 유니티 웹소켓으로 전송
                json_data = json.dumps(event_data, ensure_ascii=False)
                await websocket.send(json_data)
                #print(f"[전송 완료] {c.author.name}: {c.message} (후원: {c.amountValue})")
            
            print("[전송 완료]")

            await asyncio.sleep(0.5) # 과부하 방지 딜레이
            
    except websockets.exceptions.ConnectionClosed:
        print("[Bridge] Unity 클라이언트 연결 해제됨.")
    except Exception as e:
        print(f"[Error] 에러 발생: {e}")

#메인 프로세스로 소켓 대기 상태
async def main():
    async with websockets.serve(chat_bridge,'localhost', 4444):
        print('소켓을 연결합니다.')
        # 무한 대기
        await asyncio.Future()

# 로컬포트 8765로 웹소켓 서버 실행
# start_server = websockets.serve(chat_bridge, "localhost", 4444)

# print("[Bridge] 유튜브 채팅 브릿지 서버 대기 중 (포트: 8765)...")

# # 현재 루프(start_server)에서 진행 중인 동작 종료 대기
# asyncio.get_event_loop().run_until_complete(start_server)
# # 이벤트 발생 루프를 무한 대기 상태로 만들고 외부에서 소켓 연결 시 동작하도록 한다
# asyncio.get_event_loop().run_forever()

if __name__ == '__main__':
    try:
        asyncio.run(main())
    except KeyboardInterrupt:
        print('서버 종료')