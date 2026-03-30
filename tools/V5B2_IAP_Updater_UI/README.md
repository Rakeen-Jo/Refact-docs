# V5B2 IAP Updater UI (WinForms)

기능:
- COM 포트 자동 인식 + 목록 선택
- Baud 선택
- BIN 파일 로컬 선택
- Start Download / Cancel 버튼
- 진행률 표시 + 로그 창

자동 시퀀스:
1. SPACE 전송 (boot countdown break)
2. `Input Password` 대기
3. `wonik1234` 전송
4. `Main Menu` 대기
5. `1` 전송
6. YMODEM 전송
7. 성공 문자열 대기

## Build (Visual Studio)
- `V5B2_IAP_Updater_UI.csproj` 열기
- Run

## 참고
- GitHub 자동 다운로드 기능은 의도적으로 미포함 (요청사항)
- 비밀번호/토큰 문자열이 바뀌면 `MainForm.cs` 상수/WaitContains 토큰 수정 필요
