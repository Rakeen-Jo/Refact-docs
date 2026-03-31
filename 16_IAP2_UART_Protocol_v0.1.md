# 16. IAP2 UART Protocol v0.1 (Custom, YMODEM 대체)

## 목적
- YMODEM handshake 불안정 구간 제거
- 단계별 ACK/NACK/Progress 가시성 확보
- PC 툴과 FW 간 단순/명시적 프레임 프로토콜

## 전송 채널
- UART2, 921600, 8N1

## 프레임 포맷
```c
struct Iap2Hdr {
  uint32_t magic;   // 0x32504149 ('IAP2')
  uint8_t  type;
  uint16_t seq;
  uint16_t len;
  uint8_t  payload[len];
  uint16_t crc16;   // CRC-CCITT over (type..payload)
};
```

## 타입
- Request
  - `0x01 HELLO`
  - `0x02 META` payload: `uint32 size, uint32 crc32`
  - `0x03 ERASE`
  - `0x04 DATA` payload: `uint32 offset, uint16 n, uint8[n]`
  - `0x05 COMMIT`
  - `0x06 REBOOT`
- Response
  - `0x90 ACK` payload: `uint8 stage, uint32 value`
  - `0x91 NACK` payload: `uint8 err`
  - `0x92 DONE`

## 시퀀스
1. HELLO -> ACK
2. META -> ACK(stage=1)
3. ERASE -> ACK(stage=2)
4. DATA 반복 -> ACK(stage=3, value=written)
5. COMMIT -> DONE (CRC32 검증 + valid flag set)
6. REBOOT -> ACK + reset

## 안전 정책
- 다운로드 시작 전 app valid flag clear
- COMMIT 성공 시에만 app valid flag set
- partial image는 절대 점프하지 않음

## LED 권장 상태
- 대기/다운로드중: LED1 ON, LED2 OFF
- 성공: LED1 ON, LED2 ON
- 실패: LED1 OFF, LED2 ON
