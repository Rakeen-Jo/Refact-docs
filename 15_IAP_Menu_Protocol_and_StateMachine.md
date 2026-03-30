# 15. IAP Menu Protocol & State Machine (F429_IAP 기준)

## 대상 코드
- `work/F429_IAP/Core/Src/main.c`
- `work/F429_IAP/Core/Src/common.c`
- `work/F429_IAP/Core/Src/menu.c`
- `work/F429_IAP/Core/Src/ymodem.c`

## UART 기본
- Port: USART2 (`UartHandle=huart2`)
- Baud: **2,000,000**
- Key trigger: 부팅 카운트 중 **SPACE(0x20)**

## 메뉴 진입 시퀀스
1. 부트 카운트 출력(`Booting : 3..0`) 중 SPACE 입력
2. `Input Password : ` 프롬프트 수신
3. 비밀번호 전송: `wonik1234` + CR/LF
4. `Main Menu` 표시 후 옵션 입력
   - `1`: Download image to internal flash (YMODEM receive)

## YMODEM 진입 시퀀스
옵션 1 실행 후 출력:
- `Waiting for the file to be sent ... (press 'a' to abort)`

부트로더 수신기 동작:
- 송신자에게 `C` (CRC16 요청)
- Block#0 수신 (filename + filesize)
- ACK + `C`
- 데이터 블록 수신(ACK 반복)
- EOT 수신 시 ACK + `C`
- Empty block#0 수신 후 ACK

## 자동 업데이트 상태머신 (권장)

### States
- `OpenPort`
- `WaitBootText`
- `SendSpace`
- `WaitPasswordPrompt`
- `SendPassword`
- `WaitMainMenu`
- `SelectDownload`
- `WaitYModemReady`
- `YModemTransfer`
- `WaitSuccessText`
- `Done`
- `Fail`

### Transition 핵심
- `WaitPasswordPrompt` 타임아웃 시 `SendSpace` 재시도
- `WaitMainMenu` 실패 시 비밀번호 재전송 1회
- `YModemTransfer` 실패 시 포트 재오픈 후 1회 재시도
- 성공 문자열(`Programming Completed Successfully`) 확인 후 종료

## 실패 케이스 대응
- Wrong password: 다시 `Input Password` 대기 후 재입력
- no `C` from bootloader: 메뉴 선택 실패 가능성 -> `1` 재전송
- 중간 전송 실패: YMODEM cancel(0x18,0x18) 후 재시작

## 구현 체크리스트
- [ ] COM 포트/baud 선택 가능
- [ ] 로그 창(raw serial) 저장
- [ ] 상태 전이 타임아웃 configurable
- [ ] update 완료 후 자동 reset(선택)
