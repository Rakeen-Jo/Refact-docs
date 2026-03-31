# V5B2 Communication Protocol v1.3 (FW: 02_V5B2_SENSE_Refact_2ea0133 @ c1e6469)

## 1. 기본 네트워크 값
- IP: `192.168.0.50`
- Netmask: `255.255.255.0`
- Gateway: `192.168.0.1`
- UDP Port: `7000`
- TCP Port: `23`

## 2. 공통 헤더 (TCP/UDP 동일)
- `magic` (u32): `0x32423556` (`"V5B2"`)
- `version` (u8): `0x01`
- `type` (u8)
- `length` (u16)
- `seq` (u32)
- `timestamp_ms` (u32)

Header size = 16 bytes (packed, little-endian)

## 3. 메시지 타입
- `0x01` HELLO_REQ (PC->FW, TCP)
- `0x02` HELLO_RESP (FW->PC, TCP, 7 bytes)
- `0x10` TELEMETRY_RAW (FW->PC, UDP)
- `0x11` TORQUE_CMD (PC->FW, **UDP only**)
- `0x20` TTL_REQ / `0x21` TTL_RESP
- `0x22` SN_READ_REQ / `0x23` SN_RESP / `0x24` SN_WRITE_REQ
- `0x25` JAC_REQ / `0x26` CAL_DONE
- `0x27` LED_ALT_REQ / `0x28` LED_BLUE_REQ
- `0x29` NET_CONFIG_REQ / `0x2A` NET_CONFIG_RESP / `0x2B` NET_CONFIG_WRITE_REQ
- `0x30` MOTOR_ERROR

## 4. Payload 구조
### 4.1 TELEMETRY_RAW (164B)
- `sample_seq` u32
- `position[16]` int16
- `current[16]` int16
- `sensor[16]` uint16
- `velocity[16]` int32

총 패킷 크기: 180B (16+164)

### 4.2 TORQUE_CMD (32B)
- `torque_mA[16]` int16

총 패킷 크기: 48B (16+32)

### 4.3 TTL_RESP (8B)
- bytes[0..3]: motor mask
- bytes[4..7]: sensor mask

### 4.4 NET_CONFIG (14B)
- ip(u32), mask(u32), gw(u32), tcp_port(u16)

### 4.5 MOTOR_ERROR (2B)
- joint_index_1based(u8), error_status(u8)

## 5. v1.3 핵심 변경점 (c1e6469 반영)
1. UDP 소켓 구조 변경
   - 기존: TX/RX 소켓 분리
   - 현재: **단일 공유 UDP 소켓 1개를 `MY_UDP_PORT(7000)`에 bind**
2. UDP 응답 목적지 포트 결정 방식 변경
   - 기존: 고정 `MY_UDP_PORT`
   - 현재: **수신한 TORQUE_CMD datagram의 source port를 학습하여 해당 포트로 telemetry reply**
3. PC connected-UDP 호환성 개선
   - FW 송신 source port가 bind 포트(7000)로 고정되어 필터링 mismatch를 줄임
4. 실시간 태스크 우선순위 조정
   - UdpTxTask를 AboveNormal로 상향(333Hz cadence 안정화 목적)

## 6. 권장 운용 시퀀스
1. TCP connect
2. HELLO_REQ (`0x01`) 송신
3. UDP로 TORQUE_CMD(`0x11`) 주기 송신
4. FW는 UDP로 TELEMETRY_RAW(`0x10`) 응답 송신
5. TORQUE_CMD 1초 무수신 시 stream stop + torque zero

## 7. PC 구현 주의사항
- UDP 송신 소켓을 매번 새로 만들지 말고 재사용 권장
- UDP source port가 바뀌면 FW reply 대상 포트도 바뀜
- TCP로 TORQUE_CMD 보내도 FW는 무시함(UDP only)
- 헤더 `magic/version/length` 검증 필수

## 8. 코드 기준 파일
- `V5B2/Inc/tcp_hand_proto.h`
- `V5B2/Src/ethernet.c`
- `V5B2/Src/env_flash.c`
- `V5B2/Inc/parms.h`
